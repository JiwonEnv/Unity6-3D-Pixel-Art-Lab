#ifndef PIXEL_LAB_CHARACTER_LIGHT
#define PIXEL_LAB_CHARACTER_LIGHT
#include "LabEnvironment.hlsl"
// Keeps a character readable against detailed backgrounds: one flat rim band facing the
// sun and a soft bounce fill from below. Returns emission to add.
// rim, bounce: 0..1 strengths (PlayerVisibility sets them per renderer).
half3 LabCharacterLight(half3 albedo,half3 normalWS,half3 view,half3 sunDir,half rim,half bounce)
{
    // One band only: four quantized levels lit scattered single texels on small parts.
    float edge=step(.62,1-saturate(dot(normalWS,view)))*.6;
    float facing=lerp(.4,1,saturate(dot(normalWS,sunDir)*.5+.5));
    half3 rimColor=lerp(half3(.42,.68,1),half3(1,.86,.62),_LabEnvironment.y*.6);
    half3 bounceColor=lerp(half3(.15,.25,.40),half3(.32,.37,.24),_LabEnvironment.y);
    return rimColor*edge*rim*facing+albedo*bounceColor*bounce*(.45+.55*(1-saturate(normalWS.y)));
}
#endif
