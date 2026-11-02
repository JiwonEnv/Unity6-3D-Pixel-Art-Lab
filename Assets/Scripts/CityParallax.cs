using UnityEngine;
namespace PixelArtLab
{
    public sealed class CityParallax : MonoBehaviour
    {
        public Transform player;public float fraction=.16f;Vector3 origin;float start;
        void Awake(){origin=transform.position;start=player?player.position.x:0;}
        void LateUpdate(){if(player)transform.position=origin+Vector3.right*(player.position.x-start)*fraction;}
    }
}
