using Presentation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [BurstCompile]
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
                         .WithAll<Simulate, IsFollowingOwner, HasConfiguredOwner>()
                         .WithNone<ConvertToProjectile>())
            {
                if (!pickUp.ValueRO.HasConfiguredOwner)
                {
                    // This shouldn't be possible...
                    Debug.LogError(
                        "Found an entity that with HasConfiguredOwner component enabled but value is set to false!");
                    continue;
                }
                
                var targetTf = SystemAPI.GetComponent<LocalTransform>(pickUp.ValueRO.Owner);
                var ownerData = state.EntityManager.GetComponentData<FollowerThrower>(pickUp.ValueRO.Owner).Counts;
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

        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (animatorLink, follower) in SystemAPI
                         .Query<AnimatorLink, RefRO<Follower>>()
                         .WithAll<IsFollowingOwner>()
                         .WithNone<ConvertToProjectile>())
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
