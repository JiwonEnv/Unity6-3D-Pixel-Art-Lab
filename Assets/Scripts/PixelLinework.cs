using UnityEngine;

namespace PixelArtLab
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public class PixelLinework : MonoBehaviour
    {
        public Shader lineShader;
        public Shader identityShader;
        public bool objectBoundaries=true;
        public bool debugIdentities;
        public bool linework = true;
        [Range(0,1)] public float strength=.78f;
        public float depthThreshold=.16f;
        [Range(0,1)] public float normalThreshold=.32f;
        Material material;
        Camera identityCamera;
        RenderTexture identities;
        void OnEnable() { GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals; }
        void OnRenderImage(RenderTexture src,RenderTexture dst)
        {
            if(!linework || !lineShader || !lineShader.isSupported) { Graphics.Blit(src,dst); return; }
            if(!material) material=new Material(lineShader) { hideFlags=HideFlags.HideAndDontSave };
            bool useIds=objectBoundaries && identityShader && identityShader.isSupported;
            if(useIds)
            {
                if(!identities || identities.width!=src.width || identities.height!=src.height)
                {
                    ReleaseIdentityTexture();
                    identities=new RenderTexture(src.width,src.height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                        {name="Object ID + Line Policy",filterMode=FilterMode.Point,antiAliasing=1,hideFlags=HideFlags.HideAndDontSave};
                    identities.Create();
                }
                if(!identityCamera)
                {
                    var g=new GameObject("Object Identity Camera"){hideFlags=HideFlags.HideAndDontSave};
                    identityCamera=g.AddComponent<Camera>();identityCamera.enabled=false;
                }
                var camera=GetComponent<Camera>();identityCamera.CopyFrom(camera);
                identityCamera.transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);
                identityCamera.enabled=false;identityCamera.depthTextureMode=DepthTextureMode.None;
                identityCamera.clearFlags=CameraClearFlags.SolidColor;identityCamera.backgroundColor=Color.clear;
                identityCamera.allowHDR=false;identityCamera.allowMSAA=false;identityCamera.targetTexture=identities;
                identityCamera.RenderWithShader(identityShader,"RenderType");
                material.SetTexture("_ObjectIdentityTexture",identities);
            }
            material.SetFloat("_UseObjectIds",useIds?1:0);
            material.SetFloat("_DebugObjectIds",debugIdentities && useIds?1:0);
            material.SetFloat("_DepthThreshold",depthThreshold);
            material.SetFloat("_NormalThreshold",normalThreshold);
            material.SetFloat("_LineStrength",strength);
            Graphics.Blit(src,dst,material);
        }
        void ReleaseIdentityTexture(){if(identities){identities.Release();if(Application.isPlaying)Destroy(identities);else DestroyImmediate(identities);identities=null;}}
        void OnDisable()
        {
            ReleaseIdentityTexture();
            if(identityCamera){if(Application.isPlaying)Destroy(identityCamera.gameObject);else DestroyImmediate(identityCamera.gameObject);}
            if(material) { if(Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
