using UnityEngine;

namespace PixelArtLab
{
    [ExecuteAlways,RequireComponent(typeof(MeshFilter),typeof(MeshRenderer))]
    public sealed class PixelWeatherParticles : MonoBehaviour
    {
        public WorldEnvironment environment;
        public Material weatherMaterial;
        const int RainCount=320,SnowCount=220,SplashCount=36,Quads=RainCount+SnowCount+SplashCount*4;
        Mesh mesh;
        Vector3[] vertices=new Vector3[Quads*4];
        Color[] colors=new Color[Quads*4];
        Vector3[] seeds=new Vector3[RainCount+SnowCount+SplashCount];
        int lastTick=int.MinValue;
        Vector3 lastAnchor;
        public int ActiveDrops {get;private set;}
        public float PixelUnit {get;private set;}
        void OnEnable()
        {
            var random=new System.Random(6072);for(int i=0;i<seeds.Length;i++)seeds[i]=new Vector3((float)random.NextDouble(),(float)random.NextDouble(),(float)random.NextDouble());
            mesh=new Mesh{name="Pixel-aligned rain snow and splashes",hideFlags=HideFlags.HideAndDontSave};mesh.MarkDynamic();
            int[] triangles=new int[Quads*6];for(int i=0;i<Quads;i++){int v=i*4,t=i*6;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;}
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=GetComponent<MeshRenderer>();renderer.sharedMaterial=weatherMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            Refresh(true);
        }
        void LateUpdate()=>Refresh();
        public void Refresh(bool force=false)
        {
            if(!environment||!environment.worldCamera||!mesh)return;
            var camera=environment.worldCamera;Vector3 anchor=environment.player?environment.player.transform.position:camera.transform.position+camera.transform.forward*45;
            int tick=Mathf.FloorToInt(environment.animationTime*12);
            if(!force&&tick==lastTick&&(anchor-lastAnchor).sqrMagnitude<.01f)return;lastTick=tick;lastAnchor=anchor;
            System.Array.Clear(vertices,0,vertices.Length);System.Array.Clear(colors,0,colors.Length);
            float unit=camera.orthographicSize*2/(camera.targetTexture?camera.targetTexture.height:360);PixelUnit=unit;float time=tick/12f;
            int rain=Mathf.RoundToInt(RainCount*environment.rain),snow=Mathf.RoundToInt(SnowCount*environment.snow);ActiveDrops=rain+snow;
            float ax=Mathf.Floor(anchor.x/4)*4,az=Mathf.Floor(anchor.z/4)*4;
            var rotation=camera.transform.rotation;var inverse=Quaternion.Inverse(rotation);Vector3 right=camera.transform.right,up=camera.transform.up;
            var wind=environment.windEnabled?new Vector3(environment.windDirection.x,0,environment.windDirection.y)*environment.windStrength:Vector3.zero;
            for(int i=0;i<rain+snow;i++)
            {
                bool isSnow=i>=rain;var seed=seeds[isSnow?RainCount+i-rain:i];
                float speed=isSnow?1.6f:14;
                float x=ax+Mathf.Repeat(seed.x*32+wind.x*time*(isSnow?.7f:2),32)-16;
                float z=az+Mathf.Repeat(seed.z*32+wind.z*time*(isSnow?.7f:2),32)-16;
                if(isSnow){x+=Mathf.Sin(time*.9f+seed.y*20)*.45f;z+=Mathf.Cos(time*.7f+seed.x*20)*.3f;}
                float ground=Ground(x,z,anchor.y);
                Vector3 position=new Vector3(x,ground+Mathf.Repeat(seed.y*17-time*speed,17)+.12f,z);
                int width=isSnow?2:1,height=isSnow?2:5;Vector3 local=inverse*position;
                float halfX=(width&1)*.5f,halfY=(height&1)*.5f;
                local.x=(Mathf.Round(local.x/unit-halfX)+halfX)*unit;local.y=(Mathf.Round(local.y/unit-halfY)+halfY)*unit;position=rotation*local;
                Vector3 streak=isSnow?up:Vector3.ProjectOnPlane(Vector3.down+wind*.22f,camera.transform.forward).normalized;
                Quad(i,position,right*unit*width*.5f,streak*unit*height*.5f,isSnow?new Color(.94f,.97f,1,.92f):new Color(.55f,.76f,.91f,.65f));
            }
            for(int i=0;i<Mathf.RoundToInt(SplashCount*environment.rain);i++)
            {
                var seed=seeds[RainCount+SnowCount+i];float x=ax+seed.x*28-14,z=az+seed.z*28-14;
                float age=Mathf.Repeat(time*1.7f+seed.y,1),radius=(2+Mathf.Floor(age*4))*unit;
                Vector3 center=new Vector3(x,Ground(x,z,anchor.y)+.04f,z);float thickness=unit*.55f;Color color=new Color(.64f,.81f,.85f,(1-age)*.65f);
                int first=RainCount+SnowCount+i*4;
                Quad(first,center+Vector3.forward*radius,Vector3.right*radius,Vector3.forward*thickness,color);
                Quad(first+1,center-Vector3.forward*radius,Vector3.right*radius,Vector3.forward*thickness,color);
                Quad(first+2,center+Vector3.right*radius,Vector3.forward*radius,Vector3.right*thickness,color);
                Quad(first+3,center-Vector3.right*radius,Vector3.forward*radius,Vector3.right*thickness,color);
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
        }
        static float Ground(float x,float z,float fallback)
        {
            float height=Physics.Raycast(new Vector3(x,fallback+40,z),Vector3.down,out var hit,100,~(1<<8),QueryTriggerInteraction.Ignore)?hit.point.y:fallback;
            if(((x+30)*(x+30))/(11.5f*11.5f)+((z-22)*(z-22))/(10.5f*10.5f)<1)height=Mathf.Max(height,.36f);
            return height;
        }
        void Quad(int index,Vector3 center,Vector3 right,Vector3 up,Color color)
        {
            int i=index*4;vertices[i]=center-right-up;vertices[i+1]=center+right-up;vertices[i+2]=center+right+up;vertices[i+3]=center-right+up;
            for(int k=0;k<4;k++)colors[i+k]=color;
        }
        void OnDisable(){if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
