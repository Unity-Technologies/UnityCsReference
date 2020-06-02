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
    }

    internal class SharedLightingSettingsEditor
    {
        SavedBool m_ShowRealtimeLightsSettings;
        SavedBool m_ShowMixedLightsSettings;
        SavedBool m_ShowGeneralLightingSettings;
        SavedBool m_ShowInternalSettings;

        SerializedProperty m_GIWorkflowMode;

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
        SerializedProperty m_TextureCompression;
        SerializedProperty m_FinalGather;
        SerializedProperty m_FinalGatherRayCount;
        SerializedProperty m_FinalGatherFiltering;
        SerializedProperty m_LightmapMaxSize;
        SerializedProperty m_BakeBackend;
        // pvr
        SerializedProperty m_PVRSampling;
        SerializedProperty m_PVRSampleCount;
        SerializedProperty m_PVRDirectSampleCount;
        SerializedProperty m_PVRBounces;
        SerializedProperty m_PVRRussianRouletteStartBounce;
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
        SerializedProperty m_PVREnvironmentMIS;
        SerializedProperty m_PVREnvironmentSampleCount;
        SerializedProperty m_LightProbeSampleCountMultiplier;

        // internal
        SerializedProperty m_BounceScale;
        SerializedProperty m_ExportTrainingData;
        SerializedProperty m_TrainingDataDestination;
        SerializedProperty m_ForceWhiteAlbedo;
        SerializedProperty m_ForceUpdates;
        SerializedProperty m_FilterMode;

        enum DenoiserTarget
        {
            Direct = 0,
            Indirect = 1,
            AO = 2
        }

        static class Styles
        {
            public static readonly int[] bakeBackendValues = { (int)LightingSettings.Lightmapper.Enlighten, (int)LightingSettings.Lightmapper.ProgressiveCPU, (int)LightingSettings.Lightmapper.ProgressiveGPU };
            public static readonly GUIContent[] bakeBackendStrings =
            {
                EditorGUIUtility.TrTextContent("Enlighten (Deprecated)"),
                EditorGUIUtility.TrTextContent("Progressive CPU"),
                EditorGUIUtility.TrTextContent("Progressive GPU (Preview)"),
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
            public static readonly int[] denoiserTypeValues = { (int)LightingSettings.DenoiserType.Optix, (int)LightingSettings.DenoiserType.OpenImage, (int)LightingSettings.DenoiserType.RadeonPro, (int)LightingSettings.DenoiserType.None };
            public static readonly GUIContent[] denoiserTypeStrings =
            {
                EditorGUIUtility.TrTextContent("Optix"),
                EditorGUIUtility.TrTextContent("OpenImageDenoise"),
                EditorGUIUtility.TrTextContent("Radeon Pro"),
                EditorGUIUtility.TrTextContent("None")
            };

            public static readonly int[] bouncesValues = { 0, 1, 2, 3, 4 };
            public static readonly GUIContent[] bouncesStrings =
            {
                EditorGUIUtility.TrTextContent("None"),
                EditorGUIUtility.TextContent("1"),
                EditorGUIUtility.TextContent("2"),
                EditorGUIUtility.TextContent("3"),
                EditorGUIUtility.TextContent("4")
            };

            public static readonly int[] russianRouletteStartBounceValues = { -1, 2, 3, 4 };
            public static readonly GUIContent[] russianRouletteStartBounceStrings =
            {
                EditorGUIUtility.TrTextContent("Never"),
                EditorGUIUtility.TextContent("2"),
                EditorGUIUtility.TextContent("3"),
                EditorGUIUtility.TextContent("4")
            };

            public static readonly GUIContent[] helpStringsMixed =
            {
                EditorGUIUtility.TrTextContent("Mixed lights provide realtime direct lighting while indirect light is baked into lightmaps and light probes."),
                EditorGUIUtility.TrTextContent("Mixed lights provide baked direct and indirect lighting for static objects. Dynamic objects receive realtime direct lighting and cast shadows on static objects using the main directional light in the scene."),
                EditorGUIUtility.TrTextContent("Mixed lights provide realtime direct lighting. Indirect lighting gets baked into lightmaps and light probes. Shadowmasks and light probes occlusion get generated for baked shadows. ")
            };

            public static readonly GUIContent lightmapperNotSupportedWarning = EditorGUIUtility.TrTextContent("The Lightmapper is not supported by the current render pipeline. Fallback is ");
            public static readonly GUIContent mixedModeNotSupportedWarning = EditorGUIUtility.TrTextContent("The Mixed mode is not supported by the current render pipeline. Fallback mode is ");
            public static readonly GUIContent directionalNotSupportedWarning = EditorGUIUtility.TrTextContent("Directional Mode is not supported. Fallback will be Non-Directional.");

            public static readonly GUIContent autoGenerate = EditorGUIUtility.TrTextContent("Auto Generate", "Automatically generates lighting data in the Scene when any changes are made to the lighting systems.");
            public static readonly GUIContent enableBaked = EditorGUIUtility.TrTextContent("Baked Global Illumination", "Controls whether Mixed and Baked lights will use baked Global Illumination. If enabled, Mixed lights are baked using the specified Lighting Mode and Baked lights will be completely baked and not adjustable at runtime.");
            public static readonly GUIContent bounceScale = EditorGUIUtility.TrTextContent("Bounce Scale", "Multiplier for indirect lighting. Use with care.");
            public static readonly GUIContent updateThreshold = EditorGUIUtility.TrTextContent("Update Threshold", "Threshold for updating realtime GI. A lower value causes more frequent updates (default 1.0).");
            public static readonly GUIContent albedoBoost = EditorGUIUtility.TrTextContent("Albedo Boost", "Controls the amount of light bounced between surfaces by intensifying the albedo of materials in the scene. Increasing this draws the albedo value towards white for indirect light computation. The default value is physically accurate.");
            public static readonly GUIContent indirectOutputScale = EditorGUIUtility.TrTextContent("Indirect Intensity", "Controls the brightness of indirect light stored in realtime and baked lightmaps. A value above 1.0 will increase the intensity of indirect light while a value less than 1.0 will reduce indirect light intensity.");
            public static readonly GUIContent lightmapDirectionalMode = EditorGUIUtility.TrTextContent("Directional Mode", "Controls whether baked and realtime lightmaps will store directional lighting information from the lighting environment. Options are Directional and Non-Directional.");
            public static readonly GUIContent lightmapParameters = EditorGUIUtility.TrTextContent("Lightmap Parameters", "Allows the adjustment of advanced parameters that affect the process of generating a lightmap for an object using global illumination.");
            public static readonly GUIContent lightmapParametersDefault = EditorGUIUtility.TrTextContent("Default-Medium");
            public static readonly GUIContent realtimeLightsLabel = EditorGUIUtility.TrTextContent("Realtime Lighting", "Precompute Realtime indirect lighting for realtime lights and static objects. In this mode realtime lights, ambient lighting, materials of static objects (including emission) will generate indirect lighting at runtime. Only static objects are blocking and bouncing light, dynamic objects receive indirect lighting via light probes.");
            public static readonly GUIContent realtimeEnvironmentLighting = EditorGUIUtility.TrTextContent("Realtime Environment Lighting", "Specifies the Global Illumination mode that should be used for handling ambient light in the Scene. This property is not editable unless both Realtime Global Illumination and Baked Global Illumination are enabled for the scene.");
            public static readonly GUIContent mixedLightsLabel = EditorGUIUtility.TrTextContent("Mixed Lighting", "Bake Global Illumination for mixed lights and static objects. May bake both direct and/or indirect lighting based on settings. Only static objects are blocking and bouncing light, dynamic objects receive baked lighting via light probes.");
            public static readonly GUIContent generalLightmapLabel = EditorGUIUtility.TrTextContent("Lightmapping Settings", "Settings that apply to both Global Illumination modes (Precomputed Realtime and Baked).");
            public static readonly GUIContent internalLabel = EditorGUIUtility.TrTextContent("Internal Settings", "Internal only settings. ");
            public static readonly GUIContent noRealtimeGIInSM2AndGLES2 = EditorGUIUtility.TrTextContent("Realtime Global Illumination is not supported on SM2.0 hardware nor when using GLES2.0.");
            public static readonly GUIContent forceWhiteAlbedo = EditorGUIUtility.TrTextContent("Force White Albedo", "Force white albedo during lighting calculations.");
            public static readonly GUIContent forceUpdates = EditorGUIUtility.TrTextContent("Force Updates", "Force continuous updates of runtime indirect lighting calculations.");
            public static readonly GUIContent filterMode = EditorGUIUtility.TrTextContent("Filter Mode");
            public static readonly GUIContent exportTrainingData = EditorGUIUtility.TrTextContent("Export Training Data", "Exports unfiltered textures, normals, positions.");
            public static readonly GUIContent trainingDataDestination = EditorGUIUtility.TrTextContent("Destination", "Destination for the training data, for example 'mysetup/30samples'. Will still be located at the first level in the project folder. ");
            public static readonly GUIContent indirectResolution = EditorGUIUtility.TrTextContent("Indirect Resolution", "Sets the resolution in texels that are used per unit for objects being lit by indirect lighting. The larger the value, the more significant the impact will be on the time it takes to bake the lighting.");
            public static readonly GUIContent lightmapResolution = EditorGUIUtility.TrTextContent("Lightmap Resolution", "Sets the resolution in texels that are used per unit for objects being lit by baked global illumination. Larger values will result in increased time to calculate the baked lighting.");
            public static readonly GUIContent padding = EditorGUIUtility.TrTextContent("Lightmap Padding", "Sets the separation in texels between shapes in the baked lightmap.");
            public static readonly GUIContent lightmapMaxSize = EditorGUIUtility.TrTextContent("Max Lightmap Size", "Sets the max size of the full lightmap Texture in pixels. Values are squared, so a setting of 1024 can produce a 1024x1024 pixel sized lightmap.");
            public static readonly GUIContent textureCompression = EditorGUIUtility.TrTextContent("Compress Lightmaps", "Controls whether the baked lightmap is compressed or not. When enabled, baked lightmaps are compressed to reduce required storage space but some artifacting may be present due to compression.");
            public static readonly GUIContent ambientOcclusion = EditorGUIUtility.TrTextContent("Ambient Occlusion", "Specifies whether to include ambient occlusion or not in the baked lightmap result. Enabling this results in simulating the soft shadows that occur in cracks and crevices of objects when light is reflected onto them.");
            public static readonly GUIContent ambientOcclusionContribution = EditorGUIUtility.TrTextContent("Indirect Contribution", "Adjusts the contrast of ambient occlusion applied to indirect lighting. The larger the value, the more contrast is applied to the ambient occlusion for indirect lighting.");
            public static readonly GUIContent ambientOcclusionContributionDirect = EditorGUIUtility.TrTextContent("Direct Contribution", "Adjusts the contrast of ambient occlusion applied to the direct lighting. The larger the value is, the more contrast is applied to the ambient occlusion for direct lighting. This effect is not physically accurate.");
            public static readonly GUIContent AOMaxDistance = EditorGUIUtility.TrTextContent("Max Distance", "Controls how far rays are cast in order to determine if an object is occluded or not. A larger value produces longer rays and contributes more shadows to the lightmap, while a smaller value produces shorter rays that contribute shadows only when objects are very close to one another. A value of 0 casts an infinitely long ray that has no maximum distance.");
            public static readonly GUIContent finalGather = EditorGUIUtility.TrTextContent("Final Gather", "Specifies whether the final light bounce of the global illumination calculation is calculated at the same resolution as the baked lightmap. When enabled, visual quality is improved at the cost of additional time required to bake the lighting.");
            public static readonly GUIContent finalGatherRayCount = EditorGUIUtility.TrTextContent("Ray Count", "Controls the number of rays emitted for every final gather point.");
            public static readonly GUIContent finalGatherFiltering = EditorGUIUtility.TrTextContent("Denoising", "Controls whether a denoising filter is applied to the final gather output.");
            public static readonly GUIContent mixedLightMode = EditorGUIUtility.TrTextContent("Lighting Mode", "Specifies which Scene lighting mode will be used for all Mixed lights in the Scene. Options are Baked Indirect, Shadowmask and Subtractive.");
            public static readonly GUIContent useRealtimeGI = EditorGUIUtility.TrTextContent("Realtime Global Illumination (Deprecated)", "Enlighten is entering deprecation. Please ensure that your project will not require support for Enlighten beyond the deprecation date.");
            public static readonly GUIContent bakedGIDisabledInfo = EditorGUIUtility.TrTextContent("All Baked and Mixed lights in the Scene are currently being overridden to Realtime light modes. Enable Baked Global Illumination to allow the use of Baked and Mixed light modes.");
            public static readonly GUIContent bakeBackend = EditorGUIUtility.TrTextContent("Lightmapper", "Specifies which baking system will be used to generate baked lightmaps.");
            //public static readonly GUIContent PVRSampling = EditorGUIUtility.TrTextContent("Sampling", "How to sample the lightmaps. Auto and adaptive automatically test for convergence. Auto uses a maximum of 16K samples. Adaptive uses a configurable maximum number of samples. Fixed always uses the set number of samples and does not test for convergence.");
            //public static readonly GUIContent PVRDirectSampleCountAdaptive = EditorGUIUtility.TrTextContent("Max Direct Samples", "Maximum number of samples to use for direct lighting.");
            public static readonly GUIContent directSampleCount = EditorGUIUtility.TrTextContent("Direct Samples", "Controls the number of samples the lightmapper will use for direct lighting calculations. Increasing this value may improve the quality of lightmaps but increases the time required for baking to complete.");
            //public static readonly GUIContent PVRSampleCountAdaptive = EditorGUIUtility.TrTextContent("Max Indirect Samples", "Maximum number of samples to use for indirect lighting.");
            public static readonly GUIContent indirectSampleCount = EditorGUIUtility.TrTextContent("Indirect Samples", "Controls the number of samples the lightmapper will use for indirect lighting calculations. Increasing this value may improve the quality of lightmaps but increases the time required for baking to complete.");
            public static readonly GUIContent bounces = EditorGUIUtility.TrTextContent("Bounces", "Controls the maximum number of bounces the lightmapper will compute for indirect light.");
            public static readonly GUIContent russianRouletteStartBounce = EditorGUIUtility.TrTextContent("Russian Roulette Start Bounce", "The first bounce that Russian Roulette path termination can disable. Russian roulette decreases the bake time by stochastically terminating the light path, but shorter paths may increase lightmap noise. Choose 2 to ensure at least 1 bounce, and so on. Choose Never to disable Russian roulette.");
            public static readonly GUIContent denoisingWarningDirect = EditorGUIUtility.TrTextContent("Direct Denoiser", "Your hardware doesn't support denoising. To see minimum requirements, read the documentation.");
            public static readonly GUIContent denoisingWarningIndirect = EditorGUIUtility.TrTextContent("Indirect Denoiser", "Your hardware doesn't support denoising. To see minimum requirements, read the documentation.");
            public static readonly GUIContent denoisingWarningAO = EditorGUIUtility.TrTextContent("Ambient Occlusion Denoiser", "Your hardware doesn't support denoising. To see minimum requirements, read the documentation.");
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
            public static readonly GUIContent environmentMIS = EditorGUIUtility.TrTextContent("Multiple Importance Sampling", "Specifies whether to use multiple importance sampling for sampling the environment. This will generally lead to faster convergence when generating lightmaps but can lead to noisier results in certain low frequency environments.");
            public static readonly GUIContent environmentSampleCount = EditorGUIUtility.TrTextContent("Environment Samples", "Controls the number of samples the lightmapper will use for environment lighting calculations. Increasing this value may improve the quality of lightmaps but increases the time required for baking to complete.");
            public static readonly GUIContent probeSampleCountMultiplier = EditorGUIUtility.TrTextContent("Light Probe Sample Multiplier", "Controls how many samples are used for Light Probes as a multiplier of the general sample counts above. Higher values improve the quality of Light Probes, but also take longer to bake. Enable the Light Probe sample count multiplier by disabling Project Settings > Editor > Use legacy Light Probe sample counts");

            public static readonly GUIStyle labelStyle = EditorStyles.wordWrappedMiniLabel;
        }

        public void OnEnable()
        {
            m_ShowRealtimeLightsSettings = new SavedBool("LightingSettings.ShowRealtimeLightsSettings", false);
            m_ShowMixedLightsSettings = new SavedBool("LightingSettings.ShowMixedLightsSettings", true);
            m_ShowGeneralLightingSettings = new SavedBool("LightingSettings.ShowGeneralLightingSettings", true);
            m_ShowInternalSettings = new SavedBool("LightingSettings.ShowInternalSettings", true);
        }

        public void OnGUI(bool compact, bool drawAutoGenerate)
        {
            if (drawAutoGenerate)
            {
                bool iterative = m_GIWorkflowMode.intValue == (int)Lightmapping.GIWorkflowMode.Iterative;

                var rect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(rect, Styles.autoGenerate, m_GIWorkflowMode);
                EditorGUI.BeginChangeCheck();

                iterative = EditorGUI.Toggle(rect, Styles.autoGenerate, iterative);

                if (EditorGUI.EndChangeCheck())
                {
                    m_GIWorkflowMode.intValue = (int)(iterative ? Lightmapping.GIWorkflowMode.Iterative : Lightmapping.GIWorkflowMode.OnDemand);
                }
                EditorGUI.EndProperty();

                EditorGUILayout.Space();
            }

            RealtimeLightingGUI(compact);
            MixedLightingGUI(compact);
            GeneralLightmapSettingsGUI(compact);
            InternalSettingsGUI(compact);
        }

        public void UpdateSettings(SerializedObject lso)
        {
            if (lso != null)
            {
                m_GIWorkflowMode = lso.FindProperty("m_GIWorkflowMode");

                //realtime GI
                m_RealtimeResolution = lso.FindProperty("m_RealtimeResolution");
                m_EnableRealtimeGI = lso.FindProperty("m_EnableRealtimeLightmaps");
                m_RealtimeEnvironmentLighting = lso.FindProperty("m_RealtimeEnvironmentLighting");

                //baked
                m_EnabledBakedGI = lso.FindProperty("m_EnableBakedLightmaps");
                m_BakeBackend = lso.FindProperty("m_BakeBackend");
                m_MixedBakeMode = lso.FindProperty("m_MixedBakeMode");
                m_AlbedoBoost = lso.FindProperty("m_AlbedoBoost");
                m_IndirectOutputScale = lso.FindProperty("m_IndirectOutputScale");
                m_LightmapMaxSize = lso.FindProperty("m_LightmapMaxSize");
                m_LightmapParameters = lso.FindProperty("m_LightmapParameters");
                m_LightmapDirectionalMode = lso.FindProperty("m_LightmapsBakeMode");
                m_BakeResolution = lso.FindProperty("m_BakeResolution");
                m_Padding = lso.FindProperty("m_Padding");
                m_AmbientOcclusion = lso.FindProperty("m_AO");
                m_AOMaxDistance = lso.FindProperty("m_AOMaxDistance");
                m_CompAOExponent = lso.FindProperty("m_CompAOExponent");
                m_CompAOExponentDirect = lso.FindProperty("m_CompAOExponentDirect");
                m_TextureCompression = lso.FindProperty("m_TextureCompression");
                m_FinalGather = lso.FindProperty("m_FinalGather");
                m_FinalGatherRayCount = lso.FindProperty("m_FinalGatherRayCount");
                m_FinalGatherFiltering = lso.FindProperty("m_FinalGatherFiltering");

                // pvr
                m_PVRSampling = lso.FindProperty("m_PVRSampling");
                m_PVRSampleCount = lso.FindProperty("m_PVRSampleCount");
                m_PVRDirectSampleCount = lso.FindProperty("m_PVRDirectSampleCount");
                m_PVRBounces = lso.FindProperty("m_PVRBounces");
                m_PVRRussianRouletteStartBounce = lso.FindProperty("m_PVRRussianRouletteStartBounce");
                m_PVRCulling = lso.FindProperty("m_PVRCulling");
                m_PVRFilteringMode = lso.FindProperty("m_PVRFilteringMode");
                m_PVRFilterTypeDirect = lso.FindProperty("m_PVRFilterTypeDirect");
                m_PVRFilterTypeIndirect = lso.FindProperty("m_PVRFilterTypeIndirect");
                m_PVRFilterTypeAO = lso.FindProperty("m_PVRFilterTypeAO");
                m_PVRDenoiserTypeDirect = lso.FindProperty("m_PVRDenoiserTypeDirect");
                m_PVRDenoiserTypeIndirect = lso.FindProperty("m_PVRDenoiserTypeIndirect");
                m_PVRDenoiserTypeAO = lso.FindProperty("m_PVRDenoiserTypeAO");
                m_PVRFilteringGaussRadiusDirect = lso.FindProperty("m_PVRFilteringGaussRadiusDirect");
                m_PVRFilteringGaussRadiusIndirect = lso.FindProperty("m_PVRFilteringGaussRadiusIndirect");
                m_PVRFilteringGaussRadiusAO = lso.FindProperty("m_PVRFilteringGaussRadiusAO");
                m_PVRFilteringAtrousPositionSigmaDirect = lso.FindProperty("m_PVRFilteringAtrousPositionSigmaDirect");
                m_PVRFilteringAtrousPositionSigmaIndirect = lso.FindProperty("m_PVRFilteringAtrousPositionSigmaIndirect");
                m_PVRFilteringAtrousPositionSigmaAO = lso.FindProperty("m_PVRFilteringAtrousPositionSigmaAO");
                m_PVREnvironmentMIS = lso.FindProperty("m_PVREnvironmentMIS");
                m_PVREnvironmentSampleCount = lso.FindProperty("m_PVREnvironmentSampleCount");
                m_LightProbeSampleCountMultiplier = lso.FindProperty("m_LightProbeSampleCountMultiplier");

                //dev debug properties
                m_ExportTrainingData = lso.FindProperty("m_ExportTrainingData");
                m_TrainingDataDestination = lso.FindProperty("m_TrainingDataDestination");
                m_ForceWhiteAlbedo = lso.FindProperty("m_ForceWhiteAlbedo");
                m_ForceUpdates = lso.FindProperty("m_ForceUpdates");
                m_FilterMode = lso.FindProperty("m_FilterMode");
                m_BounceScale = lso.FindProperty("m_BounceScale");
            }
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

                if (m_EnableRealtimeGI.boolValue && PlayerHasSM20Support())
                {
                    EditorGUILayout.HelpBox(Styles.noRealtimeGIInSM2AndGLES2.text, MessageType.Warning);
                }

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

                        if (mixedGISupported)
                        {
                            if (!SupportedRenderingFeatures.IsMixedLightingModeSupported((MixedLightingMode)m_MixedBakeMode.intValue))
                            {
                                string fallbackMode = Styles.mixedModeStrings[(int)SupportedRenderingFeatures.FallbackMixedLightingMode()].text;
                                EditorGUILayout.HelpBox(Styles.mixedModeNotSupportedWarning.text + fallbackMode, MessageType.Warning);
                            }
                            else if (enableBakedGI)
                            {
                                EditorGUILayout.HelpBox(Styles.helpStringsMixed[m_MixedBakeMode.intValue].text + EditorGUIUtility.TrTextContent(SupportedRenderingFeatures.active.shadowmaskMessage).text, MessageType.Info);
                            }
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

                            if (lightmapperSupported && !m_BakeBackend.hasMultipleDifferentValues)
                            {
                                if (m_BakeBackend.intValue == (int)LightingSettings.Lightmapper.Enlighten)
                                {
                                    EditorGUI.indentLevel++;

                                    EditorGUILayout.PropertyField(m_FinalGather, Styles.finalGather);
                                    if (m_FinalGather.boolValue)
                                    {
                                        EditorGUI.indentLevel++;
                                        EditorGUILayout.PropertyField(m_FinalGatherRayCount, Styles.finalGatherRayCount);
                                        EditorGUILayout.PropertyField(m_FinalGatherFiltering, Styles.finalGatherFiltering);
                                        EditorGUI.indentLevel--;
                                    }

                                    EditorGUI.indentLevel--;
                                }

                                if (m_BakeBackend.intValue != (int)LightingSettings.Lightmapper.Enlighten)
                                {
                                    EditorGUI.indentLevel++;

                                    EditorGUILayout.PropertyField(m_PVRCulling, Styles.culling);

                                    var rect = EditorGUILayout.GetControlRect();
                                    EditorGUI.BeginProperty(rect, Styles.environmentMIS, m_PVREnvironmentMIS);
                                    EditorGUI.BeginChangeCheck();
                                    bool enableMIS = (m_PVREnvironmentMIS.intValue & 1) != 0;

                                    enableMIS = EditorGUI.Toggle(rect, Styles.environmentMIS, enableMIS);

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        if (enableMIS)
                                            m_PVREnvironmentMIS.intValue |= 1;
                                        else
                                            m_PVREnvironmentMIS.intValue &= ~1;
                                    }
                                    EditorGUI.EndProperty();

                                    // Sampling type
                                    //EditorGUILayout.PropertyField(m_PvrSampling, Styles.m_PVRSampling); // TODO(PVR): make non-fixed sampling modes work.

                                    if (m_PVRSampling.intValue != (int)LightingSettings.Sampling.Auto)
                                    {
                                        // Sample count
                                        // TODO(PVR): make non-fixed sampling modes work.
                                        //EditorGUI.indentLevel++;
                                        //if (LightingSettings.giPathTracerSampling == LightingSettings.PathTracerSampling.PathTracerSamplingAdaptive)
                                        //  EditorGUILayout.PropertyField(m_PVRSampleCount, Styles.PVRSampleCountAdaptive);
                                        //else

                                        EditorGUILayout.PropertyField(m_PVRDirectSampleCount, Styles.directSampleCount);
                                        EditorGUILayout.PropertyField(m_PVRSampleCount, Styles.indirectSampleCount);
                                        EditorGUILayout.PropertyField(m_PVREnvironmentSampleCount, Styles.environmentSampleCount);

                                        using (new EditorGUI.DisabledScope(EditorSettings.useLegacyProbeSampleCount))
                                        {
                                            EditorGUILayout.PropertyField(m_LightProbeSampleCountMultiplier, Styles.probeSampleCountMultiplier);
                                        }

                                        // TODO(PVR): make non-fixed sampling modes work.
                                        //EditorGUI.indentLevel--;
                                    }

                                    EditorGUILayout.IntPopup(m_PVRBounces, Styles.bouncesStrings, Styles.bouncesValues, Styles.bounces);
                                    EditorGUILayout.IntPopup(m_PVRRussianRouletteStartBounce, Styles.russianRouletteStartBounceStrings, Styles.russianRouletteStartBounceValues, Styles.russianRouletteStartBounce);

                                    // Filtering
                                    EditorGUILayout.PropertyField(m_PVRFilteringMode, Styles.filteringMode);

                                    if (m_PVRFilteringMode.intValue == (int)LightingSettings.FilterMode.Advanced && !m_PVRFilteringMode.hasMultipleDifferentValues)
                                    {
                                        // Check if the platform doesn't support denoising.
                                        bool usingGPULightmapper = m_BakeBackend.intValue == (int)LightingSettings.Lightmapper.ProgressiveGPU;
                                        bool anyDenoisingSupported = (Lightmapping.IsOptixDenoiserSupported() || Lightmapping.IsOpenImageDenoiserSupported() || Lightmapping.IsRadeonDenoiserSupported());
                                        bool aoDenoisingSupported = DenoiserSupported((LightingSettings.DenoiserType)m_PVRDenoiserTypeAO.intValue);
                                        bool directDenoisingSupported = DenoiserSupported((LightingSettings.DenoiserType)m_PVRDenoiserTypeDirect.intValue);
                                        bool indirectDenoisingSupported = DenoiserSupported((LightingSettings.DenoiserType)m_PVRDenoiserTypeIndirect.intValue);

                                        EditorGUI.indentLevel++;
                                        using (new EditorGUI.DisabledScope(!anyDenoisingSupported))
                                        {
                                            DrawDenoiserTypeDropdown(m_PVRDenoiserTypeDirect, directDenoisingSupported ? Styles.denoiserTypeDirect : Styles.denoisingWarningDirect, DenoiserTarget.Direct);
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
                                            DrawDenoiserTypeDropdown(m_PVRDenoiserTypeIndirect, indirectDenoisingSupported ? Styles.denoiserTypeIndirect : Styles.denoisingWarningIndirect, DenoiserTarget.Indirect);
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
                                                DrawDenoiserTypeDropdown(m_PVRDenoiserTypeAO, aoDenoisingSupported ? Styles.denoiserTypeAO : Styles.denoisingWarningAO, DenoiserTarget.AO);
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
                    }

                    // We only want to show the Indirect Resolution in a disabled state if the user is using PLM and has the ability to turn on Realtime GI.
                    if (realtimeGISupported || (bakedGISupported && (m_BakeBackend.intValue == (int)LightingSettings.Lightmapper.Enlighten) && lightmapperSupported))
                    {
                        using (new EditorGUI.DisabledScope((m_BakeBackend.intValue != (int)LightingSettings.Lightmapper.Enlighten) && !enableRealtimeGI))
                        {
                            DrawResolutionField(m_RealtimeResolution, Styles.indirectResolution);
                        }
                    }

                    if (bakedGISupported)
                    {
                        using (new EditorGUI.DisabledScope(!enableBakedGI))
                        {
                            DrawResolutionField(m_BakeResolution, Styles.lightmapResolution);

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(m_Padding, Styles.padding);
                            GUILayout.Label(" texels", Styles.labelStyle);
                            GUILayout.EndHorizontal();

                            EditorGUILayout.IntPopup(m_LightmapMaxSize, Styles.lightmapMaxSizeStrings, Styles.lightmapMaxSizeValues, Styles.lightmapMaxSize);

                            EditorGUILayout.PropertyField(m_TextureCompression, Styles.textureCompression);

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
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_ForceWhiteAlbedo, Styles.forceWhiteAlbedo);
                EditorGUILayout.PropertyField(m_ForceUpdates, Styles.forceUpdates);

                if (m_BakeBackend.intValue != (int)LightingSettings.Lightmapper.Enlighten)
                {
                    EditorGUILayout.PropertyField(m_ExportTrainingData, Styles.exportTrainingData);

                    if (m_ExportTrainingData.boolValue && !m_ExportTrainingData.hasMultipleDifferentValues)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(m_TrainingDataDestination, Styles.trainingDataDestination);
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.PropertyField(m_FilterMode, Styles.filterMode);
                EditorGUILayout.Slider(m_BounceScale, 0.0f, 10.0f, Styles.bounceScale);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            if (compact)
                EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // Helper methods

        static bool PlayerHasSM20Support()
        {
            var apis = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget);
            bool hasSM20Api = apis.Contains(UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2);
            return hasSM20Api;
        }

        static void DrawResolutionField(SerializedProperty resolution, GUIContent label)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(resolution, label);

            GUILayout.Label(" texels per unit", Styles.labelStyle);
            GUILayout.EndHorizontal();
        }

        static void DrawFilterSettingField(SerializedProperty gaussSetting,
            SerializedProperty atrousSetting,
            GUIContent gaussLabel,
            GUIContent atrousLabel,
            LightingSettings.FilterType type)
        {
            if (type == LightingSettings.FilterType.None)
                return;

            GUILayout.BeginHorizontal();

            if (type == LightingSettings.FilterType.Gaussian)
            {
                EditorGUILayout.IntSlider(gaussSetting, 0, 5, gaussLabel);
                GUILayout.Label(" texels", Styles.labelStyle);
            }
            else if (type == LightingSettings.FilterType.ATrous)
            {
                EditorGUILayout.Slider(atrousSetting, 0.0f, 2.0f, atrousLabel);
                GUILayout.Label(" sigma", Styles.labelStyle);
            }

            GUILayout.EndHorizontal();
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

        static void LightmapParametersGUI(SerializedProperty prop, GUIContent content)
        {
            EditorGUILayout.BeginHorizontal();

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, content, prop);

            rect = EditorGUI.PrefixLabel(rect, content);
            GUIContent buttonContent = prop.hasMultipleDifferentValues ? EditorGUI.mixedValueContent : (prop.objectReferenceValue != null ? GUIContent.Temp(prop.objectReferenceStringValue) : Styles.lightmapParametersDefault);

            if (EditorGUI.DropdownButton(rect, buttonContent, FocusType.Passive, EditorStyles.popup))
                AssetPopupBackend.ShowAssetsPopupMenu<LightmapParameters>(rect, prop.objectReferenceTypeString, prop, "giparams", Styles.lightmapParametersDefault.text);

            string label = isBuiltIn(prop) ? "View" : "Edit...";

            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                Selection.activeObject = prop.objectReferenceValue;
                EditorWindow.FocusWindowIfItsOpen<InspectorWindow>();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndProperty();
        }

        static bool DenoiserSupported(LightingSettings.DenoiserType denoiserType)
        {
            if (denoiserType == LightingSettings.DenoiserType.Optix && !Lightmapping.IsOptixDenoiserSupported())
                return false;
            if (denoiserType == LightingSettings.DenoiserType.OpenImage && !Lightmapping.IsOpenImageDenoiserSupported())
                return false;
            if (denoiserType == LightingSettings.DenoiserType.RadeonPro && !Lightmapping.IsRadeonDenoiserSupported())
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
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.bakeBackend, m_BakeBackend);
            EditorGUI.BeginChangeCheck();
            rect = EditorGUI.PrefixLabel(rect, Styles.bakeBackend);

            int index = Math.Max(0, Array.IndexOf(Styles.bakeBackendValues, m_BakeBackend.intValue));

            if (EditorGUI.DropdownButton(rect, Styles.bakeBackendStrings[index], FocusType.Passive))
            {
                var menu = new GenericMenu();

                for (int i = 0; i < Styles.bakeBackendValues.Length; i++)
                {
                    int value = Styles.bakeBackendValues[i];
                    bool selected = (value == m_BakeBackend.intValue);

                    if (!SupportedRenderingFeatures.IsLightmapperSupported(value))
                        menu.AddDisabledItem(Styles.bakeBackendStrings[i], selected);
                    else
                        menu.AddItem(Styles.bakeBackendStrings[i], selected, OnBakeBackedSelected, value);
                }
                menu.DropDown(rect);
            }
            if (EditorGUI.EndChangeCheck())
                InspectorWindow.RepaintAllInspectors(); // We need to repaint other inspectors that might need to update based on the selected backend.

            EditorGUI.EndProperty();

            if (!SupportedRenderingFeatures.IsLightmapperSupported(m_BakeBackend.intValue))
            {
                string fallbackLightmapper = Styles.bakeBackendStrings[SupportedRenderingFeatures.FallbackLightmapper()].text;
                EditorGUILayout.HelpBox(Styles.lightmapperNotSupportedWarning.text + fallbackLightmapper, MessageType.Warning);
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
                bool radeonDenoiserSupported = Lightmapping.IsRadeonDenoiserSupported();
                bool openImageDenoiserSupported = Lightmapping.IsOpenImageDenoiserSupported();
                bool optixDenoiserSupported = Lightmapping.IsOptixDenoiserSupported();
                var menu = new GenericMenu();
                for (int i = 0; i < Styles.denoiserTypeValues.Length; i++)
                {
                    int value = Styles.denoiserTypeValues[i];
                    bool optixDenoiserItem = (value == (int)LightingSettings.DenoiserType.Optix);
                    bool openImageDenoiserItem = (value == (int)LightingSettings.DenoiserType.OpenImage);
                    bool radeonDenoiserItem = (value == (int)LightingSettings.DenoiserType.RadeonPro);
                    bool selected = (value == prop.intValue);

                    if (!optixDenoiserSupported && optixDenoiserItem)
                        menu.AddDisabledItem(Styles.denoiserTypeStrings[i], selected);
                    else if (!openImageDenoiserSupported && openImageDenoiserItem)
                        menu.AddDisabledItem(Styles.denoiserTypeStrings[i], selected);
                    else if (!radeonDenoiserSupported && radeonDenoiserItem)
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
    }
}
