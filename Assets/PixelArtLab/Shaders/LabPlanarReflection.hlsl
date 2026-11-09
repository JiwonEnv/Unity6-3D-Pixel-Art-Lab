#ifndef PIXEL_LAB_PLANAR_REFLECTION
#define PIXEL_LAB_PLANAR_REFLECTION
// Globals written by DetailWaterReflection: one capture for the lake, one for ground puddles.
// The reflection textures themselves are material properties of the including shader.
float4x4 _PuddleProjection;float _PuddleCaptureHeight;
float4x4 _DetailProjection;float _DetailCaptureHeight;
float _LabReflectionDisabled; // comparison switch (DetailLookControls)
// Puddles keep the receiver's real height: flattening y to the capture plane compensated
// only half the offset and detached distant reflections.
float2 LabPuddleReflectionUV(float3 world,half3 normalTS)
{
    float4 projected=mul(_PuddleProjection,float4(world,1));
    return projected.xy/projected.w*.5+.5+normalTS.xy*.012;
}
// The lake is one flat plane; ripples bend the lookup.
float2 LabWaterReflectionUV(float3 world,half3 normalTS)
{
    float4 projected=mul(_DetailProjection,float4(world.x,_DetailCaptureHeight,world.z,1));
    float2 uv=projected.xy/projected.w*.5+.5;
    return uv+normalTS.xy*.018;
}
#endif
