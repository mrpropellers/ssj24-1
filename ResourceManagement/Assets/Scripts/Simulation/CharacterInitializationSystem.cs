using Presentation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
    public partial struct CharacterInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            // TODO: Only add presentation for Clients, not Server
            foreach (var (character, playerEntity) in SystemAPI
                         .Query<Character>()
                         .WithNone<PlayerInputProvider>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                var playerPresentation = CharacterInstantiator.CreateCharacterPresentation();
                if (playerPresentation.TryGetComponent<PlayerInputAdapter>(out var inputAdapter))
                {
                    commandBuffer.AddComponent(playerEntity, new PlayerInputProvider() 
                        { Input = inputAdapter });
                    var link = new PresentationLink() { Root = playerPresentation };
                    commandBuffer.AddComponent(playerEntity, link);
                }
                else
                {
                    Debug.LogError("Failed to find the InputAdapter on the player presentation");
                }
            }
            commandBuffer.Playback(state.EntityManager);
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
