using Unity.Entities.Serialization;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
// Authoring component, a SceneAsset can only be used in the Editor
public class SceneLoaderAuthoring : MonoBehaviour
{
    /*
    public UnityEditor.SceneAsset Scene;

    class Baker : Baker<SceneLoaderAuthoring>
    {
        public override void Bake(SceneLoaderAuthoring authoring)
        {
            var reference = new EntitySceneReference(authoring.Scene);
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SceneLoaderData
            {
                LoadingEntity = reference
            });
        }
    }
    */
}
#endif