Shader "Hidden/PixelArtLab/Volumetrics"
{
    // Light shafts and lamp glow in the air (Docs/Background-Lighting-Design.md L5).
    // Pass 0 raymarches the haze at pixel-buffer resolution from a fixed start (no random
    // jitter: it left stipple at band edges). Pass 1 softens it, quantizes it into hard-edged
    // light bands and adds it to the scene. No frame history, so nothing smears.
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        float4 _LabHeightFog;            // shared with Sprite Toon: x full-density height, y falloff, z density
        float4 _LabVolumetric;           // x air density floor, y medium top height, z max march length, w steps
        float4 _LabVolumetricStrength;   // x sun/moon, y lamps, z quantize step, w unused
        ENDHLSL
        Pass
        {
            Name "March"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #define MAX_VOLUME_LIGHTS 16
            float4 _LabVolumeLights[MAX_VOLUME_LIGHTS];      // xyz position, w 1/range^2
            float4 _LabVolumeLightColors[MAX_VOLUME_LIGHTS]; // linear colour x intensity
            int _LabVolumeLightCount;
            float Medium(float y)
            {
                return _LabVolumetric.x+_LabHeightFog.z*exp(-max(0,y-_LabHeightFog.x)/max(.01,_LabHeightFog.y));
            }
            float4 frag(Varyings input):SV_Target
            {
                float2 uv=input.positionCS.xy/_ScaledScreenParams.xy;
                float3 surface=ComputeWorldSpacePosition(uv,SampleSceneDepth(uv),UNITY_MATRIX_I_VP);
                float3 forward=normalize(mul((float3x3)UNITY_MATRIX_I_V,float3(0,0,-1)));
                if(forward.y>-.01)return 0;
                // March back from the surface toward the camera through the low medium only.
                float marchLength=min(max(0,(_LabVolumetric.y-surface.y)/-forward.y),_LabVolumetric.z);
                int steps=max(1,(int)_LabVolumetric.w);float dt=marchLength/steps;
                float t=.5*dt;
                float3 sum=0;
                for(int i=0;i<steps;i++,t+=dt)
                {
                    float3 p=surface-forward*t;float density=Medium(p.y);
                    float shadow=MainLightRealtimeShadow(TransformWorldToShadowCoord(p));
                    float3 light=_MainLightColor.rgb*shadow*_LabVolumetricStrength.x;
                    for(int l=0;l<_LabVolumeLightCount;l++)
                    {
                        float3 offset=_LabVolumeLights[l].xyz-p;float r=dot(offset,offset)*_LabVolumeLights[l].w;
                        // Same falloff as the torches in Sprite Toon.
                        light+=_LabVolumeLightColors[l].rgb*saturate((1-r)/.36)/(1+25*r)*_LabVolumetricStrength.y;
                    }
                    sum+=light*density*dt;
                }
                return float4(sum,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Composite"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            TEXTURE2D(_LabVolumetricTexture);
            float4 _LabVolumetricTexel;
            float4 frag(Varyings input):SV_Target
            {
                float2 uv=input.texcoord;
                float4 color=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv);
                // 13-tap blur (radius 2) smooths the march slices before banding.
                float2 o=_LabVolumetricTexel.xy;
                #define VOL(dx,dy) SAMPLE_TEXTURE2D(_LabVolumetricTexture,sampler_PointClamp,uv+float2(dx,dy)*o).rgb
                float3 v=VOL(0,0)*.2
                    +(VOL(1,0)+VOL(-1,0)+VOL(0,1)+VOL(0,-1))*.1
                    +(VOL(1,1)+VOL(-1,1)+VOL(1,-1)+VOL(-1,-1))*.05
                    +(VOL(2,0)+VOL(-2,0)+VOL(0,2)+VOL(0,-2))*.05;
                // Quantize brightness into flat, hard-edged bands like the toon shading, keeping
                // the hue. (Dithering between bands put a screen-door over faint moonlit air.)
                float level=max(v.r,max(v.g,v.b));float stepSize=max(.001,_LabVolumetricStrength.z);
                float banded=floor(level/stepSize+.35)*stepSize;
                color.rgb+=level>0?v*(banded/level):0;
                return color;
            }
            ENDHLSL
        }
    }
}
