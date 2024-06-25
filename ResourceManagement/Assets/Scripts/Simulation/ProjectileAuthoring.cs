using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    // TODO | P1 | Projectile should expire after X seconds
    //  Projectiles should be eventually removed from the Scene so they aren't just rolling around forever
    //  Add a lifespan field here and implement a system which destroys the Projectile's entity once enough time
    //  has elapsed.
    public struct Projectile : IComponentData
    {
        public int InstigatorNetworkId;
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
