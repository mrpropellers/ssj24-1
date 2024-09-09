using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
    public struct GameSetup : IComponentData
    {
        public Entity Player;
        public Entity CharacterSimulation;
        public Entity RatProjectileSimulation;
    }

    public class GameSetupAuthoring : MonoBehaviour
    {
        [SerializeField]
        GameObject PlayerPrefab;
        [SerializeField]
        GameObject CharacterPrefab;
        [SerializeField]
        GameObject RatProjectilePrefab;
        
        public class GameSetupBaker : Baker<GameSetupAuthoring>
        {
            public override void Bake(GameSetupAuthoring authoring)
            {
                AddComponent(GetEntity(authoring, TransformUsageFlags.None), new GameSetup
                {
                    CharacterSimulation = GetEntity(authoring.CharacterPrefab, TransformUsageFlags.None),
                    Player = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.None),
                    RatProjectileSimulation = GetEntity(authoring.RatProjectilePrefab, TransformUsageFlags.None),
                });
            }
        }
    }
}
