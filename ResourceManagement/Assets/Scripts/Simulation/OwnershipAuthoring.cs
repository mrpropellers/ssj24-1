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
        public bool HasSetOwner;
        [GhostField]
        public Entity Owner;

        public bool CanBeClaimed => !HasSetOwner;
    }

    public class OwnershipAuthoring : MonoBehaviour
    {
        public class OwnershipBaker : Baker<OwnershipAuthoring>
        {
            public override void Bake(OwnershipAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Ownership { HasSetOwner = false, Owner = default });
            }
        }
    }

}

