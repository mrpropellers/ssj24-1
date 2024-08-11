using NetCode;
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
    [InternalBufferCapacity(64)]
    public struct ThrowableFollowerElement : IBufferElementData
    {
        public Entity Follower;
    }
    
    [GhostComponent]
    public struct ConvertToProjectile : IComponentData
    {
        public NetworkTick TickStartedLocal;
        public float ConversionPeriod;
        [GhostField]
        public NetworkTick TickConversionFinished;
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
            var tickNow = time.ServerTick;
            var tickConversionFinished = tickNow;
            tickConversionFinished.Add(Follower.ToProjectileTicks);
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
                    && tickNow.TicksSince(character.ValueRW.TickLastRatThrown) < character.ValueRW.ThrowCooldown)
                    continue;
                
                control.ValueRW.Throw = false;
                character.ValueRW.TickLastRatThrown = tickNow;
                
                var throwables =
                    state.EntityManager.GetBuffer<ThrowableFollowerElement>(throwingEntity);

                if (throwables.Length != character.ValueRO.NumThrowableFollowers)
                {
                    // (8.10.2024) BUG | P1 - Gameplay | Sometimes this warning trips indefinitely
                    //  Player can sometimes get stuck in a soft-lock where they can no longer throw rats because
                    //  this condition trips every time
                    Debug.LogWarning("Attempting to throw a follower that's already been thrown?");
                    continue;
                }

                if(throwables.Length == 0)
                {
                    Debug.Log("No throwables to throw!");
                    continue;
                }
                
                character.ValueRW.NumThrowableFollowers--;
                var followerIdx = character.ValueRW.NumThrowableFollowers;
                var throwee = throwables[followerIdx].Follower;
                throwables.RemoveAt(followerIdx);
                var initialTf = state.EntityManager.GetComponentData<LocalTransform>(throwee);

                Debug.Log($"Attempting to throw a rat from index {followerIdx}!");
                var tf = localTransform.ValueRO;
                var throwOffset = tf.TransformDirection(new float3(0f, character.ValueRO.ThrowHeight, 2f));
                ecb.AddComponent(throwee, new ConvertToProjectile()
                {
                    InitialPosition = initialTf.Position,
                    InitialRotation = initialTf.Rotation,
                    TickConversionFinished = tickConversionFinished,
                    TargetPosition = tf.Position + throwOffset,
                    TargetRotation = tf.Rotation,
                    OwnerId = ghostOwner.ValueRO.NetworkId
                });
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
            
            foreach (var (tf, projectileConversion, follower) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<ConvertToProjectile>, RefRO<Follower>>())
            {
                if (tick.IsNewerThan(projectileConversion.ValueRO.TickConversionFinished))
                {
                    Debug.Log($"[{tick}] Follower has reached its destination and should be destroyed!");
                    tf.ValueRW.Position = projectileConversion.ValueRW.TargetPosition;
                    tf.ValueRW.Rotation = projectileConversion.ValueRW.TargetRotation;
                    continue;
                }
                if (!projectileConversion.ValueRW.TickStartedLocal.IsValid)
                {
                    projectileConversion.ValueRW.TickStartedLocal = tick;
                    projectileConversion.ValueRW.ConversionPeriod = 
                        projectileConversion.ValueRO.TickConversionFinished.TicksSince(tick);
                }
                
                
                var t = tick.TicksSince(projectileConversion.ValueRO.TickStartedLocal) / 
                    projectileConversion.ValueRO.ConversionPeriod;
                if (t > 1f)
                {
                    Debug.LogError(
                        "Shouldn't be able to reach this because we early-out if tick is newer than TickConversionFinished");
                    t = 1f;
                }
                tf.ValueRW.Position = math.lerp(projectileConversion.ValueRW.InitialPosition, projectileConversion.ValueRW.TargetPosition, t);
                tf.ValueRW.Rotation = math.slerp(projectileConversion.ValueRW.InitialRotation, projectileConversion.ValueRW.TargetRotation, t);
            }
        }
    }
    
    // (8.10.24) TODO | P2 - NetCode/Gameplay | Locally simulate projectiles after Throw started
    //  Currently, thrown projectiles tween to their thrown state locally, but then hover in the air until they
    //  receive the replicated Ghostified replacement. However, given that the projectiles should be deterministically
    //  positioned with timing derived from the Server clock, we should be able to fully simulate it locally, and
    //  simply not calculate damage on clients until the Server confirms.
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
                if (pc.ValueRO.TickConversionFinished.IsNewerThan(tick)) // follower.ValueRO.ToProjectileTicks)
                    continue;
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
                var direction = targetTf.TransformDirection(new float3(0f, 0f, 1f));
                ecb.SetComponent(projectile, new PhysicsVelocity()
                {
                    Linear = follower.ValueRO.ProjectileSpeed * direction,
                    Angular = new float3(15f, 0f, 0f)
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
        static readonly int k_RollUp = Animator.StringToHash("RollUp");

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (tfLink, animatorLink) in SystemAPI
                         .Query<TransformLink, AnimatorLink>()
                         .WithAll<Simulation.Follower, Simulation.ConvertToProjectile>())
            {
                tfLink.TransformSetter.ApplySmoothing = false;
                animatorLink.Animator.SetBool(k_RollUp, true);
            }
        }
    }
}