using System;
using UnityEngine;

namespace PixelArtLab
{
    public enum WeatherKind {Clear,Rain,Snow}

    // The scene's single source of time of day, weather and wind. It advances them in play mode,
    // derives the sky values every driver shares, and publishes the _Lab shader globals that
    // Sprite Toon and the other shaders read. Drivers (sun, sky, lights, grading) subscribe to
    // Applied, so one Apply() updates the whole look (Docs/Conventions.md).
    [ExecuteAlways,DefaultExecutionOrder(-50)]
    public sealed class EnvironmentState : MonoBehaviour
    {
        public static EnvironmentState Active {get;private set;}
        public TimeOfDayProfile timeOfDay;
        public WeatherProfile weatherProfile;
        [Header("Clock")]
        [Range(0,24)]public float hour=14;
        public bool runClock=true;
        [Min(60)]public float dayDuration=480;
        [Header("Weather")]
        public WeatherKind weather;
        [Range(0,1)]public float rain,snow,wetness,snowCover;
        [Header("Motion")]
        public bool windEnabled=true;
        [Range(0,2)]public float windStrength=.65f;
        public Vector2 windDirection=new Vector2(1,.35f);
        public bool waterMotion=true;
        public float animationTime;
        [Header("Features")]
        public bool heightFog=true,colorGrading=true;
        public event Action<EnvironmentState> Applied;

        // Derived on every Apply.
        public float Elevation {get;private set;}  // sun height, -1..1
        public float Daylight {get;private set;}   // 0 night .. 1 full day
        public float Sunset {get;private set;}     // how low the sun is while up
        public float Dusk {get;private set;}       // dusk/dawn window weight
        public float DuskProgress {get;private set;} // 0 vermilion, .5 pink, 1 violet
        public float Cloud {get;private set;}
        public bool IsDay=>Elevation>0;
        public string ClockLabel=>$"{Mathf.FloorToInt(hour):00}:{Mathf.FloorToInt((hour%1)*60):00}";
        public TimeOfDayProfile TimeOfDay=>timeOfDay?timeOfDay:fallbackTime?fallbackTime:fallbackTime=ScriptableObject.CreateInstance<TimeOfDayProfile>();
        public WeatherProfile Weather=>weatherProfile?weatherProfile:fallbackWeather?fallbackWeather:fallbackWeather=ScriptableObject.CreateInstance<WeatherProfile>();
        TimeOfDayProfile fallbackTime;WeatherProfile fallbackWeather;

