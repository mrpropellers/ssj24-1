using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;

namespace NetCode
{
    public class EntityWorlds
    {
        // This is in kind of a weird location right now. Other things likely want to access it directly but it's
        // hiding inside of a class that shouldn't really encapsulate it. Too late to change it, so we'll just redirect
        // for now...
        static EntityWorlds Instance => GameplaySceneLoader.EntityWorldsInstance;
        
        Entity _clientConnector;
        MonoBehaviour _owner;
        List<Entity> _serverLoadEntities = new List<Entity>();
        List<Entity> _clientLoadEntities = new List<Entity>();
        float _timeLoadStarted;
        
        public World serverWorld { get; private set; }
        public World clientWorld { get; private set; }

        public EntityWorlds(MonoBehaviour owner)
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

        public static bool AreInitialized => Instance.worldsAreInitialized;
        bool worldsAreInitialized
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
                    if (!SceneSystem.IsSceneLoaded(clientWorld.Unmanaged, clientLoad)
                        && !clientWorld.Unmanaged.EntityManager.HasComponent<PrefabRoot>(clientLoad))
                        return false;
                }

                return true;
            }
        }

        public static bool HasAttemptedInitialization => 
            Instance._serverLoadEntities.Any() 
            || Instance._clientLoadEntities.Any();
        NetworkStreamConnection ClientNetworkStream => AreInitialized 
            ? clientWorld.EntityManager.GetComponentData<NetworkStreamConnection>(_clientConnector)
            : default;

        bool ClientIsCreated => AreInitialized && clientWorld.EntityManager.Exists(_clientConnector);
        
        public bool ClientIsConnected => ClientIsCreated
            && ClientNetworkStream.CurrentState == ConnectionState.State.Connected;

        public ConnectionState.State ClientConnectionState => ClientIsCreated
            ? ClientNetworkStream.CurrentState
            : ConnectionState.State.Unknown;

        public static bool GameplayIsUnderway => 
            !ReferenceEquals(null, Instance) 
            && Instance.ClientIsConnected
            && Instance.TryGetGameState(out _, out var gameState)
            && gameState.IsGameplayUnderway;

        public static int NumConnectedPlayers => 
            Instance?.CountConnectedPlayers() ?? 0;
        
        EntityQuery? m_GameStateQuery;

        bool TryGetGameState(out Entity gameStateEntity, out GameState gameState)
        {
            gameStateEntity = default;
            gameState = default;
            if (!AreInitialized)
            {
                Debug.LogError($"Can't fetch {nameof(GameState)} before you've initialized the worlds!");
                return false;
            }
            
            // Try to initialize the query with the correct world
            m_GameStateQuery ??=
                serverWorld?.EntityManager.CreateEntityQuery(typeof(GameState))
                ?? clientWorld?.EntityManager.CreateEntityQuery(typeof(GameState));
            
            return m_GameStateQuery != null 
                && m_GameStateQuery.Value.TryGetSingletonEntity<GameState>(out gameStateEntity)
                && m_GameStateQuery.Value.TryGetSingleton(out gameState);
        }
        
        public static void StartGameOnServer()
        {
            if (!Instance.TryGetGameState(out var entity, out var gameState))
            {
                Debug.LogError("Failed to get {nameof(GameState)}. Can't start server!");
                return;
            }
            gameState.IsGameplayUnderway = true;
            Instance.serverWorld.EntityManager.SetComponentData(entity, gameState);
        }

        IEnumerator StartServerThenClient(
            GameplaySceneReferences gameScene, string ip, ushort port, float timeOut = 5f)
        {
            var start = Time.time;
            
            // TODO? There may be a race condition here. We should initialize worlds first, THEN do the connection
            //  from client to server, although right now this seems to only be a problem when client is connecting
            //  to a remote server (i.e. not here)
            if (TryStartServer(port))
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
            yield return InitializeClientWorlds(gameScene, start, timeOut);
            if (!worldsAreInitialized || !TryConnectClient(ip, port))
            {
                Debug.LogError("Failed to start the Client after initializing Server!");
            }
        }

        IEnumerator InitializeClientWorlds(GameplaySceneReferences scenes,
            float start, float timeOut)
        {
            clientWorld = ClientServerBootstrap.CreateClientWorld("Ratking Client World");
            World.DefaultGameObjectInjectionWorld = clientWorld;
            
            _clientLoadEntities.Add(SceneSystem.LoadPrefabAsync(clientWorld.Unmanaged, scenes.GameState));
            _clientLoadEntities.Add(SceneSystem.LoadSceneAsync(clientWorld.Unmanaged, scenes.Level));
            _clientLoadEntities.Add(SceneSystem.LoadSceneAsync(clientWorld.Unmanaged, scenes.GameSetup));

            while (!worldsAreInitialized && Time.time - start < timeOut)
            {
                yield return null;
            }
        }
        
        IEnumerator StartClientAndConnect(
            GameplaySceneReferences gameScene, string ip, ushort port, float timeOut = 5f)
        {
            var start = Time.time;
            yield return InitializeClientWorlds(gameScene, start, timeOut);
            if (!worldsAreInitialized)
            {
                Debug.LogError("Failed to initialize the client world before timing out! Aborting connect attempt.");
                yield break;
            }
            
            var connectionEndpoint = NetworkEndpoint.Parse(ip, port);
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
                }
            }
        }
        
        public void InitializeWorlds(GameplaySceneReferences gameScene, string ip, ushort port, bool isServer)
        {
            if (AreInitialized)
            {
                Debug.LogError("Attempted to initialize server/client worlds twice! That's no good!!");
                return;
            }
            
            _serverLoadEntities.Clear();
            _clientLoadEntities.Clear();
            _timeLoadStarted = Time.time;
            
            if (isServer)
            {
                _owner.StartCoroutine(StartServerThenClient(gameScene, "127.0.0.1", port));
            }
            else
            {
                _owner.StartCoroutine(StartClientAndConnect(gameScene, ip, port));
            }
        }

        int CountConnectedPlayers()
        {
            if (!ClientIsConnected)
                return 0;

            using var playerQuery = clientWorld.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ThirdPersonPlayer>(), ComponentType.ReadOnly<GhostOwner>());
            var players = playerQuery.ToEntityArray(Allocator.Temp);
            var numPlayers = players.Length;
            players.Dispose();
            return numPlayers;
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

        private bool TryConnectClient(string ipAddress, ushort port)
        {
            Debug.Log($"Attempting to start client and connect to {ipAddress}:{port}");
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
