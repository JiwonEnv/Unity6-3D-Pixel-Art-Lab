using UnityEngine;

namespace PixelArtLab
{
    // LabSnow.hlsl: how snow settles on upward faces. How much snow there is comes from
    // EnvironmentState.snowCover; this asset only decides how it looks.
    [CreateAssetMenu(menuName="Pixel Art Lab/Style/Snow",fileName="SO_Snow_")]
    public sealed class SnowStyle : SurfaceStyle
    {
        public Color color=new Color(.86f,.91f,.95f).gamma;
        [Range(-1,1),Tooltip("Surface normal height where snow starts to stick (0 = vertical wall).")]
        public float slopeStart=.20f;
        [Min(.1f),Tooltip("How quickly snow goes from none to full above the slope start.")]
        public float slopeSharpness=1.6f;
        [Range(1,16),Tooltip("Stepped coverage levels; fewer reads more pixel-art.")]
        public int steps=4;
        [Range(0,1),Tooltip("Rounds coverage up toward the next step.")]
        public float stepBias=.2f;

        public override void Push()
        {
            Shader.SetGlobalColor("_LabSnowColor",color);
            Shader.SetGlobalVector("_LabSnowShape",new Vector4(slopeStart,slopeSharpness,steps,stepBias));
        }
    }
}
