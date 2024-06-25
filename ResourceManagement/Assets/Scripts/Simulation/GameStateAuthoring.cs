using Unity.Collections;
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
    
    public struct GameState : IComponentData
    {
    }

    public class GameStateAuthoring : MonoBehaviour
    {
        public class GameStateBaker : Baker<GameStateAuthoring>
        {
            public override void Bake(GameStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<GameState>(entity);
                AddBuffer<PendingRatScored>(entity);
            }
        }
    }
}
