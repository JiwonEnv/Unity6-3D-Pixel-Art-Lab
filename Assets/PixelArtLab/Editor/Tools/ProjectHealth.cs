using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    // Project-wide checks to run after moving or renaming assets.
    public static class ProjectHealth
    {
        // Lists components whose script can no longer be found, in every scene under Assets.
        [MenuItem("Pixel Art Lab/Tools/Check Missing Scripts")]
        public static void CheckMissingScripts()
        {
            var report=new List<string>();
            foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                int missing=0;
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        int count=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                        if(count>0){missing+=count;report.Add($"  {path}: {GetPath(t)} ({count})");}
                    }
                Debug.Log($"PROJECT_HEALTH {path}: {missing} missing script(s)");
            }
            int total=report.Count;
            report.Insert(0,total==0?"PROJECT_HEALTH_SUCCESS no missing scripts":$"PROJECT_HEALTH_FAILED {total} object(s) with missing scripts");
            Directory.CreateDirectory("Previews");File.WriteAllLines("Previews/Project-Health.txt",report);
            Debug.Log(string.Join("\n",report));
            if(Application.isBatchMode&&total>0)EditorApplication.Exit(1);
        }
        static string GetPath(Transform t)=>t.parent?GetPath(t.parent)+"/"+t.name:t.name;
    }
}
