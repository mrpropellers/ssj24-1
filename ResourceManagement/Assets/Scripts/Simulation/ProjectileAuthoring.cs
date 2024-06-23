using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    public struct Projectile : IComponentData
    {
        public int InstigatorNetworkId;
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
