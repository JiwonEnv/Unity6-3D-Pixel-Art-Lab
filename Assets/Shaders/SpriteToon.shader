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
        _PuddleWetStrength ("Damp soil around puddles",Range(0,1))=1
        _PuddleReflection ("Ground planar reflection",2D)="black"{}
        _CharacterRim ("Character rim strength",Range(0,1))=0
        _CharacterBounce ("Character bounce fill",Range(0,1))=0
        [HideInInspector] _LabCharacter("Character",Float)=0
        [HideInInspector] _LabRevealAmount("Player reveal",Float)=0
        [HideInInspector] _LabWindParams("World motion",Vector)=(0,0,1,0)
        [HideInInspector] _LabWaterBounds("Water center and radii",Vector)=(-30,22,11.5,10.5)
        [HideInInspector] _LabObjectData("Object metadata",Vector)=(.00392157,0,.25,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // Every pass shares one per-material layout so the SRP Batcher can batch them.
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4 _Color,_ShadeColor,_EmissionColor;
        half _LocalLightStrength,_SemiPBR,_NormalScale,_UsePackedARM,_Roughness,_AOStrength,_Metallic;
        half _AlphaClip,_Cutoff,_LeafTwoSided,_ReflectionStrength;
        float _LabWaveScale,_LabWaterFlow,_Puddles,_PuddleWetStrength;
        half _CharacterRim,_CharacterBounce,_LabCharacter;
        float _LabRevealAmount;
        float4 _LabWindParams,_LabWaterBounds,_LabObjectData;
        CBUFFER_END
        TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
        TEXTURE2D(_AlphaMask);SAMPLER(sampler_AlphaMask);
        #include "WorldMotion.cginc"
        #include "PlayerVisibility.cginc"
        half LeafCutout(float2 uv){return lerp(1,SAMPLE_TEXTURE2D(_AlphaMask,sampler_AlphaMask,uv).r,_AlphaClip)-_Cutoff;}
        ENDHLSL

        Pass
        {
            Name "SpriteToonForward"
            Tags {"LightMode"="UniversalForward"}
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex ToonVert
            #pragma fragment ToonFrag
            #pragma target 3.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "PuddleMask.cginc"
            TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
            TEXTURE2D(_SurfaceMap);SAMPLER(sampler_SurfaceMap);
            TEXTURE2D(_DetailReflection);SAMPLER(sampler_DetailReflection);
            TEXTURE2D(_PuddleReflection);SAMPLER(sampler_PuddleReflection);
            float _LabContinuous;
            float4 _LabAmbientTint;
            float _LabCloudCover;
            float _LabDetailedToon,_LabNormalDisabled,_LabReflectionDisabled;
            // Written globally by DetailWaterReflection (one lake capture, one puddle capture).
            float4x4 _PuddleProjection;float _PuddleCaptureHeight;
            float4x4 _DetailProjection;float _DetailCaptureHeight;

            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;float3 normalWS:TEXCOORD2;float4 tangentWS:TEXCOORD3;half fogFactor:TEXCOORD4;};
            struct ToonSurface{half3 albedo;half3 normalWS;half rough;half gloss;};

            Varyings ToonVert(Attributes v)
            {
                LabDeformVertex(v.positionOS,v.normalOS,v.uv);
                VertexPositionInputs position=GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normal=GetVertexNormalInputs(v.normalOS,v.tangentOS);
                Varyings o;o.positionCS=position.positionCS;o.positionWS=position.positionWS;o.normalWS=normal.normalWS;
                o.tangentWS=float4(normal.tangentWS,v.tangentOS.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.fogFactor=ComputeFogFactor(position.positionCS.z);
                return o;
            }
            half DetailBlend(){return _SemiPBR>.001?(_LabDetailedToon>1.5?1:_LabDetailedToon>.5?0:_SemiPBR):0;}
            half3 SurfaceSpecular(ToonSurface s,half3 lightDir,half3 viewDir,half atten,half3 lightColor)
            {
                half3 h=normalize(lightDir+viewDir);half nl=saturate(dot(s.normalWS,lightDir));
                half nv=max(.05,abs(dot(s.normalWS,viewDir))),nh=saturate(dot(s.normalWS,h)),vh=saturate(dot(viewDir,h));
                half rough=max(.08,s.rough),alpha=rough*rough,a2=alpha*alpha;
                half denom=nh*nh*(a2-1)+1;half d=a2/(PI*denom*denom+.0001);
                half k=(rough+1)*(rough+1)/8;half g=(nl/(nl*(1-k)+k+.001))*(nv/(nv*(1-k)+k));
                half3 f0=lerp(half3(.04,.04,.04),s.albedo,_Metallic);half3 f=f0+(1-f0)*pow(1-vh,5);
                return min(4,d*g*f/(4*nv*max(.03,nl)))*nl*atten*lightColor;
            }
            half3 SunLighting(ToonSurface s,half3 lightDir,half3 viewDir,half atten,half3 lightColor)
            {
                half n=lerp(saturate(dot(s.normalWS,lightDir)),abs(dot(s.normalWS,lightDir)),_LeafTwoSided);
                half detail=DetailBlend();
                half energy=n*atten*(1-s.gloss*.35);
                half band=energy<.16?0:energy<.48?.48:energy<.78?.82:1;
                band=lerp(band,energy,max(_LabContinuous,detail));
                half3 ramp=lerp(_ShadeColor.rgb,half3(1.04,1.0,.86),band);
                half3 oldLight=ramp*lerp(half3(.9,.95,1.0),lightColor,.38);
                // Shadow is ambient; visible direct light only ADDS energy. Interpolating
                // toward a dim sunset/moon color could previously brighten the shadow.
                half3 ambient=_ShadeColor.rgb*_LabAmbientTint.rgb*lerp(.24,1,_LabEnvironment.y);
                half3 timeLight=ambient+lightColor*half3(1.04,1,.86)*band*.65;
                half wetSpec=step(.975,dot(normalize(lightDir+viewDir),s.normalWS))*n*atten*_LabEnvironment.z*.12;
                half3 transmitted=s.albedo*lightColor*saturate(-dot(s.normalWS,lightDir))*_LeafTwoSided*.14*atten;
                return s.albedo*lerp(oldLight,timeLight,_LabEnvironment.x)+lightColor*wetSpec+SurfaceSpecular(s,lightDir,viewDir,atten,lightColor)*detail+transmitted;
            }
            half3 LocalLighting(ToonSurface s,half3 lightDir,half3 viewDir,half atten,half3 lightColor)
            {
                // Toon the surface response, not the distance/shadow attenuation.
                // Quantizing n * atten erased the weak light around a torch.
                half n=lerp(saturate(dot(s.normalWS,lightDir)),abs(dot(s.normalWS,lightDir)),_LeafTwoSided);
                half detail=DetailBlend();
                half localBand=n<.16?0:n<.48?.48:n<.78?.82:1;
                half localDiffuse=lerp(localBand,n,max(_LabContinuous,detail));
                return s.albedo*lightColor*localDiffuse*atten*_LocalLightStrength+SurfaceSpecular(s,lightDir,viewDir,atten,lightColor)*detail;
            }
            // Built-in point-light falloff (1/(1+25 d^2/r^2), fading out from 0.8 r) so torch
            // pools keep their authored size instead of URP's inverse-square curve.
            half LegacyAttenuation(uint i,float3 positionWS)
            {
                #if USE_CLUSTER_LIGHT_LOOP
                int index=i;
                #else
                int index=GetPerObjectLightIndex(i);
                #endif
                #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
                float4 position=_AdditionalLightsBuffer[index].position;float4 attenuation=_AdditionalLightsBuffer[index].attenuation;
                #else
                float4 position=_AdditionalLightsPosition[index];float4 attenuation=_AdditionalLightsAttenuation[index];
                #endif
                if(position.w<.5)return 1;
                float3 offset=position.xyz-positionWS;float t=dot(offset,offset)*attenuation.x;
                return saturate((1-t)/.36)/(1+25*t);
            }
            half3 TangentToWorld(Varyings i,half3 normalTS)
            {
                float3 bitangent=i.tangentWS.w*cross(i.normalWS,i.tangentWS.xyz);
                return normalize(TransformTangentToWorld(normalTS,half3x3(i.tangentWS.xyz,bitangent,i.normalWS)));
            }
            half4 ToonFrag(Varyings i):SV_Target
            {
                half4 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                clip(LeafCutout(i.uv));
                LabVisibilityClip(i.positionWS,i.positionCS.xy);
                float3 worldPos=i.positionWS;
                half3 albedo=paint.rgb*_Color.rgb;
                half3 normal=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv));
                if(_LabWindParams.x>3.5&&_SemiPBR>.01)
                {
                    float t=lerp(_LabWeatherTime,_LabWaterTime,_LabWaterFlow)*_LabWaterMotion;
                    float2 uv=worldPos.xz*.18;
                    half3 a=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,uv+float2(t*.013,t*.007)));
                    half3 b=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,uv*1.83+float2(-t*.011,t*.009)));
                    normal=normalize(half3(a.xy+b.xy,a.z*b.z));
                }
                half3 normalTS=normalize(lerp(half3(0,0,1),normal,saturate(_NormalScale)*(1-_LabNormalDisabled)));
                half3 surface=SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,i.uv).rgb;
                half rough=clamp(_Roughness*lerp(1,surface.g,_UsePackedARM),.08,1);
                albedo*=lerp(1,surface.r,_UsePackedARM*_AOStrength);
                float3 worldNormal=TangentToWorld(i,normalTS);
                // Stable world-space basins: rain expands the pools, residual wetness dries them.
                float pool=0,wetGround=0;float3 geometryNormal=normalize(i.normalWS);
                if(_Puddles>.001)
                {
                    float2 regions=LabPuddleRegions(worldPos,geometryNormal.y,_LabEnvironment.z,_LabEnvironment.w);
                    pool=_Puddles*regions.x;
                    wetGround=_Puddles*_PuddleWetStrength*regions.y*(1-pool);
                    albedo*=lerp(half3(1,1,1),half3(.43,.50,.54),wetGround);
                    rough=lerp(rough,.28,wetGround);
                    normalTS=normalize(lerp(normalTS,half3(0,0,1),wetGround*.25));
                    float2 rippleCell=floor(worldPos.xz*1.3);
                    float rippleAge=frac(_LabWaterTime*1.25+frac(sin(dot(rippleCell,float2(43.1,17.7)))*1327.8));
                    float2 rippleOffset=frac(worldPos.xz*1.3)-.5;
                    float ring=sin((length(rippleOffset)-rippleAge*.7)*65)*exp(-abs(length(rippleOffset)-rippleAge*.7)*35)*(1-rippleAge);
                    normalTS=normalize(lerp(normalTS,half3(normalize(rippleOffset+.001)*ring*.08*_LabEnvironment.z,1),pool));
                    rough=lerp(rough,.12,pool);
                }
                half gloss=0;
                if(_LabEnvironment.x>.5)
                {
                    float top=saturate((worldNormal.y-.20)*1.6);
                    float snow=floor(saturate(_LabEnvironment.w*top)*4+.2)/4;
                    float emissive=saturate(max(_EmissionColor.r,max(_EmissionColor.g,_EmissionColor.b))*3);
                    if(_LabWindParams.x<3.5)
                    {
                        albedo*=1-_LabEnvironment.z*.17*(1-emissive);
                        albedo=lerp(albedo,half3(.86,.91,.95),snow*(1-emissive));
                    }
                    float cloud=sin(worldPos.x*.085+_LabWeatherTime*.04)+sin(worldPos.z*.11-_LabWeatherTime*.03);
                    gloss=step(.3,cloud)*_LabCloudCover;
                    if(_LabWindParams.x>3.5)
                    {
                        float waterTime=lerp(_LabWeatherTime,_LabWaterTime,_LabWaterFlow)*_LabWaterMotion;
                        float ripple=sin(worldPos.z*5+sin(worldPos.x*.7+waterTime*.8)*.9-waterTime*1.4);
                        float edge=length((worldPos.xz-_LabWaterBounds.xy)/_LabWaterBounds.zw);
                        float foam=step(.982+sin(waterTime*1.3+worldPos.z*2)*.006,edge);
                        float2 cell=floor(worldPos.xz*.7);
                        float age=frac(waterTime*.9+frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453));
                        float rainRing=(1-step(.035,abs(length(frac(worldPos.xz*.7)-.5)-age*.65)))*(1-age);
                        float glint=step(.985,ripple)*step(.65,sin(worldPos.x*4.8+sin(worldPos.z*1.4)*1.8+waterTime*.6))*.4+rainRing*_LabEnvironment.z*.24;
                        albedo=lerp(albedo,half3(.65,.83,.80),saturate(glint*(1-DetailBlend()*.75)+foam*.65));
                    }
                }
                half3 emission=_EmissionColor.rgb;
                float3 view=normalize(_WorldSpaceCameraPos-worldPos);
                Light sun=GetMainLight(TransformWorldToShadowCoord(worldPos));
                if(_LabCharacter>.5)
                {
                    float rim=floor(pow(1-saturate(dot(worldNormal,view)),3.5)*4+.35)/4;
                    float facing=lerp(.4,1,saturate(dot(worldNormal,sun.direction)*.5+.5));
                    half3 rimColor=lerp(half3(.42,.68,1),half3(1,.86,.62),_LabEnvironment.y*.6);
                    emission+=rimColor*rim*_CharacterRim*facing;
                    half3 bounce=lerp(half3(.15,.25,.40),half3(.32,.37,.24),_LabEnvironment.y);
                    emission+=albedo*bounce*_CharacterBounce*(.45+.55*(1-saturate(worldNormal.y)));
                }
                if((pool>.001||wetGround>.001)&&_LabReflectionPass<.5)
                {
                    // Preserve actual receiver height: flattening y to the capture plane
                    // compensated only half the offset and detached distant reflections.
                    float4 puddleClip=mul(_PuddleProjection,float4(worldPos,1));
                    float2 uv=puddleClip.xy/puddleClip.w*.5+.5+normalTS.xy*.012;
                    half3 reflected=SAMPLE_TEXTURE2D(_PuddleReflection,sampler_PuddleReflection,uv).rgb;
                    float grazing=1-saturate(dot(geometryNormal,view));
                    float reflectionAmount=pool*(.55+.25*pow(grazing,4))*(1-_LabReflectionDisabled);
                    reflectionAmount+=wetGround*.06*pow(grazing,2)*(1-_LabReflectionDisabled);
                    albedo*=1-reflectionAmount*.8;
                    emission+=reflected*reflectionAmount;
                }
                if(_LabWindParams.x>3.5&&_SemiPBR>.01&&_LabReflectionPass<.5)
                {
                    float4 waterClip=mul(_DetailProjection,float4(worldPos.x,_DetailCaptureHeight,worldPos.z,1));
                    float2 uv=waterClip.xy/waterClip.w*.5+.5;
                    uv+=normalTS.xy*.018;
                    float fresnel=.12+.75*pow(1-saturate(dot(worldNormal,view)),4);
                    half3 reflection=SAMPLE_TEXTURE2D(_DetailReflection,sampler_DetailReflection,uv).rgb;
                    emission+=reflection*fresnel*_ReflectionStrength*(1-_LabReflectionDisabled)*(.35+.65*_LabEnvironment.y);
                }

                ToonSurface s;s.albedo=albedo;s.rough=rough;s.gloss=gloss;s.normalWS=TangentToWorld(i,normalTS);
                half3 color=SunLighting(s,sun.direction,view,sun.distanceAttenuation*sun.shadowAttenuation,sun.color)+emission;
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData=(InputData)0;inputData.positionWS=worldPos;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount=GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light=GetAdditionalLight(lightIndex,worldPos,half4(1,1,1,1));
                    color+=LocalLighting(s,light.direction,view,LegacyAttenuation(lightIndex,worldPos)*light.shadowAttenuation,light.color);
                LIGHT_LOOP_END
                #endif
                return half4(MixFog(color,i.fogFactor),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;float3 _LightPosition;
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings ShadowVert(Attributes v)
            {
                LabDeformVertex(v.positionOS,v.normalOS,v.uv);
                float3 positionWS=TransformObjectToWorld(v.positionOS.xyz),normalWS=TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection=normalize(_LightPosition-positionWS);
                #else
                float3 lightDirection=_LightDirection;
                #endif
                Varyings o;o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,lightDirection)));
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;
            }
            half4 ShadowFrag(Varyings i):SV_Target{clip(LeafCutout(i.uv));return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;};
            Varyings DepthVert(Attributes v)
            {
                LabDeformVertex(v.positionOS,v.normalOS,v.uv);
                Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;
            }
            half DepthFrag(Varyings i):SV_Target{clip(LeafCutout(i.uv));LabVisibilityClip(i.positionWS,i.positionCS.xy);return i.positionCS.z;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode"="DepthNormals"}
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;float3 normalWS:TEXCOORD2;};
            Varyings DepthNormalsVert(Attributes v)
            {
                LabDeformVertex(v.positionOS,v.normalOS,v.uv);
                Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;
            }
            half4 DepthNormalsFrag(Varyings i):SV_Target{clip(LeafCutout(i.uv));LabVisibilityClip(i.positionWS,i.positionCS.xy);return half4(normalize(i.normalWS),0);}
            ENDHLSL
        }
        Pass
        {
            Name "OccluderStencil"
            Tags {"LightMode"="PlayerVisibilityStencil"}
            ColorMask 0 ZWrite Off ZTest Equal Cull [_Cull]
            Stencil {Ref 5 Comp Always Pass Replace}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings vert(Attributes v){LabDeformVertex(v.positionOS,v.normalOS,v.uv);Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
            half4 frag(Varyings i):SV_Target{clip(LeafCutout(i.uv));return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "CharacterScan"
            Tags {"LightMode"="PlayerVisibilityScan"}
            ZWrite Off ZTest Greater Cull Back Blend SrcAlpha OneMinusSrcAlpha
            Stencil {Ref 5 Comp Equal Pass Keep}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float _LabScanAmount,_LabScanTime;
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
            Varyings vert(Attributes v){Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normalOS);return o;}
            half4 frag(Varyings i):SV_Target
            {
                float rim=pow(1-saturate(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world))),2);
                float scan=step(.7,frac(i.world.y*12-_LabScanTime*.8));
                float grid=max(step(.84,frac(i.world.x*10)),step(.84,frac(i.world.z*10)));
                return half4(lerp(float3(.04,.55,.78),float3(.4,.95,1),saturate(rim*.7+grid*.25+scan*.2)),_LabScanAmount*(.24+rim*.46+scan*.18+grid*.12));
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
