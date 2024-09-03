using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Simulation
{
    [GhostComponent]
    public struct Follower : IComponentData
    {
        public float GoalDistance;
        public float Speed;
        public float ProjectileSpeed;
        // TODO | P2 - Tech Debt | Put ProjectileConversionTime in proper prefs object
        //  I'm lazily (and redundantly) plumbing a config value set in the Scene into a static config field
        //  because I don't want to do it the right way. This value should be set once via some kind of 
        //  global "game config" object in seconds
        public static readonly uint ToProjectileTicks = 30;
        public static readonly float ToProjectileDivisor = 1f / ((float)ToProjectileTicks);
        [GhostField]
        public int BufferIndex;
    }

    public class FollowerAuthoring : MonoBehaviour
    {
        public float FollowDistance = 2f;
        public float FollowSpeed = 10f;
        public float ThrowSpeed = 10f;
        public float ToProjectilePeriod = 0.6f;
        
        public class FollowerBaker : Baker<FollowerAuthoring>
        {
            public override void Bake(FollowerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Follower()
                {
                    GoalDistance = authoring.FollowDistance,
                    Speed = authoring.FollowSpeed,
                    ProjectileSpeed = authoring.ThrowSpeed,
                });
                AddComponent(entity, new ConvertToProjectile()
                {
                    ConversionPeriod = Convert.ToUInt32(
                        Mathf.FloorToInt(authoring.ToProjectilePeriod * 60f))
                });

                AddComponent(entity, new ForceInterpolatedGhost());
                SetComponentEnabled<ConvertToProjectile>(entity, false);
            }
        }
    }
}
