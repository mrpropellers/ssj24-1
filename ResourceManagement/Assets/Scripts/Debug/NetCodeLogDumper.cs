using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEditor.Rendering;
using UnityEngine;

namespace NetCode
{
#if !UNITY_EDITOR
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct NetCodeLogDumper : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (true) //(!Debug.isDebugBuild)
                return;
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var isServer = state.World.IsServer();
            var prefix = networkTime.IsFirstTimeFullyPredictingTick ? "!" : ".";
            var name = isServer ? "SERVER" : "CLIENT";
            Debug.Log($"{prefix}[{name}] ({SystemAPI.Time.ElapsedTime}) || " +
                $"{networkTime.PredictedTickIndex}|{networkTime.ServerTick.TickValue} " +
                $"|| {networkTime.ServerTickFraction}");
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
#endif
}
