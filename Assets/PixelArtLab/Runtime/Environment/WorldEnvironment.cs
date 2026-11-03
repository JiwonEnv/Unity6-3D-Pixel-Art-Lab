using UnityEngine;

namespace PixelArtLab
{
    public enum ValleyWeather {Clear,Rain,Snow}
    [ExecuteAlways]
    public sealed class WorldEnvironment : MonoBehaviour
    {
        public Light sun;
        public Light[] torches;
        public Camera worldCamera;
        public ExplorerMotor player;
        [Range(0,24)]public float hour=14;
        public ValleyWeather weather;
        public bool runClock=true,windEnabled=true,waterEnabled=true;
        [Min(60)]public float dayDuration=480;
        [Range(0,2)]public float windStrength=.65f;
        public Vector2 windDirection=new Vector2(1,.35f);
        public float animationTime;
        [Range(0,1)]public float rain,snow,wetness,snowCover;
        // Ambient fill as a share of the shadow colour (Docs/Background-Lighting-Design.md L2).
        // Night drops low so practical lights lead the frame; night shadows lean teal
        // against the warm torches.
        [Range(0,1)]public float nightAmbient=.1f;
        // Moonlight stays below the torches so practical lights lead at night.
        [Range(0,1)]public float moonIntensity=.3f;
        // Point shadows go to this many of the nearest practical lights (512px cube maps each).
        [Range(0,8)]public int shadowedPracticals=4;
        readonly System.Collections.Generic.List<Light> ordered=new System.Collections.Generic.List<Light>();
        // Height fog (Docs/Background-Lighting-Design.md L4): haze pools over the lake and low
        // ground, thickest at night, warm-violet at sunset, and glows around practical lights.
        public bool heightFog=true;
        public float fogBaseHeight=.3f,fogFalloff=1.2f;
        [Range(0,1)]public float nightFogDensity=.28f,dayFogDensity=.12f,fogDrift=.6f,lampFogGlow=.08f;
        // Time-of-day colour grading (Docs/Background-Lighting-Design.md L7): global volumes
        // whose weights follow the sun. Golden hour peaks mid-transition, at dawn and dusk.
        public bool colorGrading=true;
        public UnityEngine.Rendering.Volume dayGrade,duskVermilionGrade,duskPinkGrade,duskVioletGrade,nightGrade;
        public Color nightFogColor=new Color(.09f,.15f,.19f),dayFogColor=new Color(.66f,.72f,.74f);
        // Dusk runs vermilion -> pink -> violet as the sun sinks (dawn runs it back). One
        // progress value drives the sun colour, the haze and the grading volumes together.
        public Color duskVermilionSun=new Color(1,.42f,.2f),duskPinkSun=new Color(1,.5f,.58f);
        public Color duskVermilionFog=new Color(.48f,.26f,.2f),duskPinkFog=new Color(.46f,.28f,.38f),duskVioletFog=new Color(.3f,.24f,.42f);
        public Color nightAmbientTint=new Color(.42f,.62f,.76f),dayAmbientTint=new Color(.95f,.97f,1);
        Color originalSunColor,originalBackground;
        Quaternion originalRotation;
        float originalIntensity;
        bool cached;
        public float Daylight {get;private set;}
        public string ClockLabel=>$"{Mathf.FloorToInt(hour):00}:{Mathf.FloorToInt((hour%1)*60):00}";
        void OnEnable()
        {
            // Shadow distance (one 80m cascade: the follow camera sits 45m away), Forward+ and
            // HDR live on the PC URP asset (Assets/PixelArtLab/Settings/RP_PixelArtLab_PC).
            if(!sun)sun=RenderSettings.sun;
            if(!worldCamera)worldCamera=Camera.main;
            if(sun){originalSunColor=sun.color;originalIntensity=sun.intensity;originalRotation=sun.transform.rotation;cached=true;}
            if(sun){sun.shadows=LightShadows.Hard;sun.shadowStrength=.9f;sun.shadowBias=.025f;sun.shadowNormalBias=.10f;sun.renderMode=LightRenderMode.ForcePixel;}
            if(worldCamera)originalBackground=worldCamera.backgroundColor;
            Apply();
        }
        void Update()
        {
            if(Application.isPlaying)
            {
                if(Input.GetKeyDown(KeyCode.F1)){hour=7;runClock=false;}
                if(Input.GetKeyDown(KeyCode.F2)){hour=13;runClock=false;}
                if(Input.GetKeyDown(KeyCode.F3)){hour=17.5f;runClock=false;}
                if(Input.GetKeyDown(KeyCode.F4)){hour=0;runClock=false;}
                if(Input.GetKeyDown(KeyCode.Alpha1))weather=ValleyWeather.Clear;
                if(Input.GetKeyDown(KeyCode.Alpha2))weather=ValleyWeather.Rain;
                if(Input.GetKeyDown(KeyCode.Alpha3))weather=ValleyWeather.Snow;
                if(Input.GetKeyDown(KeyCode.L))runClock=!runClock;
                if(Input.GetKeyDown(KeyCode.B))windEnabled=!windEnabled;
                if(Input.GetKeyDown(KeyCode.V))waterEnabled=!waterEnabled;
                if(Input.GetKeyDown(KeyCode.U)&&player)player.steppedFacing=!player.steppedFacing;
                if(runClock)hour=Mathf.Repeat(hour+Time.deltaTime*24/Mathf.Max(60,dayDuration),24);
                animationTime+=Time.deltaTime;
                rain=Mathf.MoveTowards(rain,weather==ValleyWeather.Rain?1:0,Time.deltaTime*.45f);
                snow=Mathf.MoveTowards(snow,weather==ValleyWeather.Snow?1:0,Time.deltaTime*.40f);
                wetness=Mathf.MoveTowards(wetness,rain,Time.deltaTime*(rain>wetness?.25f:.025f));
                snowCover=Mathf.MoveTowards(snowCover,weather==ValleyWeather.Snow?.95f:0,Time.deltaTime*(weather==ValleyWeather.Snow?.065f:.035f));
            }
            Apply();
        }
        public void SetWeather(ValleyWeather value,bool immediate=false)
        {
            weather=value;
            if(immediate){rain=value==ValleyWeather.Rain?1:0;snow=value==ValleyWeather.Snow?1:0;wetness=rain;snowCover=snow*.95f;}
            Apply();
        }
        public void Apply()
        {
            float elevation=Mathf.Sin((hour-6)/24*Mathf.PI*2);
            Daylight=Mathf.SmoothStep(0,1,Mathf.Clamp01((elevation+.08f)/.65f));
            float cloud=Mathf.Clamp01(rain*.8f+snow*.65f);
            float sunset=Mathf.Clamp01(1-elevation/.55f);
            // Dusk window: from the sun at ~20 degrees until well into twilight.
            float dusk=Mathf.Clamp01((.35f-elevation)/.12f)*Mathf.Clamp01((elevation+.25f)/.1f);
            float duskProgress=Mathf.InverseLerp(.3f,-.18f,elevation); // 0 vermilion, .5 pink, 1 violet
            if(sun)
            {
                bool day=elevation>0;
                sun.transform.rotation=Quaternion.Euler((hour-6)*15+(day?0:180),-35,0);
                Color duskSun=Color.Lerp(duskVermilionSun,duskPinkSun,Mathf.Clamp01(duskProgress*2));
                sun.color=day?Color.Lerp(new Color(1,.95f,.84f),duskSun,sunset):new Color(.55f,.68f,1);
                sun.color=Color.Lerp(sun.color,new Color(.82f,.88f,1),cloud*.8f);
                sun.intensity=Mathf.Lerp(moonIntensity,1.05f,Daylight)*(1-cloud*.38f);
            }
            Color ambient=Color.Lerp(nightAmbientTint,dayAmbientTint,Daylight);
            ambient=Color.Lerp(ambient,new Color(.62f,.72f,.84f),cloud*.45f)*Mathf.Lerp(nightAmbient,1,Mathf.Sqrt(Daylight)); // sqrt keeps sunset readable while the sun grazes
            Shader.SetGlobalVector("_LabEnvironment",new Vector4(1,Daylight,wetness,snowCover));
            Shader.SetGlobalColor("_LabAmbientTint",ambient);
            float gust=windEnabled?windStrength*(1+rain*.65f+snow*.25f):0;
            Shader.SetGlobalVector("_LabWind",new Vector4(windDirection.x,windDirection.y,gust,0));
            Shader.SetGlobalFloat("_LabWeatherTime",Mathf.Floor(animationTime*12)/12);
            Shader.SetGlobalFloat("_LabWaterTime",animationTime);
            Shader.SetGlobalFloat("_LabWaterMotion",waterEnabled?1:0);
            Shader.SetGlobalFloat("_LabCloudCover",Mathf.Clamp01(.12f+cloud));
            if(torches!=null)
            {
                // The nearest lit practicals around the player cast shadows; the rest only light.
                ordered.Clear();foreach(var torch in torches)if(torch)ordered.Add(torch);
                if(player){var at=player.transform.position;ordered.Sort((a,b)=>(a.transform.position-at).sqrMagnitude.CompareTo((b.transform.position-at).sqrMagnitude));}
                int localShadowLights=0;
                foreach(var torch in ordered)
                {
                    var practical=torch.GetComponent<PracticalLight>();
                    if(practical)practical.Apply(Daylight,animationTime);else torch.intensity=Mathf.Lerp(4.0f,2.1f,Daylight);
                    bool nearby=player&&torch.enabled&&Vector3.Distance(player.transform.position,torch.transform.position)<torch.range+2;
                    bool cast=nearby&&localShadowLights<shadowedPracticals;if(cast)localShadowLights++;
                    torch.shadows=cast?LightShadows.Hard:LightShadows.None;torch.shadowStrength=.85f;torch.shadowBias=.025f;torch.shadowNormalBias=.1f;torch.shadowCustomResolution=512;
                }
            }
            Color background=Color.Lerp(new Color(.035f,.055f,.105f),new Color(.32f,.43f,.48f),Daylight);
            background=Color.Lerp(background,new Color(.35f,.41f,.47f),cloud*.55f);
            if(worldCamera)worldCamera.backgroundColor=background;
            float grade=colorGrading?1:0;
            float vermilion=dusk*(1-Mathf.Clamp01(duskProgress*2)),violet=dusk*Mathf.Clamp01(duskProgress*2-1),pink=dusk-vermilion-violet;
            if(dayGrade)dayGrade.weight=grade*Daylight*(1-dusk);
            if(duskVermilionGrade)duskVermilionGrade.weight=grade*vermilion;
            if(duskPinkGrade)duskPinkGrade.weight=grade*pink;
            if(duskVioletGrade)duskVioletGrade.weight=grade*violet;
            if(nightGrade)nightGrade.weight=grade*(1-Daylight)*(1-dusk);
            Color haze=Color.Lerp(nightFogColor,dayFogColor,Daylight);
            Color duskHaze=duskProgress<.5f?Color.Lerp(duskVermilionFog,duskPinkFog,duskProgress*2):Color.Lerp(duskPinkFog,duskVioletFog,duskProgress*2-1);
            haze=Color.Lerp(haze,duskHaze,dusk);
            haze=Color.Lerp(haze,new Color(.5f,.55f,.6f),cloud*.5f);
            float density=heightFog?Mathf.Lerp(nightFogDensity,dayFogDensity,Daylight)*(1+cloud*.6f):0;
            Shader.SetGlobalVector("_LabHeightFog",new Vector4(fogBaseHeight,fogFalloff,density,fogDrift));
            // SetGlobalVector skips the sRGB->linear step SetGlobalColor does; convert explicitly.
            haze=haze.linear;Shader.SetGlobalVector("_LabHeightFogColor",new Vector4(haze.r,haze.g,haze.b,lampFogGlow));
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=background;
            RenderSettings.fogStartDistance=Mathf.Lerp(80,57,cloud);RenderSettings.fogEndDistance=Mathf.Lerp(150,99,cloud);
        }
        void OnDisable()
        {
            Shader.SetGlobalVector("_LabEnvironment",Vector4.zero);Shader.SetGlobalVector("_LabWind",Vector4.zero);
            Shader.SetGlobalFloat("_LabWeatherTime",0);Shader.SetGlobalFloat("_LabCloudCover",0);Shader.SetGlobalFloat("_LabWaterMotion",0);
            Shader.SetGlobalFloat("_LabWaterTime",0);Shader.SetGlobalVector("_LabHeightFog",Vector4.zero);
            if(cached&&sun){sun.color=originalSunColor;sun.intensity=originalIntensity;sun.transform.rotation=originalRotation;}
            if(worldCamera)worldCamera.backgroundColor=originalBackground;RenderSettings.fog=false;
            if(torches!=null)foreach(var torch in torches)if(torch){var practical=torch.GetComponent<PracticalLight>();if(practical)practical.Apply(0);else torch.intensity=2.3f;}
        }
    }
}
