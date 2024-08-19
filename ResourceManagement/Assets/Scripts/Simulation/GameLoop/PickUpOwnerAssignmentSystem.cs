using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
        public ComponentLookup<FollowerThrower> CharacterLookup;
        [ReadOnly]
        public ComponentLookup<Ownership> OwnershipLookup;
        [ReadOnly]
        public ComponentLookup<IsFollowingOwner> NeedsConfigureLookup;
        [ReadOnly]
        public ComponentLookup<Follower> FollowerLookup;
        
        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity characterEntity;
            Entity otherEntity;
            if (CharacterLookup.TryGetComponent(triggerEvent.EntityA, out _))
            {
                characterEntity = triggerEvent.EntityA;
                otherEntity = triggerEvent.EntityB;
            }
            else if (CharacterLookup.TryGetComponent(triggerEvent.EntityA, out _))
            {
                characterEntity = triggerEvent.EntityB;
                otherEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            // If no Follower component, it's probably not a rat
            if (!FollowerLookup.TryGetComponent(otherEntity, out _))
                return;
            
            if (!OwnershipLookup.TryGetComponent(otherEntity, out var ownership) || ownership.Owner != default)
                return;

            // If it's already been collected for assignment, we don't need to do it again
            if (NeedsConfigureLookup.IsComponentEnabled(otherEntity))
                return;
            
            Debug.Log("Picking something up");
            
            ownership.Owner = characterEntity;
            ownership.HasConfiguredOwnerServer = false;
            ECB.SetComponent(otherEntity, ownership);
            ECB.SetComponentEnabled<IsFollowingOwner>(otherEntity, true);
        }
    }
    
    [UpdateInGroup(typeof(PhysicsSystemGroup)), UpdateBefore(typeof(PickUpOwnerAssignmentSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile, StructLayout(LayoutKind.Auto)]
    public partial struct PickUpCollectionRadiusSystem : ISystem
    {
        ComponentLookup<FollowerThrower> _characterLookup;
        ComponentLookup<Ownership> _ownershipLookup;
        ComponentLookup<IsFollowingOwner> _needsAssignmentLookup;
        ComponentLookup<Follower> _followerLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            _characterLookup = state.GetComponentLookup<FollowerThrower>(false);
            _ownershipLookup = state.GetComponentLookup<Ownership>(false);
            _needsAssignmentLookup = state.GetComponentLookup<IsFollowingOwner>(true);
            _followerLookup = state.GetComponentLookup<Follower>(false);
        }
        
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            _characterLookup.Update(ref state);
            _ownershipLookup.Update(ref state);
            _needsAssignmentLookup.Update(ref state);
            _followerLookup.Update(ref state);
            
            state.Dependency = new PickUpJob()
                {
                    CharacterLookup = _characterLookup,
                    OwnershipLookup = _ownershipLookup,
                    NeedsConfigureLookup = _needsAssignmentLookup,
                    FollowerLookup = _followerLookup,
                    // GhostOwnerLookup = _ghostOwnerLookup,
                    // OwnerIdLookup = _idLookup,
                    ECB = ecb
                }
                .Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
    
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    public partial struct PickUpOwnerAssignmentSystem : ISystem
    {
        ComponentLookup<FollowerThrower> _characterLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            _characterLookup = state.GetComponentLookup<FollowerThrower>(false);
        }
        
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _characterLookup.Update(ref state);
            var now = (float)SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (follower, ownership, followerEntity) in SystemAPI
                         .Query<RefRW<Follower>, RefRW<Ownership>>()
                         .WithAll<IsFollowingOwner>()
                         .WithNone<HasConfiguredOwner>()
                         .WithEntityAccess())
            {
                if (ownership.ValueRW.Owner == default)
                {
                    Debug.LogError("Detected follower that has been picked up but has no Owner.");
                    continue;
                }
                if (ownership.ValueRW.HasConfiguredOwnerServer)
                {
                    // We might get multiple PickUp triggers for the same follower due to re-simulation
                    Debug.Log("Detected a follower that's already been configured.");
                    continue;
                }

                var ownerEntity = ownership.ValueRO.Owner;
                _characterLookup.TryGetComponent(ownerEntity, out var thrower);
                thrower.Counts.NumThrowableFollowers++;
                thrower.Counts.TimeLastFollowerPickedUp = now;
                thrower.Counts_Auth = thrower.Counts;
                state.EntityManager.SetComponentData(ownerEntity, thrower);
                var counts = thrower.Counts;
                follower.ValueRW.OwnerQueueRank = counts.NumThrowableFollowers + counts.NumThrownFollowers;
                var followerBuffer = state.EntityManager.GetBuffer<ThrowableFollowerElement>(ownerEntity);
                followerBuffer.Add(new ThrowableFollowerElement()
                {
                    Follower = followerEntity
                });
                
                ownership.ValueRW.HasConfiguredOwnerServer = true;
                ecb.AddComponent<HasConfiguredOwner>(followerEntity);
                //state.EntityManager.SetComponentEnabled<IsFollowingOwner>(followerEntity, false);
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    public partial struct PickUpAddToClientBufferSystem : ISystem
    {
        ComponentLookup<FollowerThrower> _characterLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            _characterLookup = state.GetComponentLookup<FollowerThrower>(false);
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _characterLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (ownership, followerEntity) in SystemAPI
                         .Query<RefRW<Ownership>>()
                         .WithAll<Follower, IsFollowingOwner>()
                         .WithNone<HasConfiguredOwner>()
                         .WithEntityAccess())
            {
                if (ownership.ValueRW.Owner == default)
                {
                    Debug.LogError("Detected follower that has been picked up but has no Owner.");
                    continue;
                }

                if (!ownership.ValueRW.HasConfiguredOwnerServer)
                {
                    // This is probably impossible?
                    Debug.LogError("Detected a follower that hasn't been configured on Server.");
                    continue;
                }

                if (ownership.ValueRW.HasConfiguredOwnerClient)
                {
                    // Not sure if this is bad or not yet
                    Debug.Log("Detected a follower that's already been configured.");
                    continue;
                }

                var ownerEntity = ownership.ValueRO.Owner;
                var followerBuffer = state.EntityManager.GetBuffer<ThrowableFollowerElement>(ownerEntity);
                followerBuffer.Add(new ThrowableFollowerElement() { Follower = followerEntity });

                ownership.ValueRW.HasConfiguredOwnerClient = true;
                ecb.AddComponent<HasConfiguredOwner>(followerEntity);

                //state.EntityManager.SetComponentEnabled<IsFollowingOwner>(followerEntity, false);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
