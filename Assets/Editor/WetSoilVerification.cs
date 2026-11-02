using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PixelArtLab.Editor
{
    public static class WetSoilVerification
    {
        static Color32[] Render(Camera camera)
        {
            LabCameraRender.Render(camera);var old=RenderTexture.active;RenderTexture.active=camera.targetTexture;var t=new Texture2D(640,360,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,640,360),0,0);t.Apply();var data=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;return data;
        }
        static int Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)>8?1:0).Sum();
        static float Bright(Color32 c)=>c.r*.2126f+c.g*.7152f+c.b*.0722f;
        static void Require(bool c,string message){if(!c)throw new Exception(message);}
        public static void Run()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();env.runClock=false;env.windEnabled=false;env.hour=13;env.animationTime=2;env.SetWeather(ValleyWeather.Rain,true);
            Require(!ShaderUtil.GetShaderMessages(Shader.Find("PixelArtLab/Sprite Toon")).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Wet soil shader error");
            var camera=env.worldCamera;camera.aspect=640f/360;camera.orthographicSize=6;camera.transform.position=new Vector3(-2,1,0)-camera.transform.forward*45;
            var target=new RenderTexture(640,360,24);target.Create();camera.targetTexture=target;
            var reflection=GameObject.Find("Walkable hills and connected trails").GetComponent<DetailWaterReflection>();var materials=new[]{reflection.waterMaterial,reflection.additionalMaterials[0]};
            Action<float> set=value=>{foreach(var m in materials)m.SetFloat("_PuddleWetStrength",value);};
            reflection.Refresh();set(0);var before=Render(camera);OpenWorldSceneBuilder.Capture(camera,"Previews/45-Puddle-Without-Damp-Soil.png");set(1);var after=Render(camera);OpenWorldSceneBuilder.Capture(camera,"Previews/46-Puddle-Damp-Soil.png");
            int changed=Difference(before,after),darker=before.Zip(after,(a,b)=>Bright(a)-Bright(b)>5?1:0).Sum();Require(changed>2000&&darker>1500,"Damp shore not visibly darker");
            env.weather=ValleyWeather.Clear;env.rain=0;env.wetness=.15f;env.Apply();set(0);var residualOff=Render(camera);set(1);int residual=Difference(residualOff,Render(camera));Require(residual>1000,$"Damp soil disappears with shallow water: {residual}");OpenWorldSceneBuilder.Capture(camera,"Previews/47-Drying-Damp-Soil.png");
            env.wetness=0;env.Apply();set(0);var dry=Render(camera);set(1);Require(Difference(dry,Render(camera))==0,"Dry ground remains stained");
            env.SetWeather(ValleyWeather.Snow,true);set(0);var snow=Render(camera);set(1);Require(Difference(snow,Render(camera))==0,"Dampness stains covered snow");
            foreach(var m in materials)EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();File.WriteAllText("Previews/Wet-Soil-Verification.txt",$"WET_SOIL_SUCCESS\nWet shore pixels: {changed}; darker soil pixels: {darker}\nResidual damp soil pixels after shallow water dries: {residual}\nDry ground unchanged: PASS\nSnow coverage hides wet stain: PASS\nNO PLAYER BUILD\n");camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);Debug.Log("WET_SOIL_SUCCESS");
        }
    }
}
