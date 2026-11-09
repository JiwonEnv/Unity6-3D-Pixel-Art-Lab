using UnityEngine;

namespace PixelArtLab
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class PixelArtObject : MonoBehaviour
    {
        [Range(1,65535)] public int objectId=1;
        [Range(0,1)] public float silhouette=1;
        [Range(0,1)] public float interior=.25f;
        public void Apply()
        {
            var block=new MaterialPropertyBlock();
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.GetComponentInParent<PixelArtObject>()!=this)continue;
                renderer.GetPropertyBlock(block);
                block.SetVector("_LabObjectData",new Vector4((objectId&255)/255f,((objectId>>8)&255)/255f,interior,silhouette));
                renderer.SetPropertyBlock(block);
            }
        }
        void OnEnable()=>Apply();
        void OnValidate()=>Apply();
    }
}
