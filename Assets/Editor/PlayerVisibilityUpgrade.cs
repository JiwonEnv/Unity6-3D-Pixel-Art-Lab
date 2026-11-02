using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace PixelArtLab.Editor
{
    public static class PlayerVisibilityUpgrade
    {
        public static PlayerVisibility Configure(Camera camera)
        {
            var player=UnityEngine.Object.FindFirstObjectByType<ExplorerMotor>();
            foreach(var actor in UnityEngine.Object.FindObjectsByType<PixelArtObject>(FindObjectsSortMode.None))
            {
                if(actor.transform.IsChildOf(player.transform)||actor.gameObject==player.gameObject)continue;
                var renderers=actor.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer).ToArray();if(renderers.Length==0)continue;
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);string name=actor.name.ToLowerInvariant();
                bool building=name.Contains("wall")||name.Contains("tower")||name.Contains("gate")||name.Contains("arch")||name.Contains("column")||name.Contains("tent")||name.Contains("roof")||name.Contains("building")||name.Contains("skyscraper");
                bool receiver=name.Contains("walkable hills")||name.Contains("dirt trails")||renderers.All(r=>r.gameObject.layer==4||r.gameObject.layer==13);
                bool lowProp=name.Contains("sign")||name.Contains("fence");
                var mode=receiver||lowProp?PlayerOcclusionMode.Ignore:building&&bounds.size.y>=1.4f?PlayerOcclusionMode.BuildingScan:bounds.size.y>=2.2f?PlayerOcclusionMode.NaturalFade:PlayerOcclusionMode.Ignore;
                var target=actor.GetComponent<PlayerOccluder>();if(mode==PlayerOcclusionMode.Ignore){if(target)target.mode=mode;continue;}
                if(!target)target=actor.gameObject.AddComponent<PlayerOccluder>();target.mode=mode;target.renderers=renderers;
            }
            var visibility=camera.GetComponent<PlayerVisibility>();if(!visibility)visibility=camera.gameObject.AddComponent<PlayerVisibility>();visibility.player=player;visibility.occluders=UnityEngine.Object.FindObjectsByType<PlayerOccluder>(FindObjectsSortMode.None);visibility.Refresh(1);return visibility;
        }
        public static void Apply()
        {
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();var visibility=Configure(env.worldCamera);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log($"PLAYER_VISIBILITY_CONFIGURED trees={visibility.occluders.Count(t=>t.mode==PlayerOcclusionMode.NaturalFade)} buildings={visibility.occluders.Count(t=>t.mode==PlayerOcclusionMode.BuildingScan)}");
        }
    }
}
