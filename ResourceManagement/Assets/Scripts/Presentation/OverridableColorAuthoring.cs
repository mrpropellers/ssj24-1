using Unity.Entities;
using UnityEngine;

namespace Presentation
{
    public class OverridableColorAuthoring : MonoBehaviour
    {
        public bool StartEnabled = true;
        
        private class ColorAssignmentBaker : Baker<OverridableColorAuthoring>
        {
            public override void Bake(OverridableColorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new ColorOverride());
                SetComponentEnabled<ColorOverride>(entity, authoring.StartEnabled);
            }
        }
    }
}

