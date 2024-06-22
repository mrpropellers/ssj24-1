using Unity.Entities;

namespace Simulation
{
    [InternalBufferCapacity(64)]
    public struct ThrowableFollowerElement : IBufferElementData
    {
        public Entity Follower;
    }
}
