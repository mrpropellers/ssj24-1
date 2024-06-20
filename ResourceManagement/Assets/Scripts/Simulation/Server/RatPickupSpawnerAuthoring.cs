using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation.Server
{
    // TODO: Add some variance to spawn cooldown?
    public struct RatPickupSpawner : IComponentData
    {
        public NetworkTick TickLastSpawned;
        public float SpawnRadius;
        public int SpawnCooldownTicks;
        public Entity RatPickup;
    }

    public class RatPickupSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField]
        GameObject RatPickupPrefab;
        [SerializeField, Min(1f)]
        float SpawnRadius = 5f;
        [SerializeField, Min(1f)]
        float SpawnCooldownSeconds;
        
        public class RatPickupSpawnerBaker : Baker<RatPickupSpawnerAuthoring>
        {
            public override void Bake(RatPickupSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RatPickupSpawner()
                {
                    TickLastSpawned = NetworkTick.Invalid,
                    RatPickup = GetEntity(authoring.RatPickupPrefab, TransformUsageFlags.None),
                    SpawnRadius = authoring.SpawnRadius,
                    SpawnCooldownTicks = Mathf.FloorToInt(authoring.SpawnCooldownSeconds * 60),
                });
            }
        }
    }
}
