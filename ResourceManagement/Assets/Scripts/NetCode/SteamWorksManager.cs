using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetCode
{
    public class SteamManager : MonoBehaviour
    {
        public static SteamManager Instance;
        private static uint gameAppId = 480;
        [SerializeField] private int maxGamePlayers = 10;
        //[SerializeField] private string defaultScene = "DevinCharacterScene";

        public string PlayerName { get; set; }
        public SteamId PlayerSteamId { get; set; }
        private string playerSteamIdString;
        public string PlayerSteamIdString { get => playerSteamIdString; }

        private bool connectedToSteam = false;

        private Friend lobbyPartner;
        public Friend LobbyPartner { get => lobbyPartner; set => lobbyPartner = value; }
        public SteamId OpponentSteamId { get; set; }
        public bool LobbyPartnerDisconnected { get; set; }

        public List<Lobby> activeLobbies;
        public Lobby currentLobby;
        private Lobby hostedMultiplayerLobby;

        private bool applicationHasQuit = false;
        private bool daRealOne = false;
        // Start is called before the first frame update
        public void Awake()
        {
            if (Instance == null)
            {
                daRealOne = true;
                DontDestroyOnLoad(gameObject);
                Instance = this;
                PlayerName = "";
                try
                {
                    // Create client
                    SteamClient.Init(gameAppId, true);

                    if (!SteamClient.IsValid)
                    {
                        Debug.Log("Steam client not valid");
                        throw new Exception();
                    }

                    PlayerName = SteamClient.Name;
                    PlayerSteamId = SteamClient.SteamId;
                    playerSteamIdString = PlayerSteamId.ToString();
                    activeLobbies = new List<Lobby>();
                    connectedToSteam = true;
                    Debug.Log("Steam initialized: " + PlayerName);
                }
                catch (Exception e)
                {
                    connectedToSteam = false;
                    playerSteamIdString = "NoSteamId";
                    Debug.Log("Error connecting to Steam");
                    Debug.Log(e);
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public bool TryToReconnectToSteam()
        {
            Debug.Log("Attempting to reconnect to Steam");
            try
            {
                // Create client
                SteamClient.Init(gameAppId, true);

                if (!SteamClient.IsValid)
                {
                    Debug.Log("Steam client not valid");
                    throw new Exception();
                }

                PlayerName = SteamClient.Name;
                PlayerSteamId = SteamClient.SteamId;
                activeLobbies = new List<Lobby>();
                Debug.Log("Steam initialized: " + PlayerName);
                connectedToSteam = true;
                return true;
            }
            catch (Exception e)
            {
                connectedToSteam = false;
                Debug.Log("Error connecting to Steam");
                Debug.Log(e);
                return false;
            }
        }

        public bool ConnectedToSteam()
        {
            return connectedToSteam;
        }

        void Start()
        {
            //SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreatedCallback;
            //SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
            //SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
            //SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
            //SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnectedCallback;
            //SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeaveCallback;
            //SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequestedCallback;
            //SceneManager.sceneLoaded += OnSceneLoaded;

           // UpdateRichPresenceStatus(SceneManager.GetActiveScene().name);
        }

        // Update is called once per frame
        void OnUpdate()
        {
            //SteamClient.RunCallbacks();
        }

        void OnDisable()
        {
            if (daRealOne)
            {
                gameCleanup();
            }
        }

        void OnDestroy()
        {
            if (daRealOne)
            {
                gameCleanup();
            }
        }

        void OnApplicationQuit()
        {
            if (daRealOne)
            {
                gameCleanup();
            }
        }

        // Place where you can update saves, etc. on sudden game quit as well
        private void gameCleanup()
        {
            if (!applicationHasQuit)
            {
                applicationHasQuit = true;
                leaveLobby();
                SteamClient.Shutdown();
            }
        }
        /*
        void OnLobbyMemberDisconnectedCallback(Lobby lobby, Friend friend)
        {
            OtherLobbyMemberLeft(friend);
        }

        void OnLobbyMemberLeaveCallback(Lobby lobby, Friend friend)
        {
            OtherLobbyMemberLeft(friend);
        }

        private void OtherLobbyMemberLeft(Friend friend)
        {
            if (friend.Id != PlayerSteamId)
            {
                Debug.Log("Opponent has left the lobby");
                LobbyPartnerDisconnected = true;
                try
                {
                    SteamNetworking.CloseP2PSessionWithUser(friend.Id);
                    // Handle game / UI changes that need to happen when other player leaves
                }
                catch
                {
                    Debug.Log("Unable to update disconnected player nameplate / process disconnect cleanly");
                }

            }
        }
        void OnLobbyGameCreatedCallback(Lobby lobby, uint ip, ushort port, SteamId steamId)
        {
            AcceptP2P(OpponentSteamId);
            SceneManager.LoadScene("SceneToLoad");
        }

        private void AcceptP2P(SteamId opponentId)
        {
            try
            {
                // For two players to send P2P packets to each other, they each must call this on the other player
                SteamNetworking.AcceptP2PSessionWithUser(opponentId);
            }
            catch
            {
                Debug.Log("Unable to accept P2P Session with user");
            }
        }
        void OnLobbyEnteredCallback(Lobby lobby)
        {
            // You joined this lobby
            if (lobby.MemberCount != 1) // I do this because this callback triggers on host, I only wanted to use for players joining after host
            {
                // You will need to have gotten OpponentSteamId from various methods before (lobby data, joined invite, etc)
                AcceptP2P(OpponentSteamId);

                // Examples of things to do
                //SceneManager.LoadScene(defaultScene);
            }
        }

        // Accepted Steam Game Invite
        async void OnGameLobbyJoinRequestedCallback(Lobby joinedLobby, SteamId id)
        {
            // Attempt to join lobby
            RoomEnter joinedLobbySuccess = await joinedLobby.Join();
            if (joinedLobbySuccess != RoomEnter.Success)
            {
                Debug.Log("failed to join lobby");
            }
            else
            {
                // This was hacky, I didn't have clean way of getting lobby host steam id when joining lobby from game invite from friend 
                foreach (Friend friend in SteamFriends.GetFriends())
                {
                    if (friend.Id == id)
                    {
                        lobbyPartner = friend;
                        break;
                    }
                }
                currentLobby = joinedLobby;
                OpponentSteamId = id;
                LobbyPartnerDisconnected = false;
                AcceptP2P(OpponentSteamId);
                SceneManager.LoadScene("Scene to load");
            }
        }

        void OnLobbyCreatedCallback(Result result, Lobby lobby)
        {
            // Lobby was created
            LobbyPartnerDisconnected = false;
            if (result != Result.OK)
            {
                Debug.Log("lobby creation result not ok");
                Debug.Log(result.ToString());
            }
        }

        void OnLobbyMemberJoinedCallback(Lobby lobby, Friend friend)
        {
            // The lobby member joined
            Debug.Log("someone else joined lobby");
            if (friend.Id != PlayerSteamId)
            {
                LobbyPartner = friend;
                OpponentSteamId = friend.Id;
                AcceptP2P(OpponentSteamId);
                LobbyPartnerDisconnected = false;
            }
        }
        public async Task<bool> RefreshMultiplayerLobbies()
        {
            try
            {
                {
                    activeLobbies.Clear();
                    Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithMaxResults(20).RequestAsync();
                    if (lobbies != null)
                    {
                        foreach (Lobby lobby in lobbies.ToList())
                        {
                            activeLobbies.Add(lobby);
                        }
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                Debug.Log("Error fetching multiplayer lobbies");
                return true;
            }
        }
         */
        public void leaveLobby()
        {
            try
            {
                currentLobby.Leave();
            }
            catch
            {
                Debug.Log("Error leaving current lobby");
            }
            try
            {
                SteamNetworking.CloseP2PSessionWithUser(OpponentSteamId);
            }
            catch
            {
                Debug.Log("Error closing P2P session with opponent");
            }
        }
       
        public async Task<bool> CreateLobby() //CreateLobby(int lobbyParameters) Preston remove this bc I'm not sure what other parameters we have besides max players? Perhaps the IP address and port will need to be passed here.
        {
            try
            {
                var createLobbyOutput = await SteamMatchmaking.CreateLobbyAsync(maxGamePlayers);
                if (!createLobbyOutput.HasValue)
                {
                    Debug.Log("Lobby created but not correctly instantiated");
                    throw new Exception();
                }

                LobbyPartnerDisconnected = false;
                hostedMultiplayerLobby = createLobbyOutput.Value;
                hostedMultiplayerLobby.SetPublic();
                hostedMultiplayerLobby.SetJoinable(true);
                //TO DO? staticDataString needs to come from somewhere to set lobby Parameters
                //hostedMultiplayerLobby.SetData(staticDataString, lobbyParameters);

                currentLobby = hostedMultiplayerLobby;

                return true;
            }
            catch (Exception exception)
            {
                Debug.Log("Failed to create multiplayer lobby");
                Debug.Log(exception.ToString());
                return false;
            }
        }

        // Allows you to open friends list where game invites will have lobby id
        public void OpenFriendOverlayForGameInvite()
        {
            SteamFriends.OpenGameInviteOverlay(currentLobby.Id);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            UpdateRichPresenceStatus(scene.name);
        }

        public void UpdateRichPresenceStatus(string SceneName)
        {
            if (connectedToSteam)
            {
                string richPresenceKey = "steam_display";

                if (SceneName.Equals("SillyScene"))
                {
                    SteamFriends.SetRichPresence(richPresenceKey, "#SillyScene");
                }
                else if (SceneName.Contains("SillyScene2"))
                {
                    SteamFriends.SetRichPresence(richPresenceKey, "#SillyScene2");
                }
            }
        }

    }

}