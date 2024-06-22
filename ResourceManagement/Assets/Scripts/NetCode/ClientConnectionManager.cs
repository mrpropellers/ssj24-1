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
//using Simulation;
//using UnityEngine.UI;

namespace NetCode
{
    public class ClientConnectionManager : MonoBehaviour
    {
        private VisualElement uiDoc;
        private TextField _addressField => uiDoc.Q<TextField>("_addressField");
        private TextField _portField => uiDoc.Q<TextField>("_portField");
        private DropdownField _connectionModeDropdown => uiDoc.Q<DropdownField>("_connectionModeDropdown");
        private Button _connectionButton => uiDoc.Q<Button>("_connectionButton");
        private Button _hostLobbyButton => uiDoc.Q<Button>("_hostLobby");
        private Button _joinLobbyButton => uiDoc.Q<Button>("_joinLobby");
        private Button _cancelLobbyButton => uiDoc.Q<Button>("_cancelLobby");
        private RadioButtonGroup _lobbyRadioButtonGroup => uiDoc.Q<RadioButtonGroup>("_lobbyRadioButtonGroup");
        private ListView _lobbyMembers => uiDoc.Q<ListView>("_lobbyMembers");

        public List<Friend> membersInLobby = new List<Friend>();

        private ushort Port => ushort.Parse(_portField.text);
        private string Address => _addressField.text;

        [SerializeField] GameObject steamManagerObject;
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
            _connectionButton.clicked += () => {
                OnButtonConnect();
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
            _connectionModeDropdown.RegisterValueChangedCallback(
                (evt) => changeConnectionMode(evt.newValue));

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
                setLobbyMemberList(SteamManager.currentLobby.Members.ToList());
                hidePreHostComponents();
                revealHostComponents();
            }

            
        }
        private void OnCancelLobby()
        {
            SteamManager.currentLobby.Leave();
            hideHostComponents();
            revealPreHostComponents();
        }

        async private void OnJoinLobby()
        {

        }
        async private void changeConnectionMode(string newValue)
        {
            if(newValue.Equals("host"))
            {
                Debug.Log("Host Selected");
                hideJoinComponents();
                revealPreHostComponents();
            }
            
            if (newValue.Equals("join"))
            {
                Debug.Log("Join Selected");

                await SteamManager.RefreshMultiplayerLobbies();
                var currentSteamLobbies = SteamManager.activeLobbies;
                Debug.Log($"Games available count: {currentSteamLobbies.Count}");

                if(currentSteamLobbies.Count > 0)
                {
                    setLobbyList(currentSteamLobbies);

                } else
                {
                    Debug.Log("No current lobbies available");
                }

                hideStartGame();
                hideHostComponents();
                revealJoinComponents();
                
            }
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

        private void setLobbyMemberList(List<Friend> members)
        {

            //private ListView _lobbyMembers = uiDoc.Q<ListView>("_lobbyMembers");
            //public List<Friend> membersInLobby = new List<Friend>();
            _lobbyMembers.Clear();
            _lobbyMembers.itemsSource = members;
        }

        private void revealPreHostComponents()
        {
            _addressField.style.display = DisplayStyle.Flex;
            _portField.style.display = DisplayStyle.Flex;
        }
        private void hidePreHostComponents()
        {
            _addressField.style.display = DisplayStyle.None;
            _portField.style.display = DisplayStyle.None;
        }

        private void hideHostComponents()
        {
            hideStartGame();
            _cancelLobbyButton.style.display = DisplayStyle.None;
            _lobbyMembers.style.display = DisplayStyle.None;

        }
        private void revealHostComponents()
        {
            _cancelLobbyButton.style.display = DisplayStyle.Flex;
            _lobbyMembers.style.display = DisplayStyle.Flex;
            //INSERT LOBBY MEMBERS LIST
        }
        private void revealStartGame()
        {
            _connectionButton.style.display = DisplayStyle.Flex;
        }
        private void hideStartGame()
        {
            _connectionButton.style.display = DisplayStyle.None;
        }

        private void revealJoinComponents()
        {
            _lobbyRadioButtonGroup.style.display = DisplayStyle.Flex;
        }
        private void hideJoinComponents()
        {
            _lobbyRadioButtonGroup.style.display = DisplayStyle.None;
        }

        private void OnButtonConnect()
        {
            Debug.Log("port is: " + _portField.text);
            DestroyLocalSimulationWorld();
            SceneManager.LoadScene("DevinCharacterScene");

            switch (_connectionModeDropdown.index)
            {
                case 0:
                    StartServer();
                    StartClient();
                    break;
                case 1:
                    StartServer();
                    break;
                default:
                    Debug.LogError("Error: Unknown connection mode.");
                    break;
            }
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
            var serverWorld = ClientServerBootstrap.CreateServerWorld("Turbo Server World");
            var serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(Port);
            {
                using var networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
            }

            

        }

        private void StartClient()
        {
            var clientWorld = ClientServerBootstrap.CreateClientWorld("Turbo Client World");
            var connectionEndpoint = NetworkEndpoint.Parse(Address, Port);
            {
                using var networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, connectionEndpoint);
            }

            World.DefaultGameObjectInjectionWorld = clientWorld;
        }
    }
}       
