using Unity.Entities;
using UnityEngine;

namespace Simulation.Authoring
{
    public class DeathZone : MonoBehaviour
    {
        class DeathZoneBaker : Baker<DeathZone>
        {
            public override void Bake(DeathZone authoring)
            {
                AddComponent(GetEntity(authoring, TransformUsageFlags.WorldSpace), 
                    new Server.DeathZone{ });
            }
            
        }
    }
}

