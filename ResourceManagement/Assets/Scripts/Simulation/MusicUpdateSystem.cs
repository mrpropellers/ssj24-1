using UnityEngine;
using Unity.Entities;
using Simulation;

namespace Presentation 
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct MusicUpdateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameState>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var gameState = SystemAPI.GetSingleton<GameState>();
            if (gameState.IsGameplayUnderway)
            {
                if (MusicManager.IsPlayingGameMusic)
                    return;
                MusicManager.PlayGameMusic();
            }
            else if (MusicManager.IsPlayingGameMusic)
            {
                MusicManager.PlayMenuMusic();
            }
        }
    }
}