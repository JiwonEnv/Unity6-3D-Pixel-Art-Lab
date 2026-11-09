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
        public static float ActiveMinimumHeight {get;private set;}=.38f;

        [Min(1),Tooltip("World grid (m); at most one puddle per cell.")]
        public float cellSize=6;
        [Tooltip("No puddles below this world height (e.g. a lake shore); they fade in over 0.22 m.")]
        public float minimumHeight=.38f;
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
            ActiveCellSize=cellSize;ActiveMinimumHeight=minimumHeight;
            Shader.SetGlobalVector("_LabPuddleLevel",new Vector4(minimumHeight,minimumHeight+.22f,0,0));
            Shader.SetGlobalVector("_LabPuddleShape",new Vector4(cellSize,ringDensity,ringSpeed,ringStrength));
            var damp=dampTint.linear;
            Shader.SetGlobalVector("_LabPuddleDamp",new Vector4(damp.r,damp.g,damp.b,dampRoughness));
            Shader.SetGlobalVector("_LabPuddlePool",new Vector4(poolRoughness,poolReflection,grazingReflection,dampReflection));
        }
    }
}
