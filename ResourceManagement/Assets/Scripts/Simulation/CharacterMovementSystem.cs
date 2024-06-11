using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct CharacterMovementSystem : ISystem
    {
    
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var speed = SystemAPI.Time.DeltaTime * 4;
            foreach (var (input, trans) in SystemAPI
                         .Query<RefRO<CharacterMovement>, RefRW<LocalTransform>>().WithAll<Simulate>())
            {
                var moveInput = math.normalizesafe(input.ValueRO.Lateral) * speed;
                trans.ValueRW.Position += new float3(moveInput.x, 0, moveInput.y);
            }
        }
    }
}
