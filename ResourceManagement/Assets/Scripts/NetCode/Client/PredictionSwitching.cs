using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

namespace Simulation
{
    // Attached to Ghost Entities that are not allowed to switch to Predicted when available.
    // Used to selectively pick which rats should be predicted (ones that are in flight or imminently
    // throwable)
    public struct ForceInterpolatedGhost : IComponentData, IEnableableComponent
    { }

    public partial struct FixCommandTargetSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CommandTarget>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var player in SystemAPI
                         .Query<RefRO<ThirdPersonPlayer>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                SystemAPI.SetSingleton(new CommandTarget() { targetEntity = player.ValueRO.ControlledCharacter });
            }
        }
    }
    
    //[BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [StructLayout(LayoutKind.Auto)]
    public partial struct PredictionSwitchingSystem : ISystem
    {
        ComponentLookup<GhostOwner> m_GhostOwnerFromEntity;
        ComponentLookup<ForceInterpolatedGhost> m_ForceInterpolation;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PredictionSwitchingSettings>();
            state.RequireForUpdate<CommandTarget>();
            state.RequireForUpdate<GhostPredictionSwitchingQueues>();
            m_GhostOwnerFromEntity = state.GetComponentLookup<GhostOwner>(true);
            m_ForceInterpolation = state.GetComponentLookup<ForceInterpolatedGhost>(true);
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var playerEnt = SystemAPI.GetSingleton<CommandTarget>().targetEntity;
            if (playerEnt == Entity.Null)
                return;

            var playerPos = state.EntityManager.GetComponentData<LocalTransform>(playerEnt).Position;

            var ghostPredictionSwitchingQueues = SystemAPI.GetSingletonRW<GhostPredictionSwitchingQueues>().ValueRW;
            var parallelEcb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            m_GhostOwnerFromEntity.Update(ref state);
            m_ForceInterpolation.Update(ref state);
            // (Devin) I don't know why we do this but it's the pattern they used in the example code...
            var ghostOwnerFromEntity = m_GhostOwnerFromEntity;
            var forceInterpolation = m_ForceInterpolation;

            var predictionSwitchingSettings = SystemAPI.GetSingleton<PredictionSwitchingSettings>();

            new SwitchToPredictedGhost
            {
                playerPos = playerPos,
                parallelEcb = parallelEcb,
                predictedQueue = ghostPredictionSwitchingQueues.ConvertToPredictedQueue,
                enterRadiusSq = predictionSwitchingSettings.PredictionSwitchingRadius * predictionSwitchingSettings.PredictionSwitchingRadius,
                ghostOwnerFromEntity = ghostOwnerFromEntity,
                forceInterpolation = forceInterpolation,
                transitionDurationSeconds = predictionSwitchingSettings.TransitionDurationSeconds,
            }.ScheduleParallel();

            var radiusPlusMargin = (predictionSwitchingSettings.PredictionSwitchingRadius + predictionSwitchingSettings.PredictionSwitchingMargin);
            new SwitchToInterpolatedGhost
            {
                playerPos = playerPos,
                parallelEcb = parallelEcb,
                interpolatedQueue = ghostPredictionSwitchingQueues.ConvertToInterpolatedQueue,
                exitRadiusSq = radiusPlusMargin * radiusPlusMargin,
                ghostOwnerFromEntity = ghostOwnerFromEntity,
                forceInterpolated = forceInterpolation,
                transitionDurationSeconds = predictionSwitchingSettings.TransitionDurationSeconds,
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithNone(typeof(PredictedGhost), typeof(SwitchPredictionSmoothing), typeof(GameState))]
        //[WithNone(typeof(ForceInterpolatedGhost))]
        [StructLayout(LayoutKind.Auto)]
        partial struct SwitchToPredictedGhost : IJobEntity
        {
            public float3 playerPos;
            public float enterRadiusSq;

            public NativeQueue<ConvertPredictionEntry>.ParallelWriter predictedQueue;
            public EntityCommandBuffer.ParallelWriter parallelEcb;

            [ReadOnly]
            public ComponentLookup<GhostOwner> ghostOwnerFromEntity;
            [ReadOnly]
            public ComponentLookup<ForceInterpolatedGhost> forceInterpolation;

            public float transitionDurationSeconds;

            void Execute(Entity ent, [EntityIndexInQuery] int entityIndexInQuery, in LocalTransform transform, in GhostInstance ghostInstance)
            {
                if (ghostInstance.ghostType < 0) return;

                if (forceInterpolation.HasComponent(ent) && forceInterpolation.IsComponentEnabled(ent))
                    return;

                if (math.distancesq(playerPos, transform.Position) < enterRadiusSq)
                {
                    predictedQueue.Enqueue(new ConvertPredictionEntry
                    {
                        TargetEntity = ent,
                        TransitionDurationSeconds = transitionDurationSeconds,
                    });
                }
            }
        }

        [BurstCompile]
        [WithNone(typeof(SwitchPredictionSmoothing), typeof(GhostOwnerIsLocal))]
        [WithAll(typeof(PredictedGhost))]
        partial struct SwitchToInterpolatedGhost : IJobEntity
        {
            public float3 playerPos;
            public float exitRadiusSq;

            public NativeQueue<ConvertPredictionEntry>.ParallelWriter interpolatedQueue;
            public EntityCommandBuffer.ParallelWriter parallelEcb;

            [ReadOnly]
            public ComponentLookup<GhostOwner> ghostOwnerFromEntity;
            [ReadOnly]
            public ComponentLookup<ForceInterpolatedGhost> forceInterpolated;
            public float transitionDurationSeconds;

            void Execute(Entity ent, [EntityIndexInQuery] int entityIndexInQuery, in LocalTransform transform, in GhostInstance ghostInstance)
            {
                if (ghostInstance.ghostType < 0) return;

                if (math.distancesq(playerPos, transform.Position) > exitRadiusSq  
                    || (forceInterpolated.TryGetComponent(ent, out _) && forceInterpolated.IsComponentEnabled(ent)))
                {
                    interpolatedQueue.Enqueue(new ConvertPredictionEntry
                    {
                        TargetEntity = ent,
                        TransitionDurationSeconds = transitionDurationSeconds,
                    });
                    // if (!ghostOwnerFromEntity.HasComponent(ent))
                    //     parallelEcb.RemoveComponent<URPMaterialPropertyBaseColor>(entityIndexInQuery, ent);
                }
            }
        }
    }

}
