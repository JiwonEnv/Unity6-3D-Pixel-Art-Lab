using UnityEngine;

namespace PixelArtLab
{
    // LabWater.hlsl: stylized lake glints, shore foam and reflection.
    [CreateAssetMenu(menuName="Pixel Art Lab/Style/Water",fileName="SO_Water_")]
    public sealed class WaterStyle : SurfaceStyle
    {
        [Tooltip("Color of glints, rain rings and shore foam.")]
        public Color glintColor=new Color(.65f,.83f,.80f).gamma;
        [Range(0,1)]public float glintStrength=.4f;
        [Range(0,1)]public float rainRingStrength=.24f;
        [Range(0,1)]public float foamStrength=.65f;
        [Min(.01f),Tooltip("Normal map repeats per meter.")]
        public float normalTiling=.18f;
        [Header("Reflection")]
        [Range(0,1),Tooltip("Reflection looking straight down.")]
        public float fresnelBase=.12f;
        [Range(0,1),Tooltip("Extra reflection at grazing angles.")]
        public float fresnelGrazing=.75f;
        [Range(0,1),Tooltip("Reflection strength at night relative to day.")]
        public float nightReflection=.35f;

        public override void Push()
        {
            Shader.SetGlobalColor("_LabWaterGlintColor",glintColor);
            Shader.SetGlobalVector("_LabWaterShape",new Vector4(glintStrength,rainRingStrength,foamStrength,normalTiling));
            Shader.SetGlobalVector("_LabWaterReflect",new Vector4(fresnelBase,fresnelGrazing,nightReflection,0));
        }
    }
}
