using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixelArtLab.Editor
{
    // R3: stepped animation left ExplorerMotor for SteppedAnimator, and the lake shape that was
    // hardcoded in code and materials became a WaterBody. Values stay as they were.
    public static class ComponentMigration
    {
        public static readonly Vector2 LakeCenter=new Vector2(-30,22),LakeRadii=new Vector2(11.5f,10.5f);
        public const float LakeSurface=.36f;

        [MenuItem("Pixel Art Lab/Samples/Migrate Character and Water (R3)")]
        public static void Run()
        {
            foreach(var path in new[]{OpenWorldSceneBuilder.ScenePath,DetailedWoodlandBuilder.ScenePath})
            {
                var scene=EditorSceneManager.OpenScene(path);
                foreach(var player in Object.FindObjectsByType<ExplorerMotor>(FindObjectsInactive.Include,FindObjectsSortMode.None))AddStepped(player);
                foreach(var motion in Object.FindObjectsByType<WorldSurfaceMotion>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(m=>m.kind==WorldMotionKind.Water))AddLake(motion.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("COMPONENT_MIGRATION_SUCCESS");
        }
        public static void AddStepped(ExplorerMotor player)
        {
            if(!player.animator)return;
            var stepped=player.GetComponent<SteppedAnimator>();if(!stepped)stepped=player.gameObject.AddComponent<SteppedAnimator>();
            stepped.animator=player.animator;stepped.frameRate=12;stepped.enabled=player.steppedFacing;EditorUtility.SetDirty(stepped);
        }
        public static void AddLake(GameObject lake)
        {
            var water=lake.GetComponent<WaterBody>();if(!water)water=lake.AddComponent<WaterBody>();
            water.center=LakeCenter;water.radii=LakeRadii;water.surfaceHeight=LakeSurface;water.Apply();EditorUtility.SetDirty(water);
        }
    }
}
