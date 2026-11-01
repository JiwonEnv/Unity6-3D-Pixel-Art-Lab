using UnityEngine;

namespace PixelArtLab
{
    [ExecuteAlways,RequireComponent(typeof(MeshFilter))]
    public sealed class AuthoredNormalProfile : MonoBehaviour
    {
        public Mesh geometricMesh;
        public Mesh authoredMesh;
        public bool useAuthored=true;
        public void Apply(){var mesh=useAuthored?authoredMesh:geometricMesh;if(mesh)GetComponent<MeshFilter>().sharedMesh=mesh;}
        void OnEnable()=>Apply();
        void OnValidate(){if(authoredMesh&&geometricMesh)Apply();}
    }
}
