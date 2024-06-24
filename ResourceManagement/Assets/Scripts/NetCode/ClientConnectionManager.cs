using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine.UIElements;
using System;
using Steamworks.Data;
using Steamworks;
using System.Linq;
using System.Threading.Tasks;
using Unity.Entities.Serialization;
using Unity.Scenes;
using Simulation;
using UnityEditor.PackageManager;

namespace NetCode
{
    public class ClientConnectionManager : MonoBehaviour
    {
        public static ClientConnectionManager Instance { get; private set; }
        
        private enum uiModes { inGame, loading, startOfGame, noSteamClient, chooseMode, setupHost, Host, findLobby, inLobby, noLobbies, pauseMenu }
        private uiModes uiMode = uiModes.chooseMode;
 
        //create all ui elements
        #region 
        public VisualElement uiDoc;
        private TextField _lobbyTitleField => uiDoc.Q<TextField>("_lobbyTitleField");
        private Button _startButton => uiDoc.Q<Button>("_start");
        private Button _setupHostButton => uiDoc.Q<Button>("_setupHost");
        private Button _startGameButton => uiDoc.Q<Button>("_startGame");
        private Button _findLobbyButton => uiDoc.Q<Button>("_findLobby");
        private Button _hostLobbyButton => uiDoc.Q<Button>("_hostLobby");
        private Button _joinLobbyButton => uiDoc.Q<Button>("_joinLobby");
        private Button _cancelLobbyButton => uiDoc.Q<Button>("_cancelLobby");
        private Button _leaveLobbyButton => uiDoc.Q<Button>("_leaveLobby");
        private Button _cancelButton => uiDoc.Q<Button>("_cancel");
        private Button _exitButton => uiDoc.Q<Button>("_exitGame");
        private Button _refreshLobbiesButton => uiDoc.Q<Button>("_refreshLobbies");

        private Label _noLobbiesLabel => uiDoc.Q<Label>("_noLobbies");

        private Label _menuTitle => uiDoc.Q<Label>("menu__title");
        private Label _errorMessage => uiDoc.Q<Label>("_errorMessage");
        private Label _subheader => uiDoc.Q<Label>("_subheader");

        private ListView _lobbiesList => uiDoc.Q<ListView>("_lobbyList");
        private ListView _lobbyMembers => uiDoc.Q<ListView>("_lobbyMembers");

        #endregion
        //ui elements created!

        public List<Friend> membersInLobby = new List<Friend>();
        public bool WorldsAreInitialized { get; private set; }
        public World serverWorld { get; private set; }
        public World clientWorld { get; private set; }
        private Lobby targetLobby;
        private string melon;
        
        public bool IsLobbyHost { get; private set; }
        public bool IsInLobby { get; private set; }
        
        public static bool ShouldInitializeWorlds => Instance.IsInLobby && !Instance.WorldsAreInitialized;
        public bool HasCleanedUpLocalWorld { get; private set; }
        public bool ShouldStartGame { get; private set; }
        private int currentLobbyMemberCount = 0;
        // [SerializeField]
        // SubScene GameplayScene;
        [SerializeField] private string defaultTitle = "Welcome to Ratking";
        [SerializeField] GameObject steamManagerObject;
        //[SerializeField] private FixedString64Bytes defaultScene = "DevinCharacterScene";
        [SerializeField] private string ratKingPass = "abc123throwthemrats";
        [SerializeField] private ushort defaultPort = 7979;
        [SerializeField] GameObject ratKingIPManager;
        //[SerializeField] private SceneAsset MenuScene;
        //[SerializeField] private SceneAsset GameScene;
        private SteamManager SteamManager { get; set; }
        private RatKingIPManager RatKingIPManager { get; set; }
        private ushort Port => defaultPort; //ushort.Parse(defaultPort);

        private void Awake()
        {
            //DontDestroyOnLoad(this);
            Instance = this;
            uiDoc = GetComponent<UIDocument>().rootVisualElement;
            //SteamMatchmaking.OnLobbyDataChanged<SteamManager.currentLobby> += () => { OnMemberJoinedLobby(); };
        }

        private void Start()
        {
            SteamManager = steamManagerObject.GetComponent<SteamManager>();
            RatKingIPManager = ratKingIPManager.GetComponent<RatKingIPManager>();
        }

