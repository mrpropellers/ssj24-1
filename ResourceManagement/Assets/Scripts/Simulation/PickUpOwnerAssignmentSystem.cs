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
        public ComponentLookup<ThirdPersonCharacterComponent> CharacterLookup;
        // [ReadOnly]
        // public BufferLookup<PendingPickUp> CharacterPickUpBuffer;
        [ReadOnly]
        public ComponentLookup<PickUp> PickUpLookup;
        [ReadOnly]
        public ComponentLookup<Follower> FollowerLookup;
        
        
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity characterEntity;
            ThirdPersonCharacterComponent character;
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

            // if (!CharacterPickUpBuffer.TryGetBuffer(character, out var pendingPickUps))
            // {
            //     // TODO: Raise this error case
            //     return;
            // }

            if (!PickUpLookup.TryGetComponent(otherEntity, out var pickUp) || !pickUp.CanBePickedUp)
                return;
            
            
            Debug.Log("Picking something up");
            pickUp.Owner = characterEntity;
            pickUp.HasSetOwner = true;
            ECB.SetComponent(otherEntity, pickUp);
            if (FollowerLookup.TryGetComponent(otherEntity, out var follower))
            {
                follower.OwnerQueueRank = character.NumFollowers;
                ECB.SetComponent(otherEntity, follower);
                character.NumFollowers++;
                ECB.SetComponent(characterEntity, character);
            }
        }
    }
    
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    //[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PickUpOwnerAssignmentSystem : ISystem
    {
        ComponentLookup<ThirdPersonCharacterComponent> _characterLookup;
        // [ReadOnly]
        // public BufferLookup<PendingPickUp> CharacterPickUpBuffer;
        ComponentLookup<PickUp> _pickUpLookup;
        ComponentLookup<Follower> _followerLookup;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }
        
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _characterLookup = state.GetComponentLookup<ThirdPersonCharacterComponent>(false);
            _pickUpLookup = state.GetComponentLookup<PickUp>(false);
            _followerLookup = state.GetComponentLookup<Follower>(false);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new PickUpJob()
                {
                    CharacterLookup = _characterLookup,
                    PickUpLookup = _pickUpLookup,
                    FollowerLookup = _followerLookup,
                    ECB = ecb
                }
                .Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
