Shader "PixelArtLab/Sprite Toon"
{
    Properties
    {
        _Color ("Paint Color", Color) = (1,1,1,1)
        _ShadeColor ("Shadow Hue", Color) = (.40,.40,.58,1)
        _MainTex ("Painted Detail (Optional)", 2D) = "white" {}
        _EmissionColor ("Emission", Color) = (0,0,0,0)
        _LocalLightStrength ("Local Light Strength", Range(0,3)) = 1
        [HideInInspector] _LabObjectData("Object metadata",Vector)=(.00392157,0,.25,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf SpriteToon fullforwardshadows addshadow noambient
        #pragma target 3.0
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        fixed4 _Color, _ShadeColor, _EmissionColor;
        float _LabContinuous;
        half _LocalLightStrength;
        struct Input { float2 uv_MainTex; float3 worldPos; };
        half4 LightingSpriteToon(SurfaceOutput s, half3 lightDir, half atten)
        {
            half n = saturate(dot(s.Normal,lightDir));
            #ifdef UNITY_PASS_FORWARDADD
            // Toon the surface response, not the distance/shadow attenuation.
            // Quantizing n * atten erased the weak light around a torch.
            half localBand = n < .16 ? 0 : n < .48 ? .48 : n < .78 ? .82 : 1;
            half localDiffuse = lerp(localBand,n,_LabContinuous);
            return half4(s.Albedo * _LightColor0.rgb * localDiffuse * atten * _LocalLightStrength,0);
            #else
            half energy = n * atten;
            half band = energy < .16 ? 0 : energy < .48 ? .48 : energy < .78 ? .82 : 1;
            band = lerp(band, energy, _LabContinuous);
            half3 ramp = lerp(_ShadeColor.rgb, half3(1.04,1.0,.86),band);
            return half4(s.Albedo * ramp * lerp(half3(.9,.95,1.0),_LightColor0.rgb,.38),1);
            #endif
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = tex2D(_MainTex,IN.uv_MainTex).rgb * _Color.rgb;
            o.Alpha=1; o.Emission=_EmissionColor.rgb;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