        private void OnEnable()
        {
            _refreshLobbiesButton.clicked += () => { OnFindLobby(); }; //note that this is the same logic as findLobbies, intentional reuse
            _startButton.clicked += () => { OnStartClicked(); };
            _exitButton.clicked += () => { OnExitClicked(); };
            _findLobbyButton.clicked += () => { OnFindLobby(); };
            _setupHostButton.clicked += () => { OnSetupHost(); };
            _startGameButton.clicked += () => { OnStartGame(); };
            _hostLobbyButton.clicked += () => { OnHostLobby(); };
            _joinLobbyButton.clicked += () => { OnJoinLobby(); };
            _cancelLobbyButton.clicked += () => { OnCancelLobby(); };
            _leaveLobbyButton.clicked += () => { OnLeaveLobby(); };
            _cancelButton.clicked += () => { OnCancelButton(); };
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyChanges;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyChanges;
            //_lobbiesList.RegisterCallback<ChangeEvent<bool>>((evt) => { OnLobbyChosen(evt); });
            //callbacks for when lobbies close or open?

        }

        void Update()
        {
           
            if (HasCleanedUpLocalWorld || !WorldsAreInitialized)
                return;
            
            // (Devin) I think we actually just want to leave it there so we can cleanly exit a game?
            //DestroyLocalSimulationWorld();
        }

        private void OnStartClicked()
        {
            
            if (SteamClient.IsValid)
            {
                setUI(uiModes.chooseMode);
            } else
            {
                _errorMessage.text = "Unable to connect to Steam. Please exit the game, make sure you are connected to the internet, your steam client is running and then restart. Thank you!";
                setUI(uiModes.noSteamClient);
            }
        }

        public void goToMenu()
        {
            setUI(uiModes.pauseMenu);
        }

        private void OnExitClicked()
        {
            Application.Quit();
        }

        async private void OnFindLobby()
        {
            await GetRatKingLobbies();
            var currentSteamLobbies = SteamManager.activeLobbies;
            Debug.Log($"Rat King available count: {currentSteamLobbies.Count}");

            if (currentSteamLobbies.Count > 0)
            {
                uiMode = uiModes.findLobby;
                createLobbyList(currentSteamLobbies);
                setUI(uiMode);
            }
            else
            {
                uiMode = uiModes.noLobbies;
                Debug.Log("No current RK lobbies available");
                setUI(uiMode);
            }
        }

        private void OnCancelButton()
        {
            setUI(uiModes.chooseMode);
        }

        private void OnSetupHost()
        {
            setUI(uiModes.setupHost);
        }

        async private void OnHostLobby()
        {
            setUI(uiModes.loading);
            var createLobby = false;
            try
            {
                createLobby = await SteamManager.CreateLobby();
            }
            catch (Exception exception)
            {
                Debug.Log("UI failed to create lobby");
                Debug.Log(exception.ToString());
            }

            if (!createLobby)
                return;
            
            Debug.Log($"Lobby created: {SteamManager.currentLobby.Id}");
            Debug.Log(SteamManager.currentLobby.ToString());
            Debug.Log($"melon: {RatKingIPManager.myAddressGlobal}");
            SteamManager.currentLobby.SetData("ratMakerId", ratKingPass);
            SteamManager.currentLobby.SetData("mymelon", RatKingIPManager.myAddressGlobal);
            SteamManager.currentLobby.SetData("lobbyName", _lobbyTitleField.text);
            melon = RatKingIPManager.myAddressGlobal;
            setLobbyMemberList(SteamManager.currentLobby.Members.ToList());
            setUI(uiModes.Host);
            IsLobbyHost = true;
            IsInLobby = true;
            //activate callback function to update member list as people join/leave lobby


            // (Devin) HostSceneLoaderSystem should handle this now
            //DestroyLocalSimulationWorld();
            //StartServer();
            //GameplayScene.enabled = true;
            //StartClient();
        }

        private void OnLobbyChanges(Lobby lobby, Friend friend)
        {
            Debug.Log("LOBBY HATH CHANGED");
            setLobbyMemberList(SteamManager.currentLobby.Members.ToList());
            //setUI(uiMode);

        }
        private void OnCancelLobby()
        {
            serverWorld.Dispose();
            clientWorld.Dispose();
            SteamManager.currentLobby.SetData("melon", "000000000");
            SteamManager.currentLobby.Leave();
            
            setUI(uiModes.chooseMode);
        }

