using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Simulation.Server
{
    // TODO: Add some variance to spawn cooldown?
    public struct RatPickupSpawner : IComponentData 
    {
        public NetworkTick TickLastSpawned;
        public float SpawnRadius;
        public int SpawnCooldownTicks;
        public Entity Simulation;
        public Unity.Mathematics.Random Rand;
    }

    public class RatPickupSpawnerAuthoring : MonoBehaviour
    {
        [FormerlySerializedAs("RatPickupPrefab")]
        [SerializeField]
        GameObject RatPickupSimulation;
        [SerializeField, Min(0.1f)]
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
                    Simulation = GetEntity(authoring.RatPickupSimulation, TransformUsageFlags.Dynamic),
                    SpawnRadius = authoring.SpawnRadius,
                    SpawnCooldownTicks = Mathf.FloorToInt(authoring.SpawnCooldownSeconds * 60),
                    Rand = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(0, int.MaxValue))
                });
            }
        }
    }
}
