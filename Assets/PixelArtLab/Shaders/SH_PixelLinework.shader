Shader "Hidden/PixelArtLab/Linework"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            Name "Linework"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            TEXTURE2D(_ObjectIdentityTexture);
            TEXTURE2D(_LabSceneDepthNormals);
            float4 _LabTexel; // 1/width, 1/height of the source pixel buffer
            float _LabFarPlane;
            float _DepthThreshold,_NormalThreshold,_LineStrength;
            float _UseObjectIds,_DebugObjectIds;
            void DecodeDepthNormal(float4 enc,out float depth,out float3 normal)
            {
                depth=dot(enc.zw,float2(1,1/255.0));
                float3 nn=enc.xyz*float3(3.5554,3.5554,0)+float3(-1.7777,-1.7777,1);
                float g=2/dot(nn.xyz,nn.xyz);normal=float3(g*nn.xy,g-1);
            }
            float4 Geometry(float2 uv){return SAMPLE_TEXTURE2D(_LabSceneDepthNormals,sampler_PointClamp,uv);}
            float4 Identity(float2 uv){return SAMPLE_TEXTURE2D(_ObjectIdentityTexture,sampler_PointClamp,uv);}
            float4 frag(Varyings input):SV_Target
            {
                float2 uv=input.texcoord;
                float4 color=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_PointClamp,uv);
                float depth;float3 normal;
                DecodeDepthNormal(Geometry(uv),depth,normal);
                float4 objectData=Identity(uv);
                float id=round(objectData.r*255)+round(objectData.g*255)*256;
                if(_DebugObjectIds>.5)
                {
                    float3 hue=abs(frac(frac(id*.618034)+float3(0,2.0/3,1.0/3))*6-3);
                    return float4(id>.5?lerp(float3(.20,.20,.20),saturate(hue-1),.8):float3(.04,.04,.06),1);
                }
                float outline=0,ridge=0,boundary=0;
                const float2 offsets[4]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1)};
                for(int k=0;k<4;k++)
                {
                    float2 sampleUv=uv+offsets[k]*_LabTexel.xy;
                    float d;float3 n;
                    DecodeDepthNormal(Geometry(sampleUv),d,n);
                    float4 other=Identity(sampleUv);
                    float otherId=round(other.r*255)+round(other.g*255)*256;
                    float separate=step(.5,abs(id-otherId));
                    // The ID boundary belongs to the closer object. Equal-depth ties go to lower ID.
                    float front=(abs(d-depth)*_LabFarPlane<.015)?step(id+.5,otherId):step(depth,d);
                    boundary=max(boundary,separate*front*objectData.a*step(.5,id));
                    // Only the foreground pixel owns a depth boundary: one source-pixel ink.
                    outline=max(outline,step(_DepthThreshold,(d-depth)*_LabFarPlane));
                    ridge=max(ridge,step(_NormalThreshold,1-dot(normal,n))*step(abs(d-depth)*_LabFarPlane,.24));
                }
                float interior=lerp(1,objectData.b,_UseObjectIds);
                float ink=saturate(max(outline,ridge*.48)*interior)*_LineStrength;
                color.rgb=lerp(color.rgb,color.rgb*float3(.37,.32,.51),ink);
                // Object silhouettes get a deep, cool outline so characters and props read as
                // clean sprite shapes even at dusk; the soft tint above stays for inner ink.
                float silhouette=boundary*_UseObjectIds*_LineStrength;
                color.rgb=lerp(color.rgb,color.rgb*float3(.16,.14,.24),silhouette);
                return color;
            }
            ENDHLSL
        }
    }
}