        async private void OnJoinLobby()
        {

            RoomEnter joinedLobbySuccess = await SteamManager.activeLobbies[_lobbiesList.selectedIndex].Join();

            if (joinedLobbySuccess == RoomEnter.Success)
            {
                SteamManager.currentLobby = SteamManager.activeLobbies[_lobbiesList.selectedIndex];
                setLobbyMemberList(SteamManager.currentLobby.Members.ToList());
                Debug.
                    Log($"ip is {SteamManager.currentLobby.GetData("mymelon")}");
                melon = SteamManager.currentLobby.GetData("mymelon");
                setUI(uiModes.inLobby);
                IsInLobby = true;
            }
            else
            {
                Debug.Log("Failed to enter lobby");
            }

        }

        async private void OnLeaveLobby()
        {
            //i bet if we checked on whether or not the person who left was the host...
            serverWorld.Dispose();
            await GetRatKingLobbies();
            OnFindLobby();
        }
        private void setLobbyMemberList(List<Friend> members)
        {

            //private ListView _lobbyMembers = uiDoc.Q<ListView>("_lobbyMembers");
            //public List<Friend> membersInLobby = new List<Friend>();
            _lobbyMembers.Clear();
            _lobbyMembers.itemsSource = members;
        }

        private async Task<bool> GetRatKingLobbies()
        {
            try
            {
                SteamManager.activeLobbies.Clear();
                Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithMaxResults(5).WithKeyValue("ratMakerId", ratKingPass).RequestAsync();
                if (lobbies != null)
                {
                    foreach (Lobby lobby in lobbies.ToList())
                    {
                        SteamManager.activeLobbies.Add(lobby);
                    }
                    return true;
                }
                Debug.Log("No rat king lobbies found.");
                return false;
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                Debug.Log("Error fetching rat king lobbies");
                return true;
            }
        }

        private void createLobbyList(List<Lobby> lobbies)
        {
            var displayLobbiesList = new List<String> { };
            _lobbiesList.Clear();
            foreach (var lobby in lobbies)
            {
                var lobbyTitle = lobby.GetData("lobbyName");
                displayLobbiesList.Add($"{lobbyTitle}: {lobby.MemberCount} players.");
                if (displayLobbiesList.Count > 5) { break; };
            }
            _lobbiesList.itemsSource = displayLobbiesList;
        }

        private void setUI(uiModes newUIMode)
        {
            //setUI clears all UI elements, chooses which to reveal and sets menu titles. All other logic of contents of elements is handled in the button callback functions.
            uiMode = newUIMode;
            clearUI();
            switch (uiMode)
            {
                case uiModes.inGame:
                    hideElement(_menuTitle);
                    break;
                case uiModes.loading:
                    setMenuTitle("One moment please...");
                    break;
                case uiModes.startOfGame:
                    setMenuTitle(defaultTitle);
                    showElement(_startButton);
                    showElement(_exitButton);
                    break;
                case uiModes.noSteamClient:
                    setMenuTitle(defaultTitle);
                    showElement(_errorMessage);
                    showElement(_exitButton);
                    break;
                case uiModes.chooseMode:
                    setMenuTitle(defaultTitle);
                    showElement(_setupHostButton);
                    showElement(_findLobbyButton);
                    showElement(_exitButton);
                    break;
                case uiModes.setupHost:
                    setMenuTitle("Set Up Host");
                    setSubheader("Please choose a public name for your game lobby. Note: hosting a game will reveal your IP address to people who may be querying the Steam matchmaking API.");
                    showElement(_subheader);
                    showElement(_lobbyTitleField);
                    showElement(_hostLobbyButton);
                    showElement(_cancelButton);
                    break;
                case uiModes.Host:
                    setMenuTitle("Your Lobby");
                    showElement(_cancelLobbyButton);
                    showElement(_lobbyMembers);
                    showElement(_startGameButton);
                    break;
                case uiModes.findLobby:
                    setMenuTitle("Choose a Lobby");
                    showElement(_lobbiesList);
                    showElement(_refreshLobbiesButton);
                    showElement(_joinLobbyButton);
                    showElement(_cancelButton);
                    break;
                case uiModes.noLobbies:
                    setMenuTitle("Awaiting Lobbies");
                    setSubheader("No lobbies are available. Please wait a few moments and click refresh!");
                    showElement(_subheader);
                    showElement(_refreshLobbiesButton);
                    showElement(_joinLobbyButton);
                    showElement(_cancelButton);
                    break;
                case uiModes.inLobby:
                    setMenuTitle($"In Lobby: {SteamManager.currentLobby.GetData("lobbyName")}");
                    showElement(_lobbyMembers);
                    showElement(_leaveLobbyButton);
                    break;
                case uiModes.pauseMenu:
                    setMenuTitle("PAUSE");
                    showElement(_exitButton);
                    break;
                default:
                    setUI(uiModes.startOfGame);
                    break;
            }
        }

