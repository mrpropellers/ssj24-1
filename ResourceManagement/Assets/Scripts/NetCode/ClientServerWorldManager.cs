using System;
using System.Collections.Generic;
using Simulation;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;

namespace NetCode
{
    public class ClientServerWorldManager
    {
        Entity _clientConnector;
        List<Entity> _serverLoadEntities = new List<Entity>();
        List<Entity> _clientLoadEntities = new List<Entity>();
        
        public World serverWorld { get; private set; }
        public World clientWorld { get; private set; }
        
        public bool WorldsAreInitialized { get; private set; }
        public bool HasAttemptedInitialization { get; private set; }
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
        
        public void InitializeWorlds(GameplaySceneReferences gameScene, string ip, ushort port, bool isServer)
        {
            if (WorldsAreInitialized)
            {
                Debug.LogError("Attempted to initialize server/client worlds twice! That's no good!!");
                return;
            }
            
            HasAttemptedInitialization = true;
            if (isServer)
            {
                if (TryStartServer(port))
                {
                    Debug.Log("Server started. Loading Server World");
                    var thing = SceneSystem.LoadSceneAsync(serverWorld.Unmanaged, gameScene.GameSetup);
                    SceneSystem.LoadSceneAsync(serverWorld.Unmanaged, gameScene.Level);
                }
                else
                {
                    Debug.LogError("Failed to start Server. Aborting world initialization.");
                    return;
                }
            }
            
            if (TryStartClient(ip, port))
            { 
                Debug.Log("Client started. Loading Client world.");
                SceneSystem.LoadSceneAsync(clientWorld.Unmanaged, gameScene.GameSetup);
                SceneSystem.LoadSceneAsync(clientWorld.Unmanaged, gameScene.Level);
            }
            else
            {
                Debug.LogError("Failed to start Client. Aborting world initialization.");
                return;
            }
            
            WorldsAreInitialized = true;
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
            // TODO | P1 - NetCode | Always use 127.0.0.1 if we're on the same machine as the Server
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
            if (isServer)
            {
                serverWorld.Dispose();
            }

            WorldsAreInitialized = false;
        }
    }
}
