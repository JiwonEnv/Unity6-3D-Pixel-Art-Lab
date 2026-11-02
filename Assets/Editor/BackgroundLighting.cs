using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelArtLab.Editor
{
    // Background lighting phases for the woodland (Docs/Background-Lighting-Design.md).
    public static class BackgroundLighting
    {
        public const string ProfilePath="Assets/Settings/PixelArtLab-Woodland-Atmosphere.asset";
        // HDR emission so flames exceed 1 and the pixel bloom has light to spread (L1).
        public const float FlameEmission=4f;

        // Phase 1: HDR flame emission, pixel-buffer bloom and vignette (L1, L6, L7).
        [MenuItem("Pixel Art Lab/Background Lighting/Apply Phase 1")]
        public static void ApplyPhase1()
        {
            var flame=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/OpenWorld/Flame.mat");
            if(flame){flame.SetColor("_EmissionColor",flame.color*FlameEmission);EditorUtility.SetDirty(flame);}

            // Rebuilt each run: effect settings must be stored as sub-assets of the profile.
            AssetDatabase.DeleteAsset(ProfilePath);
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);
            var bloom=profile.Add<Bloom>(true);
            bloom.threshold.Override(.95f);bloom.intensity.Override(1f);bloom.scatter.Override(.75f);
            bloom.highQualityFiltering.Override(false);bloom.downscale.Override(BloomDownscaleMode.Half);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.22f);vignette.smoothness.Override(.45f);
            // No tonemapping: values above 1 clip as before, so the toon palette keeps its colours.
            var tonemapping=profile.Add<Tonemapping>(true);tonemapping.mode.Override(TonemappingMode.None);
            foreach(var component in profile.components){component.hideFlags=HideFlags.HideInInspector|HideFlags.HideInHierarchy;AssetDatabase.AddObjectToAsset(component,profile);}
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();

            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var volume=Object.FindFirstObjectByType<Volume>();
            if(!volume)volume=new GameObject("Atmosphere (bloom, vignette)").AddComponent<Volume>();
            volume.isGlobal=true;volume.sharedProfile=profile;
            var camera=Object.FindFirstObjectByType<WorldEnvironment>().worldCamera;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;EditorUtility.SetDirty(data);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("BACKGROUND_LIGHTING_PHASE1_APPLIED");
        }

        // Play-mode captures of the live pixel buffer (post-processing only runs there).
        // Writes Previews/Lighting/<hour>[-flat].png and a luminance report.
        [MenuItem("Pixel Art Lab/Background Lighting/Capture Play Mode")]
        public static void Capture(){EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);SessionState.SetBool("Lighting.Capture",true);EditorApplication.isPlaying=true;}
        [InitializeOnLoadMethod]static void Resume(){if(SessionState.GetBool("Lighting.Capture",false))EditorApplication.update+=Tick;}
        static readonly (string name,float hour)[] Shots={("Noon",13),("Sunset",17.5f),("Night",0)};
        static readonly List<string> report=new List<string>();
        static int frame,shot;static bool flat;
        static void Tick()
        {
            if(!Application.isPlaying)return;
            var follow=Object.FindFirstObjectByType<ExplorationCamera>();if(!follow)return;
            var buffer=follow.GetComponent<Camera>().targetTexture;if(!buffer)return;
            var env=Object.FindFirstObjectByType<WorldEnvironment>();var volume=Object.FindFirstObjectByType<Volume>();
            env.runClock=false;env.hour=Shots[shot].hour;if(volume)volume.weight=flat?0:1;
            // Let the camera settle and the volume stack update before reading.
            if(++frame<30)return;frame=0;
            Directory.CreateDirectory("Previews/Lighting");
            var pixels=Save(buffer,$"Previews/Lighting/{Shots[shot].name}{(flat?"-no-post":"")}.png");
            var luma=pixels.Select(p=>(p.r*.2126f+p.g*.7152f+p.b*.0722f)/255).OrderBy(v=>v).ToArray();
            float mean=luma.Average(),dark=luma[(int)(luma.Length*.1f)],bright=luma[(int)(luma.Length*.95f)];
            report.Add($"{Shots[shot].name}{(flat?" (no post)":"")}: mean {mean:F3}, p10 {dark:F3}, p95 {bright:F3}, contrast p95/p10 {bright/Mathf.Max(.002f,dark):F1}");
            if(!flat){flat=true;return;}
            flat=false;if(++shot<Shots.Length)return;
            EditorApplication.update-=Tick;SessionState.SetBool("Lighting.Capture",false);
            report.Insert(0,"BACKGROUND_LIGHTING_CAPTURE");File.WriteAllLines("Previews/Lighting/Lighting-Capture.txt",report);
            EditorApplication.isPlaying=false;EditorApplication.Exit(0);
        }
        static Color32[] Save(RenderTexture rt,string path)
        {
            var previous=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(path,t.EncodeToPNG());var pixels=t.GetPixels32();Object.DestroyImmediate(t);return pixels;
        }
    }
}
