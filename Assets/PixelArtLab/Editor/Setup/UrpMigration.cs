using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PixelArtLab.Editor
{
    // Moves the project from the Built-in pipeline to a PC-tuned URP asset (see
    // Docs/Render-Pipeline-Decision.md). Safe to re-run: assets are reused and
    // converted materials are skipped.
    public static class UrpMigration
    {
        public const string SettingsFolder="Assets/PixelArtLab/Settings";
        public const string PipelinePath=SettingsFolder+"/RP_PixelArtLab_PC.asset",RendererPath=SettingsFolder+"/RD_PixelArtLab_PC.asset";
        [MenuItem("Pixel Art Lab/Set Up PC URP")]
        public static void Setup()
        {
            Directory.CreateDirectory(SettingsFolder);
            // Earlier names (mobile first, then PC under Assets/Settings): rename in place so
            // GUID references from Graphics/Quality settings hold.
            foreach(var (oldPipeline,oldRenderer) in new[]{("Assets/Settings/PixelArtLab-Mobile-URP.asset","Assets/Settings/PixelArtLab-Mobile-Renderer.asset"),("Assets/Settings/PixelArtLab-PC-URP.asset","Assets/Settings/PixelArtLab-PC-Renderer.asset")})
            {
                if(File.Exists(oldPipeline))AssetDatabase.MoveAsset(oldPipeline,PipelinePath);
                if(File.Exists(oldRenderer))AssetDatabase.MoveAsset(oldRenderer,RendererPath);
            }
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if(!renderer){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,RendererPath);}
            ConfigureRenderer(renderer);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if(!pipeline){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,PipelinePath);}
            ConfigurePipeline(pipeline,renderer);
            GraphicsSettings.defaultRenderPipeline=pipeline;
            int level=QualitySettings.GetQualityLevel();
            for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=pipeline;}
            QualitySettings.SetQualityLevel(level,false);
            AssetDatabase.SaveAssets();
            ConvertStandardMaterials();
            AssetDatabase.SaveAssets();
            Debug.Log("URP_MIGRATION_SUCCESS "+PipelinePath);
        }
        static void ConfigureRenderer(UniversalRendererData renderer)
        {
            var so=new SerializedObject(renderer);
            // Forward+: no per-object light limit. The ground is one 150m mesh, so plain
            // Forward capped every practical light on the map at four.
            Set(so,"m_RenderingMode",p=>p.intValue=2);
            Set(so,"m_DepthPrimingMode",p=>p.intValue=0);
            // A renderer made with CreateInstance has no post-process resources, and URP then
            // silently skips every post effect.
            if(!renderer.postProcessData)renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            var features=so.FindProperty("m_RendererFeatures");var map=so.FindProperty("m_RendererFeatureMap");
            bool present=false;for(int i=0;i<features.arraySize;i++)present|=features.GetArrayElementAtIndex(i).objectReferenceValue is PixelArtLabRendererFeature;
            if(!present)
            {
                var feature=ScriptableObject.CreateInstance<PixelArtLabRendererFeature>();feature.name="PixelArtLab Linework and Visibility";
                AssetDatabase.AddObjectToAsset(feature,renderer);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature,out string _,out long localId);
                features.arraySize++;features.GetArrayElementAtIndex(features.arraySize-1).objectReferenceValue=feature;
                map.arraySize++;map.GetArrayElementAtIndex(map.arraySize-1).longValue=localId;
            }
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(renderer);
        }
        static void ConfigurePipeline(UniversalRenderPipelineAsset pipeline,UniversalRendererData renderer)
        {
            var so=new SerializedObject(pipeline);
            // Pixel art: 1x MSAA, native scale. HDR so flames and torch pools exceed 1 and the
            // pixel bloom has light to spread; the buffer is ~640x360, so the cost is small.
            // Depth/opaque copies are not used by any pass yet.
            Set(so,"m_MSAA",p=>p.intValue=1);Set(so,"m_SupportsHDR",p=>p.boolValue=true);Set(so,"m_RenderScale",p=>p.floatValue=1);
            Set(so,"m_RequireDepthTexture",p=>p.boolValue=false);Set(so,"m_RequireOpaqueTexture",p=>p.boolValue=false);
            Set(so,"m_UseSRPBatcher",p=>p.boolValue=true);
            // The follow camera sits 45m back, so visible ground spans roughly 30-60m of depth.
            // One 80m cascade covers it whole: a cascade split inside the view slid across the
            // ground with the camera and re-shaped shadow edges as it passed.
            Set(so,"m_MainLightRenderingMode",p=>p.intValue=1);Set(so,"m_MainLightShadowsSupported",p=>p.boolValue=true);
            Set(so,"m_MainLightShadowmapResolution",p=>p.intValue=4096);
            Set(so,"m_ShadowDistance",p=>p.floatValue=80);Set(so,"m_ShadowCascadeCount",p=>p.intValue=1);
            Set(so,"m_SoftShadowsSupported",p=>p.boolValue=false);
            // Per-pixel practical lights; the nearest ones cast 512px point shadows (PracticalLightManager).
            Set(so,"m_AdditionalLightsRenderingMode",p=>p.intValue=1);Set(so,"m_AdditionalLightsPerObjectLimit",p=>p.intValue=8);
            Set(so,"m_AdditionalLightShadowsSupported",p=>p.boolValue=true);Set(so,"m_AdditionalLightsShadowmapResolution",p=>p.intValue=4096);
            Set(so,"m_AdditionalLightsShadowResolutionTierLow",p=>p.intValue=256);Set(so,"m_AdditionalLightsShadowResolutionTierMedium",p=>p.intValue=512);
            Set(so,"m_AdditionalLightsShadowResolutionTierHigh",p=>p.intValue=512);
            var list=so.FindProperty("m_RendererDataList");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
            Set(so,"m_DefaultRendererIndex",p=>p.intValue=0);
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
        }
        static void Set(SerializedObject so,string name,Action<SerializedProperty> apply)
        {
            var property=so.FindProperty(name);
            if(property==null){Debug.LogWarning("URP setting not found: "+name);return;}
            apply(property);
        }
        // The first lighting study still uses Unity's Standard shader.
        static void ConvertStandardMaterials()
        {
            var lit=Shader.Find("Universal Render Pipeline/Lit");
            foreach(var path in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material||material.shader.name!="Standard")continue;
                Color color=material.GetColor("_Color"),emission=material.GetColor("_EmissionColor");Texture albedo=material.GetTexture("_MainTex");
                float smoothness=material.GetFloat("_Glossiness"),metallic=material.GetFloat("_Metallic");bool emissive=material.IsKeywordEnabled("_EMISSION");
                material.shader=lit;
                material.SetColor("_BaseColor",color);material.SetTexture("_BaseMap",albedo);material.SetFloat("_Smoothness",smoothness);material.SetFloat("_Metallic",metallic);
                material.SetColor("_EmissionColor",emission);if(emissive)material.EnableKeyword("_EMISSION");
                EditorUtility.SetDirty(material);Debug.Log("Converted to URP Lit: "+path);
            }
        }
        // Scene captures live with each sample (e.g. BackgroundLighting.Capture for the woodland).
    }
}
