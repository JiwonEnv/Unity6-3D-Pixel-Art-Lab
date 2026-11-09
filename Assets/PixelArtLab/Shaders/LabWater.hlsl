#ifndef PIXEL_LAB_WATER
#define PIXEL_LAB_WATER
#include "LabEnvironment.hlsl"
#include "LabPlanarReflection.hlsl"
// Stylized lake surface: scrolling normals, toon glints, rain rings, a stepped shore foam
// line and a fresnel planar reflection. Vertex waves live in LabWind.hlsl (mode 4).
// waterFlow: 0 weather clock, 1 smooth water clock.
float LabWaterClock(float waterFlow){return lerp(_LabWeatherTime,_LabWaterTime,waterFlow)*_LabWaterMotion;}
// Two scrolling world-space samples of the normal map.
half3 LabWaterNormal(TEXTURE2D_PARAM(normalMap,normalSampler),float3 worldPos,float waterFlow)
{
    float t=LabWaterClock(waterFlow);
    float2 uv=worldPos.xz*.18;
    half3 a=UnpackNormal(SAMPLE_TEXTURE2D(normalMap,normalSampler,uv+float2(t*.013,t*.007)));
    half3 b=UnpackNormal(SAMPLE_TEXTURE2D(normalMap,normalSampler,uv*1.83+float2(-t*.011,t*.009)));
    return normalize(half3(a.xy+b.xy,a.z*b.z));
}
// bounds: shore ellipse (center xz, radii). detail: semi-real blend, softens glints.
half3 LabWaterGlint(half3 albedo,float3 worldPos,float waterFlow,float4 bounds,half detail)
{
    float waterTime=LabWaterClock(waterFlow);
    float ripple=sin(worldPos.z*5+sin(worldPos.x*.7+waterTime*.8)*.9-waterTime*1.4);
    float edge=length((worldPos.xz-bounds.xy)/bounds.zw);
    float foam=step(.982+sin(waterTime*1.3+worldPos.z*2)*.006,edge);
    float2 cell=floor(worldPos.xz*.7);
    float age=frac(waterTime*.9+frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453));
    float rainRing=(1-step(.035,abs(length(frac(worldPos.xz*.7)-.5)-age*.65)))*(1-age);
    float glint=step(.985,ripple)*step(.65,sin(worldPos.x*4.8+sin(worldPos.z*1.4)*1.8+waterTime*.6))*.4+rainRing*_LabEnvironment.z*.24;
    return lerp(albedo,half3(.65,.83,.80),saturate(glint*(1-detail*.75)+foam*.65));
}
// reflection: the lake capture sampled at LabWaterReflectionUV. Returns emission to add.
half3 LabWaterReflection(half3 reflection,half3 normalWS,half3 view,half strength)
{
    float fresnel=.12+.75*pow(1-saturate(dot(normalWS,view)),4);
    return reflection*fresnel*strength*(1-_LabReflectionDisabled)*(.35+.65*_LabEnvironment.y);
}
#endif
