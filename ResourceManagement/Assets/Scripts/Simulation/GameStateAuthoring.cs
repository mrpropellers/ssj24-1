using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public struct PendingRatScored : IBufferElementData
    {
        public Entity RatEntityScored;
        public int OwnerId;
        public float3 LocationTriggered;
        public float3 CauldronSplashCenter;
    }
    
    //[GhostComponent]
    public struct GameState : IComponentData
    {
        //[GhostField]
        public bool IsGameplayUnderway;
    }

    public class GameStateAuthoring : MonoBehaviour
    {
        public bool ForceUnderway;
        
        public class GameStateBaker : Baker<GameStateAuthoring>
        {
            public override void Bake(GameStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GameState()
                {
                    #if UNITY_EDITOR
                    IsGameplayUnderway = authoring.ForceUnderway
                    #endif
                });
                AddBuffer<PendingRatScored>(entity);
            }
        }
    }
}
