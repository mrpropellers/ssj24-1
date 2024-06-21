using Unity.Entities;
using Unity.NetCode;

namespace Simulation
{

    public struct Rat : IComponentData { }

    public struct CharacterRatState : IComponentData
    {
        // immutable
        // public int MaxThrowableRats;
        public float InitialRatVelocity;
        public float ThrowHeight;
        public int ThrowCooldown;
        
        // mutable
        public int NumThrowableRats;
        // Probably need some kind of cooldown system
        public NetworkTick TickLastRatThrown;
    }
}
