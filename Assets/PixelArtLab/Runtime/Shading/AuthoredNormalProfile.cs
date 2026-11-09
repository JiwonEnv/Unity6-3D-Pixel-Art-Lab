using UnityEngine;

namespace PixelArtLab
{
    // Swaps between the imported mesh and a copy with art-directed normals.
    // Works on a MeshFilter or a SkinnedMeshRenderer (animated characters).
    [ExecuteAlways]
    public sealed class AuthoredNormalProfile : MonoBehaviour
    {
        public Mesh geometricMesh;
        public Mesh authoredMesh;
        public bool useAuthored=true;
        public void Apply()
        {
            var mesh=useAuthored?authoredMesh:geometricMesh;if(!mesh)return;
            var filter=GetComponent<MeshFilter>();if(filter){filter.sharedMesh=mesh;return;}
            var skinned=GetComponent<SkinnedMeshRenderer>();if(skinned)skinned.sharedMesh=mesh;
        }
        void OnEnable()=>Apply();
        void OnValidate(){if(authoredMesh&&geometricMesh)Apply();}
    }
}
