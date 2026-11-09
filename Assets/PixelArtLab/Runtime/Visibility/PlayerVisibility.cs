using System.Collections.Generic;
using UnityEngine;
namespace PixelArtLab
{
    // Collects the wall stencil and occluded-scan draws; PixelArtLabRendererFeature
    // replays them after transparents on the camera that carries this component.
    [ExecuteAlways,DefaultExecutionOrder(115),RequireComponent(typeof(Camera))]
    public sealed class PlayerVisibility : MonoBehaviour
    {
        public struct Draw {public Renderer renderer;public Material material;public int submesh,pass;}
        public LabCharacter player;
        public PlayerOccluder[] occluders;
        [Range(0,1)]public float rimStrength=.32f,bounceStrength=.12f;
        public bool rimEnabled=true,naturalReveal=true,buildingScan=true;
        public int FadingObjects {get;private set;}
        public int ScannedBuildings {get;private set;}
        internal readonly List<Draw> draws=new List<Draw>();
        Camera cameraComponent;Material scanMaterial;
        Renderer[] character;float scanAmount;
        void OnEnable()
        {
            cameraComponent=GetComponent<Camera>();
            scanMaterial=new Material(Shader.Find("PixelArtLab/Sprite Toon")){hideFlags=HideFlags.HideAndDontSave};
            Refresh(0);
        }
        void LateUpdate()
        {
            if(!Application.isPlaying){Refresh(0);return;}
            if(Input.GetKeyDown(KeyCode.F5))rimEnabled=!rimEnabled;
            if(Input.GetKeyDown(KeyCode.F6))naturalReveal=!naturalReveal;
            if(Input.GetKeyDown(KeyCode.F7))buildingScan=!buildingScan;
            Refresh(Time.deltaTime);
        }
        public void Refresh(float deltaTime)
        {
            if(!player||!scanMaterial)return;
            if(character==null)character=player.GetComponentsInChildren<Renderer>(true);
            var block=new MaterialPropertyBlock();foreach(var r in character)if(r){r.GetPropertyBlock(block);block.SetFloat("_LabCharacter",1);block.SetFloat("_CharacterRim",rimEnabled?rimStrength:0);block.SetFloat("_CharacterBounce",rimEnabled?bounceStrength:0);r.SetPropertyBlock(block);}
            Shader.SetGlobalVector("_LabRevealPlayer",player.transform.position+Vector3.up*.95f);
            FadingObjects=0;ScannedBuildings=0;draws.Clear();
            if(occluders!=null)foreach(var target in occluders)if(target&&target.isActiveAndEnabled)
            {
                if(target.mode==PlayerOcclusionMode.Ignore){if(target.visibility>0)target.Apply(0);continue;}
                int blocked=0;for(int h=0;h<3;h++)if(target.Blocks(player.transform.position+Vector3.up*(.85f+h*.4f),cameraComponent))blocked++;
                bool active=blocked>=2&&((target.mode==PlayerOcclusionMode.NaturalFade&&naturalReveal)||(target.mode==PlayerOcclusionMode.BuildingScan&&buildingScan));
                float fade=Mathf.MoveTowards(target.visibility,active?1:0,deltaTime*5);if(fade!=target.visibility)target.Apply(fade);
                if(target.visibility<.001f)continue;
                if(target.mode==PlayerOcclusionMode.NaturalFade){FadingObjects++;continue;}
                ScannedBuildings++;
                foreach(var renderer in target.renderers)if(renderer&&renderer.enabled&&renderer.gameObject.activeInHierarchy)
                {
                    var materials=renderer.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]){int pass=materials[i].FindPass("OccluderStencil");if(pass>=0)draws.Add(new Draw{renderer=renderer,material=materials[i],submesh=i,pass=pass});}
                }
            }
            scanAmount=Mathf.MoveTowards(scanAmount,ScannedBuildings>0?1:0,deltaTime*7);
            Shader.SetGlobalFloat("_LabScanAmount",buildingScan?scanAmount:0);Shader.SetGlobalFloat("_LabScanTime",Time.time);
            int scanPass=scanMaterial.FindPass("CharacterScan");if(scanPass>=0&&scanAmount>.001f)foreach(var renderer in character)if(renderer&&renderer.enabled&&renderer.gameObject.activeInHierarchy)for(int i=0;i<renderer.sharedMaterials.Length;i++)draws.Add(new Draw{renderer=renderer,material=scanMaterial,submesh=i,pass=scanPass});
        }
        void OnDisable()
        {
            draws.Clear();
            if(occluders!=null)foreach(var target in occluders)if(target)target.Apply(0);
            if(character!=null){var block=new MaterialPropertyBlock();foreach(var r in character)if(r){r.GetPropertyBlock(block);block.SetFloat("_LabCharacter",0);r.SetPropertyBlock(block);}}
            Shader.SetGlobalFloat("_LabScanAmount",0);
            if(scanMaterial){if(Application.isPlaying)Destroy(scanMaterial);else DestroyImmediate(scanMaterial);}
        }
    }
}
