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
    public struct ConvertToProjectile : IComponentData, IEnableableComponent
    {
        public float ConversionPeriod;
        
        // These are set locally because we don't care if they diverge with Server
        public float TimeStarted;
        public int OwnerId;
        public float3 InitialPosition;
        public quaternion InitialRotation;
        public float3 TargetPosition;
        public quaternion TargetRotation;

        public float TimeFinished => TimeStarted + ConversionPeriod;
        
        [GhostField]
        public float TimeStarted_Auth;
        [GhostField]
        public float3 TargetPosition_Auth;
        [GhostField]
        public quaternion TargetRotation_Auth;
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    //[UpdateAfter(typeof(ThirdPersonCharacterVariableUpdateSystem))]
    [BurstCompile]
    public partial struct FollowerThrowingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
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
                         .WithAll<GhostOwnerIsLocal>()
                         .WithAll<Simulate>()
                         .WithAll<ThrowableFollowerElement>()
                         .WithEntityAccess())
            {
                // TODO | P1 NetCode | Set first-in-line follower to Predicted if it's on Interpolated
                //  While most of the followers are fine to be set to interpolated. For any given player, on their
                //  own follower queue, their first follower should be Predicted to make it more responsive when the
                //  player presses throw
                
                if (thrower.ValueRW.Counts.NumThrowableFollowers <= 0)
                {
                    if (thrower.ValueRW.Counts.NumThrowableFollowers < 0)
                    {
                        Debug.LogError($"Detected a NumThrowableFollowers that was less than 0. This is a bug!");
                    }
                    continue;
                }
                
                if (!control.ValueRW.Throw)
                    continue;

                var now = (float)SystemAPI.Time.ElapsedTime;
                var timeNextCanThrow = thrower.ValueRO.Counts.TimeLastThrowPerformed + config.ValueRO.ThrowCooldown;
                if (timeNextCanThrow > now)
                    continue;
                
                // (Devin) We're not supposed to need to do this because input consumption should be handled by Unity,
                //  but seems like it gets screwy if we don't explicitly reset it here...
                control.ValueRW.Throw = false;
                thrower.ValueRW.Counts.TimeLastThrowPerformed = now;
                
                var throwables = 
                    state.EntityManager.GetBuffer<ThrowableFollowerElement>(throwingEntity);

                var numThrowables = thrower.ValueRO.Counts.NumThrowableFollowers;
                if (throwables.Length == 0)
                {
                    Debug.LogError($"We thought we had {numThrowables} followers to throw, but there are none in the buffer. Something is wrong!");
                    thrower.ValueRW.Counts.NumThrowableFollowers = 0;
                    continue;
                }
                if (throwables.Length < numThrowables || throwables.Length > numThrowables + 1)
                {
                    // Something is likely wrong here..
                    Debug.LogError($"There are {throwables.Length} throwables in buffer but we should have {numThrowables}... what's going on here?");
                }
                else if (throwables.Length > numThrowables)
                {
                    // This is *probably* ok? Should only happen if latency is >= throwCooldown though
                    Debug.LogWarning("Throwing another follower before the last was authenticated by server...");
                }
                
                // BUG: Until I complete the work to get prediction/late cleanup on the buffer working,
                //  this indexing will break when you pick up a new follower after having thrown one
                var followerIdx = numThrowables - 1;
                var follower = throwables[followerIdx].Follower;
                thrower.ValueRW.Counts.NumThrowableFollowers--;
                
                var followerTf = state.EntityManager.GetComponentData<LocalTransform>(follower);

                Debug.Log($"Attempting to throw a rat from index {followerIdx}!");
                var throwerTf = localTransform.ValueRO;
                var throwOffset = throwerTf.TransformDirection(
                    new float3(0f, config.ValueRO.ThrowHeight, 1f));

                var toProjectile = new ConvertToProjectile()
                {
                    InitialPosition = followerTf.Position,
                    InitialRotation = followerTf.Rotation,
                    TimeStarted = now,
                    TargetPosition = throwerTf.Position + throwOffset,
                    TargetRotation = throwerTf.Rotation,
                    OwnerId = ghostOwner.ValueRO.NetworkId
                };
                // If we're on the client, these values may be overwritten later by the Server
                toProjectile.TimeStarted_Auth = toProjectile.TimeStarted;
                toProjectile.TargetPosition_Auth = toProjectile.TargetPosition;
                toProjectile.TargetRotation_Auth = toProjectile.TargetRotation;
                state.EntityManager.SetComponentData(follower, toProjectile);
                // This is probably true by default, but better safe than sorry
                state.EntityManager.SetComponentEnabled<ConvertToProjectile>(follower, true);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
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
            var now = (float)SystemAPI.Time.ElapsedTime;
            
            foreach (var (tf, converter, follower) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<ConvertToProjectile>, RefRO<Follower>>()
                         .WithAll<Simulate>()
                         // Ensures that we only fetch Enabled ConvertToProjectile components
                         .WithAll<ConvertToProjectile>())
            {
                var period = converter.ValueRO.ConversionPeriod;
                var timeStarted = converter.ValueRO.TimeStarted;
                var timeFinished = converter.ValueRO.TimeStarted + period;
                if (now > timeFinished)
                {
                    Debug.Log($"Follower has reached its destination and should be destroyed!");
                    tf.ValueRW.Position = converter.ValueRW.TargetPosition;
                    tf.ValueRW.Rotation = converter.ValueRW.TargetRotation;
                    continue;
                }

                var t = (now - timeStarted) / period;
                if (t > 1f)
                {
                    Debug.LogError(
                        "Shouldn't be able to reach this because we early-out above...");
                    t = 1f;
                }
                tf.ValueRW.Position = math.lerp(converter.ValueRW.InitialPosition, converter.ValueRW.TargetPosition, t);
                tf.ValueRW.Rotation = math.slerp(converter.ValueRW.InitialRotation, converter.ValueRW.TargetRotation, t);
            }
        }
    }
    
    // >>> TODO: IN PROGRESS: Spawn the projectile as soon as the ConvertToProjectile component is
    //  activated and link it to the Entity it spawned from. When the Projectile shows up on the 
    //  client, hide it until the follower is finished tweening, then destroy the follower and 
    //  activate the projectile (predicted)
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
            var now = (float)SystemAPI.Time.ElapsedTime;
            foreach (var (follower, pc, entity) in SystemAPI
                         .Query<RefRO<Follower>, RefRO<ConvertToProjectile>>()
                         .WithAll<Simulate>()
                         //.WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                if (now <  pc.ValueRO.TimeFinished) // follower.ValueRO.ToProjectileTicks)
                    continue;
                
                // TODO >>> IN PROGRESS: Move Projectile component logic to same prefab as Follower
                //  (merge Projectile/Pickup prefabs). Allow logic to run locally in addition to Server,
                //  and be sure to remove the Entity from the buffer here.
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