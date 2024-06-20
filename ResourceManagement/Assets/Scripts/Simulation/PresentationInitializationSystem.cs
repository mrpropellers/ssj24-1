using Presentation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct PresentationInitializationSystem : ISystem
    {
        static void AddPresentationLinks(
            ref EntityCommandBuffer commandBuffer, in Entity entity, GameObject presentation)
        {
            var link = new TransformLink()
            {
                Root = presentation,
                TransformSetter = presentation.GetComponent<TransformSetter>()
            };
            commandBuffer.AddComponent(entity, link);
            if (presentation.TryGetComponent(out Animator animator))
            {
                commandBuffer.AddComponent(entity, new AnimatorLink()
                {
                    Animator = animator
                });
            }
        }
        
        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (playerComponent, playerEntity) in SystemAPI
                         .Query<ThirdPersonPlayer>()
                         .WithNone<PlayerInputProvider>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                var playerPresentation = PresentationInstantiator.CreateCharacterPresentation();
                if (playerPresentation.TryGetComponent<PlayerInputAdapter>(out var inputAdapter))
                {
                    commandBuffer.AddComponent(playerEntity, new PlayerInputProvider() 
                        { Input = inputAdapter });
                }
                else
                {
                    Debug.LogError("Failed to find the InputAdapter on the player presentation");
                }
                AddPresentationLinks(ref commandBuffer, playerComponent.ControlledCharacter, playerPresentation);
                PresentationInstantiator.PlayerCamera.Follow = playerPresentation.transform;
                PresentationInstantiator.PlayerCamera.LookAt = playerPresentation.transform;
            }

            foreach (var (tf, _, __, ratPickupEntity) in SystemAPI
                         .Query<LocalTransform, PickUp, Follower>()
                         .WithNone<TransformLink>()
                         .WithEntityAccess())
            {
                var ratPresentation = PresentationInstantiator.CreateRatPickupPresentation();
                ratPresentation.transform.position = tf.Position;
                AddPresentationLinks(ref commandBuffer, in ratPickupEntity, ratPresentation);
            }
            commandBuffer.Playback(state.EntityManager);
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