        private void clearUI()
        {
            //got to be a way to just iterate over all elements... but brute forcing will do for now.
            showElement(_menuTitle);
            hideElement(_startButton);
            hideElement(_lobbyTitleField);
            hideElement(_findLobbyButton);
            hideElement(_setupHostButton);
            hideElement(_startGameButton);
            hideElement(_hostLobbyButton);
            hideElement(_joinLobbyButton);
            hideElement(_cancelLobbyButton);
            hideElement(_lobbiesList);
            hideElement(_lobbyMembers);
            hideElement(_leaveLobbyButton);
            hideElement(_cancelButton);
            hideElement(_exitButton);
            hideElement(_errorMessage);
            hideElement(_subheader);
            hideElement(_refreshLobbiesButton);
        }
        private void setMenuTitle(string text)
        {
            _menuTitle.text = text;
        }
        private void setErrorMessage(string text)
        {
            _errorMessage.text = text;
        }

        private void setSubheader(string text)
        {
            _subheader.text = text;
        }

        private void hideElement(VisualElement element)
        {
            element.style.display = DisplayStyle.None;
        }

        private void showElement(VisualElement element)
        {
            element.style.display = DisplayStyle.Flex;
        }

        private void OnStartGame()
        {
            //create a new entity type of sceneloader
            // var targetScene = new EntitySceneReference(GameScene);
            // var loadTest = SceneSystem.LoadSceneAsync(serverWorld.Unmanaged, targetScene);
            // //I need to pass loadTest into a system?
            // FixedString64Bytes shortName = GameScene.name;
            // 
            // serverWorld.EntityManager.AddComponent(loadTest, typeof(SceneLoaderData));
            // serverWorld.EntityManager.SetComponentData<SceneLoaderData>(loadTest, new SceneLoaderData
            // {
            //     LoadingEntity = loadTest,
            //     SceneName = shortName
            // });

            GameObject.FindGameObjectWithTag("MusicManager").GetComponent<MusicManager>().PlayGameMusic();
            Debug.LogWarning("running game startup");

            setUI(uiModes.inGame);
            SteamManager.currentLobby.SetJoinable(false);
        
            serverWorld.EntityManager.CreateEntity(typeof(StartTheGame));
     
            //SystemState systemState = new SystemState();
            ShouldStartGame = true;


        }
   

        private static void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    Debug.Log($"Disposing of local world: {world.Name}");
                    world.Dispose();
                    break;
                }
            }
        }

        public static void InitializeWorlds(EntitySceneReference gameScene)
        {
            if (Instance.WorldsAreInitialized)
            {
                Debug.LogError("Attempted to initialize server/client worlds twice! That's no good!!");
                return;
            }

            if (Instance.IsLobbyHost)
            {
                Instance.StartServer();
                SceneSystem.LoadSceneAsync(Instance.serverWorld.Unmanaged, gameScene);
            }
            
            Instance.StartClient();
            SceneSystem.LoadSceneAsync(Instance.clientWorld.Unmanaged, gameScene);
            
            Instance.WorldsAreInitialized = true;
        }
        
        private void StartServer()
        {
            Debug.Log("Starting server");
            serverWorld = ClientServerBootstrap.CreateServerWorld("Ratking Server World");
            var serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(Port);
            {
                using var networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
            }
        }

        private void StartClient()
        {
            clientWorld = ClientServerBootstrap.CreateClientWorld("Ratking Client World");
            var connectionEndpoint = NetworkEndpoint.Parse(melon, Port);
            {
                using var networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, connectionEndpoint); 
            }
            World.DefaultGameObjectInjectionWorld = clientWorld;
        }
    }
}
