using UnityEngine;

namespace PixelArtLab
{
    [ExecuteAlways,DefaultExecutionOrder(120)]
    public sealed class DetailWaterReflection : MonoBehaviour
    {
        public Camera source;
        public Material waterMaterial;
        public Material[] additionalMaterials;
        public string textureProperty="_DetailReflection";
        public float waterHeight=.3f;
        public bool reflections=true;
        [HideInInspector]public Transform groundAnchor; // Legacy serialized field; no longer drives height.
        public Renderer[] groundReceivers;
        Camera mirror;
        RenderTexture texture;
        bool rendering;
        float lastTime=-10;
        public RenderTexture ReflectionTexture=>texture;
        void LateUpdate(){if(Application.isPlaying&&Time.unscaledTime-lastTime>1f/20)Refresh();}
        public void Refresh()
        {
            if(rendering||!source||!waterMaterial||!reflections)return;
            if(Shader.GetGlobalFloat("_LabReflectionDisabled")>.5)return;
            if(textureProperty=="_PuddleReflection"&&Shader.GetGlobalVector("_LabEnvironment").z<.22f)return;
            // A stable reference plane plus per-fragment world height works across the
            // orthographic landscape. Never move the mirror plane with the player.
            lastTime=Time.unscaledTime;
            if(!mirror){var g=new GameObject("Detail Water Reflection Camera"){hideFlags=HideFlags.HideAndDontSave};mirror=g.AddComponent<Camera>();mirror.enabled=false;}
            bool puddle=textureProperty=="_PuddleReflection";
            float bottom=-source.orthographicSize,top=source.orthographicSize;
            if(puddle&&source.orthographic)
            {
                float low=0,high=8;
                if(groundReceivers!=null&&groundReceivers.Length>0){low=float.PositiveInfinity;high=float.NegativeInfinity;foreach(var r in groundReceivers)if(r){low=Mathf.Min(low,r.bounds.min.y);high=Mathf.Max(high,r.bounds.max.y);}}
                // Include the full terrain-height envelope, without reducing flower pixel size.
                float vertical=Mathf.Abs(source.transform.up.y);
                bottom+=Mathf.Min(0,2*(waterHeight-high)*vertical)-.1f;
                top+=Mathf.Max(0,2*(waterHeight-low)*vertical)+.1f;
            }
            int height=source.targetTexture?Mathf.Clamp(source.targetTexture.height,360,720):360;
            int width=source.targetTexture?Mathf.Clamp(source.targetTexture.width,640,1536):Mathf.Clamp(Mathf.RoundToInt(height*source.aspect),640,1536);
            if(puddle)height=Mathf.Clamp(Mathf.CeilToInt(height*(top-bottom)/(source.orthographicSize*2)),360,2048);
            if(texture&&(texture.width!=width||texture.height!=height)){texture.Release();if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);texture=null;}
            if(!texture){texture=new RenderTexture(width,height,24){name="Woodland Planar Reflection",filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};texture.Create();}
            rendering=true;bool previous=GL.invertCulling;float previousPass=Shader.GetGlobalFloat("_LabReflectionPass");
            bool[] receiverStates=groundReceivers==null?null:new bool[groundReceivers.Length];
            try
            {
                mirror.CopyFrom(source);mirror.enabled=false;mirror.targetTexture=texture;mirror.allowMSAA=false;mirror.depthTextureMode=DepthTextureMode.None;
                mirror.cullingMask=source.cullingMask&~(1<<LabLayers.Water)&~(1<<LabLayers.Effects);mirror.useOcclusionCulling=false;
                Matrix4x4 reflection=Matrix4x4.identity;reflection.m11=-1;reflection.m13=waterHeight*2;
                mirror.worldToCameraMatrix=source.worldToCameraMatrix*reflection;
                mirror.transform.position=reflection.MultiplyPoint(source.transform.position);
                Vector3 point=mirror.worldToCameraMatrix.MultiplyPoint(new Vector3(0,waterHeight+.02f,0));
                Vector3 normal=mirror.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
                Vector4 plane=new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(point,normal));
                float halfWidth=source.orthographicSize*source.aspect;
                mirror.projectionMatrix=puddle?Matrix4x4.Ortho(-halfWidth,halfWidth,bottom,top,source.nearClipPlane,source.farClipPlane):source.CalculateObliqueMatrix(plane);
                // No global height clip for puddles: it deleted low flowers on lower hills.
                // Hide only the receiving terrain to prevent it blocking the below-ground view.
                if(puddle&&groundReceivers!=null)for(int i=0;i<groundReceivers.Length;i++)if(groundReceivers[i]){receiverStates[i]=groundReceivers[i].forceRenderingOff;groundReceivers[i].forceRenderingOff=true;}
                GL.invertCulling=!previous;Shader.SetGlobalFloat("_LabReflectionPass",1);LabCameraRender.Render(mirror);waterMaterial.SetTexture(textureProperty,texture);
                if(additionalMaterials!=null)foreach(var material in additionalMaterials)if(material)material.SetTexture(textureProperty,texture);
                // Texture UVs use bottom-left viewport space; render-into-texture GPU projection
                // flips Y on D3D and would sample the opposite side of this reflection.
                var projection=GL.GetGPUProjectionMatrix(mirror.projectionMatrix,false)*mirror.worldToCameraMatrix;
                // Globals keep Sprite Toon's per-material buffer SRP Batcher compatible.
                if(puddle){Shader.SetGlobalMatrix("_PuddleProjection",projection);Shader.SetGlobalFloat("_PuddleCaptureHeight",waterHeight);}
                else{Shader.SetGlobalMatrix("_DetailProjection",projection);Shader.SetGlobalFloat("_DetailCaptureHeight",waterHeight);}
            }
            finally{if(textureProperty=="_PuddleReflection"&&groundReceivers!=null)for(int i=0;i<groundReceivers.Length;i++)if(groundReceivers[i])groundReceivers[i].forceRenderingOff=receiverStates[i];GL.invertCulling=previous;Shader.SetGlobalFloat("_LabReflectionPass",previousPass);rendering=false;}
        }
        void OnDisable()
        {
            if(waterMaterial)waterMaterial.SetTexture(textureProperty,null);
            if(additionalMaterials!=null)foreach(var material in additionalMaterials)if(material)material.SetTexture(textureProperty,null);
            if(texture){texture.Release();if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
            if(mirror){if(Application.isPlaying)Destroy(mirror.gameObject);else DestroyImmediate(mirror.gameObject);}
        }
    }
}
