using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace QBitDigital.BunnyKnight
{
    public class ScreenSpaceOutlinesRenderFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class OutlinesSettings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            public Material blitMaterial;
        }

        [SerializeField] private OutlinesSettings settings = new();

        ScreenSpaceOutlinesRenderPass _renderPass;

        public override void Create()
        {
            _renderPass = new ScreenSpaceOutlinesRenderPass(settings, name)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera != Camera.main) return;
            _renderPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            renderer.EnqueuePass(_renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            _renderPass.Dispose();
        }
    }
}
