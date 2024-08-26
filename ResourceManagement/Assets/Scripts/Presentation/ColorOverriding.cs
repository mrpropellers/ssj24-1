using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Presentation
{
    public struct ColorOverride : IComponentData, IEnableableComponent
    {
        public Color Value;
    }
    
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct PredictionDebugColorSystem : ISystem
    {
        static readonly int k_ColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color k_PredictedColor = Color.green;
        static readonly Color k_InterpolatedColor = Color.yellow;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (colorOverride, entity) in SystemAPI
                         .Query<RefRW<ColorOverride>>()
                         .WithAll<PredictedGhost>().WithEntityAccess())
            {
                colorOverride.ValueRW.Value = k_PredictedColor;
                state.EntityManager.SetComponentEnabled<ColorOverride>(entity, true);
            }
            
            foreach (var (colorOverride, entity) in SystemAPI
                         .Query<RefRW<ColorOverride>>()
                         .WithNone<PredictedGhost>().WithEntityAccess())
            {
                colorOverride.ValueRW.Value = k_InterpolatedColor;
                state.EntityManager.SetComponentEnabled<ColorOverride>(entity, true);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
    
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct ColorOverriding : ISystem
    {
        static readonly int k_ColorId = Shader.PropertyToID("_BaseColor");
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (colorOverride, rendererLink) in SystemAPI
                         .Query<RefRO<ColorOverride>, RendererLink>()
                         .WithAll<ColorOverride>())
            {
                var mbp = new MaterialPropertyBlock();
                mbp.SetColor(k_ColorId, colorOverride.ValueRO.Value);
                rendererLink.Renderer.SetPropertyBlock(mbp);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
