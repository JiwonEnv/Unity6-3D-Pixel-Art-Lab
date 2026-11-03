using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PixelArtLab.Editor
{
    public static class ReflectionShadowVerification
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);var env=Object.FindFirstObjectByType<WorldEnvironment>();env.SetWeather(ValleyWeather.Rain,true);env.hour=13;env.Apply();
            Debug.Log($"SHADOW_DIAG distance={QualitySettings.shadowDistance} quality={QualitySettings.shadows} light={env.sun.shadows} camera={env.worldCamera.transform.position}");
            foreach(var r in env.player.GetComponentsInChildren<Renderer>())Debug.Log($"PLAYER_RENDER {r.name} layer={r.gameObject.layer} enabled={r.enabled} shadows={r.shadowCastingMode} bounds={r.bounds}");
            var reflection=Object.FindObjectsByType<DetailWaterReflection>(FindObjectsSortMode.None).First(r=>r.textureProperty=="_PuddleReflection");
            env.worldCamera.aspect=640f/360;env.worldCamera.orthographicSize=5;env.worldCamera.transform.position=env.player.transform.position+Vector3.up-env.worldCamera.transform.forward*45;reflection.Refresh();
            var rt=reflection.ReflectionTexture;var before=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();File.WriteAllBytes("Previews/34-Reflection-Diagnostic.png",t.EncodeToPNG());Object.DestroyImmediate(t);RenderTexture.active=before;
            // Locate an isolated emissive marker's actual texture coordinates versus CPU projection.
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.transform.position=env.player.transform.position+new Vector3(1,2,0);marker.transform.localScale=Vector3.one*.25f;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=Color.magenta;marker.GetComponent<Renderer>().sharedMaterial=mat;reflection.Refresh();
            RenderTexture.active=rt;t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();var pixels=t.GetPixels32();float x=0,y=0,n=0;
            for(int j=0;j<rt.height;j++)for(int i=0;i<rt.width;i++){var c=pixels[j*rt.width+i];if(c.r>200&&c.b>200&&c.g<80){x+=i;y+=j;n++;}}
            var mirror=Resources.FindObjectsOfTypeAll<Camera>().First(c=>c.name=="Detail Water Reflection Camera"&&c.targetTexture==rt);var cpu=mirror.WorldToViewportPoint(marker.transform.position);var gpu=GL.GetGPUProjectionMatrix(mirror.projectionMatrix,true)*mirror.worldToCameraMatrix*new Vector4(marker.transform.position.x,marker.transform.position.y,marker.transform.position.z,1);
            Debug.Log($"REFLECTION_UV actual=({x/n/rt.width},{y/n/rt.height}) cpu={cpu} gpu=({gpu.x/gpu.w*.5f+.5f},{gpu.y/gpu.w*.5f+.5f}) count={n}");
            var corrected=GL.GetGPUProjectionMatrix(mirror.projectionMatrix,false)*mirror.worldToCameraMatrix*new Vector4(marker.transform.position.x,marker.transform.position.y,marker.transform.position.z,1);
            if(n<1||Mathf.Abs(x/n/rt.width-(corrected.x/corrected.w*.5f+.5f))>.01f||Mathf.Abs(y/n/rt.height-(corrected.y/corrected.w*.5f+.5f))>.01f)throw new System.Exception("Reflection coordinate calibration failed");
            RenderTexture.active=before;Object.DestroyImmediate(t);Object.DestroyImmediate(marker);Object.DestroyImmediate(mat);
            reflection.Refresh();var original=Read(rt);var character=env.player.GetComponentsInChildren<Renderer>();foreach(var r in character)r.enabled=false;reflection.Refresh();int characterPixels=Difference(original,Read(rt));foreach(var r in character)r.enabled=true;
            if(characterPixels<50)throw new System.Exception("Character missing from reflection");
            env.SetWeather(ValleyWeather.Clear,true);env.hour=10;env.Apply();var target=new RenderTexture(640,360,24);target.Create();env.worldCamera.targetTexture=target;
            var shadows=Render(env.worldCamera);foreach(var r in character)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;int playerShadow=Difference(shadows,Render(env.worldCamera));foreach(var r in character)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            var tree=GameObject.Find("Poly Haven Detailed Woodland").GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("island_tree")).ToArray();foreach(var r in tree)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;int treeShadow=Difference(shadows,Render(env.worldCamera));foreach(var r in tree)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            if(playerShadow<10||treeShadow<100)throw new System.Exception($"Shadow missing: character={playerShadow} tree={treeShadow}");
            env.hour=0;env.Apply();if(!env.torches.Any(l=>l&&l.shadows!=LightShadows.None))throw new System.Exception("No local torch shadow enabled");var night=Render(env.worldCamera);foreach(var l in env.torches)if(l)l.shadows=LightShadows.None;int torchShadow=Difference(night,Render(env.worldCamera));if(torchShadow<10)throw new System.Exception("Torch shadows not visible: "+torchShadow);env.hour=10;env.Apply();
            OpenWorldSceneBuilder.Capture(env.worldCamera,"Previews/35-Character-Tree-Shadows.png");env.worldCamera.targetTexture=null;target.Release();Object.DestroyImmediate(target);
            env.SetWeather(ValleyWeather.Rain,true);env.hour=13;env.Apply();Vector3 toward= -env.worldCamera.transform.forward;toward.y=0;toward.Normalize();bool found=false;
            for(float px=-10;px<10&&!found;px+=.5f)for(float pz=-10;pz<10&&!found;pz+=.5f)
            {
                Vector3 p=new Vector3(px,OpenWorldSceneBuilder.Height(px,pz)+.1f,pz);if(!PuddleSurface.Ground(p,out var hit)||PuddleSurface.Coverage(hit.point,hit.normal,1,0)<.7f)continue;
                bool fits=true;for(float d=.5f;d<=2;d+=.5f){var q=hit.point+toward*d;if(!PuddleSurface.Ground(q,out var h)||PuddleSurface.Coverage(h.point,h.normal,1,0)<.6f)fits=false;}
                if(fits){env.player.transform.position=hit.point+Vector3.up*.04f;found=true;}
            }
            if(!found)throw new System.Exception("No pool suitable for character reflection test");
            env.worldCamera.orthographicSize=4.5f;env.worldCamera.transform.position=env.player.transform.position+Vector3.up*.8f-env.worldCamera.transform.forward*45;
            target=new RenderTexture(640,360,24);target.Create();env.worldCamera.targetTexture=target;reflection.Refresh();var surfaceWith=Render(env.worldCamera);
            foreach(var r in character)r.enabled=false;reflection.Refresh();foreach(var r in character)r.enabled=true;int visibleCharacter=Difference(surfaceWith,Render(env.worldCamera));if(visibleCharacter<50)throw new System.Exception("Character reflection absent on actual puddle surface: "+visibleCharacter);
            reflection.Refresh();OpenWorldSceneBuilder.Capture(env.worldCamera,"Previews/37-Corrected-Character-Reflection.png");env.worldCamera.targetTexture=null;target.Release();Object.DestroyImmediate(target);
            File.WriteAllText("Previews/Reflection-Shadow-Verification.txt",$"REFLECTION_SHADOW_SUCCESS\nReflection marker coordinates match within 1%: PASS\nCharacter reflection texture pixels: {characterPixels}\nCharacter reflection on actual puddle pixels: {visibleCharacter}\nCharacter shadow pixels: {playerShadow}\nTree shadow pixels: {treeShadow}\nNight torch shadow pixels: {torchShadow}\nShadow distance: {QualitySettings.shadowDistance}m\nNO PLAYER BUILD\n");Debug.Log("REFLECTION_SHADOW_SUCCESS");
        }
        static Color32[] Read(RenderTexture rt){var old=RenderTexture.active;RenderTexture.active=rt;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);t.Apply();var p=t.GetPixels32();Object.DestroyImmediate(t);RenderTexture.active=old;return p;}
        static Color32[] Render(Camera c){LabCameraRender.Render(c);return Read(c.targetTexture);}
        static int Difference(Color32[] a,Color32[] b)=>a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)>8?1:0).Sum();
    }
}
