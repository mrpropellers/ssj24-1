using System;
using Simulation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace NetCode
{
    public struct AnticipationSyncTracking
    {
        public NetworkTick TickLastDiverged;
        public NetworkTick TickLastInSync;
        
        public bool IsOutOfSync => TickLastDiverged.IsNewerThan(TickLastInSync);
    }

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(TweenToProjectileSystem))]
    public partial struct ConvertToProjectileSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var converter in SystemAPI
                         .Query<RefRW<ConvertToProjectile>>()
                         .WithAll<Simulate>()
                         .WithAll<ConvertToProjectile>()
                    )
            {
                // If this has not been set, there's nothing to sync with
                if (converter.ValueRO.TimeStarted_Auth <= float.Epsilon)
                    continue;
                
                var authDelta = converter.ValueRO.TimeStarted_Auth - converter.ValueRO.TimeStarted;
                if (math.abs(authDelta) > 0.1f)
                {
                    Debug.LogWarning(
                        $"[NETCODE] Prediction anomaly detected! Delta between auth and anticipated is {authDelta}.");
                }

                converter.ValueRW.TimeStarted = converter.ValueRO.TimeStarted_Auth;
                converter.ValueRW.TargetPosition = converter.ValueRO.TargetPosition_Auth;
                converter.ValueRW.TargetRotation = converter.ValueRO.TargetRotation_Auth;
            }
        }
    }
}
