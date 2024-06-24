using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Simulation
{
    [BurstCompile]
    public struct CauldronDepositJob : ITriggerEventsJob
    {
        public EntityCommandBuffer ECB;
        
        [ReadOnly]
        public ComponentLookup<Projectile> ProjectileLookup;
        [ReadOnly]
        public ComponentLookup<Cauldron> OwnershipLookup;
        
        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity characterEntity;
            Entity otherEntity;
            // if (CharacterLookup.TryGetComponent(triggerEvent.EntityA, out _))
            // {
            //     characterEntity = triggerEvent.EntityA;
            //     otherEntity = triggerEvent.EntityB;
            // }
            // else if (CharacterLookup.TryGetComponent(triggerEvent.EntityA, out _))
            // {
            //     characterEntity = triggerEvent.EntityB;
            //     otherEntity = triggerEvent.EntityA;
            // }
            // else
            // {
            //     return;
            // }

            // // If no Follower component, it's probably not a rat
            // if (!FollowerLookup.TryGetComponent(otherEntity, out _))
            //     return;
            // 
            // // If it has an Ownership component, it's already been claimed
            // if (!NeedsAssignmentLookup.TryGetComponent(otherEntity, out _))
            //     return;

            // if (!OwnershipLookup.TryGetComponent(otherEntity, out var ownership) || ownership.Owner != default)
            //     return;
            // 
            // Debug.Log("Picking something up");
            // 
            // ECB.SetComponent(otherEntity, new Ownership()
            // {
            //     Owner = characterEntity,
            //     HasConfiguredOwner = false
            // });
        }
    }
    
    public partial struct CauldronScoring : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
