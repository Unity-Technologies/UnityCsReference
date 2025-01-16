// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEditor.AnimatedValues;

namespace UnityEditor.SpeedTree.Importer
{
    class SpeedTreeConstants
    {
        internal static readonly float kFeetToMetersRatio = 0.3048f;
        internal static readonly float kCentimetersToMetersRatio = 0.01f;
        internal static readonly float kInchesToMetersRatio = 0.0254f;
        internal static readonly float kAlphaTestRef = 0.33f;
        internal static readonly string kBillboardMaterialName = "Billboard";
    }

    static class SpeedTreeImporterCommon
    {
        internal enum STUnitConversion
        {
            kLeaveAsIs = 0,
            kFeetToMeters,
            kCentimetersToMeters,
            kInchesToMeters,
            kCustomConversion
        };

        internal static class MaterialProperties
        {
            internal static readonly int MainTexID = Shader.PropertyToID("_MainTex");
            internal static readonly int ColorTintID = Shader.PropertyToID("_ColorTint");

            internal static readonly int NormalMapKwToggleID = Shader.PropertyToID("_NormalMapKwToggle");
            internal static readonly int NormalMapID = Shader.PropertyToID("_NormalMap");

            internal static readonly int ExtraMapKwToggleID = Shader.PropertyToID("_ExtraMapKwToggle");
            internal static readonly int ExtraTexID = Shader.PropertyToID("_ExtraTex");
            internal static readonly int GlossinessID = Shader.PropertyToID("_Glossiness");
            internal static readonly int MetallicID = Shader.PropertyToID("_Metallic");

            internal static readonly int SubsurfaceKwToggleID = Shader.PropertyToID("_SubsurfaceKwToggle");
            internal static readonly int SubsurfaceTexID = Shader.PropertyToID("_SubsurfaceTex");
            internal static readonly int SubsurfaceColorID = Shader.PropertyToID("_SubsurfaceColor");
            internal static readonly int AlphaClipThresholdID = Shader.PropertyToID("_AlphaClipThreshold");
            internal static readonly int TransmissionScaleID = Shader.PropertyToID("_TransmissionScale");
            internal static readonly int DiffusionProfileAssetID = Shader.PropertyToID("_Diffusion_Profile_Asset");
            internal static readonly int DiffusionProfileID = Shader.PropertyToID("_DiffusionProfile");

            internal static readonly int HueVariationKwToggleID = Shader.PropertyToID("_HueVariationKwToggle");
            internal static readonly int HueVariationColorID = Shader.PropertyToID("_HueVariationColor");

            internal static readonly int LeafFacingKwToggleID = Shader.PropertyToID("_LeafFacingKwToggle");
            internal static readonly int BillboardKwToggleID = Shader.PropertyToID("_BillboardKwToggle");
            internal static readonly int DoubleSidedToggleID = Shader.PropertyToID("_DoubleSidedKwToggle");
            internal static readonly int DoubleSidedNormalModeID = Shader.PropertyToID("_DoubleSidedNormalMode");
            internal static readonly int BackfaceNormalModeID = Shader.PropertyToID("_BackfaceNormalMode");
            internal static readonly int TwoSidedID = Shader.PropertyToID("_TwoSided");

            internal static readonly int WindSharedKwToggle = Shader.PropertyToID("_WIND_SHARED");
            internal static readonly int WindBranch2KwToggle = Shader.PropertyToID("_WIND_BRANCH2");
            internal static readonly int WindBranch1KwToggle = Shader.PropertyToID("_WIND_BRANCH1");
            internal static readonly int WindRippleKwToggle = Shader.PropertyToID("_WIND_RIPPLE");
            internal static readonly int WindShimmerKwToggle = Shader.PropertyToID("_WIND_SHIMMER");
        }

        internal static class MaterialKeywords
        {
            internal static readonly string VBSetupStandardID = "VB_SETUP_STANDARD";
            internal static readonly string VBSetupBranch2ID = "VB_SETUP_BRANCH2";
            internal static readonly string VBSetupCameraFacingID = "VB_SETUP_CAMERA_FACING";
            internal static readonly string VBSetupBranch2AndCameraFacingID = "VB_SETUP_BRANCH2_AND_CAMERA_FACING";
            internal static readonly string VBSetupBillboardID = "VB_SETUP_BILLBOARD";
            internal static readonly string VBSetupID = "VB_SETUP";
            internal static readonly string BillboardID = "_BILLBOARD";
        }
    }

