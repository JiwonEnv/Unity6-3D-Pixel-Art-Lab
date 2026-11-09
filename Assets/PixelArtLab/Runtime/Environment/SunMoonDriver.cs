using UnityEngine;

namespace PixelArtLab
{
    // One directional light plays both sun and moon: it follows the hour, takes the dusk
    // colours of the TimeOfDayProfile, and dims under cloud.
    public sealed class SunMoonDriver : EnvironmentDriver
    {
        [Tooltip("Leave empty to use RenderSettings.sun.")]
        public Light sun;
        Color originalColor;Quaternion originalRotation;float originalIntensity;bool cached;
        protected override void OnEnable()
        {
            if(!sun)sun=RenderSettings.sun;
            if(sun)
            {
                originalColor=sun.color;originalIntensity=sun.intensity;originalRotation=sun.transform.rotation;cached=true;
                sun.shadows=LightShadows.Hard;sun.shadowStrength=.9f;sun.shadowBias=.025f;sun.shadowNormalBias=.10f;sun.renderMode=LightRenderMode.ForcePixel;
            }
            base.OnEnable();
        }
        protected override void OnApplied(EnvironmentState s)
        {
            if(!sun)return;var t=s.TimeOfDay;
            // Below the horizon the light flips over and becomes the moon.
            sun.transform.rotation=Quaternion.Euler((s.hour-6)*15+(s.IsDay?0:180),t.sunAzimuth,0);
            Color duskSun=Color.Lerp(t.duskVermilionSun,t.duskPinkSun,Mathf.Clamp01(s.DuskProgress*2));
            sun.color=s.IsDay?Color.Lerp(t.daySunColor,duskSun,s.Sunset):t.moonColor;
            sun.color=Color.Lerp(sun.color,t.overcastSunColor,s.Cloud*t.overcastSunTint);
            sun.intensity=Mathf.Lerp(t.moonIntensity,t.sunIntensity,s.Daylight)*(1-s.Cloud*t.overcastSunDim);
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            if(cached&&sun){sun.color=originalColor;sun.intensity=originalIntensity;sun.transform.rotation=originalRotation;}
        }
    }
}
