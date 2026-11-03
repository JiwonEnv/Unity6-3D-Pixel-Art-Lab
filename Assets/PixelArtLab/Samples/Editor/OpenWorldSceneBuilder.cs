using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab.Editor
{
    public static class OpenWorldSceneBuilder
    {
        public const string ScenePath="Assets/Scenes/Amberwood-Free-Roam.unity";
        const string Generated="Assets/Art/OpenWorld";
        static Transform worldRoot;
        static int id;
        static System.Random random;
        static readonly Dictionary<Material,Material> materials=new Dictionary<Material,Material>();
        static readonly Vector2[] destinations={new Vector2(0,0),new Vector2(34,26),new Vector2(-17,12),new Vector2(-33,-30),new Vector2(39,-30)};
        static float R(float a,float b)=>a+(float)random.NextDouble()*(b-a);
        public static float Height(float x,float z)
        {
            float h=1.1f+Mathf.Sin(x*.055f)*.8f+Mathf.Sin(z*.065f)*.7f;
            h+=6*Mathf.Exp(-((x-39)*(x-39)+(z+30)*(z+30))/450f);
            h+=2.6f*Mathf.Exp(-((x-34)*(x-34)+(z-26)*(z-26))/600f);
            h-=3.7f*Mathf.Exp(-((x+30)*(x+30)+(z-22)*(z-22))/145f);
            return Mathf.Lerp(h,1.1f,1-Mathf.SmoothStep(0,1,new Vector2(x,z).magnitude/13));
        }
        static Vector3 Surface(float x,float z)=>new Vector3(x,Height(x,z),z);
        static float Segment(Vector2 p,Vector2 a,Vector2 b){Vector2 d=b-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));}
        static float TrailDistance(Vector2 p)
        {
            float d=float.MaxValue;for(int i=1;i<destinations.Length;i++)d=Mathf.Min(d,Segment(p,Vector2.zero,destinations[i]));
            d=Mathf.Min(d,Segment(p,destinations[2]+new Vector2(0,-17),destinations[3]));
            d=Mathf.Min(d,Segment(p,destinations[1],destinations[4]));return d;
        }
        static Material Paint(string name,Color color,Texture texture=null,bool emission=false)
        {
            string path=Generated+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("PixelArtLab/Sprite Toon"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetColor("_ShadeColor",new Color(.42f,.42f,.56f));m.SetFloat("_LocalLightStrength",1.2f);
            if(texture)m.mainTexture=texture;if(emission)m.SetColor("_EmissionColor",color*BackgroundLighting.FlameEmission);return m;
        }
        static Material Palette(Material source)
        {
            if(materials.TryGetValue(source,out var result))return result;
            string name=source.name.ToLowerInvariant();Color color=source.color;
            if(!source.mainTexture)
            {
                if(name.Contains("leaf")||name.Contains("grass"))color=new Color(.34f,.53f,.34f);
                if(name.Contains("fall"))color=new Color(.77f,.45f,.22f);
                if(name.Contains("bark"))color=new Color(.38f,.25f,.22f);
                else if(name.Contains("wood"))color=new Color(.62f,.43f,.29f);
                if(name.Contains("stone"))color=new Color(.65f,.64f,.58f);
                if(name.Contains("dirt"))color=new Color(.48f,.40f,.34f);
            }
            string key=AssetDatabase.GetAssetPath(source)+"/"+source.name;
            string token=Hash128.Compute(key).ToString().Substring(0,12);
            result=Paint("Imported-"+token,color,source.mainTexture);materials.Add(source,result);return result;
        }
        static Bounds BoundsOf(GameObject g)
        {
            bool first=true;var b=new Bounds();
            foreach(var renderer in g.GetComponentsInChildren<Renderer>())
            {
                if(renderer is SkinnedMeshRenderer skin)
                {
                    var baked=new Mesh();skin.BakeMesh(baked,false);
                    foreach(var vertex in baked.vertices){Vector3 p=skin.transform.TransformPoint(vertex);if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                else {if(first){b=renderer.bounds;first=false;}else b.Encapsulate(renderer.bounds);}
            }
            return b;
        }
        static GameObject Asset(string pack,string model,Vector3 point,float size,float yaw=0,bool solid=true,bool tree=false,bool heightSize=false)
        {
            string path="Assets/ThirdParty/"+pack+"/"+model+".fbx";
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Missing asset "+path);
            var holder=new GameObject(model);holder.transform.SetParent(worldRoot);holder.transform.position=point;holder.transform.rotation=Quaternion.Euler(0,yaw,0);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,holder.transform);instance.transform.localPosition=Vector3.zero;
            var bounds=BoundsOf(instance);float dimension=heightSize?bounds.size.y:Mathf.Max(bounds.size.x,bounds.size.z);
            instance.transform.localScale*=size/Mathf.Max(.01f,dimension);
            bounds=BoundsOf(instance);instance.transform.position+=Vector3.up*(point.y-bounds.min.y);
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(Palette).ToArray();
            var data=holder.AddComponent<PixelArtObject>();data.objectId=id++;data.interior=tree?.025f:.12f;data.silhouette=tree?.85f:1;data.Apply();
            if(solid)
            {
                if(tree)
                {
                    var trunk=holder.AddComponent<CapsuleCollider>();trunk.radius=Mathf.Max(.24f,size*.075f);trunk.height=Mathf.Min(size*.65f,4);trunk.center=Vector3.up*trunk.height*.5f;
                }
                else foreach(var filter in instance.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh){var collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;}
            }
            return holder;
        }
        static GameObject Primitive(string name,Vector3 p,Vector3 scale,Material material,PrimitiveType type=PrimitiveType.Cube,bool solid=true)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(worldRoot);g.transform.position=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            var data=g.AddComponent<PixelArtObject>();data.objectId=id++;data.interior=.05f;data.Apply();return g;
        }
        [MenuItem("Pixel Art Lab/Build Playable Amberwood Free Roam")]
        public static void Build()
        {
            Directory.CreateDirectory(Generated);Directory.CreateDirectory("Previews");random=new System.Random(7724);id=1000;materials.Clear();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);worldRoot=new GameObject("Amberwood — 150m Seamless Exploration Map").transform;
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/ThirdParty"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer){importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();}
            }
            Ground();
            var water=Paint("Lake Teal",new Color(.20f,.43f,.48f));
            Primitive("Stillwater Lake",new Vector3(-30,.30f,22),new Vector3(23,.05f,21),water,PrimitiveType.Cylinder,false);
            Asset("KenneyNature","canoe",new Vector3(-21,.35f,21),3,25,false);
            // Region clearing is intentionally generous so landmarks remain walkable.
            for(int i=0;i<480;i++)
            {
                float x=R(-72,72),z=R(-72,72);Vector2 point=new Vector2(x,z);
                if(TrailDistance(point)<4.2f || destinations.Any(p=>Vector2.Distance(p,point)<10) || Height(x,z)<.5f)continue;
                string model=z<-12?new[]{"tree_pineTallA","tree_pineRoundC","tree_detailed"}[i%3]:new[]{"tree_oak","tree_default","tree_detailed","tree_default_fall"}[i%4];
                Asset("KenneyNature",model,Surface(x,z),R(4.1f,7.2f),R(0,360),true,true,true);
            }
            for(int i=0;i<230;i++)
            {
                float x=R(-71,71),z=R(-71,71);Vector2 point=new Vector2(x,z);
                if(TrailDistance(point)<2.8f||destinations.Any(p=>Vector2.Distance(point,p)<7)||Height(x,z)<.4f)continue;
                if(i%3==0)Asset("KenneyNature",i%2==0?"rock_largeA":"rock_largeB",Surface(x,z),R(1.2f,3.4f),R(0,360));
                else Asset("KenneyNature",i%4==0?"plant_bushLarge":i%3==1?"grass_large":"flower_yellowA",Surface(x,z),R(.55f,1.5f),R(0,360),false);
            }
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI*2/32,x=-30+Mathf.Cos(a)*14,z=22+Mathf.Sin(a)*12;
                Asset("KenneyNature",i%2==0?"rock_largeC":"rock_smallA",Surface(x,z),R(1.4f,2.6f),R(0,360));
            }
            Camp();Ruins();Lookout();
            // Broad stepping stones: jumpable collision surfaces without blocking the trails.
            for(int i=0;i<5;i++)
            {
                float x=11+i*1.8f,z=-5;var rock=Asset("KenneyNature","rock_largeC",Surface(x,z),1.4f,0,true);
                rock.transform.position+=Vector3.up*(.18f+i*.12f);
            }
            for(int i=0;i<4;i++)Asset("KenneyNature","sign",Surface(destinations[i+1].x*.35f+2,destinations[i+1].y*.35f),1.15f,42);
            // Invisible boundary prevents leaving the finite prototype; the forest frames it.
            foreach(float x in new[]{-76f,76f})Boundary(new Vector3(x,10,0),new Vector3(1,40,154));
            foreach(float z in new[]{-76f,76f})Boundary(new Vector3(0,10,z),new Vector3(154,40,1));
            var player=Player();
            var camera=new GameObject("Pixel Follow Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=180;
            camera.orthographicSize=7.2f;camera.allowHDR=false;camera.allowMSAA=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.22f,.25f);
            camera.transform.rotation=Quaternion.Euler(38,42,0);camera.transform.position=player.transform.position+Vector3.up*.95f-camera.transform.forward*45;
            camera.gameObject.AddComponent<AudioListener>();var ink=camera.gameObject.AddComponent<PixelLinework>();ink.lineShader=Shader.Find("Hidden/PixelArtLab/Linework");ink.identityShader=Shader.Find("Hidden/PixelArtLab/Object Identity");ink.strength=.82f;ink.normalThreshold=.44f;
            var follow=camera.gameObject.AddComponent<ExplorationCamera>();follow.player=player;follow.linework=ink;player.cameraTransform=camera.transform;
            var sun=new GameObject("Warm afternoon key").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.color=new Color(1,.94f,.82f);sun.intensity=1;
            sun.shadows=LightShadows.Hard;sun.shadowBias=.035f;sun.shadowNormalBias=.18f;RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.40f,.44f,.50f);RenderSettings.fog=false;
            QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=42;QualitySettings.shadowCascades=2;QualitySettings.pixelLightCount=4;QualitySettings.antiAliasing=0;
            var game=new GameObject("Exploration Objectives and HUD").AddComponent<ExplorationWorld>();game.player=player;game.followCamera=follow;
            game.landmarkNames=new[]{"Amberwood Camp","Oldwatch Ruins","Stillwater Lake","Whispering Pines","Sunrise Lookout"};
            game.landmarks=destinations.Select((p,i)=>{var g=new GameObject(game.landmarkNames[i]);g.transform.SetParent(worldRoot);g.transform.position=Surface(p.x,p.y);return g.transform;}).ToArray();
            game.crystals=new[]{Waystone(new Vector2(34,23)),Waystone(new Vector2(-33,-30)),Waystone(new Vector2(39,-30))};
            Shader.SetGlobalFloat("_LabContinuous",0);PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene("Assets/Scenes/3D-Sprite-Rendering-Study.unity",true),new EditorBuildSettingsScene("Assets/Scenes/3D-Pixel-Art-Lighting-Study.unity",true)};
            AssetDatabase.SaveAssets();
            Capture(camera,"Previews/11-Amberwood-Camp.png");
            camera.transform.position=Surface(34,26)+Vector3.up*1.5f-camera.transform.forward*45;Capture(camera,"Previews/12-Amberwood-Ruins.png");
            camera.transform.position=Surface(-30,22)+Vector3.up*2-camera.transform.forward*45;Capture(camera,"Previews/13-Amberwood-Lake.png");
            camera.orthographicSize=85;camera.transform.rotation=Quaternion.Euler(65,0,0);camera.transform.position=new Vector3(0,2,0)-camera.transform.forward*110;Capture(camera,"Previews/14-Amberwood-Map.png");
            Debug.Log("OPEN_WORLD_BUILD_SUCCESS models="+worldRoot.GetComponentsInChildren<MeshRenderer>().Length+" imported-materials="+materials.Count);
            OpenWorldAtmosphereUpgrade.Apply();
        }
        static void Ground()
        {
            const int n=150;const float half=75;var vertices=new Vector3[(n+1)*(n+1)];var uv=new Vector2[vertices.Length];var faces=new[]{new List<int>(),new List<int>(),new List<int>()};
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++){int i=z*(n+1)+x;vertices[i]=Surface(x-half,z-half);uv[i]=new Vector2(x/8f,z/8f);}
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                int a=z*(n+1)+x,b=a+1,c=a+n+1,d=c+1;var p=new Vector2(x-half+.5f,z-half+.5f);
                int material=Vector2.Distance(p,new Vector2(-30,22))<16&&Height(p.x,p.y)<.65f?2:0;
                faces[material].AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh=new Mesh{name="Amberwood continuous walkable terrain",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.subMeshCount=3;
            for(int i=0;i<3;i++)mesh.SetTriangles(faces[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Generated+"/AmberwoodTerrain.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
            var ground=new GameObject("Walkable hills and connected trails",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));ground.transform.SetParent(worldRoot);
            ground.GetComponent<MeshFilter>().sharedMesh=mesh;ground.GetComponent<MeshCollider>().sharedMesh=mesh;
            ground.GetComponent<MeshRenderer>().sharedMaterials=new[]{Paint("Meadow",new Color(.34f,.47f,.31f)),Paint("Trail",new Color(.66f,.59f,.44f)),Paint("Shore",new Color(.51f,.48f,.37f))};
            var data=ground.AddComponent<PixelArtObject>();data.objectId=id++;data.interior=0;data.silhouette=.6f;data.Apply();
            Trails();
        }
        static void Trails()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            var segments=new List<Vector2[]>();for(int i=1;i<destinations.Length;i++)segments.Add(new[]{Vector2.zero,destinations[i]});
            segments.Add(new[]{destinations[2]+new Vector2(0,-17),destinations[3]});segments.Add(new[]{destinations[1],destinations[4]});
            foreach(var segment in segments)
            {
                var delta=segment[1]-segment[0];var right=new Vector2(-delta.y,delta.x).normalized*1.6f;
                int steps=Mathf.CeilToInt(delta.magnitude/1.5f),start=vertices.Count;
                for(int i=0;i<=steps;i++)
                {
                    var center=Vector2.Lerp(segment[0],segment[1],(float)i/steps);
                    foreach(float side in new[]{-1f,1f}){var p=center+right*side;vertices.Add(Surface(p.x,p.y)+Vector3.up*.035f);}
                    if(i<steps){int a=start+i*2;triangles.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
                }
            }
            // A round clearing makes the junction read as a camp rather than a star of tile edges.
            int hub=vertices.Count;vertices.Add(Surface(0,0)+Vector3.up*.045f);
            for(int i=0;i<=32;i++){float a=i*Mathf.PI*2/32;vertices.Add(Surface(Mathf.Cos(a)*4,Mathf.Sin(a)*4)+Vector3.up*.045f);if(i<32)triangles.AddRange(new[]{hub,hub+i+2,hub+i+1});}
            var mesh=new Mesh{name="Continuous trail ribbons"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            string path=Generated+"/TrailRibbons.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
            var g=new GameObject("Connected dirt trails",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(worldRoot);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=Paint("Trail",new Color(.66f,.59f,.44f));
            var ink=g.AddComponent<PixelArtObject>();ink.objectId=id++;ink.interior=0;ink.silhouette=0;ink.Apply();
        }
        static void Boundary(Vector3 p,Vector3 size){var g=new GameObject("Map boundary");g.transform.SetParent(worldRoot);g.transform.position=p;g.AddComponent<BoxCollider>().size=size;}
        static void Camp()
        {
            Asset("KenneyNature","tent_detailedOpen",Surface(-5,4),5.5f,160);
            Asset("KenneyNature","tent_smallOpen",Surface(5,5),4.3f,210);
            Asset("KenneyPlatformer","crate",Surface(-4,1),1.1f,15);
            Asset("KenneyPlatformer","barrel",Surface(6,2),1,0);
            Asset("KenneyNature","bed_floor",Surface(-4,5),1.7f,40,false);
            Asset("KenneyNature","campfire_stones",Surface(0,2),1.6f,0,false);
            Asset("KenneyNature","campfire_logs",Surface(0,2)+Vector3.up*.08f,1.1f,0,false);
            Fire(Surface(0,2)+Vector3.up*.35f);
            Asset("KenneyNature","tree_oak",Surface(-8,7),7,15,true,true,true);
            Asset("KenneyNature","tree_default_fall",Surface(8,7),6.5f,200,true,true,true);
            for(int i=0;i<25;i++)
            {
                float a=i*Mathf.PI*2/25,x=Mathf.Cos(a)*R(5,10),z=Mathf.Sin(a)*R(5,10);
                if(TrailDistance(new Vector2(x,z))<2.3f)continue;
                Asset("KenneyNature",i%3==0?"plant_bush":"flower_purpleA",Surface(x,z),i%3==0?1.2f:.8f,R(0,360),false);
            }
        }
        static void Ruins()
        {
            for(int i=-2;i<=2;i++)if(i!=0)Asset("KenneyCastle","wall-half",Surface(34+i*3.5f,30),3.5f,0);
            Asset("KenneyCastle","wall-doorway",Surface(34,30),3.5f,0);
            Asset("KenneyCastle","tower-square",Surface(26,29),7,0);
            Asset("KenneyCastle","tower-square",Surface(42,29),7,0);
            Asset("KenneyCastle","wall-half",Surface(26,23),3.5f,90);
            Asset("KenneyCastle","wall-half",Surface(42,23),3.5f,90);
            Asset("KenneyCastle","flag",Surface(27,25),3,0,false);
            for(int i=0;i<7;i++)Asset("KenneyCastle","rocks-large",Surface(R(26,42),R(32,36)),R(1.2f,2.6f),R(0,360));
            Fire(Surface(31,22)+Vector3.up*.5f);Fire(Surface(37,22)+Vector3.up*.5f);
        }
        static void Lookout()
        {
            Asset("KenneyNature","tree_default_fall",Surface(44,-34),7,70,true,true,true);
            Asset("KenneyNature","rock_largeB",Surface(43,-28),3.7f,20);
            Asset("KenneyNature","fence_simpleLow",Surface(39,-35),4,0);
            Asset("KenneyNature","fence_simpleLow",Surface(43,-35),4,0);
            Asset("KenneyNature","tent_smallOpen",Surface(-35,-27),3.5f,20);
            Fire(Surface(-30,-32)+Vector3.up*.5f);
        }
        static void Fire(Vector3 p)
        {
            var m=Paint("Flame",new Color(1,.55f,.18f),null,true);Primitive("Fire",p+Vector3.up*.3f,new Vector3(.28f,.65f,.28f),m,PrimitiveType.Capsule,false);
            var light=new GameObject("Warm camp light").AddComponent<Light>();light.transform.SetParent(worldRoot);light.transform.position=p+Vector3.up*.65f;light.type=LightType.Point;light.color=new Color(1,.52f,.23f);light.intensity=2.3f;light.range=5.5f;light.renderMode=LightRenderMode.ForcePixel;light.shadows=LightShadows.None;
        }
        static Transform Waystone(Vector2 point)
        {
            var p=Surface(point.x,point.y);Asset("KenneyNature","rock_smallA",p,1.7f,0);
            var g=Asset("KenneyPlatformer","coin-gold",p+Vector3.up*1,1.1f,0,false,true,true);g.name="Ancient waystone";return g.transform;
        }
        // KayKit ships every weapon variant on the hand slots, all enabled. Stacked, four shields
        // and three swords read as a cluster of single-texel noise at sprite scale.
        public static readonly string[] KnightWeapons={"1H_Sword","Round_Shield"};
        public static void KnightLoadout(GameObject visual)
        {
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                string name=renderer.name;if(!name.Contains("Sword")&&!name.Contains("Shield"))continue;
                bool keep=KnightWeapons.Contains(name);if(renderer.gameObject.activeSelf!=keep){renderer.gameObject.SetActive(keep);EditorUtility.SetDirty(renderer.gameObject);}
            }
        }
        // Authored normals for the knight. Its many small facets each took their own toon band,
        // which at sprite scale reads as single-texel speckle. Each part's normals lean toward
        // its bounding-ellipsoid normal (one broad light shape per part) with a little sky bias,
        // as with the study scene's face, hair and cloak profiles.
        public static void CharacterNormals(GameObject visual,float shapeWeight=.7f,float upWeight=.15f)
        {
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var filter=renderer.GetComponent<MeshFilter>();var skinned=renderer as SkinnedMeshRenderer;
                var profile=renderer.GetComponent<AuthoredNormalProfile>();
                var source=profile&&profile.geometricMesh?profile.geometricMesh:filter?filter.sharedMesh:skinned?skinned.sharedMesh:null;
                if(!source)continue;
                var normals=source.normals;var vertices=source.vertices;var bounds=source.bounds;
                var extents=Vector3.Max(bounds.extents,Vector3.one*.001f);var up=renderer.transform.InverseTransformDirection(Vector3.up).normalized;
                for(int i=0;i<normals.Length;i++)
                {
                    var d=vertices[i]-bounds.center;var shape=new Vector3(d.x/(extents.x*extents.x),d.y/(extents.y*extents.y),d.z/(extents.z*extents.z));
                    if(shape.sqrMagnitude<1e-8f)continue;shape.Normalize();
                    // Inner faces (visor, cape back) point away from the proxy; leave them mostly alone.
                    float weight=shapeWeight*Mathf.SmoothStep(0,1,(Vector3.Dot(normals[i],shape)+.2f)/.6f);
                    var n=Vector3.Slerp(normals[i],shape,weight);
                    normals[i]=Vector3.Slerp(n,up,upWeight*Mathf.SmoothStep(0,1,(Vector3.Dot(n,up)+.3f)/.9f)).normalized;
                }
                string path=Generated+"/Knight-"+source.name+"-AuthoredNormals.asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh){mesh=UnityEngine.Object.Instantiate(source);mesh.name=source.name+" — broad painted lighting";mesh.normals=normals;AssetDatabase.CreateAsset(mesh,path);}
                else{mesh.normals=normals;EditorUtility.SetDirty(mesh);}
                if(!profile)profile=renderer.gameObject.AddComponent<AuthoredNormalProfile>();
                profile.geometricMesh=source;profile.authoredMesh=mesh;profile.useAuthored=true;profile.Apply();EditorUtility.SetDirty(profile);
            }
        }
        static ExplorerMotor Player()
        {
            var g=new GameObject("Player — KayKit Knight");g.transform.position=Surface(0,-2)+Vector3.up*.2f;
            var controller=g.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.34f;controller.center=Vector3.up*.9f;controller.stepOffset=.32f;controller.slopeLimit=48;controller.skinWidth=.04f;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KayKitAdventurers/Knight.fbx");
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,g.transform);visual.name="Animated character";visual.transform.localPosition=Vector3.zero;
            var idle=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(source)).OfType<AnimationClip>().First(c=>c.name=="Idle");idle.SampleAnimation(visual,0);
            var b=BoundsOf(visual);visual.transform.localScale*=1.8f/b.size.y;b=BoundsOf(visual);visual.transform.position+=Vector3.up*(g.transform.position.y-b.min.y);
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(Palette).ToArray();
            KnightLoadout(visual);CharacterNormals(visual);
            var animator=visual.GetComponent<Animator>();if(!animator)animator=visual.AddComponent<Animator>();animator.runtimeAnimatorController=Controller();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var player=g.AddComponent<ExplorerMotor>();player.visual=visual.transform;player.animator=animator;player.spawnPoint=g.transform.position;
            var data=g.AddComponent<PixelArtObject>();data.objectId=id++;data.interior=.05f;data.Apply();return player;
        }
        static AnimatorController Controller()
        {
            string path=Generated+"/KnightExplorer.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller)return controller;
            controller=AnimatorController.CreateAnimatorControllerAtPath(path);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);controller.AddParameter("VerticalSpeed",AnimatorControllerParameterType.Float);
            var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/KayKitAdventurers/Knight.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToDictionary(c=>c.name);
            var machine=controller.layers[0].stateMachine;var locomotion=machine.AddState("Locomotion");machine.defaultState=locomotion;
            var tree=new BlendTree{name="Idle Walk Sprint",blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);
            tree.AddChild(clips["Idle"],0);tree.AddChild(clips["Walking_A"],4.2f);tree.AddChild(clips["Running_A"],7.2f);locomotion.motion=tree;
            var jump=machine.AddState("Jump");jump.motion=clips["Jump_Start"];var fall=machine.AddState("Fall");fall.motion=clips["Jump_Idle"];
            Transition(locomotion,jump,false,.1f);Transition(locomotion,fall,false,-.1f);
            var apex=jump.AddTransition(fall);apex.hasExitTime=false;apex.duration=.08f;apex.AddCondition(AnimatorConditionMode.Less,.1f,"VerticalSpeed");
            foreach(var state in new[]{jump,fall}){var land=state.AddTransition(locomotion);land.hasExitTime=false;land.duration=.1f;land.AddCondition(AnimatorConditionMode.If,0,"Grounded");}
            return controller;
        }
        static void Transition(AnimatorState from,AnimatorState to,bool grounded,float vertical)
        {
            var t=from.AddTransition(to);t.hasExitTime=false;t.duration=.08f;t.AddCondition(AnimatorConditionMode.IfNot,0,"Grounded");t.AddCondition(vertical>0?AnimatorConditionMode.Greater:AnimatorConditionMode.Less,Mathf.Abs(vertical),"VerticalSpeed");
        }
        public static void Capture(Camera camera,string path)
        {
            var old=camera.targetTexture;var rt=new RenderTexture(640,360,24){filterMode=FilterMode.Point};camera.targetTexture=rt;LabCameraRender.Render(camera);var prior=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(640,360,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,360),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());camera.targetTexture=old;RenderTexture.active=prior;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
