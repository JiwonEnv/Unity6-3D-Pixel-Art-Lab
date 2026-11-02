using UnityEngine;

namespace PixelArtLab
{
    [DefaultExecutionOrder(100),RequireComponent(typeof(Camera))]
    public sealed class ExplorationCamera : MonoBehaviour
    {
        public ExplorerMotor player;
        public PixelLinework linework;
        public int sourceHeight=360,pixelsPerUnit=25;
        public float yaw=42,pitch=38;
        public bool pixelated=true,toon=true;
        public Vector3 lookOffset;
        Camera cameraComponent;
        RenderTexture buffer;
        Camera displayClearCamera;
        Vector3 focus,velocity;
        bool initialized;
        // One guard texel per side so the subpixel shift never exposes the buffer edge.
        const int Margin=1;
        public Rect OutputRect {get; private set;}
        // Texels the grid-snapped render trails the smooth camera; applied when upscaling.
        public Vector2 SubpixelOffset {get; private set;}
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
            Vector3 target=player.transform.position+Vector3.up*.95f+lookOffset;
            if(!initialized || Vector3.Distance(focus,target)>15){focus=target;initialized=true;velocity=Vector3.zero;}
            focus=Vector3.SmoothDamp(focus,target,ref velocity,.13f);
            ApplyCamera();
        }
        public void SnapToPlayer(){if(player){focus=player.transform.position+Vector3.up*.95f+lookOffset;velocity=Vector3.zero;initialized=true;ApplyCamera();}}
        void ApplyCamera()
        {
            if(!cameraComponent)cameraComponent=GetComponent<Camera>();
            int height=Mathf.Min(sourceHeight,Mathf.Max(1,Screen.height)),scale=Mathf.Max(1,Screen.height/height),width=Mathf.Max(1,Screen.width/scale);
            int bufferWidth=width+Margin*2,bufferHeight=height+Margin*2;
            if(pixelated&&(!buffer||buffer.width!=bufferWidth||buffer.height!=bufferHeight))
            {
                Release();buffer=new RenderTexture(bufferWidth,bufferHeight,24){filterMode=FilterMode.Point,antiAliasing=1,name="Exploration Pixel Buffer"};buffer.Create();
            }
            float alignedPitch=Mathf.Asin(Mathf.Round(Mathf.Sin(pitch*Mathf.Deg2Rad)*pixelsPerUnit)/pixelsPerUnit)*Mathf.Rad2Deg;
            var rotation=Quaternion.Euler(alignedPitch,yaw,0);var local=Quaternion.Inverse(rotation)*(focus-rotation*Vector3.forward*45);
            SubpixelOffset=Vector2.zero;
            if(pixelated)
            {
                // Render from the texel grid so static texels never resample, and hand the
                // remainder to the upscale: the world then scrolls smoothly instead of in
                // uneven one- and two-texel jumps.
                float x=Mathf.Round(local.x*pixelsPerUnit-(width&1)*.5f)+(width&1)*.5f;
                float y=Mathf.Round(local.y*pixelsPerUnit-(height&1)*.5f)+(height&1)*.5f;
                SubpixelOffset=new Vector2(local.x*pixelsPerUnit-x,local.y*pixelsPerUnit-y);
                local.x=x/pixelsPerUnit;local.y=y/pixelsPerUnit;
            }
            transform.SetPositionAndRotation(rotation*local,rotation);cameraComponent.orthographicSize=(pixelated?bufferHeight:height)/(2f*pixelsPerUnit);
            cameraComponent.targetTexture=pixelated?buffer:null;Shader.SetGlobalFloat("_LabContinuous",toon?0:1);
            // Keep a real display camera active while the scene camera renders offscreen.
            // The existing OnGUI draws the integer-scaled pixel buffer over its black clear.
            if(pixelated&&!displayClearCamera)
            {
                var display=new GameObject("Pixel Display Clear Camera"){hideFlags=HideFlags.HideAndDontSave};displayClearCamera=display.AddComponent<Camera>();
                displayClearCamera.cullingMask=0;displayClearCamera.clearFlags=CameraClearFlags.SolidColor;displayClearCamera.backgroundColor=Color.black;displayClearCamera.depth=-100;
            }
            if(displayClearCamera)displayClearCamera.enabled=pixelated;
            OutputRect=new Rect((Screen.width-width*scale)*.5f,(Screen.height-height*scale)*.5f,width*scale,height*scale);
        }
        void OnGUI()
        {
            GUI.depth=100;
            if(pixelated&&buffer)
            {
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.blackTexture);
                // Shift by whole screen pixels only, so every texel keeps the same integer size.
                float texel=OutputRect.height/(buffer.height-Margin*2);
                float x=Mathf.Round((-Margin-SubpixelOffset.x)*texel),y=Mathf.Round((-Margin+SubpixelOffset.y)*texel);
                GUI.BeginGroup(OutputRect);GUI.DrawTexture(new Rect(x,y,buffer.width*texel,buffer.height*texel),buffer,ScaleMode.StretchToFill);GUI.EndGroup();
            }
        }
        void Release(){if(buffer){buffer.Release();Destroy(buffer);buffer=null;}}
        void OnDisable(){if(cameraComponent)cameraComponent.targetTexture=null;Release();if(displayClearCamera)Destroy(displayClearCamera.gameObject);Shader.SetGlobalFloat("_LabContinuous",0);}
    }
}
