using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class SpriteWorldVerification
    {
        static double start;
        static int runtimeErrors;
        public static void CaptureTorchBaseline(){SessionState.SetBool("SpriteWorld.TorchBaseline",true);RunTorchLighting();}
        public static void RunTorchLighting()
        {
            SessionState.SetBool("SpriteWorld.TorchOnly",true);Run();
        }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/3D-Sprite-Rendering-Study.unity");
            SessionState.SetBool("SpriteWorld.Verify",true);
            EditorApplication.isPlaying=true;Register();
        }
        [InitializeOnLoadMethod]
        static void Resume(){if(SessionState.GetBool("SpriteWorld.Verify",false))Register();}
        static void Register()
        {
            start=EditorApplication.timeSinceStartup;
            EditorApplication.update-=Check;EditorApplication.update+=Check;
            Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;
        }
        static void Log(string message,string stack,LogType type)
        {
            if(type==LogType.Error || type==LogType.Exception)
            {
                // Unity's unrelated editor Search index can throw during batch startup.
                if(!stack.Contains("UnityEditor.Search"))runtimeErrors++;
            }
        }
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static int Difference(Color32[] a,Color32[] b)
        {
            int count=0;for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>8)count++;return count;
        }
        static Color32[] Render(Camera camera)
        {
            camera.Render();var prior=RenderTexture.active;RenderTexture.active=camera.targetTexture;
            var tex=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGB24,false);
            tex.ReadPixels(new Rect(0,0,tex.width,tex.height),0,0);tex.Apply();var pixels=tex.GetPixels32();
            UnityEngine.Object.Destroy(tex);RenderTexture.active=prior;return pixels;
        }
        static void Check()
        {
            if(EditorApplication.timeSinceStartup-start<9)return;
            EditorApplication.update-=Check;SessionState.SetBool("SpriteWorld.Verify",false);
            int code=0;
            try
            {
                Require(Application.isPlaying,"Play mode did not start");
                if(SessionState.GetBool("SpriteWorld.TorchOnly",false))
                {
                    SessionState.SetBool("SpriteWorld.TorchOnly",false);VerifyTorchLighting();
                    Application.logMessageReceived-=Log;EditorApplication.isPlaying=false;EditorApplication.Exit(0);return;
                }
                var lab=UnityEngine.Object.FindFirstObjectByType<SpriteWorldControls>();Require(lab,"Missing controls");
                lab.ApplyCamera();var camera=lab.worldCamera;var rt=camera.targetTexture;
                Require(rt && rt.filterMode==FilterMode.Point && rt.antiAliasing==1,"Pixel buffer configuration");
                Require(lab.linework.lineShader.isSupported,"Linework shader unsupported");
                Require(Mathf.Abs(camera.orthographicSize*2/rt.height-1f/lab.pixelsPerUnit)<.00001f,"Pixel density mismatch");
                Vector3 local=Quaternion.Inverse(camera.transform.rotation)*camera.transform.position;
                float x=local.x*lab.pixelsPerUnit-(rt.width&1)*.5f,y=local.y*lab.pixelsPerUnit-(rt.height&1)*.5f;
                Require(Mathf.Abs(x-Mathf.Round(x))<.001f && Mathf.Abs(y-Mathf.Round(y))<.001f,"Camera off pixel grid");
                Require(Mathf.Abs(lab.OutputRect.width/rt.width-Mathf.Round(lab.OutputRect.width/rt.width))<.001f,"Non-integer display scale");
                var original=Render(camera);
                Require(lab.linework.identityShader && lab.linework.identityShader.isSupported,"Object identity shader unavailable");
                var objects=UnityEngine.Object.FindObjectsByType<PixelArtObject>(FindObjectsSortMode.None);
                Require(objects.Length>20,"Semantic object groups missing");
                var seen=new System.Collections.Generic.HashSet<int>();
                foreach(var obj in objects)
                {
                    Require(seen.Add(obj.objectId),"Duplicate object ID");
                    foreach(var renderer in obj.GetComponentsInChildren<Renderer>())
                    {
                        if(renderer.GetComponentInParent<PixelArtObject>()!=obj)continue;
                        var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var data=block.GetVector("_LabObjectData");
                        int id=Mathf.RoundToInt(data.x*255)+Mathf.RoundToInt(data.y*255)*256;
                        Require(id==obj.objectId,"Renderer lost its object ID after scene reload");
                    }
                }
                lab.linework.debugIdentities=true;var debugIds=Render(camera);lab.linework.debugIdentities=false;
                var idColors=new System.Collections.Generic.HashSet<int>();
                foreach(var pixel in debugIds)idColors.Add(pixel.r+(pixel.g<<8)+(pixel.b<<16));
                Require(idColors.Count>20,"Identity buffer does not separate objects");
                lab.linework.objectBoundaries=false;var depthOnly=Render(camera);
                int idDifference=Difference(original,depthOnly);Require(idDifference>100,"Object line policies did not affect rendered pixels");
                lab.linework.objectBoundaries=true;
                var profiles=UnityEngine.Object.FindObjectsByType<AuthoredNormalProfile>(FindObjectsSortMode.None);
                Require(profiles.Length>20,"Authored normal profiles missing");
                foreach(var profile in profiles){profile.useAuthored=false;profile.Apply();}
                int normalDifference=Difference(original,Render(camera));Require(normalDifference>100,"Authored normals did not affect lighting");
                foreach(var profile in profiles){profile.useAuthored=true;profile.Apply();}
                var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach(var light in lights)if(light.type==LightType.Point)light.enabled=false;
                int lampDifference=Difference(original,Render(camera));Require(lampDifference>10,"Point lights did not illuminate rendered surfaces");
                foreach(var light in lights)if(light.type==LightType.Point)light.enabled=true;
                lab.linework.linework=false;var noLines=Render(camera);int inkDifference=Difference(original,noLines);
                Require(inkDifference>100,"Linework did not change rendered pixels");
                lab.toon=false;lab.ApplyCamera();var continuous=Render(camera);int shadeDifference=Difference(noLines,continuous);
                Require(shadeDifference>100,"Toon switch did not change rendered pixels");
                lab.lowResolution=false;lab.ApplyCamera();Require(camera.targetTexture==null,"Native resolution toggle failed");
                lab.lowResolution=true;lab.toon=true;lab.linework.linework=true;lab.ApplyCamera();Require(camera.targetTexture,"Pixel resolution restore failed");
                Require(runtimeErrors==0,"Runtime errors: "+runtimeErrors);
                string result=$"SPRITE_WORLD_PLAYMODE_SUCCESS\nPixel grid, density, integer upscale, native toggle: PASS\nLinework changed {inkDifference} pixels\nToon bands changed {shadeDifference} pixels\nObject groups: {objects.Length}; debug ID colors: {idColors.Count}\nObject policies changed {idDifference} pixels\nAuthored normals changed {normalDifference} pixels\nPoint lights changed {lampDifference} pixels\nRuntime script errors: {runtimeErrors}\n";
                File.WriteAllText("Previews/Rendering-Verification.txt",result);Debug.Log(result);
            }
            catch(Exception ex){Debug.LogError("SPRITE_WORLD_VERIFY_FAILED: "+ex);code=1;}
            Application.logMessageReceived-=Log;EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
        static void VerifyTorchLighting()
        {
            bool baseline=SessionState.GetBool("SpriteWorld.TorchBaseline",false);
            SessionState.SetBool("SpriteWorld.TorchBaseline",false);
            var lab=UnityEngine.Object.FindFirstObjectByType<SpriteWorldControls>();Require(lab,"Missing controls");
            lab.SetView(3);lab.ApplyCamera();
            var camera=lab.worldCamera;var previous=camera.targetTexture;
            var target=new RenderTexture(640,360,24){filterMode=FilterMode.Point};target.Create();camera.targetTexture=target;
            var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            var lamps=Array.FindAll(lights,l=>l.type==LightType.Point);
            Require(lamps.Length==2,"Expected two scene torches");
            Color32[] toonLit=null,toonDark=null,smoothLit=null,smoothDark=null;
            var originalStates=Array.ConvertAll(lamps,l=>l.enabled);
            try
            {
                Shader.SetGlobalFloat("_LabContinuous",0);
                foreach(var lamp in lamps)lamp.enabled=true;toonLit=Render(camera);
                foreach(var lamp in lamps)lamp.enabled=false;toonDark=Render(camera);
                Shader.SetGlobalFloat("_LabContinuous",1);
                foreach(var lamp in lamps)lamp.enabled=true;smoothLit=Render(camera);
                foreach(var lamp in lamps)lamp.enabled=false;smoothDark=Render(camera);
                int toonPixels=Difference(toonLit,toonDark),smoothPixels=Difference(smoothLit,smoothDark);
                float ratio=(float)toonPixels/Mathf.Max(1,smoothPixels);
                if(!baseline)
                {
                    Require(toonPixels>500,"Toon mode lost the surrounding torch illumination");
                    Require(ratio>.6f,"Toon mode discarded most of the continuous-light footprint");
                    Require(runtimeErrors==0,"Runtime shader/script errors");
                }
                string prefix=baseline?"09-Before":"10-After";
                SavePixels(prefix+"-Toon-Torch.png",toonLit,640,360);
                SavePixels(prefix+"-Continuous-Torch.png",smoothLit,640,360);
                SavePixels(prefix+"-Toon-No-Torches.png",toonDark,640,360);
                string result=$"TORCH_LIGHTING_{(baseline?"BASELINE":"SUCCESS")}\nToon illuminated pixels: {toonPixels}\nContinuous illuminated pixels: {smoothPixels}\nToon/continuous footprint ratio: {ratio:F3}\nRuntime errors: {runtimeErrors}\n";
                File.WriteAllText("Previews/"+(baseline?"Torch-Lighting-Before.txt":"Torch-Lighting-Verification.txt"),result);Debug.Log(result);
            }
            finally
            {
                for(int i=0;i<lamps.Length;i++)lamps[i].enabled=originalStates[i];
                Shader.SetGlobalFloat("_LabContinuous",lab.toon?0:1);
                camera.targetTexture=previous;target.Release();UnityEngine.Object.Destroy(target);
            }
        }
        static void SavePixels(string name,Color32[] pixels,int width,int height)
        {
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.SetPixels32(pixels);texture.Apply();
            File.WriteAllBytes("Previews/"+name,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
        }
    }
}
