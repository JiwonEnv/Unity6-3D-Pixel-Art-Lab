using UnityEngine;

namespace PixelArtLab
{
    // A placed light source (lantern, lamp) driven by WorldEnvironment's time of day.
    // Its visible glass glows in step with the light so lit and unlit lamps both read.
    [DisallowMultipleComponent,RequireComponent(typeof(Light))]
    public sealed class PracticalLight : MonoBehaviour
    {
        public float dayIntensity,nightIntensity=2.5f;
        [ColorUsage(false,true)]public Color glowColor=new Color(2.2f,1.45f,.8f);
        public Renderer[] glow;
        MaterialPropertyBlock block;
        public void Apply(float daylight)
        {
            var light=GetComponent<Light>();light.intensity=Mathf.Lerp(nightIntensity,dayIntensity,daylight);light.enabled=light.intensity>.01f;
            if(glow==null)return;if(block==null)block=new MaterialPropertyBlock();
            var emission=glowColor*Mathf.Clamp01(light.intensity/Mathf.Max(.001f,nightIntensity));
            foreach(var r in glow)if(r){r.GetPropertyBlock(block);block.SetColor("_EmissionColor",emission);r.SetPropertyBlock(block);}
        }
    }
}
