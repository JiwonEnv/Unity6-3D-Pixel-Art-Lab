using UnityEngine;

namespace PixelArtLab
{
    // LabWetness.hlsl: how rain-soaked surfaces look. How wet they are comes from
    // EnvironmentState.wetness.
    [CreateAssetMenu(menuName="Pixel Art Lab/Style/Wetness",fileName="SO_Wetness_")]
    public sealed class WetnessStyle : SurfaceStyle
    {
        [Range(0,1),Tooltip("How much fully wet surfaces darken.")]
        public float darkening=.17f;
        [Range(.8f,1),Tooltip("Higher = smaller wet highlight.")]
        public float sheenSize=.975f;
        [Range(0,1)]public float sheenStrength=.12f;

        public override void Push()=>Shader.SetGlobalVector("_LabWetShape",new Vector4(darkening,sheenSize,sheenStrength,0));
    }
}
