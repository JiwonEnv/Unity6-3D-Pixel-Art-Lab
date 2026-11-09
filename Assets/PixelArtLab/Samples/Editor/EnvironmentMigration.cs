using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab.Editor
{
    // R1: moves the sample scenes from the old all-in-one WorldEnvironment onto
    // EnvironmentState + profiles + drivers, keeping every value the scenes had.
    public static class EnvironmentMigration
    {
        public const string PresetsFolder="Assets/PixelArtLab/Presets";
        public const string DefaultTimePath=PresetsFolder+"/SO_TimeOfDay_Default.asset",DefaultWeatherPath=PresetsFolder+"/SO_Weather_Default.asset";
        public const string WoodlandTimePath=BackgroundLighting.SettingsFolder+"/SO_TimeOfDay_Woodland.asset";

        [MenuItem("Pixel Art Lab/Environment/Migrate Sample Scenes")]
        public static void Run()
        {
            var time=LoadOrCreate<TimeOfDayProfile>(DefaultTimePath);var weather=LoadOrCreate<WeatherProfile>(DefaultWeatherPath);
            Migrate(OpenWorldSceneBuilder.ScenePath,time,weather);
            Migrate(DetailedWoodlandBuilder.ScenePath,WoodlandTimeOfDay(),weather);
            AssetDatabase.SaveAssets();
            Debug.Log("ENVIRONMENT_MIGRATION_SUCCESS");
        }
        // Default values plus the woodland's grading volume profiles.
        public static TimeOfDayProfile WoodlandTimeOfDay()
        {
            var time=LoadOrCreate<TimeOfDayProfile>(WoodlandTimePath);
            VolumeProfile Grade(string name)=>AssetDatabase.LoadAssetAtPath<VolumeProfile>(BackgroundLighting.SettingsFolder+"/VP_Woodland_Grade_"+name+".asset");
            if(!time.dayGrade)time.dayGrade=Grade("Day");
            if(!time.duskVermilionGrade)time.duskVermilionGrade=Grade("DuskVermilion");
            if(!time.duskPinkGrade)time.duskPinkGrade=Grade("DuskPink");
            if(!time.duskVioletGrade)time.duskVioletGrade=Grade("DuskViolet");
            if(!time.nightGrade)time.nightGrade=Grade("Night");
            EditorUtility.SetDirty(time);return time;
        }
        static void Migrate(string path,TimeOfDayProfile time,WeatherProfile weather)
        {
            EditorSceneManager.OpenScene(path);
            var env=Object.FindFirstObjectByType<WorldEnvironment>();if(!env){Debug.LogWarning("No WorldEnvironment in "+path);return;}
            var state=env.State?env.State:env.gameObject.AddComponent<EnvironmentState>();
            state.timeOfDay=time;state.weatherProfile=weather;
            var camera=Object.FindFirstObjectByType<ExplorationCamera>()?Object.FindFirstObjectByType<ExplorationCamera>().GetComponent<Camera>():Camera.main;
            env.sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).First(l=>l.type==LightType.Directional);
            env.worldCamera=camera;
            // Every point light was a torch; plain ones get PracticalLight with their old values.
            env.torches=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.type==LightType.Point).ToArray();
            if(time.dayGrade&&!env.GetComponent<TimeOfDayGrading>())env.gameObject.AddComponent<TimeOfDayGrading>();
            foreach(var name in new[]{"Grade - Day","Grade - Dusk Vermilion","Grade - Dusk Pink","Grade - Dusk Violet","Grade - Night"}){var old=GameObject.Find(name);if(old&&old.GetComponent<Volume>()&&!old.transform.parent)Object.DestroyImmediate(old);}
            foreach(var rain in Object.FindObjectsByType<PixelWeatherParticles>(FindObjectsSortMode.None)){rain.environment=state;rain.targetCamera=camera;rain.anchor=env.player?env.player.transform:null;EditorUtility.SetDirty(rain);}
            foreach(var splash in Object.FindObjectsByType<PuddleFootsteps>(FindObjectsSortMode.None)){splash.environment=state;splash.targetCamera=camera;EditorUtility.SetDirty(splash);}
            foreach(var prints in Object.FindObjectsByType<SnowFootprints>(FindObjectsSortMode.None)){prints.environment=state;EditorUtility.SetDirty(prints);}
            foreach(var air in Object.FindObjectsByType<PixelVolumetrics>(FindObjectsSortMode.None)){air.environment=state;EditorUtility.SetDirty(air);}
            EditorUtility.SetDirty(state);EditorUtility.SetDirty(env);state.Apply();
            EditorSceneManager.MarkSceneDirty(env.gameObject.scene);EditorSceneManager.SaveScene(env.gameObject.scene);
            Debug.Log($"ENVIRONMENT_MIGRATED {path}: practicals {PracticalLight.All.Count}, grading {(env.GetComponent<TimeOfDayGrading>()?"on":"off")}");
        }
        static T LoadOrCreate<T>(string path) where T:ScriptableObject
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset)return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
    }
}
