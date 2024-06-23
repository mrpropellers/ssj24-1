using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Simulation
{
    public class PlayerInputProvider : IComponentData
    {
        public PlayerInputAdapter Input;
    }
    
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputAdapter : MonoBehaviour 
    {
        PlayerInput _playerInput;
        Camera _mainCamera;
        
        InputAction _move;
        InputAction _throw;

        bool _throwWasPressed;

        void Start()
        {
            _playerInput = GetComponent<PlayerInput>();
            _mainCamera = Camera.main;
            _move = _playerInput.actions.FindAction("Move", true);
            _throw = _playerInput.actions.FindAction("Throw", true);
            _throw.started += (_) => _throwWasPressed = true;
        }

        public Vector2 MoveVector => _move.ReadValue<Vector2>();
        public Quaternion CameraOrientation => _mainCamera.transform.rotation;

        public bool ConsumeThrowInput()
        {
            var wasPressed = _throwWasPressed || _throw.WasPressedThisFrame() || _throw.IsPressed();
            _throwWasPressed = false;
            return wasPressed;
        }
    }
}
