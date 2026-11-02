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
            var volume=AtmosphereVolume();
            if(!volume)volume=new GameObject("Atmosphere (bloom, vignette)").AddComponent<Volume>();
            volume.isGlobal=true;volume.sharedProfile=profile;
            var camera=Object.FindFirstObjectByType<WorldEnvironment>().worldCamera;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;EditorUtility.SetDirty(data);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("BACKGROUND_LIGHTING_PHASE1_APPLIED");
        }

        // Phase 2: practical lights around the camp and along the shore (L3).
        const string PracticalRoot="Practical lights";
        static readonly Color Warm=new Color(1,.66f,.36f),Cool=new Color(.72f,.88f,1);
        struct Lamp{public string name;public Vector2 xz;public bool post;public Color color;public float range,night;}
        static readonly Lamp[] Lamps=
        {
            // Camp: path entrances, tent fronts, and a candle lantern on the crate.
            new Lamp{name="Camp path lantern south",xz=new Vector2(-2.5f,-4.5f),post=true,color=Warm,range=4.5f,night=2.6f},
            new Lamp{name="Camp path lantern east",xz=new Vector2(3.5f,-1.5f),post=true,color=Warm,range=4.5f,night=2.6f},
            new Lamp{name="Large tent lantern",xz=new Vector2(-1.8f,6.5f),post=true,color=Warm,range=4f,night=2.2f},
            new Lamp{name="Small tent lantern",xz=new Vector2(2.4f,7.2f),post=true,color=Warm,range=4f,night=2.2f},
            new Lamp{name="Crate candle lantern",xz=new Vector2(-4,1),post=false,color=Warm,range=3,night=1.6f},
            // Shore: two warm path lanterns and one cool fishing lamp by the canoe.
            new Lamp{name="Shore lantern east",xz=new Vector2(-18.2f,10.4f),post=true,color=Warm,range=4.5f,night=2.6f},
            new Lamp{name="Shore lantern west",xz=new Vector2(-23.8f,8),post=true,color=Warm,range=4.5f,night=2.6f},
            new Lamp{name="Canoe fishing lamp",xz=new Vector2(-18,19.5f),post=true,color=Cool,range=5,night=2f},
        };
        [MenuItem("Pixel Art Lab/Background Lighting/Apply Phase 2")]
        public static void ApplyPhase2()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var env=Object.FindFirstObjectByType<WorldEnvironment>();
            // Rebuilt each run.
            var old=GameObject.Find(PracticalRoot);
            if(old){var oldLights=old.GetComponentsInChildren<Light>(true);env.torches=env.torches.Where(t=>t&&!oldLights.Contains(t)).ToArray();Object.DestroyImmediate(old);}
            var root=new GameObject(PracticalRoot).transform;
            var wood=LampMaterial("Lantern-Wood",new Color(.36f,.25f,.18f));var iron=LampMaterial("Lantern-Iron",new Color(.2f,.18f,.17f));
            int id=Object.FindObjectsByType<PixelArtObject>(FindObjectsSortMode.None).Max(d=>d.objectId)+1;
            var lights=new List<Light>();
            foreach(var lamp in Lamps)
            {
                // Posts stand on the ground; prop lanterns sit on whatever is below (the crate).
                var from=new Vector3(lamp.xz.x,60,lamp.xz.y);
                if(!Physics.Raycast(from,Vector3.down,out var hit,120,~((1<<8)|(1<<4)|(1<<13)),QueryTriggerInteraction.Ignore)){Debug.LogWarning("No surface under "+lamp.name);continue;}
                if(lamp.post&&hit.collider.GetComponent<MeshRenderer>()&&!hit.collider.GetComponent<MeshRenderer>().sharedMaterials.Any(m=>m&&m.HasProperty("_Puddles")))Debug.LogWarning(lamp.name+" post lands on "+hit.collider.name);
                var g=new GameObject(lamp.name);g.transform.SetParent(root);g.transform.position=hit.point;
                var glassMaterial=LampMaterial("Lantern-Glass-"+(lamp.color==Cool?"Cool":"Warm"),Color.Lerp(lamp.color,Color.white,.35f));
                Vector3 center;
                if(lamp.post){Part(g,"Post",new Vector3(0,.85f,0),new Vector3(.12f,1.7f,.12f),wood,true);Part(g,"Arm",new Vector3(.18f,1.62f,0),new Vector3(.36f,.07f,.07f),wood,false);center=new Vector3(.3f,1.36f,0);}
                else center=new Vector3(0,.2f,0);
                Part(g,"Cap",center+Vector3.up*.17f,new Vector3(.26f,.06f,.26f),iron,false);Part(g,"Base",center-Vector3.up*.16f,new Vector3(.24f,.05f,.24f),iron,false);
                var glass=Part(g,"Glass",center,new Vector3(.17f,.26f,.17f),glassMaterial,false);
                // The light sits inside the glass, so the glass must not shadow it.
                glass.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var lightObject=new GameObject("Light");lightObject.transform.SetParent(g.transform,false);lightObject.transform.localPosition=center;
                var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.color=lamp.color;light.range=lamp.range;light.renderMode=LightRenderMode.ForcePixel;light.shadows=LightShadows.None;
                var practical=lightObject.AddComponent<PracticalLight>();practical.nightIntensity=lamp.night;practical.dayIntensity=0;practical.glowColor=lamp.color*2.2f; // above ~2.5 the glass clips to white and loses its huepractical.glow=new[]{glass.GetComponent<Renderer>()};
                var data=g.AddComponent<PixelArtObject>();data.objectId=id++;data.interior=.05f;data.Apply();
                lights.Add(light);
            }
            env.torches=env.torches.Where(t=>t).Concat(lights).ToArray();EditorUtility.SetDirty(env);env.Apply();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log($"BACKGROUND_LIGHTING_PHASE2_APPLIED lamps={lights.Count} practicals={env.torches.Length}");
        }
        static Material LampMaterial(string name,Color color)
        {
            string path="Assets/Art/Atmosphere/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("PixelArtLab/Sprite Toon"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetColor("_ShadeColor",new Color(.42f,.42f,.56f));m.SetFloat("_LocalLightStrength",1.2f);EditorUtility.SetDirty(m);return m;
        }
        static GameObject Part(GameObject parent,string name,Vector3 local,Vector3 scale,Material material,bool solid)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent.transform,false);g.transform.localPosition=local;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(g.GetComponent<Collider>());return g;
        }

        // Phase 4: pixel-resolution light shafts and lamp glow in the air (L5).
        [MenuItem("Pixel Art Lab/Background Lighting/Apply Phase 4")]
        public static void ApplyPhase4()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var env=Object.FindFirstObjectByType<WorldEnvironment>();
            var air=env.worldCamera.GetComponent<PixelVolumetrics>();if(!air)air=env.worldCamera.gameObject.AddComponent<PixelVolumetrics>();
            air.shader=Shader.Find("Hidden/PixelArtLab/Volumetrics");air.environment=env;
            // Tuned values, written explicitly so re-running reproduces them.
            air.steps=24;air.airDensity=.01f;air.mediumTop=8;air.maxMarch=30;air.sunStrength=.08f;air.lampStrength=.15f;air.lampGlowReach=.6f;air.moonScatter=.25f;air.bandStep=.035f;
            EditorUtility.SetDirty(air);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("BACKGROUND_LIGHTING_PHASE4_APPLIED "+(air.shader?air.shader.name:"missing shader"));
        }

        // The bloom/vignette volume, told apart from the grading volumes by its profile.
        static Volume AtmosphereVolume()=>Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).FirstOrDefault(v=>v.sharedProfile&&AssetDatabase.GetAssetPath(v.sharedProfile)==ProfilePath);

        // Phase 5: time-of-day colour grading volumes blended by WorldEnvironment (L7).
        [MenuItem("Pixel Art Lab/Background Lighting/Apply Phase 5")]
        public static void ApplyPhase5()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var env=Object.FindFirstObjectByType<WorldEnvironment>();
            env.dayGrade=GradeVolume("Grade - Day",profile=>
            {
                var white=profile.Add<WhiteBalance>(true);white.temperature.Override(4);
                var adjust=profile.Add<ColorAdjustments>(true);adjust.saturation.Override(6);adjust.contrast.Override(4);
            });
            // Dusk runs vermilion -> pink -> violet as the sun sinks (WorldEnvironment blends them).
            env.duskVermilionGrade=GradeVolume("Grade - Dusk Vermilion",profile=>
            {
                var white=profile.Add<WhiteBalance>(true);white.temperature.Override(25);white.tint.Override(6);
                var split=profile.Add<SplitToning>(true);split.shadows.Override(new Color(.55f,.30f,.35f));split.highlights.Override(new Color(1,.5f,.25f));split.balance.Override(-10);
                var adjust=profile.Add<ColorAdjustments>(true);adjust.saturation.Override(14);
            });
            env.duskPinkGrade=GradeVolume("Grade - Dusk Pink",profile=>
            {
                var white=profile.Add<WhiteBalance>(true);white.temperature.Override(12);white.tint.Override(22);
                var split=profile.Add<SplitToning>(true);split.shadows.Override(new Color(.50f,.32f,.55f));split.highlights.Override(new Color(1,.6f,.72f));split.balance.Override(-5);
                var adjust=profile.Add<ColorAdjustments>(true);adjust.saturation.Override(10);
            });
            env.duskVioletGrade=GradeVolume("Grade - Dusk Violet",profile=>
            {
                var white=profile.Add<WhiteBalance>(true);white.temperature.Override(-6);white.tint.Override(18);
                var split=profile.Add<SplitToning>(true);split.shadows.Override(new Color(.36f,.28f,.60f));split.highlights.Override(new Color(.85f,.6f,.95f));split.balance.Override(5);
                var adjust=profile.Add<ColorAdjustments>(true);adjust.saturation.Override(4);
            });
            // Night: cool teal shadows; highlights stay warm so lamps keep their colour.
            env.nightGrade=GradeVolume("Grade - Night",profile=>
            {
                var white=profile.Add<WhiteBalance>(true);white.temperature.Override(-18);white.tint.Override(-4);
                var split=profile.Add<SplitToning>(true);split.shadows.Override(new Color(.22f,.40f,.48f));split.highlights.Override(new Color(1,.72f,.46f));split.balance.Override(20);
                var adjust=profile.Add<ColorAdjustments>(true);adjust.saturation.Override(-4);adjust.contrast.Override(6);
            });
            // The single golden-hour grade was replaced by the three dusk grades.
            var oldGolden=GameObject.Find("Grade - Golden Hour");if(oldGolden)Object.DestroyImmediate(oldGolden);
            AssetDatabase.DeleteAsset("Assets/Settings/PixelArtLab-Woodland-Grade-GoldenHour.asset");
            EditorUtility.SetDirty(env);env.Apply();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log("BACKGROUND_LIGHTING_PHASE5_APPLIED");
        }
        static Volume GradeVolume(string name,System.Action<VolumeProfile> build)
        {
            // Rebuilt each run; effect settings live as sub-assets of the profile file.
            string path="Assets/Settings/PixelArtLab-Woodland-"+name.Replace("Grade - ","Grade-").Replace(" ","")+".asset";
            AssetDatabase.DeleteAsset(path);
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);build(profile);
            foreach(var component in profile.components){component.hideFlags=HideFlags.HideInInspector|HideFlags.HideInHierarchy;AssetDatabase.AddObjectToAsset(component,profile);}
            EditorUtility.SetDirty(profile);
            var existing=GameObject.Find(name);if(existing)Object.DestroyImmediate(existing);
            var volume=new GameObject(name).AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;volume.sharedProfile=profile;return volume;
        }

        // Play-mode captures of the live pixel buffer (post-processing only runs there).
        // Writes Previews/Lighting/<shot>[-no-post|-no-fog|-no-volumetrics|-no-grading].png, a luminance report and frame times.
        [MenuItem("Pixel Art Lab/Background Lighting/Capture Play Mode")]
        public static void Capture(){EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);SessionState.SetBool("Lighting.Capture",true);EditorApplication.isPlaying=true;}
        [InitializeOnLoadMethod]static void Resume(){if(SessionState.GetBool("Lighting.Capture",false))EditorApplication.update+=Tick;}
        static readonly (string name,float hour,bool shore)[] Shots={("Morning",8,false),("Noon",13,false),("Dusk-Vermilion",17.1f,false),("Dusk-Pink",17.8f,false),("Dusk-Violet",18.5f,false),("Night",0,false),("Night-Shore",0,true),("Dusk-Pink-Shore",17.8f,true)};
        static readonly List<string> report=new List<string>();
        static readonly List<float> frameTimes=new List<float>();
        static int frame,shot,timing,variant;static double timingStart;
        static void Tick()
        {
            if(!Application.isPlaying)return;
            var follow=Object.FindFirstObjectByType<ExplorationCamera>();if(!follow)return;
            var buffer=follow.GetComponent<Camera>().targetTexture;if(!buffer)return;
            var env=Object.FindFirstObjectByType<WorldEnvironment>();var volume=AtmosphereVolume();
            env.runClock=false;
            if(shot<Shots.Length)
            {
                // Variants: 0 full, 1 no post-processing, 2 no height fog, 3 no volumetrics, 4 no grading.
                env.hour=Shots[shot].hour;if(volume)volume.weight=variant==1?0:1;env.heightFog=variant!=2;env.colorGrading=variant!=4&&variant!=1;
                var air=follow.GetComponent<PixelVolumetrics>();if(air)air.enabled=variant!=3;
                if(frame==0){var at=Shots[shot].shore?new Vector3(-17,1,12):env.player.spawnPoint;if((env.player.transform.position-at).sqrMagnitude>1){env.player.Teleport(at);follow.lookOffset=Shots[shot].shore?new Vector3(-6,0,7):Vector3.zero;follow.SnapToPlayer();}}
                // Let the camera settle and the volume stack update before reading.
                if(++frame<30)return;frame=0;
                Directory.CreateDirectory("Previews/Lighting");
                string suffix=variant==1?"-no-post":variant==2?"-no-fog":variant==3?"-no-volumetrics":variant==4?"-no-grading":"";
                var pixels=Save(buffer,$"Previews/Lighting/{Shots[shot].name}{suffix}.png");
                var luma=pixels.Select(p=>(p.r*.2126f+p.g*.7152f+p.b*.0722f)/255).OrderBy(v=>v).ToArray();
                float mean=luma.Average(),dark=luma[(int)(luma.Length*.1f)],bright=luma[(int)(luma.Length*.95f)];
                report.Add($"{Shots[shot].name}{suffix}: mean {mean:F3}, p10 {dark:F3}, p95 {bright:F3}, contrast p95/p10 {bright/Mathf.Max(.002f,dark):F1}");
                if(++variant<5)return;
                variant=0;shot++;env.heightFog=true;env.colorGrading=true;return;
            }
            // Frame time at the night camp with the placed practical lights off, then on (uncapped).
            Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;env.hour=0;if(volume)volume.weight=1;
            var practicals=GameObject.Find(PracticalRoot);
            if(timing<2)
            {
                if(frameTimes.Count==0&&frame==0){if(practicals)practicals.SetActive(timing==1);timingStart=EditorApplication.timeSinceStartup;}
                if(++frame<20)return; // settle after toggling
                frameTimes.Add(Time.unscaledDeltaTime*1000);
                if(EditorApplication.timeSinceStartup-timingStart<4)return;
                var sorted=frameTimes.OrderBy(t=>t).ToList();
                report.Add($"Frame time, night camp, placed practicals {(timing==1?"on":"off")}: median {sorted[sorted.Count/2]:F2} ms, p90 {sorted[(int)(sorted.Count*.9f)]:F2} ms ({sorted.Count} samples)");
                frameTimes.Clear();frame=0;timing++;return;
            }
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
