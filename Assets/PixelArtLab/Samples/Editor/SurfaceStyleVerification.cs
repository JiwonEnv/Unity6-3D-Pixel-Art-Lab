using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab.Editor
{
    // R3: style assets, the PixelAtmosphere volume and WaterBody really drive the render.
    public static class SurfaceStyleVerification
    {
        static Color32[] Read(RenderTexture target)
        {
            var previous=RenderTexture.active;RenderTexture.active=target;
            var t=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            t.ReadPixels(new Rect(0,0,target.width,target.height),0,0);t.Apply();var data=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=previous;return data;
        }
        static Color32[] Render(Camera camera){LabCameraRender.Render(camera);return Read(camera.targetTexture);}
        static int Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)>8?1:0).Sum();
        static void Require(bool value,string message){if(!value)throw new Exception(message);}

        public static void Run()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var report=new List<string>();
            var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();env.runClock=false;env.hour=13;env.animationTime=2;
            var set=env.GetComponent<SurfaceStyleSet>();Require(set&&set.snow&&set.toonRamp,"Scene has no SurfaceStyleSet");
            var camera=env.worldCamera;camera.aspect=640f/360;camera.orthographicSize=5.8f;camera.transform.position=new Vector3(-5,2,2)-camera.transform.forward*45;
            var target=new RenderTexture(640,360,24);target.Create();camera.targetTexture=target;

            // Snow color comes from SnowStyle.
            env.SetWeather(WeatherKind.Snow,true);var snowy=Render(camera);
            var snow=ScriptableObject.CreateInstance<SnowStyle>();snow.color=new Color(1,.3f,.3f);var original=set.snow;set.snow=snow;set.Push();
            int recolored=Difference(snowy,Render(camera));set.snow=original;set.Push();
            Require(recolored>1000,"SnowStyle color ignored: "+recolored);Require(Difference(snowy,Render(camera))==0,"Snow preset did not restore");
            report.Add("SnowStyle color change pixels: "+recolored);

            // Toon bands come from ToonRampStyle.
            env.SetWeather(WeatherKind.Clear,true);var clear=Render(camera);
            var ramp=ScriptableObject.CreateInstance<ToonRampStyle>();ramp.bandLevels=new Vector4(0,.2f,.4f,.6f);var originalRamp=set.toonRamp;set.toonRamp=ramp;set.Push();
            int banded=Difference(clear,Render(camera));set.toonRamp=originalRamp;set.Push();
            Require(banded>1000,"ToonRampStyle ignored: "+banded);report.Add("ToonRampStyle band change pixels: "+banded);

            // A PixelAtmosphere volume thickens the height fog; removing it restores the frame.
            env.hour=21;env.Apply();var night=Render(camera);
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();var air=profile.Add<PixelAtmosphere>();air.heightFogDensity.Override(4);
            var holder=new GameObject("Fog zone test");var volume=holder.AddComponent<Volume>();volume.isGlobal=true;volume.priority=50;volume.sharedProfile=profile;
            int fogged=Difference(night,Render(camera));UnityEngine.Object.DestroyImmediate(holder);
            int restored=Difference(night,Render(camera));
            Require(fogged>1000,"PixelAtmosphere volume ignored: "+fogged);Require(restored==0,"Fog zone stayed after the volume left: "+restored);
            report.Add("PixelAtmosphere height fog x4 pixels: "+fogged+", after removal: "+restored);

            // Water: rain splashes land on the WaterBody surface, puddle grid follows PuddleStyle.
            var lake=WaterBody.All.FirstOrDefault();Require(lake,"No WaterBody");
            Require(Mathf.Approximately(WaterBody.SurfaceAt(lake.center.x,lake.center.y,-5),lake.surfaceHeight),"WaterBody surface lookup");
            Require(Mathf.Approximately(PuddleStyle.ActiveCellSize,set.puddles.cellSize),"Puddle cell size not shared with PuddleSurface");
            report.Add("WaterBody surface and PuddleStyle cell size: PASS");

            report.Insert(0,"SURFACE_STYLE_SUCCESS");File.WriteAllLines("Previews/Surface-Style-Verification.txt",report);Debug.Log(string.Join("\n",report));
            camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
