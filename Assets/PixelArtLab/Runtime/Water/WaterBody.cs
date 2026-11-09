using System.Collections.Generic;
using UnityEngine;

namespace PixelArtLab
{
    // An elliptical body of water: gives LabWater.hlsl its shore line for foam and tells other
    // features (rain splashes) where the water surface is. Fit fills the shape from the renderer.
    [ExecuteAlways]
    public sealed class WaterBody : MonoBehaviour
    {
        public static readonly List<WaterBody> All=new List<WaterBody>();
        [Tooltip("Shore ellipse center in world x/z.")]
        public Vector2 center;
        [Tooltip("Shore ellipse radii along world x/z.")]
        public Vector2 radii=new Vector2(5,5);
        [Tooltip("World height where rain splashes land on the water.")]
        public float surfaceHeight;
        static readonly int boundsId=Shader.PropertyToID("_LabWaterBounds");

        void OnEnable(){if(!All.Contains(this))All.Add(this);Apply();}
        void OnDisable()=>All.Remove(this);
        void OnValidate(){if(isActiveAndEnabled)Apply();}
        [ContextMenu("Fit To Renderer")]
        public void Fit()
        {
            var r=GetComponentInChildren<Renderer>();if(!r)return;
            var b=r.bounds;center=new Vector2(b.center.x,b.center.z);radii=new Vector2(b.extents.x,b.extents.z);surfaceHeight=b.max.y;Apply();
        }
        public void Apply()
        {
            var block=new MaterialPropertyBlock();
            foreach(var r in GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(block);block.SetVector(boundsId,new Vector4(center.x,center.y,Mathf.Max(.01f,radii.x),Mathf.Max(.01f,radii.y)));r.SetPropertyBlock(block);
            }
        }
        public bool Contains(float x,float z)
        {
            float dx=(x-center.x)/Mathf.Max(.01f,radii.x),dz=(z-center.y)/Mathf.Max(.01f,radii.y);return dx*dx+dz*dz<1;
        }
        // Highest water surface at x/z, or fallback when no water body covers it.
        public static float SurfaceAt(float x,float z,float fallback)
        {
            foreach(var water in All)if(water&&water.Contains(x,z))fallback=Mathf.Max(fallback,water.surfaceHeight);
            return fallback;
        }
    }
}
