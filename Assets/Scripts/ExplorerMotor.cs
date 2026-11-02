using UnityEngine;

namespace PixelArtLab
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ExplorerMotor : MonoBehaviour
    {
        public Transform cameraTransform, visual;
        public Animator animator;
        public float walkSpeed=4.2f, sprintSpeed=7.2f, jumpHeight=1.65f, gravity=-26f;
        public Vector3 spawnPoint;
        public bool externalControl;
        public Vector2 externalMove;
        public bool externalSprint;
        public LayerMask groundLayers=~(1<<8);
        public bool steppedFacing=true;
        // Sprite-sheet cadence while steppedFacing is on: the rig is posed only this many
        // times a second, like hand-placed stepped keys (Never's End GDC 2026, slides 92-93).
        [Min(0)]public float animationFrameRate=12;
        float animationClock;
        CharacterController controller;
        Vector3 horizontal;
        float vertical, lastGrounded=-10, lastJump=-10;
        bool requestedJump;
        public bool Grounded {get; private set;}
        public float VerticalSpeed=>vertical;
        public float HorizontalSpeed=>horizontal.magnitude;
        public int JumpCount {get; private set;}
        public int RespawnCount {get; private set;}
        public string Region {get; set;}="Amberwood Camp";
        void Awake(){controller=GetComponent<CharacterController>();gameObject.layer=8;if(spawnPoint==Vector3.zero)spawnPoint=transform.position;}
        bool ProbeGround()
        {
            return Physics.SphereCast(transform.position+Vector3.up*.38f,controller.radius*.8f,Vector3.down,out var hit,.25f,groundLayers,QueryTriggerInteraction.Ignore)
                && Vector3.Angle(hit.normal,Vector3.up)<=controller.slopeLimit;
        }
        public void QueueJump()=>requestedJump=true;
        void Update()
        {
            if(Time.timeScale==0)return;
            Vector2 move=externalControl?externalMove:new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
            bool sprint=externalControl?externalSprint:Input.GetKey(KeyCode.LeftShift);
            if(!externalControl&&Input.GetKeyDown(KeyCode.Space))QueueJump();
            if(!externalControl&&Input.GetKeyDown(KeyCode.R))Respawn();
            if(requestedJump){lastJump=Time.time;requestedJump=false;}
            Vector3 forward=cameraTransform?cameraTransform.forward:Vector3.forward;forward.y=0;forward.Normalize();
            Vector3 right=cameraTransform?cameraTransform.right:Vector3.right;right.y=0;right.Normalize();
            Vector3 direction=Vector3.ClampMagnitude(forward*move.y+right*move.x,1);
            // Keep collision sweeps and gravity stable even in a slow/background frame.
            float remaining=Time.deltaTime;
            while(remaining>.00001f)
            {
                float step=Mathf.Min(remaining,1f/60);remaining-=step;
                Grounded=vertical<=0&&(controller.isGrounded||ProbeGround());
                if(Grounded){lastGrounded=Time.time;if(vertical<0)vertical=-2;}
                if(Time.time-lastJump<.14f&&Time.time-lastGrounded<.12f)
                {
                    vertical=Mathf.Sqrt(jumpHeight*-2*gravity);lastJump=-10;lastGrounded=-10;JumpCount++;
                }
                horizontal=Vector3.MoveTowards(horizontal,direction*(sprint?sprintSpeed:walkSpeed),(Grounded?30:12)*step);
                float dy=vertical*step+gravity*step*step*.5f;vertical=Mathf.Max(vertical+gravity*step,-38);
                var flags=controller.Move(horizontal*step+Vector3.up*dy);
                if((flags&CollisionFlags.Above)!=0&&vertical>0)vertical=0;
                Grounded=(flags&CollisionFlags.Below)!=0;
            }
            Grounded=vertical<=0&&(Grounded||ProbeGround());
            if(visual&&direction.sqrMagnitude>.01f)
            {
                if(steppedFacing){float angle=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;visual.rotation=Quaternion.Euler(0,Mathf.Round(angle/22.5f)*22.5f,0);}
                else visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(direction),Time.deltaTime*14);
            }
            if(animator)
            {
                animator.SetFloat("Speed",horizontal.magnitude);animator.SetBool("Grounded",Grounded);animator.SetFloat("VerticalSpeed",vertical);
                bool stepped=steppedFacing&&animationFrameRate>0;animator.enabled=!stepped;
                if(stepped)
                {
                    animationClock+=Time.deltaTime;float frame=1f/animationFrameRate;
                    if(animationClock>=frame){int frames=Mathf.FloorToInt(animationClock/frame);animator.Update(frames*frame);animationClock-=frames*frame;}
                }
            }
            if(transform.position.y<-8 || Mathf.Abs(transform.position.x)>85 || Mathf.Abs(transform.position.z)>85)Respawn();
        }
        public void Respawn()
        {
            controller.enabled=false;transform.position=spawnPoint;controller.enabled=true;
            horizontal=Vector3.zero;vertical=0;lastGrounded=-10;lastJump=-10;RespawnCount++;
        }
        public void Teleport(Vector3 position)
        {
            controller.enabled=false;transform.position=position;controller.enabled=true;horizontal=Vector3.zero;vertical=0;
        }
    }
}
