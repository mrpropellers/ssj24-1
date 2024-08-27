using NetCode;
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

        MultiplayerConnectionMenu m_ConnectionMenu;

        InputAction _move;
        InputAction _throw;
        InputAction _menu;

        bool _throwWasPressed;

        void Start()
        {
            _playerInput = GetComponent<PlayerInput>();
            _mainCamera = Camera.main;
            _move = _playerInput.actions.FindAction("Move", true);
            _throw = _playerInput.actions.FindAction("Throw", true);
            _menu = _playerInput.actions.FindAction("Pause", true);
            _throw.started += (_) => _throwWasPressed = true;

            _menu.performed += (_) => goToMenu();
        }

        public Vector2 MoveVector => _move.ReadValue<Vector2>();
        public Quaternion CameraOrientation => _mainCamera.transform.rotation;

        public bool ConsumeThrowInput()
        {
            var wasPressed = _throwWasPressed || _throw.WasPressedThisFrame() || _throw.IsPressed();
            _throwWasPressed = false;
            return wasPressed;
        }
        public void goToMenu()
        {
            if (!ReferenceEquals(null, m_ConnectionMenu))
                m_ConnectionMenu.goToMenu();
        }

    }
}