        void OnEnable(){Active=this;Apply();}
        void Update()
        {
            if(Application.isPlaying)Advance(Time.deltaTime);
            Apply();
        }
        // Moves the clock, the animation time and the weather toward its target.
        public void Advance(float deltaTime)
        {
            var w=Weather;
            if(runClock)hour=Mathf.Repeat(hour+deltaTime*24/Mathf.Max(60,dayDuration),24);
            animationTime+=deltaTime;
            rain=Mathf.MoveTowards(rain,weather==WeatherKind.Rain?1:0,deltaTime*w.rainRate);
            snow=Mathf.MoveTowards(snow,weather==WeatherKind.Snow?1:0,deltaTime*w.snowRate);
            wetness=Mathf.MoveTowards(wetness,rain,deltaTime*(rain>wetness?w.wetRate:w.dryRate));
            snowCover=Mathf.MoveTowards(snowCover,weather==WeatherKind.Snow?w.snowCoverMax:0,deltaTime*(weather==WeatherKind.Snow?w.snowAccumulateRate:w.snowMeltRate));
        }
        public void SetWeather(WeatherKind value,bool immediate=false)
        {
            weather=value;
            if(immediate){rain=value==WeatherKind.Rain?1:0;snow=value==WeatherKind.Snow?1:0;wetness=rain;snowCover=snow*Weather.snowCoverMax;}
            Apply();
        }
        public void Apply()
        {
            var t=TimeOfDay;var w=Weather;
            Elevation=Mathf.Sin((hour-6)/24*Mathf.PI*2);
            Daylight=Mathf.SmoothStep(0,1,Mathf.Clamp01((Elevation+.08f)/.65f));
            Cloud=Mathf.Clamp01(rain*w.rainCloud+snow*w.snowCloud);
            Sunset=Mathf.Clamp01(1-Elevation/.55f);
            // Dusk window: from the sun at ~20 degrees until well into twilight.
            Dusk=Mathf.Clamp01((.35f-Elevation)/.12f)*Mathf.Clamp01((Elevation+.25f)/.1f);
            DuskProgress=Mathf.InverseLerp(.3f,-.18f,Elevation);

            // Shadow is ambient; direct light only adds energy (see Sprite Toon SunLighting).
            Color ambient=Color.Lerp(t.nightAmbientTint,t.dayAmbientTint,Daylight);
            // sqrt keeps sunset readable while the sun grazes.
            ambient=Color.Lerp(ambient,t.overcastAmbientTint,Cloud*t.overcastAmbientMix)*Mathf.Lerp(t.nightAmbient,1,Mathf.Sqrt(Daylight));
            Shader.SetGlobalVector("_LabEnvironment",new Vector4(1,Daylight,wetness,snowCover));
            Shader.SetGlobalColor("_LabAmbientTint",ambient);
            float gust=windEnabled?windStrength*(1+rain*w.rainGust+snow*w.snowGust):0;
            Shader.SetGlobalVector("_LabWind",new Vector4(windDirection.x,windDirection.y,gust,0));
            Shader.SetGlobalFloat("_LabWeatherTime",Mathf.Floor(animationTime*12)/12);
            Shader.SetGlobalFloat("_LabWaterTime",animationTime);
            Shader.SetGlobalFloat("_LabWaterMotion",waterMotion?1:0);
            Shader.SetGlobalFloat("_LabCloudCover",Mathf.Clamp01(.12f+Cloud));

            Color haze=Color.Lerp(t.nightFogColor,t.dayFogColor,Daylight);
            Color duskHaze=DuskProgress<.5f?Color.Lerp(t.duskVermilionFog,t.duskPinkFog,DuskProgress*2):Color.Lerp(t.duskPinkFog,t.duskVioletFog,DuskProgress*2-1);
            haze=Color.Lerp(haze,duskHaze,Dusk);
            haze=Color.Lerp(haze,t.overcastFogColor,Cloud*t.overcastFogMix);
            float density=heightFog?Mathf.Lerp(t.nightFogDensity,t.dayFogDensity,Daylight)*(1+Cloud*t.overcastFogBoost):0;
            Shader.SetGlobalVector("_LabHeightFog",new Vector4(t.fogBaseHeight,t.fogFalloff,density,t.fogDrift));
            // SetGlobalVector skips the sRGB->linear step SetGlobalColor does; convert explicitly.
            haze=haze.linear;Shader.SetGlobalVector("_LabHeightFogColor",new Vector4(haze.r,haze.g,haze.b,t.lampFogGlow));

            Applied?.Invoke(this);
        }
        void OnDisable()
        {
            if(Active==this)Active=null;
            Shader.SetGlobalVector("_LabEnvironment",Vector4.zero);Shader.SetGlobalVector("_LabWind",Vector4.zero);
            Shader.SetGlobalFloat("_LabWeatherTime",0);Shader.SetGlobalFloat("_LabCloudCover",0);Shader.SetGlobalFloat("_LabWaterMotion",0);
            Shader.SetGlobalFloat("_LabWaterTime",0);Shader.SetGlobalVector("_LabHeightFog",Vector4.zero);
        }
        void OnDestroy()
        {
            if(fallbackTime){if(Application.isPlaying)Destroy(fallbackTime);else DestroyImmediate(fallbackTime);}
            if(fallbackWeather){if(Application.isPlaying)Destroy(fallbackWeather);else DestroyImmediate(fallbackWeather);}
        }
    }
}
