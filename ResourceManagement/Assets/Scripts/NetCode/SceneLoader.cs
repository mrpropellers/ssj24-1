using Unity.Entities.Serialization;
using Unity.Entities;
using Unity.Collections;

public struct SceneLoaderData : IComponentData
{
    //public World sceneWorld;
    public Entity LoadingEntity;
    public FixedString64Bytes SceneName;
}