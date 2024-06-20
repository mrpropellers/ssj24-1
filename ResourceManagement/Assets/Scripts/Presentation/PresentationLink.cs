using Cinemachine;
using Unity.Entities;
using UnityEngine;

namespace Presentation
{
    public class PresentationLink : IComponentData
    {
        public GameObject Root;
        public CinemachineBrain CinemachineBrain;
        public TransformSetter TransformSetter;
    }
}
