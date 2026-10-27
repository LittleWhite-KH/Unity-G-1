using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP 17 (RenderGraph) fullscreen pass that distorts the finished frame when the player's
/// SAN value drops. Intensity is fed through the global shader float "_SanGlitch".
/// </summary>
public class SanGlitchFeature : ScriptableRendererFeature
{
    public Shader shader;
    public int shaderPass = 0;

    private Material material;
    private SanGlitchPass pass;

    public override void Create()
    {
        if (shader == null) shader = Shader.Find("DSH/SanGlitch");
        if (shader == null) return;

        if (material == null) material = CoreUtils.CreateEngineMaterial(shader);
        pass = new SanGlitchPass(material, shaderPass)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || pass == null) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
    }

    private class SanGlitchPass : ScriptableRenderPass
    {
        private readonly Material material;
        private readonly int shaderPass;

        private class PassData
        {
            public TextureHandle source;
            public Material material;
            public int passIndex;
        }

        public SanGlitchPass(Material material, int shaderPass)
        {
            this.material = material;
            this.shaderPass = shaderPass;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null) return;
            if (Shader.GetGlobalFloat("_SanGlitch") < 0.001f) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType != CameraType.Game) return;
            if (cameraData.camera.cullingMask == 0) return;

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc desc = renderGraph.GetTextureDesc(source);
            desc.name = "SanGlitchTarget";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("SanGlitch", out PassData passData))
            {
                passData.source = source;
                passData.material = material;
                passData.passIndex = shaderPass;
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, data.passIndex);
                });
            }

            resourceData.cameraColor = destination;
        }
    }
}
