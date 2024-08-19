using NetCode;
using Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.CharacterController;
using Unity.NetCode;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class ThirdPersonPlayerInputsSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkTime>();
        RequireForUpdate(SystemAPI.QueryBuilder().WithAll<PlayerInputProvider, ThirdPersonPlayerInputs, GhostOwnerIsLocal>().Build());
    }

    protected override void OnUpdate()
    {
        //uint tick = SystemAPI.GetSingleton<FixedTickSystem.Singleton>().Tick;
        var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        
        foreach (var (inputProvider, inputs) in SystemAPI
                     .Query<PlayerInputProvider,RefRW<ThirdPersonPlayerInputs>>()
                     .WithAll<GhostOwnerIsLocal>())
        {
            inputs.ValueRW = default;
            inputs.ValueRW.MoveInput = inputProvider.Input.MoveVector;
            inputs.ValueRW.CameraOrientation = inputProvider.Input.CameraOrientation;
            // (8.17.2024) TODO | P0 - NetCode | Separate action Inputs into their own ICommandBuffer
            //  Since action inputs are discrete and should also be sent to other Non-Owner ghosts,
            //  they should be separated into their own CommandBuffer and set up correctly. This will
            //  allow us to re-work the rat Systems so they can locally simulate nearly everything and just
            //  check with the Server values occasionally to ensure their state is correct.
            //  https://docs.unity3d.com/Packages/com.unity.netcode@1.3/api/Unity.NetCode.ICommandData.html
            if (inputProvider.Input.ConsumeThrowInput())
            {
                //Debug.Log("Throw is down.");
                inputs.ValueRW.ThrowPressed.Set();
            }

            // NetworkInputUtilities.AddInputDelta(ref playerInputs.ValueRW.CameraLookInput.x, Input.GetAxis("Mouse X"));
            // NetworkInputUtilities.AddInputDelta(ref playerInputs.ValueRW.CameraLookInput.y, Input.GetAxis("Mouse Y"));
            // NetworkInputUtilities.AddInputDelta(ref playerInputs.ValueRW.CameraZoomInput, -Input.mouseScrollDelta.y);

            // if (Input.GetKeyDown(KeyCode.Space))
            // {
            //     inputs.ValueRW.JumpPressed.Set();
            // }
        }
    }
}

/// <summary>
/// Apply inputs that need to be read at a variable rate
/// </summary>
// [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
// [UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
// [BurstCompile]
// public partial struct ThirdPersonPlayerVariableStepControlSystem : ISystem
// {
//     [BurstCompile]
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<NetworkTime>();
//         state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ThirdPersonPlayer, ThirdPersonPlayerInputs>().Build());
//     }
//     
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         NetworkInputUtilities.GetCurrentAndPreviousTick(SystemAPI.GetSingleton<NetworkTime>(), out NetworkTick currentTick, out NetworkTick previousTick);
//         
//         foreach (var (playerInputsBuffer, player) in SystemAPI.Query<DynamicBuffer<InputBufferData<ThirdPersonPlayerInputs>>, ThirdPersonPlayer>().WithAll<Simulate>())
//         {
//             NetworkInputUtilities.GetCurrentAndPreviousTickInputs(playerInputsBuffer, currentTick, previousTick, out ThirdPersonPlayerInputs currentTickInputs, out ThirdPersonPlayerInputs previousTickInputs);
//             
//             if (SystemAPI.HasComponent<OrbitCameraControl>(player.ControlledCamera))
//             {
//                 OrbitCameraControl cameraControl = SystemAPI.GetComponent<OrbitCameraControl>(player.ControlledCamera);
//                 
//                 cameraControl.FollowedCharacterEntity = player.ControlledCharacter;
//                 cameraControl.LookDegreesDelta.x = NetworkInputUtilities.GetInputDelta(currentTickInputs.CameraLookInput.x, previousTickInputs.CameraLookInput.x);
//                 cameraControl.LookDegreesDelta.y = NetworkInputUtilities.GetInputDelta(currentTickInputs.CameraLookInput.y, previousTickInputs.CameraLookInput.y);
//                 cameraControl.ZoomDelta = NetworkInputUtilities.GetInputDelta(currentTickInputs.CameraZoomInput, previousTickInputs.CameraZoomInput);
//                 
//                 SystemAPI.SetComponent(player.ControlledCamera, cameraControl);
//             }
//         }
//     }
// }

/// <summary>
/// Apply inputs that need to be read at a fixed rate.
/// It is necessary to handle this as part of the fixed step group, in case your framerate is lower than the fixed step rate.
/// </summary>
[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
[BurstCompile]
public partial struct ThirdPersonPlayerFixedStepControlSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<ThirdPersonPlayer, ThirdPersonPlayerInputs>().Build());
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (playerInputs, player) in SystemAPI.Query<ThirdPersonPlayerInputs, ThirdPersonPlayer>().WithAll<Simulate>())
        {
            if (SystemAPI.HasComponent<ThirdPersonCharacterControl>(player.ControlledCharacter))
            {
                ThirdPersonCharacterControl characterControl = SystemAPI.GetComponent<ThirdPersonCharacterControl>(player.ControlledCharacter);

                float3 characterUp = MathUtilities.GetUpFromRotation(SystemAPI.GetComponent<LocalTransform>(player.ControlledCharacter).Rotation);
                
                // Get camera rotation, since our movement is relative to it.
                quaternion cameraRotation = playerInputs.CameraOrientation;
                // if (SystemAPI.HasComponent<OrbitCamera>(player.ControlledCamera))
                // {
                //     // Camera rotation is calculated rather than gotten from transform, because this allows us to 
                //     // reduce the size of the camera ghost state in a netcode prediction context.
                //     // If not using netcode prediction, we could simply get rotation from transform here instead.
                //     OrbitCamera orbitCamera = SystemAPI.GetComponent<OrbitCamera>(player.ControlledCamera);
                //     cameraRotation = OrbitCameraUtilities.CalculateCameraRotation(characterUp, orbitCamera.PlanarForward, orbitCamera.PitchAngle);
                // }
                float3 cameraForwardOnUpPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(MathUtilities.GetForwardFromRotation(cameraRotation), characterUp));
                float3 cameraRight = MathUtilities.GetRightFromRotation(cameraRotation);

                // Move
                characterControl.MoveVector = (playerInputs.MoveInput.y * cameraForwardOnUpPlane) + (playerInputs.MoveInput.x * cameraRight);
                characterControl.MoveVector = MathUtilities.ClampToMaxLength(characterControl.MoveVector, 1f);

                // Jump
                characterControl.Jump = playerInputs.JumpPressed.IsSet;
                var shouldThrow = playerInputs.ThrowPressed.IsSet;
                if (shouldThrow)
                {
                    //Debug.Log("Throw Pressed.");
                }
                characterControl.Throw = shouldThrow;

                SystemAPI.SetComponent(player.ControlledCharacter, characterControl);
            }
        }
    }
}