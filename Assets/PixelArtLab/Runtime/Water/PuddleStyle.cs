using UnityEngine;

namespace PixelArtLab
{
    // LabPuddles.hlsl: rain puddle layout and look. Which materials get puddles is the
    // material's Puddles value; how full they are comes from EnvironmentState.wetness.
    [CreateAssetMenu(menuName="Pixel Art Lab/Style/Puddles",fileName="SO_Puddles_")]
    public sealed class PuddleStyle : SurfaceStyle
    {
        // Read by PuddleSurface so footstep splashes follow the visible water.
        public static float ActiveCellSize {get;private set;}=6;

        [Min(1),Tooltip("World grid (m); at most one puddle per cell.")]
        public float cellSize=6;
        [Header("Damp soil")]
        public Color dampTint=new Color(.43f,.50f,.54f).gamma;
        [Range(.08f,1)]public float dampRoughness=.28f;
        [Range(0,.2f)]public float dampReflection=.06f;
        [Header("Pool")]
        [Range(.08f,1)]public float poolRoughness=.12f;
        [Range(0,1),Tooltip("Mirror strength looking straight down.")]
        public float poolReflection=.55f;
        [Range(0,1),Tooltip("Extra mirror strength at grazing angles.")]
        public float grazingReflection=.25f;
        [Header("Rain rings")]
        [Min(.1f),Tooltip("Ring cells per meter.")]
        public float ringDensity=1.3f;
        [Min(0)]public float ringSpeed=1.25f;
        [Range(0,.5f)]public float ringStrength=.08f;

        public override void Push()
        {
            ActiveCellSize=cellSize;
            Shader.SetGlobalVector("_LabPuddleShape",new Vector4(cellSize,ringDensity,ringSpeed,ringStrength));
            var damp=dampTint.linear;
            Shader.SetGlobalVector("_LabPuddleDamp",new Vector4(damp.r,damp.g,damp.b,dampRoughness));
            Shader.SetGlobalVector("_LabPuddlePool",new Vector4(poolRoughness,poolReflection,grazingReflection,dampReflection));
        }
    }
}
