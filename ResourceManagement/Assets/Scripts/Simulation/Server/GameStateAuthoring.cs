using Unity.Entities;

namespace Simulation.Server
{
    public struct PlayerState : IBufferElementData
    {
        public uint SteamId;
        public int NetworkId;
        public int Score;
    }

    // public struct PendingRatScore : IBufferElementData
    // {
    //    
    // }
    
    public struct GameState : IComponentData
    {
        
    }
}
