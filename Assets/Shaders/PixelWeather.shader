Shader "PixelArtLab/Weather Particles"
{
    SubShader
    {
        Tags {"Queue"="Transparent+20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _LabEnvironment;
            struct appdata{float4 vertex:POSITION;float4 color:COLOR;};
            struct v2f{float4 position:SV_POSITION;float4 color:COLOR;float fog:TEXCOORD0;};
            v2f vert(appdata v){v2f o;o.position=TransformObjectToHClip(v.vertex.xyz);o.color=v.color;o.color.rgb*=lerp(float3(.34,.47,.75),float3(.88,.95,1),_LabEnvironment.y);o.fog=ComputeFogFactor(o.position.z);return o;}
            float4 frag(v2f i):SV_Target{float4 c=i.color;c.rgb=MixFog(c.rgb,i.fog);return c;}
            ENDHLSL
        }
    }
}
