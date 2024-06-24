using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

namespace NetCode
{
#if UNITY_EDITOR
    public class GameplaySceneAuthoring : MonoBehaviour
    {
        public UnityEditor.SceneAsset Scene;
        public class GameplaySceneReferenceBaker : Baker<GameplaySceneAuthoring>
        {
            public override void Bake(GameplaySceneAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GameplaySceneReference()
                {
                    Value = new EntitySceneReference(authoring.Scene)
                });
            }
        }
    }
#endif // UNITY_EDITOR
}

