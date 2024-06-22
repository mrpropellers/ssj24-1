using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(ThirdPersonCharacterVariableUpdateSystem))]
    [BurstCompile]
    public partial struct FollowerThrowingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<GameSetup>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var game = SystemAPI.GetSingleton<GameSetup>();
            //var followerBuffers = new List<DynamicBuffer<ThrowableFollowerElement>>();
            foreach (var (localTransform, 
                         control, 
                         character, 
                         ghostOwner,
                         throwingEntity) in SystemAPI
                         .Query<RefRO<LocalTransform>, 
                             RefRO<ThirdPersonCharacterControl>, 
                             RefRW<CharacterFollowerThrowing>, 
                             RefRO<GhostOwner>>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                if (!control.ValueRO.Throw)
                    continue;

                if (character.ValueRW.NumThrowableFollowers == 0)
                    continue;

                if (character.ValueRW.TickLastRatThrown.IsValid 
                    && tick.TicksSince(character.ValueRW.TickLastRatThrown) < character.ValueRW.ThrowCooldown)
                    continue;

                character.ValueRW.TickLastRatThrown = tick;
                character.ValueRW.NumThrowableFollowers--;
                var followerIdx = character.ValueRW.NumThrowableFollowers;
                var throwables =
                    state.EntityManager.GetBuffer<ThrowableFollowerElement>(throwingEntity);
                var throwee = throwables[followerIdx];
                // BUG: Sometimes this doesn't happen?? Followers continue to track target via rotation
                ecb.RemoveComponent<Follower>(throwee.Follower);
                //followerBuffers.Add(throwables);
                throwables.RemoveAt(followerIdx);

                Debug.Log($"Attempting to throw a rat from index {followerIdx}!");
                // >>> TODO: Add presentation initialization for RatProjectile
                var projectile = ecb.Instantiate(game.RatProjectileSimulation);
                var tf = localTransform.ValueRO;
                var throwOffset = tf.TransformDirection(new float3(0f, 0.5f, 0.25f));
                ecb.SetComponent(projectile, new LocalTransform()
                {
                    Position = tf.Position + throwOffset,
                    Rotation = tf.Rotation,
                    Scale = 1f
                });
                ecb.SetComponent(projectile, new GhostOwner() {NetworkId = ghostOwner.ValueRO.NetworkId});
                
                // if we spawned the rat, update the ratstate
                //character.ValueRW.NumThrowableFollowers -= 1;
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
