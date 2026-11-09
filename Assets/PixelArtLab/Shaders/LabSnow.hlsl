#ifndef PIXEL_LAB_SNOW
#define PIXEL_LAB_SNOW
#include "LabEnvironment.hlsl"
// SnowStyle (SurfaceStyleSet)
float4 _LabSnowColor;
float4 _LabSnowShape; // x slope start, y slope sharpness, z steps, w step bias
// Snow settling on upward faces, driven by EnvironmentState.snowCover (_LabEnvironment.w).
// Four stepped levels keep the pixel look; no extra textures or meshes are needed.
// normalWS: the surface normal including its normal map. emissive: LabEmissiveMask.
half LabSnowAmount(half3 normalWS)
{
    float top=saturate((normalWS.y-_LabSnowShape.x)*_LabSnowShape.y);
    return floor(saturate(_LabEnvironment.w*top)*_LabSnowShape.z+_LabSnowShape.w)/_LabSnowShape.z;
}
half3 LabSnowAlbedo(half3 albedo,half3 normalWS,half emissive)
{
    return lerp(albedo,_LabSnowColor.rgb,LabSnowAmount(normalWS)*(1-emissive));
}
#endif
