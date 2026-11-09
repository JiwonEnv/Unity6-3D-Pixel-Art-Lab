using UnityEngine;

namespace PixelArtLab
{
    // LabCharacter for any existing controller (CharacterController, Rigidbody, NavMeshAgent,
    // animation-driven): speed comes from how the transform moves, ground from a short ray.
    [DefaultExecutionOrder(50)]
    public sealed class MotionCharacter : LabCharacter
    {
        public Transform visual;
        [Tooltip("Ray length below the pivot that still counts as standing.")]
        [Min(0)]public float groundDistance=.2f;
        Vector3 previous;float speed;bool grounded,initialized;
        public override bool Grounded=>grounded;
        public override float HorizontalSpeed=>speed;
        public override Transform Visual=>visual;
        void OnEnable(){initialized=false;}
        void Update()
        {
            Vector3 current=transform.position,delta=current-previous;delta.y=0;
            speed=initialized&&Time.deltaTime>0?delta.magnitude/Time.deltaTime:0;previous=current;initialized=true;
            grounded=Physics.Raycast(current+Vector3.up*.3f,Vector3.down,.3f+groundDistance,LabLayers.GroundProbeMask,QueryTriggerInteraction.Ignore);
        }
    }
}
