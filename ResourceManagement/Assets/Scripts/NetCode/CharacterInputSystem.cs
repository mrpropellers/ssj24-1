using Simulation;
using Unity.Entities;
using Unity.NetCode;

namespace NetCode 
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class CharacterInputSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (inputProvider, characterMovement) in SystemAPI
                         .Query<PlayerInputProvider, RefRW<CharacterMovement>>()
                         .WithAll<GhostOwnerIsLocal>())
            {
                characterMovement.ValueRW = default; 
                characterMovement.ValueRW.Lateral = inputProvider.Input.MoveVector;
            }
        }
    }
}
