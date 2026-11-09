#ifndef PIXEL_LAB_CHARACTER_LIGHT
#define PIXEL_LAB_CHARACTER_LIGHT
#include "LabEnvironment.hlsl"
// CharacterLightStyle (SurfaceStyleSet)
float4 _LabRimDay,_LabRimNight,_LabBounceDay,_LabBounceNight;
float4 _LabRimShape; // x edge threshold, y rim level, z day mix
// Keeps a character readable against detailed backgrounds: one flat rim band facing the
// sun and a soft bounce fill from below. Returns emission to add.
// rim, bounce: 0..1 strengths (PlayerVisibility sets them per renderer).
half3 LabCharacterLight(half3 albedo,half3 normalWS,half3 view,half3 sunDir,half rim,half bounce)
{
    // One band only: four quantized levels lit scattered single texels on small parts.
    float edge=step(_LabRimShape.x,1-saturate(dot(normalWS,view)))*_LabRimShape.y;
    float facing=lerp(.4,1,saturate(dot(normalWS,sunDir)*.5+.5));
    half3 rimColor=lerp(_LabRimNight.rgb,_LabRimDay.rgb,_LabEnvironment.y*_LabRimShape.z);
    half3 bounceColor=lerp(_LabBounceNight.rgb,_LabBounceDay.rgb,_LabEnvironment.y);
    return rimColor*edge*rim*facing+albedo*bounceColor*bounce*(.45+.55*(1-saturate(normalWS.y)));
}
#endif
