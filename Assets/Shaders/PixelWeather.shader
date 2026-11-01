Shader "PixelArtLab/Weather Particles"
{
    SubShader
    {
        Tags {"Queue"="Transparent+20" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float4 _LabEnvironment;
            struct appdata{float4 vertex:POSITION;float4 color:COLOR;};
            struct v2f{float4 position:SV_POSITION;float4 color:COLOR;UNITY_FOG_COORDS(0)};
            v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.color=v.color;o.color.rgb*=lerp(float3(.34,.47,.75),float3(.88,.95,1),_LabEnvironment.y);UNITY_TRANSFER_FOG(o,o.position);return o;}
            float4 frag(v2f i):SV_Target{float4 c=i.color;UNITY_APPLY_FOG(i.fogCoord,c);return c;}
            ENDCG
        }
    }
}
