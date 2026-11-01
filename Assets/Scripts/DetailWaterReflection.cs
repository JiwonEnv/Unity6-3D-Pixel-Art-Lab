using UnityEngine;

namespace PixelArtLab
{
    [ExecuteAlways,DefaultExecutionOrder(120)]
    public sealed class DetailWaterReflection : MonoBehaviour
    {
        public Camera source;
        public Material waterMaterial;
        public float waterHeight=.3f;
        public bool reflections=true;
        Camera mirror;
        RenderTexture texture;
        bool rendering;
        float lastTime=-10;
        public RenderTexture ReflectionTexture=>texture;
        void LateUpdate(){if(Application.isPlaying&&Time.unscaledTime-lastTime>1f/20)Refresh();}
        public void Refresh()
        {
            if(rendering||!source||!waterMaterial||!reflections)return;
            lastTime=Time.unscaledTime;
            if(!mirror){var g=new GameObject("Detail Water Reflection Camera"){hideFlags=HideFlags.HideAndDontSave};mirror=g.AddComponent<Camera>();mirror.enabled=false;}
            if(!texture){texture=new RenderTexture(640,360,24){name="Woodland Planar Reflection",filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};texture.Create();}
            rendering=true;bool previous=GL.invertCulling;
            try
            {
                mirror.CopyFrom(source);mirror.enabled=false;mirror.targetTexture=texture;mirror.allowMSAA=false;mirror.depthTextureMode=DepthTextureMode.None;
                mirror.cullingMask=source.cullingMask&~(1<<4)&~(1<<13);mirror.useOcclusionCulling=false;
                Matrix4x4 reflection=Matrix4x4.identity;reflection.m11=-1;reflection.m13=waterHeight*2;
                mirror.worldToCameraMatrix=source.worldToCameraMatrix*reflection;
                mirror.transform.position=reflection.MultiplyPoint(source.transform.position);
                Vector3 point=mirror.worldToCameraMatrix.MultiplyPoint(new Vector3(0,waterHeight+.02f,0));
                Vector3 normal=mirror.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
                Vector4 plane=new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(point,normal));
                mirror.projectionMatrix=source.CalculateObliqueMatrix(plane);
                GL.invertCulling=!previous;mirror.Render();waterMaterial.SetTexture("_DetailReflection",texture);
            }
            finally{GL.invertCulling=previous;rendering=false;}
        }
        void OnDisable()
        {
            if(waterMaterial)waterMaterial.SetTexture("_DetailReflection",null);
            if(texture){texture.Release();if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
            if(mirror){if(Application.isPlaying)Destroy(mirror.gameObject);else DestroyImmediate(mirror.gameObject);}
        }
    }
}