    static class SpeedTreeImporterCommonEditor
    {
        internal class Styles
        {
            // Meshes labels
            public static GUIContent MeshesHeader = EditorGUIUtility.TrTextContent("Meshes");
            public static GUIContent UnitConversion = EditorGUIUtility.TrTextContent("Unit Conversion", "Select the unit conversion to apply to the imported SpeedTree asset.");
            public static GUIContent ScaleFactor = EditorGUIUtility.TrTextContent("Scale Factor", "How much to scale the tree model, interpreting the exported units as meters. Must be positive.");
            public static GUIContent[] UnitConversionNames =
            {
                  new GUIContent("Leave As Is")
                , new GUIContent("ft to m")
                , new GUIContent("cm to m")
                , new GUIContent("inch to m")
                , new GUIContent("Custom")
            };

            // Materials labels
            public static GUIContent MaterialHeader = EditorGUIUtility.TrTextContent("Material");
            public static GUIContent MainColor = EditorGUIUtility.TrTextContent("Main Color", "The color modulating the diffuse lighting component.");
            public static GUIContent EnableColorVariation = EditorGUIUtility.TrTextContent("Color Variation", "Color is determined by linearly interpolating between the Main Color & Color Variation values based on the world position X, Y and Z values");
            public static GUIContent EnableBump = EditorGUIUtility.TrTextContent("Normal Map", "Enable normal (Bump) mapping.");
            public static GUIContent EnableSubsurface = EditorGUIUtility.TrTextContent("Subsurface Scattering", "Enable subsurface scattering effects.");
            public static GUIContent HueVariation = EditorGUIUtility.TrTextContent("Variation Color (RGB), Intensity (A)", "Tint the tree with the Variation Color");
            public static GUIContent AlphaTestRef = EditorGUIUtility.TrTextContent("Alpha Cutoff", "The alpha-test reference value.");
            public static GUIContent TransmissionScale = EditorGUIUtility.TrTextContent("Transmission Scale", "The transmission scale value.");

            // Lighting labels
            public static GUIContent LightingHeader = EditorGUIUtility.TrTextContent("Lighting");
            public static GUIContent CastShadows = EditorGUIUtility.TrTextContent("Cast Shadows", "The tree casts shadow");
            public static GUIContent ReceiveShadows = EditorGUIUtility.TrTextContent("Receive Shadows", "The tree receives shadow");
            public static GUIContent UseLightProbes = EditorGUIUtility.TrTextContent("Light Probes", "The tree uses light probe for lighting"); // TODO: update help text
            public static GUIContent UseReflectionProbes = EditorGUIUtility.TrTextContent("Reflection Probes", "The tree uses reflection probe for rendering"); // TODO: update help text
            public static GUIContent[] ReflectionProbeUsageNames = GetReflectionProbeUsageNames();

            public static GUIContent[] GetReflectionProbeUsageNames()
            {
                string[] names = Enum.GetNames(typeof(ReflectionProbeUsage));
                GUIContent[] probUsageNames = new GUIContent[names.Length];

                for (int i = 0; i < names.Length; ++i)
                {
                    string varName = ObjectNames.NicifyVariableName(names[i]);
                    probUsageNames[i] = (new GUIContent(varName));
                }

                return probUsageNames;
            }

            // Additional Settings labels
            public static GUIContent AdditionalSettingsHeader = EditorGUIUtility.TrTextContent("Additional Settings");
            public static GUIContent MotionVectorMode = EditorGUIUtility.TrTextContent("Motion Vectors", "Motion vector mode to set for the mesh renderer of each LOD object");

            // Wind labels
            public static GUIContent WindHeader = EditorGUIUtility.TrTextContent("Wind");
            public static GUIContent WindQuality = EditorGUIUtility.TrTextContent("Wind Quality", "Controls the wind effect's quality.");
            public static GUIContent[] MotionVectorModeNames =  // Match SharedRendererDataTypes.h / enum MotionVectorGenerationMode
{
                  new GUIContent("Camera Motion Only")  // kMotionVectorCamera = 0,    // Use camera motion for motion vectors
                , new GUIContent("Per Object Motion")   // kMotionVectorObject,        // Use a per object motion vector pass for this object
                , new GUIContent("Force No Motion")     // kMotionVectorForceNoMotion, // Force no motion for this object (0 into motion buffer)
            };

