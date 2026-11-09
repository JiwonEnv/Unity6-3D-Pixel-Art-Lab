using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PixelArtLab.Editor
{
    // Default Sprite Toon inspector that keeps the feature keywords in sync with the values.
    public sealed class SpriteToonGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor editor,MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            base.OnGUI(editor,properties);
            if(EditorGUI.EndChangeCheck())foreach(var target in editor.targets.OfType<Material>())SpriteToonKeywords.Sync(target);
        }
        public override void ValidateMaterial(Material material)=>SpriteToonKeywords.Sync(material);

        // Re-syncs every Sprite Toon material in the project, e.g. after importing materials.
        [MenuItem("Pixel Art Lab/Materials/Sync Sprite Toon Keywords")]
        public static void SyncAll()
        {
            int count=0;
            foreach(var path in AssetDatabase.FindAssets("t:Material",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!SpriteToonKeywords.IsSpriteToon(material))continue;
                SpriteToonKeywords.Sync(material);EditorUtility.SetDirty(material);count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"SPRITE_TOON_KEYWORDS_SYNCED {count} materials");
        }
    }
}
