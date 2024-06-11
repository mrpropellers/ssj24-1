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

        void Start()
        {
            _playerInput = GetComponent<PlayerInput>();
            _move = _playerInput.actions.FindAction("Move", true);
        }

        public Vector2 MoveVector => _move.ReadValue<Vector2>();
    }
}
