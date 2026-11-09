using System.Collections.Generic;
using UnityEngine;

namespace PixelArtLab
{
    // Drives every PracticalLight in the scene from the time of day (brightness, flicker) and
    // gives point shadows only to the few nearest a focus point, usually the player.
    public sealed class PracticalLightManager : EnvironmentDriver
    {
        [Tooltip("Shadows go to the lights nearest this transform. Empty: no point shadows.")]
        public Transform focus;
        [Tooltip("Point shadows cost a cube map each (512px).")]
        [Range(0,8)]public int shadowedLights=4;
        readonly List<PracticalLight> ordered=new List<PracticalLight>();
        protected override void OnApplied(EnvironmentState s)
        {
            ordered.Clear();ordered.AddRange(PracticalLight.All);
            if(focus){var at=focus.position;ordered.Sort((a,b)=>(a.transform.position-at).sqrMagnitude.CompareTo((b.transform.position-at).sqrMagnitude));}
            int shadowed=0;
            foreach(var practical in ordered)
            {
                if(!practical)continue;
                practical.Apply(s.Daylight,s.animationTime);
                var light=practical.Light;
                bool nearby=focus&&light.enabled&&Vector3.Distance(focus.position,light.transform.position)<light.range+2;
                bool cast=nearby&&shadowed<shadowedLights;if(cast)shadowed++;
                light.shadows=cast?LightShadows.Hard:LightShadows.None;light.shadowStrength=.85f;light.shadowBias=.025f;light.shadowNormalBias=.1f;light.shadowCustomResolution=512;
            }
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            foreach(var practical in PracticalLight.All)if(practical)practical.Apply(0);
        }
    }
}
