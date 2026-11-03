using UnityEngine;
using UnityEngine.Rendering;

namespace PixelArtLab
{
    // Blends the TimeOfDayProfile's grading VolumeProfiles by the sun: day, dusk vermilion ->
    // pink -> violet, night. The volumes are created at runtime and never saved in the scene,
    // so changing the look means editing the profile asset only.
    public sealed class TimeOfDayGrading : EnvironmentDriver
    {
        public float priority=1;
        Volume day,vermilion,pink,violet,night;
        protected override void OnEnable()
        {
            day=Create("Grade - Day");vermilion=Create("Grade - Dusk Vermilion");pink=Create("Grade - Dusk Pink");violet=Create("Grade - Dusk Violet");night=Create("Grade - Night");
            base.OnEnable();
        }
        Volume Create(string name)
        {
            var g=new GameObject(name){hideFlags=HideFlags.HideAndDontSave};g.transform.SetParent(transform,false);
            var v=g.AddComponent<Volume>();v.isGlobal=true;v.priority=priority;v.weight=0;return v;
        }
        protected override void OnApplied(EnvironmentState s)
        {
            var t=s.TimeOfDay;float on=s.colorGrading?1:0;
            float vermilionWeight=s.Dusk*(1-Mathf.Clamp01(s.DuskProgress*2)),violetWeight=s.Dusk*Mathf.Clamp01(s.DuskProgress*2-1),pinkWeight=s.Dusk-vermilionWeight-violetWeight;
            Set(day,t.dayGrade,on*s.Daylight*(1-s.Dusk));
            Set(vermilion,t.duskVermilionGrade,on*vermilionWeight);
            Set(pink,t.duskPinkGrade,on*pinkWeight);
            Set(violet,t.duskVioletGrade,on*violetWeight);
            Set(night,t.nightGrade,on*(1-s.Daylight)*(1-s.Dusk));
        }
        static void Set(Volume volume,VolumeProfile profile,float weight){if(!volume)return;volume.sharedProfile=profile;volume.weight=profile?weight:0;}
        protected override void OnDisable()
        {
            base.OnDisable();
            foreach(var v in new[]{day,vermilion,pink,violet,night})if(v){if(Application.isPlaying)Destroy(v.gameObject);else DestroyImmediate(v.gameObject);}
        }
    }
}
