using UnityEngine;

namespace PixelArtLab
{
    public sealed class ExplorationWorld : MonoBehaviour
    {
        public ExplorerMotor player;
        public ExplorationCamera followCamera;
        public Transform[] landmarks;
        public string[] landmarkNames;
        public Transform[] crystals;
        public int collected;
        public bool[] visited;
        public bool showGuide=true;
        GUIStyle heading,label,small;
        float bannerUntil;
        string banner="Find the three ancient waystones";
        public int VisitedCount {get{int n=0;if(visited!=null)foreach(bool v in visited)if(v)n++;return n;}}
        void Awake(){visited=new bool[landmarks.Length];bannerUntil=Time.time+7;Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;}
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.H))showGuide=!showGuide;
            if(Input.GetKeyDown(KeyCode.Escape))Time.timeScale=Time.timeScale==0?1:0;
            if(!player)return;
            Vector3 position=player.transform.position;
            float lake=((position.x+30)*(position.x+30))/(11.5f*11.5f)+((position.z-22)*(position.z-22))/(10.5f*10.5f);
            if(lake<1&&position.y<.32f){player.Respawn();banner="Deep water — returned to camp";bannerUntil=Time.time+3;}
            float nearest=float.MaxValue;int region=0;
            for(int i=0;i<landmarks.Length;i++)
            {
                float distance=Vector3.Distance(player.transform.position,landmarks[i].position);
                if(distance<nearest){nearest=distance;region=i;}
                if(distance<6&&!visited[i]){visited[i]=true;banner="Discovered: "+landmarkNames[i];bannerUntil=Time.time+4;}
            }
            player.Region=landmarkNames[region];
            foreach(var crystal in crystals)
            {
                if(!crystal||!crystal.gameObject.activeSelf)continue;
                crystal.Rotate(0,45*Time.deltaTime,0,Space.World);
                if(Vector3.Distance(player.transform.position+Vector3.up*.8f,crystal.position)<1.4f)
                {
                    crystal.gameObject.SetActive(false);collected++;banner=collected==crystals.Length?"All waystones restored — the valley is yours to explore":"Waystone restored  "+collected+" / "+crystals.Length;bannerUntil=Time.time+5;
                }
            }
        }
        void OnGUI()
        {
            GUI.depth=-10;
            if(!player)return;
            if(heading==null)
            {
                heading=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold,normal={textColor=new Color(.96f,.86f,.64f)}};
                label=new GUIStyle(GUI.skin.label){fontSize=13,normal={textColor=new Color(.92f,.92f,.85f)}};
                small=new GUIStyle(label){fontSize=12};
            }
            Panel(new Rect(20,20,330,85));GUI.Label(new Rect(35,29,310,26),"AMBERWOOD / FREE ROAM",heading);
            GUI.Label(new Rect(35,55,310,23),player.Region,label);
            GUI.Label(new Rect(35,77,310,22),$"Waystones {collected}/{crystals.Length}    Regions {VisitedCount}/{landmarks.Length}",small);
            DrawMap();
            if(showGuide)
            {
                Panel(new Rect(20,Screen.height-79,690,59));
                GUI.Label(new Rect(34,Screen.height-74,660,24),"WASD / Arrows  Move   SHIFT  Sprint   SPACE  Jump   R  Return to camp",label);
                GUI.Label(new Rect(34,Screen.height-49,660,24),"Q/E  Orbit   Wheel  Zoom   T  Toon   O  Ink   TAB  Pixels   H  Guide   ESC  Pause",small);
            }
            if(Time.time<bannerUntil){Panel(new Rect(Screen.width*.5f-240,120,480,34));GUI.Label(new Rect(Screen.width*.5f-226,125,460,26),banner,label);}
            if(Time.timeScale==0){Panel(new Rect(Screen.width*.5f-100,Screen.height*.5f-30,200,60));GUI.Label(new Rect(Screen.width*.5f-80,Screen.height*.5f-18,170,30),"PAUSED — ESC",heading);}
        }
        void DrawMap()
        {
            Rect map=new Rect(Screen.width-174,20,154,154);Panel(map);
            GUI.color=new Color(.36f,.47f,.38f);GUI.DrawTexture(new Rect(map.x+8,map.y+8,138,138),Texture2D.whiteTexture);
            for(int i=0;i<landmarks.Length;i++)
            {
                var p=MapPosition(landmarks[i].position,map);GUI.color=visited[i]?new Color(1,.77f,.35f):new Color(.62f,.67f,.62f);GUI.DrawTexture(new Rect(p.x-3,p.y-3,6,6),Texture2D.whiteTexture);
            }
            var marker=MapPosition(player.transform.position,map);GUI.color=Color.white;GUI.DrawTexture(new Rect(marker.x-3,marker.y-3,6,6),Texture2D.whiteTexture);GUI.color=Color.white;
        }
        static Vector2 MapPosition(Vector3 p,Rect map)=>new Vector2(map.x+map.width*.5f+p.x*.86f,map.y+map.height*.5f-p.z*.86f);
        static void Panel(Rect rect){GUI.color=new Color(.075f,.09f,.12f,.90f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        void OnDisable(){Time.timeScale=1;}
    }
}
