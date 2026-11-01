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
                lab.linework.linework=false;var noLines=Render(camera);int inkDifference=Difference(original,noLines);
                Require(inkDifference>100,"Linework did not change rendered pixels");
                lab.toon=false;lab.ApplyCamera();var continuous=Render(camera);int shadeDifference=Difference(noLines,continuous);
                Require(shadeDifference>100,"Toon switch did not change rendered pixels");
                lab.lowResolution=false;lab.ApplyCamera();Require(camera.targetTexture==null,"Native resolution toggle failed");
                lab.lowResolution=true;lab.toon=true;lab.linework.linework=true;lab.ApplyCamera();Require(camera.targetTexture,"Pixel resolution restore failed");
                Require(runtimeErrors==0,"Runtime errors: "+runtimeErrors);
                string result=$"SPRITE_WORLD_PLAYMODE_SUCCESS\nPixel grid, density, integer upscale, native toggle: PASS\nLinework changed {inkDifference} pixels\nToon bands changed {shadeDifference} pixels\nRuntime script errors: {runtimeErrors}\n";
                File.WriteAllText("Previews/Rendering-Verification.txt",result);Debug.Log(result);
            }
            catch(Exception ex){Debug.LogError("SPRITE_WORLD_VERIFY_FAILED: "+ex);code=1;}
            Application.logMessageReceived-=Log;EditorApplication.isPlaying=false;EditorApplication.Exit(code);
        }
    }
}
