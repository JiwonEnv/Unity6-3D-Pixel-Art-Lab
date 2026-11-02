Shader "Hidden/PixelArtLab/Object Identity"
{
    // Used as an override shader by PixelArtLabRendererFeature: each scene material keeps
    // its own property values. Pass 0 writes ID + line policy, pass 1 encoded view depth/normal.
    Properties
    {
        _LabObjectData("ID / Interior / Silhouette",Vector)=(.00392157,0,.25,1)
        _LabWindParams("World motion",Vector)=(0,0,1,0)
        _MainTex("Surface",2D)="white"{}
        _AlphaMask("Mask",2D)="white"{}
        _AlphaClip("Cutout",Float)=0
        _Cutoff("Threshold",Float)=.45
        _Cull("Cull",Float)=2
        _LabWaveScale("Waves",Float)=1
        _LabWaterFlow("Flow",Float)=0
        _LabRevealAmount("Player reveal",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _LabObjectData,_LabWindParams;
        float _AlphaClip,_Cutoff,_LabWaveScale,_LabWaterFlow,_LabRevealAmount;
        CBUFFER_END
        TEXTURE2D(_AlphaMask);SAMPLER(sampler_AlphaMask);
        #include "WorldMotion.cginc"
        #include "PlayerVisibility.cginc"
        struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
        struct Varyings{float4 positionCS:SV_POSITION;float3 normal:TEXCOORD0;float depth:TEXCOORD1;float2 uv:TEXCOORD2;float3 world:TEXCOORD3;};
        Varyings vert(Attributes v)
        {
            LabDeformVertex(v.positionOS,v.normalOS,v.uv);
            Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
            o.normal=mul((float3x3)UNITY_MATRIX_V,TransformObjectToWorldNormal(v.normalOS));
            o.depth=-TransformWorldToView(o.world).z*_ProjectionParams.w;o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;
        }
        void Cutout(Varyings i)
        {
            clip(lerp(1,SAMPLE_TEXTURE2D(_AlphaMask,sampler_AlphaMask,i.uv).r,_AlphaClip)-_Cutoff);
            LabVisibilityClip(i.world,i.positionCS.xy);
        }
        // Same packing as the Built-in EncodeDepthNormal (stereographic normal + 16-bit depth).
        float4 EncodeDepthNormal(float depth,float3 normal)
        {
            float2 enc=normal.xy/(normal.z+1)/1.7777;enc=enc*.5+.5;
            float2 depthRG=float2(1,255)*depth;depthRG=frac(depthRG);depthRG.x-=depthRG.y/255;
            return float4(enc,depthRG);
        }
        ENDHLSL
        Pass
        {
            Name "ObjectIdentity"
            ZWrite On ZTest LEqual Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(Varyings i):SV_Target{Cutout(i);return _LabObjectData;}
            ENDHLSL
        }
        Pass
        {
            Name "AnimatedDepthNormals"
            ZWrite On ZTest LEqual Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(Varyings i):SV_Target{Cutout(i);return EncodeDepthNormal(saturate(i.depth),normalize(i.normal));}
            ENDHLSL
        }
    }
}
