using System;
using NetCode;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.CharacterController;
using Unity.Collections;
using Unity.NetCode;
using UnityEngine.Serialization;

namespace Simulation
{
    [Serializable]
    public struct ThirdPersonCharacterComponent : IComponentData
    {
        public float RotationSharpness;
        public float GroundMaxSpeed;
        public float GroundedMovementSharpness;
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float AirDrag;
        public float JumpSpeed;
        public float3 Gravity;
        public bool PreventAirAccelerationAgainstUngroundedHits;
        public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;
    }
    
    [InternalBufferCapacity(8)]
    public struct PendingProjectileCollisions : IBufferElementData
    {
        public Projectile Projectile;
        public float TimeCollisionRegistered;
        public float3 ProjectilePosition;
        public float3 CharacterPosition;
    }

    [Serializable]
    public struct FollowerCounts
    {
        public int NumThrownFollowers;
        // TODO: Do we need this any more? Maybe can just rely on buffer now that it's being replicated
        public int NumThrowableFollowers;
        
        public float TimeLastThrowPerformed;
        public float TimeLastFollowerPickedUp;

        public float TimeLastUpdated => math.max(TimeLastThrowPerformed, TimeLastFollowerPickedUp);
    }

    public struct ThrowerConfig : IComponentData
    {
        //public float InitialRatVelocity;
        public float ThrowHeight;
        public float ThrowCooldown;
    }
    
    [GhostComponent]
    public struct FollowerThrower : IComponentData
    {
        [GhostField]
        public FollowerCounts Counts;

        // [GhostField]
        // public FollowerCounts Counts_Auth;
    }

    [Serializable]
    public struct CharacterActionTracking
    {
        public float TimeBankingLastStarted;
        public float TimeBankingLastStopped;
        public float TimeLastHit;
        
        public bool IsBanking => TimeBankingLastStarted > TimeBankingLastStopped + float.Epsilon;
    }

    public struct CharacterState : IComponentData
    {
        public NetworkTick TickLastModified;
        public CharacterActionTracking actionTracking;
    }
    
    [GhostComponent]
    [InternalBufferCapacity(512)]
    public struct ThrowableFollowerElement : IBufferElementData
    {
        [GhostField]
        public Entity Follower;
    }

    [Serializable]
    public struct ThirdPersonCharacterControl : IComponentData
    {
        public float3 MoveVector;
        public bool Jump;
        public bool Throw;
    }

    [GhostComponent]
    public struct CharacterScore : IComponentData
    {
        [GhostField]
        public int Value;
    }
}