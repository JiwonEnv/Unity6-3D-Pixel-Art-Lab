using UnityEngine;

namespace PixelArtLab
{
    // LabToonLighting.hlsl: how light energy is cut into flat bands.
    [CreateAssetMenu(menuName="Pixel Art Lab/Style/Toon Ramp",fileName="SO_ToonRamp_")]
    public sealed class ToonRampStyle : SurfaceStyle
    {
        [Tooltip("Light energy where each band starts (ascending).")]
        public Vector3 bandStarts=new Vector3(.16f,.48f,.78f);
        [Tooltip("Brightness of the four bands, darkest first.")]
        public Vector4 bandLevels=new Vector4(0,.48f,.82f,1);
        [Tooltip("Hue of the fully lit side; the shadow side uses the material's Shadow Hue.")]
        public Color litTint=new Color(1.04f,1,.86f).gamma;
        [Range(0,1),Tooltip("How much the sun/moon color tints the bands.")]
        public float lightColorMix=.38f;
        [Range(0,2),Tooltip("Direct light strength on top of the ambient shadow side.")]
        public float directStrength=.65f;
        [Range(0,1),Tooltip("How much cloud shadows dim direct light.")]
        public float cloudDimming=.35f;
        [Range(0,1),Tooltip("Light passing through leaves from behind.")]
        public float leafTransmission=.14f;

        public override void Push()
        {
            Shader.SetGlobalVector("_LabToonBands",bandStarts);
            Shader.SetGlobalVector("_LabToonLevels",bandLevels);
            Shader.SetGlobalColor("_LabToonLitTint",litTint);
            Shader.SetGlobalVector("_LabToonParams",new Vector4(lightColorMix,directStrength,cloudDimming,leafTransmission));
        }
    }
}
