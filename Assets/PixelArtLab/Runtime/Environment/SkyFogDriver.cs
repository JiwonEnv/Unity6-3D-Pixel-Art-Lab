using UnityEngine;

namespace PixelArtLab
{
    // Camera background colour and Unity's distance fog by time of day and cloud.
    // (Height fog is a shader global published by EnvironmentState.)
    public sealed class SkyFogDriver : EnvironmentDriver
    {
        [Tooltip("Leave empty to use Camera.main.")]
        public Camera targetCamera;
        Color originalBackground;
        protected override void OnEnable()
        {
            if(!targetCamera)targetCamera=Camera.main;
            if(targetCamera)originalBackground=targetCamera.backgroundColor;
            base.OnEnable();
        }
        protected override void OnApplied(EnvironmentState s)
        {
            var t=s.TimeOfDay;
            Color sky=Color.Lerp(t.nightSky,t.daySky,s.Daylight);
            sky=Color.Lerp(sky,t.overcastSky,s.Cloud*t.overcastSkyMix);
            if(targetCamera)targetCamera.backgroundColor=sky;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=sky;
            RenderSettings.fogStartDistance=Mathf.Lerp(t.distanceFogClear.x,t.distanceFogOvercast.x,s.Cloud);
            RenderSettings.fogEndDistance=Mathf.Lerp(t.distanceFogClear.y,t.distanceFogOvercast.y,s.Cloud);
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            if(targetCamera)targetCamera.backgroundColor=originalBackground;RenderSettings.fog=false;
        }
    }
}
