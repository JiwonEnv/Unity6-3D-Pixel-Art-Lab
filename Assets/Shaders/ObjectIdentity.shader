Shader "Hidden/PixelArtLab/Object Identity"
{
    Properties
    {
        _LabObjectData("ID / Interior / Silhouette",Vector)=(.00392157,0,.25,1)
        _LabWindParams("World motion",Vector)=(0,0,1,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZWrite On ZTest LEqual Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "WorldMotion.cginc"
            float4 _LabObjectData;
            float _LabRenderDepthNormals;
            struct v2f {float4 position:SV_POSITION;float3 normal:TEXCOORD0;float depth:TEXCOORD1;};
            v2f vert(appdata_base v)
            {
                LabDeformVertex(v.vertex,v.normal,v.texcoord.xy);
                v2f o;o.position=UnityObjectToClipPos(v.vertex);
                o.normal=mul((float3x3)UNITY_MATRIX_V,UnityObjectToWorldNormal(v.normal));
                o.depth=-UnityObjectToViewPos(v.vertex).z*_ProjectionParams.w;return o;
            }
            float4 frag(v2f i):SV_Target
            {
                if(_LabRenderDepthNormals>.5)return EncodeDepthNormal(saturate(i.depth),normalize(i.normal));
                return _LabObjectData;
            }
            ENDCG
        }
    }
}
