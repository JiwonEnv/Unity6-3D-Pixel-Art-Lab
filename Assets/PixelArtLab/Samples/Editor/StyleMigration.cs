using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    // R3: the look constants that lived in the Lab*.hlsl modules became style assets. Creates the
    // default presets (same values as before) and gives each sample scene a SurfaceStyleSet.
    public static class StyleMigration
    {
        const string Folder=EnvironmentMigration.PresetsFolder;

        [MenuItem("Pixel Art Lab/Style/Migrate Sample Scenes (R3)")]
        public static void Run()
        {
            foreach(var path in new[]{OpenWorldSceneBuilder.ScenePath,DetailedWoodlandBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);
                var state=Object.FindFirstObjectByType<EnvironmentState>();if(!state){Debug.LogWarning("No EnvironmentState in "+path);continue;}
                var set=state.GetComponent<SurfaceStyleSet>();if(!set)set=state.gameObject.AddComponent<SurfaceStyleSet>();
                set.toonRamp=Preset<ToonRampStyle>("SO_ToonRamp_Default");set.snow=Preset<SnowStyle>("SO_Snow_Default");
                set.wetness=Preset<WetnessStyle>("SO_Wetness_Default");set.puddles=Preset<PuddleStyle>("SO_Puddles_Default");
                set.water=Preset<WaterStyle>("SO_Water_Default");set.characterLight=Preset<CharacterLightStyle>("SO_CharacterLight_Default");
                EditorUtility.SetDirty(set);set.Push();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("STYLE_MIGRATION_SUCCESS");
        }
        static T Preset<T>(string name) where T:SurfaceStyle
        {
            string path=Folder+"/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset)return asset;
            Directory.CreateDirectory(Folder);asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);return asset;
        }
    }
}
