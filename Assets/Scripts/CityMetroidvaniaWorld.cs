using UnityEngine;
namespace PixelArtLab
{
    public sealed class CityMetroidvaniaWorld : MonoBehaviour
    {
        public ExplorerMotor player;
        public ExplorationCamera follow;
        public WorldEnvironment environment;
        public Transform dashModule,jumpModule,signalTerminal;
        public Transform[] checkpoints;
        public GameObject powerGate;
        public Vector3 liftBottom=new Vector3(26,2.08f,0),liftTop=new Vector3(26,14.08f,0);
        public bool cityRestored;
        public int ActiveCheckpoint {get;private set;}=-1;
        public int LiftTrips {get;private set;}
        bool showMap;float bannerUntil;string banner="Westworks: recover the dash module above the station";
        GUIStyle title,label,small;
        public static readonly Vector2[] RouteNodes={new Vector2(-52,8),new Vector2(-43,14),new Vector2(-25,14),new Vector2(-15,8),new Vector2(-15,2),new Vector2(10,2),new Vector2(26,2),new Vector2(26,14),new Vector2(10,8),new Vector2(26,8),new Vector2(50,14),new Vector2(48,17.5f),new Vector2(42,20),new Vector2(56,22)};
        public static readonly int[,] Connections={{0,1},{1,2},{0,3},{3,4},{4,5},{5,6},{6,7},{3,8},{8,9},{9,7},{7,10},{10,11},{11,12},{12,13},{9,10}};
        void Awake(){Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;bannerUntil=Time.time+8;}
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.M))showMap=!showMap;
            if(Input.GetKeyDown(KeyCode.Escape))Time.timeScale=Time.timeScale==0?1:0;
            if(Input.GetKeyDown(KeyCode.E))TryInteract();
            if(player.transform.position.y<.75f&&((player.transform.position.x>-6&&player.transform.position.x<2)||(player.transform.position.x>30&&player.transform.position.x<38)))
            {player.Respawn();Notify("Flooded culvert — returned to checkpoint");}
            for(int i=0;i<checkpoints.Length;i++)if(i!=ActiveCheckpoint&&player.Grounded&&Vector2.Distance(player.transform.position,checkpoints[i].position)<1.1f)
            {ActiveCheckpoint=i;player.spawnPoint=checkpoints[i].position;Notify("Checkpoint synchronized");}
            if(dashModule&&dashModule.gameObject.activeSelf)dashModule.Rotate(0,60*Time.deltaTime,0);
            if(jumpModule&&jumpModule.gameObject.activeSelf)jumpModule.Rotate(0,60*Time.deltaTime,0);
            player.Region=player.transform.position.y<5?"Drainway":player.transform.position.x>38?"Crown Relay":player.transform.position.x<-35?(player.transform.position.y>11?"Westworks":"Rainwell Station"):player.transform.position.y>11?"Rooftop Exchange":"Market Viaduct";
        }
        void Notify(string message){banner=message;bannerUntil=Time.time+6;}
        bool Near(Vector3 point,float range=1.8f)=>Vector2.Distance(player.transform.position+Vector3.up*.8f,point)<range;
        public bool TryInteract()
        {
            if(!player.dashUnlocked&&dashModule&&Near(dashModule.position)){player.dashUnlocked=true;dashModule.gameObject.SetActive(false);Notify("DASH ONLINE  /  Ctrl or X — cross the Drainway gap");return true;}
            if(!player.doubleJumpUnlocked&&jumpModule&&Near(jumpModule.position)){player.doubleJumpUnlocked=true;jumpModule.gameObject.SetActive(false);if(powerGate)powerGate.SetActive(false);Notify("AIR STEP ONLINE  /  Space again — shutter and lift unlocked");return true;}
            if(player.doubleJumpUnlocked&&(Near(liftBottom+Vector3.up*.8f)||Near(liftTop+Vector3.up*.8f)))
            {bool up=Mathf.Abs(player.transform.position.y-liftBottom.y)<3;player.Teleport(up?liftTop:liftBottom);follow.SnapToPlayer();LiftTrips++;Notify("Service lift — shortcut connected");return true;}
            if(!cityRestored&&Near(signalTerminal.position)&&player.dashUnlocked&&player.doubleJumpUnlocked){cityRestored=true;Notify("CITY SIGNAL RESTORED  /  All routes open");return true;}
            return false;
        }
        string Prompt()
        {
            if(!player.dashUnlocked&&dashModule&&Near(dashModule.position))return "E  Recover dash module";
            if(!player.doubleJumpUnlocked&&jumpModule&&Near(jumpModule.position))return "E  Recover air-step module";
            if(Near(liftBottom+Vector3.up*.8f)||Near(liftTop+Vector3.up*.8f))return player.doubleJumpUnlocked?"E  Use service lift":"Service lift — air-step power required";
            if(Near(signalTerminal.position))return cityRestored?"Signal restored":"E  Restore city signal";
            return "";
        }
        void OnGUI()
        {
            if(!player)return;GUI.depth=-30;
            if(title==null){title=new GUIStyle(GUI.skin.label){fontSize=17,fontStyle=FontStyle.Bold,normal={textColor=new Color(.65f,.9f,1)}};label=new GUIStyle(GUI.skin.label){fontSize=12,normal={textColor=new Color(.91f,.94f,.93f)}};small=new GUIStyle(label){fontSize=11};}
            Panel(new Rect(18,18,355,99));GUI.Label(new Rect(31,24,335,26),"RAINFALL / CITY CIRCUIT",title);GUI.Label(new Rect(31,50,335,22),player.Region+"  /  "+environment.ClockLabel+"  "+environment.weather,label);
            GUI.Label(new Rect(31,73,335,21),"Dash "+(player.dashUnlocked?"ONLINE":"LOCKED")+"   Air step "+(player.doubleJumpUnlocked?"ONLINE":"LOCKED")+"   Signal "+(cityRestored?"ON":"OFF"),small);
            Panel(new Rect(18,Screen.height-65,Mathf.Min(760,Screen.width-36),49));GUI.Label(new Rect(30,Screen.height-61,740,21),"A/D or arrows  Move    SPACE  Jump / Air step    CTRL or X  Dash    E  Interact    M  Map",small);GUI.Label(new Rect(30,Screen.height-40,740,21),"R  Checkpoint    F1–F4  Time    1/2/3  Clear/Rain/Snow    TAB  Pixels    F5/F6/F7  Rim/Reveal/Scan",small);
            string prompt=Prompt();if(prompt!=""){Panel(new Rect(Screen.width*.5f-200,Screen.height-110,400,30));GUI.Label(new Rect(Screen.width*.5f-185,Screen.height-106,380,25),prompt,label);}
            if(Time.time<bannerUntil){Panel(new Rect(Screen.width*.5f-290,125,580,30));GUI.Label(new Rect(Screen.width*.5f-275,129,555,25),banner,label);}
            DrawMap(showMap?new Rect(Screen.width*.5f-350,Screen.height*.5f-155,700,310):new Rect(Screen.width-215,18,197,100),showMap);
        }
        static void Panel(Rect r){GUI.color=new Color(.025f,.055f,.09f,.9f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
        Vector2 MapPoint(Vector2 p,Rect rect)=>new Vector2(rect.x+12+(p.x+65)/130*(rect.width-24),rect.y+rect.height-12-(p.y-1)/24*(rect.height-24));
        void DrawMap(Rect rect,bool large)
        {
            Panel(rect);
            for(int i=0;i<Connections.GetLength(0);i++){Vector2 a=MapPoint(RouteNodes[Connections[i,0]],rect),b=MapPoint(RouteNodes[Connections[i,1]],rect);Line(a,b,new Color(.23f,.48f,.56f),large?3:2);}
            for(int i=0;i<RouteNodes.Length;i++){Vector2 p=MapPoint(RouteNodes[i],rect);GUI.color=(i==1&&!player.dashUnlocked)||(i==5&&!player.doubleJumpUnlocked)?new Color(1,.7f,.25f):new Color(.35f,.74f,.79f);GUI.DrawTexture(new Rect(p.x-3,p.y-3,6,6),Texture2D.whiteTexture);}
            Vector2 current=MapPoint(player.transform.position,rect);GUI.color=Color.white;GUI.DrawTexture(new Rect(current.x-3,current.y-3,6,6),Texture2D.whiteTexture);
            if(large){GUI.Label(new Rect(rect.x+18,rect.y+8,650,24),"CITY ROUTES — station / workshop / drainway / rooftops / crown",label);GUI.Label(new Rect(rect.x+18,rect.y+rect.height-26,660,22),!player.dashUnlocked?"WESTWORKS: climb above the station for dash":!player.doubleJumpUnlocked?"DRAINWAY: dash across the lower 8m gap for air step":"ROOFTOPS: service lift at x26; double jump to Crown Relay",label);}
        }
        static void Line(Vector2 a,Vector2 b,Color color,float width){var matrix=GUI.matrix;GUI.color=color;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=Color.white;}
        void OnDisable(){Time.timeScale=1;}
    }
}
