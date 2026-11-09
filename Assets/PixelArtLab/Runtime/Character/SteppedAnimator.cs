using UnityEngine;

namespace PixelArtLab
{
    // Poses an Animator only frameRate times a second, like hand-placed stepped keys in a
    // sprite sheet (Never's End GDC 2026, slides 92-93). Works with any Animator; disable the
    // component to go back to smooth animation. Runs after the controller sets its parameters.
    [DefaultExecutionOrder(100)]
    public sealed class SteppedAnimator : MonoBehaviour
    {
        public Animator animator;
        [Min(1)]public float frameRate=12;
        float clock;
        void OnEnable(){if(!animator)animator=GetComponentInChildren<Animator>();if(animator)animator.enabled=false;clock=0;}
        void OnDisable(){if(animator)animator.enabled=true;}
        void Update()
        {
            if(!animator)return;
            clock+=Time.deltaTime;float frame=1f/frameRate;
            if(clock>=frame){int frames=Mathf.FloorToInt(clock/frame);animator.Update(frames*frame);clock-=frames*frame;}
        }
    }
}
