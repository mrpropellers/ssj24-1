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
    [GhostComponent, GhostEnabledBit]
    public struct ConvertToProjectile : IComponentData, IEnableableComponent
    {
        public uint ConversionPeriod;
        
        [GhostField]
        public NetworkTick TickStarted;
        [GhostField]
        public float3 TargetPosition;
        [GhostField]
        public quaternion TargetRotation;
        public int OwnerId;
        public float3 InitialPosition;
        public quaternion InitialRotation;

        // public float TimeStarted_Auth;
        // public float3 TargetPosition_Auth;
        // public quaternion TargetRotation_Auth;
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

        // TODO: Re-enable BurstCompile here once we're sure it's not throwing exceptions any more
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // if (!SystemAPI.GetSingleton<NetworkTime>().IsFirstTimeFullyPredictingTick)
            //     return;
            var now = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            
            foreach (var (localTransform, 
                         control, 
                         config,
                         thrower,
                         ghostOwner,
                         throwingEntity) in SystemAPI.Query<
                             RefRO<LocalTransform>, 
                             RefRW<ThirdPersonCharacterControl>, 
                             RefRO<ThrowerConfig>,
                             RefRW<FollowerThrower>,
                             RefRO<GhostOwner>
                         >()
                         .WithAll<Simulate, GhostOwnerIsLocal>()
                         .WithAll<ThrowableFollowerElement>()
                         .WithEntityAccess())
            {
                var throwables = 
                    state.EntityManager.GetBuffer<ThrowableFollowerElement>(throwingEntity);
                if (throwables.Length == 0)
                {
                    continue;
                }
                
                // Since we can't set this field the moment a follower is picked up we just make sure to do it here
                // Need to check if it exists because the buffer may be updated on Client before the Rat was
                // actually spawned (happens when you're standing directly on top of a spawner)
                if(state.EntityManager.Exists(throwables[^1].Follower))
                {
                    state.EntityManager.SetComponentEnabled<ForceInterpolatedGhost>(
                        throwables[^1].Follower, false);
                    if (throwables.Length > 1)
                    {
                        state.EntityManager.SetComponentEnabled<ForceInterpolatedGhost>(
                            throwables[^2].Follower, false);
                    }
                }
                // BUG: When picking up a bunch of rats at once, some get stuck as predicted
                if (throwables.Length > 2)
                {
                    state.EntityManager.SetComponentEnabled<ForceInterpolatedGhost>(
                        throwables[^3].Follower, true);
                }
                
                if (!control.ValueRO.Throw)
                    continue;

                if (thrower.ValueRO.TickNextThrowAllowed.IsValid
                    && thrower.ValueRO.TickNextThrowAllowed.IsNewerThan(now))
                    continue;

                
                // (Devin) We're not supposed to need to do this because input consumption should be handled by Unity,
                //  but seems like it gets screwy if we don't explicitly reset it here...
                control.ValueRW.Throw = false;
                var cooledDownTick = now;
                cooledDownTick.Add(config.ValueRO.ThrowCooldownTicks);
                thrower.ValueRW.TickNextThrowAllowed = cooledDownTick;
                thrower.ValueRW.NumThrownFollowers++;
                
                var numThrowables = throwables.Length;
                var followerIdx = numThrowables - 1;
                var follower = throwables[followerIdx].Follower;
                var followerTf = state.EntityManager.GetComponentData<LocalTransform>(follower);

                //Debug.Log($"Attempting to throw a rat from index {followerIdx}!");
                var throwerTf = localTransform.ValueRO;
                var throwOffset = throwerTf.TransformDirection(
                    new float3(0f, config.ValueRO.ThrowHeight, 3f));

                var toProjectile = state.EntityManager.GetComponentData<ConvertToProjectile>(follower);
                toProjectile.InitialPosition = followerTf.Position;
                toProjectile.InitialRotation = followerTf.Rotation;
                toProjectile.TickStarted = now;
                toProjectile.TargetPosition = throwerTf.Position + throwOffset;
                toProjectile.TargetRotation = throwerTf.Rotation;
                toProjectile.OwnerId = ghostOwner.ValueRO.NetworkId;
                state.EntityManager.SetComponentData(follower, toProjectile);
                // This is probably true by default, but better safe than sorry
                state.EntityManager.SetComponentEnabled<ConvertToProjectile>(follower, true);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
    
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(FollowerThrowingSystem))]
    [BurstCompile]
    public partial struct RemoveFollowersFromBufferSystem : ISystem
    {
        BufferLookup<ThrowableFollowerElement> m_FollowerBufferLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            m_FollowerBufferLookup = state.GetBufferLookup<ThrowableFollowerElement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.GetSingleton<NetworkTime>().IsFirstTimeFullyPredictingTick)
                return;

            m_FollowerBufferLookup.Update(ref state);
            //var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (ownership, _, followerEntity) in SystemAPI
                         .Query<RefRW<Ownership>, RefRO<Follower>>()
                         .WithAll<ConvertToProjectile, IsFollowingOwner, HasConfiguredOwner>()
                         .WithAll<Simulate, PredictedGhost>()
                         .WithEntityAccess())
            {
                var owner = ownership.ValueRW.Owner;
                m_FollowerBufferLookup.TryGetBuffer(owner, out var followerBuffer);
                var numFollowers = followerBuffer.Length;
                var followerIdx = numFollowers - 1;
                if (followerBuffer[followerIdx].Follower != followerEntity)
                {
                    Debug.LogWarning(
                        "Follower was not the last detected in buffer. That's weird! Looking for it...");
                    for (var i = followerIdx - 1; i >= 0; --i)
                    {
                        if (followerEntity == followerBuffer[i].Follower)
                        {
                            Debug.Log("Follower found earlier in buffer!");
                            followerIdx = i;
                            break;
                        }
                    }

                    var followerFound = followerIdx != numFollowers - 1;
                    if (!followerFound)
                    {
                        Debug.LogError("Follower not found in buffer. Something is wrong!");
                        continue;
                    }
                }
                
                followerBuffer.RemoveAt(followerIdx);
                state.EntityManager.SetComponentEnabled<IsFollowingOwner>(followerEntity, false);
            }
            // ecb.Playback(state.EntityManager);
            // ecb.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(RemoveFollowersFromBufferSystem))]
    public partial struct TweenToProjectileSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.GetSingleton<NetworkTime>().IsFirstTimeFullyPredictingTick)
               return;
            
            var now = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            var fraction = SystemAPI.GetSingleton<NetworkTime>().ServerTickFraction;
            
            foreach (var (tf, converter, follower) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<ConvertToProjectile>, RefRO<Follower>>()
                         .WithAll<Simulate, PredictedGhost, ConvertToProjectile>()
                         .WithNone<MarkedForDestroy>())
                         // Ensures that we only fetch Enabled ConvertToProjectile components
            {
                var period = converter.ValueRO.ConversionPeriod;
                var tickStarted = converter.ValueRO.TickStarted;
                var tickFinished = tickStarted;
                tickFinished.Add(period);
                if (!tickFinished.IsNewerThan(now))
                {
                    //Debug.Log($"Follower has reached its destination and should be destroyed!");
                    tf.ValueRW.Position = converter.ValueRW.TargetPosition;
                    tf.ValueRW.Rotation = converter.ValueRW.TargetRotation;
                    continue;
                }

                var t = (now.TicksSince(tickStarted) + fraction) / period;
                if (t > 1f + float.Epsilon)
                {
                    // Debug.LogError(
                    //     "Shouldn't be able to reach this because we early-out above...");
                    t = 1f;
                }
                tf.ValueRW.Position = math.lerp(converter.ValueRW.InitialPosition, converter.ValueRW.TargetPosition, t);
                tf.ValueRW.Rotation = math.slerp(converter.ValueRW.InitialRotation, converter.ValueRW.TargetRotation, t);
            }
        }
    }
    
    // TODO | P2 - NetCode | Pre-spawn the projectile
    //  We could guarantee responsiveness by spawning the rat projectile hidden and inert when the tween starts and
    //  then simply attach the presentation and apply the velocity when the tween finishes. This would guarantee
    //  the projectile is spawned on the Server by the time it should fire
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct ConvertToProjectileSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSetup>();
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // if (!SystemAPI.GetSingleton<NetworkTime>().IsFirstTimeFullyPredictingTick)
            //     return;
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var game = SystemAPI.GetSingleton<GameSetup>();
            var now = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            foreach (var (follower, pc, entity) in SystemAPI
                         .Query<RefRO<Follower>, RefRO<ConvertToProjectile>>()
                         .WithAll<Simulate, PredictedGhost, ConvertToProjectile>()
                         .WithNone<MarkedForDestroy>()
                         //.WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                var tickFinished = pc.ValueRO.TickStarted;
                tickFinished.Add(pc.ValueRO.ConversionPeriod);
                if (tickFinished.IsNewerThan(now)) // follower.ValueRO.ToProjectileTicks)
                    continue;
                
                //Debug.Log("converting to projectile!");
                
                var projectile = ecb.Instantiate(game.RatProjectileSimulation);
                var targetTf = new LocalTransform()
                {
                    Position = pc.ValueRO.TargetPosition,
                    Rotation = pc.ValueRO.TargetRotation,
                    Scale = 1f
                };
                ecb.SetComponent(projectile, targetTf);
                ecb.SetComponent(projectile, new Projectile()
                {
                    InstigatorNetworkId = pc.ValueRO.OwnerId,
                    TimeSpawned = (float)SystemAPI.Time.ElapsedTime,
                });
                var direction = targetTf.TransformDirection(new float3(0f, 0f, 1f));
                ecb.SetComponent(projectile, new PhysicsVelocity()
                {
                    Linear = follower.ValueRO.ProjectileSpeed * direction,
                    Angular = new float3(15f, 0f, 0f)
                });
                ecb.AddComponent<MarkedForDestroy>(entity);
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