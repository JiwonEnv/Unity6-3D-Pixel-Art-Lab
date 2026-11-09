using UnityEngine;

namespace PixelArtLab
{
    // One per scene: the style asset each technique module uses. Empty slots use the built-in
    // defaults. Scenes without a set still get the defaults (EnvironmentState calls EnsurePushed).
    [ExecuteAlways,DefaultExecutionOrder(-60)]
    public sealed class SurfaceStyleSet : MonoBehaviour
    {
        public ToonRampStyle toonRamp;
        public SnowStyle snow;
        public WetnessStyle wetness;
        public PuddleStyle puddles;
        public WaterStyle water;
        public CharacterLightStyle characterLight;

        static SurfaceStyleSet active;
        static bool pushed;

        void OnEnable(){active=this;Push();}
        void OnValidate(){if(isActiveAndEnabled)Push();}
        void OnDisable(){if(active!=this)return;active=null;pushed=false;}
        public void Push()
        {
            Use(toonRamp).Push();Use(snow).Push();Use(wetness).Push();
            Use(puddles).Push();Use(water).Push();Use(characterLight).Push();
            pushed=true;
        }
        static T Use<T>(T style) where T:SurfaceStyle=>style?style:SurfaceStyle.Default<T>();

        // Re-publishes after a style asset was edited.
        public static void RefreshActive(){if(active)active.Push();}
        // Shaders read these globals even in scenes with neither a set nor an EnvironmentState.
        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        #endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void PushOnLoad(){pushed=false;EnsurePushed();}
        public static void EnsurePushed()
        {
            if(pushed)return;
            if(active){active.Push();return;}
            SurfaceStyle.Default<ToonRampStyle>().Push();SurfaceStyle.Default<SnowStyle>().Push();SurfaceStyle.Default<WetnessStyle>().Push();
            SurfaceStyle.Default<PuddleStyle>().Push();SurfaceStyle.Default<WaterStyle>().Push();SurfaceStyle.Default<CharacterLightStyle>().Push();
            pushed=true;
        }
    }
}
