using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

namespace Simulation
{

    // [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    // public struct PendingPickUp : IBufferElementData
    // {
    //     
    // }
    
    [BurstCompile]
    public struct PickUpJob : ITriggerEventsJob
    {
        public EntityCommandBuffer ECB;
        
        [ReadOnly]
        public ComponentLookup<CharacterFollowerThrowing> CharacterLookup;
        [ReadOnly]
        public ComponentLookup<Ownership> PickUpLookup;
        [ReadOnly]
        public ComponentLookup<GhostOwner> GhostOwnerLookup;
        [ReadOnly]
        public ComponentLookup<Follower> FollowerLookup;
        [ReadOnly]
        public ComponentLookup<NetworkId> OwnerIdLookup;
        
        
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity characterEntity;
            CharacterFollowerThrowing character;
            Entity otherEntity;
            if (CharacterLookup.TryGetComponent(triggerEvent.EntityA, out character))
            {
                characterEntity = triggerEvent.EntityA;
                otherEntity = triggerEvent.EntityB;
            }
            else if (CharacterLookup.TryGetComponent(triggerEvent.EntityA, out character))
            {
                characterEntity = triggerEvent.EntityB;
                otherEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            if (!PickUpLookup.TryGetComponent(otherEntity, out var pickUp) || !pickUp.CanBeClaimed)
                return;

            // if (!GhostOwnerLookup.TryGetComponent(otherEntity, out var ghostOwner))
            //     return;

            // if (!OwnerIdLookup.TryGetComponent(characterEntity, out var networkId))
            //     return;
            
            Debug.Log("Picking something up");
            
            pickUp.Owner = characterEntity;
            pickUp.HasSetOwner = true;
            ECB.SetComponent(otherEntity, pickUp);
            //ghostOwner.NetworkId = networkId.Value;
            //ECB.SetComponent(otherEntity, ghostOwner);
            if (FollowerLookup.TryGetComponent(otherEntity, out var follower))
            {
                character.NumThrowableFollowers++;
                follower.OwnerQueueRank = character.NumThrowableFollowers + character.NumThrownFollowers;
                ECB.SetComponent(otherEntity, follower);
                ECB.SetComponent(characterEntity, character);
                ECB.AppendToBuffer(characterEntity, new ThrowableFollowerElement()
                {
                    Follower = otherEntity
                });
            }
        }
    }
    
    // TODO? Use RPCs from client to set PickUp state
    //  Right now the server is simulating the pickups and needs to detect the triggers to assign Ownership, but this
    //  is problematic because something breaks if we add a RigidBody to the rat pickups... instead we could just
    //  have the client report when it picks something up and then the Server can replicate down the Ownership update,
    //  then everyone can locally simulate the follower code rather than relying on updates from the server
    //[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    //[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct PickUpOwnerAssignmentSystem : ISystem
    {
        ComponentLookup<CharacterFollowerThrowing> _characterLookup;
        // [ReadOnly]
        // public BufferLookup<PendingPickUp> CharacterPickUpBuffer;
        ComponentLookup<Ownership> _pickUpLookup;
        ComponentLookup<GhostOwner> _ghostOwnerLookup;
        ComponentLookup<NetworkId> _idLookup;
        ComponentLookup<Follower> _followerLookup;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }
        
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _characterLookup = state.GetComponentLookup<CharacterFollowerThrowing>(false);
            _pickUpLookup = state.GetComponentLookup<Ownership>(false);
            _ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(false);
            _idLookup = state.GetComponentLookup<NetworkId>(true);
            _followerLookup = state.GetComponentLookup<Follower>(false);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new PickUpJob()
                {
                    CharacterLookup = _characterLookup,
                    PickUpLookup = _pickUpLookup,
                    FollowerLookup = _followerLookup,
                    GhostOwnerLookup = _ghostOwnerLookup,
                    OwnerIdLookup = _idLookup,
                    ECB = ecb
                }
                .Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
