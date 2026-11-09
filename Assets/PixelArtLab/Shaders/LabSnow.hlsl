#ifndef PIXEL_LAB_SNOW
#define PIXEL_LAB_SNOW
#include "LabEnvironment.hlsl"
// Snow settling on upward faces, driven by EnvironmentState.snowCover (_LabEnvironment.w).
// Four stepped levels keep the pixel look; no extra textures or meshes are needed.
// normalWS: the surface normal including its normal map. emissive: LabEmissiveMask.
half LabSnowAmount(half3 normalWS)
{
    float top=saturate((normalWS.y-.20)*1.6);
    return floor(saturate(_LabEnvironment.w*top)*4+.2)/4;
}
half3 LabSnowAlbedo(half3 albedo,half3 normalWS,half emissive)
{
    return lerp(albedo,half3(.86,.91,.95),LabSnowAmount(normalWS)*(1-emissive));
}
#endif
