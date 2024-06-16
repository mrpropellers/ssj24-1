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
        
        InputAction _move;
        InputAction _throw;

        void Start()
        {
            _playerInput = GetComponent<PlayerInput>();
            _move = _playerInput.actions.FindAction("Move", true);
            _throw = _playerInput.actions.FindAction("Throw", true);
        }

        public Vector2 MoveVector => _move.ReadValue<Vector2>();
        public bool ThrowIsDown => _throw.IsPressed();
    }
}
