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
    // One-time move from the Built-in pipeline to a mobile-tuned URP asset.
    // Safe to re-run: existing assets are reused and converted materials are skipped.
    public static class UrpMigration
    {
        public const string PipelinePath="Assets/Settings/PixelArtLab-Mobile-URP.asset",RendererPath="Assets/Settings/PixelArtLab-Mobile-Renderer.asset";
        [MenuItem("Pixel Art Lab/Set Up Mobile URP")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Settings");
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
            Set(so,"m_RenderingMode",p=>p.intValue=0);      // Forward: per-object light limit, cheapest on tile GPUs
            Set(so,"m_DepthPrimingMode",p=>p.intValue=0);
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
            // Pixel art: 1x MSAA, LDR, native scale. Depth/opaque copies are not used by any pass.
            Set(so,"m_MSAA",p=>p.intValue=1);Set(so,"m_SupportsHDR",p=>p.boolValue=false);Set(so,"m_RenderScale",p=>p.floatValue=1);
            Set(so,"m_RequireDepthTexture",p=>p.boolValue=false);Set(so,"m_RequireOpaqueTexture",p=>p.boolValue=false);
            Set(so,"m_UseSRPBatcher",p=>p.boolValue=true);
            // The follow camera sits 45m away; a 42m shadow distance cannot reach the ground.
            Set(so,"m_MainLightRenderingMode",p=>p.intValue=1);Set(so,"m_MainLightShadowsSupported",p=>p.boolValue=true);
            Set(so,"m_MainLightShadowmapResolution",p=>p.intValue=2048);
            Set(so,"m_ShadowDistance",p=>p.floatValue=120);Set(so,"m_ShadowCascadeCount",p=>p.intValue=4);
            Set(so,"m_SoftShadowsSupported",p=>p.boolValue=false);
            // Up to four per-pixel torches; the two nearest cast 512px point shadows.
            Set(so,"m_AdditionalLightsRenderingMode",p=>p.intValue=1);Set(so,"m_AdditionalLightsPerObjectLimit",p=>p.intValue=4);
            Set(so,"m_AdditionalLightShadowsSupported",p=>p.boolValue=true);Set(so,"m_AdditionalLightsShadowmapResolution",p=>p.intValue=2048);
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

        // Renders the woodland scene to Previews/URP for a visual check against the Built-in previews.
        public static void Capture()
        {
            Directory.CreateDirectory("Previews/URP");
            EditorSceneManager.OpenScene(DetailedWoodlandBuilder.ScenePath);
            var env=UnityEngine.Object.FindFirstObjectByType<WorldEnvironment>();env.runClock=false;env.animationTime=2;
            var camera=env.worldCamera;var target=new RenderTexture(640,360,24){filterMode=FilterMode.Point};target.Create();
            camera.targetTexture=target;camera.aspect=640f/360;
            var reflections=UnityEngine.Object.FindObjectsByType<DetailWaterReflection>(FindObjectsSortMode.None);
            void Shot(string name,Vector3 focus,float size,float hour,ValleyWeather weather)
            {
                env.hour=hour;env.SetWeather(weather,true);camera.orthographicSize=size;camera.transform.position=focus-camera.transform.forward*45;
                foreach(var reflection in reflections)reflection.Refresh();
                LabCameraRender.Render(camera);
                var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(640,360,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,640,360),0,0);texture.Apply();RenderTexture.active=previous;
                File.WriteAllBytes("Previews/URP/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
            }
            Shot("Woodland-Noon",new Vector3(-5,2,2),5.8f,13,ValleyWeather.Clear);
            Shot("Lake-Noon",new Vector3(-30,1,22),9,13,ValleyWeather.Clear);
            Shot("Woodland-Night",new Vector3(-5,2,2),5.8f,0,ValleyWeather.Clear);
            Shot("Woodland-Sunset",new Vector3(-5,2,2),5.8f,17.5f,ValleyWeather.Clear);
            Shot("Rain-Puddles",new Vector3(-5,2,2),7,13,ValleyWeather.Rain);
            Shot("Snow",new Vector3(-5,2,2),7,13,ValleyWeather.Snow);
            camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
            Debug.Log("URP_CAPTURE_SUCCESS");
        }
    }
}
