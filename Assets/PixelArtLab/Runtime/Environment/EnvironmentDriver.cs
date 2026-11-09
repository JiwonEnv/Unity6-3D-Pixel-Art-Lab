using UnityEngine;

namespace PixelArtLab
{
    // Base for components that turn EnvironmentState into scene changes (sun, sky, lights,
    // grading). Each driver is optional, so a project adds only what it needs.
    [ExecuteAlways]
    public abstract class EnvironmentDriver : MonoBehaviour
    {
        [Tooltip("Leave empty to use the EnvironmentState on this object, or the active one.")]
        public EnvironmentState state;
        EnvironmentState subscribed;
        protected EnvironmentState State
        {
            get
            {
                if(!state)state=GetComponent<EnvironmentState>();
                return state?state:EnvironmentState.Active;
            }
        }
        protected virtual void OnEnable()
        {
            subscribed=State;
            if(subscribed){subscribed.Applied+=OnApplied;OnApplied(subscribed);}
        }
        protected virtual void OnDisable(){if(subscribed)subscribed.Applied-=OnApplied;subscribed=null;}
        protected abstract void OnApplied(EnvironmentState environment);
    }
}
