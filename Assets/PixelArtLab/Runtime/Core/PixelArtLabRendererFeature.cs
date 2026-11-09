using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace PixelArtLab
{
    // Custom passes on top of URP, each opt-in per camera by component:
    // PlayerVisibility (wall stencil and occluded scan), PixelVolumetrics (light shafts)
    // and PixelLinework (object-ID ink, drawn last so lines stay crisp over the haze).
    public sealed class PixelArtLabRendererFeature : ScriptableRendererFeature
    {
        VisibilityPass visibility;
        LineworkPass linework;
        VolumetricPass volumetrics;
        public override void Create()
        {
            visibility=new VisibilityPass{renderPassEvent=RenderPassEvent.AfterRenderingTransparents,requiresIntermediateTexture=true};
            volumetrics=new VolumetricPass{renderPassEvent=RenderPassEvent.AfterRenderingTransparents+1,requiresIntermediateTexture=true};
            volumetrics.ConfigureInput(ScriptableRenderPassInput.Depth);
            linework=new LineworkPass{renderPassEvent=RenderPassEvent.BeforeRenderingPostProcessing,requiresIntermediateTexture=true};
        }
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData)
        {
            var camera=renderingData.cameraData.camera;
            if(renderingData.cameraData.cameraType!=CameraType.Game)return;
            // Local atmosphere volumes for this camera: height fog zone now, light shafts in their pass.
            var zone=PixelAtmosphere.ForCamera(camera);PixelAtmosphere.PushHeightFogZone(zone);
            var player=camera.GetComponent<PlayerVisibility>();
            if(player&&player.isActiveAndEnabled&&player.draws.Count>0){visibility.source=player;renderer.EnqueuePass(visibility);}
            var air=camera.GetComponent<PixelVolumetrics>();
            if(air&&air.isActiveAndEnabled){volumetrics.source=air;volumetrics.zone=zone;renderer.EnqueuePass(volumetrics);}
            var ink=camera.GetComponent<PixelLinework>();
            if(ink&&ink.isActiveAndEnabled&&ink.linework){linework.source=ink;renderer.EnqueuePass(linework);}
        }

        sealed class VolumetricPass : ScriptableRenderPass
        {
            public PixelVolumetrics source;
            public PixelAtmosphere zone;
            static readonly int volumeId=Shader.PropertyToID("_LabVolumetricTexture");
            sealed class MarchData {public Material material;}
            sealed class CompositeData {public TextureHandle color,volume;public Material material;}
            public override void RecordRenderGraph(RenderGraph renderGraph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();
                if(resources.isActiveTargetBackBuffer||!resources.cameraDepthTexture.IsValid())return;
                var colorDesc=renderGraph.GetTextureDesc(resources.activeColorTexture);
                var material=source.Prepare(colorDesc.width,colorDesc.height,zone);if(!material)return;
                // Pass 0: march the haze into an HDR buffer the size of the pixel buffer.
                var volume=UniversalRenderer.CreateRenderGraphTexture(renderGraph,new RenderTextureDescriptor(colorDesc.width,colorDesc.height,GraphicsFormat.R16G16B16A16_SFloat,GraphicsFormat.None),"Pixel Volumetrics",false);
                using(var builder=renderGraph.AddRasterRenderPass<MarchData>("Pixel volumetric march",out var data))
                {
                    data.material=material;
                    builder.UseTexture(resources.cameraDepthTexture);
                    builder.UseAllGlobalTextures(true); // main light shadow map, camera depth
                    builder.SetRenderAttachment(volume,0);
                    builder.SetRenderFunc((MarchData d,RasterGraphContext context)=>Blitter.BlitTexture(context.cmd,new Vector4(1,1,0,0),d.material,0));
                }
                // Pass 1: blur, band and add to the scene colour.
                colorDesc.name="Pixel Volumetrics Composite";colorDesc.clearBuffer=false;
                var lit=renderGraph.CreateTexture(colorDesc);
                using(var builder=renderGraph.AddRasterRenderPass<CompositeData>("Pixel volumetric composite",out var data))
                {
                    data.color=resources.activeColorTexture;data.volume=volume;data.material=material;
                    builder.UseTexture(data.color);builder.UseTexture(volume);builder.SetRenderAttachment(lit,0);
                    builder.SetRenderFunc((CompositeData d,RasterGraphContext context)=>
                    {
                        d.material.SetTexture(volumeId,(RTHandle)d.volume);
                        Blitter.BlitTexture(context.cmd,d.color,new Vector4(1,1,0,0),d.material,1);
                    });
                }
                resources.cameraColor=lit;
            }
        }

        sealed class VisibilityPass : ScriptableRenderPass
        {
            public PlayerVisibility source;
            sealed class PassData {public List<PlayerVisibility.Draw> draws;}
            public override void RecordRenderGraph(RenderGraph renderGraph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();
                using(var builder=renderGraph.AddRasterRenderPass<PassData>("Player visibility stencil and scan",out var data))
                {
                    data.draws=source.draws;
                    builder.SetRenderAttachment(resources.activeColorTexture,0);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture,AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((PassData d,RasterGraphContext context)=>
                    {
                        foreach(var draw in d.draws)if(draw.renderer)context.cmd.DrawRenderer(draw.renderer,draw.material,draw.submesh,draw.pass);
                    });
                }
            }
        }

        sealed class LineworkPass : ScriptableRenderPass
        {
            public PixelLinework source;
            static readonly List<ShaderTagId> tags=new List<ShaderTagId>{new ShaderTagId("UniversalForward"),new ShaderTagId("UniversalForwardOnly"),new ShaderTagId("SRPDefaultUnlit")};
            static readonly int identityId=Shader.PropertyToID("_ObjectIdentityTexture"),geometryId=Shader.PropertyToID("_LabSceneDepthNormals");
            sealed class MetadataData {public RendererListHandle list;public Color clear;}
            sealed class InkData {public TextureHandle color,ids,geometry;public Material material;}
            public override void RecordRenderGraph(RenderGraph renderGraph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();
                var cameraData=frameData.Get<UniversalCameraData>();
                if(resources.isActiveTargetBackBuffer)return;
                var colorDesc=renderGraph.GetTextureDesc(resources.activeColorTexture);
                if(!source.Prepare(colorDesc.width,colorDesc.height,cameraData.camera.farClipPlane))return;
                var renderingData=frameData.Get<UniversalRenderingData>();
                var lightData=frameData.Get<UniversalLightData>();
                TextureHandle ids=renderGraph.ImportTexture(source.IdentityHandle),geometry=renderGraph.ImportTexture(source.GeometryHandle);
                var depthDesc=new RenderTextureDescriptor(colorDesc.width,colorDesc.height,GraphicsFormat.None,SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil)){msaaSamples=1};
                // Pass 0: object ID + line policy. Pass 1: depth/normal of the wind-deformed world.
                RenderMetadata(renderGraph,cameraData,renderingData,lightData,ids,UniversalRenderer.CreateRenderGraphTexture(renderGraph,depthDesc,"Object ID Depth",true),0,Color.clear);
                RenderMetadata(renderGraph,cameraData,renderingData,lightData,geometry,UniversalRenderer.CreateRenderGraphTexture(renderGraph,depthDesc,"Animated Geometry Depth",true),1,new Color(.5f,.5f,1,1));

                colorDesc.name="Pixel Linework";colorDesc.clearBuffer=false;
                TextureHandle inked=renderGraph.CreateTexture(colorDesc);
                using(var builder=renderGraph.AddRasterRenderPass<InkData>("Pixel linework",out var data))
                {
                    data.color=resources.activeColorTexture;data.ids=ids;data.geometry=geometry;data.material=source.LineMaterial;
                    builder.UseTexture(data.color);builder.UseTexture(ids);builder.UseTexture(geometry);
                    builder.SetRenderAttachment(inked,0);
                    builder.SetRenderFunc((InkData d,RasterGraphContext context)=>
                    {
                        d.material.SetTexture(identityId,(RTHandle)d.ids);d.material.SetTexture(geometryId,(RTHandle)d.geometry);
                        Blitter.BlitTexture(context.cmd,d.color,new Vector4(1,1,0,0),d.material,0);
                    });
                }
                resources.cameraColor=inked;
            }
            void RenderMetadata(RenderGraph renderGraph,UniversalCameraData cameraData,UniversalRenderingData renderingData,UniversalLightData lightData,TextureHandle target,TextureHandle depth,int pass,Color clear)
            {
                using(var builder=renderGraph.AddRasterRenderPass<MetadataData>(pass==0?"Object identity":"Animated depth normals",out var data))
                {
                    var drawing=RenderingUtils.CreateDrawingSettings(tags,renderingData,cameraData,lightData,SortingCriteria.CommonOpaque);
                    drawing.overrideShader=source.identityShader;drawing.overrideShaderPassIndex=pass;
                    var filtering=new FilteringSettings(RenderQueueRange.opaque,cameraData.camera.cullingMask);
                    data.list=renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults,drawing,filtering));data.clear=clear;
                    builder.UseRendererList(data.list);
                    builder.SetRenderAttachment(target,0);builder.SetRenderAttachmentDepth(depth);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((MetadataData d,RasterGraphContext context)=>
                    {
                        context.cmd.ClearRenderTarget(true,true,d.clear);
                        context.cmd.DrawRendererList(d.list);
                    });
                }
            }
        }
    }
}
