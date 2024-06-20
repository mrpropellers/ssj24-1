using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Simulation.Server
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct RatPickupSpawningSystem : ISystem
    {
        static Random s_Rand;
        
        //[BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RatPickupSpawner>()
                .WithAll<LocalTransform>();
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            s_Rand = new Random(1);
        }

        static bool hasUpdated = false;
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!hasUpdated)
            {
                Debug.Log("Spawner updated");
                hasUpdated = true;
            }
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            // if (!networkTime.IsFirstTimeFullyPredictingTick)
            //     return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var tick = networkTime.ServerTick;
            foreach (var (tf, ratSpawner) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRW<RatPickupSpawner>>())
            {
                var spawnTick = ratSpawner.ValueRW.TickLastSpawned;
                if (spawnTick.IsValid && tick.TicksSince(spawnTick) < ratSpawner.ValueRW.SpawnCooldownTicks)
                    continue;
                Debug.Log("Spawning a guy");
                ratSpawner.ValueRW.TickLastSpawned = tick;
                var randomVector = ratSpawner.ValueRW.SpawnRadius * s_Rand.NextFloat3();
                randomVector.y = tf.ValueRO.Position.y + 0.25f;
                var rat = ecb.Instantiate(ratSpawner.ValueRW.RatPickup);
                ecb.SetComponent(rat, new LocalTransform()
                {
                    Position = tf.ValueRO.Position + randomVector
                });
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
