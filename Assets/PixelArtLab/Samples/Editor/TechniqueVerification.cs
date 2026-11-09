using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab.Editor
{
    // R4: opens every technique test scene, switches its technique on and off and requires a
    // visible (or measurable) difference. Saves one preview per technique.
    public static class TechniqueVerification
    {
        const string PreviewFolder="Previews/Techniques";
        static Camera camera;static RenderTexture target;static EnvironmentState env;

        public static void Run()
        {
            var report=new List<string>();Directory.CreateDirectory(PreviewFolder);
            foreach(var name in TechniqueScenes.Names)
            {
                Open(name);
                var lines=Check(name);
                Save(name);
                report.Add($"{name}: PASS");report.AddRange(lines.Select(l=>"  "+l));
                camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
            }
            report.Insert(0,"TECHNIQUE_SCENES_SUCCESS");File.WriteAllLines("Previews/Technique-Verification.txt",report);Debug.Log(string.Join("\n",report));
        }
        static void Open(string name)
        {
            EditorSceneManager.OpenScene(TechniqueScenes.ScenePath(name));
            env=UnityEngine.Object.FindFirstObjectByType<EnvironmentState>();env.animationTime=2;env.Apply();
            camera=GameObject.Find("Technique Camera").GetComponent<Camera>();camera.aspect=640f/360;
            target=new RenderTexture(640,360,24);target.Create();camera.targetTexture=target;
        }
        static List<string> Check(string name)
        {
            var lines=new List<string>();
            void Changed(string what,Color32[] a,Color32[] b,int minimum){int d=Difference(a,b);Require(d>=minimum,$"{name}: {what} changed only {d} pixels");lines.Add($"{what}: {d} px");}
            var baseline=Render();
            switch(name)
            {
                case "ToonLighting":
                {
                    var set=env.GetComponent<SurfaceStyleSet>();var original=set.toonRamp;
                    var ramp=ScriptableObject.CreateInstance<ToonRampStyle>();ramp.bandLevels=new Vector4(0,.2f,.4f,.6f);set.toonRamp=ramp;set.Push();
                    Changed("ToonRampStyle bands",baseline,Render(),1000);set.toonRamp=original;set.Push();
                    Require(Difference(baseline,Render())==0,"Toon ramp preset did not restore");
                    env.hour=21;env.Apply();Changed("Time of day 13h -> 21h",baseline,Render(),1000);env.hour=13;env.Apply();break;
                }
                case "PixelCamera":
                {
                    var follow=camera.GetComponent<ExplorationCamera>();var player=follow.player.transform;var start=player.position;
                    for(int i=0;i<8;i++)
                    {
                        player.position=start+new Vector3(i*.013f,0,i*.007f);follow.SnapToPlayer();
                        var local=Quaternion.Inverse(camera.transform.rotation)*camera.transform.position*follow.pixelsPerUnit*2;
                        Require(Mathf.Abs(local.x-Mathf.Round(local.x))<.01f&&Mathf.Abs(local.y-Mathf.Round(local.y))<.01f,"Camera left the texel grid");
                        Require(Mathf.Abs(follow.SubpixelOffset.x)<=.501f&&Mathf.Abs(follow.SubpixelOffset.y)<=.501f,"Subpixel remainder over half a texel");
                    }
                    Require(camera.targetTexture&&camera.targetTexture.filterMode==FilterMode.Point,"No point-filtered pixel buffer");
                    lines.Add($"Camera snapped to the texel grid for 8 sub-texel moves; buffer {camera.targetTexture.width}x{camera.targetTexture.height}");
                    player.position=start;camera.targetTexture=target;baseline=Render();break;
                }
                case "Linework":
                {
                    var ink=camera.GetComponent<PixelLinework>();ink.linework=false;Changed("Linework off",baseline,Render(),300);ink.linework=true;break;
                }
                case "Wind":
                {
                    env.animationTime=3.4f;env.Apply();Changed("Wind 1.4 s later",baseline,Render(),100);
                    // Same moment with and without wind: only the foliage displacement differs (clouds keep moving either way).
                    var windy=Render();env.windEnabled=false;env.Apply();Changed("Wind off at the same moment",Render(),windy,100);env.windEnabled=true;env.Apply();break;
                }
                case "Wetness":
                {
                    env.SetWeather(WeatherKind.Rain,true);Changed("Rain wetness",baseline,Render(),1000);break;
                }
                case "Snow":
                {
                    env.SetWeather(WeatherKind.Snow,true);var snowy=Render();Changed("Snow cover",baseline,snowy,1000);
                    var prints=UnityEngine.Object.FindFirstObjectByType<SnowFootprints>();
                    Require(prints.TryPlace(new Vector3(1.5f,0,-1),Vector3.forward)&&prints.TryPlace(new Vector3(1.66f,0,-.6f),Vector3.forward),"Footprints rejected on flat ground");
                    prints.Refresh(Time.time);Changed("Footprints",snowy,Render(),4);break;
                }
                case "Puddles":
                {
                    env.SetWeather(WeatherKind.Rain,true);var mirror=UnityEngine.Object.FindFirstObjectByType<DetailWaterReflection>();mirror.Refresh();var wet=Render();
                    var soil=mirror.waterMaterial;soil.SetFloat("_Puddles",0);Changed("Puddles",Render(),wet,500);soil.SetFloat("_Puddles",1);
                    Shader.SetGlobalFloat("_LabReflectionDisabled",1);Changed("Puddle reflection",wet,Render(),20);Shader.SetGlobalFloat("_LabReflectionDisabled",0);
                    Vector3 pool=Vector3.zero;bool found=false;
                    for(float x=-6;x<6&&!found;x+=.25f)for(float z=-6;z<6&&!found;z+=.25f)if(PuddleSurface.Coverage(new Vector3(x,0,z),Vector3.up,env.wetness,env.snowCover)>.5f){pool=new Vector3(x,0,z);found=true;}
                    Require(found,"PuddleSurface finds no pool where the shader draws them");
                    Require(UnityEngine.Object.FindFirstObjectByType<PuddleFootsteps>().TryStep(pool),"Footstep splash rejected in a pool");lines.Add($"Splash accepted in pool at {pool}");
                    break;
                }
                case "Water":
                {
                    var lake=WaterBody.All.First();var mirror=lake.GetComponent<DetailWaterReflection>();mirror.Refresh();var lit=Render();
                    Shader.SetGlobalFloat("_LabReflectionDisabled",1);Changed("Water reflection",lit,Render(),100);Shader.SetGlobalFloat("_LabReflectionDisabled",0);
                    var radii=lake.radii;lake.radii*=.8f;lake.Apply();Changed("WaterBody shore (foam)",lit,Render(),50);lake.radii=radii;lake.Apply();
                    Require(Mathf.Approximately(WaterBody.SurfaceAt(lake.center.x,lake.center.y,-9),lake.surfaceHeight),"Water surface lookup");lines.Add("Rain lands on WaterBody surface: PASS");break;
                }
                case "PracticalLights":
                {
                    var practical=PracticalLight.All.First(p=>p);practical.Apply(0,.10f);float a=practical.Light.intensity;practical.Apply(0,.43f);float b=practical.Light.intensity;
                    Require(!Mathf.Approximately(a,b),"Flicker does not change intensity");lines.Add($"Flicker intensity {a:0.00} -> {b:0.00}");
                    env.Apply();var night=Render();foreach(var p in PracticalLight.All)if(p)p.Light.enabled=false;Changed("Lanterns at night",night,Render(),500);foreach(var p in PracticalLight.All)if(p)p.Light.enabled=true;
                    break;
                }
                case "Atmosphere":
                {
                    env.heightFog=false;env.Apply();Changed("Height fog",Render(),baseline,500);env.heightFog=true;env.Apply();
                    var air=camera.GetComponent<PixelVolumetrics>();air.enabled=false;Changed("Volumetric light",Render(),baseline,100);air.enabled=true;
                    var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.Add<PixelAtmosphere>().heightFogDensity.Override(4);
                    var holder=new GameObject("Fog zone");var volume=holder.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
                    Changed("PixelAtmosphere fog zone",baseline,Render(),500);UnityEngine.Object.DestroyImmediate(holder);
                    Require(Difference(baseline,Render())==0,"Fog zone stayed after the volume left");break;
                }
                case "Visibility":
                {
                    var visibility=camera.GetComponent<PlayerVisibility>();visibility.naturalReveal=true;for(int i=0;i<4;i++)visibility.Refresh(1);
                    Require(visibility.FadingObjects>0,"Wall not detected in front of the character");var revealed=Render();
                    visibility.naturalReveal=false;for(int i=0;i<4;i++)visibility.Refresh(1);Changed("Wall reveal",Render(),revealed,300);
                    visibility.naturalReveal=true;for(int i=0;i<4;i++)visibility.Refresh(1);baseline=Render();break;
                }
            }
            return lines;
        }
        static Color32[] Render(){LabCameraRender.Render(camera);return Read(camera.targetTexture);}
        static void Save(string name)
        {
            camera.targetTexture=target;LabCameraRender.Render(camera);
            var previous=RenderTexture.active;RenderTexture.active=target;var t=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            t.ReadPixels(new Rect(0,0,target.width,target.height),0,0);t.Apply();File.WriteAllBytes($"{PreviewFolder}/{name}.png",t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=previous;
        }
        static Color32[] Read(RenderTexture texture)
        {
            var previous=RenderTexture.active;RenderTexture.active=texture;
            var t=new Texture2D(texture.width,texture.height,TextureFormat.RGB24,false);
            t.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);t.Apply();var data=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);RenderTexture.active=previous;return data;
        }
        static int Difference(Color32[] a,Color32[] b)=>a.Length!=b.Length?int.MaxValue:a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)>8?1:0).Sum();
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
    }
}
