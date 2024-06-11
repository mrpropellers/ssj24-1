using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Simulation
{
    public struct CharacterEntitySpawner : IComponentData
    {
        public Entity Simulated;
    }

    public class CharacterEntitySpawnerAuthoring : MonoBehaviour
    {
        [FormerlySerializedAs("CharacterSimulation")]
        [FormerlySerializedAs("CharacterPrefab")]
        [SerializeField]
        GameObject CharacterEntityPrefab;
        
        public class CharacterSpawnerBaker : Baker<CharacterEntitySpawnerAuthoring>
        {
            public override void Bake(CharacterEntitySpawnerAuthoring authoring)
            {
                var component = default(CharacterEntitySpawner);
                component.Simulated = GetEntity(authoring.CharacterEntityPrefab, TransformUsageFlags.Dynamic);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
