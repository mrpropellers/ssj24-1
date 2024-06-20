using System;
using Cinemachine;
using UnityEngine;

namespace Presentation 
{
    public class CharacterInstantiator : MonoBehaviour
    {
        static CharacterInstantiator _instance;

        [SerializeField]
        GameObject CharacterPrefab;
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
    }
}
