using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine.UIElements;
using System;
using Steamworks.Data;
using Steamworks;
using System.Linq;
using Unity.VisualScripting;
using System.Threading.Tasks;
//using Simulation;
//using UnityEngine.UI;

namespace NetCode
{
    public class ClientConnectionManager : MonoBehaviour
    {
        private enum uiModes {chooseMode, setupHost, Host, findLobby, inLobby, noLobbies }
        private uiModes uiMode = uiModes.chooseMode;
        
        private VisualElement uiDoc;
        private TextField _addressField => uiDoc.Q<TextField>("_addressField");
        private TextField _portField => uiDoc.Q<TextField>("_portField");
        private DropdownField _connectionModeDropdown => uiDoc.Q<DropdownField>("_connectionModeDropdown");
        private Button _setupHostButton => uiDoc.Q<Button>("_setupHost");
        private Button _startGameButton => uiDoc.Q<Button>("_startGame");
        private Button _findLobbyButton => uiDoc.Q<Button>("_findLobby");
        private Button _hostLobbyButton => uiDoc.Q<Button>("_hostLobby");
        private Button _joinLobbyButton => uiDoc.Q<Button>("_joinLobby");
        private Button _cancelLobbyButton => uiDoc.Q<Button>("_cancelLobby");
        private Button _leaveLobbyButton => uiDoc.Q<Button>("_leaveLobby");
        private Button _cancelButton => uiDoc.Q<Button>("_cancel");

        private Button _findRatKingLobbyButton => uiDoc.Q<Button>("_findRatKingLobby");
        private Label _noLobbiesLabel => uiDoc.Q<Label>("_noLobbies");

        private Label _menuTitle => uiDoc.Q<Label>("menu__title");
        private RadioButtonGroup _lobbyRadioButtonGroup => uiDoc.Q<RadioButtonGroup>("_lobbyRadioButtonGroup");

        private ListView _lobbiesList => uiDoc.Q<ListView>("_lobbyList");
        private ListView _lobbyMembers => uiDoc.Q<ListView>("_lobbyMembers");

        public List<Friend> membersInLobby = new List<Friend>();

        private Lobby targetLobby;

        private ushort Port => ushort.Parse(_portField.text);
        private string Address => _addressField.text;
        [SerializeField] private string defaultTitle = "Welcome to Ratking";
        [SerializeField] GameObject steamManagerObject;
        [SerializeField] private string defaultScene = "DevinCharacterScene";
        [SerializeField] private string ratKingPass = "abc123throwthemrats";
        private SteamManager SteamManager { get; set; }

        private void Awake()
        {
            uiDoc = GetComponent<UIDocument>().rootVisualElement;
        }

        private void Start()
        {
            SteamManager = steamManagerObject.GetComponent<SteamManager>();
        }

        private void OnEnable()
        {
            _findLobbyButton.clicked += () => {
                OnFindLobby();
            };
            _findRatKingLobbyButton.clicked += () =>
            {
                OnFindRatKingLobby();
            };
            _setupHostButton.clicked += () => {
                Debug.Log("Setup host clicked");
                OnSetupHost();
            };
            _startGameButton.clicked += () => {
                OnStartGame();
            };
            _hostLobbyButton.clicked += () => {
                OnHostLobby();
            };
            _joinLobbyButton.clicked += () => {
                OnJoinLobby();
            };
            _cancelLobbyButton.clicked += () =>
            {
                OnCancelLobby();
            };
            _leaveLobbyButton.clicked += () =>
            {
                OnLeaveLobby();
            };
            _cancelButton.clicked += () =>
            {
                OnCancelButton();
            };
            //_lobbiesList.RegisterCallback<ChangeEvent<bool>>((evt) => { OnLobbyChosen(evt); });
            //callbacks for when lobbies close or open?

        }

        private void OnDisable()
        {
            //_connectionModeDropdown.onValueChanged.RemoveAllListeners();
            //_connectionButton.onClick.RemoveAllListeners();
        }

