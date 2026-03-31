using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

public class FurShellRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public Settings settings = new Settings();
    FurShellRenderPass shellPass;

    public override void Create()
    {
        shellPass = new FurShellRenderPass(settings.renderEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (FurShellController.ActiveControllers.Count > 0)
            renderer.EnqueuePass(shellPass);
    }

    class FurShellRenderPass : ScriptableRenderPass
    {
        public FurShellRenderPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
        }

#if UNITY_6000_0_OR_NEWER
        class PassData
        {
            public List<FurShellController> controllers;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fur Shell Pass", out var passData))
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                passData.controllers = new List<FurShellController>(FurShellController.ActiveControllers);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    DrawShells(ctx.cmd, data.controllers);
                });
            }
        }
#endif

#pragma warning disable CS0618
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("FurShellPass");
            DrawShells(cmd, FurShellController.ActiveControllers);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore CS0618

#if UNITY_6000_0_OR_NEWER
        static void DrawShells(CommandBuffer cmd, List<FurShellController> controllers)
        {
            foreach (var ctrl in controllers)
            {
                if (ctrl == null || ctrl.ShellMaterials == null || ctrl.TargetRenderer == null) continue;
                var rend = ctrl.TargetRenderer;
                int subMeshCount = 1;
                if (rend is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                    subMeshCount = smr.sharedMesh.subMeshCount;
                else if (rend is MeshRenderer)
                {
                    var mf = rend.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                        subMeshCount = mf.sharedMesh.subMeshCount;
                }

                foreach (var mat in ctrl.ShellMaterials)
                {
                    for (int sub = 0; sub < subMeshCount; sub++)
                        cmd.DrawRenderer(rend, mat, sub, 0);
                }
            }
        }

        static void DrawShells(RasterCommandBuffer cmd, List<FurShellController> controllers)
        {
#else
        static void DrawShells(CommandBuffer cmd, List<FurShellController> controllers)
        {
#endif
            foreach (var ctrl in controllers)
            {
                if (ctrl == null || ctrl.ShellMaterials == null || ctrl.TargetRenderer == null) continue;
                var rend = ctrl.TargetRenderer;
                int subMeshCount = 1;
                if (rend is SkinnedMeshRenderer smr && smr.sharedMesh != null)
                    subMeshCount = smr.sharedMesh.subMeshCount;
                else if (rend is MeshRenderer)
                {
                    var mf = rend.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                        subMeshCount = mf.sharedMesh.subMeshCount;
                }

                foreach (var mat in ctrl.ShellMaterials)
                {
                    for (int sub = 0; sub < subMeshCount; sub++)
                        cmd.DrawRenderer(rend, mat, sub, 0);
                }
            }
        }
    }
}
