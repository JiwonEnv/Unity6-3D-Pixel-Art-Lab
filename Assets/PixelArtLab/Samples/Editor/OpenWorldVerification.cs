using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class OpenWorldVerification
    {
        static double started;
        public static void Run()
        {
            EditorSceneManager.OpenScene(OpenWorldSceneBuilder.ScenePath);SessionState.SetBool("Amberwood.Verify",true);EditorApplication.isPlaying=true;Register();
        }
        [InitializeOnLoadMethod]static void Resume(){if(SessionState.GetBool("Amberwood.Verify",false))Register();}
        static void Register(){started=EditorApplication.timeSinceStartup;EditorApplication.update-=Begin;EditorApplication.update+=Begin;}
        static void Begin()
        {
            if(EditorApplication.timeSinceStartup-started>100){EditorApplication.update-=Begin;SessionState.SetBool("Amberwood.Verify",false);Debug.LogError("Gameplay verification timeout");EditorApplication.Exit(1);return;}
            if(!Application.isPlaying||EditorApplication.timeSinceStartup-started<2)return;
            EditorApplication.update-=Begin;SessionState.SetBool("Amberwood.Verify",false);
            var runner=new GameObject("Gameplay test driver").AddComponent<ExplorationPlaytest>();
            runner.completed=(success,result)=>
            {
                var camera=Object.FindFirstObjectByType<ExplorationCamera>().GetComponent<Camera>();OpenWorldSceneBuilder.Capture(camera,"Previews/15-Amberwood-Playable.png");
                EditorApplication.isPlaying=false;EditorApplication.Exit(success?0:1);
            };
        }
    }
}
