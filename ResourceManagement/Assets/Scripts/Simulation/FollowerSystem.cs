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
                         .Query<RefRW<LocalTransform>, RefRO<Ownership>, RefRO<Follower>>())
            {
                if (!pickUp.ValueRO.HasSetOwner)
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
                    continue;
                tf.ValueRW.Position += travelVector;

                // TODO: Figure out how to do this with velocities so the rats can collide with each other
                //velocity.ValueRW.Linear = travelVector;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
