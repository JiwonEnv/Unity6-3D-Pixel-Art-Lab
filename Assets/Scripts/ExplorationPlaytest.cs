using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PixelArtLab
{
    // Activated explicitly by editor verification or a standalone smoke-test argument.
    public sealed class ExplorationPlaytest : MonoBehaviour
    {
        public Action<bool,string> completed;
        readonly List<string> checks=new List<string>();
        readonly HashSet<string> clips=new HashSet<string>();
        bool failed;
        ExplorerMotor player;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CommandLineTest()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--amberwood-smoke")>=0)new GameObject("Standalone Gameplay Verification").AddComponent<ExplorationPlaytest>();
        }
        void Check(bool value,string name){checks.Add((value?"PASS ":"FAIL ")+name);if(!value)failed=true;}
        void Update()
        {
            if(player&&player.animator)foreach(var info in player.animator.GetCurrentAnimatorClipInfo(0))if(info.weight>.15f)clips.Add(info.clip.name);
        }
        IEnumerator Start()
        {
            yield return new WaitForSeconds(.7f);
            player=FindFirstObjectByType<ExplorerMotor>();var world=FindFirstObjectByType<ExplorationWorld>();var camera=FindFirstObjectByType<ExplorationCamera>();
            Check(player&&world&&camera,"Player, objectives and follow camera present");
            if(!player||!world||!camera){Finish();yield break;}
            player.externalControl=true;
            Vector3 start=new Vector3(0,1,-13);bool groundHit=Physics.Raycast(new Vector3(start.x,40,start.z),Vector3.down,out var hit,100);start.y=hit.point.y+.1f;
            Check(groundHit,"Terrain raycast finds a collision surface");checks.Add("Start surface: "+(groundHit?hit.collider.name:"none")+" at "+start);
            player.Teleport(start);yield return new WaitForSeconds(.6f);Check(player.Grounded,"Character settles on terrain");
            checks.Add("Settled position: "+player.transform.position+"; deltaTime="+Time.deltaTime.ToString("F4")+"; respawns="+player.RespawnCount);
            Vector3 before=player.transform.position;player.externalMove=Vector2.up;yield return new WaitForSeconds(1);player.externalMove=Vector2.zero;
            float walk=Vector3.Distance(before,player.transform.position);Check(walk>3,"Camera-relative walk moves more than 3m");
            player.Teleport(start);yield return new WaitForSeconds(.4f);before=player.transform.position;player.externalSprint=true;player.externalMove=Vector2.up;
            yield return new WaitForSeconds(1);player.externalMove=Vector2.zero;player.externalSprint=false;float sprint=Vector3.Distance(before,player.transform.position);
            Check(sprint>walk*1.35f,"Sprint is faster than walking");
            player.Teleport(start);yield return new WaitForSeconds(.5f);
            var forward=player.cameraTransform.forward;forward.y=0;forward.Normalize();
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Temporary playtest obstacle";wall.transform.position=start+forward*2.2f+Vector3.up*.25f;wall.transform.localScale=new Vector3(2,.7f,1);wall.transform.rotation=Quaternion.Euler(0,player.cameraTransform.eulerAngles.y,0);
            Physics.SyncTransforms();player.externalMove=Vector2.up;yield return new WaitForSeconds(.8f);player.externalMove=Vector2.zero;
            Check(Vector3.Dot(player.transform.position-start,forward)<1.6f,"Solid obstacle blocks walking");
            int previousFrameRate=Application.targetFrameRate;Application.targetFrameRate=12;
            player.Teleport(start);yield return new WaitForSeconds(.4f);int jumps=player.JumpCount;float baseY=player.transform.position.y,peak=baseY,maxDelta=0;
            player.QueueJump();player.externalMove=Vector2.up;
            float until=Time.time+1.3f;bool buffered=false;
            while(Time.time<until)
            {
                peak=Mathf.Max(peak,player.transform.position.y);
                maxDelta=Mathf.Max(maxDelta,Time.deltaTime);
                if(!buffered&&player.VerticalSpeed<2&&player.VerticalSpeed>0){player.QueueJump();buffered=true;}
                yield return null;
            }
            player.externalMove=Vector2.zero;yield return new WaitForSeconds(.4f);
            Check(peak-baseY>1.2f,"Jump reaches configured height");Check(player.JumpCount==jumps+1,"Airborne jump request does not double-jump");
            Check(Vector3.Dot(player.transform.position-start,forward)>3.5f,"Jump clears the obstacle");Check(player.Grounded,"Character lands after jumping");Destroy(wall);Application.targetFrameRate=previousFrameRate;
            checks.Add("Jump test frame limit: 12fps; maximum frame delta="+maxDelta.ToString("F4"));
            player.Teleport(player.spawnPoint);yield return new WaitForSeconds(.5f);
            int respawns=player.RespawnCount;player.Teleport(new Vector3(-30,0,22));yield return new WaitForSeconds(.3f);Check(player.RespawnCount>respawns,"Deep water returns player to camp");
            for(int i=0;i<world.landmarks.Length;i++){player.Teleport(world.landmarks[i].position+Vector3.up*.2f);yield return new WaitForSeconds(.3f);}
            Check(world.VisitedCount==world.landmarks.Length,"All five regions can be discovered");
            foreach(var collectible in world.crystals){player.Teleport(collectible.position-Vector3.up*.8f);yield return new WaitForSeconds(.3f);}
            Check(world.collected==world.crystals.Length,"All three waystones can be collected");
            player.Teleport(player.spawnPoint);yield return new WaitForSeconds(.7f);camera.SnapToPlayer();
            var viewport=player.cameraTransform.GetComponent<Camera>().WorldToViewportPoint(player.transform.position+Vector3.up*.8f);
            Check(Mathf.Abs(viewport.x-.5f)<.08f&&Mathf.Abs(viewport.y-.5f)<.1f,"Follow camera centers the player");
            Check(player.animator&&player.animator.isInitialized,"Imported character Animator runs");
            Check(clips.Contains("Idle")&&clips.Contains("Walking_A")&&clips.Contains("Running_A")&&clips.Contains("Jump_Start")&&clips.Contains("Jump_Idle"),"Idle/walk/run/jump/fall clips played");
            checks.Add("Observed clips: "+string.Join(", ",clips));checks.Add($"Walk distance {walk:F2}m; sprint {sprint:F2}m; jump rise {peak-baseY:F2}m");
            player.externalControl=false;Finish();
        }
        void Finish()
        {
            string result=(failed?"OPEN_WORLD_PLAYTEST_FAILED":"OPEN_WORLD_PLAYTEST_SUCCESS")+"\n"+string.Join("\n",checks);
            Debug.Log(result);
            if(Application.isEditor){Directory.CreateDirectory("Previews");File.WriteAllText("Previews/Open-World-Playtest.txt",result);}
            if(completed!=null)completed(!failed,result);else if(!Application.isEditor)Application.Quit(failed?1:0);
        }
    }
}
