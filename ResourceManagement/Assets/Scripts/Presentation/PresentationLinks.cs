using Cinemachine;
using Unity.Entities;
using UnityEngine;

namespace Presentation
{
    public class TransformLink : IComponentData
    {
        public GameObject Root;
        public TransformSetter TransformSetter;
    }

    public class AnimatorLink : IComponentData
    {
        public Animator Animator;
    }
}
