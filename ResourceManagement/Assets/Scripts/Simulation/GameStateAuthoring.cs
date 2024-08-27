using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;

namespace Simulation
{
    public enum ScoringReceptacle
    {
        Cauldron,
        Basket
    }
    
    [InternalBufferCapacity(32)]
    public struct PendingRatScored : IBufferElementData
    {
        public Entity RatEntityScored;
        public ScoringReceptacle Receptacle;
        public int OwnerId;
        public float3 LocationTriggered;
        public float3 ReceptacleCenter;
    }

    [InternalBufferCapacity(128)]
    public struct PendingCollision : IBufferElementData
    {
        public Projectile ProjectileState;
        public Entity ProjectileEntity;
        public Entity EntityHit;
        //public CollisionEvent CollisionEvent;
    }
    
    [GhostComponent]
    public struct GameState : IComponentData
    {
        [GhostField]
        public bool IsGameplayUnderway;
    }

    public class GameStateAuthoring : MonoBehaviour
    {
        public class GameStateBaker : Baker<GameStateAuthoring>
        {
            public override void Bake(GameStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new GameState()
                {
                });
                AddBuffer<PendingRatScored>(entity);
                AddBuffer<PendingCollision>(entity);
            }
        }
    }
}
