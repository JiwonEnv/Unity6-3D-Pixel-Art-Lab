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
        Color originalSunColor,originalBackground;
        Quaternion originalRotation;
        float originalIntensity;
        bool cached;
        public float Daylight {get;private set;}
        public string ClockLabel=>$"{Mathf.FloorToInt(hour):00}:{Mathf.FloorToInt((hour%1)*60):00}";
        void OnEnable()
        {
            if(!sun)sun=RenderSettings.sun;
            if(!worldCamera)worldCamera=Camera.main;
            if(sun){originalSunColor=sun.color;originalIntensity=sun.intensity;originalRotation=sun.transform.rotation;cached=true;}
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
            if(sun)
            {
                bool day=elevation>0;
                sun.transform.rotation=Quaternion.Euler((hour-6)*15+(day?0:180),-35,0);
                sun.color=day?Color.Lerp(new Color(1,.95f,.84f),new Color(1,.47f,.24f),sunset):new Color(.55f,.68f,1);
                sun.color=Color.Lerp(sun.color,new Color(.82f,.88f,1),cloud*.8f);
                sun.intensity=Mathf.Lerp(.52f,1.05f,Daylight)*(1-cloud*.38f);
            }
            Color ambient=Color.Lerp(new Color(.55f,.68f,1),new Color(.95f,.97f,1),Daylight);
            ambient=Color.Lerp(ambient,new Color(.62f,.72f,.84f),cloud*.45f);
            Shader.SetGlobalVector("_LabEnvironment",new Vector4(1,Daylight,wetness,snowCover));
            Shader.SetGlobalColor("_LabAmbientTint",ambient);
            float gust=windEnabled?windStrength*(1+rain*.65f+snow*.25f):0;
            Shader.SetGlobalVector("_LabWind",new Vector4(windDirection.x,windDirection.y,gust,0));
            Shader.SetGlobalFloat("_LabWeatherTime",Mathf.Floor(animationTime*12)/12);
            Shader.SetGlobalFloat("_LabWaterTime",animationTime);
            Shader.SetGlobalFloat("_LabWaterMotion",waterEnabled?1:0);
            Shader.SetGlobalFloat("_LabCloudCover",Mathf.Clamp01(.12f+cloud));
            if(torches!=null)foreach(var torch in torches)if(torch)torch.intensity=Mathf.Lerp(4.0f,2.1f,Daylight);
            Color background=Color.Lerp(new Color(.035f,.055f,.105f),new Color(.32f,.43f,.48f),Daylight);
            background=Color.Lerp(background,new Color(.35f,.41f,.47f),cloud*.55f);
            if(worldCamera)worldCamera.backgroundColor=background;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=background;
            RenderSettings.fogStartDistance=Mathf.Lerp(80,57,cloud);RenderSettings.fogEndDistance=Mathf.Lerp(150,99,cloud);
        }
        void OnDisable()
        {
            Shader.SetGlobalVector("_LabEnvironment",Vector4.zero);Shader.SetGlobalVector("_LabWind",Vector4.zero);
            Shader.SetGlobalFloat("_LabWeatherTime",0);Shader.SetGlobalFloat("_LabCloudCover",0);Shader.SetGlobalFloat("_LabWaterMotion",0);
            Shader.SetGlobalFloat("_LabWaterTime",0);
            if(cached&&sun){sun.color=originalSunColor;sun.intensity=originalIntensity;sun.transform.rotation=originalRotation;}
            if(worldCamera)worldCamera.backgroundColor=originalBackground;RenderSettings.fog=false;
            if(torches!=null)foreach(var torch in torches)if(torch)torch.intensity=2.3f;
        }
    }
}
