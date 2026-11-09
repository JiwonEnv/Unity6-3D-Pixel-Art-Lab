using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class DetailedWoodlandBuilder
    {
        public const string ScenePath="Assets/Scenes/Semi-Real-Pixel-Woodland.unity";
        const string Source="Assets/ThirdParty/PolyHaven",Art="Assets/Art/DetailedWoodland";
        static int nextId=5000;
        static Transform detailRoot;
        public static void Inspect()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Source}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
                bool normal=path.Contains("nor_gl"),linear=normal||path.Contains("arm")||path.Contains("alpha")||path.Contains("rough");
                importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!linear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            foreach(string name in new[]{"island_tree_01","boulder_01","fern_02"})
            {
                string path=Source+"/"+name+"/"+name+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var renderers=model.GetComponentsInChildren<MeshRenderer>();
                Debug.Log("DETAIL_ASSET "+name+" meshes="+renderers.Length+" triangles="+model.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3));
                foreach(var r in renderers.Take(12))Debug.Log("DETAIL_PART "+r.name+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null")));
            }
            Debug.Log("DETAIL_INSPECTION_SUCCESS");
        }
        [MenuItem("Pixel Art Lab/Create Semi-Real Woodland Comparison")]
        public static void Create()
        {
            Directory.CreateDirectory(Art);nextId=5000;
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Source})){var path=AssetDatabase.GUIDToAssetPath(guid);if(path.Contains("leaves_diff")||path.Contains("fern_02_diff")){var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.SaveAndReimport();}}
            if(!File.Exists(ScenePath)&&!AssetDatabase.CopyAsset(OpenWorldSceneBuilder.ScenePath,ScenePath))throw new Exception("Cannot copy comparison scene");
            EditorSceneManager.OpenScene(ScenePath);
            var old=GameObject.Find("Poly Haven Detailed Woodland");if(old)UnityEngine.Object.DestroyImmediate(old);
            detailRoot=new GameObject("Poly Haven Detailed Woodland").transform;
            var world=GameObject.Find("Amberwood — 150m Seamless Exploration Map").transform;
            foreach(var item in world.Cast<Transform>().ToArray())
            {
                bool vegetation=item.name.StartsWith("tree_")||item.name.StartsWith("plant_")||item.name.StartsWith("grass")||item.name.StartsWith("flower_")||item.name.StartsWith("rock_");
                Vector2 point=new Vector2(item.position.x,item.position.z);
                if(vegetation&&(point.magnitude<18||Vector2.Distance(point,new Vector2(-30,22))<19))UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
            AddAsset("island_tree_01",new Vector2(-8,5),7.5f,15,0);
            AddAsset("island_tree_01",new Vector2(-36,31),8.5f,145,1);
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI*2/10;AddAsset("boulder_01",new Vector2(-30+Mathf.Cos(a)*13.5f,22+Mathf.Sin(a)*12.5f),1.8f+(i%3)*.6f,i*41,i);
            }
            AddAsset("boulder_01",new Vector2(-5,-1),2.3f,70,0);AddAsset("boulder_01",new Vector2(5,4),1.5f,130,1);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI*2/16;var point=i<8?new Vector2(Mathf.Cos(a)*7,Mathf.Sin(a)*6):new Vector2(-21+Mathf.Cos(a)*5,13+Mathf.Sin(a)*4);
                AddAsset("fern_02",point,1.15f+(i%3)*.2f,i*37,i);
            }
            var ground=GameObject.Find("Walkable hills and connected trails");var groundMesh=CloneMesh(ground.GetComponent<MeshFilter>().sharedMesh,"DetailedTerrain");ground.GetComponent<MeshFilter>().sharedMesh=groundMesh;
            var soil=SurfaceMaterial("ForestGround","forest_ground_04","forest_ground_04",false);
            soil.mainTextureScale=new Vector2(2,2);ground.GetComponent<MeshRenderer>().sharedMaterials=new[]{soil,soil,soil};
            var trail=GameObject.Find("Connected dirt trails");var trailMesh=CloneMesh(trail.GetComponent<MeshFilter>().sharedMesh,"DetailedTrails");trail.GetComponent<MeshFilter>().sharedMesh=trailMesh;
            var trailSoil=SurfaceMaterial("ForestTrail","forest_ground_04","forest_ground_04",false);trailSoil.color=new Color(1.18f,1.1f,.92f);trailSoil.mainTextureScale=new Vector2(2,2);trail.GetComponent<MeshRenderer>().sharedMaterial=trailSoil;
            var lake=GameObject.Find("Stillwater Lake");if(!lake.GetComponent<MeshFilter>())lake=world.Cast<Transform>().First(t=>t.name=="Stillwater Lake"&&t.GetComponent<MeshFilter>()).gameObject;
            lake.layer=LabLayers.Water;lake.GetComponent<MeshFilter>().sharedMesh=CloneMesh(lake.GetComponent<MeshFilter>().sharedMesh,"DetailedLake");
            var water=NewMaterial("DetailedWater");water.color=new Color(.10f,.25f,.28f);water.SetFloat("_SemiPBR",.8f);water.SetFloat("_Roughness",.18f);
            water.SetFloat("_LabWaterFlow",1);water.SetFloat("_LabWaveScale",3);water.SetFloat("_NormalScale",.7f);water.SetFloat("_ReflectionStrength",.95f);water.SetTexture("_NormalMap",WaterNormals());water.SetFloat("_WaterSurface",1);SpriteToonKeywords.Sync(water);
            lake.GetComponent<MeshRenderer>().sharedMaterial=water;
            var follow=UnityEngine.Object.FindFirstObjectByType<ExplorationCamera>();var player=UnityEngine.Object.FindFirstObjectByType<ExplorerMotor>();
            follow.pixelsPerUnit=40;follow.GetComponent<Camera>().orthographicSize=6;follow.lookOffset=Vector3.zero;
            var environment=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();environment.hour=13;environment.SetWeather(WeatherKind.Clear,true);environment.runClock=true;
            var rain=UnityEngine.Object.FindFirstObjectByType<PixelWeatherParticles>();if(rain)rain.gameObject.layer=LabLayers.Effects;
            var reflection=lake.GetComponent<DetailWaterReflection>();if(!reflection)reflection=lake.AddComponent<DetailWaterReflection>();reflection.source=follow.GetComponent<Camera>();reflection.waterMaterial=water;
            var controls=UnityEngine.Object.FindFirstObjectByType<DetailLookControls>();if(!controls)controls=new GameObject("Detailed Asset Comparison Controls").AddComponent<DetailLookControls>();controls.follow=follow;controls.player=player;controls.water=reflection;controls.style=0;controls.Apply();
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            RainPuddleUpgrade.Configure(follow.GetComponent<Camera>());
            Preview(environment,reflection);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            AssetDatabase.SaveAssets();Debug.Log("DETAILED_WOODLAND_SUCCESS assets="+detailRoot.childCount);
        }
        static Mesh CloneMesh(Mesh source,string name)
        {
            string path=Art+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=UnityEngine.Object.Instantiate(source);mesh.name=name;mesh.RecalculateTangents();AssetDatabase.CreateAsset(mesh,path);}if(mesh.uv.Length!=mesh.vertexCount){mesh.uv=mesh.vertices.Select(v=>new Vector2(v.x,v.z)*.125f).ToArray();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);}return mesh;
        }
        static Material NewMaterial(string name)
        {
            string path=Art+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("PixelArtLab/Sprite Toon"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_ShadeColor",new Color(.48f,.52f,.61f));material.SetFloat("_LocalLightStrength",1.1f);return material;
        }
        static Texture2D Texture(string asset,string pattern)
        {
            string[] paths=Directory.GetFiles(Source+"/"+asset,"*",SearchOption.AllDirectories);
            string path=paths.FirstOrDefault(p=>p.Replace('\\','/').Contains(pattern)&&!p.EndsWith(".meta")&&(p.EndsWith(".jpg")||p.EndsWith(".png")||p.EndsWith(".exr")));
            return path==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\','/'));
        }
        static Material SurfaceMaterial(string label,string asset,string prefix,bool leaves)
        {
            var material=NewMaterial(label);material.color=Color.white;
            var diffuse=Texture(asset,prefix+"_diff");var normal=Texture(asset,prefix+"_nor_gl");var arm=Texture(asset,prefix+"_arm");
            material.mainTexture=diffuse;material.SetTexture("_NormalMap",normal);material.SetTexture("_SurfaceMap",arm);material.SetFloat("_UsePackedARM",arm?1:0);
            material.SetFloat("_Roughness",1);material.SetFloat("_SemiPBR",.65f);material.SetFloat("_NormalScale",.8f);material.SetFloat("_AOStrength",.65f);
            material.SetFloat("_AlphaClip",leaves?1:0);material.SetFloat("_Cull",leaves?0:2);material.SetFloat("_LeafTwoSided",leaves?.65f:0);
            if(leaves){material.SetTexture("_AlphaMask",Texture(asset,prefix+"_alpha"));material.renderQueue=2450;material.SetFloat("_Cutoff",.28f);}
            SpriteToonKeywords.Sync(material);
            if(!diffuse)Debug.LogWarning("Missing diffuse "+prefix);return material;
        }
        static GameObject AddAsset(string asset,Vector2 point,float size,float yaw,int variant)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"/"+asset+"/"+asset+".fbx");var holder=new GameObject(asset);holder.transform.SetParent(detailRoot);holder.transform.position=new Vector3(point.x,OpenWorldSceneBuilder.Height(point.x,point.y),point.y);holder.transform.rotation=Quaternion.Euler(0,yaw,0);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,holder.transform);
            var renderers=instance.GetComponentsInChildren<MeshRenderer>();
            if(asset=="fern_02")for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.SetActive(i==variant%4);
            if(asset=="boulder_01")
            {
                var lod=instance.GetComponent<LODGroup>();if(lod)UnityEngine.Object.DestroyImmediate(lod);
                foreach(var r in renderers)r.gameObject.SetActive(r.name.EndsWith("LOD1"));
            }
            renderers=instance.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float scale=size/(asset=="island_tree_01"?bounds.size.y:Mathf.Max(bounds.size.x,bounds.size.z));instance.transform.localScale*=scale;
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);instance.transform.position+=new Vector3(holder.transform.position.x-bounds.center.x,holder.transform.position.y-bounds.min.y,holder.transform.position.z-bounds.center.z);
            foreach(var renderer in renderers)
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
                {
                    string name=m.name.ToLowerInvariant();bool leaf=name.Contains("leaves")||asset=="fern_02";
                    string prefix=asset=="island_tree_01"?(name.Contains("leaves")?asset+"_leaves":name.Contains("branches")?asset+"_branches":asset):asset;
                    return SurfaceMaterial(prefix,asset,prefix,leaf);
                }).ToArray();
                if(asset=="boulder_01")renderer.gameObject.AddComponent<MeshCollider>().sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            }
            var ink=holder.AddComponent<PixelArtObject>();ink.objectId=nextId++;ink.interior=.015f;ink.silhouette=asset=="boulder_01"?.55f:.3f;ink.Apply();
            if(asset=="island_tree_01")
            {
                var collider=holder.AddComponent<CapsuleCollider>();collider.height=size*.65f;collider.radius=.4f;collider.center=Vector3.up*collider.height*.5f;
            }
            if(asset!="boulder_01"){var motion=holder.AddComponent<WorldSurfaceMotion>();motion.kind=asset=="fern_02"?WorldMotionKind.Grass:WorldMotionKind.Tree;motion.Apply();}
            return holder;
        }
        static Texture2D WaterNormals()
        {
            const string path=Art+"/CapillaryNormals.png";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture)return texture;
            var pixels=new Texture2D(256,256,TextureFormat.RGB24,false);
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                float u=x/256f*Mathf.PI*2,v=y/256f*Mathf.PI*2;
                float dx=Mathf.Cos(u*9+Mathf.Sin(v*4))*.23f+Mathf.Cos(u*17+v*11)*.12f;
                float dy=Mathf.Cos(v*13+Mathf.Sin(u*3))*.23f+Mathf.Cos(u*17+v*11)*.08f;
                var n=new Vector3(-dx,-dy,1).normalized;pixels.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f));
            }
            pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.NormalMap;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void Preview(WorldEnvironment env,DetailWaterReflection reflection)
        {
            var camera=env.worldCamera;var oldPosition=camera.transform.position;var rotation=camera.transform.rotation;var oldSize=camera.orthographicSize;float oldAspect=camera.aspect;camera.aspect=640f/360;
            env.runClock=false;env.hour=13;env.animationTime=2;env.SetWeather(WeatherKind.Clear,true);
            camera.transform.position=new Vector3(-5,2,2)-camera.transform.forward*45;camera.orthographicSize=5.8f;
            Shader.SetGlobalFloat("_LabDetailedToon",0);reflection.Refresh();OpenWorldSceneBuilder.Capture(camera,"Previews/25-Detailed-Woodland-Hybrid.png");
            Shader.SetGlobalFloat("_LabDetailedToon",1);reflection.Refresh();OpenWorldSceneBuilder.Capture(camera,"Previews/26-Detailed-Woodland-Toon.png");
            Shader.SetGlobalFloat("_LabDetailedToon",0);camera.transform.position=new Vector3(-30,1,22)-camera.transform.forward*45;camera.orthographicSize=9;
            reflection.Refresh();OpenWorldSceneBuilder.Capture(camera,"Previews/27-Detailed-Lake.png");
            env.animationTime=3.2f;env.Apply();reflection.Refresh();OpenWorldSceneBuilder.Capture(camera,"Previews/28-Detailed-Lake-Moving.png");
            env.hour=0;env.Apply();camera.transform.position=new Vector3(-5,2,2)-camera.transform.forward*45;camera.orthographicSize=5.8f;reflection.Refresh();OpenWorldSceneBuilder.Capture(camera,"Previews/29-Detailed-Woodland-Night.png");
            env.hour=14;env.animationTime=0;env.runClock=true;env.SetWeather(WeatherKind.Clear,true);camera.transform.SetPositionAndRotation(oldPosition,rotation);camera.orthographicSize=oldSize;camera.aspect=oldAspect;reflection.waterMaterial.SetTexture("_DetailReflection",null);
        }
    }
}
