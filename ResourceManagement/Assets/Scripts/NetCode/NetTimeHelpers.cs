using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace NetCode
{
    [BurstCompile]
    public static class NetTimeHelpers
    {
        [BurstCompile]
        public static bool IfFirstFullySimulatedGetTick(in NetworkTime time, out NetworkTick tick)
        {
            tick = time.ServerTick;
            return time.IsFirstTimeFullyPredictingTick;
        }
        
    }
}
