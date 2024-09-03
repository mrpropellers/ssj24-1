using Simulation;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Presentation
{
    public struct ColorOverride : IComponentData, IEnableableComponent
    {
        public Color Value;
        public bool WasApplied;
    }
    
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct PredictionDebugColorSystem : ISystem
    {
        static readonly int k_ColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color k_PredictedColor = Color.green;
        static readonly Color k_InterpolatedColor = Color.yellow;
        static readonly Color k_BouncedColor = Color.cyan;

        ComponentLookup<Projectile> m_ProjectileLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ProjectileLookup = state.GetComponentLookup<Projectile>(true);
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_ProjectileLookup.Update(ref state);
            if (!Debug.isDebugBuild)
                return;
            
            foreach (var (colorOverride, entity) in SystemAPI
                         .Query<RefRW<ColorOverride>>()
                         .WithAll<PredictedGhost>()
                         .WithEntityAccess())
            {
                if (m_ProjectileLookup.TryGetComponent(entity, out var projectile)
                    && projectile.HasBounced
                    && colorOverride.ValueRO.Value != k_BouncedColor)
                {
                    colorOverride.ValueRW.Value = k_BouncedColor;
                    colorOverride.ValueRW.WasApplied = false;
                }
                else if (colorOverride.ValueRO.Value != k_PredictedColor)
                {
                    colorOverride.ValueRW.Value = k_PredictedColor;
                    colorOverride.ValueRW.WasApplied = false;
                }
            }
            
            foreach (var (colorOverride, entity) in SystemAPI
                         .Query<RefRW<ColorOverride>>()
                         .WithNone<PredictedGhost>().WithEntityAccess())
            {
                if (colorOverride.ValueRO.Value == k_InterpolatedColor)
                    continue;
                
                colorOverride.ValueRW.Value = k_InterpolatedColor;
                colorOverride.ValueRW.WasApplied = false;
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
            if (!Debug.isDebugBuild)
                return;
            
            foreach (var (colorOverride, rendererLink) in SystemAPI
                         .Query<RefRW<ColorOverride>, RendererLink>()
                         .WithAll<ColorOverride>())
            {
                if (colorOverride.ValueRO.WasApplied)
                    continue;
                var mbp = new MaterialPropertyBlock();
                mbp.SetColor(k_ColorId, colorOverride.ValueRO.Value);
                rendererLink.Renderer.SetPropertyBlock(mbp);
                colorOverride.ValueRW.WasApplied = true;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
    }
}
