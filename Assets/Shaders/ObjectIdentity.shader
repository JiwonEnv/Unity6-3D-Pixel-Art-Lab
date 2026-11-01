Shader "Hidden/PixelArtLab/Object Identity"
{
    Properties { _LabObjectData("ID / Interior / Silhouette",Vector)=(.00392157,0,.25,1) }
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
            float4 _LabObjectData;
            struct v2f {float4 position:SV_POSITION;};
            v2f vert(appdata_base v){v2f o;o.position=UnityObjectToClipPos(v.vertex);return o;}
            float4 frag(v2f i):SV_Target{return _LabObjectData;}
            ENDCG
        }
    }
}
