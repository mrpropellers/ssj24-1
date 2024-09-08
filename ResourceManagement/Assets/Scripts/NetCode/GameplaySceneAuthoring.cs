using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
using UnityEngine.Serialization;

namespace NetCode
{
#if UNITY_EDITOR
    public class GameplaySceneAuthoring : MonoBehaviour
    {
        [FormerlySerializedAs("Scene")]
        public UnityEditor.SceneAsset GameplayScene;
        public UnityEditor.SceneAsset GameSetupScene;
        public GameObject GameStatePrefab;
        public class GameplaySceneReferenceBaker : Baker<GameplaySceneAuthoring>
        {
            public override void Bake(GameplaySceneAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GameplaySceneReferences()
                {
                    Level = new EntitySceneReference(authoring.GameplayScene),
                    GameSetup = new EntitySceneReference(authoring.GameSetupScene),
                    GameState = new EntityPrefabReference(authoring.GameStatePrefab)
                });
            }
        }
    }
#endif // UNITY_EDITOR
}

