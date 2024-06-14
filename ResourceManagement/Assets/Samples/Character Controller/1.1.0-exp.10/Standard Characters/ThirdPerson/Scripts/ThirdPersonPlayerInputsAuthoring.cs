using Unity.Entities;
using UnityEngine;

public class ThirdPersonPlayerInputsAuthoring : MonoBehaviour
{
    private class ThirdPersonPlayerInputsAuthoringBaker : Baker<ThirdPersonPlayerInputsAuthoring>
    {
        public override void Bake(ThirdPersonPlayerInputsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ThirdPersonPlayerInputs>(entity);
        }
    }
}

