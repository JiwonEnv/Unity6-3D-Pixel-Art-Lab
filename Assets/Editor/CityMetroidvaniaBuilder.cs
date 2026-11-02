using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace PixelArtLab.Editor
{
    public static class CityMetroidvaniaBuilder
    {
        public const string ScenePath="Assets/Scenes/City-Metroidvania-Rendering-Lab.unity";
        const string Art="Assets/Art/CityMetroidvania";
        static Transform root;static int nextId;static readonly List<Renderer> receivers=new List<Renderer>();static readonly List<Light> lamps=new List<Light>();
        static Material concrete,indoor,metal,trim,cyan,amber,red;
        static Material Paint(string name,Color color,bool glow=false)
        {
            string path=Art+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("PixelArtLab/Sprite Toon"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetColor("_ShadeColor",new Color(.31f,.35f,.46f));m.SetFloat("_SemiPBR",.35f);m.SetFloat("_Roughness",.8f);if(glow)m.SetColor("_EmissionColor",color*.65f);return m;
        }
        static Bounds Bounds(GameObject g){var renderers=g.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);return b;}
        static void Identity(GameObject g,float ink=.15f){var id=g.GetComponent<PixelArtObject>();if(!id)id=g.AddComponent<PixelArtObject>();id.objectId=nextId++;id.interior=ink;id.silhouette=.7f;id.Apply();}
        static GameObject Box(string name,Vector3 position,Vector3 size,Material material,bool solid=false)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.position=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());Identity(g);return g;
        }
        static void Platform(string name,float left,float right,float y,bool sheltered=false)
        {
            var floor=Box(name,new Vector3((left+right)*.5f,y-.35f,0),new Vector3(right-left,.7f,4),sheltered?indoor:concrete,true);
            if(!sheltered)receivers.Add(floor.GetComponent<Renderer>());
            Box("Deck lip",new Vector3((left+right)*.5f,y-.08f,-2.03f),new Vector3(right-left,.16f,.12f),trim);
            for(float x=left+.8f;x<right;x+=4)Box("Deck bracket",new Vector3(x,y-1.05f,1.4f),new Vector3(.16f,1.4f,.22f),metal);
        }
        static GameObject Asset(string pack,string model,Vector3 position,float width,float yaw=0,Transform parent=null)
        {
            string path="Assets/ThirdParty/"+pack+"/"+model+".fbx";var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Missing city asset "+path);
            var holder=new GameObject(model);holder.transform.SetParent(parent?parent:root);holder.transform.position=position;holder.transform.rotation=Quaternion.Euler(0,yaw,0);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,holder.transform);var bounds=Bounds(instance);instance.transform.localScale*=width/Mathf.Max(.01f,bounds.size.x);bounds=Bounds(instance);instance.transform.position+=new Vector3(position.x-bounds.center.x,position.y-bounds.min.y,position.z-bounds.center.z);
            var palette=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/"+pack+"/Textures/colormap.png");
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(original=>
            {
                string token=Hash128.Compute(pack+original.name).ToString().Substring(0,10);var m=Paint("CityPalette-"+token,original.HasProperty("_Color")?original.color:Color.white);m.mainTexture=palette;return m;
            }).ToArray();Identity(holder,.06f);return holder;
        }
        static void Label(string text,Vector3 position,float size,Color color,Transform parent=null)
        {
            var g=new GameObject(text);g.transform.SetParent(parent?parent:root);g.transform.position=position;g.transform.rotation=Quaternion.Euler(12,0,0);var mesh=g.AddComponent<TextMesh>();mesh.text=text;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.fontSize=64;mesh.characterSize=size;mesh.color=color;
        }
        static void Lamp(float x,float y,float z,Color color)
        {
            Box("Street light pole",new Vector3(x,y+1.3f,z),new Vector3(.12f,2.6f,.12f),metal);Box("Street light",new Vector3(x,y+2.65f,z),new Vector3(.8f,.16f,.3f),color.b>color.r?cyan:amber);
            var go=new GameObject("City local light");go.transform.SetParent(root);go.transform.position=new Vector3(x,y+2.35f,z-.3f);var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=3;light.range=8;light.renderMode=LightRenderMode.ForcePixel;lamps.Add(light);
        }
        static Transform Module(string name,float x,float y,Material material,string label)
        {
            Box(name+" pedestal",new Vector3(x,y+.25f,.5f),new Vector3(.8f,.5f,.7f),metal);
            var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name=name;g.transform.SetParent(root);g.transform.position=new Vector3(x,y+.95f,0);g.transform.localScale=Vector3.one*.5f;g.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());Identity(g);Label(label,new Vector3(x,y+1.8f,0),.045f,material.color);return g.transform;
        }
        static void Water(float x,Camera camera)
        {
            var material=Paint("Culvert water",new Color(.07f,.19f,.24f));material.SetFloat("_SemiPBR",.8f);material.SetFloat("_Roughness",.18f);material.SetFloat("_LabWaterFlow",1);material.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/DetailedWoodland/CapillaryNormals.png"));material.SetFloat("_NormalScale",.5f);
            var g=GameObject.CreatePrimitive(PrimitiveType.Plane);g.name="Flooded culvert";g.transform.SetParent(root);g.transform.position=new Vector3(x,.4f,0);g.transform.localScale=new Vector3(.8f,1,.4f);g.layer=4;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;Identity(g,.02f);
            var motion=g.AddComponent<WorldSurfaceMotion>();motion.kind=WorldMotionKind.Water;motion.Apply();var block=new MaterialPropertyBlock();g.GetComponent<Renderer>().GetPropertyBlock(block);block.SetVector("_LabWaterBounds",new Vector4(x,0,4,2));g.GetComponent<Renderer>().SetPropertyBlock(block);
            var reflection=UnityEngine.Object.FindObjectsByType<DetailWaterReflection>(FindObjectsSortMode.None).FirstOrDefault(r=>r.textureProperty=="_DetailReflection");if(!reflection){reflection=g.AddComponent<DetailWaterReflection>();reflection.source=camera;reflection.waterMaterial=material;reflection.waterHeight=.4f;}
        }
        static void Flag(Vector3 position)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Atmosphere/FlexibleFlag.asset");var go=new GameObject("City fabric banner",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=Paint("City banner fabric",new Color(.15f,.45f,.59f));go.transform.localScale=Vector3.one*(2.4f/Mathf.Max(mesh.bounds.size.x,mesh.bounds.size.y));var b=go.GetComponent<Renderer>().bounds;go.transform.position=position-new Vector3(b.center.x,b.min.y,b.center.z);Identity(go);var motion=go.AddComponent<WorldSurfaceMotion>();motion.kind=WorldMotionKind.Flag;motion.Apply();
        }
        [MenuItem("Pixel Art Lab/Create City Metroidvania Scene")]
        public static void Create()
        {
            Directory.CreateDirectory(Art);nextId=12000;receivers.Clear();lamps.Clear();
            foreach(string pack in new[]{"KenneyCityCommercial","KenneyCityIndustrial"})
            {
                string texPath="Assets/ThirdParty/"+pack+"/Textures/colormap.png";var importer=(TextureImporter)AssetImporter.GetAtPath(texPath);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.SaveAndReimport();
            }
            var previous=EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);var playerObject=UnityEngine.Object.Instantiate(UnityEngine.Object.FindFirstObjectByType<ExplorerMotor>().gameObject);playerObject.name="City Explorer — KayKit Knight";
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.MoveGameObjectToScene(playerObject,scene);EditorSceneManager.CloseScene(previous,true);SceneManager.SetActiveScene(scene);
            root=new GameObject("Rainfall City — layered Metroidvania district").transform;
            concrete=Paint("Rain-worn concrete",new Color(.38f,.41f,.45f));concrete.SetFloat("_Puddles",1);concrete.SetFloat("_PuddleWetStrength",1);
            indoor=Paint("Sheltered service deck",new Color(.19f,.22f,.27f));indoor.SetFloat("_SurfaceWeatherExposure",0);
            metal=Paint("Steel structure",new Color(.24f,.29f,.35f));trim=Paint("Ochre safety strip",new Color(.86f,.58f,.22f));cyan=Paint("Cyan signal",new Color(.16f,.78f,.91f),true);amber=Paint("Amber signal",new Color(1,.57f,.18f),true);red=Paint("Locked signal",new Color(.9f,.12f,.18f),true);
            Platform("Station street",-64,-36,8);Platform("Market street",-32,-12,8);Platform("Central street",-8,12,8);Platform("Exchange street",16,34,8);Platform("East street",42,64,8);
            Platform("West drainway",-58,-6,2,true);Platform("Module drainway",2,30,2,true);Platform("East drainway",38,64,2,true);
            Platform("Return stair 1",-35.8f,-34.2f,3.5f,true);Platform("Return stair 2",-33.8f,-32.2f,5,true);Platform("Return stair 3",-35.8f,-34.2f,6.5f);
            Platform("Workshop stair 1",-59,-56,9.5f);Platform("Workshop stair 2",-55,-52,11);Platform("Workshop stair 3",-51,-48,12.5f);Platform("Westworks rooftop",-47,-39,14);Platform("West optional roof",-30,-18,14);
            Platform("Air-step ledge",17.5f,20.5f,11.7f);Platform("Exchange rooftop",22,34,14);Platform("Crown landing",42,64,14);Platform("Crown air-step ledge",46,50,17.5f);Platform("Crown checkpoint roof",40,44,20);Platform("Signal roof",50.5f,64,22);
            Box("West boundary",new Vector3(-66,13,0),new Vector3(1,28,5),metal,true);Box("East boundary",new Vector3(66,13,0),new Vector3(1,28,5),metal,true);
            var gate=Box("Power shutter",new Vector3(-10,13.25f,0),new Vector3(.3f,10.5f,4),red,true);Label("AIR-STEP POWER REQUIRED",new Vector3(-14,10,-.1f),.04f,new Color(1,.5f,.4f));
            var skyline=new GameObject("Distant city parallax").transform;skyline.SetParent(root);
            for(int i=0;i<15;i++)Asset("KenneyCityCommercial","building-skyscraper-"+(char)('a'+i%5),new Vector3(-70+i*10,4,23+(i%3)*3),3.5f+(i%4)*.7f,0,skyline);
            for(int i=0;i<11;i++)
            {
                bool industry=i<3||i>7;string pack=industry?"KenneyCityIndustrial":"KenneyCityCommercial";string model="building-"+(char)('a'+i%14);
                var building=Asset(pack,model,new Vector3(-59+i*12,8,6.5f+(i%2)*2),6.5f);var b=Bounds(building);
                if(i%2==0)Asset("KenneyCityIndustrial","water-tower",new Vector3(building.transform.position.x,b.max.y,7),2.4f);
            }
            Box("Sheltered drainway backing",new Vector3(0,4.5f,4.5f),new Vector3(130,7,.4f),indoor);
            foreach(float x in new[]{-49f,-24f,5f,24f,49f}){Asset("KenneyCityIndustrial","shipping-container-a",new Vector3(x,2,2.8f),3);Asset("KenneyCityIndustrial","detail-tank",new Vector3(x+2.5f,8,3),1.3f);}
            Asset("KenneyCityCommercial","detail-awning-wide",new Vector3(-43,13.4f,2),4);Asset("KenneyCityIndustrial","solar-panel-landscape-group",new Vector3(27,14,3),3.5f);Asset("KenneyCityIndustrial","chimney-medium",new Vector3(55,22,3),2);
            Asset("KenneyNature","tree_default",new Vector3(-57,8,-2.9f),2.4f);Asset("KenneyNature","tree_default_fall",new Vector3(30,8,-2.8f),2.2f);
            // Foreground architectural spans demonstrate the wall-only scan while keeping the lane clear.
            var facade=Paint("Service facade",new Color(.46f,.53f,.59f));Box("Foreground market wall",new Vector3(-23,9.1f,-2.3f),new Vector3(2.2f,2.2f,.25f),facade);Box("Foreground exchange wall",new Vector3(29,15.2f,-2.4f),new Vector3(2,2.4f,.25f),facade);
            Label("SERVICE",new Vector3(-23,9.1f,-2.5f),.045f,new Color(.5f,.85f,1));Label("SERVICE",new Vector3(29,15.2f,-2.6f),.045f,new Color(.5f,.85f,1));
            foreach(float x in new[]{-61f,-52f,-34f,-24f,-14f,3f,21f,30f,44f,54f,62f})Lamp(x,8,-3f,new Color(1,.62f,.3f));Lamp(-44,14,-3f,new Color(.25f,.7f,1));foreach(float x in new[]{-48f,-20f,12f,48f})Lamp(x,2,-3f,new Color(.25f,.8f,1));Lamp(27,14,-3f,new Color(.25f,.8f,1));Lamp(54,22,-3f,new Color(.25f,.8f,1));Flag(new Vector3(-41,14,2.7f));Flag(new Vector3(58,22,2.7f));
            Label("RAINFALL STATION",new Vector3(-53,11.5f,2.5f),.065f,new Color(.58f,.87f,1));Label("WESTWORKS",new Vector3(-43,16.7f,1),.07f,new Color(1,.7f,.35f));Label("DRAINWAY",new Vector3(9,4.7f,2),.07f,new Color(.4f,.8f,.9f));Label("CROWN RELAY",new Vector3(55,25,2),.07f,new Color(.6f,.9f,1));
            for(int i=0;i<8;i++){float x=26;Box("Lift guide rail",new Vector3(x-1.1f,2.7f+i*1.4f,1.6f),new Vector3(.1f,1.3f,.12f),trim);Box("Lift guide rail",new Vector3(x+1.1f,2.7f+i*1.4f,1.6f),new Vector3(.1f,1.3f,.12f),trim);}
            Label("E / SERVICE LIFT",new Vector3(26,4.2f,1),.045f,Color.cyan);
            var player=playerObject.GetComponent<ExplorerMotor>();player.sideScrolling=true;player.sidePlaneZ=0;player.doubleJumpUnlocked=false;player.dashUnlocked=false;player.jumpHeight=2.5f;player.walkSpeed=5.2f;player.sprintSpeed=7.2f;player.steppedFacing=true;player.transform.position=new Vector3(-52,8.1f,0);player.spawnPoint=player.transform.position;player.visual.rotation=Quaternion.Euler(0,90,0);Identity(playerObject,.04f);
            var cameraObject=new GameObject("Side-scrolling pixel camera");var camera=cameraObject.AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=360f/56;camera.nearClipPlane=.1f;camera.farClipPlane=220;camera.clearFlags=CameraClearFlags.SolidColor;camera.transform.rotation=Quaternion.Euler(12,0,0);camera.transform.position=player.transform.position+Vector3.up*2-camera.transform.forward*45;
            var ink=cameraObject.AddComponent<PixelLinework>();ink.lineShader=Shader.Find("Hidden/PixelArtLab/Linework");ink.identityShader=Shader.Find("Hidden/PixelArtLab/Object Identity");var follow=cameraObject.AddComponent<ExplorationCamera>();follow.player=player;follow.linework=ink;follow.pitch=12;follow.yaw=0;follow.pixelsPerUnit=28;follow.sideView=true;player.cameraTransform=camera.transform;
            var alignment=player.GetComponent<PixelCharacterAlignment>();alignment.player=player;alignment.follow=follow;
            var parallax=skyline.gameObject.AddComponent<CityParallax>();parallax.player=player.transform;
            var lightObject=new GameObject("City sun and moon");var sun=lightObject.AddComponent<Light>();sun.type=LightType.Directional;RenderSettings.sun=sun;
            var env=new GameObject("City day night and weather").AddComponent<WorldEnvironment>();env.sun=sun;env.worldCamera=camera;env.player=player;env.torches=lamps.ToArray();env.hour=17;env.runClock=true;env.SetWeather(ValleyWeather.Rain,true);
            var weather=new GameObject("City pixel precipitation").AddComponent<PixelWeatherParticles>();weather.environment=env;weather.weatherMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Atmosphere/Precipitation.mat");weather.gameObject.layer=13;weather.Refresh(true);
            weather.GetComponent<MeshRenderer>().sharedMaterial=weather.weatherMaterial;
            var reflect=new GameObject("City deck planar reflection").AddComponent<DetailWaterReflection>();reflect.source=camera;reflect.waterMaterial=concrete;reflect.textureProperty="_PuddleReflection";reflect.waterHeight=8;reflect.groundReceivers=receivers.ToArray();
            Water(-2,camera);Water(34,camera);
            var steps=new GameObject("City puddle footsteps").AddComponent<PuddleFootsteps>();steps.player=player;steps.environment=env;var prints=new GameObject("City snow footprints").AddComponent<SnowFootprints>();prints.player=player;prints.environment=env;
            var world=new GameObject("City progression and route map").AddComponent<CityMetroidvaniaWorld>();world.player=player;world.follow=follow;world.environment=env;world.powerGate=gate;world.dashModule=Module("Dash module",-43,14,cyan,"E / DASH");world.jumpModule=Module("Air-step module",10,2,amber,"E / AIR STEP");world.signalTerminal=Module("City signal terminal",56,22,cyan,"E / RESTORE SIGNAL");
            world.checkpoints=new[]{new Vector3(-52,8.08f,0),new Vector3(-15,2.08f,0),new Vector3(26,14.08f,0),new Vector3(42,20.08f,0)}.Select((p,i)=>{var g=new GameObject("Checkpoint "+i);g.transform.position=p;Box("Checkpoint beacon",p+new Vector3(0,.5f,.9f),new Vector3(.16f,1,.16f),cyan);return g.transform;}).ToArray();
            var visibility=PlayerVisibilityUpgrade.Configure(camera);var gateTarget=gate.GetComponent<PlayerOccluder>();if(gateTarget)gateTarget.mode=PlayerOcclusionMode.Ignore;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);Preview(env,reflect);EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Debug.Log($"CITY_METROIDVANIA_CREATED platforms={receivers.Count} models={root.childCount} occluders={visibility.occluders.Length}");
        }
        static void Preview(WorldEnvironment env,DetailWaterReflection reflection)
        {
            var camera=env.worldCamera;var position=camera.transform.position;float size=camera.orthographicSize;env.runClock=false;camera.aspect=640f/360;
            foreach(var shot in new[]{new Vector3(-49,10,6.4f),new Vector3(10,5,6.4f),new Vector3(27,15,7),new Vector3(51,20,7)})
            {camera.transform.position=new Vector3(shot.x,shot.y,0)-camera.transform.forward*45;camera.orthographicSize=shot.z;reflection.Refresh();OpenWorldSceneBuilder.Capture(camera,"Previews/City-"+(shot.x<0?"Station":shot.x<20?"Drainway":shot.x<40?"Rooftops":"Crown")+".png");}
            env.hour=13;env.SetWeather(ValleyWeather.Snow,true);camera.transform.position=new Vector3(-49,10,0)-camera.transform.forward*45;camera.orthographicSize=6.4f;UnityEngine.Object.FindFirstObjectByType<PixelWeatherParticles>().Refresh(true);OpenWorldSceneBuilder.Capture(camera,"Previews/City-Snow.png");
            env.hour=17;env.runClock=true;env.SetWeather(ValleyWeather.Rain,true);camera.transform.position=position;camera.orthographicSize=size;
        }
    }
}
