using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    [GhostComponent]
    public struct ConvertToProjectile : IComponentData
    {
        [GhostField]
        public NetworkTick TickStarted;
        [GhostField]
        public int OwnerId;
        [GhostField]
        public float3 InitialPosition;
        [GhostField]
        public quaternion InitialRotation;
        [GhostField]
        public float3 TargetPosition;
        [GhostField]
        public quaternion TargetRotation;
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    //[UpdateAfter(typeof(ThirdPersonCharacterVariableUpdateSystem))]
    [BurstCompile]
    public partial struct FollowerThrowingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var time = SystemAPI.GetSingleton<NetworkTime>();
            // if (!time.IsFirstTimeFullyPredictingTick)
            //     return;
            var tick = time.ServerTick;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            //var followerBuffers = new List<DynamicBuffer<ThrowableFollowerElement>>();
            foreach (var (
                         localTransform, 
                         control, 
                         character, 
                         ghostOwner,
                         throwingEntity) in SystemAPI.Query<
                             RefRO<LocalTransform>, 
                             RefRW<ThirdPersonCharacterControl>, 
                             RefRW<CharacterFollowerThrowing>,
                             RefRO<GhostOwner>
                         >()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                if (!control.ValueRW.Throw)
                    continue;

                if (character.ValueRW.NumThrowableFollowers <= 0)
                    continue;

                if (character.ValueRW.TickLastRatThrown.IsValid 
                    && tick.TicksSince(character.ValueRW.TickLastRatThrown) < character.ValueRW.ThrowCooldown)
                    continue;
                
                control.ValueRW.Throw = false;
                character.ValueRW.TickLastRatThrown = tick;
                
                var throwables =
                    state.EntityManager.GetBuffer<ThrowableFollowerElement>(throwingEntity);

                if (throwables.Length < character.ValueRO.NumThrowableFollowers - 1)
                {
                    Debug.LogWarning("Attempting to throw a follower that's already been thrown?");
                    continue;
                }
                
                character.ValueRW.NumThrowableFollowers--;
                var followerIdx = character.ValueRW.NumThrowableFollowers;
                var throwee = throwables[followerIdx].Follower;
                // BUG: Sometimes this doesn't happen?? Followers continue to track target via rotation
                // TODO: After we have ConvertToProjectile, we will no longer need to remove this,
                //  we can just have the Follower system query for Followers with no ConvertToProjectile
                //ecb.RemoveComponent<Follower>(throwee.Follower);
                throwables.RemoveAt(followerIdx);
                var initialTf = state.EntityManager.GetComponentData<LocalTransform>(throwee);

                Debug.Log($"Attempting to throw a rat from index {followerIdx}!");
                var tf = localTransform.ValueRO;
                var throwOffset = tf.TransformDirection(new float3(0f, character.ValueRO.ThrowHeight, 2f));
                ecb.AddComponent(throwee, new ConvertToProjectile()
                {
                    InitialPosition = initialTf.Position,
                    InitialRotation = initialTf.Rotation,
                    TickStarted = tick,
                    TargetPosition = tf.Position + throwOffset,
                    TargetRotation = tf.Rotation,
                    OwnerId = ghostOwner.ValueRO.NetworkId
                });
                //ecb.AddComponent<GhostOwnerIsLocal>(throwee);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FollowerThrowingSystem))]
    public partial struct TweenToProjectileSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            foreach (var (tf, pc, follower) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<ConvertToProjectile>, RefRO<Follower>>())
            {
                // TODO: Might want to keep track of when tween started locally and speed it up if it's behind
                //tfLink.TransformSetter.ApplySmoothing = false;
                var t = (float)(tick.TicksSince(pc.ValueRO.TickStarted)) / follower.ValueRO.ToProjectileTicks;
                if (t > 1f)
                {
                    //Debug.Log("Follower has reached its destination and should be destroyed!");
                    t = 1f;
                }
                tf.ValueRW.Position = math.lerp(pc.ValueRW.InitialPosition, pc.ValueRW.TargetPosition, t);
                tf.ValueRW.Rotation = math.slerp(pc.ValueRW.InitialRotation, pc.ValueRW.TargetRotation, t);
            }
        }
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ConvertToProjectileSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSetup>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var game = SystemAPI.GetSingleton<GameSetup>();
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            foreach (var (follower, pc, entity) in SystemAPI
                         .Query<RefRO<Follower>, RefRO<ConvertToProjectile>>()
                         //.WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                if (tick.TicksSince(pc.ValueRO.TickStarted) < follower.ValueRO.ToProjectileTicks)
                    continue;
                // TODO: Figure out best way to request the Entity be destroyed on server?
                // ecb.SetEnabled(entity, false);
                // tfLink.Root.SetActive(false);
                Debug.Log("converting to projectile!");
                ecb.DestroyEntity(entity);
                var projectile = ecb.Instantiate(game.RatProjectileSimulation);
                var targetTf = new LocalTransform()
                {
                    Position = pc.ValueRO.TargetPosition,
                    Rotation = pc.ValueRO.TargetRotation,
                    Scale = 1f
                };
                ecb.SetComponent(projectile, targetTf);
                ecb.SetComponent(projectile, new Projectile() { InstigatorNetworkId = pc.ValueRO.OwnerId });
                //ecb.SetComponent(projectile, new GhostOwner() {NetworkId = pc.ValueRO.OwnerId});
                var direction = targetTf.TransformDirection(new float3(0f, 0f, 1f));
                ecb.SetComponent(projectile, new PhysicsVelocity()
                {
                    Linear = follower.ValueRO.ProjectileSpeed * direction
                });
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

namespace Presentation
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct DisableSmoothingOnProjectilesSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var tfLink in SystemAPI
                         .Query<TransformLink>()
                         .WithAll<Simulation.Follower, Simulation.ConvertToProjectile>())
            {
                tfLink.TransformSetter.ApplySmoothing = false;
            }
        }
    }
}