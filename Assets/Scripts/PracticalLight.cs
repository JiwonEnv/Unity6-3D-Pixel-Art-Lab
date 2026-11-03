using UnityEngine;

namespace PixelArtLab
{
    // A placed light source (lantern, lamp, campfire) driven by WorldEnvironment's time of day.
    // Its visible glass or flame glows in step with the light so lit and unlit lamps both read.
    [DisallowMultipleComponent,RequireComponent(typeof(Light))]
    public sealed class PracticalLight : MonoBehaviour
    {
        public float dayIntensity,nightIntensity=2.5f;
        [ColorUsage(false,true)]public Color glowColor=new Color(2.2f,1.45f,.8f);
        public Renderer[] glow;
        // Flame flicker: brightness wobbles by up to this share and, in play mode, the light
        // drifts by up to `wobble` metres so shadows and air glow sway with it. The pattern
        // advances in steps (`flickerRate` per second) like a hand-drawn sprite loop.
        [Range(0,.5f)]public float flicker=.25f;
        [Range(1,24)]public float flickerRate=8;
        [Range(0,.15f)]public float wobble=.02f;
        MaterialPropertyBlock block;
        Vector3 restPosition,restWorld;bool rested;
        public void Apply(float daylight,float time=0)
        {
            var light=GetComponent<Light>();
            // Seed from the world rest position so each lamp flickers out of phase with the
            // others (the light's local offset is the same inside every lantern).
            if(!rested){restPosition=transform.localPosition;restWorld=transform.position;rested=true;}
            float seed=Mathf.Repeat(restWorld.x*12.9f+restWorld.z*7.3f,100);
            float step=Mathf.Floor(time*flickerRate)/flickerRate;
            float n=(Mathf.PerlinNoise(seed,step*1.7f)*2-1)*.7f+(Mathf.PerlinNoise(seed+31,step*4.3f)*2-1)*.3f;
            float pulse=1+flicker*n;
            light.intensity=Mathf.Lerp(nightIntensity,dayIntensity,daylight)*pulse;light.enabled=light.intensity>.01f;
            // Moving the light only in play mode keeps the scene file unchanged while editing.
            if(Application.isPlaying)transform.localPosition=restPosition+new Vector3(Mathf.PerlinNoise(seed+7,step*2.1f)-.5f,Mathf.PerlinNoise(seed+13,step*2.6f)-.5f,Mathf.PerlinNoise(seed+19,step*1.9f)-.5f)*(wobble*2);
            if(glow==null)return;if(block==null)block=new MaterialPropertyBlock();
            var emission=glowColor*Mathf.Clamp(light.intensity/Mathf.Max(.001f,nightIntensity),0,1.5f);
            foreach(var r in glow)if(r){r.GetPropertyBlock(block);block.SetColor("_EmissionColor",emission);r.SetPropertyBlock(block);}
        }
    }
}
