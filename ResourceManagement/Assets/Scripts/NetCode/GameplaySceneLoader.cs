using UnityEngine;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;

namespace NetCode
{
    public struct GameplaySceneReference : IComponentData
    {
        public EntitySceneReference Value;
    }

    public partial class GameplaySceneLoaderSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<GameplaySceneReference>();
        }

        protected override void OnUpdate()
        {
            if (!ClientConnectionManager.ShouldInitializeWorlds)
                return;

            Debug.Log("Initializing gameplay scene.");
            var gameScene = SystemAPI.GetSingleton<GameplaySceneReference>().Value;
            ClientConnectionManager.InitializeWorlds(gameScene);
        }
    }
   
    // [RequireMatchingQueriesForUpdate]
    // public partial class SceneLoaderSystem : SystemBase
    // {
    //     private EntityQuery newRequests;

    //     protected override void OnCreate()
    //     {
    //         newRequests = GetEntityQuery(typeof(SceneLoaderData));
    //     }

    //     protected override void OnUpdate()
    //     {
    //         var requests = newRequests.ToComponentDataArray<SceneLoaderData>(Allocator.Temp);

    //         // Can't use a foreach with a query as SceneSystem.LoadSceneAsync does structural changes
    //         for (int i = 0; i < requests.Length; i += 1)
    //         {
    //             var currentWorld = World;
    //             var loadingStatus = SceneSystem.GetSceneStreamingState(World.Unmanaged, requests[i].LoadingEntity);
    //             Debug.Log("SYSTEM LOADING STATUS:");
    //             Debug.Log(loadingStatus);
    //             if (loadingStatus == SceneSystem.SceneStreamingState.LoadedSuccessfully)
    //             {
    //                 Debug.Log("SCENE IS LOADED");
    //                 //Debug.Log(requests[i].SceneName.ToString());
    //                 //var sceneGUID = SceneSystem.GetSceneGUID(requests[i].LoadingEntity);
    //                 //var scene = SceneManager.GetSceneByName(requests[i].SceneName.ToString());
    //                 //Debug.Log(scene);
    //                 //SceneManager.SetActiveScene(requests[i].LoadingEntity);
    //                 requests.Dispose();

    //                 //EntityManager.DestroyEntity(newRequests);
    //             }
    //             //SceneSystem.LoadSceneAsync(World.Unmanaged, requests[i].SceneReference);
    //         }

    //         
    //         
    //     }
    // }
   
}