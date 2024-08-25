using System;
using UnityEngine;
using QBitDigital.BunnyKnight;

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
        // [SerializeField]
        // CinemachineVirtualCamera PlayerVCam;
        [SerializeField]
        CameraController PlayerQBitCam;

        // public static CinemachineVirtualCamera PlayerVirtualCamera => _instance.PlayerVCam;
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
