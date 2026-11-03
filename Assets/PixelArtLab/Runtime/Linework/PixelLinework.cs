using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab
{
    // Settings and persistent buffers for the object-aware ink. The URP passes live in
    // PixelArtLabRendererFeature, which runs only on cameras that carry this component.
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
        RenderTexture identities;
        RenderTexture sceneDepthNormals;
        RTHandle identityHandle,geometryHandle;
        public RenderTexture IdentityBuffer=>identities;
        public RenderTexture AnimatedGeometryBuffer=>sceneDepthNormals;
        internal Material LineMaterial=>material;
        internal RTHandle IdentityHandle=>identityHandle;
        internal RTHandle GeometryHandle=>geometryHandle;
        // Identity and depth/normal buffers stay persistent so verification can read them back.
        internal bool Prepare(int width,int height,float farPlane)
        {
            if(!linework || !lineShader || !lineShader.isSupported || !identityShader || !identityShader.isSupported) return false;
            if(!material) material=new Material(lineShader) { hideFlags=HideFlags.HideAndDontSave };
            if(!identities || identities.width!=width || identities.height!=height)
            {
                ReleaseIdentityTexture();
                identities=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                    {name="Object ID + Line Policy",filterMode=FilterMode.Point,antiAliasing=1,hideFlags=HideFlags.HideAndDontSave};
                identities.Create();
                sceneDepthNormals=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                    {name="Animated World Depth Normals",filterMode=FilterMode.Point,antiAliasing=1,hideFlags=HideFlags.HideAndDontSave};
                sceneDepthNormals.Create();
                identityHandle=RTHandles.Alloc(identities);geometryHandle=RTHandles.Alloc(sceneDepthNormals);
            }
            bool useIds=objectBoundaries;
            material.SetFloat("_UseObjectIds",useIds?1:0);
            material.SetFloat("_DebugObjectIds",debugIdentities && useIds?1:0);
            material.SetFloat("_DepthThreshold",depthThreshold);
            material.SetFloat("_NormalThreshold",normalThreshold);
            material.SetFloat("_LineStrength",strength);
            material.SetFloat("_LabFarPlane",farPlane);
            material.SetVector("_LabTexel",new Vector4(1f/width,1f/height,width,height));
            return true;
        }
        void ReleaseIdentityTexture()
        {
            // RTHandles wrapping an external texture do not own it.
            identityHandle?.Release();geometryHandle?.Release();identityHandle=null;geometryHandle=null;
            if(identities){identities.Release();if(Application.isPlaying)Destroy(identities);else DestroyImmediate(identities);identities=null;}
            if(sceneDepthNormals){sceneDepthNormals.Release();if(Application.isPlaying)Destroy(sceneDepthNormals);else DestroyImmediate(sceneDepthNormals);sceneDepthNormals=null;}
        }
        void OnDisable()
        {
            ReleaseIdentityTexture();
            if(material) { if(Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
        }
    }
}