            public static GUIContent[] GetWindQualityNames()
            {
                GUIContent[] windQualityNames = new GUIContent[SpeedTreeImporter.windQualityNames.Length];

                for (int i = 0; i < SpeedTreeImporter.windQualityNames.Length; ++i)
                {
                    windQualityNames[i] = new GUIContent(SpeedTreeImporter.windQualityNames[i]);
                }

                return windQualityNames;
            }

            // LOD labels
            public static GUIContent LODHeader = EditorGUIUtility.TrTextContent("LOD");
            public static GUIContent ResetLOD = EditorGUIUtility.TrTextContent("Reset LOD to...", "Unify the LOD settings for all selected assets");
            public static GUIContent SmoothLOD = EditorGUIUtility.TrTextContent("Smooth Transitions", "Toggles smooth LOD transitions");
            public static GUIContent AnimateCrossFading = EditorGUIUtility.TrTextContent("Animate Cross-fading", "Cross-fading is animated instead of being calculated by distance");
            public static GUIContent CrossFadeWidth = EditorGUIUtility.TrTextContent("Crossfade Width", "Proportion of the last 3D mesh LOD region width which is used for cross-fading to billboard tree");
            public static GUIContent FadeOutWidth = EditorGUIUtility.TrTextContent("Fade Out Width", "The proportion of the billboard LOD region width that is used to fade out the billboard.");

            public static GUIContent EnableLodCustomizationsWarn = EditorGUIUtility.TrTextContent("Customizing LOD options may help with tuning the GPU performance but will likely negatively impact the instanced draw batching, i.e. CPU performance.\nPlease use the per-LOD customizations with careful memory and performance profiling for both CPU and GPU and remember that these options are a trade-off rather than a free win.");
            public static GUIContent BillboardSettingsHelp = EditorGUIUtility.TrTextContent("Billboard options are separate from the 3D model options shown above.\nChange the options below for influencing billboard rendering.");
            public static GUIContent MultiSelectionLODNotSupported = EditorGUIUtility.TrTextContent("Multi-selection is not supported for LOD settings.");

            public static GUIContent ApplyAndGenerate = EditorGUIUtility.TrTextContent("Apply & Generate Materials", "Apply current importer settings and generate asset materials with the new settings.");
        }

        static internal void ShowMeshGUI(
            ref SerializedProperty unitConversionEnumValue,
            ref SerializedProperty scaleFactor)
        {
            GUILayout.Label(Styles.MeshesHeader, EditorStyles.boldLabel);

            EditorGUILayout.Popup(unitConversionEnumValue, Styles.UnitConversionNames, Styles.UnitConversion);

            bool bShowCustomScaleFactor = unitConversionEnumValue.intValue == Styles.UnitConversionNames.Length - 1;
            if (bShowCustomScaleFactor)
            {
                EditorGUILayout.PropertyField(scaleFactor, Styles.ScaleFactor);
                if (scaleFactor.floatValue < 0f)
                {
                    scaleFactor.floatValue = 0f;
                }
                if (scaleFactor.floatValue == 0f)
                {
                    EditorGUILayout.HelpBox("Scale factor must be positive.", MessageType.Warning);
                }
            }
        }

