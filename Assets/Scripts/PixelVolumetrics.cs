using UnityEngine;

namespace PixelArtLab
{
    // Settings for the pixel-resolution light shafts (Docs/Background-Lighting-Design.md L5).
    // PixelArtLabRendererFeature runs the pass only on cameras that carry this component.
    [ExecuteAlways,RequireComponent(typeof(Camera))]
    public sealed class PixelVolumetrics : MonoBehaviour
    {
        public Shader shader;
        public WorldEnvironment environment;
        [Range(4,32)]public int steps=24;
        [Range(0,.1f)]public float airDensity=.01f;
        public float mediumTop=8,maxMarch=30;
        [Range(0,1)]public float sunStrength=.08f,lampStrength=.15f,lampGlowReach=.6f;
        // Once the sun is down, moonlight scatters at this share of the sun's strength. Full
        // strength lit a flat band of low shore haze at night; lamps should lead there instead.
        [Range(0,1)]public float moonScatter=.25f;
        [Tooltip("Brightness step of the light bands; 0 keeps the shafts smooth.")]
        [Range(0,.2f)]public float bandStep=.035f;
        const int MaxLights=16;
        readonly Vector4[] lights=new Vector4[MaxLights],colors=new Vector4[MaxLights];
        Material material;
        internal Material Prepare(int width,int height)
        {
            if(!shader||!shader.isSupported)return null;
            if(!material)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            int count=0;
            if(environment&&environment.torches!=null)foreach(var light in environment.torches)
            {
                if(count==MaxLights)break;if(!light||!light.enabled||light.intensity<=.01f)continue;
                // Air glow covers 60% of the light's reach, so halos read lantern-sized.
                float reach=light.range*lampGlowReach;
                lights[count]=new Vector4(light.transform.position.x,light.transform.position.y,light.transform.position.z,1/Mathf.Max(.01f,reach*reach));
                colors[count++]=light.color.linear*light.intensity;
            }
            material.SetVectorArray("_LabVolumeLights",lights);material.SetVectorArray("_LabVolumeLightColors",colors);material.SetInt("_LabVolumeLightCount",count);
            material.SetVector("_LabVolumetric",new Vector4(airDensity,mediumTop,maxMarch,steps));
            // Lamp halos fade to a quarter in full daylight; torches stay lit by day.
            float daylight=environment?environment.Daylight:1;
            material.SetVector("_LabVolumetricStrength",new Vector4(sunStrength*Mathf.Lerp(moonScatter,1,Mathf.Clamp01(daylight*10)),lampStrength*Mathf.Lerp(1,.25f,daylight),bandStep,0));
            material.SetVector("_LabVolumetricTexel",new Vector4(1f/width,1f/height,width,height));
            return material;
        }
        void OnDisable(){if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }
}
