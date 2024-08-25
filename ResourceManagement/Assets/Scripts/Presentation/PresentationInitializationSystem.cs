using Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Presentation 
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct PresentationInitializationSystem : ISystem
    {
        static void AddPresentationLinks(
            ref EntityCommandBuffer commandBuffer, in Entity entity, in LocalTransform tf, GameObject presentation)
        {
            if (!presentation.TryGetComponent<TransformSetter>(out var tfSetter))
            {
                Debug.LogWarning(
                    $"{presentation} has not {nameof(TransformSetter)}, adding one now (you should fix the prefab tho)");
                tfSetter = presentation.AddComponent<TransformSetter>();
            }
            var link = new TransformLink()
            {
                Root = presentation,
                TransformSetter = tfSetter
            };
            commandBuffer.AddComponent(entity, link);
            var animator = presentation.GetComponentInChildren<Animator>();
            if (animator != null)
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
                // PresentationInstantiator.PlayerVirtualCamera.Follow = playerPresentation.transform;
                // PresentationInstantiator.PlayerVirtualCamera.LookAt = playerPresentation.transform;
                PresentationInstantiator.PlayerCameraRig.follow = playerPresentation.transform;
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
            foreach (var (tf, _, ratPickupEntity) in SystemAPI
                         .Query<LocalTransform, Follower>()
                         .WithNone<TransformLink>()
                         .WithEntityAccess())
            {
                var ratPresentation = PresentationInstantiator.CreateRatPickupPresentation();
                //ratPresentation.transform.position = tf.Position;
                AddPresentationLinks(ref commandBuffer, in ratPickupEntity, tf, ratPresentation);
            }
            
            foreach (var (tf, _, projectileEntity) in SystemAPI
                         .Query<LocalTransform, Projectile>()
                         .WithNone<TransformLink>()
                         .WithEntityAccess())
            {
                var presentation = PresentationInstantiator.CreateRatProjectilePresentation();
                //ratPresentation.transform.position = tf.Position;
                AddPresentationLinks(ref commandBuffer, in projectileEntity, tf, presentation);
            }
            
            // Cleanup any destroyed Entity's presentation GameObjects
            foreach (var (link, entity) in SystemAPI
                         .Query<TransformLink>()
                         .WithNone<LocalToWorld>()
                         .WithEntityAccess())
            {
                Object.Destroy(link.Root);
                commandBuffer.RemoveComponent<TransformLink>(entity);
            }
            
            commandBuffer.Playback(state.EntityManager);
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
