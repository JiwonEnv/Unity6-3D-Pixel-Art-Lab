Shader "Hidden/PixelArtLab/Object Identity"
{
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
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite On ZTest LEqual Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "WorldMotion.cginc"
            #include "PlayerVisibility.cginc"
            float4 _LabObjectData;
            float _LabRenderDepthNormals;
            sampler2D _MainTex,_AlphaMask;float4 _MainTex_ST;float _AlphaClip,_Cutoff;
            struct v2f {float4 position:SV_POSITION;float3 normal:TEXCOORD0;float depth:TEXCOORD1;float2 uv:TEXCOORD2;float3 world:TEXCOORD3;float4 screen:TEXCOORD4;};
            v2f vert(appdata_base v)
            {
                LabDeformVertex(v.vertex,v.normal,v.texcoord.xy);
                v2f o;o.position=UnityObjectToClipPos(v.vertex);
                o.normal=mul((float3x3)UNITY_MATRIX_V,UnityObjectToWorldNormal(v.normal));
                o.depth=-UnityObjectToViewPos(v.vertex).z*_ProjectionParams.w;o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.screen=ComputeScreenPos(o.position);return o;
            }
            float4 frag(v2f i):SV_Target
            {
                clip(lerp(1,tex2D(_AlphaMask,i.uv).r,_AlphaClip)-_Cutoff);
                LabVisibilityClip(i.world,i.screen);
                if(_LabRenderDepthNormals>.5)return EncodeDepthNormal(saturate(i.depth),normalize(i.normal));
                return _LabObjectData;
            }
            ENDCG
        }
    }
}
