using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    // R2: Sprite Toon features became material keywords. Water was recognised only by the
    // renderer's wind mode, so lake materials get the new Water Surface toggle from the scenes,
    // then every Sprite Toon material syncs its keywords from its values.
    public static class ShaderModuleMigration
    {
        [MenuItem("Pixel Art Lab/Materials/Migrate Sample Materials (R2)")]
        public static void Run()
        {
            int lakes=0;
            foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                foreach(var motion in Object.FindObjectsByType<WorldSurfaceMotion>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(m=>m.kind==WorldMotionKind.Water))
                    foreach(var renderer in motion.GetComponentsInChildren<MeshRenderer>(true))
                        foreach(var material in renderer.sharedMaterials.Where(SpriteToonKeywords.IsSpriteToon))
                        {
                            if(!AssetDatabase.Contains(material)){Debug.LogWarning($"Scene-embedded water material {material.name} in {path}");continue;}
                            material.SetFloat("_WaterSurface",1);EditorUtility.SetDirty(material);lakes++;
                        }
            }
            SpriteToonGUI.SyncAll();
            Debug.Log($"SHADER_MODULE_MIGRATION_SUCCESS water materials {lakes}");
        }
    }
}
