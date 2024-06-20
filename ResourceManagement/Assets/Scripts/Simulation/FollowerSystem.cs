using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    // TODO? Should I run this in the prediction system group? Seems like it'd be a heavy lift, but rat position
    //  will need to be synced occasionally in case a player loses control of them?
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
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
                         .Query<RefRW<LocalTransform>, RefRO<PickUp>, RefRO<Follower>>())
            {
                if (!pickUp.ValueRO.HasSetOwner)
                    continue;

                var targetTf = SystemAPI.GetComponent<LocalTransform>(pickUp.ValueRO.Owner);
                var goalDistance = follower.ValueRO.GoalDistance + follower.ValueRO.OwnerQueueRank * 1.2f;
                var currentDistance = math.distance(targetTf.Position, tf.ValueRW.Position);
                if (currentDistance < goalDistance)
                    continue;

                var direction = math.normalize(targetTf.Position - tf.ValueRW.Position);
                var travelVector = direction * follower.ValueRO.Speed * deltaTime;
                travelVector.y = 0f;
                tf.ValueRW.Position += travelVector;
                tf.ValueRW.Rotation = quaternion.LookRotation(travelVector, up);

                // TODO: Figure out how to do this with velocities so the rats can collide with each other
                //velocity.ValueRW.Linear = travelVector;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
