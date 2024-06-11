using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    public struct Character : IComponentData
    {
        
    }

    [DisallowMultipleComponent]
    public class CharacterAuthoring : MonoBehaviour
    {
        class CharacterBaker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Character>(entity);
            }
        }
    }
}
