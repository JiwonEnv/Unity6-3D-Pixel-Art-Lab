Shader "PixelArtLab/Sprite Toon"
{
    Properties
    {
        _Color ("Paint Color", Color) = (1,1,1,1)
        _ShadeColor ("Shadow Hue", Color) = (.40,.40,.58,1)
        _MainTex ("Painted Detail (Optional)", 2D) = "white" {}
        _EmissionColor ("Emission", Color) = (0,0,0,0)
        _LocalLightStrength ("Local Light Strength", Range(0,3)) = 1
        [HideInInspector] _LabWindParams("World motion",Vector)=(0,0,1,0)
        [HideInInspector] _LabWaterBounds("Water center and radii",Vector)=(-30,22,11.5,10.5)
        [HideInInspector] _LabObjectData("Object metadata",Vector)=(.00392157,0,.25,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf SpriteToon fullforwardshadows addshadow noambient vertex:LabVertex
        #pragma target 3.0
        #include "UnityCG.cginc"
        #include "WorldMotion.cginc"
        sampler2D _MainTex;
        fixed4 _Color, _ShadeColor, _EmissionColor;
        float _LabContinuous;
        half _LocalLightStrength;
        float4 _LabAmbientTint, _LabWaterBounds;
        float _LabCloudCover;
        struct Input { float2 uv_MainTex; float3 worldPos; float3 worldNormal; };
        void LabVertex(inout appdata_full v){LabDeformVertex(v.vertex,v.normal,v.texcoord.xy);}
        half4 LightingSpriteToon(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half n = saturate(dot(s.Normal,lightDir));
            #ifdef UNITY_PASS_FORWARDADD
            // Toon the surface response, not the distance/shadow attenuation.
            // Quantizing n * atten erased the weak light around a torch.
            half localBand = n < .16 ? 0 : n < .48 ? .48 : n < .78 ? .82 : 1;
            half localDiffuse = lerp(localBand,n,_LabContinuous);
            return half4(s.Albedo * _LightColor0.rgb * localDiffuse * atten * _LocalLightStrength,0);
            #else
            half energy = n * atten * (1-s.Gloss*.35);
            half band = energy < .16 ? 0 : energy < .48 ? .48 : energy < .78 ? .82 : 1;
            band = lerp(band, energy, _LabContinuous);
            half3 ramp = lerp(_ShadeColor.rgb, half3(1.04,1.0,.86),band);
            half3 oldLight=ramp*lerp(half3(.9,.95,1.0),_LightColor0.rgb,.38);
            half3 timeLight=lerp(_ShadeColor.rgb*_LabAmbientTint.rgb,_LightColor0.rgb*half3(1.04,1,.86),band);
            half wetSpec=step(.975,dot(normalize(lightDir+viewDir),s.Normal))*n*atten*_LabEnvironment.z*.12;
            return half4(s.Albedo * lerp(oldLight,timeLight,_LabEnvironment.x)+_LightColor0.rgb*wetSpec,1);
            #endif
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = tex2D(_MainTex,IN.uv_MainTex).rgb * _Color.rgb;
            if(_LabEnvironment.x>.5)
            {
                float top=saturate((IN.worldNormal.y-.20)*1.6);
                float snow=floor(saturate(_LabEnvironment.w*top)*4+.2)/4;
                float emissive=saturate(max(_EmissionColor.r,max(_EmissionColor.g,_EmissionColor.b))*3);
                if(_LabWindParams.x<3.5)
                {
                    o.Albedo*=1-_LabEnvironment.z*.17*(1-emissive);
                    o.Albedo=lerp(o.Albedo,half3(.86,.91,.95),snow*(1-emissive));
                }
                float cloud=sin(IN.worldPos.x*.085+_LabWeatherTime*.04)+sin(IN.worldPos.z*.11-_LabWeatherTime*.03);
                o.Gloss=step(.3,cloud)*_LabCloudCover;
                if(_LabWindParams.x>3.5)
                {
                    float waterTime=_LabWeatherTime*_LabWaterMotion;
                    float ripple=sin(IN.worldPos.z*5+sin(IN.worldPos.x*.7+waterTime*.8)*.9-waterTime*1.4);
                    float edge=length((IN.worldPos.xz-_LabWaterBounds.xy)/_LabWaterBounds.zw);
                    float foam=step(.982+sin(waterTime*1.3+IN.worldPos.z*2)*.006,edge);
                    float2 cell=floor(IN.worldPos.xz*.7);
                    float age=frac(waterTime*.9+frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453));
                    float rainRing=(1-step(.035,abs(length(frac(IN.worldPos.xz*.7)-.5)-age*.65)))*(1-age);
                    float glint=step(.985,ripple)*step(.65,sin(IN.worldPos.x*4.8+sin(IN.worldPos.z*1.4)*1.8+waterTime*.6))*.4+rainRing*_LabEnvironment.z*.24;
                    o.Albedo=lerp(o.Albedo,half3(.65,.83,.80),saturate(glint+foam*.65));
                }
            }
            o.Alpha=1; o.Emission=_EmissionColor.rgb;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
