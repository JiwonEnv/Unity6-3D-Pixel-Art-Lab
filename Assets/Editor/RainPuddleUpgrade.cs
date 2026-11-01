using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class RainPuddleUpgrade
    {
        public static DetailWaterReflection Configure(Camera camera)
        {
            var ground=GameObject.Find("Walkable hills and connected trails");
            var soil=ground.GetComponent<MeshRenderer>().sharedMaterial;
            var trailObject=GameObject.Find("Connected dirt trails");var trail=trailObject.GetComponent<MeshRenderer>().sharedMaterial;
            // Rendered trail ribbons sit above terrain; footsteps must hit that visible surface.
            var trailCollider=trailObject.GetComponent<MeshCollider>();if(!trailCollider)trailCollider=trailObject.AddComponent<MeshCollider>();trailCollider.sharedMesh=trailObject.GetComponent<MeshFilter>().sharedMesh;
            foreach(var material in new[]{soil,trail}){material.SetFloat("_Puddles",1);EditorUtility.SetDirty(material);}
            var reflection=ground.GetComponent<DetailWaterReflection>();if(!reflection)reflection=ground.AddComponent<DetailWaterReflection>();
            reflection.source=camera;reflection.waterMaterial=soil;reflection.additionalMaterials=new[]{trail};reflection.textureProperty="_PuddleReflection";reflection.waterHeight=1.1f;
            reflection.groundReceivers=new Renderer[]{ground.GetComponent<MeshRenderer>(),trailObject.GetComponent<MeshRenderer>()};
            var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();reflection.groundAnchor=null;
            var steps=UnityEngine.Object.FindFirstObjectByType<PuddleFootsteps>();if(!steps)steps=new GameObject("Puddle footstep rings and splashes").AddComponent<PuddleFootsteps>();steps.player=env.player;steps.environment=env;
            var snow=UnityEngine.Object.FindFirstObjectByType<SnowFootprints>();if(!snow)snow=new GameObject("Fading snow footprints").AddComponent<SnowFootprints>();snow.player=env.player;snow.environment=env;
            foreach(var renderer in env.player.GetComponentsInChildren<Renderer>(true)){renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;}
            return reflection;
        }
        static Color32[] Render(Camera camera)
        {
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;
            var t=new Texture2D(640,360,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,640,360),0,0);t.Apply();var pixels=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=previous;return pixels;
        }
        static int Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)>8?1:0).Sum();
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        public static void Apply()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();var camera=env.worldCamera;
            var reflection=Configure(camera);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            env.runClock=false;env.hour=13;env.animationTime=2;env.SetWeather(ValleyWeather.Rain,true);
            UnityEngine.Object.FindFirstObjectByType<PixelWeatherParticles>().Refresh(true);
            camera.aspect=640f/360;camera.orthographicSize=6;camera.transform.position=new Vector3(-2,1,0)-camera.transform.forward*45;
            var target=new RenderTexture(640,360,24);target.Create();camera.targetTexture=target;reflection.Refresh();
            var soil=reflection.waterMaterial;var trail=reflection.additionalMaterials[0];
            OpenWorldSceneBuilder.Capture(camera,"Previews/30-Rain-Puddles.png");var wet=Render(camera);soil.SetFloat("_Puddles",0);trail.SetFloat("_Puddles",0);var without=Render(camera);
            int pools=Difference(wet,without);Require(pools>1000,"Puddles not visible: "+pools);soil.SetFloat("_Puddles",1);trail.SetFloat("_Puddles",1);
            Shader.SetGlobalFloat("_LabReflectionDisabled",1);int reflections=Difference(wet,Render(camera));Shader.SetGlobalFloat("_LabReflectionDisabled",0);Require(reflections>1000,"Puddle reflection not visible");
            Shader.SetGlobalFloat("_LabDetailedToon",1);var toon=Render(camera);Shader.SetGlobalFloat("_LabReflectionDisabled",1);Require(Difference(toon,Render(camera))>1000,"Reflection missing with Toon");Shader.SetGlobalFloat("_LabReflectionDisabled",0);Shader.SetGlobalFloat("_LabDetailedToon",0);
            OpenWorldSceneBuilder.Capture(camera,"Previews/30-Rain-Puddles.png");
            env.animationTime=2.3f;env.Apply();Require(Difference(wet,Render(camera))>100,"Rain ripple unchanged");
            env.weather=ValleyWeather.Clear;env.rain=0;env.Apply();UnityEngine.Object.FindFirstObjectByType<PixelWeatherParticles>().Refresh(true);var residual=Render(camera);soil.SetFloat("_Puddles",0);trail.SetFloat("_Puddles",0);Require(Difference(residual,Render(camera))>100,"Puddles disappeared immediately after rain");soil.SetFloat("_Puddles",1);trail.SetFloat("_Puddles",1);
            env.wetness=0;env.Apply();var dry=Render(camera);soil.SetFloat("_Puddles",0);trail.SetFloat("_Puddles",0);Require(Difference(dry,Render(camera))==0,"Puddles remain on dry ground");soil.SetFloat("_Puddles",1);trail.SetFloat("_Puddles",1);
            OpenWorldSceneBuilder.Capture(camera,"Previews/31-Dry-Puddle-Ground.png");
            env.SetWeather(ValleyWeather.Snow,true);var snow=Render(camera);soil.SetFloat("_Puddles",0);trail.SetFloat("_Puddles",0);Require(Difference(snow,Render(camera))==0,"Puddles visible through snow");soil.SetFloat("_Puddles",1);trail.SetFloat("_Puddles",1);
            Require(!GL.invertCulling&&Shader.GetGlobalFloat("_LabReflectionPass")==0,"Reflection state leaked");
            File.WriteAllText("Previews/Rain-Puddle-Verification.txt",$"RAIN_PUDDLE_SUCCESS\nPuddle pixels: {pools}\nReflection pixels: {reflections}\nToon reflections: PASS\nAnimated rain rings: PASS\nResidual wetness after rain: PASS\nDry ground and snow hide pools: PASS\nReflection render state restored: PASS\nNO PLAYER BUILD\n");
            camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);Debug.Log("RAIN_PUDDLE_SUCCESS");
        }
    }
}
