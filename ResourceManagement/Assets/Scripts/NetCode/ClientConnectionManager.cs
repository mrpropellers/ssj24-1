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
//using Simulation;
//using UnityEngine.UI;

namespace NetCode
{
    public class ClientConnectionManager : MonoBehaviour
    {
        //[SerializeField] private TMP_InputField _addressField;
        //[SerializeField] private TMP_InputField _portField;
        //[SerializeField] private TMP_Dropdown _connectionModeDropdown;
        //[SerializeField] private Button _connectionButton;
        private VisualElement uiDoc;
        private TextField _addressField => uiDoc.Q<TextField>("_addressField");
        private TextField _portField => uiDoc.Q<TextField>("_portField");
        private DropdownField _connectionModeDropdown => uiDoc.Q<DropdownField>("_connectionModeDropdown");
        private Button _connectionButton;

        private ushort Port => ushort.Parse(_portField.text);
        private string Address => _addressField.text;

        

        private void Awake()
        {
            uiDoc = GetComponent<UIDocument>().rootVisualElement;
        }

        private void OnEnable()
        {
            _connectionButton = uiDoc.Q<Button>("_connectionButton");
            _connectionButton.clicked += () => {
                OnButtonConnect();
              };
            //_connectionButton.onClick.AddListener(OnButtonConnect);

                //_connectionModeDropdown.onValueChanged.AddListener(OnConnectionModeChanged);
                //_connectionButton.onClick.AddListener(OnButtonConnect);
            //OnConnectionModeChanged(_connectionModeDropdown.value);
        }

        private void OnDisable()
        {
            //_connectionModeDropdown.onValueChanged.RemoveAllListeners();
            //_connectionButton.onClick.RemoveAllListeners();
        }

        /*private void OnConnectionModeChanged(string connectionMode)
        {
            string buttonLabel;
            _connectionButton.enabled = true;
            switch (connectionMode) {
                case 0:
                    buttonLabel = "Start Host";
                    break;
                case 1:
                    buttonLabel = "Start Server";
                    break;
                default:
                    Debug.LogError("Error: Unknown connection mode", gameObject);
                    break;
            }
        }*/

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
