using UnityEngine;

namespace PixelArtLab
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public class PixelLinework : MonoBehaviour
    {
        public Shader lineShader;
        public bool linework = true;
        [Range(0,1)] public float strength=.78f;
        public float depthThreshold=.16f;
        [Range(0,1)] public float normalThreshold=.32f;
        Material material;
        void OnEnable() { GetComponent<Camera>().depthTextureMode |= DepthTextureMode.DepthNormals; }
        void OnRenderImage(RenderTexture src,RenderTexture dst)
        {
            if(!linework || !lineShader || !lineShader.isSupported) { Graphics.Blit(src,dst); return; }
            if(!material) material=new Material(lineShader) { hideFlags=HideFlags.HideAndDontSave };
            material.SetFloat("_DepthThreshold",depthThreshold);
            material.SetFloat("_NormalThreshold",normalThreshold);
            material.SetFloat("_LineStrength",strength);
            Graphics.Blit(src,dst,material);
        }
        void OnDisable() { if(material) { if(Application.isPlaying) Destroy(material); else DestroyImmediate(material); } }
    }
}
