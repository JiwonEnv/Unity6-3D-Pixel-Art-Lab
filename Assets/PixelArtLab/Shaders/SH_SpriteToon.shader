Shader "PixelArtLab/Sprite Toon"
{
    // Surface shader that composes the Lab*.hlsl technique modules. Each module can be
    // included by other shaders on its own; this file only wires material values into them.
    // Material features compile in by keyword (SpriteToonKeywords keeps them in sync):
    //   _LAB_ALPHACLIP leaf cutout, _LAB_NORMALMAP normal map, _LAB_PACKED_ARM AO/roughness map,
    //   _LAB_SEMIREAL GGX highlights, _LAB_PUDDLES rain puddles, _LAB_WATER lake surface.
    // Per-renderer switches (wind mode, character light, player reveal) stay uniforms because
    // they are set with MaterialPropertyBlocks on shared materials.
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
        [Toggle(_LAB_PACKED_ARM)] _UsePackedARM ("Use packed surface map",Float)=0
        _Roughness ("Roughness multiplier",Range(.08,1))=.75
        _AOStrength ("AO strength",Range(0,1))=.7
        _Metallic ("Metallic",Range(0,1))=0
        _AlphaMask ("Leaf alpha mask",2D)="white"{}
        [Toggle(_LAB_ALPHACLIP)] _AlphaClip ("Alpha cutout",Float)=0
        _Cutoff ("Cutout threshold",Range(0,1))=.45
        _LeafTwoSided ("Leaf light transmission",Range(0,1))=0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
        [Toggle(_LAB_WATER)] _WaterSurface ("Water surface",Float)=0
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
        [HideInInspector] _LabWaterBounds("Water center and radii (WaterBody)",Vector)=(-30,22,11.5,10.5)
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
        half _AlphaClip,_Cutoff,_LeafTwoSided,_ReflectionStrength,_WaterSurface;
        float _LabWaveScale,_LabWaterFlow,_Puddles,_PuddleWetStrength;
        half _CharacterRim,_CharacterBounce,_LabCharacter;
        float _LabRevealAmount;
        float4 _LabWindParams,_LabWaterBounds,_LabObjectData;
        CBUFFER_END
        TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
        TEXTURE2D(_AlphaMask);SAMPLER(sampler_AlphaMask);
        #include "LabWind.hlsl"
        #include "LabVisibility.hlsl"
        #define TOON_DEFORM(v) LabDeformVertex(v.positionOS,v.normalOS,_LabWindParams,_LabWaterFlow,_LabWaveScale)
        void ToonClip(float2 uv)
        {
            #if defined(_LAB_ALPHACLIP)
            clip(SAMPLE_TEXTURE2D(_AlphaMask,sampler_AlphaMask,uv).r-_Cutoff);
            #endif
        }
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
            #pragma shader_feature_local_fragment _LAB_ALPHACLIP
            #pragma shader_feature_local_fragment _LAB_NORMALMAP
            #pragma shader_feature_local_fragment _LAB_PACKED_ARM
            #pragma shader_feature_local_fragment _LAB_SEMIREAL
            #pragma shader_feature_local_fragment _LAB_PUDDLES
            #pragma shader_feature_local_fragment _LAB_WATER
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "LabToonLighting.hlsl"
            #include "LabWetness.hlsl"
            #include "LabSnow.hlsl"
            #include "LabPuddles.hlsl"
            #include "LabWater.hlsl"
            #include "LabCharacterLight.hlsl"
            #include "LabHeightFog.hlsl"
            TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
            TEXTURE2D(_SurfaceMap);SAMPLER(sampler_SurfaceMap);
            TEXTURE2D(_DetailReflection);SAMPLER(sampler_DetailReflection);
            TEXTURE2D(_PuddleReflection);SAMPLER(sampler_PuddleReflection);
            // Comparison switches (DetailLookControls): 1 forces toon / semi-real, 1 drops normal maps.
            float _LabDetailedToon,_LabNormalDisabled;

            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;float3 normalWS:TEXCOORD2;float4 tangentWS:TEXCOORD3;half fogFactor:TEXCOORD4;};

            Varyings ToonVert(Attributes v)
            {
                TOON_DEFORM(v);
                VertexPositionInputs position=GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs normal=GetVertexNormalInputs(v.normalOS,v.tangentOS);
                Varyings o;o.positionCS=position.positionCS;o.positionWS=position.positionWS;o.normalWS=normal.normalWS;
                o.tangentWS=float4(normal.tangentWS,v.tangentOS.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.fogFactor=ComputeFogFactor(position.positionCS.z);
                return o;
            }
            half DetailBlend()
            {
                #if defined(_LAB_SEMIREAL)
                return _SemiPBR>.001?(_LabDetailedToon>1.5?1:_LabDetailedToon>.5?0:_SemiPBR):0;
                #else
                return 0;
                #endif
            }
            half3 TangentToWorld(Varyings i,half3 normalTS)
            {
                float3 bitangent=i.tangentWS.w*cross(i.normalWS,i.tangentWS.xyz);
                return normalize(TransformTangentToWorld(normalTS,half3x3(i.tangentWS.xyz,bitangent,i.normalWS)));
            }
            half4 ToonFrag(Varyings i):SV_Target
            {
                half4 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                ToonClip(i.uv);
                LabVisibilityClip(i.positionWS,i.positionCS.xy,_LabRevealAmount);
                float3 worldPos=i.positionWS;
                half3 albedo=paint.rgb*_Color.rgb;
                bool water=_LabWindParams.x>3.5;

                // Surface: normal map, packed AO/roughness.
                half3 normal=half3(0,0,1);
                #if defined(_LAB_NORMALMAP)
                normal=UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv));
                #endif
                #if defined(_LAB_WATER)&&defined(_LAB_SEMIREAL)
                if(water&&_SemiPBR>.01)normal=LabWaterNormal(TEXTURE2D_ARGS(_NormalMap,sampler_NormalMap),worldPos,_LabWaterFlow);
                #endif
                half3 normalTS=normalize(lerp(half3(0,0,1),normal,saturate(_NormalScale)*(1-_LabNormalDisabled)));
                half rough=clamp(_Roughness,.08,1);
                #if defined(_LAB_PACKED_ARM)
                half3 surface=SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,i.uv).rgb;
                rough=clamp(_Roughness*surface.g,.08,1);
                albedo*=lerp(1,surface.r,_AOStrength);
                #endif
                float3 worldNormal=TangentToWorld(i,normalTS);
                float3 geometryNormal=normalize(i.normalWS);

                // Rain puddles: stable basins, damp soil, rain rings.
                LabPuddle puddle;puddle.pool=0;puddle.wetGround=0;
                #if defined(_LAB_PUDDLES)
                puddle=LabApplyPuddles(worldPos,geometryNormal.y,_Puddles,_PuddleWetStrength,albedo,rough,normalTS);
                #endif

                // Weather on every surface: wet darkening, snow, cloud shade, water glints.
                half gloss=0;
                if(_LabEnvironment.x>.5)
                {
                    half emissive=LabEmissiveMask(_EmissionColor.rgb);
                    if(!water)albedo=LabSnowAlbedo(LabWetAlbedo(albedo,emissive),worldNormal,emissive);
                    gloss=LabCloudShade(worldPos);
                    #if defined(_LAB_WATER)
                    if(water)albedo=LabWaterGlint(albedo,worldPos,_LabWaterFlow,_LabWaterBounds,DetailBlend());
                    #endif
                }
                half3 emission=_EmissionColor.rgb;
                float3 view=normalize(_WorldSpaceCameraPos-worldPos);
                Light sun=GetMainLight(TransformWorldToShadowCoord(worldPos));
                if(_LabCharacter>.5)emission+=LabCharacterLight(albedo,worldNormal,view,sun.direction,_CharacterRim,_CharacterBounce);

                // Planar reflections.
                #if defined(_LAB_PUDDLES)
                if((puddle.pool>.001||puddle.wetGround>.001)&&_LabReflectionPass<.5)
                {
                    half3 reflected=SAMPLE_TEXTURE2D(_PuddleReflection,sampler_PuddleReflection,LabPuddleReflectionUV(worldPos,normalTS)).rgb;
                    LabPuddleReflect(puddle,reflected,geometryNormal,view,albedo,emission);
                }
                #endif
                #if defined(_LAB_WATER)&&defined(_LAB_SEMIREAL)
                if(water&&_SemiPBR>.01&&_LabReflectionPass<.5)
                {
                    half3 reflection=SAMPLE_TEXTURE2D(_DetailReflection,sampler_DetailReflection,LabWaterReflectionUV(worldPos,normalTS)).rgb;
                    emission+=LabWaterReflection(reflection,worldNormal,view,_ReflectionStrength);
                }
                #endif

                // Lighting: sun/moon, wet sheen, lamps.
                LabToonSurface s;s.albedo=albedo;s.normalWS=TangentToWorld(i,normalTS);s.rough=rough;s.gloss=gloss;
                s.shadeColor=_ShadeColor.rgb;s.metallic=_Metallic;s.twoSided=_LeafTwoSided;s.detail=DetailBlend();
                half sunAtten=sun.distanceAttenuation*sun.shadowAttenuation;
                half3 color=LabSunLight(s,sun.direction,view,sunAtten,sun.color);
                color+=sun.color*LabWetSheen(s.normalWS,sun.direction,view,LabToonNdotL(s,sun.direction),sunAtten)+emission;
                half3 lampGlow=0;
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData=(InputData)0;inputData.positionWS=worldPos;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount=GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light light=GetAdditionalLight(lightIndex,worldPos,half4(1,1,1,1));
                    half falloff=LabLegacyAttenuation(lightIndex,worldPos);
                    color+=LabLocalLight(s,light.direction,view,falloff*light.shadowAttenuation,light.color,_LocalLightStrength);
                    // Haze scatters lamp light regardless of what blocks the surface.
                    lampGlow+=light.color*falloff;
                LIGHT_LOOP_END
                #endif
                // Characters take a lighter haze so they stay readable against the background (L8).
                color=LabApplyHeightFog(color,worldPos,lampGlow,_LabCharacter>.5?.35:1);
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
            #pragma shader_feature_local_fragment _LAB_ALPHACLIP
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;float3 _LightPosition;
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings ShadowVert(Attributes v)
            {
                TOON_DEFORM(v);
                float3 positionWS=TransformObjectToWorld(v.positionOS.xyz),normalWS=TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection=normalize(_LightPosition-positionWS);
                #else
                float3 lightDirection=_LightDirection;
                #endif
                Varyings o;o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS,normalWS,lightDirection)));
                o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;
            }
            half4 ShadowFrag(Varyings i):SV_Target{ToonClip(i.uv);return 0;}
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
            #pragma shader_feature_local_fragment _LAB_ALPHACLIP
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;};
            Varyings DepthVert(Attributes v)
            {
                TOON_DEFORM(v);
                Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;
            }
            half DepthFrag(Varyings i):SV_Target{ToonClip(i.uv);LabVisibilityClip(i.positionWS,i.positionCS.xy,_LabRevealAmount);return i.positionCS.z;}
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
            #pragma shader_feature_local_fragment _LAB_ALPHACLIP
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionWS:TEXCOORD1;float3 normalWS:TEXCOORD2;};
            Varyings DepthNormalsVert(Attributes v)
            {
                TOON_DEFORM(v);
                Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;
            }
            half4 DepthNormalsFrag(Varyings i):SV_Target{ToonClip(i.uv);LabVisibilityClip(i.positionWS,i.positionCS.xy,_LabRevealAmount);return half4(normalize(i.normalWS),0);}
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
            #pragma shader_feature_local_fragment _LAB_ALPHACLIP
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            Varyings vert(Attributes v){TOON_DEFORM(v);Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
            half4 frag(Varyings i):SV_Target{ToonClip(i.uv);return 0;}
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
    CustomEditor "PixelArtLab.Editor.SpriteToonGUI"
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
