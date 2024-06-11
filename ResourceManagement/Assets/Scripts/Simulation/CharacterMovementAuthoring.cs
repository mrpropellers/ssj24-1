using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Simulation
{
    public struct CharacterMovement : IInputComponentData 
    {
        public float2 Lateral;
    }

    public class CharacterMovementAuthoring : MonoBehaviour
    {
        public class CharacterMovementBaker : Baker<CharacterMovementAuthoring>
        {
            public override void Bake(CharacterMovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CharacterMovement {});
            }
        }
    }
}
