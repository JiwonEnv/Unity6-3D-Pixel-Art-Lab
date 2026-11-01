Shader "Hidden/PixelArtLab/Linework"
{
    Properties { _MainTex("Scene",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _CameraDepthNormalsTexture;
            float4 _MainTex_TexelSize;
            float _DepthThreshold, _NormalThreshold, _LineStrength;
            float4 frag(v2f_img i):SV_Target
            {
                float4 color=tex2D(_MainTex,i.uv);
                float depth; float3 normal;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,i.uv),depth,normal);
                float outline=0, ridge=0;
                const float2 offsets[4]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1)};
                for(int k=0;k<4;k++)
                {
                    float d;float3 n;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture,i.uv+offsets[k]*abs(_MainTex_TexelSize.xy)),d,n);
                    // Only the foreground pixel owns a depth boundary: one source-pixel ink.
                    outline=max(outline,step(_DepthThreshold,(d-depth)*_ProjectionParams.z));
                    ridge=max(ridge,step(_NormalThreshold,1-dot(normal,n))*step(abs(d-depth)*_ProjectionParams.z,.24));
                }
                float ink=saturate(max(outline,ridge*.48))*_LineStrength;
                color.rgb=lerp(color.rgb,color.rgb*float3(.37,.32,.51),ink);
                return color;
            }
            ENDCG
        }
    }
}
