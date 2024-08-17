using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Presentation
{
    public class AnimationOffset : MonoBehaviour
    {
        [SerializeField] Animator _animator;

        static readonly int k_offset = Animator.StringToHash("AnimationOffset");

        private void Start()
        {
            float randomOffset = UnityEngine.Random.Range(0.1f, 0.9f);
            _animator.SetFloat(k_offset, randomOffset);
        }
    }
}
