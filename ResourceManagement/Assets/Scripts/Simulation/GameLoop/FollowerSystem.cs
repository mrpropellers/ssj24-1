using Presentation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    // TODO? Run this locally instead of on the Server
    //  Location of rat followers is only really relevant for the presentation layer. If we can figure out how to 
    //  replicate down only the spawn location, clients can run all the follower code locally and not rely on the Server
    //  to constantly be updating these transforms
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct FollowerSystem : ISystem
    {
        static readonly float3 up = new float3(0f, 1, 0);
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            //var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (tf, pickUp, follower) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRO<Ownership>, RefRW<Follower>>()
                         .WithNone<ConvertToProjectile, NeedsOwnerAssignment>())
            {
                if (!pickUp.ValueRO.HasConfiguredOwner)
                    continue;

                var targetTf = SystemAPI.GetComponent<LocalTransform>(pickUp.ValueRO.Owner);
                var ownerData = state.EntityManager.GetComponentData<CharacterFollowerThrowing>(pickUp.ValueRO.Owner);
                var placeInLine = ownerData.NumThrowableFollowers - (follower.ValueRO.OwnerQueueRank - ownerData.NumThrownFollowers);
                var goalDistance = follower.ValueRO.GoalDistance + placeInLine * 1.2f;
                var currentDistance = math.distance(targetTf.Position, tf.ValueRW.Position);

                var direction = math.normalize(targetTf.Position - tf.ValueRW.Position);
                var travelVector = direction * follower.ValueRO.Speed * deltaTime;
                travelVector.y = 0f;
                tf.ValueRW.Rotation = quaternion.LookRotation(travelVector, up);
                if (currentDistance < goalDistance)
                {
                    follower.ValueRW.CurrentSpeed = 0f;
                    continue;
                }
                tf.ValueRW.Position += travelVector;
                follower.ValueRW.CurrentSpeed = follower.ValueRO.Speed;

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
            foreach (var (animatorLink, follower) in SystemAPI
                         .Query<AnimatorLink, RefRO<Follower>>()
                         .WithNone<ConvertToProjectile, NeedsOwnerAssignment>())
            {
                animatorLink.Animator.SetFloat(k_Speed, follower.ValueRO.CurrentSpeed / follower.ValueRO.Speed);
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
