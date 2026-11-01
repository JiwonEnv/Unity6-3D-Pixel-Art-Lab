using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class OpenWorldAssetInspection
    {
        public static void Inspect()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets/ThirdParty"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(path.Contains("character")||path.Contains("Knight"))
                {
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                    importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation=true;var clips=importer.defaultClipAnimations;
                    foreach(var c in clips)c.loopTime=c.name.ToLowerInvariant().Contains("idle")||c.name.ToLowerInvariant().Contains("walk")||c.name.ToLowerInvariant().Contains("run");
                    importer.clipAnimations=clips;importer.SaveAndReimport();
                }
                if(!new[]{"Knight","character-oopi","tree_oak","rock_largeA","tent_detailedOpen","wall.fbx","tower-square.fbx","bridge_wood","campfire_stones"}.Any(path.Contains))continue;
                var obj=AssetDatabase.LoadAssetAtPath<GameObject>(path);var instance=(GameObject)PrefabUtility.InstantiatePrefab(obj);
                var bounds=new Bounds();bool first=true;
                foreach(var r in instance.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
                Debug.Log("ASSET_INSPECT "+path+" BOUNDS "+bounds+" MATERIALS "+string.Join(",",instance.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name+" color="+m.color+" tex="+(m.mainTexture?m.mainTexture.name:"none")).Distinct()));
                Debug.Log("CLIPS "+path+" "+string.Join(",",AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).Select(c=>c.name+" length="+c.length)));
                Object.DestroyImmediate(instance);
            }
            Debug.Log("ASSET_INSPECTION_SUCCESS");
        }
    }
}
