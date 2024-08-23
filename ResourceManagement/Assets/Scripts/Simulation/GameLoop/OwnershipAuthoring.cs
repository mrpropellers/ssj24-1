using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
    [GhostComponent]
    public struct Ownership : IComponentData
    {
        [GhostField]
        public bool HasConfiguredOwner;
        [GhostField]
        public Entity Owner;
    }

    [GhostComponent, GhostEnabledBit]
    public struct IsFollowingOwner : IComponentData, IEnableableComponent 
    { }
    
    [GhostComponent, GhostEnabledBit]
    public struct HasConfiguredOwner : IComponentData, IEnableableComponent
    {}

    public class OwnershipAuthoring : MonoBehaviour
    {
        public class OwnershipBaker : Baker<OwnershipAuthoring>
        {
            public override void Bake(OwnershipAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Ownership { HasConfiguredOwner = false, Owner = default });
                AddComponent(entity, new IsFollowingOwner() {});
                SetComponentEnabled<IsFollowingOwner>(entity, false);
                AddComponent(entity, new HasConfiguredOwner() { });
                SetComponentEnabled<HasConfiguredOwner>(entity, false);
            }
        }
    }

}

