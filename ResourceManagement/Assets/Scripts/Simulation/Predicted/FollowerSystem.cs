using Presentation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(TweenToProjectileSystem))]
    [BurstCompile]
    public partial struct FollowerSystem : ISystem
    {
        static readonly float3 up = new float3(0f, 1, 0);
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.GetSingleton<NetworkTime>().IsFirstTimeFullyPredictingTick)
                return;
            
            var deltaTime = SystemAPI.Time.DeltaTime;
            //var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (tf, pickUp, follower) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<Ownership>, RefRW<Follower>>()
                         .WithAll<IsFollowingOwner, HasConfiguredOwner>()
                         .WithAll<Simulate, PredictedGhost>()
                         .WithNone<ConvertToProjectile>())
            {
                if (!pickUp.ValueRO.HasConfiguredOwner)
                {
                    // This shouldn't be possible...
                    Debug.LogError(
                        "Found an entity that with HasConfiguredOwner component enabled but value is set to false!");
                    continue;
                }

                // (9/8/2024) TODO | P1 | Tech Debt | Clean up followers when a Player disconnects
                if (!state.EntityManager.Exists(pickUp.ValueRO.Owner))
                    continue;
                
                var targetTf = SystemAPI.GetComponent<LocalTransform>(pickUp.ValueRO.Owner);
                var followerBuffer = state.EntityManager.GetBuffer<ThrowableFollowerElement>(pickUp.ValueRO.Owner);
                var numFollowers = followerBuffer.Length;

                var placeInLine = numFollowers - follower.ValueRO.BufferIndex - 1;
                var goalDistance = follower.ValueRO.GoalDistance + placeInLine * 1.2f;
                var currentDistance = math.distance(targetTf.Position, tf.ValueRW.Position);

                var direction = math.normalize(targetTf.Position - tf.ValueRW.Position);
                var travelVector = direction * follower.ValueRO.Speed * deltaTime;
                travelVector.y = 0f;
                tf.ValueRW.Rotation = quaternion.LookRotation(travelVector, up);
                if (currentDistance < goalDistance)
                {
                    continue;
                }
                tf.ValueRW.Position += travelVector;

                // TODO: Figure out how to do this with physics velocities so the rats can collide with each other
                //velocity.ValueRW.Linear = travelVector;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct FollowerAnimationSystem : ISystem
    {
        static readonly int k_Speed = Animator.StringToHash("Speed");
        static readonly int k_RollUp = Animator.StringToHash("RollUp");
        static readonly int k_Offset = Animator.StringToHash("AnimationOffset");

        public void OnCreate(ref SystemState state)
        {
            // Scott couldn't figure out how to get a random number in here.  See AnimationOffset component!
            //foreach (var (animatorLink, follower) in SystemAPI
            //             .Query<AnimatorLink, RefRO<Follower>>()
            //             .WithNone<ConvertToProjectile, NeedsOwnerAssignment>())
            //{
            //    Unity.Mathematics.Random random = new Unity.Mathematics.Random();
            //    float randomOffset = random.NextFloat(0.1f, 0.9f);

            //    animatorLink.Animator.SetFloat(k_Offset, randomOffset);
            //}
        }

        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (animatorLink, follower, pickUp, tf) in SystemAPI
                         .Query<AnimatorLink, RefRO<Follower>, RefRO<Ownership>, RefRO<LocalTransform>>()
                         .WithAll<IsFollowingOwner, HasConfiguredOwner>()
                         .WithNone<ConvertToProjectile>())
            {
                var targetTf = SystemAPI.GetComponent<LocalTransform>(pickUp.ValueRO.Owner);
                var followerBuffer = state.EntityManager.GetBuffer<ThrowableFollowerElement>(pickUp.ValueRO.Owner);
                var placeInLine = followerBuffer.Length - follower.ValueRO.BufferIndex - 1;
                var goalDistance = follower.ValueRO.GoalDistance + placeInLine * 1.2f;
                var currentDistance = math.distance(targetTf.Position, tf.ValueRO.Position);

                if (currentDistance < goalDistance)
                {
                    animatorLink.Animator.SetFloat(k_Speed, 0f);
                }
                else
                {
                    animatorLink.Animator.SetFloat(k_Speed, 1f);
                }
            }
            
            // (In theory this is being done in the FollowerThrowingSystem)
            // foreach (var (animatorLink, follower) in SystemAPI
            //              .Query<AnimatorLink, RefRO<Follower>>()
            //              .WithNone<NeedsOwnerAssignment>()
            //              .WithAll<ConvertToProjectile>())
            // {
            //     animatorLink.Animator.SetBool(k_RollUp, true);
            // }
        }
    } 
}
