using System;
using Cinemachine;
using UnityEngine;
using QBitDigital.BunnyKnight;
using UnityEngine.Serialization;

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
        GameObject RatProjectilePrefab;
        // (currently not using this but we set it up anyway)
        [SerializeField]
        CinemachineVirtualCamera PlayerVCam;
        [SerializeField]
        CameraController PlayerQBitCam;

        public static CinemachineVirtualCamera PlayerVirtualCamera => _instance.PlayerVCam;
        public static CameraController PlayerCameraRig => _instance.PlayerQBitCam;

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
        
        public static GameObject CreateRatProjectilePresentation()
        {
            return Instantiate(_instance.RatProjectilePrefab);
        }
    }
}
