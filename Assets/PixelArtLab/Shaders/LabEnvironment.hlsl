#ifndef PIXEL_LAB_ENVIRONMENT
#define PIXEL_LAB_ENVIRONMENT
// Shader globals written by EnvironmentState (Runtime/Environment). Any URP shader that
// includes this reads the same time of day, weather and clocks as Sprite Toon.
float4 _LabEnvironment;   // x enabled, y daylight, z wetness, w snow cover
float4 _LabWind;          // xy world x/z direction, z strength
float _LabWeatherTime,_LabWaterTime,_LabWaterMotion;
float4 _LabAmbientTint;   // shadow-side light by time of day
float _LabCloudCover;
float _LabContinuous;     // 1 = smooth shading instead of toon bands (ExplorationCamera)
float _LabReflectionPass; // 1 while a planar reflection camera renders
// Moving cloud shadows on the ground: 1 under a cloud, 0 in open sun.
half LabCloudShade(float3 p)
{
    float cloud=sin(p.x*.085+_LabWeatherTime*.04)+sin(p.z*.11-_LabWeatherTime*.03);
    return step(.3,cloud)*_LabCloudCover;
}
// Emission brightness as 0..1; lit parts (lamps, windows) stay clear of rain and snow.
half LabEmissiveMask(half3 emission){return saturate(max(emission.r,max(emission.g,emission.b))*3);}
#endif
