using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[Serializable]
[GhostComponent]
public struct ThirdPersonPlayer : IComponentData
{
    [GhostField]
    public Entity ControlledCharacter;
    //public Entity ControlledCamera;
}

[Serializable]
public struct ThirdPersonPlayerInputs : IInputComponentData 
{
    public float2 MoveInput;
    public float2 CameraLookInput;
    public float CameraZoomInput;
    public InputEvent JumpPressed;
    public InputEvent ThrowPressed;
}
