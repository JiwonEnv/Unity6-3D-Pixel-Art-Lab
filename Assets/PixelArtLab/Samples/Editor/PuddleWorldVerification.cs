using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class PuddleWorldVerification
    {
        static int stage,baseline;static float started;static double timeout;
        static WorldEnvironment env;static PuddleFootsteps steps;static ExplorerMotor player;
        static Vector3 wetPoint;static readonly List<string> report=new List<string>();
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        public static void Distribution()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();env.SetWeather(WeatherKind.Rain,true);env.hour=13;env.Apply();
            int pools=0,outside=0;float min=100,max=-100;var quadrants=new int[4];
            for(int x=-68;x<=68;x+=2)for(int z=-68;z<=68;z+=2)
            {
                if(!PuddleSurface.Ground(new Vector3(x,OpenWorldSceneBuilder.Height(x,z)+.1f,z),out var hit))continue;
                if(PuddleSurface.Coverage(hit.point,hit.normal,1,0)<.5f)continue;
                pools++;min=Mathf.Min(min,hit.point.y);max=Mathf.Max(max,hit.point.y);if(Mathf.Abs(hit.point.y-1.1f)>.3f)outside++;quadrants[(x>0?1:0)+(z>0?2:0)]++;
            }
            Require(outside>200&&max-min>1,"Puddles still constrained to altitude band");foreach(int n in quadrants)Require(n>50,"Missing puddles in a map quadrant");
            var camera=env.worldCamera;camera.aspect=640f/360;camera.orthographicSize=58;camera.transform.position=new Vector3(0,1,0)-camera.transform.forward*100;camera.farClipPlane=260;
            var reflection=GameObject.Find("Walkable hills and connected trails").GetComponent<DetailWaterReflection>();reflection.Refresh();OpenWorldSceneBuilder.Capture(camera,"Previews/32-Mapwide-Rain-Puddles.png");
            File.WriteAllText("Previews/Puddle-Distribution-Verification.txt",$"MAPWIDE_PUDDLES_SUCCESS\nWet samples: {pools}; outside old height band: {outside}\nHeight range: {min:F2}m to {max:F2}m\nQuadrants: {string.Join(", ",quadrants)}\nAll four quadrants and multiple elevations: PASS\nNO PLAYER BUILD\n");Debug.Log("MAPWIDE_PUDDLES_SUCCESS");
        }
        public static void Play(){EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);SessionState.SetBool("Puddle.Verify",true);EditorApplication.isPlaying=true;}
        [InitializeOnLoadMethod]static void Resume(){if(SessionState.GetBool("Puddle.Verify",false)){stage=0;timeout=EditorApplication.timeSinceStartup+60;EditorApplication.update+=Tick;}}
        static void Tick()
        {
            if(!Application.isPlaying)return;
            try
            {
                Require(EditorApplication.timeSinceStartup<timeout,"Puddle interaction test timed out at stage "+stage);
                if(stage==0)
                {
                    env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();steps=UnityEngine.Object.FindFirstObjectByType<PuddleFootsteps>();player=env.player;
                    Require(steps,"Footstep component not saved");env.runClock=false;env.hour=13;env.SetWeather(WeatherKind.Rain,true);player.externalControl=true;player.externalMove=Vector2.zero;
                    Vector3 forward=player.cameraTransform.forward;forward.y=0;forward.Normalize();bool found=false;
                    for(int x=-15;x<=15&&!found;x++)for(int z=-15;z<=15&&!found;z++)
                    {
                        Vector3 p=new Vector3(x,OpenWorldSceneBuilder.Height(x,z)+.1f,z);if(!PuddleSurface.Ground(p,out var hit)||PuddleSurface.Coverage(hit.point,hit.normal,1,0)<.95f)continue;
                        bool fits=true;for(float d=0;d<2;d+=.35f){Vector3 q=hit.point+forward*d;if(!PuddleSurface.Ground(q,out var h)||PuddleSurface.Coverage(h.point,h.normal,1,0)<.7f)fits=false;}
                        if(fits){wetPoint=hit.point;found=true;}
                    }
                    Require(found,"No walkable water patch found");player.Teleport(wetPoint+Vector3.up*.1f);started=Time.time;stage=1;
                }
                else if(stage==1&&Time.time-started>.8f)
                {
                    Require(player.Grounded,"Player did not settle");baseline=steps.EmittedCount;player.externalMove=Vector2.up;started=Time.time;stage=2;
                }
                else if(stage==2&&steps.EmittedCount>baseline)
                {
                    player.externalMove=Vector2.zero;steps.Refresh(Time.time+.32f);Require(steps.GetComponent<MeshFilter>().sharedMesh.vertexCount>40,"No ring / droplet geometry");
                    var reflection=GameObject.Find("Walkable hills and connected trails").GetComponent<DetailWaterReflection>();reflection.Refresh();OpenWorldSceneBuilder.Capture(env.worldCamera,"Previews/33-Puddle-Footstep-Splash.png");
                    report.Add("Walking on visible water emits expanding rings and droplets: PASS");started=Time.time;stage=3;
                }
                else if(stage==3&&Time.time-started>1.2f)
                {
                    Require(steps.GetComponent<MeshFilter>().sharedMesh.vertexCount==0,"Old splash geometry did not expire");baseline=steps.EmittedCount;
                    env.SetWeather(WeatherKind.Clear,true);Require(!steps.TryStep(wetPoint),"Dry ground emitted a splash");env.SetWeather(WeatherKind.Snow,true);Require(!steps.TryStep(wetPoint),"Snow emitted a splash");Require(steps.EmittedCount==baseline,"Dry / snow changed splash count");report.Add("Stopped rings expire; dry ground and snow suppress splashes: PASS");
                    env.SetWeather(WeatherKind.Rain,true);player.Teleport(wetPoint+Vector3.up*.1f);started=Time.time;stage=4;
                }
                else if(stage==4&&Time.time-started>.6f){Require(player.Grounded,"Cannot start jump");baseline=steps.EmittedCount;player.QueueJump();started=Time.time;stage=5;}
                else if(stage==5&&Time.time-started>.25f){Require(!player.Grounded,"Jump did not leave ground");Require(steps.EmittedCount==baseline,"Airborne player emitted footsteps");report.Add("Airborne player does not emit water steps: PASS");stage=6;}
                else if(stage==6&&Time.time-started>1.2f){Require(player.Grounded&&steps.EmittedCount>baseline,"Landing did not splash");report.Add("Landing in puddle emits a stronger splash: PASS");Finish(true);}
            }
            catch(Exception e){report.Add(e.ToString());Finish(false);}
        }
        static void Finish(bool success)
        {
            EditorApplication.update-=Tick;SessionState.SetBool("Puddle.Verify",false);report.Insert(0,success?"PUDDLE_INTERACTION_SUCCESS":"PUDDLE_INTERACTION_FAILED");File.WriteAllLines("Previews/Puddle-Interaction-Verification.txt",report);Debug.Log(string.Join("\n",report));EditorApplication.isPlaying=false;EditorApplication.Exit(success?0:1);
        }
    }
}
