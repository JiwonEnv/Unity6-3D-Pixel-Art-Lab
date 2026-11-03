using UnityEngine;

namespace PixelArtLab
{
    // How fast weather changes and how strongly it affects sky and wind. Rates are per second.
    [CreateAssetMenu(menuName="Pixel Art Lab/Environment/Weather Profile",fileName="SO_Weather_")]
    public sealed class WeatherProfile : ScriptableObject
    {
        [Header("Transitions")]
        public float rainRate=.45f,snowRate=.40f;
        [Tooltip("Surfaces soak quickly in rain and dry slowly afterwards.")]
        public float wetRate=.25f,dryRate=.025f;
        [Range(0,1)]public float snowCoverMax=.95f;
        public float snowAccumulateRate=.065f,snowMeltRate=.035f;
        [Header("Effects")]
        [Range(0,1)]public float rainCloud=.8f,snowCloud=.65f;
        [Tooltip("Extra wind gust share while raining / snowing.")]
        public float rainGust=.65f,snowGust=.25f;
    }
}