        private void OnUpdate()
        {
            
        }
        async private void OnFindLobby()
        {
            Debug.Log("Find Lobby Selected");
            await SteamManager.RefreshMultiplayerLobbies();
            var currentSteamLobbies = SteamManager.activeLobbies;
            Debug.Log($"Games available count: {currentSteamLobbies.Count}");

            if (currentSteamLobbies.Count > 0)
            {
                setFindLobbyList(currentSteamLobbies);
                setUI(uiModes.findLobby);

            }
            else
            {
                Debug.Log("No current lobbies available");
                setUI(uiModes.noLobbies);
            } 
        }

        async private void OnFindRatKingLobby()
        {
            Debug.Log("Find RK Lobby Selected");
            await GetRatKingLobbies();
            var currentSteamLobbies = SteamManager.activeLobbies;
            Debug.Log($"Rat King available count: {currentSteamLobbies.Count}");

            if (currentSteamLobbies.Count > 0)
            {
                setFindLobbyList(currentSteamLobbies);
                setUI(uiModes.findLobby);

            }
            else
            {
                Debug.Log("No current RK lobbies available");
                setUI(uiModes.noLobbies);
            }
        }

        async private void OnSetupHost()
        {
            Debug.Log("Host Selected");
            setUI(uiModes.setupHost);
        }

        private void OnLobbyChosen(ChangeEvent<bool> clickEvent)
        {
            Debug.Log("Change Event called");
            Debug.Log(clickEvent);
            Debug.Log(clickEvent.newValue);
            //Debug.Log(chosenLobby.Id);
            //targetLobby = chosenLobby;
        }

        private void OnCancelButton()
        {
            setUI(uiModes.chooseMode);
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
            } catch (Exception e)
            {
                Debug.Log(e.ToString());
                Debug.Log("Error fetching rat king lobbies");
                return true;
            }
        }

        async private void OnHostLobby()
        {
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
            if (createLobby)
            {
                Debug.Log($"Lobby created: {SteamManager.currentLobby.Id}");
                Debug.Log(SteamManager.currentLobby.ToString());
                SteamManager.currentLobby.SetData("ratMakerId", ratKingPass);
                setLobbyMemberList(SteamManager.currentLobby.Members.ToList());
                setUI(uiModes.Host);

            }   
        }
        private void OnCancelLobby()
        {
            SteamManager.currentLobby.Leave();
            setUI(uiModes.chooseMode);
        }

        async private void OnJoinLobby()
        {
            
            RoomEnter joinedLobbySuccess = await SteamManager.activeLobbies[_lobbiesList.selectedIndex].Join();
           
            if (joinedLobbySuccess == RoomEnter.Success) {
                SteamManager.currentLobby = SteamManager.activeLobbies[_lobbiesList.selectedIndex];
                Debug.Log($"Lobby entered! Current Lobby {SteamManager.currentLobby.Id}");
                setLobbyMemberList(SteamManager.currentLobby.Members.ToList());
                setUI(uiModes.inLobby);
            } else
            {
                Debug.Log("Failed to enter lobby");
            }

        }

        private void OnLeaveLobby()
        {
            SteamManager.currentLobby.Leave();
            setUI(uiModes.findLobby);
        }
        private void setLobbyMemberList(List<Friend> members)
        {

            //private ListView _lobbyMembers = uiDoc.Q<ListView>("_lobbyMembers");
            //public List<Friend> membersInLobby = new List<Friend>();
            _lobbyMembers.Clear();
            _lobbyMembers.itemsSource = members;
        }

        private void setFindLobbyList(List<Lobby> lobbies)
        {
            var displayLobbiesList = new List<String> { };
            _lobbiesList.Clear();
            foreach (var lobby in lobbies)
            {
                if (lobby.Owner.Name.Length > 0)
                {
                    displayLobbiesList.Add($"{lobby.Owner.Name}'s lobby of {lobby.MemberCount} players.");
                }
                else
                {
                    displayLobbiesList.Add($"Unknown's lobby of {lobby.MemberCount} players.");
                }

                if (displayLobbiesList.Count > 5) { break; };
                //Debug.Log($"{lobby.Owner.Name}'s lobby of {lobby.MemberCount} players.");

            }
            _lobbiesList.itemsSource = displayLobbiesList;
        }

