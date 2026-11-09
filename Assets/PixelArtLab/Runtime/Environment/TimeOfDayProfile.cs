using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab
{
    // Everything that makes a time of day look a certain way: sun and moon, ambient fill, sky,
    // height fog and colour grading. One asset per art direction; scenes share or swap them.
    // Values are Built-in-era tuned colours in sRGB; drivers convert where shaders need linear.
    [CreateAssetMenu(menuName="Pixel Art Lab/Environment/Time Of Day Profile",fileName="SO_TimeOfDay_")]
    public sealed class TimeOfDayProfile : ScriptableObject
    {
        [Header("Sun and moon")]
        public float sunAzimuth=-35;
        public float sunIntensity=1.05f;
        [Tooltip("Moonlight stays below the practical lights so they lead at night.")]
        [Range(0,1)]public float moonIntensity=.3f;
        public Color daySunColor=new Color(1,.95f,.84f),moonColor=new Color(.55f,.68f,1);
        [Tooltip("Dusk runs vermilion -> pink as the sun sinks (dawn runs it back).")]
        public Color duskVermilionSun=new Color(1,.42f,.2f),duskPinkSun=new Color(1,.5f,.58f);
        public Color overcastSunColor=new Color(.82f,.88f,1);
        [Range(0,1)]public float overcastSunTint=.8f,overcastSunDim=.38f;

        [Header("Ambient fill (share of the shadow colour)")]
        [Range(0,1)]public float nightAmbient=.1f;
        public Color nightAmbientTint=new Color(.42f,.62f,.76f),dayAmbientTint=new Color(.95f,.97f,1);
        public Color overcastAmbientTint=new Color(.62f,.72f,.84f);
        [Range(0,1)]public float overcastAmbientMix=.45f;

        [Header("Sky and distance fog")]
        public Color nightSky=new Color(.035f,.055f,.105f),daySky=new Color(.32f,.43f,.48f),overcastSky=new Color(.35f,.41f,.47f);
        [Range(0,1)]public float overcastSkyMix=.55f;
        public Vector2 distanceFogClear=new Vector2(80,150),distanceFogOvercast=new Vector2(57,99);

        [Header("Height fog (pools in low ground)")]
        public float fogBaseHeight=.3f,fogFalloff=1.2f;
        [Range(0,1)]public float nightFogDensity=.28f,dayFogDensity=.12f,fogDrift=.6f,lampFogGlow=.08f;
        [Range(0,2)]public float overcastFogBoost=.6f;
        public Color nightFogColor=new Color(.09f,.15f,.19f),dayFogColor=new Color(.66f,.72f,.74f);
        public Color duskVermilionFog=new Color(.48f,.26f,.2f),duskPinkFog=new Color(.46f,.28f,.38f),duskVioletFog=new Color(.3f,.24f,.42f);
        public Color overcastFogColor=new Color(.5f,.55f,.6f);
        [Range(0,1)]public float overcastFogMix=.5f;

        [Header("Colour grading volumes (optional)")]
        public VolumeProfile dayGrade;
        public VolumeProfile duskVermilionGrade,duskPinkGrade,duskVioletGrade;
        public VolumeProfile nightGrade;
    }
}
