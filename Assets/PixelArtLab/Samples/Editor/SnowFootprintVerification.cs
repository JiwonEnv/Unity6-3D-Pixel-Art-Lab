using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PixelArtLab.Editor
{
    public static class SnowFootprintVerification
    {
        static int stage,baseline;static float started;static double deadline;static WorldEnvironment env;static SnowFootprints prints;static ExplorerMotor player;
        public static void Play(){EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);SessionState.SetBool("SnowPrint.Verify",true);EditorApplication.isPlaying=true;}
        [InitializeOnLoadMethod]static void Resume(){if(SessionState.GetBool("SnowPrint.Verify",false)){stage=0;deadline=EditorApplication.timeSinceStartup+60;EditorApplication.update+=Tick;}}
        static void Require(bool c,string message){if(!c)throw new Exception(message);}
        static void Tick()
        {
            if(!Application.isPlaying)return;
            try
            {
                Require(EditorApplication.timeSinceStartup<deadline,"Snow test timeout stage "+stage);
                if(stage==0)
                {
                    env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();prints=UnityEngine.Object.FindFirstObjectByType<SnowFootprints>();player=env.player;Require(prints,"Footprint component missing");
                    env.runClock=false;env.hour=13;env.SetWeather(WeatherKind.Snow,true);env.snowCover=.39f;env.Apply();Require(!prints.TryPlace(player.transform.position,Vector3.forward),"Marks below threshold");env.snowCover=.4f;env.Apply();Require(prints.TryPlace(player.transform.position,Vector3.forward),"No mark at 40% snow");
                    env.snowCover=.95f;env.Apply();player.externalControl=true;player.externalMove=Vector2.zero;player.Teleport(new Vector3(1,OpenWorldSceneBuilder.Height(1,-12)+.1f,-12));started=Time.time;stage=1;
                }
                else if(stage==1&&Time.time-started>.8f){baseline=prints.EmittedCount;player.externalMove=Vector2.up;started=Time.time;stage=2;}
                else if(stage==2&&Time.time-started>2.2f)
                {
                    player.externalMove=Vector2.zero;Require(prints.EmittedCount>=baseline+6,"Walking did not leave a trail");prints.Refresh(Time.time);var mesh=prints.GetComponent<MeshFilter>().sharedMesh;Require(mesh.vertexCount>100,"Footprints not rendered");
                    OpenWorldSceneBuilder.Capture(env.worldCamera,"Previews/36-Snow-Footprint-Trail.png");float initial=mesh.colors.Average(c=>c.a);prints.Refresh(Time.time+11);Require(mesh.colors.Length>0&&mesh.colors.Average(c=>c.a)<initial*.8f,"Prints did not fade");prints.Refresh(Time.time+prints.lifetime+1);Require(mesh.vertexCount==0,"Expired marks remain");
                    env.SetWeather(WeatherKind.Clear,true);prints.Refresh(Time.time);Require(mesh.vertexCount==0&&!prints.TryPlace(player.transform.position,Vector3.forward),"Marks survive melted snow");
                    var display=Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c=>c.name=="Pixel Display Clear Camera");Require(display&&display.enabled&&!display.targetTexture,"No display camera while pixel camera renders offscreen");
                    Finish(true,"40% threshold; walking trail; gradual fade and expiry; melting removes marks; display camera active: PASS");
                }
            }
            catch(Exception e){Finish(false,e.ToString());}
        }
        static void Finish(bool success,string message){EditorApplication.update-=Tick;SessionState.SetBool("SnowPrint.Verify",false);string result=(success?"SNOW_FOOTPRINT_SUCCESS":"SNOW_FOOTPRINT_FAILED")+"\n"+message+"\nNO PLAYER BUILD\n";File.WriteAllText("Previews/Snow-Footprint-Verification.txt",result);Debug.Log(result);EditorApplication.isPlaying=false;EditorApplication.Exit(success?0:1);}
    }
}
