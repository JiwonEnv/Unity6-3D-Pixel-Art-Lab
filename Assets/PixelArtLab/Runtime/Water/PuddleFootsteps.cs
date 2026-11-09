using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab
{
    [DefaultExecutionOrder(130)]
    public sealed class PuddleFootsteps : MonoBehaviour
    {
        public LabCharacter player;
        [Tooltip("Leave empty to use the active EnvironmentState.")]
        public EnvironmentState environment;
        EnvironmentState Env=>environment?environment:EnvironmentState.Active;
        [Tooltip("Droplets face this camera. Empty: Camera.main.")]
        public Camera targetCamera;
        public int EmittedCount {get;private set;}
        struct Step {public Vector3 position,normal;public float birth,strength;}
        readonly Step[] steps=new Step[12];
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Color> colors=new List<Color>();
        readonly List<int> triangles=new List<int>();
        Mesh mesh;Material material;
        Vector3 previous;float distance;int next,foot;bool grounded,initialized;
        void Ensure()
        {
            if(mesh)return;
            mesh=new Mesh{name="Puddle footfall rings and droplets"};mesh.MarkDynamic();
            var filter=GetComponent<MeshFilter>();if(!filter)filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var renderer=GetComponent<MeshRenderer>();if(!renderer)renderer=gameObject.AddComponent<MeshRenderer>();
            material=new Material(Shader.Find("PixelArtLab/Weather Particles")){hideFlags=HideFlags.HideAndDontSave};renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;gameObject.layer=LabLayers.Effects;
            for(int i=0;i<steps.Length;i++)steps[i].birth=-100;
        }
        void LateUpdate()
        {
            Ensure();if(!player||!Env)return;
            Vector3 current=player.transform.position;
            if(!initialized){previous=current;grounded=player.Grounded;initialized=true;}
            Vector3 delta=current-previous;delta.y=0;
            if(delta.magnitude>4){distance=0;grounded=player.Grounded;}
            else if(player.Grounded)
            {
                if(!grounded)TryStep(current,1.6f);
                if(delta.magnitude>.001f&&player.HorizontalSpeed>.3f)
                {
                    distance+=delta.magnitude;
                    if(distance>=(player.HorizontalSpeed>5?1.35f:1.1f))
                    {
                        distance=0;Vector3 side=Vector3.Cross(Vector3.up,delta.normalized)*((foot++%2==0?1:-1)*.13f);
                        TryStep(current+side,player.HorizontalSpeed>5?1.25f:1);
                    }
                }
                else distance=0;
            }
            previous=current;grounded=player.Grounded;Refresh(Time.time);
        }
        public bool TryStep(Vector3 position,float strength=1)
        {
            var environment=Env;
            if(!environment||!PuddleSurface.Ground(position,out var hit))return false;
            if(PuddleSurface.Coverage(hit.point,hit.normal,environment.wetness,environment.snowCover)<.3f)return false;
            Ensure();steps[next]=new Step{position=hit.point+hit.normal*.055f,normal=hit.normal,birth=Time.time,strength=strength};next=(next+1)%steps.Length;EmittedCount++;return true;
        }
        public void Refresh(float now)
        {
            Ensure();vertices.Clear();colors.Clear();triangles.Clear();
            var environment=Env;var camera=targetCamera?targetCamera:Camera.main;if(!environment||!camera)return;
            foreach(var step in steps)
            {
                float age=now-step.birth;if(age<0||age>.85f)continue;
                Vector3 right=Vector3.Cross(step.normal,Vector3.forward).normalized,forward=Vector3.Cross(right,step.normal).normalized;
                float radius=(.1f+age*1.5f)*step.strength,thickness=.025f;
                for(int i=0;i<20;i++)
                {
                    float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;
                    Vector3 u=right*Mathf.Cos(a)+forward*Mathf.Sin(a),v=right*Mathf.Cos(b)+forward*Mathf.Sin(b);
                    Vector3 middle=step.position+(u+v)*radius*.5f;
                    if(PuddleSurface.Coverage(middle,step.normal,environment.wetness,environment.snowCover)<.2f)continue;
                    Quad(step.position+u*(radius-thickness),step.position+v*(radius-thickness),step.position+v*(radius+thickness),step.position+u*(radius+thickness),new Color(.69f,.89f,.94f,(1-age/.85f)*.8f));
                }
                if(age<.45f)for(int i=0;i<6;i++)
                {
                    float angle=i*Mathf.PI/3;Vector3 direction=right*Mathf.Cos(angle)+forward*Mathf.Sin(angle);
                    Vector3 position=step.position+direction*age*.9f*step.strength+step.normal*(age*(1.5f+(i%3)*.25f)-age*age*4);
                    if(Vector3.Dot(position-step.position,step.normal)<0)continue;
                    float size=.027f;Vector3 x=camera.transform.right*size,y=camera.transform.up*size;
                    Quad(position-x-y,position+x-y,position+x+y,position-x+y,new Color(.78f,.94f,1,1-age/.45f));
                }
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        }
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
        {
            int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);for(int n=0;n<4;n++)colors.Add(color);
            triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);triangles.Add(i);triangles.Add(i+2);triangles.Add(i+3);
        }
        void OnDisable()
        {
            if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
            if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
            mesh=null;material=null;initialized=false;
        }
    }
}
