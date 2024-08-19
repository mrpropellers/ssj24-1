using Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.CharacterController;

[DisallowMultipleComponent]
public class ThirdPersonCharacterAuthoring : MonoBehaviour
{
    public AuthoringKinematicCharacterProperties CharacterProperties = AuthoringKinematicCharacterProperties.GetDefault();
    
    public float RotationSharpness = 25f;
    public float GroundMaxSpeed = 10f;
    public float GroundedMovementSharpness = 15f;
    public float AirAcceleration = 50f;
    public float AirMaxSpeed = 10f;
    public float AirDrag = 0f;
    public float JumpSpeed = 10f;
    public float3 Gravity = math.up() * -30f;
    public float ThrowHeight = 1f;
    public float ThrowCooldownSeconds = 1f;
    public float ThrowVelocity = 10f;
    public bool PreventAirAccelerationAgainstUngroundedHits = true;
    public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling = BasicStepAndSlopeHandlingParameters.GetDefault();

    public class Baker : Baker<ThirdPersonCharacterAuthoring>
    {
        public override void Bake(ThirdPersonCharacterAuthoring authoring)
        {
            KinematicCharacterUtilities.BakeCharacter(this, authoring, authoring.CharacterProperties);

            Entity entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace);

            AddComponent(entity, new ThirdPersonCharacterComponent
            {
                RotationSharpness = authoring.RotationSharpness,
                GroundMaxSpeed = authoring.GroundMaxSpeed,
                GroundedMovementSharpness = authoring.GroundedMovementSharpness,
                AirAcceleration = authoring.AirAcceleration,
                AirMaxSpeed = authoring.AirMaxSpeed,
                AirDrag = authoring.AirDrag,
                JumpSpeed = authoring.JumpSpeed,
                Gravity = authoring.Gravity,
                PreventAirAccelerationAgainstUngroundedHits = authoring.PreventAirAccelerationAgainstUngroundedHits,
                StepAndSlopeHandling = authoring.StepAndSlopeHandling,
            });
            AddComponent(entity, new ThirdPersonCharacterControl());
            AddComponent(entity, new ThrowerConfig()
            {
                InitialRatVelocity = authoring.ThrowVelocity,
                ThrowHeight = authoring.ThrowHeight,
                ThrowCooldown = authoring.ThrowCooldownSeconds 
            });
            AddComponent<FollowerThrower>(entity);
            AddBuffer<ThrowableFollowerElement>(entity);
            AddBuffer<PendingProjectileCollisions>(entity);
            AddComponent(entity, new CharacterScore() { Value = 0 });
            AddComponent(entity, new CharacterState());
            AddComponent(entity, new CharacterState_Auth());

            //AddBuffer<PendingPickUp>(entity);
        }
    }

}
