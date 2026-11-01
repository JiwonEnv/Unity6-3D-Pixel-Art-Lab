using UnityEngine;

namespace PixelArtLab
{
    public class SpriteWorldControls : MonoBehaviour
    {
        public Camera worldCamera;
        public PixelLinework linework;
        public int sourceHeight=360;
        public int pixelsPerUnit=21;
        public bool snapCamera=true;
        public bool toon=true;
        public bool lowResolution=true;
        Vector3 focus=new Vector3(0,1.1f,0);
        float yaw=42;
        RenderTexture buffer;
        SpriteRenderer[] billboards;
        GUIStyle text;
        bool hud=true;
        public Rect OutputRect {get; private set;}
        void OnEnable(){billboards=FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);ApplyCamera();}
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Tab))lowResolution=!lowResolution;
            if(Input.GetKeyDown(KeyCode.O))linework.linework=!linework.linework;
            if(Input.GetKeyDown(KeyCode.P))snapCamera=!snapCamera;
            if(Input.GetKeyDown(KeyCode.T))toon=!toon;
            if(Input.GetKeyDown(KeyCode.H))hud=!hud;
            if(Input.GetKeyDown(KeyCode.R)){focus=new Vector3(0,1.1f,0);yaw=42;pixelsPerUnit=21;}
            if(Input.GetKey(KeyCode.Q))yaw-=Time.deltaTime*35;
            if(Input.GetKey(KeyCode.E))yaw+=Time.deltaTime*35;
            if(Input.mouseScrollDelta.y!=0)pixelsPerUnit=Mathf.Clamp(pixelsPerUnit+Mathf.RoundToInt(Input.mouseScrollDelta.y),12,42);
            var rotation=Quaternion.Euler(0,yaw,0);
            focus+=(rotation*Vector3.right*Input.GetAxisRaw("Horizontal")+rotation*Vector3.forward*Input.GetAxisRaw("Vertical"))*Time.deltaTime*5;
            ApplyCamera();
        }
        public void ApplyCamera()
        {
            if(!worldCamera)return;
            sourceHeight=Mathf.Clamp(sourceHeight,180,720);pixelsPerUnit=Mathf.Clamp(pixelsPerUnit,12,42);
            int height=Mathf.Min(sourceHeight,Mathf.Max(1,Screen.height));
            int scale=Mathf.Max(1,Mathf.FloorToInt((float)Screen.height/height));
            int width=Mathf.Max(1,Screen.width/scale);
            if(lowResolution && (!buffer || buffer.width!=width || buffer.height!=height))
            {
                Release();buffer=new RenderTexture(width,height,24){filterMode=FilterMode.Point,antiAliasing=1,name="Sprite World Pixel Buffer"};buffer.Create();
            }
            float pitch=35;
            if(snapCamera)pitch=Mathf.Asin(Mathf.Round(Mathf.Sin(pitch*Mathf.Deg2Rad)*pixelsPerUnit)/pixelsPerUnit)*Mathf.Rad2Deg;
            var orientation=Quaternion.Euler(pitch,yaw,0);
            Vector3 position=focus-orientation*Vector3.forward*30;
            if(snapCamera)
            {
                var local=Quaternion.Inverse(orientation)*position;
                local.x=(Mathf.Round(local.x*pixelsPerUnit-((width&1)*.5f))+(width&1)*.5f)/pixelsPerUnit;
                local.y=(Mathf.Round(local.y*pixelsPerUnit-((height&1)*.5f))+(height&1)*.5f)/pixelsPerUnit;
                position=orientation*local;
            }
            worldCamera.transform.SetPositionAndRotation(position,orientation);
            worldCamera.orthographicSize=height/(2f*pixelsPerUnit);
            worldCamera.targetTexture=lowResolution?buffer:null;
            OutputRect=new Rect((Screen.width-width*scale)*.5f,(Screen.height-height*scale)*.5f,width*scale,height*scale);
            Shader.SetGlobalFloat("_LabContinuous",toon?0:1);
            if(billboards!=null)foreach(var sprite in billboards)if(sprite)sprite.transform.rotation=orientation;
        }
        void Release(){if(buffer){buffer.Release();Destroy(buffer);buffer=null;}}
        void OnDisable(){if(worldCamera)worldCamera.targetTexture=null;Release();Shader.SetGlobalFloat("_LabContinuous",0);}
        void OnGUI()
        {
            if(lowResolution&&buffer){GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);GUI.DrawTexture(OutputRect,buffer,ScaleMode.StretchToFill);}
            if(!hud)return;
            if(text==null)text=new GUIStyle(GUI.skin.label){fontSize=12,normal={textColor=new Color(.94f,.91f,.81f)}};
            GUI.color=new Color(.12f,.12f,.17f,.92f);GUI.DrawTexture(new Rect(18,18,385,52),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(31,24,370,23),"3D SPRITE WORLD  /  RENDERING STUDY 02",text);
            GUI.Label(new Rect(31,44,370,23),$"Toon {(toon?"ON":"OFF")}   Ink {(linework.linework?"ON":"OFF")}   Snap {(snapCamera?"ON":"OFF")}   {pixelsPerUnit} px/unit",text);
            GUI.color=new Color(.12f,.12f,.17f,.92f);GUI.DrawTexture(new Rect(18,Screen.height-65,690,47),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(31,Screen.height-61,670,22),"WASD  Pan   Q/E  Orbit   Wheel  Pixel density   R  Reset   H  Hide guide",text);
            GUI.Label(new Rect(31,Screen.height-41,670,22),"T  Toon / Continuous   O  Linework   P  Pixel snap   TAB  Low / Native resolution",text);
        }
    }
}
