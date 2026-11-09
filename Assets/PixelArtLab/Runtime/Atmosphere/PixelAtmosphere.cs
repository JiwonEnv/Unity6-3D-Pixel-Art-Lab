using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelArtLab
{
    // URP Volume override for the atmosphere: place local volumes for a misty hollow or a clear
    // ridge and the values blend as the camera moves. Unchecked values keep the camera's
    // PixelVolumetrics settings and the TimeOfDayProfile height fog.
    [Serializable,VolumeComponentMenu("Pixel Art Lab/Pixel Atmosphere")]
    public sealed class PixelAtmosphere : VolumeComponent
    {
        [Header("Light shafts (PixelVolumetrics)")]
        public ClampedFloatParameter airDensity=new ClampedFloatParameter(.01f,0,.1f);
        public ClampedFloatParameter sunStrength=new ClampedFloatParameter(.08f,0,1);
        public ClampedFloatParameter moonScatter=new ClampedFloatParameter(.25f,0,1);
        public ClampedFloatParameter lampStrength=new ClampedFloatParameter(.15f,0,1);
        public ClampedFloatParameter lampGlowReach=new ClampedFloatParameter(.6f,0,1);
        public ClampedFloatParameter bandStep=new ClampedFloatParameter(.035f,0,.2f);
        public MinFloatParameter mediumTop=new MinFloatParameter(8,0);
        [Header("Height fog (multiplies the time-of-day fog)")]
        public MinFloatParameter heightFogDensity=new MinFloatParameter(1,0);
        [Tooltip("Meters added to the height where the fog is thickest.")]
        public FloatParameter heightFogLift=new FloatParameter(0);

        // Own stack, updated per camera: URP skips its volume update for scripted render
        // requests, and this way every camera (incl. reflection captures) blends its own zone.
        static VolumeStack stack;
        public static PixelAtmosphere ForCamera(Camera camera)
        {
            if(!camera||!VolumeManager.instance.isInitialized)return null;
            // The pipeline disposes created stacks when it is rebuilt (entering play mode, settings change).
            if(stack==null||!stack.isValid)stack=VolumeManager.instance.CreateStack();
            camera.TryGetComponent<UniversalAdditionalCameraData>(out var data);
            LayerMask mask=data?data.volumeLayerMask:(LayerMask)1;
            VolumeManager.instance.Update(stack,data&&data.volumeTrigger?data.volumeTrigger:camera.transform,mask);
            return stack.GetComponent<PixelAtmosphere>();
        }
        // The override's value where a volume sets it, otherwise the caller's own value.
        public static float Pick(VolumeParameter<float> parameter,float fallback)=>parameter!=null&&parameter.overrideState?parameter.value:fallback;
        static readonly int zoneId=Shader.PropertyToID("_LabHeightFogZone");
        // Height fog zone for the camera about to render (zero = no change; see LabHeightFog.hlsl).
        public static void PushHeightFogZone(PixelAtmosphere atmosphere)
        {
            Shader.SetGlobalVector(zoneId,atmosphere?new Vector4(Pick(atmosphere.heightFogDensity,1)-1,Pick(atmosphere.heightFogLift,0),0,0):Vector4.zero);
        }
    }
}
