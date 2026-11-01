using UnityEngine;

namespace PixelArtLab
{
    public sealed class DetailLookControls : MonoBehaviour
    {
        public ExplorationCamera follow;
        public ExplorerMotor player;
        public DetailWaterReflection water;
        public int style;
        bool normalMaps=true,reflection=true,shore;
        Vector3 spawn;
        GUIStyle label;
        void Awake(){spawn=player.spawnPoint;Apply();}
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.J)){style=(style+1)%3;Apply();}
            if(Input.GetKeyDown(KeyCode.K)){normalMaps=!normalMaps;Apply();}
            if(Input.GetKeyDown(KeyCode.C)){reflection=!reflection;Apply();}
            if(Input.GetKeyDown(KeyCode.M))
            {
                shore=!shore;follow.lookOffset=shore?new Vector3(-6,0,7):Vector3.zero;
                if(shore)player.Teleport(new Vector3(-17,1,12));else player.Teleport(spawn);
                follow.SnapToPlayer();
            }
        }
        public void Apply()
        {
            Shader.SetGlobalFloat("_LabDetailedToon",style);Shader.SetGlobalFloat("_LabNormalDisabled",normalMaps?0:1);Shader.SetGlobalFloat("_LabReflectionDisabled",reflection?0:1);
            if(water)water.reflections=reflection;
        }
        void OnGUI()
        {
            GUI.depth=-20;if(label==null)label=new GUIStyle(GUI.skin.label){fontSize=12,normal={textColor=new Color(.94f,.91f,.8f)}};
            GUI.color=new Color(.06f,.08f,.1f,.88f);GUI.DrawTexture(new Rect(20,138,480,49),Texture2D.whiteTexture);GUI.color=Color.white;
            string name=style==0?"HYBRID":style==1?"TOON":"CONTINUOUS";
            GUI.Label(new Rect(32,143,460,21),"DETAILED WOODLAND / "+name+" / Powered by Poly Haven",label);
            GUI.Label(new Rect(32,164,460,21),"J  Shading   K  Normal maps   C  Reflections   M  Forest / Shore",label);
        }
        void OnDisable(){Shader.SetGlobalFloat("_LabDetailedToon",0);Shader.SetGlobalFloat("_LabNormalDisabled",0);Shader.SetGlobalFloat("_LabReflectionDisabled",0);}
    }
}
