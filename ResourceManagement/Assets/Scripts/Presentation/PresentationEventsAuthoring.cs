using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Presentation
{
    public struct CauldronSplash : IBufferElementData
    {
        public Entity Cauldron;
        public LocalTransform Location;
    }

    public struct PlayerThrow : IBufferElementData
    {
        public Entity Player;
        public LocalTransform Location;
    }
    
    public struct PresentationEvents : IComponentData
    {
        
    }

    public class PresentationEventsAuthoring : MonoBehaviour
    {
        public class PresentationEventsBaker : Baker<PresentationEventsAuthoring>
        {
            public override void Bake(PresentationEventsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PresentationEvents>(entity);
                AddBuffer<CauldronSplash>(entity);
                AddBuffer<PlayerThrow>(entity);
            }
        }
    }
}
