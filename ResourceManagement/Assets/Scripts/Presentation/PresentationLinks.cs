using Cinemachine;
using Unity.Entities;
using UnityEngine;

namespace Presentation
{
    public class TransformLink : ICleanupComponentData 
    {
        public GameObject Root;
        public TransformSetter TransformSetter;
    }

    public class AnimatorLink : IComponentData
    {
        public Animator Animator;
    }
}
