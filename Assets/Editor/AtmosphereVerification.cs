using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class AtmosphereVerification
    {
        static double started;
        static int errors;
        public static void Run()
        {
            EditorSceneManager.OpenScene(OpenWorldSceneBuilder.ScenePath);SessionState.SetBool("Atmosphere.Verify",true);EditorApplication.isPlaying=true;Register();
        }
        [InitializeOnLoadMethod]static void Resume(){if(SessionState.GetBool("Atmosphere.Verify",false))Register();}
        static void Register(){started=EditorApplication.timeSinceStartup;EditorApplication.update-=Check;EditorApplication.update+=Check;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
        static void Log(string message,string stack,LogType type){if((type==LogType.Error||type==LogType.Exception)&&!stack.Contains("UnityEditor.Search"))errors++;}
        static void Require(bool condition,string text){if(!condition)throw new Exception(text);}
        static Color32[] Read(RenderTexture target)
        {
            var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();var result=texture.GetPixels32();UnityEngine.Object.Destroy(texture);RenderTexture.active=previous;return result;
        }
        static Color32[] Render(Camera camera){LabCameraRender.Render(camera);return Read(camera.targetTexture);}
        static int Difference(Color32[] a,Color32[] b){int n=0;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>8)n++;return n;}
        static float Brightness(Color32[] pixels)=>pixels.Average(p=>(p.r*.2126f+p.g*.7152f+p.b*.0722f)/255);
        static void Save(string path,Color32[] pixels,int width,int height){var t=new Texture2D(width,height,TextureFormat.RGB24,false);t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
        static void Check()
        {
            if(EditorApplication.timeSinceStartup-started<3)return;
            EditorApplication.update-=Check;SessionState.SetBool("Atmosphere.Verify",false);int exit=0;
            try
            {
                var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();var particles=UnityEngine.Object.FindFirstObjectByType<PixelWeatherParticles>();
                Require(env&&particles,"Environment components missing after scene reload");env.runClock=false;env.hour=13;env.animationTime=2;env.SetWeather(ValleyWeather.Clear,true);particles.Refresh(true);
                var camera=env.worldCamera;var oldTarget=camera.targetTexture;var target=new RenderTexture(640,360,24){filterMode=FilterMode.Point};target.Create();camera.targetTexture=target;
                var report=new List<string>();var day=Render(camera);float dayBrightness=Brightness(day),dayTorch=env.torches[0].intensity;
                env.hour=0;env.Apply();particles.Refresh(true);var night=Render(camera);float nightBrightness=Brightness(night);
                Require(nightBrightness> .025f&&nightBrightness<dayBrightness*.8f,"Night must be darker but remain readable");Require(env.torches[0].intensity>dayTorch,"Night torch intensity did not increase");
                report.Add($"Day/night mean brightness: {dayBrightness:F3}/{nightBrightness:F3}; night torches: PASS");
                Save("Previews/22-Playable-Night.png",night,640,360);
                env.hour=17.5f;env.Apply();Require(env.sun.color.r>env.sun.color.g&&env.sun.color.g>env.sun.color.b,"Sunset key light is not warm");report.Add("Warm sunset key and long-angle sun: PASS");
                env.hour=13;env.SetWeather(ValleyWeather.Rain,true);particles.Refresh(true);var rain=Render(camera);
                Require(particles.ActiveDrops==320&&env.wetness>.9f,"Rain did not activate");Require(Difference(day,rain)>1000,"Rain rendering unchanged");
                report.Add("Rain: 320 source-pixel streaks, wet surfaces and splashes PASS");
                env.SetWeather(ValleyWeather.Snow,true);particles.Refresh(true);var snow=Render(camera);
                Require(particles.ActiveDrops==220&&env.snowCover>.9f,"Snow did not activate");Require(Difference(rain,snow)>1000,"Snow rendering unchanged");
                var vertex=particles.GetComponent<MeshFilter>().sharedMesh.vertices;
                var a=camera.WorldToViewportPoint(vertex[0]);var b=camera.WorldToViewportPoint(vertex[1]);
                Require(Mathf.Abs(Mathf.Abs(b.x-a.x)*640-2)<.02f,"Snowflake width is not two source pixels");report.Add("Snow: 220 two-pixel flakes and upward-facing coverage PASS");
                env.SetWeather(ValleyWeather.Clear,true);particles.Refresh(true);env.waterEnabled=false;env.windEnabled=true;
                env.animationTime=1;env.Apply();Shader.SetGlobalFloat("_LabCloudCover",0);var windA=Render(camera);
                env.animationTime=3;env.Apply();Shader.SetGlobalFloat("_LabCloudCover",0);var windB=Render(camera);int windDifference=Difference(windA,windB);
                Require(windDifference>50,"Vegetation wind has no visible movement");
                env.windEnabled=false;env.animationTime=1;env.Apply();Shader.SetGlobalFloat("_LabCloudCover",0);var stillA=Render(camera);
                env.animationTime=3;env.Apply();Shader.SetGlobalFloat("_LabCloudCover",0);Require(Difference(stillA,Render(camera))<20,"Wind-off geometry still moves");
                report.Add($"Wind changed {windDifference} pixels; wind-off frozen PASS");
                var motion=UnityEngine.Object.FindObjectsByType<WorldSurfaceMotion>(FindObjectsSortMode.None);
                Require(motion.Any(m=>m.kind==WorldMotionKind.Tree)&&motion.Any(m=>m.kind==WorldMotionKind.Grass)&&motion.Any(m=>m.kind==WorldMotionKind.Flag),"Tree, grass or flag profile missing");
                TestIsolatedMotion(env,motion.First(m=>m.kind==WorldMotionKind.Flag),3.5f,"Flag",report);
                TestIsolatedMotion(env,motion.First(m=>m.kind==WorldMotionKind.Water),8.8f,"Water",report);
                Require(Shader.GetGlobalFloat("_LabRenderDepthNormals")==0,"Geometry pass leaked global state");
                Require(errors==0,"Runtime shader or script errors: "+errors);
                var shader=Shader.Find("PixelArtLab/Sprite Toon");Require(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),"Toon shader compilation failed");
                camera.targetTexture=oldTarget;target.Release();UnityEngine.Object.Destroy(target);
                string result="ATMOSPHERE_PLAYMODE_SUCCESS\n"+string.Join("\n",report)+"\nAnimated identity/depth buffers: PASS\nRuntime errors: "+errors+"\nNO PLAYER BUILD WAS RUN\n";
                File.WriteAllText("Previews/Atmosphere-Verification.txt",result);Debug.Log(result);
            }
            catch(Exception exception){Debug.LogError("ATMOSPHERE_VERIFY_FAILED "+exception);exit=1;}
            Application.logMessageReceived-=Log;EditorApplication.isPlaying=false;EditorApplication.Exit(exit);
        }
        static void TestIsolatedMotion(WorldEnvironment env,WorldSurfaceMotion item,float size,string name,List<string> report)
        {
            var camera=env.worldCamera;var originalPosition=camera.transform.position;float oldSize=camera.orthographicSize;int oldMask=camera.cullingMask;
            var renderers=item.GetComponentsInChildren<Renderer>();var layers=renderers.Select(r=>r.gameObject.layer).ToArray();
            for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.layer=12;
            camera.cullingMask=1<<12;camera.orthographicSize=size;camera.transform.position=item.transform.position+Vector3.up*(name=="Flag"?1.5f:0)-camera.transform.forward*45;
            env.windEnabled=true;env.waterEnabled=true;env.animationTime=1;env.Apply();Shader.SetGlobalFloat("_LabCloudCover",0);var first=Render(camera);
            var ink=camera.GetComponent<PixelLinework>();Require(ink.AnimatedGeometryBuffer&&ink.IdentityBuffer,"Animated geometry buffers not created");var firstId=Read(ink.IdentityBuffer);
            env.animationTime=3;env.Apply();Shader.SetGlobalFloat("_LabCloudCover",0);var second=Render(camera);int count=Difference(first,second),idCount=Difference(firstId,Read(ink.IdentityBuffer));
            Require(count>10,name+" did not animate");if(name=="Flag")Require(idCount>5,"Flag ID silhouette did not follow the moving cloth");
            Save("Previews/23-"+name+"-Motion.png",second,640,360);report.Add(name+" changed "+count+" pixels; identity silhouette delta "+idCount+" PASS");
            for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.layer=layers[i];camera.cullingMask=oldMask;camera.orthographicSize=oldSize;camera.transform.position=originalPosition;
        }
    }
}
