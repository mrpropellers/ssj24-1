using NetCode;
using Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Presentation
{
    // WARNING: Accessing Properties of this class is likely not super performant.
    //  Refrain from checking them every frame.
    public static class CurrentGame
    {
        static EntityQuery? ThisPlayerQuery
        {
            get
            {
                if (EntityWorlds.ClientEntityManager == null)
                    return null;
                return EntityWorlds.ClientEntityManager.Value.CreateEntityQuery(
                    typeof(GhostOwnerIsLocal), typeof(ThirdPersonCharacterComponent));
            }
        }

        public static Entity? ThisPlayer
        {
            get
            {
                var players = ThisPlayerQuery?.ToEntityArray(Allocator.Temp);
                if (players == null || players.Value.Length == 0)
                {
                    return null;
                }

                if (players.Value.Length > 1)
                {
                    Debug.LogError("Somehow found more than one local player??? That's wrong.");
                    return null;
                }

                return players.Value[0];
            }
        }
        
        public static int ThisPlayerScore
        {
            get
            {
                var player = ThisPlayer;
                if (player == null)
                    return 0;

                var score = EntityWorlds.ClientEntityManager?.GetComponentData<CharacterScore>(player.Value);
                return score?.Value ?? 0;
            }
        }
    }
}
