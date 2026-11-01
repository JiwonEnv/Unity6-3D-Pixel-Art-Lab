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
        RenderTexture sceneDepthNormals;
        public RenderTexture IdentityBuffer=>identities;
        public RenderTexture AnimatedGeometryBuffer=>sceneDepthNormals;
        void OnEnable() { GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals; }
        void OnRenderImage(RenderTexture src,RenderTexture dst)
        {
            if(!linework || !lineShader || !lineShader.isSupported) { Graphics.Blit(src,dst); return; }
            if(!material) material=new Material(lineShader) { hideFlags=HideFlags.HideAndDontSave };
            bool metadata=identityShader&&identityShader.isSupported;
            bool useIds=objectBoundaries && metadata;
            if(metadata)
            {
                if(!identities || identities.width!=src.width || identities.height!=src.height)
                {
                    ReleaseIdentityTexture();
                    identities=new RenderTexture(src.width,src.height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                        {name="Object ID + Line Policy",filterMode=FilterMode.Point,antiAliasing=1,hideFlags=HideFlags.HideAndDontSave};
                    identities.Create();
                    sceneDepthNormals=new RenderTexture(src.width,src.height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                        {name="Animated World Depth Normals",filterMode=FilterMode.Point,antiAliasing=1,hideFlags=HideFlags.HideAndDontSave};
                    sceneDepthNormals.Create();
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
                Shader.SetGlobalFloat("_LabRenderDepthNormals",0);
                identityCamera.RenderWithShader(identityShader,"RenderType");
                material.SetTexture("_ObjectIdentityTexture",identities);
                identityCamera.targetTexture=sceneDepthNormals;identityCamera.backgroundColor=new Color(.5f,.5f,1,1);
                try{Shader.SetGlobalFloat("_LabRenderDepthNormals",1);identityCamera.RenderWithShader(identityShader,"RenderType");}
                finally{Shader.SetGlobalFloat("_LabRenderDepthNormals",0);}
                material.SetTexture("_LabSceneDepthNormals",sceneDepthNormals);
            }
            material.SetFloat("_HasSceneDepthNormals",metadata?1:0);
            material.SetFloat("_UseObjectIds",useIds?1:0);
            material.SetFloat("_DebugObjectIds",debugIdentities && useIds?1:0);
            material.SetFloat("_DepthThreshold",depthThreshold);
            material.SetFloat("_NormalThreshold",normalThreshold);
            material.SetFloat("_LineStrength",strength);
            Graphics.Blit(src,dst,material);
        }
        void ReleaseIdentityTexture()
        {
            if(identities){identities.Release();if(Application.isPlaying)Destroy(identities);else DestroyImmediate(identities);identities=null;}
            if(sceneDepthNormals){sceneDepthNormals.Release();if(Application.isPlaying)Destroy(sceneDepthNormals);else DestroyImmediate(sceneDepthNormals);sceneDepthNormals=null;}
        }
        void OnDisable()
        {
            ReleaseIdentityTexture();
            if(identityCamera){if(Application.isPlaying)Destroy(identityCamera.gameObject);else DestroyImmediate(identityCamera.gameObject);}
            if(material) { if(Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
