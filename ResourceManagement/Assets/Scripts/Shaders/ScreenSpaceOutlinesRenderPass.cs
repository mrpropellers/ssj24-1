using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QBitDigital.BunnyKnight
{
    public class ScreenSpaceOutlinesRenderPass : ScriptableRenderPass
    {
        private readonly ScreenSpaceOutlinesRenderFeature.OutlinesSettings _settings;
        private readonly ProfilingSampler _profilingSampler;
        private RTHandle _rtTempColor;

        public ScreenSpaceOutlinesRenderPass(ScreenSpaceOutlinesRenderFeature.OutlinesSettings settings, string name)
        {
            _settings = settings;
            _profilingSampler = new ProfilingSampler(name);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Get a temp color RT, configure it for this pass, and then clear afterwards
            RenderTextureDescriptor colorDesc = renderingData.cameraData.cameraTargetDescriptor;
            colorDesc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _rtTempColor, colorDesc, name: "_TemporaryColorTexture");
            ConfigureTarget(_rtTempColor);
            ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 0));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Blit to the temp color RT and back to the camera with the shader specified in _settings
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (_settings.blitMaterial != null)
                {
                    RTHandle camTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                    if (camTarget != null && _rtTempColor != null)
                    {
                        Blitter.BlitCameraTexture(cmd, camTarget, _rtTempColor, _settings.blitMaterial, 0);
                        Blitter.BlitCameraTexture(cmd, _rtTempColor, camTarget);
                    }
                }
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _rtTempColor?.Release();
        }
    }
}
