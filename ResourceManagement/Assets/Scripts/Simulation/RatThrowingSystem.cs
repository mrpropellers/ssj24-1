using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Simulation
{
    
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(ThirdPersonCharacterVariableUpdateSystem))]
    [BurstCompile]
    public partial struct RatThrowingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            foreach (var (localTransform, control, ratThrowState) in SystemAPI
                         .Query<RefRO<LocalTransform>, RefRO<ThirdPersonCharacterControl>, 
                             RefRW<CharacterRatState>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                if (!control.ValueRO.Throw)
                    continue;

                if (ratThrowState.ValueRW.NumThrowableRats == 0)
                    continue;

                if (tick.TicksSince(ratThrowState.ValueRW.TickLastRatThrown) < ratThrowState.ValueRW.ThrowCooldown)
                    continue;
                
                
                // TODO: Preston implements this
                // 1. Instantiate a rat entity
                // 2. Set its initial transform and velocity
                // 3. (do this later) Make sure it's synced on server and other player clients
                
                // if we spawned the rat, update the ratstate
                ratThrowState.ValueRW.TickLastRatThrown = tick;
                ratThrowState.ValueRW.NumThrowableRats -= 1;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