        static internal void ShowMaterialGUI(
            ref SerializedProperty mainColor,
            ref SerializedProperty enableHueVariation,
            ref SerializedProperty hueVariation,
            ref SerializedProperty alphaTestRef,
            ref SerializedProperty enableBumpMapping,
            ref SerializedProperty enableSubsurfaceScattering,
            bool renderHueVariationDropdown = false,
            bool renderAlphaTestRef = false)
        {
            EditorGUILayout.LabelField(Styles.MaterialHeader, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(mainColor, Styles.MainColor);
            EditorGUILayout.PropertyField(enableHueVariation, Styles.EnableColorVariation);

            if (renderHueVariationDropdown)
            {
                EditorGUILayout.PropertyField(hueVariation, Styles.HueVariation);
            }

            if (renderAlphaTestRef)
                EditorGUILayout.Slider(alphaTestRef, 0f, 1f, Styles.AlphaTestRef);

            EditorGUILayout.PropertyField(enableBumpMapping, Styles.EnableBump);
            EditorGUILayout.PropertyField(enableSubsurfaceScattering, Styles.EnableSubsurface);
        }

        static internal void ShowLightingGUI(
            ref SerializedProperty enableShadowCasting,
            ref SerializedProperty enableShadowReceiving,
            ref SerializedProperty enableLightProbeUsage,
            ref SerializedProperty reflectionProbeUsage)
        {
            GUILayout.Label(Styles.LightingHeader, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(enableShadowCasting, Styles.CastShadows);

            // from the docs page: https://docs.unity3d.com/Manual/SpeedTree.html
            // Known issues: As with any other renderer, the Receive Shadows option has no effect while using deferred rendering.
            // TODO: test and conditionally expose this field
            using (new EditorGUI.DisabledScope(!UnityEngine.Rendering.SupportedRenderingFeatures.active.receiveShadows))
            {
                EditorGUILayout.PropertyField(enableShadowReceiving, Styles.ReceiveShadows);
            }

            EditorGUILayout.PropertyField(enableLightProbeUsage, Styles.UseLightProbes);
        }

        static internal void ShowAdditionalSettingsGUI(
            ref SerializedProperty motionVectorModeEnumValue,
            ref SerializedProperty generateColliders,
            ref SerializedProperty generateRigidbody)
        {
            GUILayout.Label(Styles.AdditionalSettingsHeader, EditorStyles.boldLabel);
            EditorGUILayout.Popup(motionVectorModeEnumValue, Styles.MotionVectorModeNames, Styles.MotionVectorMode);

            EditorGUILayout.PropertyField(generateColliders);
            EditorGUILayout.PropertyField(generateRigidbody);
        }

        static internal void ShowWindGUI(
            ref SerializedProperty bestWindQuality,
            ref SerializedProperty selectedWindQuality)
        {
            GUILayout.Label(Styles.WindHeader, EditorStyles.boldLabel);

            int NumAvailableWindQualityOptions = 1 + bestWindQuality.intValue; // 0 is None, we want at least 1 value
            ArraySegment<GUIContent> availableWindQualityOptions = new ArraySegment<GUIContent>(Styles.GetWindQualityNames(), 0, NumAvailableWindQualityOptions);
            EditorGUILayout.Popup(selectedWindQuality, availableWindQualityOptions.ToArray(), Styles.WindQuality);
        }

        static internal void ShowLODGUI(
            ref SerializedProperty enableSmoothLOD,
            ref SerializedProperty animateCrossFading,
            ref SerializedProperty billboardTransitionCrossFadeWidth,
            ref SerializedProperty fadeOutWidth,
            ref AnimBool showSmoothLODOptions,
            ref AnimBool showCrossFadeWidthOptions)
        {
            showSmoothLODOptions.target = enableSmoothLOD.hasMultipleDifferentValues || enableSmoothLOD.boolValue;
            showCrossFadeWidthOptions.target = animateCrossFading.hasMultipleDifferentValues || !animateCrossFading.boolValue;

            GUILayout.Label(Styles.LODHeader, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(enableSmoothLOD, Styles.SmoothLOD);

            using (new EditorGUI.IndentLevelScope())
            {
                // Note: FadeGroupScope doesn't work here, as if the 'faded' is not updated correctly.

                if (EditorGUILayout.BeginFadeGroup(showSmoothLODOptions.faded))
                {
                    EditorGUILayout.PropertyField(animateCrossFading, Styles.AnimateCrossFading);

                    if (EditorGUILayout.BeginFadeGroup(showCrossFadeWidthOptions.faded))
                    {
                        EditorGUILayout.Slider(billboardTransitionCrossFadeWidth, 0.0f, 1.0f, Styles.CrossFadeWidth);
                        EditorGUILayout.Slider(fadeOutWidth, 0.0f, 1.0f, Styles.FadeOutWidth);
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                EditorGUILayout.EndFadeGroup();
            }
        }
    }
}
