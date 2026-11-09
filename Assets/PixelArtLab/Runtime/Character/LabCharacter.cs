using UnityEngine;

namespace PixelArtLab
{
    // What the reusable features need from a character: footprints and splashes read Grounded
    // and HorizontalSpeed, pixel alignment nudges Visual, camera follow and visibility use the
    // transform. Derive your own controller from this, or add MotionCharacter to any object.
    public abstract class LabCharacter : MonoBehaviour
    {
        public abstract bool Grounded {get;}
        public abstract float HorizontalSpeed {get;}
        // The posed model under the root; null when the root itself is the model.
        public abstract Transform Visual {get;}
        public Transform VisualOrRoot=>Visual?Visual:transform;
    }
}
