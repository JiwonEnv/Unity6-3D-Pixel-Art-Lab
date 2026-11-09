#ifndef PIXEL_LAB_HEIGHT_FOG
#define PIXEL_LAB_HEIGHT_FOG
#include "LabEnvironment.hlsl"
// Height fog (Docs/Background-Lighting-Design.md L4), values from TimeOfDayProfile via
// EnvironmentState. An orthographic camera sees every surface from about the same distance,
// so distance fog is a flat tint; height lets haze pool over water and low ground instead.
float4 _LabHeightFog;      // x full-density height, y falloff (m), z density, w drift
float4 _LabHeightFogColor; // rgb haze colour, a how strongly nearby lamps glow in it
float4 _LabHeightFogZone;  // PixelAtmosphere volume: x extra density share, y lift (m); 0 = unchanged
half LabHeightFogAmount(float3 p)
{
    float d=_LabHeightFog.z*(1+_LabHeightFogZone.x)*exp(-max(0,p.y-(_LabHeightFog.x+_LabHeightFogZone.y))/max(.01,_LabHeightFog.y));
    float drift=sin(p.x*.17+_LabWaterTime*.07)*sin(p.z*.13-_LabWaterTime*.05)*.5+.5;
    return saturate(d*lerp(1,.55+.9*drift,_LabHeightFog.w));
}
// lampGlow: summed local light reaching the point, lit inside the haze.
// strength: 1 for scenery, lower for characters so they stay readable (L8).
half3 LabApplyHeightFog(half3 color,float3 p,half3 lampGlow,half strength)
{
    half haze=LabHeightFogAmount(p)*strength;
    return lerp(color,_LabHeightFogColor.rgb+lampGlow*_LabHeightFogColor.a,haze);
}
#endif
