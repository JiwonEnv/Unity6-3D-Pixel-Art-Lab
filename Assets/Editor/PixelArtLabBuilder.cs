using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace PixelArtLab.Editor
{
    public static class PixelArtLabBuilder
    {
        static Transform root;
        static Material grass, earth, stone, darkStone, wood, leaf, gold, water;
        static System.Random rng;
        const string Generated = "Assets/Art/Generated";

        [MenuItem("Pixel Art Lab/Rebuild Demonstration Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Generated);
            Directory.CreateDirectory("Assets/Scenes");
            rng = new System.Random(2317);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            root = new GameObject("The Amber Crossing — Generated Environment").transform;
            grass = Mat("Moss", new Color(.33f,.42f,.28f));
            earth = Mat("Cliff", new Color(.29f,.27f,.25f));
            stone = Mat("Limestone", new Color(.48f,.48f,.40f));
            darkStone = Mat("Slate", new Color(.20f,.25f,.25f));
            wood = Mat("Weathered Timber", new Color(.26f,.17f,.11f));
            leaf = Mat("Amber Leaves", new Color(.66f,.32f,.09f));
            gold = Mat("Lantern Glow", new Color(1,.56f,.17f), true);
            water = Mat("River", new Color(.15f,.42f,.44f));
            var sand = Mat("Riverbank", new Color(.43f,.40f,.29f));
            // Terraced columns give actual depth and shadows to the pixel silhouettes.
            for (int x=-12; x<=12; x++) for (int z=-10; z<=10; z++)
            {
                float river = Mathf.Sin(z*.23f)*1.5f;
                if (Mathf.Abs(x-river)<2.0f) continue;
                if ((x*x/180f+z*z/128f)>1.16f) continue;
                float h = x < river ? 1.7f : 2.45f;
                if (x < -7 && z > 1) h += 1.2f;
                if (x > 7 && z > 3) h += .85f;
                h += Mathf.Floor(Mathf.PerlinNoise(x*.26f+40,z*.26f+20)*3)*.28f;
                Box("Stratified cliff", new Vector3(x,h/2-1.1f,z),new Vector3(.99f,h+2.2f,.99f),earth);
                Box("Moss cap",new Vector3(x,h+.01f,z),new Vector3(1,.20f,1),Mathf.Abs(x-river)<3 ? sand : grass);
                if (rng.NextDouble()<.18) Box("Stone seam",new Vector3(x,h-.65f,z),new Vector3(1.015f,.12f,1.015f),darkStone);
                if (rng.NextDouble()<.12 && Mathf.Abs(x)>3) Rock(new Vector3(x,h+.17f,z),.3f+(float)rng.NextDouble()*.4f);
                if (rng.NextDouble()<.09 && Mathf.Abs(x)>3) Tuft(new Vector3(x,h+.14f,z));
            }
            Box("River surface",new Vector3(0,.30f,0),new Vector3(7,.12f,24),water);
            var glint = Mat("Water glints",new Color(.39f,.65f,.62f),true);
            for (int i=0;i<100;i++)
            {
                float z = Range(-11,11), x = Mathf.Sin(z*.23f)*1.5f+Range(-1.65f,1.65f);
                Box("Current",new Vector3(x,.375f,z),new Vector3(Range(.12f,.6f),.015f,.035f),glint);
            }
            // A narrow timber crossing connects the two terrain heights.
            for(int i=0;i<19;i++)
            {
                float x=-3.6f+i*.4f, y=Mathf.Lerp(2.05f,2.8f,i/18f);
                Box("Bridge plank",new Vector3(x,y,-1.1f),new Vector3(.36f,.15f,1.75f),wood);
                if(i%3==0) foreach(float z in new[]{-2f,-.2f})
                {
                    Box("Bridge post",new Vector3(x,y+.4f,z),new Vector3(.13f,1,.13f),wood);
                    Box("Rail",new Vector3(x+.35f,y+.75f,z),new Vector3(.9f,.10f,.10f),wood);
                }
            }
            for(int i=0;i<7;i++) Box("Old path",new Vector3(4+i*.72f,2.92f,-1.1f+Mathf.Sin(i)*.15f),new Vector3(.63f,.10f,1.2f),stone);
            Ruin(new Vector3(6,2.95f,4));
            Tree(new Vector3(-8,3.6f,4),1.1f);
            Tree(new Vector3(-7,2.5f,-5),.95f);
            Tree(new Vector3(8,3.6f,7),1.25f);
            Tree(new Vector3(10,2.9f,-3),.85f);
            Tree(new Vector3(-5,2.3f,7),.7f);
            Lantern(new Vector3(-3.6f,2.2f,-2.4f));
            Lantern(new Vector3(4,2.9f,-2.4f));
            var hero = Character(new Vector3(4.3f,3f,-.9f));
            var cam = new GameObject("Isometric Camera").AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 15;
            cam.transform.position = new Vector3(-22,21,-25); cam.transform.LookAt(new Vector3(0,1,0));
            cam.nearClipPlane=.1f; cam.farClipPlane=150;
            cam.backgroundColor=new Color(.075f,.12f,.15f); cam.clearFlags=CameraClearFlags.SolidColor;
            cam.allowHDR=false; cam.allowMSAA=false; cam.tag="MainCamera";
            cam.gameObject.AddComponent<AudioListener>();
            hero.rotation=Quaternion.LookRotation(cam.transform.forward,Vector3.up);
            var lab = new GameObject("Pixel Art Lab Controls").AddComponent<PixelArtLab>();
            lab.worldCamera=cam; lab.hero=hero;
            var sun = new GameObject("Late afternoon sun").AddComponent<Light>();
            sun.type=LightType.Directional; sun.color=new Color(1,.79f,.52f); sun.intensity=1.35f;
            sun.transform.rotation=Quaternion.Euler(38,-32,0); sun.shadows=LightShadows.Hard;
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.36f,.46f,.52f);
            RenderSettings.ambientEquatorColor=new Color(.24f,.29f,.30f);
            RenderSettings.ambientGroundColor=new Color(.13f,.12f,.16f);
            RenderSettings.fog=true; RenderSettings.fogColor=cam.backgroundColor;
            RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=40; RenderSettings.fogEndDistance=85;
            QualitySettings.shadowDistance=70; QualitySettings.shadows=ShadowQuality.All;
            QualitySettings.shadowResolution=ShadowResolution.High; QualitySettings.antiAliasing=0;
            PlayerSettings.companyName="Pixel Art Studies";
            PlayerSettings.productName="Unity6-3D-Pixel-Art-Lab";
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            var input = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var handler=input.FindProperty("activeInputHandler"); if(handler!=null) { handler.intValue=0; input.ApplyModifiedPropertiesWithoutUndo(); }
            var scene=EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/3D-Pixel-Art-Lighting-Study.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/3D-Pixel-Art-Lighting-Study.unity",true)};
            AssetDatabase.SaveAssets();
            Preview(cam);
            Debug.Log("PIXEL_ART_LAB_BUILD_SUCCESS scene="+scene);
        }
        static float Range(float min,float max) => min+(float)rng.NextDouble()*(max-min);
        static Material Mat(string name,Color color,bool emission=false)
        {
            string path=Generated+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat) { mat=new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat,path); }
            mat.color=color; mat.SetFloat("_Glossiness",0);
            if(!emission && name!="River")
            {
                var texture=new Texture2D(16,16,TextureFormat.RGB24,false);
                texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Repeat;
                var noise=new System.Random(name.Length*179);
                for(int y=0;y<16;y++) for(int x=0;x<16;x++)
                {
                    float v=.83f+(float)noise.NextDouble()*.24f;
                    if(noise.NextDouble()<.08) v=.65f;
                    texture.SetPixel(x,y,new Color(v,v,v));
                }
                texture.Apply();
                string texPath=Generated+"/"+name+"-PixelSurface.png";
                File.WriteAllBytes(texPath,texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texPath);
                var ti=(TextureImporter)AssetImporter.GetAtPath(texPath);
                ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
                mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }
            if(emission) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor",color*.5f); }
            return mat;
        }
        static GameObject Box(string name,Vector3 pos,Vector3 size,Material mat)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);
            g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
            return g;
        }
        static void Rock(Vector3 pos,float s)
        {
            var g=Box("Weathered rock",pos,new Vector3(s,s*.6f,s*.8f),stone);
            g.transform.rotation=Quaternion.Euler(0,Range(0,90),Range(-12,12));
        }
        static void Tuft(Vector3 pos)
        {
            for(int i=0;i<3;i++) Box("Grass blade",pos+new Vector3(i*.09f,.12f,0),new Vector3(.055f,Range(.15f,.35f),.055f),grass);
        }
        static void Tree(Vector3 p,float s)
        {
            Box("Angular trunk",p+Vector3.up*1.45f*s,new Vector3(.38f,2.9f,.38f)*s,wood);
            var branch=Box("Branch",p+new Vector3(.55f,2.1f,0)*s,new Vector3(1.5f,.2f,.22f)*s,wood);
            branch.transform.rotation=Quaternion.Euler(0,0,25);
            for(int i=0;i<13;i++)
            {
                Vector3 offset=new Vector3(Range(-1.2f,1.2f),Range(2.6f,4.0f),Range(-1,1))*s;
                var g=Box("Amber canopy",p+offset,new Vector3(Range(1,1.8f),Range(.5f,1),Range(1,1.8f))*s,leaf);
                g.transform.rotation=Quaternion.Euler(0,rng.Next(0,4)*90,0);
            }
        }
        static void Ruin(Vector3 p)
        {
            Box("Sanctuary foundation",p,new Vector3(4,.3f,3),darkStone);
            foreach(float x in new[]{-1.6f,1.6f})
            {
                for(int i=0;i<7;i++) Box("Pillar block",p+new Vector3(x,.35f+i*.43f,.7f),new Vector3(.8f,.4f,.8f),stone);
                Box("Pillar capital",p+new Vector3(x,3.35f,.7f),new Vector3(1.1f,.3f,1.1f),stone);
            }
            Box("Broken arch lintel",p+new Vector3(0,3.65f,.7f),new Vector3(4.3f,.45f,1),stone);
            for(int i=0;i<5;i++) Rock(p+new Vector3(Range(-2,2),.3f,Range(-1.6f,0)),Range(.3f,.7f));
            Box("Altar",p+new Vector3(0,.6f,.6f),new Vector3(1,.8f,.9f),darkStone);
            var crystal=Box("Amber relic",p+new Vector3(0,1.4f,.6f),new Vector3(.36f,.7f,.36f),gold);
            crystal.transform.rotation=Quaternion.Euler(0,45,12);
        }
        static void Lantern(Vector3 p)
        {
            Box("Lantern post",p+Vector3.up*.8f,new Vector3(.12f,1.6f,.12f),wood);
            Box("Lantern housing",p+Vector3.up*1.65f,new Vector3(.4f,.5f,.4f),darkStone);
            Box("Warm lantern glass",p+Vector3.up*1.65f,new Vector3(.31f,.34f,.43f),gold);
            var l=new GameObject("Lantern light").AddComponent<Light>();l.transform.SetParent(root);l.transform.position=p+Vector3.up*1.7f;
            l.type=LightType.Point;l.color=new Color(1,.49f,.15f);l.range=4;l.intensity=2;
        }
        static Transform Character(Vector3 p)
        {
            // Hand-authored 16x24 sprite palette: cloak, boots, face and silver hair.
            string[] rows={"................","......hhhh......",".....hhhhhh.....",".....hffffh.....",".....hffffh.....","......ffff......",".....cccccc.....","....cccccccc....","...cccccccccc...","...ccddddddcc...","...ccddddddcc...","....cddddddc....","....cddddddc....","....cddddddc....","....cccccccc....","....cccccccc....",".....bbbbbb.....",".....bb..bb.....",".....bb..bb.....","....bbb..bbb....","................","................","................","................"};
            var tex=new Texture2D(16,24,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Point;
            for(int y=0;y<24;y++)for(int x=0;x<16;x++)
            {
                char c=rows[y][x];Color col=Color.clear;
                if(c=='h')col=new Color(.79f,.83f,.78f);if(c=='f')col=new Color(.80f,.56f,.37f);
                if(c=='c')col=new Color(.21f,.40f,.46f);if(c=='d')col=new Color(.12f,.24f,.30f);if(c=='b')col=new Color(.17f,.13f,.12f);
                tex.SetPixel(x,23-y,col);
            }
            tex.Apply();File.WriteAllBytes(Generated+"/Explorer.png",tex.EncodeToPNG());AssetDatabase.Refresh();
            var importer=(TextureImporter)AssetImporter.GetAtPath(Generated+"/Explorer.png");
            importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=12;importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            var g=new GameObject("Explorer — Pixel Billboard");g.transform.SetParent(root);g.transform.position=p+Vector3.up*.8f;
            var sr=g.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Generated+"/Explorer.png");
            return g.transform;
        }
        static void Preview(Camera cam)
        {
            Directory.CreateDirectory("Previews");
            var rt=new RenderTexture(640,360,24);rt.filterMode=FilterMode.Point;
            cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(640,360,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,360),0,0);tex.Apply();
            File.WriteAllBytes("Previews/3D-Pixel-Art-Lighting-Study.png",tex.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
        }

        static double playStarted;
        [MenuItem("Pixel Art Lab/Verify Play Mode")]
        public static void VerifyPlayMode()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/3D-Pixel-Art-Lighting-Study.unity");
            SessionState.SetBool("PixelArtLab.Verify",true);
            EditorApplication.isPlaying=true;
            playStarted=EditorApplication.timeSinceStartup;
            EditorApplication.update+=CheckPlayMode;
        }
        [InitializeOnLoadMethod]
        static void ResumeVerification()
        {
            if(!SessionState.GetBool("PixelArtLab.Verify",false)) return;
            playStarted=EditorApplication.timeSinceStartup;
            EditorApplication.update-=CheckPlayMode;
            EditorApplication.update+=CheckPlayMode;
        }
        static void CheckPlayMode()
        {
            if(EditorApplication.timeSinceStartup-playStarted<9) return;
            EditorApplication.update-=CheckPlayMode;
            SessionState.SetBool("PixelArtLab.Verify",false);
            var lab=Object.FindFirstObjectByType<PixelArtLab>();
            bool valid=Application.isPlaying && lab && lab.worldCamera && lab.worldCamera.targetTexture && lab.worldCamera.targetTexture.height==360;
            if(valid)
            {
                lab.pixelated=false;
                lab.SendMessage("Update");
                valid=lab.worldCamera.targetTexture==null;
                lab.pixelated=true;lab.SendMessage("Update");
                valid &= lab.worldCamera.targetTexture!=null;
            }
            Debug.Log(valid ? "PIXEL_ART_LAB_PLAYMODE_SUCCESS: 360p target and native/pixel toggle verified" : "PIXEL_ART_LAB_PLAYMODE_FAILED");
            EditorApplication.isPlaying=false;
            EditorApplication.Exit(valid ? 0 : 1);
        }
    }
}
