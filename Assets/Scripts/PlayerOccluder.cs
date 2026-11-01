using UnityEngine;
namespace PixelArtLab
{
    public enum PlayerOcclusionMode { Ignore, NaturalFade, BuildingScan }
    [DisallowMultipleComponent]
    public sealed class PlayerOccluder : MonoBehaviour
    {
        public PlayerOcclusionMode mode;
        public Renderer[] renderers;
        [System.NonSerialized]public float visibility;
        MaterialPropertyBlock block;
        public bool Blocks(Vector3 point,Camera camera)
        {
            if(renderers==null||mode==PlayerOcclusionMode.Ignore)return false;
            var delta=camera.transform.position-point;var ray=new Ray(point,delta.normalized);
            foreach(var renderer in renderers)if(renderer&&renderer.enabled&&renderer.gameObject.activeInHierarchy){var bounds=renderer.bounds;bounds.Expand(.2f);if(bounds.IntersectRay(ray,out float distance)&&distance<delta.magnitude-.2f)return true;}
            return false;
        }
        public void Apply(float fade)
        {
            visibility=fade;if(block==null)block=new MaterialPropertyBlock();
            if(renderers==null)return;
            foreach(var renderer in renderers)if(renderer){renderer.GetPropertyBlock(block);block.SetFloat("_LabRevealAmount",mode==PlayerOcclusionMode.NaturalFade?fade:0);renderer.SetPropertyBlock(block);}
        }
        void OnDisable(){if(renderers!=null)Apply(0);}
    }
}
