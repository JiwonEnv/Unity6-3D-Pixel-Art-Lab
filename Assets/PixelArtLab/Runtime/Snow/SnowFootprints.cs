using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab
{
    [DefaultExecutionOrder(135)]
    public sealed class SnowFootprints : MonoBehaviour
    {
        public LabCharacter player;
        [Tooltip("Leave empty to use the active EnvironmentState.")]
        public EnvironmentState environment;
        EnvironmentState Env=>environment?environment:EnvironmentState.Active;
        [Range(0,1)]public float minimumSnow=.4f;
        public float lifetime=18;
        public int EmittedCount {get;private set;}
        struct Print {public Vector3 position,normal,forward;public float birth;}
        readonly Print[] prints=new Print[128];
        readonly List<Vector3> vertices=new List<Vector3>();readonly List<Color> colors=new List<Color>();readonly List<int> indices=new List<int>();
        Mesh mesh;Material material;int next,foot;float distance;Vector3 previous;bool initialized,grounded;
        void Ensure()
        {
            if(mesh)return;
            mesh=new Mesh{name="Fading snow boot impressions"};mesh.MarkDynamic();
            var filter=GetComponent<MeshFilter>();if(!filter)filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var renderer=GetComponent<MeshRenderer>();if(!renderer)renderer=gameObject.AddComponent<MeshRenderer>();
            material=new Material(Shader.Find("PixelArtLab/Weather Particles")){hideFlags=HideFlags.HideAndDontSave};renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;gameObject.layer=LabLayers.Effects;
            for(int i=0;i<prints.Length;i++)prints[i].birth=-1000;
        }
        void LateUpdate()
        {
            Ensure();if(!player||!Env)return;
            Vector3 current=player.transform.position,delta=current-previous;delta.y=0;
            if(!initialized){previous=current;grounded=player.Grounded;initialized=true;delta=Vector3.zero;}
            if(delta.magnitude>4)distance=0;
            else if(player.Grounded)
            {
                Vector3 direction=delta.sqrMagnitude>.0001f?delta.normalized:player.VisualOrRoot.forward;
                if(!grounded){TryPlace(current-Vector3.Cross(Vector3.up,direction)*.14f,direction);TryPlace(current+Vector3.Cross(Vector3.up,direction)*.14f,direction);}
                if(player.HorizontalSpeed>.3f&&delta.magnitude>.001f)
                {
                    distance+=delta.magnitude;
                    if(distance>(player.HorizontalSpeed>5?1.1f:.8f))
                    {distance=0;TryPlace(current+Vector3.Cross(Vector3.up,direction)*((foot++%2==0?1:-1)*.14f),direction);}
                }
                else distance=0;
            }
            previous=current;grounded=player.Grounded;Refresh(Time.time);
        }
        public bool TryPlace(Vector3 position,Vector3 forward)
        {
            var environment=Env;
            if(!environment||environment.snowCover<minimumSnow||!PuddleSurface.Ground(position,out var hit)||hit.normal.y<.88f)return false;
            Ensure();prints[next]=new Print{position=hit.point+hit.normal*.058f,normal=hit.normal,forward=Vector3.ProjectOnPlane(forward,hit.normal).normalized,birth=Time.time};next=(next+1)%prints.Length;EmittedCount++;return true;
        }
        public void Refresh(float now)
        {
            Ensure();vertices.Clear();colors.Clear();indices.Clear();
            var environment=Env;if(!environment)return;
            foreach(var print in prints)
            {
                float age=now-print.birth;if(age<0||age>=lifetime)continue;
                float opacity=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,lifetime,age)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.4f,environment.snowCover));
                if(opacity<.005f)continue;
                Vector3 forward=print.forward.sqrMagnitude>.5f?print.forward:Vector3.forward,right=Vector3.Cross(print.normal,forward).normalized;
                Color color=new Color(.32f,.43f,.56f,opacity*.85f);
                Vector3 toe=print.position+forward*.08f;
                for(int i=0;i<10;i++)
                {
                    float a=i*Mathf.PI/5,b=(i+1)*Mathf.PI/5;
                    Triangle(toe,toe+right*(Mathf.Cos(a)*.09f)+forward*(Mathf.Sin(a)*.135f),toe+right*(Mathf.Cos(b)*.09f)+forward*(Mathf.Sin(b)*.135f),color);
                }
                Vector3 heel=print.position-forward*.13f;Vector3 x=right*.07f,y=forward*.055f;
                Triangle(heel-x-y,heel+x-y,heel+x+y,color);Triangle(heel-x-y,heel+x+y,heel-x+y,color);
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
        }
        void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
        {int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);indices.Add(i);indices.Add(i+1);indices.Add(i+2);}
        void OnDisable(){if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}mesh=null;material=null;initialized=false;}
    }
}
