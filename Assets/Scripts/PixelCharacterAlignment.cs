using UnityEngine;

namespace PixelArtLab
{
    [DefaultExecutionOrder(110)]
    public sealed class PixelCharacterAlignment : MonoBehaviour
    {
        public ExplorerMotor player;
        public ExplorationCamera follow;
        Vector3 basePosition;
        void Awake(){if(player&&player.visual)basePosition=player.visual.localPosition;}
        void LateUpdate()
        {
            if(!player||!player.visual||!follow)return;
            if(!follow.pixelated){player.visual.localPosition=basePosition;return;}
            var rotation=follow.transform.rotation;Vector3 world=player.transform.TransformPoint(basePosition),local=Quaternion.Inverse(rotation)*world;
            float pixels=follow.pixelsPerUnit;local.x=Mathf.Round(local.x*pixels)/pixels;local.y=Mathf.Round(local.y*pixels)/pixels;
            player.visual.position=rotation*local;
        }
        void OnDisable(){if(player&&player.visual)player.visual.localPosition=basePosition;}
    }
}
