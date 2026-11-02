using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PixelArtLab.Editor
{
    public static class CityMetroidvaniaVerification
    {
        static double deadline;
        public static void Play(){EditorSceneManager.OpenScene(CityMetroidvaniaBuilder.ScenePath);SessionState.SetBool("City.Verify",true);EditorApplication.isPlaying=true;}
        [InitializeOnLoadMethod]static void Resume(){if(SessionState.GetBool("City.Verify",false)){deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Begin;}}
        static void Begin()
        {
            if(EditorApplication.timeSinceStartup>deadline){Debug.LogError("City playtest startup timeout");EditorApplication.Exit(1);return;}
            if(!Application.isPlaying||!UnityEngine.Object.FindFirstObjectByType<CityMetroidvaniaWorld>())return;EditorApplication.update-=Begin;SessionState.SetBool("City.Verify",false);
            var test=new GameObject("City Metroidvania traversal verification").AddComponent<CityMetroidvaniaPlaytest>();test.completed=(success,result)=>{EditorApplication.isPlaying=false;EditorApplication.Exit(success?0:1);};
        }
        public static void Inspect()
        {
            EditorSceneManager.OpenScene(CityMetroidvaniaBuilder.ScenePath);var world=UnityEngine.Object.FindFirstObjectByType<CityMetroidvaniaWorld>();var env=world.environment;
            if(!world.player.sideScrolling||!world.follow.sideView||world.player.doubleJumpUnlocked||world.player.dashUnlocked)throw new Exception("Invalid initial progression state");
            if(ShaderUtil.GetShaderMessages(Shader.Find("PixelArtLab/Sprite Toon")).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("City shader error");
            int commercial=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Count(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("KenneyCityCommercial"));int industrial=UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Count(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("KenneyCityIndustrial"));if(commercial<15||industrial<10)throw new Exception("Imported city asset instances missing");
            var outdoor=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CityMetroidvania/Rain-worn concrete.mat");var indoor=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/CityMetroidvania/Sheltered service deck.mat");
            if(outdoor.GetFloat("_Puddles")!=1||indoor.GetFloat("_SurfaceWeatherExposure")!=0)throw new Exception("Weather exposure invalid");
            if(!UnityEngine.Object.FindFirstObjectByType<PlayerVisibility>()||!UnityEngine.Object.FindFirstObjectByType<SnowFootprints>()||!UnityEngine.Object.FindFirstObjectByType<PuddleFootsteps>())throw new Exception("Existing rendering features not wired");
            env.hour=13;env.SetWeather(ValleyWeather.Clear,true);env.worldCamera.transform.position=new Vector3(0,13,0)-env.worldCamera.transform.forward*100;env.worldCamera.orthographicSize=37;env.worldCamera.aspect=640f/360;OpenWorldSceneBuilder.Capture(env.worldCamera,"Previews/City-Overview.png");
            File.WriteAllText("Previews/City-Scene-Verification.txt",$"CITY_SCENE_SUCCESS\nCommercial mesh instances: {commercial}\nIndustrial mesh instances: {industrial}\nSide movement and camera; dash / air step initially locked: PASS\nOutdoor wetness / snow and sheltered drainway: PASS\nRim, reveal, scan, puddle ripples and snow footprints connected: PASS\nNO PLAYER BUILD\n");Debug.Log("CITY_SCENE_SUCCESS");
        }
    }
}
