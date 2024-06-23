using System.Collections.Generic;
using Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace NetCode
{
    public struct ClientUid : IComponentData
    {
        public ulong Value;
    }
    
    public struct ClientJoinRequest : IRpcCommand
    {
        public ClientUid Id;
    }
    
    /// <summary>
    /// This allows sending RPCs between a stand alone build and the editor for testing purposes in the event when you finish this example
    /// you want to connect a server-client stand alone build to a client configured editor instance. 
    /// </summary>
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    public partial struct SetRpcSystemDynamicAssemblyListSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<RpcCollection>().ValueRW.DynamicAssemblyList = true;
            state.Enabled = false;
        }
    }

    // When client has a connection with network id, go in game and tell server to also go in game
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientGameSetupSystem : ISystem
    {
        static ulong Guids = 0;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSetup>();
            
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId>()
                .WithNone<NetworkStreamInGame>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess().WithNone<NetworkStreamInGame>())
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(entity);
                var req = commandBuffer.CreateEntity();
                // TODO: Get the ClientUID from Steam or something
                commandBuffer.AddComponent(req, new ClientJoinRequest() {
                    Id = new ClientUid() { Value = Guids++}
                });
                commandBuffer.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });
            }
            commandBuffer.Playback(state.EntityManager);
        }
    }

    // When server receives go in game request, go in game and delete request
    //[BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerGameSetupSystem : ISystem
    {
        private ComponentLookup<NetworkId> networkIdFromEntity;
        //Dictionary<ClientUid, NetworkId> UidToNetworkIdMap;

        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSetup>();
            
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ClientJoinRequest>()
                .WithAll<ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            networkIdFromEntity = state.GetComponentLookup<NetworkId>(true);
            //UidToNetworkIdMap = new Dictionary<ClientUid, NetworkId>();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI.GetSingletonRW<EndSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged);

            // Get our GameSetup singleton, which contains the prefabs we'll spawn
            GameSetup gameSetup = SystemAPI.GetSingleton<GameSetup>();
            
            // When a client wants to join, spawn and setup a character for them
            foreach (var (recieveRPC, joinRequest, entity) in SystemAPI.Query<ReceiveRpcCommandRequest, ClientJoinRequest>().WithEntityAccess())
            {                
                // TODO: Check the UidToNetworkIdMap to see if this player has already joined
                //  if so, just re-enable all their stuff
                // Spawn character, player, and camera ghost prefabs
                Entity characterEntity = ecb.Instantiate(gameSetup.CharacterSimulation);
                Entity playerEntity = ecb.Instantiate(gameSetup.Player);
                //Entity cameraEntity = ecb.Instantiate(gameSetup.CameraPrefab);
                    
                // Add spawned prefabs to the connection entity's linked entities, so they get destroyed along with it
                ecb.AppendToBuffer(recieveRPC.SourceConnection, new LinkedEntityGroup { Value = characterEntity });
                ecb.AppendToBuffer(recieveRPC.SourceConnection, new LinkedEntityGroup { Value = playerEntity });
                //ecb.AppendToBuffer(recieveRPC.SourceConnection, new LinkedEntityGroup { Value = cameraEntity });
                
                // Setup the owners of the ghost prefabs (which are all owner-predicted) 
                // The owner is the client connection that sent the join request
                var clientConnectionId = SystemAPI.GetComponent<NetworkId>(recieveRPC.SourceConnection);
                int clientConnectionIdValue = clientConnectionId.Value;
                //UidToNetworkIdMap[joinRequest.Id] = clientConnectionId;
                ecb.SetComponent(characterEntity, new GhostOwner { NetworkId = clientConnectionIdValue });
                ecb.SetComponent(playerEntity, new GhostOwner { NetworkId = clientConnectionIdValue });
                //ecb.SetComponent(cameraEntity, new GhostOwner { NetworkId = clientConnectionId });

                // Setup links between the prefabs
                ThirdPersonPlayer player = SystemAPI.GetComponent<ThirdPersonPlayer>(gameSetup.Player);
                player.ControlledCharacter = characterEntity;
                //player.ControlledCamera = cameraEntity;
                ecb.SetComponent(playerEntity, player);
                
                // Place character at a random point around world origin
                //ecb.SetComponent(characterEntity, LocalTransform.FromPosition(_random.NextFloat3(new float3(-5f,0f,-5f), new float3(5f,0f,5f))));
                
                // Allow this client to stream in game
                ecb.AddComponent<NetworkStreamInGame>(recieveRPC.SourceConnection);
                    
                // Destroy the RPC since we've processed it
                ecb.DestroyEntity(entity);
            }
        }

    }
}
