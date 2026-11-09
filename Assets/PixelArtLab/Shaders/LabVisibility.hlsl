#ifndef PIXEL_LAB_VISIBILITY
#define PIXEL_LAB_VISIBILITY
#include "LabEnvironment.hlsl"
// Cuts a dithered hole around the player in surfaces in front of them (PlayerOccluder).
float4 _LabRevealPlayer;
// pixel: SV_POSITION.xy of the fragment. revealAmount: _LabRevealAmount per renderer.
void LabVisibilityClip(float3 world,float2 pixel,float revealAmount)
{
    if(revealAmount>.001&&_LabReflectionPass<.5)
    {
        float3 viewPosition=mul(UNITY_MATRIX_V,float4(world,1)).xyz;
        float3 player=mul(UNITY_MATRIX_V,float4(_LabRevealPlayer.xyz,1)).xyz;
        float radius=length((viewPosition.xy-player.xy)/float2(.95,1.25));
        float hole=(1-smoothstep(.65,1.1,radius))*revealAmount*step(player.z+.12,viewPosition.z);
        float2 cell=floor(pixel);
        float threshold=frac(dot(fmod(cell,4),float2(.25,.5625)))+.03125;
        clip(1-hole*.98-threshold);
    }
}
#endif
