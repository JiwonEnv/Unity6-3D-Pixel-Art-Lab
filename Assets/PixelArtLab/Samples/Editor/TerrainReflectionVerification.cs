using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PixelArtLab.Editor
{
    public static class TerrainReflectionVerification
    {
        static Color32[] Read(RenderTexture target)
        {var old=RenderTexture.active;RenderTexture.active=target;var t=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,target.width,target.height),0,0);t.Apply();var data=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=old;return data;}
        static Color32[] Render(Camera camera){LabCameraRender.Render(camera);return Read(camera.targetTexture);}
        static int Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)>8?1:0).Sum();
        static float Luma(Color32 c)=>(c.r*.2126f+c.g*.7152f+c.b*.0722f)/255;
        static void Require(bool c,string message){if(!c)throw new Exception(message);}
        public static void Run()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();env.runClock=false;env.hour=13;env.windEnabled=false;env.SetWeather(WeatherKind.Rain,true);
            var ground=GameObject.Find("Walkable hills and connected trails");var reflection=ground.GetComponent<DetailWaterReflection>();var camera=env.worldCamera;camera.aspect=640f/360;camera.orthographicSize=5;
            var target=new RenderTexture(640,360,24);target.Create();camera.targetTexture=target;var report=new List<string>();
            // An existing low flower, below the old 1.1m clip plane, must survive.
            var flower=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name.StartsWith("flower_")&&t.position.y<1&&t.GetComponentInChildren<MeshRenderer>());
            camera.transform.position=flower.position+Vector3.up*.4f-camera.transform.forward*45;reflection.Refresh();var withFlower=Read(reflection.ReflectionTexture);var renderers=flower.GetComponentsInChildren<Renderer>();foreach(var r in renderers)r.forceRenderingOff=true;reflection.Refresh();int flowerPixels=Difference(withFlower,Read(reflection.ReflectionTexture));foreach(var r in renderers)r.forceRenderingOff=false;Require(flowerPixels>2,"Existing low flower missing from reflection");report.Add("Existing low flower reflection pixels: "+flowerPixels);
            // Stable mirror contact at several elevations and camera offsets. The source foot
            // and actual receiving point are the same world point, regardless of capture height.
            var fence=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name.StartsWith("fence_simpleLow")&&t.GetComponentInChildren<Renderer>());
            float height=reflection.waterHeight;var positions=new[]{new Vector3(39,OpenWorldSceneBuilder.Height(39,-35)+.1f,-35),new Vector3(-10,OpenWorldSceneBuilder.Height(-10,-10)+.1f,-10)};
            foreach(var focus in positions)
            {
                env.player.transform.position=focus;camera.orthographicSize=focus.x<0?6:5;camera.transform.position=fence.position+Vector3.up*.6f-camera.transform.forward*45;reflection.Refresh();Require(Mathf.Abs(reflection.waterHeight-height)<.001f,"Mirror plane still follows player");
                var matrix=Shader.GetGlobalMatrix("_PuddleProjection");var p=fence.position;var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.transform.position=p+Vector3.up*.08f;marker.transform.localScale=Vector3.one*.1f;var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=Color.magenta;marker.GetComponent<Renderer>().sharedMaterial=mat;reflection.Refresh();
                var data=Read(reflection.ReflectionTexture);float x=0,y=0,n=0;for(int row=0;row<reflection.ReflectionTexture.height;row++)for(int col=0;col<reflection.ReflectionTexture.width;col++){var c=data[row*reflection.ReflectionTexture.width+col];if(c.r>200&&c.b>200&&c.g<70){x+=col;y+=row;n++;}}
                var clip=matrix*new Vector4(marker.transform.position.x,marker.transform.position.y,marker.transform.position.z,1);Require(n>0&&Mathf.Abs(x/n/reflection.ReflectionTexture.width-(clip.x/clip.w*.5f+.5f))<.01f&&Mathf.Abs(y/n/reflection.ReflectionTexture.height-(clip.y/clip.w*.5f+.5f))<.01f,"Far receiver projection mismatch");UnityEngine.Object.DestroyImmediate(marker);UnityEngine.Object.DestroyImmediate(mat);
            }
            report.Add("Fence contact projection, player near / far and multiple elevations: PASS");
            reflection.Refresh();OpenWorldSceneBuilder.Capture(camera,"Previews/38-Terrain-Aligned-Fence-Reflection.png");
            int groundId=ground.GetComponent<PixelArtObject>().objectId,trailId=GameObject.Find("Connected dirt trails").GetComponent<PixelArtObject>().objectId;
            foreach(var torch in env.torches)if(torch)torch.enabled=false;
            camera.transform.position=new Vector3(-2,1,0)-camera.transform.forward*45;camera.orthographicSize=7;env.SetWeather(WeatherKind.Snow,true);
            foreach(float hour in new[]{7f,13f,17.5f,0f})
            {
                env.hour=hour;env.Apply();env.sun.shadows=LightShadows.Hard;var shadowed=Render(camera);var ids=Read(camera.GetComponent<PixelLinework>().IdentityBuffer);env.sun.shadows=LightShadows.None;var lit=Render(camera);int darker=0,brighter=0;float maximum=0;
                for(int i=0;i<lit.Length;i++){int id=ids[i].r+ids[i].g*256;if(id!=groundId&&id!=trailId)continue;float difference=Luma(shadowed[i])-Luma(lit[i]);if(difference<-.012f)darker++;if(difference>.012f)brighter++;maximum=Mathf.Max(maximum,difference);}
                Require(darker>20&&brighter==0,$"Snow shadow inversion at {hour}: dark={darker}, bright={brighter}, max={maximum}");report.Add($"Snow {hour:00.0}h: darker shadow pixels={darker}, brighter shadow pixels={brighter} PASS");env.sun.shadows=LightShadows.Hard;
                if(hour==17.5f)OpenWorldSceneBuilder.Capture(camera,"Previews/39-Snow-Sunset-Shadows.png");if(hour==0)OpenWorldSceneBuilder.Capture(camera,"Previews/40-Snow-Night-Shadows.png");
            }
            Require(reflection.groundReceivers.All(r=>!r.forceRenderingOff)&&!GL.invertCulling,"Reflection receiver state leaked");report.Insert(0,"TERRAIN_REFLECTION_LIGHTING_SUCCESS");File.WriteAllLines("Previews/Terrain-Reflection-Lighting-Verification.txt",report);camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);Debug.Log(string.Join("\n",report));
        }
    }
}
