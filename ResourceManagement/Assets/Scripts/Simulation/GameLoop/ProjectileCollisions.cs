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
    public struct ProjectileCollisionsJob : ITriggerEventsJob
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
            Entity cauldronEntity;
            Entity projectileEntity;
            if (CauldronLookup.TryGetComponent(triggerEvent.EntityA, out var cauldron))
            {
                cauldronEntity = triggerEvent.EntityA;
                projectileEntity = triggerEvent.EntityB;
            }
            else if (CauldronLookup.TryGetComponent(triggerEvent.EntityB, out cauldron))
            {
                cauldronEntity = triggerEvent.EntityB;
                projectileEntity = triggerEvent.EntityA;
            }
            else
            {
                return;
            }

            if (!ProjectileLookup.TryGetComponent(projectileEntity, out var projectile) ||
                projectile.HasScored ||
                !TransformLookup.TryGetComponent(projectileEntity, out var projectileTf))
            {
                return;
            }

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

            state.Dependency = new ProjectileCollisionsJob()
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

}
