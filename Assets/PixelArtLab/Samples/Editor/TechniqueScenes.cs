using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab.Editor
{
    // R4: one small test scene per technique, built only from PixelArtLab.Runtime, primitives
    // and the default presets. If a technique works here it does not depend on the sample art.
    public static class TechniqueScenes
    {
        public const string Folder="Assets/PixelArtLab/Samples/Techniques";
        public static readonly string[] Names={"ToonLighting","PixelCamera","Linework","Wind","Wetness","Snow","Puddles","Water","PracticalLights","Atmosphere","Visibility"};
        public static string ScenePath(string name)=>$"{Folder}/Scenes/Technique-{name}.unity";
        static int nextId;

        [MenuItem("Pixel Art Lab/Techniques/Build Test Scenes")]
        public static void BuildAll()
        {
            foreach(var name in Names)Build(name);
            AssetDatabase.SaveAssets();
            Debug.Log("TECHNIQUE_SCENES_SUCCESS "+Names.Length);
        }

        // Shared rig: ground, three props, a capsule character, sun, environment and camera.
        public sealed class Rig
        {
            public EnvironmentState env;public Camera camera;public Light sun;public PixelLinework ink;
            public GameObject ground,crate,post,ball;public MotionCharacter character;
        }
        static void Build(string name)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);nextId=1;
            var rig=CreateRig();
            switch(name)
            {
                case "PixelCamera":
                    var follow=rig.camera.gameObject.AddComponent<ExplorationCamera>();follow.player=rig.character;follow.linework=rig.ink;follow.pixelsPerUnit=25;break;
                case "Wind":
                    Tree(new Vector3(-2.5f,0,-1.5f));Tree(new Vector3(2.8f,0,-2.2f));
                    for(int i=0;i<7;i++){var blade=Prop(PrimitiveType.Cube,"Grass "+i,new Vector3(-1+i*.35f,.3f,2.4f),new Vector3(.08f,.6f,.08f),Mat("Grass",new Color(.36f,.62f,.30f)));Motion(blade,WorldMotionKind.Grass);}
                    break;
                case "Snow":
                    var prints=new GameObject("Snow Footprints").AddComponent<SnowFootprints>();prints.player=rig.character;prints.environment=rig.env;break;
                case "Puddles":
                    var soil=Mat("PuddleGround",new Color(.52f,.44f,.34f),m=>m.SetFloat("_Puddles",1));rig.ground.GetComponent<Renderer>().sharedMaterial=soil;
                    // Flat test ground sits at height 0, below the default puddle minimum (a lake shore).
                    var flat=Style<PuddleStyle>("SO_Puddles_FlatGround");flat.minimumHeight=-10;flat.cellSize=4;EditorUtility.SetDirty(flat);
                    rig.env.GetComponent<SurfaceStyleSet>().puddles=flat;
                    var mirror=rig.ground.AddComponent<DetailWaterReflection>();mirror.source=rig.camera;mirror.waterMaterial=soil;mirror.textureProperty="_PuddleReflection";mirror.waterHeight=0;mirror.groundReceivers=new[]{rig.ground.GetComponent<Renderer>()};
                    var steps=new GameObject("Puddle Footsteps").AddComponent<PuddleFootsteps>();steps.player=rig.character;steps.environment=rig.env;steps.targetCamera=rig.camera;
                    break;
                case "Water":
                    var water=Mat("Water",new Color(.10f,.25f,.28f),m=>{m.SetFloat("_WaterSurface",1);m.SetFloat("_SemiPBR",.8f);m.SetFloat("_Roughness",.18f);m.SetFloat("_ReflectionStrength",.95f);m.SetFloat("_LabWaterFlow",1);});
                    var lake=Prop(PrimitiveType.Cylinder,"Pond",new Vector3(-.5f,.02f,-1.5f),new Vector3(6,.02f,4.5f),water);lake.layer=LabLayers.Water;Object.DestroyImmediate(lake.GetComponent<Collider>());
                    Motion(lake,WorldMotionKind.Water);lake.AddComponent<WaterBody>().Fit();
                    var reflect=lake.AddComponent<DetailWaterReflection>();reflect.source=rig.camera;reflect.waterMaterial=water;reflect.textureProperty="_DetailReflection";reflect.waterHeight=.04f;
                    rig.ball.transform.position=new Vector3(-1,.6f,-.2f);
                    break;
                case "PracticalLights":
                    Lantern(new Vector3(-1.5f,0,-1.5f));Lantern(new Vector3(2,0,1));rig.env.hour=0;break;
                case "Atmosphere":
                    Lantern(new Vector3(-1.5f,0,-1.5f));rig.env.hour=21;
                    var air=rig.camera.gameObject.AddComponent<PixelVolumetrics>();air.shader=Shader.Find("Hidden/PixelArtLab/Volumetrics");air.environment=rig.env;break;
                case "Visibility":
                    var wall=Prop(PrimitiveType.Cube,"Wall",Vector3.zero,new Vector3(3.5f,3,.4f),Mat("Wall",new Color(.62f,.58f,.52f)));
                    Vector3 toCamera=-rig.camera.transform.forward;toCamera.y=0;toCamera.Normalize();
                    wall.transform.position=rig.character.transform.position+toCamera*1.6f+Vector3.up*1.5f;wall.transform.rotation=Quaternion.LookRotation(toCamera);
                    var occluder=wall.AddComponent<PlayerOccluder>();occluder.mode=PlayerOcclusionMode.NaturalFade;occluder.renderers=new[]{wall.GetComponent<Renderer>()};
                    var visibility=rig.camera.gameObject.AddComponent<PlayerVisibility>();visibility.player=rig.character;visibility.occluders=new[]{occluder};
                    break;
            }
            rig.env.Apply();
            Directory.CreateDirectory(Folder+"/Scenes");
            EditorSceneManager.SaveScene(scene,ScenePath(name));
        }
        static Rig CreateRig()
        {
            var rig=new Rig();
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.40f,.44f,.50f);RenderSettings.fog=false;RenderSettings.skybox=null;
            rig.ground=Prop(PrimitiveType.Cube,"Ground",new Vector3(0,-.1f,0),new Vector3(16,.2f,16),Mat("Ground",new Color(.47f,.56f,.36f)));
            rig.crate=Prop(PrimitiveType.Cube,"Crate",new Vector3(-1.6f,.5f,.8f),Vector3.one,Mat("Crate",new Color(.62f,.42f,.26f)));
            rig.post=Prop(PrimitiveType.Cylinder,"Post",new Vector3(.4f,1.2f,-2.4f),new Vector3(.35f,1.2f,.35f),Mat("Stone",new Color(.58f,.58f,.62f)));
            rig.ball=Prop(PrimitiveType.Sphere,"Ball",new Vector3(2.2f,.6f,1.4f),Vector3.one*1.2f,Mat("Ball",new Color(.78f,.36f,.30f)));
            var body=Prop(PrimitiveType.Capsule,"Character",new Vector3(.6f,1,.4f),new Vector3(.7f,1,.7f),Mat("Character",new Color(.30f,.42f,.72f)));
            body.layer=LabLayers.Character;rig.character=body.AddComponent<MotionCharacter>();

            rig.sun=new GameObject("Sun").AddComponent<Light>();rig.sun.type=LightType.Directional;rig.sun.shadows=LightShadows.Hard;rig.sun.shadowBias=.035f;rig.sun.shadowNormalBias=.18f;RenderSettings.sun=rig.sun;

            rig.camera=new GameObject("Technique Camera").AddComponent<Camera>();rig.camera.tag="MainCamera";rig.camera.orthographic=true;rig.camera.orthographicSize=4.2f;
            rig.camera.nearClipPlane=.1f;rig.camera.farClipPlane=120;rig.camera.clearFlags=CameraClearFlags.SolidColor;rig.camera.allowMSAA=false;
            rig.camera.transform.rotation=Quaternion.Euler(38,42,0);rig.camera.transform.position=new Vector3(0,.8f,0)-rig.camera.transform.forward*45;
            rig.ink=rig.camera.gameObject.AddComponent<PixelLinework>();rig.ink.lineShader=Shader.Find("Hidden/PixelArtLab/Linework");rig.ink.identityShader=Shader.Find("Hidden/PixelArtLab/Object Identity");rig.ink.strength=.82f;rig.ink.normalThreshold=.44f;

            var holder=new GameObject("Environment");
            rig.env=holder.AddComponent<EnvironmentState>();rig.env.timeOfDay=Preset<TimeOfDayProfile>("SO_TimeOfDay_Default");rig.env.weatherProfile=Preset<WeatherProfile>("SO_Weather_Default");
            rig.env.hour=13;rig.env.runClock=false;rig.env.SetWeather(WeatherKind.Clear,true);
            holder.AddComponent<SunMoonDriver>().sun=rig.sun;holder.AddComponent<SkyFogDriver>().targetCamera=rig.camera;holder.AddComponent<PracticalLightManager>().focus=body.transform;
            var styles=holder.AddComponent<SurfaceStyleSet>();
            styles.toonRamp=Preset<ToonRampStyle>("SO_ToonRamp_Default");styles.snow=Preset<SnowStyle>("SO_Snow_Default");styles.wetness=Preset<WetnessStyle>("SO_Wetness_Default");
            styles.puddles=Preset<PuddleStyle>("SO_Puddles_Default");styles.water=Preset<WaterStyle>("SO_Water_Default");styles.characterLight=Preset<CharacterLightStyle>("SO_CharacterLight_Default");
            return rig;
        }
        static GameObject Prop(PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.position=position;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
            var id=g.AddComponent<PixelArtObject>();id.objectId=nextId++;id.Apply();return g;
        }
        static void Motion(GameObject target,WorldMotionKind kind){var motion=target.AddComponent<WorldSurfaceMotion>();motion.kind=kind;motion.Apply();}
        static void Tree(Vector3 at)
        {
            Prop(PrimitiveType.Cylinder,"Trunk",at+Vector3.up*1,new Vector3(.3f,1,.3f),Mat("Bark",new Color(.42f,.30f,.22f)));
            var canopy=Prop(PrimitiveType.Sphere,"Canopy",at+Vector3.up*2.6f,new Vector3(2,1.8f,2),Mat("Leaves",new Color(.30f,.52f,.28f)));Motion(canopy,WorldMotionKind.Tree);
        }
        static void Lantern(Vector3 at)
        {
            Prop(PrimitiveType.Cube,"Lantern Post",at+Vector3.up*.9f,new Vector3(.12f,1.8f,.12f),Mat("Iron",new Color(.22f,.20f,.19f)));
            var glass=Prop(PrimitiveType.Cube,"Lantern Glass",at+Vector3.up*1.9f,Vector3.one*.28f,Mat("LanternGlass",new Color(1,.8f,.5f),m=>m.SetColor("_EmissionColor",new Color(2.2f,1.45f,.8f))));
            var light=new GameObject("Lantern Light").AddComponent<Light>();light.type=LightType.Point;light.range=6;light.color=new Color(1,.72f,.42f);light.shadows=LightShadows.Hard;
            light.transform.position=at+Vector3.up*1.9f;
            var practical=light.gameObject.AddComponent<PracticalLight>();practical.nightIntensity=3;practical.dayIntensity=0;practical.glow=new[]{glass.GetComponent<Renderer>()};
        }
        static Material Mat(string name,Color color,System.Action<Material> setup=null)
        {
            string path=$"{Folder}/Materials/MAT_Technique_{name}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){Directory.CreateDirectory(Folder+"/Materials");m=new Material(Shader.Find("PixelArtLab/Sprite Toon"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetColor("_ShadeColor",new Color(.42f,.42f,.56f));setup?.Invoke(m);SpriteToonKeywords.Sync(m);EditorUtility.SetDirty(m);return m;
        }
        static T Preset<T>(string name) where T:Object=>AssetDatabase.LoadAssetAtPath<T>(EnvironmentMigration.PresetsFolder+"/"+name+".asset");
        static T Style<T>(string name) where T:SurfaceStyle
        {
            string path=$"{Folder}/Styles/{name}.asset";var s=AssetDatabase.LoadAssetAtPath<T>(path);if(s)return s;
            Directory.CreateDirectory(Folder+"/Styles");s=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(s,path);return s;
        }
    }
}
