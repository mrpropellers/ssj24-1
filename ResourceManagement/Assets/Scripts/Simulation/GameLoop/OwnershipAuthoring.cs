using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
    [GhostComponent]
    public struct Ownership : IComponentData
    {
        //public bool HasPendingOwner;
        [GhostField]
        public bool HasConfiguredOwner;
        [GhostField]
        public Entity Owner;

        public bool CanBeClaimed => !HasConfiguredOwner;
    }

    [GhostComponent]
    public struct NeedsOwnerAssignment : IComponentData
    {
        
    }

    public class OwnershipAuthoring : MonoBehaviour
    {
        public class OwnershipBaker : Baker<OwnershipAuthoring>
        {
            public override void Bake(OwnershipAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Ownership { HasConfiguredOwner = false, Owner = default });
                AddComponent(entity, new NeedsOwnerAssignment());
            }
        }
    }

}

