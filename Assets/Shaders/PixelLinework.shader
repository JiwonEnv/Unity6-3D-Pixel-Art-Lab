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
            sampler2D _ObjectIdentityTexture;
            sampler2D _LabSceneDepthNormals;
            float _HasSceneDepthNormals;
            float4 SceneGeometry(float2 uv){return _HasSceneDepthNormals>.5?tex2D(_LabSceneDepthNormals,uv):tex2D(_CameraDepthNormalsTexture,uv);}
            float4 _MainTex_TexelSize;
            float _DepthThreshold, _NormalThreshold, _LineStrength;
            float _UseObjectIds, _DebugObjectIds;
            float4 frag(v2f_img i):SV_Target
            {
                float4 color=tex2D(_MainTex,i.uv);
                float depth; float3 normal;
                DecodeDepthNormal(SceneGeometry(i.uv),depth,normal);
                float4 objectData=tex2D(_ObjectIdentityTexture,i.uv);
                float id=round(objectData.r*255)+round(objectData.g*255)*256;
                if(_DebugObjectIds>.5)
                {
                    float3 hue=abs(frac(frac(id*.618034)+float3(0,2.0/3,1.0/3))*6-3);
                    return float4(id>.5?lerp(float3(.20,.20,.20),saturate(hue-1),.8):float3(.04,.04,.06),1);
                }
                float outline=0, ridge=0, boundary=0;
                const float2 offsets[4]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1)};
                for(int k=0;k<4;k++)
                {
                    float d;float3 n;
                    DecodeDepthNormal(SceneGeometry(i.uv+offsets[k]*abs(_MainTex_TexelSize.xy)),d,n);
                    float4 other=tex2D(_ObjectIdentityTexture,i.uv+offsets[k]*abs(_MainTex_TexelSize.xy));
                    float otherId=round(other.r*255)+round(other.g*255)*256;
                    float separate=step(.5,abs(id-otherId));
                    // The ID boundary belongs to the closer object. Equal-depth ties go to lower ID.
                    float front=(abs(d-depth)*_ProjectionParams.z<.015)?step(id+.5,otherId):step(depth,d);
                    boundary=max(boundary,separate*front*objectData.a*step(.5,id));
                    // Only the foreground pixel owns a depth boundary: one source-pixel ink.
                    outline=max(outline,step(_DepthThreshold,(d-depth)*_ProjectionParams.z));
                    ridge=max(ridge,step(_NormalThreshold,1-dot(normal,n))*step(abs(d-depth)*_ProjectionParams.z,.24));
                }
                float interior=lerp(1,objectData.b,_UseObjectIds);
                float ink=saturate(max(max(outline,ridge*.48)*interior,boundary*_UseObjectIds))*_LineStrength;
                color.rgb=lerp(color.rgb,color.rgb*float3(.37,.32,.51),ink);
                return color;
            }
            ENDCG
        }
    }
}
