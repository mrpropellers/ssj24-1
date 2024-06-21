using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
    [GhostComponent]
    public struct PickUp : IComponentData
    {
        //public bool HasPendingOwner;
        [GhostField]
        public bool HasSetOwner;
        [GhostField]
        public Entity Owner;

        public bool CanBePickedUp => !HasSetOwner;
    }

    public class PickUpAuthoring : MonoBehaviour
    {
        public class PickUpBaker : Baker<PickUpAuthoring>
        {
            public override void Bake(PickUpAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PickUp { HasSetOwner = false, Owner = default });
            }
        }
    }

}

