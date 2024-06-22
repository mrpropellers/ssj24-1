using Unity.Entities;
using Unity.Mathematics;

namespace Simulation
{
    public struct Projectile : IComponentData
    {
        public float3 InitialPosition;
    }
}
