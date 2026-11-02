using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelArtLab
{
    // Off-screen single-camera render that works under URP (reflections, captures, verification).
    public static class LabCameraRender
    {
        public static void Render(Camera camera)
        {
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=camera.targetTexture};
            if(GraphicsSettings.currentRenderPipeline&&RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);
            else camera.Render();
        }
    }
}
