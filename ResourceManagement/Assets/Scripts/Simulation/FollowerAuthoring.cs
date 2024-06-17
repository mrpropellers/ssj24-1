using Unity.Entities;
using UnityEngine;

namespace Simulation
{
    public struct Follower : IComponentData
    {
        public float GoalDistance;
        public float Speed;
    }

    public class FollowerAuthoring : MonoBehaviour
    {
        public float FollowDistance = 2f;
        public float FollowSpeed = 10f;
        
        public class FollowerBaker : Baker<FollowerAuthoring>
        {
            public override void Bake(FollowerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Follower()
                {
                    GoalDistance = authoring.FollowDistance,
                    Speed = authoring.FollowSpeed
                });
            }
        }
    }
}
