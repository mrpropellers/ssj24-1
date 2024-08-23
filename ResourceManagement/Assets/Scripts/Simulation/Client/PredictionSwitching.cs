using Unity.Burst;
using Unity.Entities;

namespace Simulation
{
    [BurstCompile]
    public partial struct PredictionSwitching : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
