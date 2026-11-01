Shader "PixelArtLab/Sprite Toon"
{
    Properties
    {
        _Color ("Paint Color", Color) = (1,1,1,1)
        _ShadeColor ("Shadow Hue", Color) = (.40,.40,.58,1)
        _MainTex ("Painted Detail (Optional)", 2D) = "white" {}
        _EmissionColor ("Emission", Color) = (0,0,0,0)
        _LocalLightStrength ("Local Light Strength", Range(0,3)) = 1
        _SemiPBR ("Semi-real shading blend",Range(0,1))=0
        _NormalMap ("Surface Normal",2D)="bump"{}
        _NormalScale ("Normal strength",Range(0,2))=1
        _SurfaceMap ("AO Roughness Metal (RGB)",2D)="white"{}
        _UsePackedARM ("Use packed surface map",Float)=0
        _Roughness ("Roughness multiplier",Range(.08,1))=.75
        _AOStrength ("AO strength",Range(0,1))=.7
        _Metallic ("Metallic",Range(0,1))=0
        _AlphaMask ("Leaf alpha mask",2D)="white"{}
        _AlphaClip ("Alpha cutout",Float)=0
        _Cutoff ("Cutout threshold",Range(0,1))=.45
        _LeafTwoSided ("Leaf light transmission",Range(0,1))=0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
        [HideInInspector] _LabWaveScale("Wave scale",Float)=1
        [HideInInspector] _LabWaterFlow("Smooth water clock",Float)=0
        _DetailReflection ("Planar water reflection",2D)="black"{}
        _ReflectionStrength ("Water reflection strength",Range(0,1))=.6
        _Puddles ("Rain puddles on ground",Range(0,1))=0
        _PuddleReflection ("Ground planar reflection",2D)="black"{}
        [HideInInspector] _LabWindParams("World motion",Vector)=(0,0,1,0)
        [HideInInspector] _LabWaterBounds("Water center and radii",Vector)=(-30,22,11.5,10.5)
        [HideInInspector] _LabObjectData("Object metadata",Vector)=(.00392157,0,.25,1)
    }
    SubShader
    {
        Cull [_Cull]
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf SpriteToon fullforwardshadows addshadow noambient vertex:LabVertex
        #pragma target 3.5
        #include "UnityCG.cginc"
        #include "WorldMotion.cginc"
        #include "PuddleMask.cginc"
        sampler2D _MainTex;
        sampler2D _NormalMap,_SurfaceMap,_AlphaMask,_DetailReflection;
        fixed4 _Color, _ShadeColor, _EmissionColor;
        float _LabContinuous;
        half _LocalLightStrength;
        float4 _LabAmbientTint, _LabWaterBounds;
        float _LabCloudCover;
        half _SemiPBR,_NormalScale,_UsePackedARM,_Roughness,_AOStrength,_Metallic,_AlphaClip,_Cutoff,_LeafTwoSided,_ReflectionStrength;
        float _LabDetailedToon,_LabNormalDisabled,_LabReflectionDisabled;
        sampler2D _PuddleReflection;
        float _Puddles,_LabReflectionPass;
        float4x4 _PuddleProjection;float _PuddleCaptureHeight;
        float4x4 _DetailProjection;float _DetailCaptureHeight;
        struct Input { float2 uv_MainTex; float3 worldPos; float3 worldNormal; float3 viewDir; float4 screenPos; INTERNAL_DATA };
        void LabVertex(inout appdata_full v){LabDeformVertex(v.vertex,v.normal,v.texcoord.xy);}
        half DetailBlend(){return _SemiPBR>.001?(_LabDetailedToon>1.5?1:_LabDetailedToon>.5?0:_SemiPBR):0;}
        half3 SurfaceSpecular(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten)
        {
            half3 h=normalize(lightDir+viewDir);half nl=saturate(dot(s.Normal,lightDir));
            half nv=max(.05,abs(dot(s.Normal,viewDir))),nh=saturate(dot(s.Normal,h)),vh=saturate(dot(viewDir,h));
            half rough=max(.08,s.Specular),alpha=rough*rough,a2=alpha*alpha;
            half denom=nh*nh*(a2-1)+1;half d=a2/(UNITY_PI*denom*denom+.0001);
            half k=(rough+1)*(rough+1)/8;half g=(nl/(nl*(1-k)+k+.001))*(nv/(nv*(1-k)+k));
            half3 f0=lerp(half3(.04,.04,.04),s.Albedo,_Metallic);half3 f=f0+(1-f0)*pow(1-vh,5);
            return min(4,d*g*f/(4*nv*max(.03,nl)))*nl*atten*_LightColor0.rgb;
        }
        half4 LightingSpriteToon(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half n = lerp(saturate(dot(s.Normal,lightDir)),abs(dot(s.Normal,lightDir)),_LeafTwoSided);
            half detail=DetailBlend();
            #ifdef UNITY_PASS_FORWARDADD
            // Toon the surface response, not the distance/shadow attenuation.
            // Quantizing n * atten erased the weak light around a torch.
            half localBand = n < .16 ? 0 : n < .48 ? .48 : n < .78 ? .82 : 1;
            half localDiffuse = lerp(localBand,n,max(_LabContinuous,detail));
            return half4(s.Albedo * _LightColor0.rgb * localDiffuse * atten * _LocalLightStrength+SurfaceSpecular(s,lightDir,viewDir,atten)*detail,0);
            #else
            half energy = n * atten * (1-s.Gloss*.35);
            half band = energy < .16 ? 0 : energy < .48 ? .48 : energy < .78 ? .82 : 1;
            band = lerp(band, energy, max(_LabContinuous,detail));
            half3 ramp = lerp(_ShadeColor.rgb, half3(1.04,1.0,.86),band);
            half3 oldLight=ramp*lerp(half3(.9,.95,1.0),_LightColor0.rgb,.38);
            // Shadow is ambient; visible direct light only ADDS energy. Interpolating
            // toward a dim sunset/moon color could previously brighten the shadow.
            half3 ambient=_ShadeColor.rgb*_LabAmbientTint.rgb*lerp(.24,1,_LabEnvironment.y);
            half3 timeLight=ambient+_LightColor0.rgb*half3(1.04,1,.86)*band*.65;
            half wetSpec=step(.975,dot(normalize(lightDir+viewDir),s.Normal))*n*atten*_LabEnvironment.z*.12;
            half3 transmitted=s.Albedo*_LightColor0.rgb*saturate(-dot(s.Normal,lightDir))*_LeafTwoSided*.14*atten;
            return half4(s.Albedo * lerp(oldLight,timeLight,_LabEnvironment.x)+_LightColor0.rgb*wetSpec+SurfaceSpecular(s,lightDir,viewDir,atten)*detail+transmitted,1);
            #endif
        }
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 paint=tex2D(_MainTex,IN.uv_MainTex);
            clip(lerp(1,tex2D(_AlphaMask,IN.uv_MainTex).r,_AlphaClip)-_Cutoff);
            o.Albedo = paint.rgb * _Color.rgb;
            half3 normal=UnpackNormal(tex2D(_NormalMap,IN.uv_MainTex));
            if(_LabWindParams.x>3.5&&_SemiPBR>.01)
            {
                float t=lerp(_LabWeatherTime,_LabWaterTime,_LabWaterFlow)*_LabWaterMotion;
                float2 uv=IN.worldPos.xz*.18;
                half3 a=UnpackNormal(tex2D(_NormalMap,uv+float2(t*.013,t*.007)));
                half3 b=UnpackNormal(tex2D(_NormalMap,uv*1.83+float2(-t*.011,t*.009)));
                normal=normalize(half3(a.xy+b.xy,a.z*b.z));
            }
            o.Normal=normalize(lerp(half3(0,0,1),normal,saturate(_NormalScale)*(1-_LabNormalDisabled)));
            half3 surface=tex2D(_SurfaceMap,IN.uv_MainTex).rgb;
            o.Specular=clamp(_Roughness*lerp(1,surface.g,_UsePackedARM),.08,1);
            o.Albedo*=lerp(1,surface.r,_UsePackedARM*_AOStrength);
            float3 worldNormal=normalize(WorldNormalVector(IN,o.Normal));
            // Stable world-space basins: rain expands the pools, residual wetness dries them.
            float pool=0;float3 geometryNormal=normalize(WorldNormalVector(IN,half3(0,0,1)));
            if(_Puddles>.001){
            pool=_Puddles*LabPuddleMask(IN.worldPos,geometryNormal.y,_LabEnvironment.z,_LabEnvironment.w);
            float2 rippleCell=floor(IN.worldPos.xz*1.3);
            float rippleAge=frac(_LabWaterTime*1.25+frac(sin(dot(rippleCell,float2(43.1,17.7)))*1327.8));
            float2 rippleOffset=frac(IN.worldPos.xz*1.3)-.5;
            float ring=sin((length(rippleOffset)-rippleAge*.7)*65)*exp(-abs(length(rippleOffset)-rippleAge*.7)*35)*(1-rippleAge);
            o.Normal=normalize(lerp(o.Normal,half3(normalize(rippleOffset+.001)*ring*.08*_LabEnvironment.z,1),pool));
            o.Specular=lerp(o.Specular,.12,pool);
            }
            if(_LabEnvironment.x>.5)
            {
                float top=saturate((worldNormal.y-.20)*1.6);
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
                    float waterTime=lerp(_LabWeatherTime,_LabWaterTime,_LabWaterFlow)*_LabWaterMotion;
                    float ripple=sin(IN.worldPos.z*5+sin(IN.worldPos.x*.7+waterTime*.8)*.9-waterTime*1.4);
                    float edge=length((IN.worldPos.xz-_LabWaterBounds.xy)/_LabWaterBounds.zw);
                    float foam=step(.982+sin(waterTime*1.3+IN.worldPos.z*2)*.006,edge);
                    float2 cell=floor(IN.worldPos.xz*.7);
                    float age=frac(waterTime*.9+frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453));
                    float rainRing=(1-step(.035,abs(length(frac(IN.worldPos.xz*.7)-.5)-age*.65)))*(1-age);
                    float glint=step(.985,ripple)*step(.65,sin(IN.worldPos.x*4.8+sin(IN.worldPos.z*1.4)*1.8+waterTime*.6))*.4+rainRing*_LabEnvironment.z*.24;
                    o.Albedo=lerp(o.Albedo,half3(.65,.83,.80),saturate(glint*(1-DetailBlend()*.75)+foam*.65));
                }
            }
            o.Alpha=1; o.Emission=_EmissionColor.rgb;
            if(pool>.001&&_LabReflectionPass<.5)
            {
                // Preserve actual receiver height: flattening y to the capture plane
                // compensated only half the offset and detached distant reflections.
                float4 puddleClip=mul(_PuddleProjection,float4(IN.worldPos,1));
                float2 uv=puddleClip.xy/puddleClip.w*.5+.5+o.Normal.xy*.012;
                half3 reflected=tex2D(_PuddleReflection,uv).rgb;
                float reflectionAmount=pool*(.55+.25*pow(1-saturate(dot(geometryNormal,normalize(_WorldSpaceCameraPos-IN.worldPos))),4))*(1-_LabReflectionDisabled);
                o.Albedo*=1-reflectionAmount*.8;
                o.Emission+=reflected*reflectionAmount;
            }
            if(_LabWindParams.x>3.5&&_SemiPBR>.01&&_LabReflectionPass<.5)
            {
                float4 waterClip=mul(_DetailProjection,float4(IN.worldPos.x,_DetailCaptureHeight,IN.worldPos.z,1));
                float2 uv=waterClip.xy/waterClip.w*.5+.5;
                uv+=o.Normal.xy*.018;
                float3 view=normalize(_WorldSpaceCameraPos-IN.worldPos);
                float fresnel=.12+.75*pow(1-saturate(dot(worldNormal,view)),4);
                half3 reflection=tex2D(_DetailReflection,uv).rgb;
                o.Emission+=reflection*fresnel*_ReflectionStrength*(1-_LabReflectionDisabled)*(.35+.65*_LabEnvironment.y);
            }
        }
        ENDCG
    }
    Fallback "Diffuse"
}
