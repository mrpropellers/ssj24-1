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
        public ComponentLookup<Ownership> OwnershipLookup;
        [ReadOnly]
        public ComponentLookup<NeedsOwnerAssignment> NeedsAssignmentLookup;
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
            
            // If it has an Ownership component, it's already been claimed
            if (!NeedsAssignmentLookup.TryGetComponent(otherEntity, out _))
                return;

            if (!OwnershipLookup.TryGetComponent(otherEntity, out var ownership) || ownership.Owner != default)
                return;
            
            Debug.Log("Picking something up");
            
            ECB.SetComponent(otherEntity, new Ownership()
            {
                Owner = characterEntity,
                HasConfiguredOwner = false
            });
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
    [BurstCompile]
    public partial struct PickUpOwnerAssignmentSystem : ISystem
    {
        ComponentLookup<CharacterFollowerThrowing> _characterLookup;
        // [ReadOnly]
        // public BufferLookup<PendingPickUp> CharacterPickUpBuffer;
        ComponentLookup<Ownership> _ownershipLookup;
        ComponentLookup<NeedsOwnerAssignment> _needsAssignmentLookup;
        //ComponentLookup<GhostOwner> _ghostOwnerLookup;
        //ComponentLookup<NetworkId> _idLookup;
        ComponentLookup<Follower> _followerLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }
        
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _characterLookup = state.GetComponentLookup<CharacterFollowerThrowing>(false);
            _ownershipLookup = state.GetComponentLookup<Ownership>(false);
            _needsAssignmentLookup = state.GetComponentLookup<NeedsOwnerAssignment>(true);
            //_idLookup = state.GetComponentLookup<NetworkId>(true);
            _followerLookup = state.GetComponentLookup<Follower>(false);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            // Set up any followers collected last frame
            foreach (var (follower, ownership, followerEntity) in SystemAPI
                         .Query<RefRW<Follower>, RefRW<Ownership>>()
                         .WithAll<NeedsOwnerAssignment>()
                         .WithEntityAccess())
            {
                if (ownership.ValueRW.Owner == default)
                    continue;
                if (ownership.ValueRW.HasConfiguredOwner)
                {
                    // We should have already cleaned up its NeedsOwnerAssignment tag by now...
                    Debug.LogWarning("Detected a follower that's already been configured. That shouldn't be possible!");
                    continue;
                }

                _characterLookup.TryGetComponent(ownership.ValueRO.Owner, out var character);
                character.NumThrowableFollowers++;
                ecb.SetComponent(ownership.ValueRO.Owner, character);
                follower.ValueRW.OwnerQueueRank = character.NumThrowableFollowers + character.NumThrownFollowers;
                ecb.AppendToBuffer(ownership.ValueRO.Owner, new ThrowableFollowerElement()
                {
                    Follower = followerEntity
                });
                
                ownership.ValueRW.HasConfiguredOwner = true;
            }
            
            state.Dependency = new PickUpJob()
                {
                    CharacterLookup = _characterLookup,
                    OwnershipLookup = _ownershipLookup,
                    NeedsAssignmentLookup = _needsAssignmentLookup,
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

    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    [BurstCompile]
    //[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct CleanUpOwnerAssignmentTagSystem : ISystem
    {
        // Removes the NeedsOwnerAssignment tag component when things have been successfully configured
        // Has to be in its own System because we're not allowed to make structural changes (remove a component)
        // inside the PhysicsSystemGroup
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (ownership, entity) in SystemAPI
                         .Query<RefRO<Ownership>>().WithAll<NeedsOwnerAssignment>().WithEntityAccess())
            {
                // Leave any unconfigured things alone
                if (!ownership.ValueRO.HasConfiguredOwner)
                    continue;
                
                ecb.RemoveComponent<NeedsOwnerAssignment>(entity);
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
