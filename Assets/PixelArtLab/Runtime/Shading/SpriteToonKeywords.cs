using UnityEngine;

namespace PixelArtLab
{
    // Sprite Toon compiles each surface feature only into the materials that use it. The
    // keywords follow the material values: the inspector syncs them itself (SpriteToonGUI),
    // code that sets these values on a material calls Sync afterwards.
    public static class SpriteToonKeywords
    {
        public const string AlphaClip="_LAB_ALPHACLIP",NormalMap="_LAB_NORMALMAP",PackedArm="_LAB_PACKED_ARM";
        public const string SemiReal="_LAB_SEMIREAL",Puddles="_LAB_PUDDLES",Water="_LAB_WATER";

        public static bool IsSpriteToon(Material m)=>m&&m.shader&&m.shader.name=="PixelArtLab/Sprite Toon";
        public static void Sync(Material m)
        {
            if(!IsSpriteToon(m))return;
            Set(m,AlphaClip,m.GetFloat("_AlphaClip")>.5f);
            Set(m,NormalMap,m.GetTexture("_NormalMap"));
            Set(m,PackedArm,m.GetFloat("_UsePackedARM")>.5f);
            Set(m,SemiReal,m.GetFloat("_SemiPBR")>.001f);
            Set(m,Puddles,m.GetFloat("_Puddles")>.001f);
            Set(m,Water,m.GetFloat("_WaterSurface")>.5f);
        }
        static void Set(Material m,string keyword,bool on){if(on)m.EnableKeyword(keyword);else m.DisableKeyword(keyword);}
    }
}
