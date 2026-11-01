using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab.Editor
{
    public static class SpriteWorldStudyBuilder
    {
        const string Art="Assets/Art/SpriteWorldStudy";
        const string Scene="Assets/Scenes/3D-Sprite-Rendering-Study.unity";
        static Transform root;
        static System.Random random;
        static int nextId;
        static Material ground,edge,stone,stoneLight,stoneDark,wood,bark,leaves,leafLight,leafDark,grass,water,foam,roof,metal,glow;
        static float R(float a,float b)=>a+(float)random.NextDouble()*(b-a);
        static Transform BeginObject(string name,float interior=.18f,float silhouette=1)
        {
            var previous=root;var g=new GameObject(name);g.transform.SetParent(root);root=g.transform;
            var data=g.AddComponent<PixelArtObject>();data.objectId=nextId++;data.interior=interior;data.silhouette=silhouette;
            return previous;
        }
        static void EndObject(Transform previous){root.GetComponent<PixelArtObject>().Apply();root=previous;}
        static void AuthoredNormals(GameObject obj,string profile,float upWeight)
        {
            var filter=obj.GetComponent<MeshFilter>();if(!filter)return;
            string path=Art+"/"+profile+"-AuthoredNormals.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name=profile+" — broad painted lighting";
                var normals=mesh.normals;
                for(int i=0;i<normals.Length;i++)
                {
                    float weight=upWeight*Mathf.SmoothStep(0,1,(normals[i].y+.3f)/.9f);
                    normals[i]=Vector3.Slerp(normals[i],Vector3.up,weight).normalized;
                }
                mesh.normals=normals;AssetDatabase.CreateAsset(mesh,path);
            }
            var normalProfile=obj.AddComponent<AuthoredNormalProfile>();normalProfile.geometricMesh=filter.sharedMesh;normalProfile.authoredMesh=mesh;
            filter.sharedMesh=mesh;
        }
        static Material Paint(string name,string hex,string shade="#74748E",bool emission=false)
        {
            ColorUtility.TryParseHtmlString(hex,out var color);ColorUtility.TryParseHtmlString(shade,out var shadow);
            string path=Art+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("PixelArtLab/Sprite Toon"));AssetDatabase.CreateAsset(m,path);}
            m.shader=Shader.Find("PixelArtLab/Sprite Toon");m.SetColor("_Color",color);m.SetColor("_ShadeColor",shadow);
            if(emission)m.SetColor("_EmissionColor",color*.7f);
            return m;
        }
        static GameObject Shape(string name,Vector3 p,Vector3 s,Material m,PrimitiveType type=PrimitiveType.Cube)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=s;
            g.GetComponent<Renderer>().sharedMaterial=m;
            UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }
        static GameObject MeshObject(string name,Vector3[] vertices,int[] triangles,Material m)
        {
            var mesh=new Mesh {name=name};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();
            string path=Art+"/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            else AssetDatabase.CreateAsset(mesh,path);
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root);
            g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=m;return g;
        }
        static GameObject Rock(Vector3 p,Vector3 s,Material m)
        {
            var previous=BeginObject("Stone silhouette",.14f,.8f);
            // Broad bevels carry coherent highlights; unlike boxes or white noise textures.
            Vector3[] v={new Vector3(-.5f,0,-.35f),new Vector3(.35f,0,-.5f),new Vector3(.5f,0,.3f),new Vector3(-.3f,0,.5f),
                new Vector3(-.32f,.55f,-.21f),new Vector3(.24f,.66f,-.3f),new Vector3(.31f,.47f,.23f),new Vector3(-.25f,.58f,.3f)};
            // Duplicate vertices per face for purposeful flat normals.
            int[] t={0,4,5,0,5,1,1,5,6,1,6,2,2,6,7,2,7,3,3,7,4,3,4,0,4,7,6,4,6,5};
            var mesh=new Mesh();var verts=new Vector3[t.Length];var ids=new int[t.Length];
            for(int i=0;i<t.Length;i++){verts[i]=v[t[i]];ids[i]=i;}mesh.vertices=verts;mesh.triangles=ids;mesh.RecalculateNormals();
            // One shared rock mesh, varied instances.
            string path=Art+"/BeveledRock.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!saved){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else UnityEngine.Object.DestroyImmediate(mesh);
            var g=new GameObject("Beveled stone",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=s;
            g.transform.rotation=Quaternion.Euler(0,R(0,360),0);g.GetComponent<MeshFilter>().sharedMesh=saved;g.GetComponent<MeshRenderer>().sharedMaterial=m;EndObject(previous);return g;
        }
        [MenuItem("Pixel Art Lab/Build Research-Based Sprite World")]
        public static void Build()
        {
            Directory.CreateDirectory(Art);Directory.CreateDirectory("Previews");random=new System.Random(513);nextId=10;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            root=new GameObject("Sprite World Study — shaped color and linework").transform;
            var worldData=root.gameObject.AddComponent<PixelArtObject>();worldData.objectId=1;worldData.interior=0;worldData.silhouette=.7f;
            ground=Paint("Meadow","#507760","#75718E");edge=Paint("Earth","#79615C","#665B7A");
            stone=Paint("Warm limestone","#CFC5B1");stoneLight=Paint("Stone highlights","#E6DBC4");stoneDark=Paint("Stone joints","#766979");
            wood=Paint("Timber","#936D55");bark=Paint("Tree bark","#71555C");leaves=Paint("Leaf body","#BA8155");
            leafLight=Paint("Leaf clusters light","#DBA260");leafDark=Paint("Leaf clusters shade","#986652");
            grass=Paint("Grass blades","#80956A");water=Paint("Still turquoise water","#457C86","#656F8E");foam=Paint("Water highlights","#A9C9BF");
            roof=Paint("Muted terracotta","#9D5660");metal=Paint("Blue iron","#526478");glow=Paint("Fire gold","#FFCD66","#A5766A",true);
            // Large connected terraces. No per-tile cracks, repeated checkerboard seams or random texel noise.
            Shape("Diorama foundation",new Vector3(0,-.9f,0),new Vector3(18,1.6f,14),edge);
            Shape("Meadow",new Vector3(0,-.05f,0),new Vector3(18,.25f,14),ground);
            Shape("Raised garden",new Vector3(4,.33f,3.2f),new Vector3(7,.75f,6.4f),edge);
            Shape("Garden lawn",new Vector3(4,.74f,3.2f),new Vector3(7,.13f,6.4f),ground);
            Shape("Pond",new Vector3(-5.5f,.095f,-3.6f),new Vector3(5.8f,.08f,4),water);
            for(int i=0;i<22;i++)
            {
                float a=i*Mathf.PI*2/22;var p=new Vector3(-5.5f+Mathf.Cos(a)*3.1f,.12f,-3.6f+Mathf.Sin(a)*2.2f);
                Rock(p,new Vector3(R(.6f,1),R(.45f,.7f),R(.5f,.85f)),i%3==0?stoneLight:stone);
            }
            for(int i=0;i<26;i++)Shape("Quiet ripple",new Vector3(R(-7.7f,-3.3f),.145f,R(-5.1f,-2.3f)),new Vector3(R(.15f,.6f),.012f,.035f),foam);
            // A broad pale path forms readable shapes between grass masses.
            var pathParent=BeginObject("Connected flagstone path",.04f,.35f);
            for(int z=-6;z<=5;z++)for(int x=-1;x<=1;x++)
            {
                var g=Shape("Worn path slab",new Vector3(x*.77f+.15f*Mathf.Sin(z),.13f,z),new Vector3(.73f,.10f,.95f),z%4==0?stoneLight:stone);
                g.transform.rotation=Quaternion.Euler(0,R(-4,4),0);
            }
            for(int x=2;x<=6;x++)Shape("Side path",new Vector3(x,.16f,-1.1f),new Vector3(.92f,.12f,1.6f),stone);
            for(int i=0;i<4;i++)Shape("Garden steps",new Vector3(2.3f,.16f+i*.16f,.1f+i*.32f),new Vector3(2.4f,.26f,.38f),stoneLight);
            EndObject(pathParent);
            Masonry(new Vector3(1,.65f,0),6.5f);
            Pavilion(new Vector3(3.9f,.8f,3.1f));
            Tree(new Vector3(-5.5f,.1f,3.1f),1.25f);
            Tree(new Vector3(7,.8f,5.2f),.65f);
            Tree(new Vector3(6,.1f,-4.5f),.8f);
            BareTree(new Vector3(-7,.1f,-.3f),.8f);
            BareTree(new Vector3(7.4f,.8f,1.6f),.65f);
            Shrub(new Vector3(-7.6f,.12f,-5.6f),1.1f);
            Shrub(new Vector3(-3.6f,.12f,4.8f),1.25f);
            Shrub(new Vector3(6.3f,.12f,-3.4f),.9f);
            Shrub(new Vector3(7.7f,.85f,3.6f),.8f);
            Flowers(new Vector3(-2.5f,.12f,.7f));Flowers(new Vector3(4.8f,.12f,-2.2f));
            for(int i=0;i<100;i++)
            {
                float x=R(-8.5f,8.5f),z=R(-6.5f,6.5f);
                if(Mathf.Abs(x)<1.55f || (x<-2.4f&&z<-1.2f) || (x>1&&z>0) || (x>0&&z>-2&&z<0))continue;
                float y=.1f;Grass(new Vector3(x,y,z));
            }
            for(int i=0;i<14;i++)Rock(new Vector3(R(-8,-2),.12f,R(.1f,6)),new Vector3(R(.4f,1.1f),R(.4f,.8f),R(.4f,.9f)),stone);
            for(int i=0;i<8;i++)
            {
                float x=R(2.5f,8),z=R(-6,-2.5f);Grass(new Vector3(x,.1f,z));
                Shape("Flower stalk",new Vector3(x,.32f,z),new Vector3(.04f,.45f,.04f),grass);
                Shape("Flower head",new Vector3(x,.57f,z),new Vector3(.17f,.12f,.17f),stoneLight,PrimitiveType.Sphere);
            }
            Brazier(new Vector3(1.8f,.1f,-2.9f));Brazier(new Vector3(-1.7f,.1f,2));
            Explorer(new Vector3(.15f,.2f,-1.8f));Explorer(new Vector3(2.6f,.25f,-1.2f),true);
            var cam=new GameObject("Pixel-Snapped Orthographic Camera").AddComponent<Camera>();
            cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=6.6667f;cam.nearClipPlane=.1f;cam.farClipPlane=90;
            cam.transform.rotation=Quaternion.Euler(35,42,0);cam.transform.position=new Vector3(.4f,1.6f,-.2f)-cam.transform.forward*30;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.17f,.19f,.25f);cam.allowMSAA=false;cam.allowHDR=false;
            cam.gameObject.AddComponent<AudioListener>();
            var lines=cam.gameObject.AddComponent<PixelLinework>();lines.lineShader=Shader.Find("Hidden/PixelArtLab/Linework");
            lines.identityShader=Shader.Find("Hidden/PixelArtLab/Object Identity");lines.normalThreshold=.44f;lines.strength=.88f;
            var lab=new GameObject("Rendering Study Controls").AddComponent<SpriteWorldControls>();lab.worldCamera=cam;lab.linework=lines;
            var sun=new GameObject("Warm key / cool painted shade").AddComponent<Light>();sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.color=new Color(1,.94f,.84f);sun.intensity=1;
            sun.shadows=LightShadows.Hard;sun.shadowBias=.035f;sun.shadowNormalBias=.18f;
            RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.43f,.52f);
            RenderSettings.fog=false;QualitySettings.antiAliasing=0;QualitySettings.shadowDistance=60;QualitySettings.shadowCascades=2;
            QualitySettings.shadowResolution=ShadowResolution.VeryHigh;QualitySettings.shadows=ShadowQuality.All;
            PlayerSettings.colorSpace=ColorSpace.Linear;Shader.SetGlobalFloat("_LabContinuous",0);
            worldData.Apply();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true),new EditorBuildSettingsScene("Assets/Scenes/3D-Pixel-Art-Lighting-Study.unity",true)};
            AssetDatabase.SaveAssets();
            Capture(cam,"Previews/05-Art-Directed-Sprite-World.png");
            lines.debugIdentities=true;Capture(cam,"Previews/06-Object-Identity-Debug.png");lines.debugIdentities=false;
            cam.orthographicSize=4.5f;cam.transform.position=new Vector3(.6f,1.3f,-1.5f)-cam.transform.forward*30;
            Capture(cam,"Previews/07-Character-Lighting-Closeup.png");
            foreach(var profile in UnityEngine.Object.FindObjectsByType<AuthoredNormalProfile>(FindObjectsSortMode.None)){profile.useAuthored=false;profile.Apply();}
            Capture(cam,"Previews/08-Default-Normals-Comparison.png");
            foreach(var profile in UnityEngine.Object.FindObjectsByType<AuthoredNormalProfile>(FindObjectsSortMode.None)){profile.useAuthored=true;profile.Apply();}
            cam.orthographicSize=6.6667f;cam.transform.position=new Vector3(.4f,1.6f,-.2f)-cam.transform.forward*30;
            Debug.Log("SPRITE_WORLD_BUILD_SUCCESS");
        }
        static void Pavilion(Vector3 p)
        {
            var previous=BeginObject("Sanctuary — one outline group",.26f);
            Shape("Pavilion plinth",p,new Vector3(4.6f,.35f,3.9f),stoneDark);
            Shape("Plinth cap",p+Vector3.up*.22f,new Vector3(4.7f,.18f,4),stone);
            foreach(float x in new[]{-1.7f,1.7f})foreach(float z in new[]{-1.35f,1.35f})
            {
                Shape("Column base",p+new Vector3(x,.45f,z),new Vector3(.75f,.32f,.75f),stoneLight);
                Shape("Column shaft",p+new Vector3(x,1.7f,z),new Vector3(.44f,2.3f,.44f),stone);
                Shape("Column capital",p+new Vector3(x,2.9f,z),new Vector3(.75f,.25f,.75f),stoneLight);
            }
            Shape("Lintel",p+Vector3.up*3.13f,new Vector3(4.4f,.38f,3.8f),wood);
            // A pitched roof with actual slopes, eaves and deliberate tile strips.
            foreach(int side in new[]{-1,1})
            {
                var roofPart=Shape("Pitched roof",p+new Vector3(side*1.24f,3.72f,0),new Vector3(2.85f,.18f,4.6f),roof);
                roofPart.transform.rotation=Quaternion.Euler(0,0,side*-24);
                for(int row=0;row<5;row++)
                {
                    float x=side*(.25f+row*.51f),y=4.28f-row*.225f;
                    Shape("Roof tile course",p+new Vector3(x,y,0),new Vector3(.07f,.07f,4.62f),wood);
                }
                Shape("Roof fascia",p+new Vector3(side*2.55f,3.16f,0),new Vector3(.18f,.24f,4.8f),wood);
            }
            Shape("Roof ridge",p+Vector3.up*4.33f,new Vector3(.27f,.20f,4.9f),wood);
            Shape("Sanctuary altar",p+new Vector3(0,.7f,.2f),new Vector3(1.25f,.8f,1.1f),stoneDark);
            var relic=Shape("Amber relic",p+new Vector3(0,1.5f,.2f),new Vector3(.4f,.85f,.4f),glow);
            relic.transform.rotation=Quaternion.Euler(0,45,12);
            // Deliberate stone reliefs and braces, readable in a small viewport.
            foreach(float side in new[]{-1f,1f})
            {
                Branch(p+new Vector3(side*1.7f,2.3f,-1.35f),p+new Vector3(side*.95f,3.04f,-1.35f),.07f,wood);
                Shape("Stone capital inset",p+new Vector3(side*1.7f,2.88f,-1.75f),new Vector3(.42f,.08f,.03f),stoneDark);
            }
            EndObject(previous);
        }
        static void Masonry(Vector3 p,float length)
        {
            var previous=BeginObject("Retaining wall — selected stone seams",.15f,.8f);
            for(int row=0;row<2;row++)for(int i=0;i<9;i++)
            {
                float x=p.x+(i+.5f)*length/9;
                Shape("Masonry block",new Vector3(x,p.y-.3f+row*.26f,p.z),new Vector3(length/9-.035f,.23f,.34f),i%4==0?stoneLight:stone);
            }
            EndObject(previous);
        }
        static void Branch(Vector3 a,Vector3 b,float width,Material m)
        {
            var g=Shape("Tapered branch",(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f,width),m,PrimitiveType.Cylinder);
            string path=Art+"/TaperedBranch.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                mesh=UnityEngine.Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh);mesh.name="Branch taper";
                var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++){float factor=Mathf.Lerp(1.18f,.58f,(vertices[i].y+1)*.5f);vertices[i].x*=factor;vertices[i].z*=factor;}
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            g.GetComponent<MeshFilter>().sharedMesh=mesh;
            g.transform.up=(b-a).normalized;
        }
        static void Tree(Vector3 p,float scale)
        {
            var previous=BeginObject("Tree — unified canopy outline",.025f);
            Branch(p,p+new Vector3(.1f,3.6f,0)*scale,.27f*scale,bark);
            Branch(p+Vector3.up*1.4f*scale,p+new Vector3(-1.4f,3.4f,.3f)*scale,.14f*scale,bark);
            Branch(p+Vector3.up*2*scale,p+new Vector3(1.4f,3.9f,-.3f)*scale,.16f*scale,bark);
            for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Branch(p+new Vector3(Mathf.Cos(a)*.65f,.05f,Mathf.Sin(a)*.65f)*scale,p+Vector3.up*.7f*scale,.12f*scale,bark);}
            for(int i=0;i<12;i++)
            {
                var offset=new Vector3(R(-1.3f,1.3f),R(3.4f,4.6f),R(-1,1));
                Canopy(p+offset*scale,new Vector3(R(1.3f,2.2f),R(.8f,1.4f),R(1.2f,2))*scale,i%3==0?leafLight:i%3==1?leaves:leafDark);
            }
            // Small sparse leaf marks support the large masses without becoming surface static.
            for(int i=0;i<20;i++)Shape("Fallen leaf",p+new Vector3(R(-2,2),.03f,R(-1.8f,1.8f)),new Vector3(.12f,.02f,.07f),leaves);
            EndObject(previous);
        }
        static void BareTree(Vector3 p,float s)
        {
            var previous=BeginObject("Weathered branching tree",.08f);
            Branch(p,p+new Vector3(.15f,3.4f,.1f)*s,.19f*s,bark);
            Branch(p+Vector3.up*1.3f*s,p+new Vector3(-1.3f,2.55f,.15f)*s,.105f*s,bark);
            Branch(p+new Vector3(-.9f,2.2f,.12f)*s,p+new Vector3(-1.0f,3.1f,.2f)*s,.065f*s,bark);
            Branch(p+Vector3.up*2.1f*s,p+new Vector3(1.1f,3.15f,-.2f)*s,.1f*s,bark);
            Branch(p+new Vector3(.75f,2.8f,-.15f)*s,p+new Vector3(1.6f,2.9f,-.25f)*s,.055f*s,bark);
            Branch(p+new Vector3(.1f,2.8f,.1f)*s,p+new Vector3(-.5f,3.7f,.15f)*s,.075f*s,bark);
            EndObject(previous);
        }
        static void Canopy(Vector3 position,Vector3 scale,Material material)
        {
            const string path=Art+"/PaintedCanopy-ArtDirected.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh)
            {
                const int count=16;var v=new Vector3[count*3+2];var tris=new System.Collections.Generic.List<int>();
                for(int ring=0;ring<3;ring++)for(int i=0;i<count;i++)
                {
                    float angle=i*Mathf.PI*2/count;
                    float radius=(ring==1?.55f:ring==2?.40f:.32f)*(i%2==0?1:.91f);
                    v[ring*count+i]=new Vector3(Mathf.Cos(angle)*radius,(ring-1)*.22f+(i%3)*.02f,Mathf.Sin(angle)*radius);
                }
                v[count*3]=new Vector3(0,-.34f,0);v[count*3+1]=new Vector3(0,.32f,0);
                for(int i=0;i<count;i++)
                {
                    int n=(i+1)%count;
                    tris.AddRange(new[]{count*3,i,n,count*3+1,count*2+n,count*2+i});
                    for(int ring=0;ring<2;ring++){int a=ring*count+i,b=ring*count+n,c=(ring+1)*count+i,d=(ring+1)*count+n;tris.AddRange(new[]{a,c,d,a,d,b});}
                }
                mesh=new Mesh{name="Sculpted leaf canopy — broad upper normals"};mesh.vertices=v;mesh.triangles=tris.ToArray();
                var normals=new Vector3[v.Length];
                for(int i=0;i<v.Length;i++)
                {
                    var radial=new Vector3(v[i].x,v[i].y*1.5f,v[i].z).normalized;
                    normals[i]=Vector3.Slerp(radial,Vector3.up,v[i].y>-.1f?.72f:.12f).normalized;
                }
                mesh.normals=normals;
                var uv=new Vector2[v.Length];for(int i=0;i<v.Length;i++)uv[i]=new Vector2(v[i].x+.5f,v[i].z+.5f);
                mesh.uv=uv;AssetDatabase.CreateAsset(mesh,path);
            }
            ApplyLeafPaint(material);
            var g=new GameObject("Lobed foliage silhouette",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root);g.transform.position=position;g.transform.localScale=scale;
            g.transform.rotation=Quaternion.Euler(R(-12,12),R(0,360),R(-9,9));g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;
            const string geometricPath=Art+"/PaintedCanopy-GeometricNormals.asset";
            var geometric=AssetDatabase.LoadAssetAtPath<Mesh>(geometricPath);
            if(!geometric){geometric=UnityEngine.Object.Instantiate(mesh);geometric.name="Canopy default geometric normals";geometric.RecalculateNormals();AssetDatabase.CreateAsset(geometric,geometricPath);}
            var profile=g.AddComponent<AuthoredNormalProfile>();profile.geometricMesh=geometric;profile.authoredMesh=mesh;
            profile.Apply();
        }
        static void ApplyLeafPaint(Material material)
        {
            const string path=Art+"/LeafClusterPaint.png";
            var paint=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!paint)
            {
                // A few deliberately placed leaf clusters, not per-texel random noise.
                var texture=new Texture2D(32,32,TextureFormat.RGB24,false);
                var centers=new[]{new Vector2(9,21),new Vector2(20,22),new Vector2(23,11),new Vector2(10,9)};
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {
                    float value=.91f;
                    for(int i=0;i<centers.Length;i++)
                    {
                        float dx=(Mathf.Floor(x/2f)*2-centers[i].x)/7f,dy=(Mathf.Floor(y/2f)*2-centers[i].y)/4.5f;
                        if(dx*dx+dy*dy<1)value=i<2?1:.81f;
                    }
                    texture.SetPixel(x,y,new Color(value,value,value));
                }
                texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();paint=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            material.mainTexture=paint;
        }
        static void Shrub(Vector3 p,float scale)
        {
            var previous=BeginObject("Shrub — grouped painted normals",0,.8f);
            var green=Paint("Shrub green","#63835B","#72788F");var lightGreen=Paint("Shrub tips","#92A16B","#73778D");
            for(int i=0;i<6;i++)Canopy(p+new Vector3(R(-.55f,.55f),R(.2f,.45f),R(-.45f,.45f))*scale,new Vector3(.8f,.65f,.75f)*scale,i%3==0?lightGreen:green);
            EndObject(previous);
        }
        static void Flowers(Vector3 p)
        {
            var previous=BeginObject("Flower patch",0,.4f);
            var petal=Paint("Warm pink petals","#DBA1A0");
            for(int i=0;i<7;i++)
            {
                var pos=p+new Vector3(R(-.6f,.6f),R(.17f,.3f),R(-.5f,.5f));
                Shape("Flower stem",pos-Vector3.up*.08f,new Vector3(.03f,.26f,.03f),grass);
                Shape("Petals",pos+Vector3.up*.06f,new Vector3(.19f,.08f,.19f),i%3==0?stoneLight:petal,PrimitiveType.Sphere);
            }
            EndObject(previous);
        }
        static void Grass(Vector3 p)
        {
            for(int i=0;i<4;i++)
            {
                var g=Shape("Grouped grass",p+new Vector3(i*.075f,.16f,0),new Vector3(.035f,R(.2f,.38f),.035f),grass);
                g.transform.rotation=Quaternion.Euler(0,R(0,180),R(-25,25));
            }
        }
        static void Brazier(Vector3 p)
        {
            var previous=BeginObject("Fire pedestal",.12f);
            Shape("Brazier foot",p+Vector3.up*.2f,new Vector3(.5f,.35f,.5f),stoneDark);
            Shape("Brazier pillar",p+Vector3.up*.7f,new Vector3(.3f,1,.3f),stone);
            Shape("Iron bowl",p+Vector3.up*1.25f,new Vector3(.75f,.25f,.75f),metal,PrimitiveType.Sphere);
            var orange=Paint("Fire orange","#F38944","#A5766A",true);
            Shape("Outer flame",p+Vector3.up*1.55f,new Vector3(.35f,.63f,.3f),orange,PrimitiveType.Capsule);
            var flame=Shape("Fire heart",p+new Vector3(-.035f,1.58f,-.08f),new Vector3(.19f,.43f,.18f),glow,PrimitiveType.Capsule);
            var light=new GameObject("Banded warm point light").AddComponent<Light>();light.transform.SetParent(root);light.transform.position=p+Vector3.up*1.65f;
            light.type=LightType.Point;light.color=new Color(1,.57f,.23f);light.intensity=1.5f;light.range=3.8f;light.shadows=LightShadows.Hard;
            EndObject(previous);
        }
        static void Explorer(Vector3 p,bool companion=false)
        {
            var previous=BeginObject(companion?"Ochre explorer — one silhouette":"Teal explorer — one silhouette",.07f);
            var cloak=Paint(companion?"Ochre cloak":"Teal cloak",companion?"#BE8C55":"#54858B","#696681");
            var skin=Paint("Skin","#E2B18A","#9A798A");var hair=Paint("Silver hair","#D3D1BD");var boot=Paint("Boot leather","#5C4A53");
            foreach(float x in new[]{-.17f,.17f})
            {
                Shape("Boot",p+new Vector3(x,.19f,-.035f),new Vector3(.24f,.38f,.32f),boot);
                Shape("Leg",p+new Vector3(x,.53f,0),new Vector3(.22f,.45f,.24f),metal);
            }
            var body=Shape("Cloak silhouette",p+new Vector3(0,.94f,.05f),new Vector3(.83f,.96f,.54f),cloak,PrimitiveType.Capsule);AuthoredNormals(body,"Cloak",.25f);
            var cape=Shape("Flared cape",p+new Vector3(0,.94f,.26f),new Vector3(.96f,.82f,.30f),cloak,PrimitiveType.Sphere);AuthoredNormals(cape,"Cape",.5f);
            Shape("Belt",p+new Vector3(0,.79f,-.02f),new Vector3(.65f,.13f,.55f),wood);
            var face=Shape("Face",p+new Vector3(0,1.64f,-.03f),new Vector3(.49f,.53f,.45f),skin,PrimitiveType.Sphere);AuthoredNormals(face,"Face",.32f);
            var hairMass=Shape("Hair silhouette",p+new Vector3(0,1.82f,.035f),new Vector3(.56f,.35f,.49f),hair,PrimitiveType.Sphere);AuthoredNormals(hairMass,"Hair",.55f);
            foreach(float side in new[]{-1f,1f})
            {
                Shape("Eye",p+new Vector3(side*.115f,1.67f,-.238f),new Vector3(.065f,.07f,.025f),boot);
                var fringe=Shape("Swept hair lock",p+new Vector3(side*.20f,1.78f,-.18f),new Vector3(.12f,.29f,.13f),hair,PrimitiveType.Capsule);
                fringe.transform.rotation=Quaternion.Euler(0,0,side*23);
            }
            Shape("Nose",p+new Vector3(0,1.61f,-.26f),new Vector3(.055f,.085f,.08f),skin);
            Shape("Scarf",p+new Vector3(0,1.40f,-.06f),new Vector3(.54f,.16f,.49f),stoneLight);
            Branch(p+new Vector3(-.40f,1.25f,0),p+new Vector3(-.5f,.81f,-.12f),.12f,cloak);
            Branch(p+new Vector3(.40f,1.25f,0),p+new Vector3(.5f,.81f,-.12f),.12f,cloak);
            Shape("Sword blade",p+new Vector3(.58f,.75f,.08f),new Vector3(.10f,1.1f,.07f),stoneLight);
            Shape("Sword guard",p+new Vector3(.58f,1.12f,.08f),new Vector3(.32f,.08f,.1f),metal);
            Shape("Buckle",p+new Vector3(0,.81f,-.31f),new Vector3(.14f,.12f,.04f),stoneLight);
            Shape("Shoulder armor",p+new Vector3(-.36f,1.25f,-.03f),new Vector3(.34f,.22f,.43f),metal,PrimitiveType.Sphere);
            EndObject(previous);
        }
        public static void Capture(Camera cam,string path)
        {
            var rt=new RenderTexture(640,360,24){filterMode=FilterMode.Point};cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(640,360,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,360),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
        }
    }
}
