using Unity.Entities;

namespace Simulation.Server
{
    public struct DeathZone : IComponentData
    {
        
    }
    
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct RespawnDeadPlayers : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // TODO >>> Fire off death-zone collision job, collect results, move them to a respawn point
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