        private void setLobbyList(List<Lobby> lobbies)
        {
            var displayLobbiesList = new List<String> {};
            _lobbyRadioButtonGroup.Clear();

            foreach (var lobby in lobbies)
            {
                if (lobby.Owner.Name.Length > 0)
                {
                    displayLobbiesList.Add($"{lobby.Owner.Name}'s lobby of {lobby.MemberCount} players.");
                } else
                {
                    displayLobbiesList.Add($"Unknown's lobby of {lobby.MemberCount} players.");
                }

                if(displayLobbiesList.Count > 5) { break; };
                //Debug.Log($"{lobby.Owner.Name}'s lobby of {lobby.MemberCount} players.");

            }
            _lobbyRadioButtonGroup.choices = displayLobbiesList;
        }

        private void setUI(uiModes uiMode)
        {
            clearUI();
            switch(uiMode)
            {
                case uiModes.chooseMode:
                    Debug.Log("Choose Mode View");
                    setMenuTitle(defaultTitle);
                    showElement(_setupHostButton);
                    showElement(_findLobbyButton);
                    showElement(_findRatKingLobbyButton);
                    break;
                case uiModes.setupHost:
                    Debug.Log("Host Set Up View");
                    setMenuTitle("Set host settings");
                    showElement(_addressField);
                    showElement(_portField);
                    showElement(_hostLobbyButton);
                    showElement(_cancelButton);
                    break;
                case uiModes.Host:
                    Debug.Log("Host View");
                    setMenuTitle("Your Lobby");
                    showElement(_cancelLobbyButton);
                    showElement(_lobbyMembers);
                    showElement(_startGameButton);
                    break;
                case uiModes.findLobby:
                    setMenuTitle("Choose a Lobby");
                    showElement(_lobbiesList);
                    showElement(_joinLobbyButton);
                    showElement(_cancelButton);
                    break;
                case uiModes.noLobbies:
                    setMenuTitle("Awaiting Lobbies");
                    showElement(_noLobbiesLabel);
                    showElement(_joinLobbyButton);
                    showElement(_cancelButton);
                    break;
                case uiModes.inLobby: 
                    setMenuTitle("In Lobby");
                    showElement(_lobbyMembers);
                    showElement(_leaveLobbyButton);
                    break;
                default:
                    setUI(uiModes.chooseMode);
                    break;
            }
        }

        private void clearUI()
        {
            //got to be a way to just iterate over all elements... but brute forcing will do for now.
            hideElement(_addressField);
            hideElement(_portField);
            hideElement(_findLobbyButton);
            hideElement(_setupHostButton);
            hideElement(_startGameButton);
            hideElement(_hostLobbyButton);
            hideElement(_joinLobbyButton);
            hideElement(_cancelLobbyButton);
            hideElement(_lobbiesList);
            hideElement(_lobbyMembers);
            hideElement(_leaveLobbyButton);
            hideElement(_noLobbiesLabel);
            hideElement(_cancelButton);
            hideElement(_findRatKingLobbyButton);
        }
        private void setMenuTitle(string headerText)
        {
            _menuTitle.text = headerText;
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
            DestroyLocalSimulationWorld();
            SceneManager.LoadScene(defaultScene);
            StartServer();
            StartClient();
        }

        private static void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }

        private void StartServer()
        {
            Debug.Log("Starting server");
            var serverWorld = ClientServerBootstrap.CreateServerWorld("Ratking Server World");
            var serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(Port);
            {
                using var networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
            }
        }

        private void StartClient()
        {
            var clientWorld = ClientServerBootstrap.CreateClientWorld("Ratking Client World");
            var connectionEndpoint = NetworkEndpoint.Parse(Address, Port);
            {
                using var networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, connectionEndpoint);
            }

            World.DefaultGameObjectInjectionWorld = clientWorld;
        }
    }
}       
