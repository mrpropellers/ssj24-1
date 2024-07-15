using Presentation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
            state.RequireForUpdate<GameState>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
            s_Rand = new Random(1);
        }

        static bool hasUpdated = false;
        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameState>();
            if (!gameState.IsGameplayUnderway)
                return;
            
            if (!hasUpdated)
            {
                Debug.Log("Spawner updated");
                hasUpdated = true;
            }
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            // if (!networkTime.IsFirstTimeFullyPredictingTick)
            //     return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // TODO? Possibly optimize with RPC
            //  Rather than relying on the Ghost component to send state updates for the rats down to each client, 
            //  we could simply spawn them into local simulations with a Server->Client RPC and then just let each
            //  player update their local simulations. This would heavily reduce the amount of network traffic generated
            //  by rats who aren't yet being used as projectiles
            //  Alternatively, we could probably just put initial position/orientation/scale into the Pickup component
            //  data and use that like a pseudo-RPC, then tell the server not to serialize/transmit LocalTransform
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
                var rat = ecb.Instantiate(ratSpawner.ValueRW.Simulation);
                var spawnLocation = tf.ValueRO.Position + randomVector;
                ecb.SetComponent(rat, new LocalTransform()
                {
                    Position = spawnLocation,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });
                //ecb.SetComponent(rat, new NetworkId() { Value = -1 });
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
