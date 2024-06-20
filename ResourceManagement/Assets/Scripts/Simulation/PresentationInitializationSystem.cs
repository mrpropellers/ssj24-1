using System.Collections.Generic;
using Presentation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Simulation
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct PresentationInitializationSystem : ISystem
    {
        static void AddPresentationLinks(
            ref EntityCommandBuffer commandBuffer, in Entity entity, in LocalTransform tf, GameObject presentation)
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

            var scale = Mathf.Approximately(0f, tf.Scale) ? 1f : tf.Scale; 
            commandBuffer.SetComponent(entity, new LocalTransform()
            {
                Position = tf.Position,
                Rotation = tf.Rotation,
                Scale = scale
            });
            presentation.transform.SetLocalPositionAndRotation(tf.Position, tf.Rotation);
            presentation.transform.localScale = scale * Vector3.one;
        }

        public void OnCreate(ref SystemState state)
        {
            
        }

        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var localPlayer = Entity.Null;
            // Initialize the local player
            foreach (var (tf, playerComponent, playerEntity) in SystemAPI
                         .Query<LocalTransform, ThirdPersonPlayer>()
                         .WithNone<PlayerInputProvider>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                // If we find a local player, keep track of it so we don't accidentally initialize it twice
                localPlayer = playerEntity;
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
                AddPresentationLinks(ref commandBuffer, playerComponent.ControlledCharacter, tf, playerPresentation);
                PresentationInstantiator.PlayerCamera.Follow = playerPresentation.transform;
                PresentationInstantiator.PlayerCamera.LookAt = playerPresentation.transform;
            }

            // Initialize any non-local characters
            foreach (var (tf, _, characterEntity) in SystemAPI
                         .Query<LocalTransform, ThirdPersonCharacterComponent>()
                         .WithNone<TransformLink>()
                         .WithNone<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                if (characterEntity == localPlayer)
                {
                    Debug.LogWarning("Ignoring a character because it's the local player and " +
                        "will probably be initialized this frame.");
                    continue;
                }
                var playerPresentation = PresentationInstantiator.CreateCharacterPresentation();
                AddPresentationLinks(ref commandBuffer, characterEntity, tf, playerPresentation);
            }

            // Initialize any newly spawned rat pickups
            foreach (var (tf, _, __, ratPickupEntity) in SystemAPI
                         .Query<LocalTransform, PickUp, Follower>()
                         .WithNone<TransformLink>()
                         .WithEntityAccess())
            {
                var ratPresentation = PresentationInstantiator.CreateRatPickupPresentation();
                //ratPresentation.transform.position = tf.Position;
                AddPresentationLinks(ref commandBuffer, in ratPickupEntity, tf, ratPresentation);
            }
            
            commandBuffer.Playback(state.EntityManager);
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
