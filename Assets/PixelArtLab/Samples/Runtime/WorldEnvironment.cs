using System.Linq;
using UnityEngine;

namespace PixelArtLab
{
    // Sample glue for the Amberwood/Woodland scenes. The reusable environment lives in
    // EnvironmentState and its drivers (Runtime/Environment); this adds the demo keyboard
    // shortcuts and keeps the older API the sample builders and verifications use.
    [ExecuteAlways,RequireComponent(typeof(EnvironmentState))]
    public sealed class WorldEnvironment : MonoBehaviour
    {
        public ExplorerMotor player;
        EnvironmentState cachedState;
        public EnvironmentState State=>cachedState?cachedState:cachedState=GetComponent<EnvironmentState>();

        void Update()
        {
            if(!Application.isPlaying)return;
            var s=State;
            if(Input.GetKeyDown(KeyCode.F1)){s.hour=7;s.runClock=false;}
            if(Input.GetKeyDown(KeyCode.F2)){s.hour=13;s.runClock=false;}
            if(Input.GetKeyDown(KeyCode.F3)){s.hour=17.5f;s.runClock=false;}
            if(Input.GetKeyDown(KeyCode.F4)){s.hour=0;s.runClock=false;}
            if(Input.GetKeyDown(KeyCode.Alpha1))s.weather=WeatherKind.Clear;
            if(Input.GetKeyDown(KeyCode.Alpha2))s.weather=WeatherKind.Rain;
            if(Input.GetKeyDown(KeyCode.Alpha3))s.weather=WeatherKind.Snow;
            if(Input.GetKeyDown(KeyCode.L))s.runClock=!s.runClock;
            if(Input.GetKeyDown(KeyCode.B))s.windEnabled=!s.windEnabled;
            if(Input.GetKeyDown(KeyCode.V))s.waterMotion=!s.waterMotion;
            if(Input.GetKeyDown(KeyCode.U)&&player)player.steppedFacing=!player.steppedFacing;
        }

        // Forwarded state.
        public float hour{get=>State.hour;set=>State.hour=value;}
        public bool runClock{get=>State.runClock;set=>State.runClock=value;}
        public float animationTime{get=>State.animationTime;set=>State.animationTime=value;}
        public WeatherKind weather{get=>State.weather;set=>State.weather=value;}
        public float rain{get=>State.rain;set=>State.rain=value;}
        public float snow{get=>State.snow;set=>State.snow=value;}
        public float wetness{get=>State.wetness;set=>State.wetness=value;}
        public float snowCover{get=>State.snowCover;set=>State.snowCover=value;}
        public bool windEnabled{get=>State.windEnabled;set=>State.windEnabled=value;}
        public float windStrength{get=>State.windStrength;set=>State.windStrength=value;}
        public Vector2 windDirection{get=>State.windDirection;set=>State.windDirection=value;}
        public bool waterEnabled{get=>State.waterMotion;set=>State.waterMotion=value;}
        public bool heightFog{get=>State.heightFog;set=>State.heightFog=value;}
        public bool colorGrading{get=>State.colorGrading;set=>State.colorGrading=value;}
        public float Daylight=>State.Daylight;
        public string ClockLabel=>State.ClockLabel;
        public void SetWeather(WeatherKind value,bool immediate=false)=>State.SetWeather(value,immediate);
        public void Apply()=>State.Apply();

        // Forwarded drivers; setting one adds the driver if the scene does not have it yet.
        public Light sun{get=>Driver<SunMoonDriver>(false)?Driver<SunMoonDriver>(false).sun:null;set=>Driver<SunMoonDriver>(true).sun=value;}
        public Camera worldCamera{get=>Driver<SkyFogDriver>(false)?Driver<SkyFogDriver>(false).targetCamera:Camera.main;set=>Driver<SkyFogDriver>(true).targetCamera=value;}
        // All registered practical lights. Assigning gives plain lights a PracticalLight that
        // keeps the original torch values (4 at night, 2.1 by day, no flicker).
        public Light[] torches
        {
            get=>PracticalLight.All.Where(p=>p).Select(p=>p.Light).ToArray();
            set
            {
                Driver<PracticalLightManager>(true).focus=player?player.transform:null;
                if(value!=null)foreach(var light in value)if(light&&!light.GetComponent<PracticalLight>())
                {
                    var p=light.gameObject.AddComponent<PracticalLight>();p.nightIntensity=4;p.dayIntensity=2.1f;p.flicker=0;p.wobble=0;
                }
            }
        }
        T Driver<T>(bool create) where T:EnvironmentDriver
        {
            var d=GetComponent<T>();if(!d&&create)d=gameObject.AddComponent<T>();return d;
        }
    }
}
