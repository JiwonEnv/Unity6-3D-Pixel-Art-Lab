using UnityEngine;

namespace PixelArtLab
{
    [RequireComponent(typeof(Camera))]
    public sealed class ExplorationCamera : MonoBehaviour
    {
        public ExplorerMotor player;
        public PixelLinework linework;
        public int sourceHeight=360,pixelsPerUnit=25;
        public float yaw=42,pitch=38;
        public bool pixelated=true,toon=true;
        Camera cameraComponent;
        RenderTexture buffer;
        Vector3 focus,velocity;
        bool initialized;
        public Rect OutputRect {get; private set;}
        void Awake(){cameraComponent=GetComponent<Camera>();Shader.SetGlobalFloat("_LabContinuous",0);}
        void LateUpdate()
        {
            if(!player)return;
            if(Input.GetKey(KeyCode.Q))yaw-=Time.deltaTime*65;
            if(Input.GetKey(KeyCode.E))yaw+=Time.deltaTime*65;
            pixelsPerUnit=Mathf.Clamp(pixelsPerUnit+Mathf.RoundToInt(Input.mouseScrollDelta.y*2),16,40);
            if(Input.GetKeyDown(KeyCode.T))toon=!toon;
            if(Input.GetKeyDown(KeyCode.O))linework.linework=!linework.linework;
            if(Input.GetKeyDown(KeyCode.Tab))pixelated=!pixelated;
            Vector3 target=player.transform.position+Vector3.up*.95f;
            if(!initialized || Vector3.Distance(focus,target)>15){focus=target;initialized=true;velocity=Vector3.zero;}
            focus=Vector3.SmoothDamp(focus,target,ref velocity,.13f);
            ApplyCamera();
        }
        public void SnapToPlayer(){if(player){focus=player.transform.position+Vector3.up*.95f;velocity=Vector3.zero;initialized=true;ApplyCamera();}}
        void ApplyCamera()
        {
            if(!cameraComponent)cameraComponent=GetComponent<Camera>();
            int height=Mathf.Min(sourceHeight,Mathf.Max(1,Screen.height)),scale=Mathf.Max(1,Screen.height/height),width=Mathf.Max(1,Screen.width/scale);
            if(pixelated&&(!buffer||buffer.width!=width||buffer.height!=height))
            {
                Release();buffer=new RenderTexture(width,height,24){filterMode=FilterMode.Point,antiAliasing=1,name="Exploration Pixel Buffer"};buffer.Create();
            }
            float alignedPitch=Mathf.Asin(Mathf.Round(Mathf.Sin(pitch*Mathf.Deg2Rad)*pixelsPerUnit)/pixelsPerUnit)*Mathf.Rad2Deg;
            var rotation=Quaternion.Euler(alignedPitch,yaw,0);var local=Quaternion.Inverse(rotation)*(focus-rotation*Vector3.forward*45);
            local.x=(Mathf.Round(local.x*pixelsPerUnit-(width&1)*.5f)+(width&1)*.5f)/pixelsPerUnit;
            local.y=(Mathf.Round(local.y*pixelsPerUnit-(height&1)*.5f)+(height&1)*.5f)/pixelsPerUnit;
            transform.SetPositionAndRotation(rotation*local,rotation);cameraComponent.orthographicSize=height/(2f*pixelsPerUnit);
            cameraComponent.targetTexture=pixelated?buffer:null;Shader.SetGlobalFloat("_LabContinuous",toon?0:1);
            OutputRect=new Rect((Screen.width-width*scale)*.5f,(Screen.height-height*scale)*.5f,width*scale,height*scale);
        }
        void OnGUI()
        {
            GUI.depth=100;
            if(pixelated&&buffer){GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);GUI.DrawTexture(OutputRect,buffer,ScaleMode.StretchToFill);}
        }
        void Release(){if(buffer){buffer.Release();Destroy(buffer);buffer=null;}}
        void OnDisable(){if(cameraComponent)cameraComponent.targetTexture=null;Release();Shader.SetGlobalFloat("_LabContinuous",0);}
    }
}
