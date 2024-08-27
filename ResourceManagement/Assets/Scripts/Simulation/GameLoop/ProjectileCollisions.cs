using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    [BurstCompile]
    public struct ProjectileTriggersJob : ITriggerEventsJob
    {
        public DynamicBuffer<PendingRatScored> RatScoringBuffer;
        [ReadOnly]
        public ComponentLookup<LocalTransform> TransformLookup;
        public ComponentLookup<Projectile> ProjectileLookup;
        [ReadOnly]
        public ComponentLookup<Cauldron> CauldronLookup;
        
        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity otherEntity;
            Entity projectileEntity;
            if (ProjectileLookup.TryGetComponent(triggerEvent.EntityA, out var projectile))
            {
                projectileEntity = triggerEvent.EntityA;
                otherEntity = triggerEvent.EntityB;
            }
            else if (ProjectileLookup.TryGetComponent(triggerEvent.EntityB, out projectile))
            {
                otherEntity = triggerEvent.EntityA;
                projectileEntity = triggerEvent.EntityB;
            }
            else
            {
                return;
            }

            if (projectile.HasBounced || projectile.HasScored)
                return;

            if (!TransformLookup.TryGetComponent(projectileEntity, out var projectileTf))
            {
                Debug.LogError("Failed to get a projectile LocalTransform. Something is broken!");
                return;
            }

            if (CauldronLookup.TryGetComponent(otherEntity, out var cauldron))
            {
                Debug.Log("Rat cauldron dunk detected!");
                projectile.HasScored = true;
                RatScoringBuffer.Add(new PendingRatScored()
                {
                    RatEntityScored = projectileEntity,
                    OwnerId = projectile.InstigatorNetworkId,
                    LocationTriggered = projectileTf.Position,
                    ReceptacleCenter = cauldron.SplashZone
                });
            }
            else
            {
                Debug.Log("Rat hit a trigger that was not a cauldron??");
            }
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [StructLayout(LayoutKind.Auto)]
    public partial struct CauldronTriggerProcessingSystem : ISystem
    {
        ComponentLookup<LocalTransform> _tfLookup;
        ComponentLookup<Projectile> _projectileLookup;
        ComponentLookup<Cauldron> _cauldronLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PendingRatScored>();
            _tfLookup = state.GetComponentLookup<LocalTransform>(true);
            _projectileLookup = state.GetComponentLookup<Projectile>(false);
            _cauldronLookup = state.GetComponentLookup<Cauldron>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _cauldronLookup.Update(ref state);
            _projectileLookup.Update(ref state);
            _tfLookup.Update(ref state);
            var ratScoringBuffer = SystemAPI.GetSingletonBuffer<PendingRatScored>();

            state.Dependency = new ProjectileTriggersJob()
            {
                CauldronLookup = _cauldronLookup, 
                ProjectileLookup = _projectileLookup, 
                TransformLookup = _tfLookup, 
                RatScoringBuffer = ratScoringBuffer
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            
            state.CompleteDependency();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [BurstCompile]
    public struct ProjectileCollisionsJob : ICollisionEventsJob
    {
        public ComponentLookup<Projectile> ProjectileLookup;
        public DynamicBuffer<PendingCollision> CollisionsBuffer;

        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity otherEntity;
            Entity projectileEntity;
            if (ProjectileLookup.TryGetComponent(collisionEvent.EntityA, out var projectile))
            {
                projectileEntity = collisionEvent.EntityA;
                otherEntity = collisionEvent.EntityB;
            }
            else if (ProjectileLookup.TryGetComponent(collisionEvent.EntityB, out projectile))
            {
                otherEntity = collisionEvent.EntityA;
                projectileEntity = collisionEvent.EntityB;
            }
            else
            {
                return;
            }

            if (projectile.HasBounced || projectile.HasScored)
                return;

            CollisionsBuffer.Add(new PendingCollision()
            {
                //CollisionEvent = collisionEvent, 
                ProjectileState = projectile, 
                ProjectileEntity = projectileEntity, 
                EntityHit = otherEntity
            });
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [StructLayout(LayoutKind.Auto)]
    public partial struct RatCollisionProcessingSystem : ISystem
    {
        ComponentLookup<Projectile> _projectileLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PendingCollision>();
            _projectileLookup = state.GetComponentLookup<Projectile>(false);
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _projectileLookup.Update(ref state);
            var collisionBuffer = SystemAPI.GetSingletonBuffer<PendingCollision>();

            state.Dependency = new ProjectileCollisionsJob()
            {
                CollisionsBuffer = collisionBuffer,
                ProjectileLookup = _projectileLookup, 
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
            
            state.CompleteDependency();

            // if (collisionBuffer.Length > 0)
            //     Debug.Log($"Detected {collisionBuffer.Length} collisions.");
            foreach (var collision in collisionBuffer)
            {
                if (!collision.ProjectileState.HasBounced)
                {
                    var projectile = collision.ProjectileState;
                    projectile.HasBounced = true;
                    state.EntityManager.SetComponentData(collision.ProjectileEntity, projectile);
                }
            }
            
            collisionBuffer.Clear();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
