using NetCode;
using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    public class PlayerSpawnPointAuthoring : MonoBehaviour
    {
        
        public class PlayerSpawnPointBaker : Baker<PlayerSpawnPointAuthoring>
        {
            public override void Bake(PlayerSpawnPointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerSpawnPoint()
                {
                });
            }
        }
    }
}
