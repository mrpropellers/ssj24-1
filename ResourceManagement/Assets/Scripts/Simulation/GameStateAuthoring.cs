using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation
{
    public enum ScoringReceptacle
    {
        Cauldron,
        Basket
    }
    
    public struct PendingRatScored : IBufferElementData
    {
        public Entity RatEntityScored;
        public ScoringReceptacle Receptacle;
        public int OwnerId;
        public float3 LocationTriggered;
        public float3 ReceptacleCenter;
    }
    
    //[GhostComponent]
    public struct GameState : IComponentData
    {
        //[GhostField]
        public bool IsGameplayUnderway;
    }

    public class GameStateAuthoring : MonoBehaviour
    {
        public class GameStateBaker : Baker<GameStateAuthoring>
        {
            public override void Bake(GameStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GameState()
                {
                });
                AddBuffer<PendingRatScored>(entity);
            }
        }
    }
}
