using Unity.Entities;
using UnityEngine;

namespace Presentation
{
    public class OverridableColorAuthoring : MonoBehaviour
    {
        private class ColorAssignmentBaker : Baker<OverridableColorAuthoring>
        {
            public override void Bake(OverridableColorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new ColorOverride());
                //SetComponentEnabled<ColorOverride>(entity, false);
            }
        }
    }
}

