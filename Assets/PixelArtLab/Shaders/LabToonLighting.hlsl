#ifndef PIXEL_LAB_TOON_LIGHTING
#define PIXEL_LAB_TOON_LIGHTING
// Toon light model for URP forward passes: four stepped bands per light, a hue-shifted shadow
// side, optional semi-real (GGX) highlights, and the Built-in style point light falloff.
// Include after URP's Lighting.hlsl.
#include "LabEnvironment.hlsl"
// ToonRampStyle (SurfaceStyleSet)
float4 _LabToonBands;  // xyz energy where each band starts
float4 _LabToonLevels; // brightness of the four bands
float4 _LabToonLitTint;// hue of the lit side
float4 _LabToonParams; // x light color mix, y direct strength, z cloud dimming, w leaf transmission
struct LabToonSurface
{
    half3 albedo;
    half3 normalWS;
    half rough;
    half gloss;      // cloud shade (LabCloudShade): dims direct sun
    half3 shadeColor;// shadow hue
    half metallic;
    half twoSided;   // leaf transmission 0..1: light through thin faces
    half detail;     // 0 toon bands .. 1 semi-real (smooth diffuse + GGX specular)
};
// Four light levels: the pixel-art band look.
half LabToonBand(half e){return e<_LabToonBands.x?_LabToonLevels.x:e<_LabToonBands.y?_LabToonLevels.y:e<_LabToonBands.z?_LabToonLevels.z:_LabToonLevels.w;}
half LabToonNdotL(LabToonSurface s,half3 lightDir)
{
    return lerp(saturate(dot(s.normalWS,lightDir)),abs(dot(s.normalWS,lightDir)),s.twoSided);
}
half3 LabSpecular(LabToonSurface s,half3 lightDir,half3 viewDir,half atten,half3 lightColor)
{
    half3 h=normalize(lightDir+viewDir);half nl=saturate(dot(s.normalWS,lightDir));
    half nv=max(.05,abs(dot(s.normalWS,viewDir))),nh=saturate(dot(s.normalWS,h)),vh=saturate(dot(viewDir,h));
    half rough=max(.08,s.rough),alpha=rough*rough,a2=alpha*alpha;
    half denom=nh*nh*(a2-1)+1;half d=a2/(PI*denom*denom+.0001);
    half k=(rough+1)*(rough+1)/8;half g=(nl/(nl*(1-k)+k+.001))*(nv/(nv*(1-k)+k));
    half3 f0=lerp(half3(.04,.04,.04),s.albedo,s.metallic);half3 f=f0+(1-f0)*pow(1-vh,5);
    return min(4,d*g*f/(4*nv*max(.03,nl)))*nl*atten*lightColor;
}
// Sun or moon. Shadow is ambient; visible direct light only ADDS energy. Interpolating
// toward a dim sunset/moon color could otherwise brighten the shadow side.
half3 LabSunLight(LabToonSurface s,half3 lightDir,half3 viewDir,half atten,half3 lightColor)
{
    half n=LabToonNdotL(s,lightDir);
    half energy=n*atten*(1-s.gloss*_LabToonParams.z);
    half band=lerp(LabToonBand(energy),energy,max(_LabContinuous,s.detail));
    half3 ramp=lerp(s.shadeColor,_LabToonLitTint.rgb,band);
    half3 oldLight=ramp*lerp(half3(.9,.95,1.0),lightColor,_LabToonParams.x);
    half3 ambient=s.shadeColor*_LabAmbientTint.rgb; // strength by time of day: TimeOfDayProfile
    half3 timeLight=ambient+lightColor*_LabToonLitTint.rgb*band*_LabToonParams.y;
    half3 transmitted=s.albedo*lightColor*saturate(-dot(s.normalWS,lightDir))*s.twoSided*_LabToonParams.w*atten;
    return s.albedo*lerp(oldLight,timeLight,_LabEnvironment.x)+LabSpecular(s,lightDir,viewDir,atten,lightColor)*s.detail+transmitted;
}
// Lamps and torches. Toon the surface response, not the distance/shadow attenuation:
// quantizing n * atten erased the weak light around a torch.
half3 LabLocalLight(LabToonSurface s,half3 lightDir,half3 viewDir,half atten,half3 lightColor,half strength)
{
    half n=LabToonNdotL(s,lightDir);
    half diffuse=lerp(LabToonBand(n),n,max(_LabContinuous,s.detail));
    return s.albedo*lightColor*diffuse*atten*strength+LabSpecular(s,lightDir,viewDir,atten,lightColor)*s.detail;
}
// Built-in point-light falloff (1/(1+25 d^2/r^2), fading out from 0.8 r) so torch pools keep
// their authored size instead of URP's inverse-square curve. i: index inside LIGHT_LOOP_BEGIN.
half LabLegacyAttenuation(uint i,float3 positionWS)
{
    #if USE_CLUSTER_LIGHT_LOOP
    int index=i;
    #else
    int index=GetPerObjectLightIndex(i);
    #endif
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    float4 position=_AdditionalLightsBuffer[index].position;float4 attenuation=_AdditionalLightsBuffer[index].attenuation;
    #else
    float4 position=_AdditionalLightsPosition[index];float4 attenuation=_AdditionalLightsAttenuation[index];
    #endif
    if(position.w<.5)return 1;
    float3 offset=position.xyz-positionWS;float t=dot(offset,offset)*attenuation.x;
    return saturate((1-t)/.36)/(1+25*t);
}
#endif
