using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
    [GhostComponent]
    public struct Projectile : IComponentData
    {
        public int InstigatorNetworkId;
        public float TimeSpawned;
        [GhostField]
        public bool HasBounced;
        public bool HasScored;
    }

    public class ProjectileAuthoring : MonoBehaviour
    {
        
        public class ProjectileBaker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Projectile()
                {
                });
            }
        }
    }
}
