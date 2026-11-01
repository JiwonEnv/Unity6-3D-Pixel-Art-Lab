using UnityEngine;

namespace PixelArtLab
{
    // Keep in sync with Shaders/PuddleMask.cginc: interaction must follow visible water.
    public static class PuddleSurface
    {
        static float Seed(int x,int z)
        {
            unchecked{uint h=(uint)x*374761393u+(uint)z*668265263u+7919u;h=(h^(h>>13))*1274126177u;h^=h>>16;return (h&65535u)/65535f;}
        }
        static float Smooth(float a,float b,float x){float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3-2*t);}
        public static float Coverage(Vector3 p,Vector3 normal,float wetness,float snow)
        {
            int x=Mathf.FloorToInt(p.x/6),z=Mathf.FloorToInt(p.z/6);float seed=Seed(x,z),second=Seed(x+73,z+19);
            var offset=new Vector2(p.x/6-x-(.25f+seed*.5f),p.z/6-z-(.25f+second*.5f));
            float radius=Vector2.Scale(offset,new Vector2(.8f+second*.65f,.8f+seed*.65f)).magnitude;
            float shore=Mathf.Sin(p.x*1.7f+Mathf.Sin(p.z*2.1f))*.045f+Mathf.Sin(p.z*4.7f+p.x*.5f)*.02f;
            float fill=Smooth(.22f,.95f,wetness);
            return (1-Smooth(.15f+fill*.13f,.175f+fill*.13f,radius+shore))*fill*(seed>=.2f?1:0)*Smooth(.88f,.99f,normal.y)*(1-Mathf.Clamp01(snow*2))*Smooth(.38f,.6f,p.y);
        }
        public static bool Ground(Vector3 position,out RaycastHit hit)
        {
            if(!Physics.Raycast(position+Vector3.up,Vector3.down,out hit,3,~((1<<8)|(1<<4)|(1<<13)),QueryTriggerInteraction.Ignore))return false;
            var renderer=hit.collider.GetComponent<MeshRenderer>();if(!renderer)return false;
            foreach(var material in renderer.sharedMaterials)if(material&&material.HasProperty("_Puddles")&&material.GetFloat("_Puddles")>.5f)return true;
            return false;
        }
    }
}
