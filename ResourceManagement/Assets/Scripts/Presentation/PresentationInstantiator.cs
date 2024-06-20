using System;
using Cinemachine;
using UnityEngine;

namespace Presentation 
{
    public class PresentationInstantiator : MonoBehaviour
    {
        static PresentationInstantiator _instance;

        [SerializeField]
        GameObject CharacterPrefab;
        [SerializeField]
        GameObject RatPickupPrefab;
        [SerializeField]
        CinemachineVirtualCamera PlayerVCam;

        public static CinemachineVirtualCamera PlayerCamera => _instance.PlayerVCam;

        void Start()
        {
            _instance = this;
        }

        public static GameObject CreateCharacterPresentation()
        {
            return Instantiate(_instance.CharacterPrefab);
        }
        
        public static GameObject CreateRatPickupPresentation()
        {
            return Instantiate(_instance.RatPickupPrefab);
        }
    }
}
