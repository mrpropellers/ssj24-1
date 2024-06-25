using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
    [GhostComponent]
    public struct Follower : IComponentData
    {
        public float GoalDistance;
        public float Speed;
        [GhostField]
        public float CurrentSpeed;
        public float ProjectileSpeed;
        public int ToProjectileTicks;
        public int OwnerQueueRank;
    }

    public class FollowerAuthoring : MonoBehaviour
    {
        public float FollowDistance = 2f;
        public float FollowSpeed = 10f;
        public float ThrowSpeed = 10f;
        public float ProjectileConversionTime = 0.2f;
        
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
                    ToProjectileTicks = Mathf.RoundToInt(authoring.ProjectileConversionTime * 60f),
                });
            }
        }
    }
}
