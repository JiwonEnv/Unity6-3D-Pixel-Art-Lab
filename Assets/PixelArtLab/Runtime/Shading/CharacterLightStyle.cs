using UnityEngine;

namespace PixelArtLab
{
    // LabCharacterLight.hlsl: rim and bounce colors that keep characters readable.
    // Per-character strength stays on PlayerVisibility.
    [CreateAssetMenu(menuName="Pixel Art Lab/Style/Character Light",fileName="SO_CharacterLight_")]
    public sealed class CharacterLightStyle : SurfaceStyle
    {
        public Color rimDay=new Color(1,.86f,.62f).gamma,rimNight=new Color(.42f,.68f,1).gamma;
        [Range(0,1),Tooltip("How far the rim moves toward the day color at noon.")]
        public float rimDayMix=.6f;
        public Color bounceDay=new Color(.32f,.37f,.24f).gamma,bounceNight=new Color(.15f,.25f,.40f).gamma;
        [Range(0,1),Tooltip("Edge width: lower = wider rim.")]
        public float rimThreshold=.62f;
        [Range(0,1)]public float rimLevel=.6f;

        public override void Push()
        {
            Shader.SetGlobalColor("_LabRimDay",rimDay);Shader.SetGlobalColor("_LabRimNight",rimNight);
            Shader.SetGlobalColor("_LabBounceDay",bounceDay);Shader.SetGlobalColor("_LabBounceNight",bounceNight);
            Shader.SetGlobalVector("_LabRimShape",new Vector4(rimThreshold,rimLevel,rimDayMix,0));
        }
    }
}
