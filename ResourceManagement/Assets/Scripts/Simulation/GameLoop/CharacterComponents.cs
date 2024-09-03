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
    }

    public struct ThrowerConfig : IComponentData
    {
        //public float InitialRatVelocity;
        public float ThrowHeight;
        public uint ThrowCooldownTicks;
    }
    
    [GhostComponent]
    public struct FollowerThrower : IComponentData
    {
        [GhostField]
        public int NumThrownFollowers;

        [GhostField]
        public NetworkTick TickNextThrowAllowed;
        
    }

    // [Serializable]
    // public struct CharacterActionTracking
    // {
    //     public float TimeBankingLastStarted;
    //     public float TimeBankingLastStopped;
    //     public float TimeLastHit;
    //     
    //     public bool IsBanking => TimeBankingLastStarted > TimeBankingLastStopped + float.Epsilon;
    // }

    // public struct CharacterState : IComponentData
    // {
    //     public NetworkTick TickLastModified;
    //     public CharacterActionTracking actionTracking;
    // }
    
    [GhostComponent]
    [InternalBufferCapacity(256)]
    public struct ThrowableFollowerElement : IBufferElementData
    {
        [GhostField]
        public Entity Follower;
    }

    [Serializable, GhostComponent]
    public struct ThirdPersonCharacterControl : IComponentData
    {
        [GhostField]
        public float3 MoveVector;
        public bool Jump;
        [GhostField]
        public bool Throw;
    }

    [GhostComponent]
    public struct CharacterScore : IComponentData
    {
        [GhostField]
        public int Value;
    }
}