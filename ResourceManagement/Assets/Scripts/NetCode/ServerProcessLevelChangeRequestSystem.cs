using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine.SceneManagement;
using Unity.Scenes;
using Unity.Entities.Serialization;

namespace NetCode
{
    public struct LoadLevelRpc : IRpcCommand
    {
        // public EntitySceneReference newLevel;
        // public EntitySceneReference oldLevel;
    }


    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class ServerProcessLevelChangeRequestSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<NetworkId>();
            RequireForUpdate<GameplaySceneReference>();
        }

        protected override void OnUpdate()
        {
            var sceneReference = SystemAPI.GetSingleton<GameplaySceneReference>();
            //var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, rpcEntity) in
            SystemAPI.Query<LoadLevelRpc>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                SceneSystem.LoadSceneAsync(World.Unmanaged, sceneReference.Value);
                
                //SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, levelToLoad.oldLevelName.ToString());
                EntityManager.DestroyEntity(rpcEntity);
            }
        }

        // public void LoadNewLevel(World targetWorld, EntitySceneReference newLevelName, EntitySceneReference priorScene)
        // {
        //     var entity = targetWorld.Unmanaged.EntityManager.CreateEntity(typeof(LoadLevelRpc), typeof(SendRpcCommandRequest));
        //     targetWorld.Unmanaged.EntityManager.SetComponentData(entity, new LoadLevelRpc
        //     {
        //         // newLevel = newLevelName,
        //         // oldLevel = priorScene
        //     });
        //     
        // }
        
    }
    
}