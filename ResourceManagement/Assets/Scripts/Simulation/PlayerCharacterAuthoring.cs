using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    public struct PlayerCharacter : IComponentData
    {
        
    }

    [DisallowMultipleComponent]
    public class PlayerCharacterAuthoring : MonoBehaviour
    {
        class CharacterBaker : Baker<PlayerCharacterAuthoring>
        {
            public override void Bake(PlayerCharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerCharacter>(entity);
            }
        }
    }
}
