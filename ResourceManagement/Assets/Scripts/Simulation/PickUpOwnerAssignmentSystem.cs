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
        
        
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity character;
            Entity other;
            if (CharacterLookup.HasComponent(triggerEvent.EntityA))
            {
                character = triggerEvent.EntityA;
                other = triggerEvent.EntityB;
            }
            else if (CharacterLookup.HasComponent(triggerEvent.EntityA))
            {
                character = triggerEvent.EntityB;
                other = triggerEvent.EntityA;
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

            if (!PickUpLookup.TryGetComponent(other, out var pickUp) || !pickUp.CanBePickedUp)
                return;

            Debug.Log("Picking something up");
            pickUp.Owner = character;
            pickUp.HasSetOwner = true;
            ECB.SetComponent(other, pickUp);
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
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }
        
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _characterLookup = state.GetComponentLookup<ThirdPersonCharacterComponent>(true);
            _pickUpLookup = state.GetComponentLookup<PickUp>(false);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new PickUpJob()
                {
                    CharacterLookup = _characterLookup,
                    PickUpLookup = _pickUpLookup,
                    ECB = ecb
                }
                .Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
