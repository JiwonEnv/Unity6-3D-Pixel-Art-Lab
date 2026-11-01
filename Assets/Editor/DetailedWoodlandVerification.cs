using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class DetailedWoodlandVerification
    {
        static Color32[] Read(RenderTexture target)
        {
            var previous=RenderTexture.active;RenderTexture.active=target;
            var t=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            t.ReadPixels(new Rect(0,0,target.width,target.height),0,0);t.Apply();var data=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=previous;return data;
        }
        static Color32[] Render(Camera camera){camera.Render();return Read(camera.targetTexture);}
        static int Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)>8?1:0).Sum();
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var report=new List<string>();var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();env.runClock=false;env.hour=13;env.animationTime=2;env.SetWeather(ValleyWeather.Clear,true);
            var camera=env.worldCamera;camera.aspect=640f/360;camera.orthographicSize=5.8f;camera.transform.position=new Vector3(-5,2,2)-camera.transform.forward*45;
            var target=new RenderTexture(640,360,24);target.Create();camera.targetTexture=target;
            var hybrid=Render(camera);Shader.SetGlobalFloat("_LabDetailedToon",1);int toon=Difference(hybrid,Render(camera));Require(toon>1000,"Detailed shading mode unchanged");report.Add("Hybrid / Toon pixels changed: "+toon);
            Shader.SetGlobalFloat("_LabDetailedToon",0);Shader.SetGlobalFloat("_LabNormalDisabled",1);int normal=Difference(hybrid,Render(camera));Require(normal>500,"Normal maps unchanged");report.Add("Normal map pixels changed: "+normal);Shader.SetGlobalFloat("_LabNormalDisabled",0);
            var materials=AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/DetailedWoodland"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Require(materials.Where(m=>m.name!="DetailedWater").All(m=>m.GetTexture("_NormalMap")&&m.GetTexture("_SurfaceMap")),"Missing source surface maps");
            Require(materials.Where(m=>m.name.Contains("leaves")||m.name=="fern_02").All(m=>m.GetTexture("_AlphaMask")&&m.GetFloat("_AlphaClip")==1&&m.GetFloat("_Cull")==0),"Leaf cutout setup missing");report.Add("Source normal/ARM maps and double-sided alpha foliage: PASS");
            var root=GameObject.Find("Poly Haven Detailed Woodland");Require(root.transform.childCount==30,"Detailed asset count changed");
            Require(root.GetComponentsInChildren<MeshCollider>().Length==12&&root.GetComponentsInChildren<CapsuleCollider>().Length==2,"Detailed environment colliders missing");report.Add("30 detailed placements; 12 rock colliders and 2 trunk colliders: PASS");
            var reflection=UnityEngine.Object.FindFirstObjectByType<DetailWaterReflection>();camera.transform.position=new Vector3(-30,1,22)-camera.transform.forward*45;camera.orthographicSize=9;reflection.Refresh();
            var reflected=Read(reflection.ReflectionTexture);Require(reflected.Any(p=>p.r>30)&&reflected.Select(p=>p.r).Distinct().Count()>10,"Reflection texture is empty");Require(!GL.invertCulling,"Culling state leaked");
            var lake=Render(camera);Shader.SetGlobalFloat("_LabReflectionDisabled",1);int reflectedPixels=Difference(lake,Render(camera));Require(reflectedPixels>100,"Reflection does not reach water");Shader.SetGlobalFloat("_LabReflectionDisabled",0);report.Add("Planar reflection pixels changed: "+reflectedPixels);
            env.animationTime=3.2f;env.Apply();reflection.Refresh();int water=Difference(lake,Render(camera));Require(water>500,"Water does not move");report.Add("Wave / ripple pixels changed: "+water);
            env.SetWeather(ValleyWeather.Snow,true);int snow=Difference(lake,Render(camera));Require(snow>1000,"Detailed surfaces ignore snow");report.Add("Snow on detailed surfaces: PASS");
            report.Insert(0,"DETAILED_WOODLAND_RENDERING_SUCCESS");File.WriteAllLines("Previews/Detailed-Woodland-Verification.txt",report);Debug.Log(string.Join("\n",report));
            camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
        public static void Play()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);SessionState.SetBool("Detailed.Play",true);EditorApplication.isPlaying=true;
        }
        [InitializeOnLoadMethod]static void Resume(){if(SessionState.GetBool("Detailed.Play",false))EditorApplication.update+=Begin;}
        static void Begin()
        {
            if(!Application.isPlaying||!UnityEngine.Object.FindFirstObjectByType<ExplorerMotor>())return;
            EditorApplication.update-=Begin;SessionState.SetBool("Detailed.Play",false);
            var runner=new GameObject("Detailed scene gameplay validation").AddComponent<ExplorationPlaytest>();
            runner.completed=(success,result)=>{File.WriteAllText("Previews/Detailed-Woodland-Playtest.txt",result);EditorApplication.isPlaying=false;EditorApplication.Exit(success?0:1);};
        }
    }
}
