using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using System;
using Unity.Physics;

namespace Simulation
{
    
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(ThirdPersonCharacterVariableUpdateSystem))]
    [BurstCompile]
    public partial struct RatThrowingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            foreach (var (localTransform, control, ratThrowState) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<ThirdPersonCharacterControl>, 
                             RefRW<CharacterRatState>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                
                if (!control.ValueRO.Throw)
                    continue;
                Debug.Log("Rat Throw Registered");

                //if (ratThrowState.ValueRO.NumThrowableRats == 0)
                //    continue;
                
                //if (tick.TicksSince(ratThrowState.ValueRO.TickLastRatThrown) < ratThrowState.ValueRO.ThrowCooldown)
                //    continue;
                
                GameSetup gameSetup = SystemAPI.GetSingleton<GameSetup>();

                var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
                var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                var networkTime = SystemAPI.GetSingleton<NetworkTime>();

                if (!networkTime.IsFirstTimeFullyPredictingTick) return;

                var ratAttackTransform = LocalTransform.FromPosition(localTransform.ValueRO.Position);
                ratAttackTransform.Position.y = ratThrowState.ValueRO.ThrowHeight;
                
                var newRatAttack = ecb.Instantiate(gameSetup.RatProjectile);

                //bc newRatAttack is NOT a game object, I can't access its shit.
                //ratThrowState.ValueRO.InitialRatVelocity
                //var ratVelocity = ratAttackTransform.forward;
                ecb.SetComponent(newRatAttack, ratAttackTransform);

                //I'm wondering if the object needs to set its own velocity when it spwans
                //

                // 1. Instantiate a rat entity
                // 2. Set its initial transform and velocity
                // 3. (do this later) Make sure it's synced on server and other player clients

                // if we spawned the rat, update the ratstate
                ratThrowState.ValueRW.TickLastRatThrown = tick;
                ratThrowState.ValueRW.NumThrowableRats -= 1;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

}
