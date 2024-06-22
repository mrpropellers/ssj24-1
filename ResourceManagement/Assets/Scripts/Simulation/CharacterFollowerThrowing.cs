using Unity.Entities;
using Unity.NetCode;

namespace Simulation
{
    [GhostComponent]
    public struct CharacterFollowerThrowing : IComponentData
    {
        // immutable
        // public int MaxThrowableRats;
        public float InitialRatVelocity;
        public float ThrowHeight;
        public int ThrowCooldown;

        // mutable
        //public DynamicBuffer<ThrowableFollowerElement> ThrowableFollowers;
        public int NumThrownFollowers;
        [GhostField]
        public int NumThrowableFollowers;
        // To track cooldowns
        public NetworkTick TickLastRatThrown;
    }
}
