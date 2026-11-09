using UnityEngine;

namespace PixelArtLab
{
    [DefaultExecutionOrder(110)]
    public sealed class PixelCharacterAlignment : MonoBehaviour
    {
        public LabCharacter player;
        public ExplorationCamera follow;
        Vector3 basePosition;
        void Awake(){if(player&&player.Visual)basePosition=player.Visual.localPosition;}
        void LateUpdate()
        {
            if(!player||!player.Visual||!follow)return;
            if(!follow.pixelated){player.Visual.localPosition=basePosition;return;}
            var rotation=follow.transform.rotation;Vector3 world=player.transform.TransformPoint(basePosition),local=Quaternion.Inverse(rotation)*world;
            float pixels=follow.pixelsPerUnit;local.x=Mathf.Round(local.x*pixels)/pixels;local.y=Mathf.Round(local.y*pixels)/pixels;
            player.Visual.position=rotation*local;
        }
        void OnDisable(){if(player&&player.Visual)player.Visual.localPosition=basePosition;}
    }
}
