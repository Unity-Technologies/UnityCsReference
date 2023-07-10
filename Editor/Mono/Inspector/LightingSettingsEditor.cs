// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(LightingSettings))]
    [CanEditMultipleObjects]
    internal class LightingSettingsEditor : Editor
    {
        SharedLightingSettingsEditor m_Editor;

        public void OnEnable()
        {
            m_Editor = new SharedLightingSettingsEditor();
            m_Editor.OnEnable();
            m_Editor.UpdateSettings(serializedObject);
            m_Editor.ClampMaxRanges();
        }

        internal override void OnHeaderControlsGUI()
        {
            GUILayoutUtility.GetRect(10, 10, 16, 16, EditorStyles.layerMaskField);
            GUILayout.FlexibleSpace();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_Editor.OnGUI(true, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnFocus()
        {
            m_Editor.ClampMaxRanges();
        }
    }

    internal class SharedLightingSettingsEditor
    {
        SavedBool m_ShowRealtimeLightsSettings;
        SavedBool m_ShowMixedLightsSettings;
        SavedBool m_ShowGeneralLightingSettings;
        SavedBool m_ShowInternalSettings;

        //realtime GI
        SerializedProperty m_EnableRealtimeGI;
        SerializedProperty m_RealtimeResolution;
        SerializedProperty m_RealtimeEnvironmentLighting;

        //baked
        SerializedProperty m_EnabledBakedGI;
        SerializedProperty m_MixedBakeMode;
        SerializedProperty m_AlbedoBoost;
        SerializedProperty m_IndirectOutputScale;
        SerializedProperty m_LightmapParameters;
        SerializedProperty m_LightmapDirectionalMode;
        SerializedProperty m_BakeResolution;
        SerializedProperty m_Padding;
        SerializedProperty m_AmbientOcclusion;
        SerializedProperty m_AOMaxDistance;
        SerializedProperty m_CompAOExponent;
        SerializedProperty m_CompAOExponentDirect;
        SerializedProperty m_LightmapCompression;
        SerializedProperty m_LightmapMaxSize;
        SerializedProperty m_LightmapSizeFixed;
        SerializedProperty m_UseMipmapLimits;
        SerializedProperty m_BakeBackend;
        // pvr
        SerializedProperty m_PVRSampleCount;
        SerializedProperty m_PVRDirectSampleCount;
        SerializedProperty m_PVRBounces;
        SerializedProperty m_PVRCulling;
        SerializedProperty m_PVRFilteringMode;
        SerializedProperty m_PVRFilterTypeDirect;
        SerializedProperty m_PVRFilterTypeIndirect;
        SerializedProperty m_PVRFilterTypeAO;
        SerializedProperty m_PVRDenoiserTypeDirect;
        SerializedProperty m_PVRDenoiserTypeIndirect;
        SerializedProperty m_PVRDenoiserTypeAO;
        SerializedProperty m_PVRFilteringGaussRadiusDirect;
        SerializedProperty m_PVRFilteringGaussRadiusIndirect;
        SerializedProperty m_PVRFilteringGaussRadiusAO;
        SerializedProperty m_PVRFilteringAtrousPositionSigmaDirect;
        SerializedProperty m_PVRFilteringAtrousPositionSigmaIndirect;
        SerializedProperty m_PVRFilteringAtrousPositionSigmaAO;
        SerializedProperty m_PVREnvironmentIS;
        SerializedProperty m_PVREnvironmentSampleCount;
        SerializedProperty m_LightProbeSampleCountMultiplier;

        // internal
        SerializedProperty m_BounceScale;
        SerializedProperty m_ExportTrainingData;
        SerializedProperty m_TrainingDataDestination;
        SerializedProperty m_ForceWhiteAlbedo;
        SerializedProperty m_ForceUpdates;
        SerializedProperty m_FilterMode;
        SerializedProperty m_RespectSceneVisibilityWhenBakingGI;

        enum DenoiserTarget
        {
            Direct = 0,
            Indirect = 1,
            AO = 2
        }

        static class Styles
        {
            public static readonly float buttonWidth = 200;

            public static readonly int[] bakeBackendValues = { (int)LightingSettings.Lightmapper.ProgressiveCPU, (int)LightingSettings.Lightmapper.ProgressiveGPU };
            public static readonly GUIContent[] bakeBackendStrings =
            {
                EditorGUIUtility.TrTextContent("Progressive CPU"),
                EditorGUIUtility.TrTextContent("Progressive GPU"),
            };

            public static readonly int[] lightmapDirectionalModeValues = { (int)LightmapsMode.NonDirectional, (int)LightmapsMode.CombinedDirectional };
            public static readonly GUIContent[] lightmapDirectionalModeStrings =
            {
                EditorGUIUtility.TrTextContent("Non-Directional"),
                EditorGUIUtility.TrTextContent("Directional"),
            };

            public static readonly int[] lightmapMaxSizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
            public static readonly GUIContent[] lightmapMaxSizeStrings = Array.ConvertAll(lightmapMaxSizeValues, (x) => new GUIContent(x.ToString()));

            // must match LightmapMixedBakeMode
            public static readonly int[] mixedModeValues = { 0, 1, 2 };
            public static readonly GUIContent[] mixedModeStrings =
            {
                EditorGUIUtility.TrTextContent("Baked Indirect"),
                EditorGUIUtility.TrTextContent("Subtractive"),
                EditorGUIUtility.TrTextContent("Shadowmask")
            };

            // must match PVRDenoiserType
            public static readonly int[] denoiserTypeValues = { (int)LightingSettings.DenoiserType.Optix, (int)LightingSettings.DenoiserType.OpenImage, (int)LightingSettings.DenoiserType.None };
            public static readonly GUIContent[] denoiserTypeStrings =
            {
                EditorGUIUtility.TrTextContent("Optix"),
                EditorGUIUtility.TrTextContent("OpenImageDenoise"),
                EditorGUIUtility.TrTextContent("None")
            };

            public static readonly int[] lightmapCompressionValues =
            {
                (int)LightmapCompression.None,
                (int)LightmapCompression.LowQuality,
                (int)LightmapCompression.NormalQuality,
                (int)LightmapCompression.HighQuality
            };
            public static readonly GUIContent[] lightmapCompressionStrings =
            {
                EditorGUIUtility.TrTextContent("None"),
                EditorGUIUtility.TrTextContent("Low Quality"),
                EditorGUIUtility.TrTextContent("Normal Quality"),
                EditorGUIUtility.TrTextContent("High Quality")
            };

            public static readonly int[] concurrentJobsTypeValues = { (int)Lightmapping.ConcurrentJobsType.Min, (int)Lightmapping.ConcurrentJobsType.Low, (int)Lightmapping.ConcurrentJobsType.High };
            public static readonly GUIContent[] concurrentJobsTypeStrings =
            {
                EditorGUIUtility.TrTextContent("Min"),
                EditorGUIUtility.TrTextContent("Low"),
                EditorGUIUtility.TrTextContent("High")
            };

            public static readonly GUIContent lightmapperNotSupportedWarning = EditorGUIUtility.TrTextContent("This lightmapper is not supported by the current Render Pipeline. The Editor will use ");
            public static readonly GUIContent appleSiliconLightmapperWarning = EditorGUIUtility.TrTextContent("Progressive CPU Lightmapper is not available on Apple silicon. Use Progressive GPU Lightmapper instead.");
            public static readonly GUIContent mixedModeNotSupportedWarning = EditorGUIUtility.TrTextContent("The Mixed mode is not supported by the current Render Pipeline. Fallback mode is ");
            public static readonly GUIContent directionalNotSupportedWarning = EditorGUIUtility.TrTextContent("Directional Mode is not supported. Fallback will be Non-Directional.");
            public static readonly GUIContent denoiserNotSupportedWarning = EditorGUIUtility.TrTextContent("The current hardware or system configuration does not support the selected denoiser. Select a different denoiser.");

            public static readonly GUIContent enableBaked = EditorGUIUtility.TrTextContent("Baked Global Illumination", "Controls whether Mixed and Baked lights will use baked Global Illumination. If enabled, Mixed lights are baked using the specified Lighting Mode and Baked lights will be completely baked and not adjustable at runtime.");
            public static readonly GUIContent bounceScale = EditorGUIUtility.TrTextContent("Bounce Scale", "Multiplier for indirect lighting. Use with care.");
            public static readonly GUIContent updateThreshold = EditorGUIUtility.TrTextContent("Update Threshold", "Threshold for updating realtime GI. A lower value causes more frequent updates (default 1.0).");
            public static readonly GUIContent albedoBoost = EditorGUIUtility.TrTextContent("Albedo Boost", "Controls the amount of light bounced between surfaces by intensifying the albedo of materials in the scene. Increasing this draws the albedo value towards white for indirect light computation. The default value is physically accurate.");
            public static readonly GUIContent indirectOutputScale = EditorGUIUtility.TrTextContent("Indirect Intensity", "Controls the brightness of indirect light stored in realtime and baked lightmaps. A value above 1.0 will increase the intensity of indirect light while a value less than 1.0 will reduce indirect light intensity.");
            public static readonly GUIContent lightmapDirectionalMode = EditorGUIUtility.TrTextContent("Directional Mode", "Controls whether baked and realtime lightmaps will store directional lighting information from the lighting environment. Options are Directional and Non-Directional.");
            public static readonly GUIContent lightmapParameters = EditorGUIUtility.TrTextContent("Lightmap Parameters", "Allows the adjustment of advanced parameters that affect the process of generating a lightmap for an object using global illumination.");
            public static readonly GUIContent newLightmapParameters = EditorGUIUtility.TrTextContent("New", "Create a new Lightmap Parameters Asset with default settings.");
            public static readonly GUIContent cloneLightmapParameters = EditorGUIUtility.TrTextContent("Clone", "Create a new Lightmap Parameters Asset based on the current settings.");
            public static readonly GUIContent realtimeLightsLabel = EditorGUIUtility.TrTextContent("Realtime Lighting", "Precompute Realtime indirect lighting for realtime lights and static objects. In this mode realtime lights, ambient lighting, materials of static objects (including emission) will generate indirect lighting at runtime. Only static objects are blocking and bouncing light, dynamic objects receive indirect lighting via light probes.");
            public static readonly GUIContent realtimeEnvironmentLighting = EditorGUIUtility.TrTextContent("Realtime Environment Lighting", "Specifies the Global Illumination mode that should be used for handling ambient light in the Scene. This property is not editable unless both Realtime Global Illumination and Baked Global Illumination are enabled for the scene.");
            public static readonly GUIContent mixedLightsLabel = EditorGUIUtility.TrTextContent("Mixed Lighting", "Bake Global Illumination for mixed lights and static objects. May bake both direct and/or indirect lighting based on settings. Only static objects are blocking and bouncing light, dynamic objects receive baked lighting via light probes.");
            public static readonly GUIContent generalLightmapLabel = EditorGUIUtility.TrTextContent("Lightmapping Settings", "Settings that apply to both Global Illumination modes (Precomputed Realtime and Baked).");
            public static readonly GUIContent internalLabel = EditorGUIUtility.TrTextContent("Internal Settings", "Internal only settings. ");
            public static readonly GUIContent forceWhiteAlbedo = EditorGUIUtility.TrTextContent("Force White Albedo", "Force white albedo during lighting calculations.");
            public static readonly GUIContent forceUpdates = EditorGUIUtility.TrTextContent("Force Updates", "Force continuous updates of runtime indirect lighting calculations.");
            public static readonly GUIContent filterMode = EditorGUIUtility.TrTextContent("Filter Mode");
            public static readonly GUIContent exportTrainingData = EditorGUIUtility.TrTextContent("Export Training Data", "Exports unfiltered textures, normals, positions.");
            public static readonly GUIContent trainingDataDestination = EditorGUIUtility.TrTextContent("Destination", "Destination for the training data, for example 'mysetup/30samples'. Will still be located at the first level in the project folder. ");
            public static readonly GUIContent concurrentJobs = EditorGUIUtility.TrTextContent("Concurrent Jobs", "The amount of simultaneously scheduled jobs.");
            public static readonly GUIContent indirectResolution = EditorGUIUtility.TrTextContent("Indirect Resolution", "Sets the resolution in texels that are used per unit for objects being lit by indirect lighting. The higher this value is, the more time the Editor needs to bake lighting.");
            public static readonly GUIContent lightmapResolution = EditorGUIUtility.TrTextContent("Lightmap Resolution", "Sets the resolution in texels used per unit for objects lit by baked global illumination. The higher this value is, the more time the Editor needs to bake lighting.");
            public static readonly GUIContent padding = EditorGUIUtility.TrTextContent("Lightmap Padding", "Sets the separation in texels between shapes in the baked lightmap.");
            public static readonly GUIContent lightmapMaxSize = EditorGUIUtility.TrTextContent("Max Lightmap Size", "Sets the max size of the full lightmap Texture in pixels. Values are squared, so a setting of 1024 can produce a 1024x1024 pixel sized lightmap.");
            public static readonly GUIContent lightmapSizeFixed = EditorGUIUtility.TrTextContent("Fixed Lightmap Size", "Forces all lightmap textures to use the same size. These can be no larger than Max Lightmap Size.");
            public static readonly GUIContent useMipmapLimits = EditorGUIUtility.TrTextContent("Use Mipmap Limits", "Whether lightmap textures use the Global Mipmap limit defined in Quality Settings. Disable this to ensure lightmaps are available at the full mipmap resolution.");
            public static readonly GUIContent lightmapCompression = EditorGUIUtility.TrTextContent("Lightmap Compression", "Compresses baked lightmaps created using this Lighting Settings Asset. Lower quality compression reduces memory and storage requirements, at the cost of more visual artifacts. Higher quality compression requires more memory and storage, but provides better visual results.");
            public static readonly GUIContent ambientOcclusion = EditorGUIUtility.TrTextContent("Ambient Occlusion", "Specifies whether to include ambient occlusion or not in the baked lightmap result. Enabling this results in simulating the soft shadows that occur in cracks and crevices of objects when light is reflected onto them.");
            public static readonly GUIContent ambientOcclusionContribution = EditorGUIUtility.TrTextContent("Indirect Contribution", "Adjusts the contrast of ambient occlusion applied to indirect lighting. The larger the value, the more contrast is applied to the ambient occlusion for indirect lighting.");
            public static readonly GUIContent ambientOcclusionContributionDirect = EditorGUIUtility.TrTextContent("Direct Contribution", "Adjusts the contrast of ambient occlusion applied to the direct lighting. The larger the value is, the more contrast is applied to the ambient occlusion for direct lighting. This effect is not physically accurate.");
            public static readonly GUIContent AOMaxDistance = EditorGUIUtility.TrTextContent("Max Distance", "Controls how far rays are cast in order to determine if an object is occluded or not. A larger value produces longer rays and contributes more shadows to the lightmap, while a smaller value produces shorter rays that contribute shadows only when objects are very close to one another. A value of 0 casts an infinitely long ray that has no maximum distance.");
            public static readonly GUIContent mixedLightMode = EditorGUIUtility.TrTextContent("Lighting Mode", "Specifies the lighting mode of all Mixed lights in the Scene.");
            public static readonly GUIContent useRealtimeGI = EditorGUIUtility.TrTextContent("Realtime Global Illumination", "Precomputed Realtime Global Illumination using Enlighten. Provides diffuse Realtime Global Illumination for static geometry via low resolution lightmaps and via Light Probes for dynamic geometry.");
            public static readonly GUIContent bakedGIDisabledInfo = EditorGUIUtility.TrTextContent("All Baked and Mixed lights in the Scene are currently being overridden to Realtime light modes. Enable Baked Global Illumination to allow the use of Baked and Mixed light modes.");
            public static readonly GUIContent bakeBackend = EditorGUIUtility.TrTextContent("Lightmapper", "Specifies which baking system will be used to generate baked lightmaps.");
            public static readonly GUIContent directSampleCount = EditorGUIUtility.TrTextContent("Direct Samples", "Controls the number of samples the lightmapper will use for direct lighting calculations. Increasing this value may improve the quality of lightmaps but increases the time required for baking to complete.");
            public static readonly GUIContent indirectSampleCount = EditorGUIUtility.TrTextContent("Indirect Samples", "Controls the number of samples the lightmapper will use for indirect lighting calculations. Increasing this value may improve the quality of lightmaps but increases the time required for baking to complete.");
            public static readonly GUIContent bounces = EditorGUIUtility.TrTextContent("Max Bounces", "The maximum number of bounces the Lightmapper computes for indirect lighting.");
            public static readonly GUIContent denoisingWarningDirect = EditorGUIUtility.TrTextContent("Direct Denoiser", "Your hardware does not support denoising. For minimum requirements, please read the documentation.");
            public static readonly GUIContent denoisingWarningIndirect = EditorGUIUtility.TrTextContent("Indirect Denoiser", "Your hardware does not support denoising. For minimum requirements, please read the documentation.");
            public static readonly GUIContent denoisingWarningAO = EditorGUIUtility.TrTextContent("Ambient Occlusion Denoiser", "Your hardware Your hardware does not support denoising. For minimum requirements, please read the documentation.");
            public static readonly GUIContent denoiserTypeDirect = EditorGUIUtility.TrTextContent("Direct Denoiser", "Specifies the type of denoiser used to reduce noise for direct lights.");
            public static readonly GUIContent denoiserTypeIndirect = EditorGUIUtility.TrTextContent("Indirect Denoiser", "Specifies the type of denoiser used to reduce noise for indirect lights.");
            public static readonly GUIContent denoiserTypeAO = EditorGUIUtility.TrTextContent("Ambient Occlusion Denoiser", "Specifies the type of denoiser used to reduce noise for ambient occlusion.");
            public static readonly GUIContent filteringMode = EditorGUIUtility.TrTextContent("Filtering", "Specifies the method to reduce noise in baked lightmaps.");
            public static readonly GUIContent filterTypeDirect = EditorGUIUtility.TrTextContent("Direct Filter", "Specifies the filter kernel applied to the direct light stored in the lightmap. Gaussian blurs the lightmap with some loss of detail. A-Trous reduces noise based on a threshold while maintaining edge detail.");
            public static readonly GUIContent filterTypeIndirect = EditorGUIUtility.TrTextContent("Indirect Filter", "Specifies the filter kernel applied to the indirect light stored in the lightmap. Gaussian blurs the lightmap with some loss of detail. A-Trous reduces noise based on a threshold while maintaining edge detail.");
            public static readonly GUIContent filterTypeAO = EditorGUIUtility.TrTextContent("Ambient Occlusion Filter", "Specifies the filter kernel applied to the ambient occlusion stored in the lightmap. Gaussian blurs the lightmap with some loss of detail. A-Trous reduces noise based on a threshold while maintaining edge detail.");
            public static readonly GUIContent filteringGaussRadiusDirect = EditorGUIUtility.TrTextContent("Radius", "Controls the radius of the filter for direct light stored in the lightmap. A higher value gives a stronger blur and less noise.");
            public static readonly GUIContent filteringGaussRadiusIndirect = EditorGUIUtility.TrTextContent("Radius", "Controls the radius of the filter for indirect light stored in the lightmap. A higher value gives a stronger blur and less noise.");
            public static readonly GUIContent filteringGaussRadiusAO = EditorGUIUtility.TrTextContent("Radius", "Controls the radius of the filter for ambient occlusion stored in the lightmap. A higher value gives a stronger blur and less noise.");
            public static readonly GUIContent filteringAtrousPositionSigmaDirect = EditorGUIUtility.TrTextContent("Sigma", "Controls the threshold of the filter for direct light stored in the lightmap. A higher value increases the threshold, which reduces noise in the direct layer of the lightmap. Too high of a value can cause a loss of detail in the lightmap.");
            public static readonly GUIContent filteringAtrousPositionSigmaIndirect = EditorGUIUtility.TrTextContent("Sigma", "Controls the threshold of the filter for indirect light stored in the lightmap. A higher value increases the threshold, which reduces noise in the direct layer of the lightmap. Too high of a value can cause a loss of detail in the lightmap.");
            public static readonly GUIContent filteringAtrousPositionSigmaAO = EditorGUIUtility.TrTextContent("Sigma", "Controls the threshold of the filter for ambient occlusion stored in the lightmap. A higher value increases the threshold, which reduces noise in the direct layer of the lightmap. Too high of a value can cause a loss of detail in the lightmap.");
            public static readonly GUIContent culling = EditorGUIUtility.TrTextContent("Progressive Updates", "Specifies whether the lightmapper should prioritize baking what is visible in the scene view. When disabled, lightmaps are only composited once fully converged which can improve baking performance.");
            public static readonly GUIContent environmentImportanceSampling = EditorGUIUtility.TrTextContent("Importance Sampling", "Specifies whether to use importance sampling for sampling environment lighting. In most environments importance sampling facilitates faster convergence while generating lightmaps. In certain low frequency environments, importance sampling can produce noisy results.");
            public static readonly GUIContent environmentSampleCount = EditorGUIUtility.TrTextContent("Environment Samples", "Controls the number of samples the lightmapper will use for environment lighting calculations. Increasing this value may improve the quality of lightmaps but increases the time required for baking to complete.");
            public static readonly GUIContent probeSampleCountMultiplier = EditorGUIUtility.TrTextContent("Light Probe Sample Multiplier", "Controls how many samples are used for Light Probes as a multiplier of the general sample counts above. Higher values improve the quality of Light Probes, but also take longer to bake. Enable the Light Probe sample count multiplier by disabling Project Settings > Editor > Use legacy Light Probe sample counts");
            public static readonly GUIContent texelsPerUnit = EditorGUIUtility.TrTextContent(" texels per unit");
            public static readonly GUIContent texels = EditorGUIUtility.TrTextContent(" texels");
            public static readonly GUIContent sigma = EditorGUIUtility.TrTextContent(" sigma");

            public static readonly GUIStyle labelStyle = EditorStyles.wordWrappedMiniLabel;
        }

        private int maxDirectSamples = 1024;
        private int maxIndirectSamples = 8192;
        private int maxEnvironmentSamples = 2048;
        private SerializedObject currentLSO;

        public void OnEnable()
        {
            m_ShowRealtimeLightsSettings = new SavedBool("LightingSettings.ShowRealtimeLightsSettings", false);
            m_ShowMixedLightsSettings = new SavedBool("LightingSettings.ShowMixedLightsSettings", true);
            m_ShowGeneralLightingSettings = new SavedBool("LightingSettings.ShowGeneralLightingSettings", true);
            m_ShowInternalSettings = new SavedBool("LightingSettings.ShowInternalSettings", true);
        }

        public void OnGUI(bool compact, bool drawAutoGenerate)
        {
            if (currentLSO == null || currentLSO != m_EnabledBakedGI.serializedObject)
            {
                currentLSO = m_EnabledBakedGI.serializedObject;
                ClampMaxRanges();
            }

            RealtimeLightingGUI(compact);
            MixedLightingGUI(compact);
            GeneralLightmapSettingsGUI(compact);
            InternalSettingsGUI(compact);
        }

        public void UpdateSettings(SerializedObject lightingSettingsObject)
        {
            if (lightingSettingsObject == null)
                return;

            //realtime GI
            m_RealtimeResolution = lightingSettingsObject.FindProperty("m_RealtimeResolution");
            m_EnableRealtimeGI = lightingSettingsObject.FindProperty("m_EnableRealtimeLightmaps");
            m_RealtimeEnvironmentLighting = lightingSettingsObject.FindProperty("m_RealtimeEnvironmentLighting");

            //baked
            m_EnabledBakedGI = lightingSettingsObject.FindProperty("m_EnableBakedLightmaps");
            m_BakeBackend = lightingSettingsObject.FindProperty("m_BakeBackend");
            m_MixedBakeMode = lightingSettingsObject.FindProperty("m_MixedBakeMode");
            m_AlbedoBoost = lightingSettingsObject.FindProperty("m_AlbedoBoost");
            m_IndirectOutputScale = lightingSettingsObject.FindProperty("m_IndirectOutputScale");
            m_LightmapMaxSize = lightingSettingsObject.FindProperty("m_LightmapMaxSize");
            m_LightmapSizeFixed = lightingSettingsObject.FindProperty("m_LightmapSizeFixed");
            m_UseMipmapLimits = lightingSettingsObject.FindProperty("m_UseMipmapLimits");
            m_LightmapParameters = lightingSettingsObject.FindProperty("m_LightmapParameters");
            m_LightmapDirectionalMode = lightingSettingsObject.FindProperty("m_LightmapsBakeMode");
            m_BakeResolution = lightingSettingsObject.FindProperty("m_BakeResolution");
            m_Padding = lightingSettingsObject.FindProperty("m_Padding");
            m_AmbientOcclusion = lightingSettingsObject.FindProperty("m_AO");
            m_AOMaxDistance = lightingSettingsObject.FindProperty("m_AOMaxDistance");
            m_CompAOExponent = lightingSettingsObject.FindProperty("m_CompAOExponent");
            m_CompAOExponentDirect = lightingSettingsObject.FindProperty("m_CompAOExponentDirect");
            m_LightmapCompression = lightingSettingsObject.FindProperty("m_LightmapCompression");

            // pvr
            m_PVRSampleCount = lightingSettingsObject.FindProperty("m_PVRSampleCount");
            m_PVRDirectSampleCount = lightingSettingsObject.FindProperty("m_PVRDirectSampleCount");
            m_PVRBounces = lightingSettingsObject.FindProperty("m_PVRBounces");
            m_PVRCulling = lightingSettingsObject.FindProperty("m_PVRCulling");
            m_PVRFilteringMode = lightingSettingsObject.FindProperty("m_PVRFilteringMode");
            m_PVRFilterTypeDirect = lightingSettingsObject.FindProperty("m_PVRFilterTypeDirect");
            m_PVRFilterTypeIndirect = lightingSettingsObject.FindProperty("m_PVRFilterTypeIndirect");
            m_PVRFilterTypeAO = lightingSettingsObject.FindProperty("m_PVRFilterTypeAO");
            m_PVRDenoiserTypeDirect = lightingSettingsObject.FindProperty("m_PVRDenoiserTypeDirect");
            m_PVRDenoiserTypeIndirect = lightingSettingsObject.FindProperty("m_PVRDenoiserTypeIndirect");
            m_PVRDenoiserTypeAO = lightingSettingsObject.FindProperty("m_PVRDenoiserTypeAO");
            m_PVRFilteringGaussRadiusDirect = lightingSettingsObject.FindProperty("m_PVRFilteringGaussRadiusDirect");
            m_PVRFilteringGaussRadiusIndirect = lightingSettingsObject.FindProperty("m_PVRFilteringGaussRadiusIndirect");
            m_PVRFilteringGaussRadiusAO = lightingSettingsObject.FindProperty("m_PVRFilteringGaussRadiusAO");
            m_PVRFilteringAtrousPositionSigmaDirect = lightingSettingsObject.FindProperty("m_PVRFilteringAtrousPositionSigmaDirect");
            m_PVRFilteringAtrousPositionSigmaIndirect = lightingSettingsObject.FindProperty("m_PVRFilteringAtrousPositionSigmaIndirect");
            m_PVRFilteringAtrousPositionSigmaAO = lightingSettingsObject.FindProperty("m_PVRFilteringAtrousPositionSigmaAO");
            m_PVREnvironmentIS = lightingSettingsObject.FindProperty("m_PVREnvironmentImportanceSampling");
            m_PVREnvironmentSampleCount = lightingSettingsObject.FindProperty("m_PVREnvironmentSampleCount");
            m_LightProbeSampleCountMultiplier = lightingSettingsObject.FindProperty("m_LightProbeSampleCountMultiplier");

            //dev debug properties
            m_ExportTrainingData = lightingSettingsObject.FindProperty("m_ExportTrainingData");
            m_TrainingDataDestination = lightingSettingsObject.FindProperty("m_TrainingDataDestination");
            m_ForceWhiteAlbedo = lightingSettingsObject.FindProperty("m_ForceWhiteAlbedo");
            m_ForceUpdates = lightingSettingsObject.FindProperty("m_ForceUpdates");
            m_FilterMode = lightingSettingsObject.FindProperty("m_FilterMode");
            m_BounceScale = lightingSettingsObject.FindProperty("m_BounceScale");
            m_RespectSceneVisibilityWhenBakingGI = lightingSettingsObject.FindProperty("m_RespectSceneVisibilityWhenBakingGI");
        }

        // Private methods

        void RealtimeLightingGUI(bool compact)
        {
            // ambient GI - realtime / baked
            bool realtimeGISupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime);

            if (!realtimeGISupported)
                return;

            if (compact)
                m_ShowRealtimeLightsSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowRealtimeLightsSettings.value, Styles.realtimeLightsLabel);
            else
                m_ShowRealtimeLightsSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowRealtimeLightsSettings.value, Styles.realtimeLightsLabel, true);

            if (m_ShowRealtimeLightsSettings.value)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_EnableRealtimeGI, Styles.useRealtimeGI);

                EditorGUI.indentLevel++;

                bool bakedGISupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Baked);
                bool enableRealtimeGI = (m_EnableRealtimeGI.boolValue && !m_EnableRealtimeGI.hasMultipleDifferentValues);
                bool enableBakedGI = (m_EnabledBakedGI.boolValue && !m_EnabledBakedGI.hasMultipleDifferentValues);

                if (enableBakedGI && enableRealtimeGI)
                {
                    // if the user has selected the only state that is supported, then gray it out
                    using (new EditorGUI.DisabledScope(m_RealtimeEnvironmentLighting.boolValue && !bakedGISupported))
                    {
                        EditorGUILayout.PropertyField(m_RealtimeEnvironmentLighting, Styles.realtimeEnvironmentLighting);
                    }

                    // if they have selected a state that isnt supported, show dialog, and still make the box editable
                    if (!m_RealtimeEnvironmentLighting.boolValue && !bakedGISupported)
                    {
                        EditorGUILayout.HelpBox("The following mode is not supported and will fallback on Realtime", MessageType.Warning);
                    }
                }
                // Show "Realtime" on if baked GI is disabled (but we don't wanna show the box if the whole mode is not supported.)
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(m_EnableRealtimeGI, Styles.realtimeEnvironmentLighting);
                    }
                }

                using (new EditorGUI.DisabledScope(!enableRealtimeGI))
                {
                    DrawPropertyFieldWithPostfixLabel(m_RealtimeResolution, Styles.indirectResolution, Styles.texelsPerUnit);
                }

                EditorGUI.indentLevel -= 2;
                EditorGUILayout.Space();
            }

            if (compact)
                EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void MixedLightingGUI(bool compact)
        {
            if (!SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Baked))
                return;

            if (compact)
                m_ShowMixedLightsSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowMixedLightsSettings.value, Styles.mixedLightsLabel);
            else
                m_ShowMixedLightsSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowMixedLightsSettings.value, Styles.mixedLightsLabel, true);

            if (m_ShowMixedLightsSettings.value)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_EnabledBakedGI, Styles.enableBaked);

                if (!m_EnabledBakedGI.boolValue)
                {
                    EditorGUILayout.HelpBox(Styles.bakedGIDisabledInfo.text, MessageType.Info);
                }

                bool enableBakedGI = (m_EnabledBakedGI.boolValue && !m_EnabledBakedGI.hasMultipleDifferentValues);

                using (new EditorGUI.DisabledScope(!enableBakedGI))
                {
                    bool mixedGISupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Mixed);

                    using (new EditorGUI.DisabledScope(!mixedGISupported))
                    {
                        var rect = EditorGUILayout.GetControlRect();
                        EditorGUI.BeginProperty(rect, Styles.mixedLightMode, m_MixedBakeMode);
                        rect = EditorGUI.PrefixLabel(rect, Styles.mixedLightMode);

                        int index = Math.Max(0, Array.IndexOf(Styles.mixedModeValues, m_MixedBakeMode.intValue));

                        if (EditorGUI.DropdownButton(rect, Styles.mixedModeStrings[index], FocusType.Passive))
                        {
                            var menu = new GenericMenu();

                            for (int i = 0; i < Styles.mixedModeValues.Length; i++)
                            {
                                int value = Styles.mixedModeValues[i];
                                bool selected = (value == m_MixedBakeMode.intValue);

                                if (!SupportedRenderingFeatures.IsMixedLightingModeSupported((MixedLightingMode)value))
                                    menu.AddDisabledItem(Styles.mixedModeStrings[i], selected);
                                else
                                    menu.AddItem(Styles.mixedModeStrings[i], selected, OnMixedModeSelected, value);
                            }
                            menu.DropDown(rect);
                        }
                        EditorGUI.EndProperty();

                        if (mixedGISupported && !SupportedRenderingFeatures.IsMixedLightingModeSupported((MixedLightingMode)m_MixedBakeMode.intValue))
                        {
                            string fallbackMode = Styles.mixedModeStrings[(int)SupportedRenderingFeatures.FallbackMixedLightingMode()].text;
                            EditorGUILayout.HelpBox(Styles.mixedModeNotSupportedWarning.text + fallbackMode, MessageType.Warning);
                        }
                    }
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            if (compact)
                EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void GeneralLightmapSettingsGUI(bool compact)
        {
            bool bakedGISupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Baked);
            bool realtimeGISupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime);
            bool lightmapperSupported = SupportedRenderingFeatures.IsLightmapperSupported(m_BakeBackend.intValue);

            if (!bakedGISupported && !realtimeGISupported)
                return;

            if (compact)
                m_ShowGeneralLightingSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowGeneralLightingSettings.value, Styles.generalLightmapLabel);
            else
                m_ShowGeneralLightingSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowGeneralLightingSettings.value, Styles.generalLightmapLabel, true);

            bool enableRealtimeGI = (m_EnableRealtimeGI.boolValue && !m_EnableRealtimeGI.hasMultipleDifferentValues);
            bool enableBakedGI = (m_EnabledBakedGI.boolValue && !m_EnabledBakedGI.hasMultipleDifferentValues);

            if (m_ShowGeneralLightingSettings.value)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(!enableRealtimeGI && !enableBakedGI))
                {
                    if (bakedGISupported)
                    {
                        using (new EditorGUI.DisabledScope(!enableBakedGI))
                        {
                            BakeBackendGUI();

                            if (lightmapperSupported)
                            {
                                EditorGUI.indentLevel++;

                                EditorGUILayout.PropertyField(m_PVREnvironmentIS, Styles.environmentImportanceSampling);

                                MultiEditableLogarithmicIntSlider(m_PVRDirectSampleCount, Styles.directSampleCount, 1, maxDirectSamples, 1, 1 << 30);
                                MultiEditableLogarithmicIntSlider(m_PVRSampleCount, Styles.indirectSampleCount, 1, maxIndirectSamples, 1, 1 << 30);
                                MultiEditableLogarithmicIntSlider(m_PVREnvironmentSampleCount, Styles.environmentSampleCount, 1, maxEnvironmentSamples, 1, 1 << 30);

                                maxDirectSamples = (int)Mathf.ClosestPowerOfTwo(Math.Max(maxDirectSamples, m_PVRDirectSampleCount.intValue));
                                maxIndirectSamples = (int)Mathf.ClosestPowerOfTwo(Math.Max(maxIndirectSamples, m_PVRSampleCount.intValue));

                                using (new EditorGUI.DisabledScope(EditorSettings.useLegacyProbeSampleCount))
                                {
                                    EditorGUILayout.PropertyField(m_LightProbeSampleCountMultiplier, Styles.probeSampleCountMultiplier);
                                }

                                EditorGUILayout.PropertyField(m_PVRBounces, Styles.bounces);

                                // Filtering
                                EditorGUILayout.PropertyField(m_PVRFilteringMode, Styles.filteringMode);

                                if (m_PVRFilteringMode.intValue == (int)LightingSettings.FilterMode.Advanced && !m_PVRFilteringMode.hasMultipleDifferentValues)
                                {
                                    // Check if the platform doesn't support denoising.
                                    bool anyDenoisingSupported = (Lightmapping.IsOptixDenoiserSupported() || Lightmapping.IsOpenImageDenoiserSupported());

                                    EditorGUI.indentLevel++;
                                    using (new EditorGUI.DisabledScope(!anyDenoisingSupported))
                                    {
                                        DrawDenoiserTypeDropdown(m_PVRDenoiserTypeDirect, anyDenoisingSupported ? Styles.denoiserTypeDirect : Styles.denoisingWarningDirect, DenoiserTarget.Direct);

                                        if (anyDenoisingSupported && !DenoiserSupported((LightingSettings.DenoiserType)m_PVRDenoiserTypeDirect.intValue))
                                        {
                                            EditorGUILayout.HelpBox(Styles.denoiserNotSupportedWarning.text, MessageType.Info);
                                        }
                                    }

                                    EditorGUILayout.PropertyField(m_PVRFilterTypeDirect, Styles.filterTypeDirect);

                                    if (!m_PVRFilterTypeDirect.hasMultipleDifferentValues)
                                    {
                                        EditorGUI.indentLevel++;
                                        DrawFilterSettingField(m_PVRFilteringGaussRadiusDirect,
                                            m_PVRFilteringAtrousPositionSigmaDirect,
                                            Styles.filteringGaussRadiusDirect,
                                            Styles.filteringAtrousPositionSigmaDirect,
                                            (LightingSettings.FilterType)m_PVRFilterTypeDirect.intValue);
                                        EditorGUI.indentLevel--;
                                    }

                                    EditorGUILayout.Space();

                                    using (new EditorGUI.DisabledScope(!anyDenoisingSupported))
                                    {
                                        DrawDenoiserTypeDropdown(m_PVRDenoiserTypeIndirect, anyDenoisingSupported ? Styles.denoiserTypeIndirect : Styles.denoisingWarningIndirect, DenoiserTarget.Indirect);

                                        if (anyDenoisingSupported && !DenoiserSupported((LightingSettings.DenoiserType)m_PVRDenoiserTypeIndirect.intValue))
                                        {
                                            EditorGUILayout.HelpBox(Styles.denoiserNotSupportedWarning.text, MessageType.Info);
                                        }
                                    }
                                    EditorGUILayout.PropertyField(m_PVRFilterTypeIndirect, Styles.filterTypeIndirect);

                                    if (!m_PVRFilterTypeIndirect.hasMultipleDifferentValues)
                                    {
                                        EditorGUI.indentLevel++;
                                        DrawFilterSettingField(m_PVRFilteringGaussRadiusIndirect,
                                            m_PVRFilteringAtrousPositionSigmaIndirect,
                                            Styles.filteringGaussRadiusIndirect,
                                            Styles.filteringAtrousPositionSigmaIndirect,
                                            (LightingSettings.FilterType)m_PVRFilterTypeIndirect.intValue);
                                        EditorGUI.indentLevel--;
                                    }

                                    using (new EditorGUI.DisabledScope(!m_AmbientOcclusion.boolValue))
                                    {
                                        EditorGUILayout.Space();

                                        using (new EditorGUI.DisabledScope(!anyDenoisingSupported))
                                        {
                                            DrawDenoiserTypeDropdown(m_PVRDenoiserTypeAO, anyDenoisingSupported ? Styles.denoiserTypeAO : Styles.denoisingWarningAO, DenoiserTarget.AO);

                                            if (m_AmbientOcclusion.boolValue && anyDenoisingSupported && !DenoiserSupported((LightingSettings.DenoiserType)m_PVRDenoiserTypeAO.intValue))
                                            {
                                                EditorGUILayout.HelpBox(Styles.denoiserNotSupportedWarning.text, MessageType.Info);
                                            }
                                        }
                                        EditorGUILayout.PropertyField(m_PVRFilterTypeAO, Styles.filterTypeAO);

                                        if (!m_PVRFilterTypeAO.hasMultipleDifferentValues)
                                        {
                                            EditorGUI.indentLevel++;
                                            DrawFilterSettingField(m_PVRFilteringGaussRadiusAO,
                                                m_PVRFilteringAtrousPositionSigmaAO,
                                                Styles.filteringGaussRadiusAO, Styles.filteringAtrousPositionSigmaAO,
                                                (LightingSettings.FilterType)m_PVRFilterTypeAO.intValue);
                                            EditorGUI.indentLevel--;
                                        }
                                    }

                                    EditorGUI.indentLevel--;
                                }

                                EditorGUI.indentLevel--;
                            }
                        }
                    }

                    if (bakedGISupported)
                    {
                        using (new EditorGUI.DisabledScope(!enableBakedGI))
                        {
                            DrawPropertyFieldWithPostfixLabel(m_BakeResolution, Styles.lightmapResolution, Styles.texelsPerUnit);

                            DrawPropertyFieldWithPostfixLabel(m_Padding, Styles.padding, Styles.texels);

                            EditorGUILayout.IntPopup(m_LightmapMaxSize, Styles.lightmapMaxSizeStrings, Styles.lightmapMaxSizeValues, Styles.lightmapMaxSize);

                            EditorGUILayout.PropertyField(m_LightmapSizeFixed, Styles.lightmapSizeFixed);

                            EditorGUILayout.PropertyField(m_UseMipmapLimits, Styles.useMipmapLimits);

                            EditorGUILayout.IntPopup(m_LightmapCompression, Styles.lightmapCompressionStrings, Styles.lightmapCompressionValues, Styles.lightmapCompression);

                            EditorGUILayout.PropertyField(m_AmbientOcclusion, Styles.ambientOcclusion);
                            if (m_AmbientOcclusion.boolValue)
                            {
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(m_AOMaxDistance, Styles.AOMaxDistance);
                                EditorGUILayout.Slider(m_CompAOExponent, 0.0f, 10.0f, Styles.ambientOcclusionContribution);
                                EditorGUILayout.Slider(m_CompAOExponentDirect, 0.0f, 10.0f, Styles.ambientOcclusionContributionDirect);

                                EditorGUI.indentLevel--;
                            }
                        }
                    }

                    bool directionalSupported = SupportedRenderingFeatures.IsLightmapsModeSupported(LightmapsMode.CombinedDirectional);

                    if (directionalSupported || (m_LightmapDirectionalMode.intValue == (int)LightmapsMode.CombinedDirectional))
                    {
                        EditorGUILayout.IntPopup(m_LightmapDirectionalMode, Styles.lightmapDirectionalModeStrings, Styles.lightmapDirectionalModeValues, Styles.lightmapDirectionalMode);

                        if (!directionalSupported)
                        {
                            EditorGUILayout.HelpBox(Styles.directionalNotSupportedWarning.text, MessageType.Warning);
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.IntPopup(Styles.lightmapDirectionalMode, 0, Styles.lightmapDirectionalModeStrings, Styles.lightmapDirectionalModeValues);
                        }
                    }

                    // albedo boost, push the albedo value towards one in order to get more bounce
                    EditorGUILayout.Slider(m_AlbedoBoost, 1.0f, 10.0f, Styles.albedoBoost);
                    EditorGUILayout.Slider(m_IndirectOutputScale, 0.0f, 5.0f, Styles.indirectOutputScale);

                    LightmapParametersGUI(m_LightmapParameters, Styles.lightmapParameters);
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            if (compact)
                EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void InternalSettingsGUI(bool compact)
        {
            if (!Unsupported.IsDeveloperMode())
                return;

            if (compact)
                m_ShowInternalSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowInternalSettings.value, Styles.internalLabel);
            else
                m_ShowInternalSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowInternalSettings.value, Styles.internalLabel, true);

            if (m_ShowInternalSettings.value)
            {
                bool enableRealtimeGI = (m_EnableRealtimeGI.boolValue && !m_EnableRealtimeGI.hasMultipleDifferentValues);
                EditorGUI.indentLevel++;
                if (enableRealtimeGI)
                {
                    EditorGUILayout.PropertyField(m_ForceWhiteAlbedo, Styles.forceWhiteAlbedo);
                    EditorGUILayout.PropertyField(m_ForceUpdates, Styles.forceUpdates);
                }

                EditorGUILayout.PropertyField(m_ExportTrainingData, Styles.exportTrainingData);

                if (m_ExportTrainingData.boolValue && !m_ExportTrainingData.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_TrainingDataDestination, Styles.trainingDataDestination);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(m_FilterMode, Styles.filterMode);
                if (enableRealtimeGI)
                {
                    EditorGUILayout.Slider(m_BounceScale, 0.0f, 10.0f, Styles.bounceScale);
                }

                Lightmapping.concurrentJobsType = (Lightmapping.ConcurrentJobsType)EditorGUILayout.IntPopup(Styles.concurrentJobs, (int)Lightmapping.concurrentJobsType, Styles.concurrentJobsTypeStrings, Styles.concurrentJobsTypeValues);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indent + 4);
                EditorGUILayout.BeginVertical();

                if (GUILayout.Button("Clear disk cache", GUILayout.Width(Styles.buttonWidth)))
                {
                    Lightmapping.Clear();
                    Lightmapping.ClearDiskCache();
                }

                if (GUILayout.Button("Print state to console", GUILayout.Width(Styles.buttonWidth)))
                {
                    Lightmapping.PrintStateToConsole();
                }

                if (GUILayout.Button("Reset albedo/emissive", GUILayout.Width(Styles.buttonWidth)))
                    GIDebugVisualisation.ResetRuntimeInputTextures();

                if (GUILayout.Button("Reset environment", GUILayout.Width(Styles.buttonWidth)))
                    DynamicGI.UpdateEnvironment();

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            if (compact)
                EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // Helper methods

        static void DrawPropertyFieldWithPostfixLabel(SerializedProperty property, GUIContent label, GUIContent postfixLabel)
        {
            const float minimumWidth = 170.0f;
            const float postfixLabelWidth = 80.0f;

            DrawFieldWithPostfixLabel(
                (Rect propertyRect) => { EditorGUI.PropertyField(propertyRect, property, label); },
                postfixLabel,
                EditorStyles.numberField,
                minimumWidth,
                postfixLabelWidth);
        }

        static void DrawFilterSettingField(SerializedProperty gaussSetting,
            SerializedProperty atrousSetting,
            GUIContent gaussLabel,
            GUIContent atrousLabel,
            LightingSettings.FilterType type)
        {
            const float minimumWidth = 230.0f;
            const float postfixLabelWidth = 40.0f;

            switch(type)
            {
                case LightingSettings.FilterType.Gaussian:
                    DrawFieldWithPostfixLabel(
                            (Rect propertyRect) => { EditorGUI.Slider(propertyRect, gaussSetting, 0.0f, 5.0f, gaussLabel); },
                            Styles.texels,
                            EditorStyles.toolbarSlider,
                            minimumWidth,
                            postfixLabelWidth);
                    break;

                case LightingSettings.FilterType.ATrous:
                    DrawFieldWithPostfixLabel(
                            (Rect propertyRect) => { EditorGUI.Slider(propertyRect, atrousSetting, 0.0f, 2.0f, atrousLabel); },
                            Styles.sigma,
                            EditorStyles.toolbarSlider,
                            minimumWidth,
                            postfixLabelWidth);
                    break;
            }
        }

        static void DrawFieldWithPostfixLabel(Action<Rect> drawFieldLambda, GUIContent postfixLabel, GUIStyle style, float minWidth, float postfixLabelWidth)
        {
            Rect propertyRect = GUILayoutUtility.GetRect(
                EditorGUILayout.kLabelFloatMinW,
                EditorGUILayout.kLabelFloatMaxW,
                EditorGUI.kSingleLineHeight,
                EditorGUI.kSingleLineHeight,
                style);

            propertyRect.width = Mathf.Max(propertyRect.width - postfixLabelWidth, minWidth);

            drawFieldLambda(propertyRect);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect labelRect = propertyRect;
            labelRect.x += propertyRect.width;
            EditorGUI.LabelField(labelRect, postfixLabel, Styles.labelStyle);

            EditorGUI.indentLevel = indent;
        }

        static bool isBuiltIn(SerializedProperty prop)
        {
            if (prop.objectReferenceValue != null)
            {
                var parameters = prop.objectReferenceValue as LightmapParameters;
                return (parameters.hideFlags == HideFlags.NotEditable);
            }

            return true;
        }

        private class DoCreateNewLightmapParameters : ProjectWindowCallback.DoCreateNewAsset
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                base.Action(instanceId, pathName, resourceFile);

                // Only assign the new parameters asset once it is fully imported.
                if (EditorUtility.InstanceIDToObject(instanceId) is LightmapParameters lmp)
                    lmp.AssignToLightingSettings(Lightmapping.lightingSettingsInternal);
            }
        }

        void CreateLightmapParameters(LightmapParameters from = null)
        {
            string newName = L10n.Tr("New Lightmap Parameters");

            LightmapParameters lmp;
            if (from == null)
            {
                lmp = new LightmapParameters();
                lmp.name = newName;
            }
            else
            {
                lmp = Object.Instantiate(from);
                lmp.name = from.name;
            }
            Undo.RecordObject(m_LightmapParameters.objectReferenceValue, newName);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                lmp.GetInstanceID(),
                ScriptableObject.CreateInstance<DoCreateNewLightmapParameters>(),
                (lmp.name + ".giparams"),
                AssetPreview.GetMiniThumbnail(lmp),
                null);
        }

        void LightmapParametersGUI(SerializedProperty prop, GUIContent content)
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(prop);

            if (GUILayout.Button(Styles.newLightmapParameters, EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                CreateLightmapParameters();
            else if (GUILayout.Button(Styles.cloneLightmapParameters, EditorStyles.miniButtonRight, GUILayout.Width(50)))
                CreateLightmapParameters(prop.objectReferenceValue as LightmapParameters);

            GUILayout.EndHorizontal();
        }

        static bool DenoiserSupported(LightingSettings.DenoiserType denoiserType)
        {
            if (denoiserType == LightingSettings.DenoiserType.Optix && !Lightmapping.IsOptixDenoiserSupported())
                return false;
            if (denoiserType == LightingSettings.DenoiserType.OpenImage && !Lightmapping.IsOpenImageDenoiserSupported())
                return false;
            
            return true;
        }

        void OnMixedModeSelected(object userData)
        {
            m_MixedBakeMode.intValue = (int)userData;
            m_MixedBakeMode.serializedObject.ApplyModifiedProperties();
        }

        void OnBakeBackedSelected(object userData)
        {
            m_BakeBackend.intValue = (int)userData;
            m_BakeBackend.serializedObject.ApplyModifiedProperties();
        }

        void BakeBackendGUI()
        {
            bool isAppleSiliconEditor = SystemInfo.processorType.Contains("Apple") && !EditorUtility.IsRunningUnderCPUEmulation();
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.bakeBackend, m_BakeBackend);
            EditorGUI.BeginChangeCheck();
            rect = EditorGUI.PrefixLabel(rect, Styles.bakeBackend);

            int index = Math.Max(0, Array.IndexOf(Styles.bakeBackendValues, m_BakeBackend.intValue));

            if (EditorGUI.DropdownButton(rect, Styles.bakeBackendStrings[index], FocusType.Passive))
            {
                var menu = new GenericMenu();

                for (int i = Styles.bakeBackendValues.Length - 1; i >= 0; i--)
                {
                    int value = Styles.bakeBackendValues[i];
                    bool selected = (value == m_BakeBackend.intValue);

                    if (!SupportedRenderingFeatures.IsLightmapperSupported(value) || (isAppleSiliconEditor && value == (int)LightingSettings.Lightmapper.ProgressiveCPU))
                    {
                        menu.AddDisabledItem(Styles.bakeBackendStrings[i], selected);
                    }
                    else
                    {
                        menu.AddItem(Styles.bakeBackendStrings[i], selected, OnBakeBackedSelected, value);
                    }
                }
                menu.DropDown(rect);
            }

            if (EditorGUI.EndChangeCheck())
                InspectorWindow.RepaintAllInspectors(); // We need to repaint other inspectors that might need to update based on the selected backend.

            EditorGUI.EndProperty();

            if (isAppleSiliconEditor && m_BakeBackend.intValue == (int)LightingSettings.Lightmapper.ProgressiveCPU)
            {
                EditorGUILayout.HelpBox(Styles.appleSiliconLightmapperWarning.text, MessageType.Warning);
            }
            else if (!SupportedRenderingFeatures.IsLightmapperSupported(m_BakeBackend.intValue))
            {
                string fallbackLightmapper = Styles.bakeBackendStrings[SupportedRenderingFeatures.FallbackLightmapper()].text;
                EditorGUILayout.HelpBox(Styles.lightmapperNotSupportedWarning.text + fallbackLightmapper + " Lightmapper instead.", MessageType.Warning);
            }
        }

        void OnDirectDenoiserSelected(object userData)
        {
            m_PVRDenoiserTypeDirect.intValue = (int)userData;
            m_PVRDenoiserTypeDirect.serializedObject.ApplyModifiedProperties();
        }

        void OnIndirectDenoiserSelected(object userData)
        {
            m_PVRDenoiserTypeIndirect.intValue = (int)userData;
            m_PVRDenoiserTypeIndirect.serializedObject.ApplyModifiedProperties();
        }

        void OnAODenoiserSelected(object userData)
        {
            m_PVRDenoiserTypeAO.intValue = (int)userData;
            m_PVRDenoiserTypeAO.serializedObject.ApplyModifiedProperties();
        }

        void DrawDenoiserTypeDropdown(SerializedProperty prop, GUIContent label, DenoiserTarget target)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, label, prop);
            rect = EditorGUI.PrefixLabel(rect, label);

            int index = Math.Max(0, Array.IndexOf(Styles.denoiserTypeValues, prop.intValue));

            if (EditorGUI.DropdownButton(rect, Styles.denoiserTypeStrings[index], FocusType.Passive))
            {
                var menu = new GenericMenu();

                for (int i = 0; i < Styles.denoiserTypeValues.Length; i++)
                {
                    int value = Styles.denoiserTypeValues[i];
                    bool denoiserSupported = DenoiserSupported((LightingSettings.DenoiserType)value);
                    bool selected = (value == prop.intValue);

                    if (!denoiserSupported)
                        menu.AddDisabledItem(Styles.denoiserTypeStrings[i], selected);
                    else
                    {
                        if (target == DenoiserTarget.Direct)
                            menu.AddItem(Styles.denoiserTypeStrings[i], selected, OnDirectDenoiserSelected, value);
                        else if (target == DenoiserTarget.Indirect)
                            menu.AddItem(Styles.denoiserTypeStrings[i], selected, OnIndirectDenoiserSelected, value);
                        else if (target == DenoiserTarget.AO)
                            menu.AddItem(Styles.denoiserTypeStrings[i], selected, OnAODenoiserSelected, value);
                    }
                }
                menu.DropDown(rect);
            }
            EditorGUI.EndProperty();
        }

        int MultiEditableLogarithmicIntSlider(SerializedProperty property, GUIContent style, int min, int max, int textFieldMin, int textFieldMax)
        {
            if (property.hasMultipleDifferentValues)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = true;

                int newValue = EditorGUILayout.LogarithmicIntSlider(style, property.intValue, min, max, 2, textFieldMin, textFieldMax);
                if (EditorGUI.EndChangeCheck())
                    property.intValue = newValue;

                EditorGUI.showMixedValue = false;
            }
            else
                property.intValue = EditorGUILayout.LogarithmicIntSlider(style, property.intValue, min, max, 2, textFieldMin, textFieldMax);

            return property.intValue;
        }

        internal void ClampMaxRanges()
        {
            maxDirectSamples = Mathf.Max(m_PVRDirectSampleCount.intValue, 1024);
            maxIndirectSamples = Mathf.Max(m_PVRSampleCount.intValue, 8192);
            maxEnvironmentSamples = Mathf.Max(m_PVREnvironmentSampleCount.intValue, 2048);
        }
    }
}
