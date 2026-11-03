using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    public static class CharacterNormalUpgrade
    {
        [MenuItem("Pixel Art Lab/Apply Knight Sprite Setup to Woodland")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var player=Object.FindFirstObjectByType<ExplorerMotor>();
            OpenWorldSceneBuilder.KnightLoadout(player.visual.gameObject);OpenWorldSceneBuilder.CharacterNormals(player.visual.gameObject);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log("KNIGHT_NORMALS_APPLIED");
        }
    }
}
