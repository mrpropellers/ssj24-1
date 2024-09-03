using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simulation;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;

namespace NetCode
{
    public class ClientServerWorldManager
    {
        Entity _clientConnector;
        MonoBehaviour _owner;
        List<Entity> _serverLoadEntities = new List<Entity>();
        List<Entity> _clientLoadEntities = new List<Entity>();
        float _timeLoadStarted;
        
        public World serverWorld { get; private set; }
        public World clientWorld { get; private set; }

        public ClientServerWorldManager(MonoBehaviour owner)
        {
            _owner = owner;
        }

        bool ServerIsInitialized
        {
            get
            {
                foreach (var serverLoad in _serverLoadEntities)
                {
                    if (!SceneSystem.IsSceneLoaded(serverWorld.Unmanaged, serverLoad))
                        return false;
                }

                return true;
            }
        }

        public bool WorldsAreInitialized
        {
            get
            {
                if (!HasAttemptedInitialization)
                    return false;

                if ((_serverLoadEntities.Any() && !ServerIsInitialized) 
                    || !_clientLoadEntities.Any())
                {
                    return false; 
                }
                
                foreach (var clientLoad in _clientLoadEntities)
                {
                    if (!SceneSystem.IsSceneLoaded(clientWorld.Unmanaged, clientLoad))
                        return false;
                }

                return true;
            }
        }

        public bool HasAttemptedInitialization => _serverLoadEntities.Any() || _clientLoadEntities.Any();
        public NetworkStreamConnection ClientNetworkStream => WorldsAreInitialized
            ? clientWorld.EntityManager.GetComponentData<NetworkStreamConnection>(_clientConnector)
            : default;
        
        public bool IsClientConnected => WorldsAreInitialized 
            && ClientNetworkStream.CurrentState == ConnectionState.State.Connected;
        
        public void StartGameOnServer()
        {
            var gameStateQuery = serverWorld.EntityManager.CreateEntityQuery(new ComponentType[] 
                { typeof(GameState) });
            gameStateQuery.TryGetSingletonEntity<GameState>(out var gameStateEntity);
            var gameState = serverWorld.EntityManager.GetComponentData<GameState>(gameStateEntity);
            gameState.IsGameplayUnderway = true;
            serverWorld.EntityManager.SetComponentData(gameStateEntity, gameState);
        }

        IEnumerator StartClientAfterServer(
            GameplaySceneReferences gameScene, string ip, ushort port, float timeOut = 5f)
        {
            var start = Time.time;
            if (TryStartServer(port) && TryStartClient(ip, port))
            {
                Debug.Log("Server and client started. Loading Server World");
                _serverLoadEntities.Add(SceneSystem.LoadSceneAsync(serverWorld.Unmanaged, gameScene.Level));
                _serverLoadEntities.Add(SceneSystem.LoadSceneAsync(serverWorld.Unmanaged, gameScene.GameSetup));
            }
            else
            {
                Debug.LogError("Failed to start Server or Client. Aborting world initialization.");
            }
            while (!ServerIsInitialized && Time.time - start < timeOut)
            {
                yield return null;
            }

            if (Time.time - start >= timeOut - float.Epsilon)
            {
                Debug.LogError("Failed to start the Server! Aborting");
                yield break;
            }
            
            Debug.Log("Server world loaded. Loading Client world.");
            _clientLoadEntities.Add(SceneSystem.LoadSceneAsync(clientWorld.Unmanaged, gameScene.Level));
            _clientLoadEntities.Add(SceneSystem.LoadSceneAsync(clientWorld.Unmanaged, gameScene.GameSetup));
        }
        
        public void InitializeWorlds(GameplaySceneReferences gameScene, string ip, ushort port, bool isServer)
        {
            if (WorldsAreInitialized)
            {
                Debug.LogError("Attempted to initialize server/client worlds twice! That's no good!!");
                return;
            }
            
            _serverLoadEntities.Clear();
            _clientLoadEntities.Clear();
            _timeLoadStarted = Time.time;
            
            if (isServer)
            {
                _owner.StartCoroutine(StartClientAfterServer(gameScene, ip, port));

                return;
            }
            
            if (TryStartClient(
                    //isServer ? "127.0.0.1" : 
                    ip, port))
            { 
                Debug.Log("Client started. Loading Client world.");
                _clientLoadEntities.Add(SceneSystem.LoadSceneAsync(clientWorld.Unmanaged, gameScene.Level));
                _clientLoadEntities.Add(SceneSystem.LoadSceneAsync(clientWorld.Unmanaged, gameScene.GameSetup));
            }
            else
            {
                Debug.LogError("Failed to start Client. Aborting world initialization.");
                return;
            }
            
        }
        
        private bool TryStartServer(ushort port)
        {
            Debug.Log("Starting server");
            serverWorld = ClientServerBootstrap.CreateServerWorld("Ratking Server World");
            var serverEndpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
            using var networkDriverQuery = serverWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadWrite<NetworkStreamDriver>());
            var succeeded = networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndpoint);
            return succeeded;
        }

        private bool TryStartClient(string ipAddress, ushort port)
        {
            Debug.Log($"Attempting to start client and connect to {ipAddress}:{port}");
            clientWorld = ClientServerBootstrap.CreateClientWorld("Ratking Client World");
            World.DefaultGameObjectInjectionWorld = clientWorld;
            var connectionEndpoint = NetworkEndpoint.Parse(ipAddress, port);
            using var networkDriverQuery = clientWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadWrite<NetworkStreamDriver>());
            {
                try
                {
                    _clientConnector = networkDriverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(
                        clientWorld.EntityManager, connectionEndpoint); 
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return false;
                }

                return true;
            }
        }
        
        // Not sure we're still using this?
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

        public void CleanUpGameWorlds(bool isServer)
        {
            clientWorld.Dispose();
            _clientLoadEntities.Clear();
            if (isServer)
            {
                serverWorld.Dispose();
                _serverLoadEntities.Clear();
            }
        }
    }
}
