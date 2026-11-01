using UnityEngine;

namespace PixelArtLab
{
    public enum WorldMotionKind {None,Tree,Grass,Flag,Water}
    [ExecuteAlways]
    public sealed class WorldSurfaceMotion : MonoBehaviour
    {
        public WorldMotionKind kind;
        public void Apply()
        {
            var block=new MaterialPropertyBlock();
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
                var bounds=filter.sharedMesh.bounds;
                float anchor=kind==WorldMotionKind.Flag?bounds.min.x:bounds.min.y;
                float extent=kind==WorldMotionKind.Flag?bounds.size.x:bounds.size.y;
                renderer.GetPropertyBlock(block);
                block.SetVector("_LabWindParams",new Vector4((int)kind,anchor,1/Mathf.Max(.001f,extent),transform.position.x*.7f+transform.position.z*.3f));
                renderer.SetPropertyBlock(block);
            }
        }
        void OnEnable()=>Apply();
        void OnValidate()=>Apply();
    }
}
