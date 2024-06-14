using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    public struct GameSetup : IComponentData
    {
        public Entity Player;
        public Entity CharacterSimulation;
    }

    public class GameSetupAuthoring : MonoBehaviour
    {
        [SerializeField]
        GameObject PlayerPrefab;
        [SerializeField]
        GameObject CharacterPrefab;
        
        public class GameSetupBaker : Baker<GameSetupAuthoring>
        {
            public override void Bake(GameSetupAuthoring authoring)
            {
                AddComponent(GetEntity(authoring, TransformUsageFlags.None), new GameSetup
                {
                    CharacterSimulation = GetEntity(authoring.CharacterPrefab, TransformUsageFlags.None),
                    Player = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.None),
                    //CameraPrefab = GetEntity(authoring.CameraPrefab, TransformUsageFlags.None),
                });
            }
        }
    }
}
