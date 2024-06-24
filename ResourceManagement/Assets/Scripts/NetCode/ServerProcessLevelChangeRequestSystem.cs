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
        public EntitySceneReference newLevel;
        public EntitySceneReference oldLevel;
    }


    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class ServerProcessLevelChangeRequestSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<NetworkId>();
        }

        protected override void OnUpdate()
        {
            //var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (levelRequest, rpcEntity) in
            SystemAPI.Query<LoadLevelRpc>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                EntityManager.DestroyEntity(rpcEntity);
                var loadScene = SceneSystem.LoadSceneAsync(World.Unmanaged, levelRequest.newLevel);
                
                //SceneSystem.UnloadScene(World.DefaultGameObjectInjectionWorld.Unmanaged, levelToLoad.oldLevelName.ToString());
            }
        }

        public void LoadNewLevel(World targetWorld, EntitySceneReference newLevelName, EntitySceneReference priorScene)
        {
            var entity = targetWorld.Unmanaged.EntityManager.CreateEntity(typeof(LoadLevelRpc), typeof(SendRpcCommandRequest));
            targetWorld.Unmanaged.EntityManager.SetComponentData(entity, new LoadLevelRpc
            {
                newLevel = newLevelName,
                oldLevel = priorScene
            });
            
        }
        
    }
    
}