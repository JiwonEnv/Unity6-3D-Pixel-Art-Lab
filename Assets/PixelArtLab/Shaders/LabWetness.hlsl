#ifndef PIXEL_LAB_WETNESS
#define PIXEL_LAB_WETNESS
#include "LabEnvironment.hlsl"
// WetnessStyle (SurfaceStyleSet): x darkening, y sheen size, z sheen strength
float4 _LabWetShape;
// Rain wetness on every exposed surface, driven by EnvironmentState.wetness (_LabEnvironment.z):
// a darker albedo while wet and one hard highlight that only exists in the rain.
// emissive: LabEmissiveMask of the surface emission; lit parts do not darken.
half3 LabWetAlbedo(half3 albedo,half emissive){return albedo*(1-_LabEnvironment.z*_LabWetShape.x*(1-emissive));}
// ndotl: the surface's light term (LabToonNdotL), atten: light attenuation. Multiply by the light color.
half LabWetSheen(half3 normalWS,half3 lightDir,half3 viewDir,half ndotl,half atten)
{
    return step(_LabWetShape.y,dot(normalize(lightDir+viewDir),normalWS))*ndotl*atten*_LabEnvironment.z*_LabWetShape.z;
}
#endif
