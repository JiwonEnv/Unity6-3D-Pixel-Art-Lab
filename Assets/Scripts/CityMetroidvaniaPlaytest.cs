using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace PixelArtLab
{
    public sealed class CityMetroidvaniaPlaytest : MonoBehaviour
    {
        public Action<bool,string> completed;
        ExplorerMotor player;CityMetroidvaniaWorld world;readonly List<string> report=new List<string>();bool failed;
        void Check(bool success,string message){report.Add((success?"PASS ":"FAIL ")+message);if(!success)failed=true;}
        IEnumerator JumpTo(float x,float y,bool airStep=false,bool dash=false)
        {
            float direction=Mathf.Sign(x-player.transform.position.x),started=Time.time;bool second=false,burst=false;player.externalSprint=true;player.externalMove=new Vector2(direction,0);player.QueueJump();
            while(Time.time-started<3.5f)
            {
                float age=Time.time-started;
                if(airStep&&!second&&age>.45f){player.QueueJump();second=true;}
                if(dash&&!burst&&age>(airStep?.74f:.38f)){player.QueueDash();burst=true;}
                if((x-player.transform.position.x)*direction<.8f)player.externalMove=Vector2.zero;
                if(age>.25f&&player.Grounded&&Mathf.Abs(player.transform.position.y-y)<.25f&&Mathf.Abs(player.transform.position.x-x)<2.2f)break;
                yield return null;
            }
            player.externalMove=Vector2.zero;yield return new WaitForSeconds(.25f);
            Check(player.Grounded&&Mathf.Abs(player.transform.position.y-y)<.25f,$"Reach platform {x:F1}/{y:F1}; actual {player.transform.position}");
        }
        IEnumerator Start()
        {
            player=UnityEngine.Object.FindFirstObjectByType<ExplorerMotor>();world=UnityEngine.Object.FindFirstObjectByType<CityMetroidvaniaWorld>();Check(player&&world&&player.sideScrolling,"Side-scrolling world and motor present");if(!player||!world){Finish();yield break;}
            player.externalControl=true;player.externalMove=Vector2.zero;yield return new WaitForSeconds(.6f);Check(player.Grounded,"Character settles on station deck");
            Vector3 start=player.transform.position;player.externalMove=new Vector2(1,1);yield return new WaitForSeconds(.6f);player.externalMove=Vector2.zero;yield return new WaitForSeconds(.3f);Check(player.transform.position.x-start.x>2&&Mathf.Abs(player.transform.position.z)<.001f,"Horizontal movement ignores depth input and locks Z");
            int jumps=player.JumpCount;player.QueueJump();yield return new WaitForSeconds(.28f);player.QueueJump();yield return new WaitForSeconds(1);Check(player.JumpCount==jumps+1,"Air step locked before module");int dashes=player.DashCount;player.QueueDash();yield return new WaitForSeconds(.2f);Check(player.DashCount==dashes,"Dash locked before module");Check(!world.TryInteract(),"Distant module cannot be activated");
            player.Teleport(new Vector3(-60,8.1f,0));yield return new WaitForSeconds(.5f);
            yield return JumpTo(-57.5f,9.5f);yield return JumpTo(-53.5f,11);yield return JumpTo(-49.5f,12.5f);yield return JumpTo(-43,14);
            Check(world.TryInteract()&&player.dashUnlocked&&!world.dashModule.gameObject.activeSelf,"Reach Westworks on foot and recover dash");
            player.Teleport(new Vector3(-8,2.1f,0));player.externalSprint=true;player.externalMove=Vector2.right;yield return new WaitForSeconds(.12f);player.QueueJump();yield return new WaitForSeconds(.35f);player.QueueDash();float until=Time.time+1.5f;
            while(Time.time<until){if(player.transform.position.x>4)player.externalMove=Vector2.zero;yield return null;}
            Check(player.Grounded&&player.transform.position.x>2&&Mathf.Abs(player.transform.position.y-2)<.25f,"Dash crosses real 8m Drainway gap");
            player.externalMove=Vector2.right;until=Time.time+2;while(player.transform.position.x<9.3f&&Time.time<until)yield return null;player.externalMove=Vector2.zero;yield return new WaitForSeconds(.3f);
            Check(world.TryInteract()&&player.doubleJumpUnlocked&&!world.powerGate.activeSelf,"Recover air step and open return shutter");
            player.Teleport(new Vector3(26,2.1f,0));yield return new WaitForSeconds(.3f);Check(world.TryInteract()&&player.transform.position.y>13&&world.LiftTrips==1,"Air-step power opens lift shortcut");yield return new WaitForSeconds(.4f);
            // Test the main roof crossing, then the gated climb and final signal route.
            player.Teleport(new Vector3(33,14.1f,0));yield return new WaitForSeconds(.4f);yield return JumpTo(44,14,true,true);
            player.Teleport(new Vector3(42.5f,14.1f,0));yield return new WaitForSeconds(.4f);int air=player.AirJumpCount;yield return JumpTo(48,17.5f,true);Check(player.AirJumpCount>air,"Air step reaches the 3.5m Crown ledge");yield return JumpTo(42,20,true);yield return JumpTo(56,22,true,true);
            Check(world.TryInteract()&&world.cityRestored,"Reach Crown Relay through playable platforms and restore signal");
            int respawns=player.RespawnCount;player.Teleport(new Vector3(-2,.5f,0));yield return new WaitForSeconds(.3f);Check(player.RespawnCount>respawns,"Flooded culvert returns to checkpoint");
            Check(player.animator&&player.animator.isInitialized,"Character Animator remains active");player.externalControl=false;Finish();
        }
        void Finish(){string text=(failed?"CITY_PLAYTEST_FAILED":"CITY_PLAYTEST_SUCCESS")+"\n"+string.Join("\n",report)+"\nNO PLAYER BUILD\n";File.WriteAllText("Previews/City-Playtest.txt",text);Debug.Log(text);completed?.Invoke(!failed,text);}
    }
}
