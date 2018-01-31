// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.Build;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using EditorGraphicsSettings = UnityEditor.Rendering.EditorGraphicsSettings;
using TierSettings = UnityEditor.Rendering.TierSettings;
using GraphicsTier = UnityEngine.Rendering.GraphicsTier;
using ShaderQuality = UnityEditor.Rendering.ShaderQuality;

// these are smallish editor's we use for GraphicsSettings editing

namespace UnityEditor
{
    //
    // builtin shaders
    //

    internal partial class GraphicsSettingsWindow
    {
        internal class BuiltinShaderSettings
        {
            internal enum BuiltinShaderMode
            {
                None = 0,
                Builtin,
                Custom
            }

            readonly SerializedProperty m_Mode;
            readonly SerializedProperty m_Shader;
            readonly GUIContent         m_Label;

            internal BuiltinShaderSettings(string label, string name, SerializedObject serializedObject)
            {
                m_Mode      = serializedObject.FindProperty(name + ".m_Mode");
                m_Shader    = serializedObject.FindProperty(name + ".m_Shader");
                m_Label     = EditorGUIUtility.TextContent(label);
            }

            internal void DoGUI()
            {
                EditorGUILayout.PropertyField(m_Mode, m_Label);
                if (m_Mode.intValue == (int)BuiltinShaderMode.Custom)
                    EditorGUILayout.PropertyField(m_Shader);
            }
        }

        internal class BuiltinShadersEditor : Editor
        {
            BuiltinShaderSettings m_Deferred;
            BuiltinShaderSettings m_DeferredReflections;
            BuiltinShaderSettings m_LegacyDeferred;
            BuiltinShaderSettings m_ScreenSpaceShadows;
            BuiltinShaderSettings m_DepthNormals;
            BuiltinShaderSettings m_MotionVectors;
            BuiltinShaderSettings m_LightHalo;
            BuiltinShaderSettings m_LensFlare;

            string deferredString       { get { return LocalizationDatabase.GetLocalizedString("Deferred|Shader used for Deferred Shading."); } }
            string deferredReflString   { get { return LocalizationDatabase.GetLocalizedString("Deferred Reflections|Shader used for Deferred reflection probes."); } }
            string legacyDeferredString { get { return LocalizationDatabase.GetLocalizedString("Legacy Deferred|Shader used for Legacy (light prepass) Deferred Lighting."); } }
            string screenShadowsString  { get { return LocalizationDatabase.GetLocalizedString("Screen Space Shadows|Shader used for screen-space cascaded shadows."); } }
            string depthNormalsString   { get { return LocalizationDatabase.GetLocalizedString("Depth Normals|Shader used for depth and normals texture when enabled on a Camera."); } }
            string motionVectorsString  { get { return LocalizationDatabase.GetLocalizedString("Motion Vectors|Shader for generation of Motion Vectors when the rendering camera has renderMotionVectors set to true."); } }
            string lightHaloString      { get { return LocalizationDatabase.GetLocalizedString("Light Halo|Default Shader used for light halos."); } }
            string lensFlareString      { get { return LocalizationDatabase.GetLocalizedString("Lens Flare|Default Shader used for lens flares."); } }

            public void OnEnable()
            {
                m_Deferred              = new BuiltinShaderSettings(deferredString, "m_Deferred", serializedObject);
                m_DeferredReflections   = new BuiltinShaderSettings(deferredReflString, "m_DeferredReflections", serializedObject);
                m_LegacyDeferred        = new BuiltinShaderSettings(legacyDeferredString, "m_LegacyDeferred", serializedObject);
                m_ScreenSpaceShadows    = new BuiltinShaderSettings(screenShadowsString, "m_ScreenSpaceShadows", serializedObject);
                m_DepthNormals          = new BuiltinShaderSettings(depthNormalsString, "m_DepthNormals", serializedObject);
                m_MotionVectors         = new BuiltinShaderSettings(motionVectorsString, "m_MotionVectors", serializedObject);
                m_LightHalo             = new BuiltinShaderSettings(lightHaloString, "m_LightHalo", serializedObject);
                m_LensFlare             = new BuiltinShaderSettings(lensFlareString, "m_LensFlare", serializedObject);
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                m_Deferred.DoGUI();

                // deferred reflections being off affects forward vs deferred style probe rendering;
                // need to reload shaders for new platform macro to live update
                EditorGUI.BeginChangeCheck();
                m_DeferredReflections.DoGUI();
                if (EditorGUI.EndChangeCheck())
                    ShaderUtil.ReloadAllShaders();

                m_LegacyDeferred.DoGUI();
                m_ScreenSpaceShadows.DoGUI();
                m_DepthNormals.DoGUI();
                m_MotionVectors.DoGUI();
                m_LightHalo.DoGUI();
                m_LensFlare.DoGUI();

                serializedObject.ApplyModifiedProperties();
            }
        }
    }


    //
    // always included shaders
    //


    internal partial class GraphicsSettingsWindow
    {
        internal class AlwaysIncludedShadersEditor : Editor
        {
            SerializedProperty m_AlwaysIncludedShaders;

            public void OnEnable()
            {
                m_AlwaysIncludedShaders = serializedObject.FindProperty("m_AlwaysIncludedShaders");
                m_AlwaysIncludedShaders.isExpanded = true;
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                EditorGUILayout.PropertyField(m_AlwaysIncludedShaders, true);

                serializedObject.ApplyModifiedProperties();
            }
        }
    }


    //
    // shader stripping
    //


    internal partial class GraphicsSettingsWindow
    {
        internal class ShaderStrippingEditor : Editor
        {
            SerializedProperty m_LightmapStripping;
            SerializedProperty m_LightmapKeepPlain;
            SerializedProperty m_LightmapKeepDirCombined;
            SerializedProperty m_LightmapKeepDynamicPlain;
            SerializedProperty m_LightmapKeepDynamicDirCombined;
            SerializedProperty m_LightmapKeepShadowMask;
            SerializedProperty m_LightmapKeepSubtractive;
            SerializedProperty m_FogStripping;
            SerializedProperty m_FogKeepLinear;
            SerializedProperty m_FogKeepExp;
            SerializedProperty m_FogKeepExp2;
            SerializedProperty m_InstancingStripping;

            public void OnEnable()
            {
                m_LightmapStripping = serializedObject.FindProperty("m_LightmapStripping");
                m_LightmapKeepPlain = serializedObject.FindProperty("m_LightmapKeepPlain");
                m_LightmapKeepDirCombined = serializedObject.FindProperty("m_LightmapKeepDirCombined");
                m_LightmapKeepDynamicPlain = serializedObject.FindProperty("m_LightmapKeepDynamicPlain");
                m_LightmapKeepDynamicDirCombined = serializedObject.FindProperty("m_LightmapKeepDynamicDirCombined");
                m_LightmapKeepShadowMask = serializedObject.FindProperty("m_LightmapKeepShadowMask");
                m_LightmapKeepSubtractive = serializedObject.FindProperty("m_LightmapKeepSubtractive");
                m_FogStripping = serializedObject.FindProperty("m_FogStripping");
                m_FogKeepLinear = serializedObject.FindProperty("m_FogKeepLinear");
                m_FogKeepExp = serializedObject.FindProperty("m_FogKeepExp");
                m_FogKeepExp2 = serializedObject.FindProperty("m_FogKeepExp2");
                m_InstancingStripping = serializedObject.FindProperty("m_InstancingStripping");
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                bool calcLightmapStripping = false, calcFogStripping = false;

                EditorGUILayout.PropertyField(m_LightmapStripping, Styles.lightmapModes);

                if (m_LightmapStripping.intValue != 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_LightmapKeepPlain, Styles.lightmapPlain);
                    EditorGUILayout.PropertyField(m_LightmapKeepDirCombined, Styles.lightmapDirCombined);
                    EditorGUILayout.PropertyField(m_LightmapKeepDynamicPlain, Styles.lightmapDynamicPlain);
                    EditorGUILayout.PropertyField(m_LightmapKeepDynamicDirCombined, Styles.lightmapDynamicDirCombined);
                    EditorGUILayout.PropertyField(m_LightmapKeepShadowMask, Styles.lightmapKeepShadowMask);
                    EditorGUILayout.PropertyField(m_LightmapKeepSubtractive, Styles.lightmapKeepSubtractive);
                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(GUIContent.Temp(" "), EditorStyles.miniButton);

                    if (GUILayout.Button(Styles.lightmapFromScene, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        calcLightmapStripping = true;

                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(m_FogStripping, Styles.fogModes);
                if (m_FogStripping.intValue != 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_FogKeepLinear, Styles.fogLinear);
                    EditorGUILayout.PropertyField(m_FogKeepExp, Styles.fogExp);
                    EditorGUILayout.PropertyField(m_FogKeepExp2, Styles.fogExp2);
                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(GUIContent.Temp(" "), EditorStyles.miniButton);

                    if (GUILayout.Button(Styles.fogFromScene, EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        calcFogStripping = true;

                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space();
                }

                EditorGUILayout.PropertyField(m_InstancingStripping, Styles.instancingVariants);

                serializedObject.ApplyModifiedProperties();

                // need to do these after ApplyModifiedProperties, since it changes their values from native code
                if (calcLightmapStripping)
                    ShaderUtil.CalculateLightmapStrippingFromCurrentScene();
                if (calcFogStripping)
                    ShaderUtil.CalculateFogStrippingFromCurrentScene();
            }

            internal partial class Styles
            {
                public static readonly GUIContent shaderSettings = EditorGUIUtility.TrTextContent("Platform shader settings");
                public static readonly GUIContent builtinSettings = EditorGUIUtility.TrTextContent("Built-in shader settings");
                public static readonly GUIContent shaderPreloadSettings = EditorGUIUtility.TrTextContent("Shader preloading");

                public static readonly GUIContent lightmapModes = EditorGUIUtility.TrTextContent("Lightmap Modes");
                public static readonly GUIContent lightmapPlain = EditorGUIUtility.TrTextContent("Baked Non-Directional", "Include support for baked non-directional lightmaps.");
                public static readonly GUIContent lightmapDirCombined = EditorGUIUtility.TrTextContent("Baked Directional", "Include support for baked directional lightmaps.");
                public static readonly GUIContent lightmapKeepShadowMask = EditorGUIUtility.TrTextContent("Baked Shadowmask", "Include support for baked shadow occlusion.");
                public static readonly GUIContent lightmapKeepSubtractive = EditorGUIUtility.TrTextContent("Baked Subtractive", "Include support for baked substractive lightmaps.");
                public static readonly GUIContent lightmapDynamicPlain = EditorGUIUtility.TrTextContent("Realtime Non-Directional", "Include support for realtime non-directional lightmaps.");
                public static readonly GUIContent lightmapDynamicDirCombined = EditorGUIUtility.TrTextContent("Realtime Directional", "Include support for realtime directional lightmaps.");
                public static readonly GUIContent lightmapFromScene = EditorGUIUtility.TrTextContent("Import From Current Scene", "Calculate lightmap modes used by the current scene.");

                public static readonly GUIContent fogModes = EditorGUIUtility.TrTextContent("Fog Modes");
                public static readonly GUIContent fogLinear = EditorGUIUtility.TrTextContent("Linear", "Include support for Linear fog.");
                public static readonly GUIContent fogExp = EditorGUIUtility.TrTextContent("Exponential", "Include support for Exponential fog.");
                public static readonly GUIContent fogExp2 = EditorGUIUtility.TrTextContent("Exponential Squared", "Include support for Exponential Squared fog.");
                public static readonly GUIContent fogFromScene = EditorGUIUtility.TrTextContent("Import From Current Scene", "Calculate fog modes used by the current scene.");

                public static readonly GUIContent instancingVariants = EditorGUIUtility.TrTextContent("Instancing Variants");

                public static readonly GUIContent shaderPreloadSave = EditorGUIUtility.TrTextContent("Save to asset...", "Save currently tracked shaders into a Shader Variant Manifest asset.");
                public static readonly GUIContent shaderPreloadClear = EditorGUIUtility.TrTextContent("Clear", "Clear currently tracked shader variant information.");
            }
        }
    }


    //
    // preloaded shaders
    //


    internal partial class GraphicsSettingsWindow
    {
        internal class ShaderPreloadEditor : Editor
        {
            SerializedProperty m_PreloadedShaders;

            public void OnEnable()
            {
                m_PreloadedShaders = serializedObject.FindProperty("m_PreloadedShaders");
                m_PreloadedShaders.isExpanded = true;
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.PropertyField(m_PreloadedShaders, true);

                EditorGUILayout.Space();
                GUILayout.Label(
                    string.Format("Currently tracked: {0} shaders {1} total variants",
                        ShaderUtil.GetCurrentShaderVariantCollectionShaderCount(), ShaderUtil.GetCurrentShaderVariantCollectionVariantCount()
                        )
                    );

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Styles.shaderPreloadSave, EditorStyles.miniButton))
                {
                    string message = "Save shader variant collection";
                    string assetPath = EditorUtility.SaveFilePanelInProject("Save Shader Variant Collection", "NewShaderVariants", "shadervariants", message, ProjectWindowUtil.GetActiveFolderPath());
                    if (!string.IsNullOrEmpty(assetPath))
                        ShaderUtil.SaveCurrentShaderVariantCollection(assetPath);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button(Styles.shaderPreloadClear, EditorStyles.miniButton))
                    ShaderUtil.ClearCurrentShaderVariantCollection();
                EditorGUILayout.EndHorizontal();

                serializedObject.ApplyModifiedProperties();
            }

            internal partial class Styles
            {
                public static readonly GUIContent shaderPreloadSave = EditorGUIUtility.TrTextContent("Save to asset...", "Save currently tracked shaders into a Shader Variant Manifest asset.");
                public static readonly GUIContent shaderPreloadClear = EditorGUIUtility.TrTextContent("Clear", "Clear currently tracked shader variant information.");
            }
        }
    }


    //
    // tier settings
    //


    internal partial class GraphicsSettingsWindow
    {
        internal class TierSettingsEditor : Editor
        {
            public bool verticalLayout = false;

            internal void OnFieldLabelsGUI(bool vertical)
            {
                if (!vertical)
                    EditorGUILayout.LabelField(Styles.standardShaderSettings, EditorStyles.boldLabel);

                bool usingSRP = GraphicsSettings.renderPipelineAsset != null;
                if (!usingSRP)
                {
                    EditorGUILayout.LabelField(Styles.standardShaderQuality);
                    EditorGUILayout.LabelField(Styles.reflectionProbeBoxProjection);
                    EditorGUILayout.LabelField(Styles.reflectionProbeBlending);
                    EditorGUILayout.LabelField(Styles.detailNormalMap);
                    EditorGUILayout.LabelField(Styles.semitransparentShadows);
                }

                if (SupportedRenderingFeatures.active.rendererSupportsLightProbeProxyVolumes)
                    EditorGUILayout.LabelField(Styles.enableLPPV);

                if (!vertical)
                {
                    EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(Styles.renderingSettings, EditorStyles.boldLabel);
                }

                if (!usingSRP)
                {
                    EditorGUILayout.LabelField(Styles.cascadedShadowMaps);
                    EditorGUILayout.LabelField(Styles.prefer32BitShadowMaps);
                    EditorGUILayout.LabelField(Styles.useHDR);
                }

                EditorGUILayout.LabelField(Styles.hdrMode);

                if (!usingSRP)
                {
                    EditorGUILayout.LabelField(Styles.renderingPath);
                }

                if (SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime))
                    EditorGUILayout.LabelField(Styles.realtimeGICPUUsage);
            }

            // custom enum handling
            internal partial class Styles
            {
                public static readonly GUIContent[] shaderQualityName =
                { EditorGUIUtility.TrTextContent("Low"), EditorGUIUtility.TrTextContent("Medium"), EditorGUIUtility.TrTextContent("High") };
                public static readonly int[] shaderQualityValue =
                { (int)ShaderQuality.Low, (int)ShaderQuality.Medium, (int)ShaderQuality.High };

                public static readonly GUIContent[] renderingPathName =
                { EditorGUIUtility.TrTextContent("Forward"), EditorGUIUtility.TrTextContent("Deferred"), EditorGUIUtility.TrTextContent("Legacy Vertex Lit"), EditorGUIUtility.TrTextContent("Legacy Deferred (light prepass)") };
                public static readonly int[] renderingPathValue =
                { (int)RenderingPath.Forward, (int)RenderingPath.DeferredShading, (int)RenderingPath.VertexLit, (int)RenderingPath.DeferredLighting };

                public static readonly GUIContent[] hdrModeName =
                { EditorGUIUtility.TrTextContent("FP16"), EditorGUIUtility.TrTextContent("R11G11B10") };
                public static readonly int[] hdrModeValue =
                { (int)CameraHDRMode.FP16, (int)CameraHDRMode.R11G11B10};

                public static readonly GUIContent[] realtimeGICPUUsageName =
                { EditorGUIUtility.TrTextContent("Low"), EditorGUIUtility.TrTextContent("Medium"), EditorGUIUtility.TrTextContent("High"), EditorGUIUtility.TrTextContent("Unlimited")};
                public static readonly int[] realtimeGICPUUsageValue =
                { (int)RealtimeGICPUUsage.Low, (int)RealtimeGICPUUsage.Medium, (int)RealtimeGICPUUsage.High, (int)RealtimeGICPUUsage.Unlimited };
            }

            internal ShaderQuality ShaderQualityPopup(ShaderQuality sq)
            {
                return (ShaderQuality)EditorGUILayout.IntPopup((int)sq, Styles.shaderQualityName, Styles.shaderQualityValue);
            }

            internal RenderingPath RenderingPathPopup(RenderingPath rp)
            {
                return (RenderingPath)EditorGUILayout.IntPopup((int)rp, Styles.renderingPathName, Styles.renderingPathValue);
            }

            internal CameraHDRMode HDRModePopup(CameraHDRMode mode)
            {
                return (CameraHDRMode)EditorGUILayout.IntPopup((int)mode, Styles.hdrModeName, Styles.hdrModeValue);
            }

            internal RealtimeGICPUUsage RealtimeGICPUUsagePopup(RealtimeGICPUUsage usage)
            {
                return (RealtimeGICPUUsage)EditorGUILayout.IntPopup((int)usage, Styles.realtimeGICPUUsageName, Styles.realtimeGICPUUsageValue);
            }

            internal void OnTierGUI(BuildTargetGroup platform, GraphicsTier tier, bool vertical)
            {
                TierSettings ts = EditorGraphicsSettings.GetTierSettings(platform, tier);

                EditorGUI.BeginChangeCheck();


                if (!vertical)
                    EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);

                bool usingSRP = GraphicsSettings.renderPipelineAsset != null;
                if (!usingSRP)
                {
                    ts.standardShaderQuality = ShaderQualityPopup(ts.standardShaderQuality);
                    ts.reflectionProbeBoxProjection = EditorGUILayout.Toggle(ts.reflectionProbeBoxProjection);
                    ts.reflectionProbeBlending = EditorGUILayout.Toggle(ts.reflectionProbeBlending);
                    ts.detailNormalMap = EditorGUILayout.Toggle(ts.detailNormalMap);
                    ts.semitransparentShadows = EditorGUILayout.Toggle(ts.semitransparentShadows);
                }

                if (SupportedRenderingFeatures.active.rendererSupportsLightProbeProxyVolumes)
                    ts.enableLPPV = EditorGUILayout.Toggle(ts.enableLPPV);

                if (!vertical)
                {
                    EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
                }

                if (!usingSRP)
                {
                    ts.cascadedShadowMaps = EditorGUILayout.Toggle(ts.cascadedShadowMaps);
                    ts.prefer32BitShadowMaps = EditorGUILayout.Toggle(ts.prefer32BitShadowMaps);
                    ts.hdr = EditorGUILayout.Toggle(ts.hdr);
                }

                ts.hdrMode = HDRModePopup(ts.hdrMode);

                if (!usingSRP)
                    ts.renderingPath = RenderingPathPopup(ts.renderingPath);

                if (SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime))
                    ts.realtimeGICPUUsage = RealtimeGICPUUsagePopup(ts.realtimeGICPUUsage);

                if (EditorGUI.EndChangeCheck())
                {
                    // TODO: it should be doable in c# now as we "expose" GraphicsSettings anyway
                    EditorGraphicsSettings.RegisterUndoForGraphicsSettings();
                    EditorGraphicsSettings.SetTierSettings(platform, tier, ts);
                }
            }

            internal void OnGuiHorizontal(BuildTargetGroup platform)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical();
                EditorGUIUtility.labelWidth = 140;
                EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
                OnFieldLabelsGUI(false);
                EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(Styles.autoSettings, EditorStyles.boldLabel);
                EditorGUILayout.EndVertical();

                EditorGUIUtility.labelWidth = 50;
                foreach (GraphicsTier tier in Enum.GetValues(typeof(GraphicsTier)))
                {
                    bool autoSettings = EditorGraphicsSettings.AreTierSettingsAutomatic(platform, tier);

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(Styles.tierName[(int)tier], EditorStyles.boldLabel);
                    using (new EditorGUI.DisabledScope(autoSettings))
                        OnTierGUI(platform, tier, false);

                    EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    autoSettings = EditorGUILayout.Toggle(autoSettings);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorGraphicsSettings.RegisterUndoForGraphicsSettings();
                        EditorGraphicsSettings.MakeTierSettingsAutomatic(platform, tier, autoSettings);
                        EditorGraphicsSettings.OnUpdateTierSettingsImpl(platform, true);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUIUtility.labelWidth = 0;

                EditorGUILayout.EndHorizontal();
            }

            internal void OnGuiVertical(BuildTargetGroup platform)
            {
                foreach (GraphicsTier tier in Enum.GetValues(typeof(GraphicsTier)))
                {
                    bool autoSettings = EditorGraphicsSettings.AreTierSettingsAutomatic(platform, tier);
                    EditorGUI.BeginChangeCheck();
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUIUtility.labelWidth = 80;
                        EditorGUILayout.LabelField(Styles.tierName[(int)tier], EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        EditorGUIUtility.labelWidth = 75;
                        autoSettings = EditorGUILayout.Toggle(Styles.autoSettings, autoSettings);
                        GUILayout.EndHorizontal();
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorGraphicsSettings.RegisterUndoForGraphicsSettings();
                        EditorGraphicsSettings.MakeTierSettingsAutomatic(platform, tier, autoSettings);
                        EditorGraphicsSettings.OnUpdateTierSettingsImpl(platform, true);
                    }

                    using (new EditorGUI.DisabledScope(autoSettings))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.BeginVertical();
                        EditorGUIUtility.labelWidth = 140;
                        OnFieldLabelsGUI(true);
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical();
                        EditorGUIUtility.labelWidth = 50;
                        OnTierGUI(platform, tier, true);
                        EditorGUILayout.EndVertical();

                        GUILayout.EndHorizontal();
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUIUtility.labelWidth = 0;
            }

            public override void OnInspectorGUI()
            {
                BuildPlatform[] validPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();
                BuildTargetGroup platform = validPlatforms[EditorGUILayout.BeginPlatformGrouping(validPlatforms, null, GUIStyle.none)].targetGroup;

                if (verticalLayout)  OnGuiVertical(platform);
                else                OnGuiHorizontal(platform);

                EditorGUILayout.EndPlatformGrouping();
            }

            internal partial class Styles
            {
                public static readonly GUIContent[] tierName =
                { EditorGUIUtility.TrTextContent("Low (Tier1)"), EditorGUIUtility.TrTextContent("Medium (Tier 2)"), EditorGUIUtility.TrTextContent("High (Tier 3)") };

                public static readonly GUIContent empty = EditorGUIUtility.TextContent("");
                public static readonly GUIContent autoSettings = EditorGUIUtility.TrTextContent("Use Defaults");

                public static readonly GUIContent standardShaderSettings = EditorGUIUtility.TrTextContent("Standard Shader");
                public static readonly GUIContent renderingSettings = EditorGUIUtility.TrTextContent("Rendering");

                public static readonly GUIContent standardShaderQuality = EditorGUIUtility.TrTextContent("Standard Shader Quality");
                public static readonly GUIContent reflectionProbeBoxProjection = EditorGUIUtility.TrTextContent("Reflection Probes Box Projection");
                public static readonly GUIContent reflectionProbeBlending = EditorGUIUtility.TrTextContent("Reflection Probes Blending");
                public static readonly GUIContent detailNormalMap = EditorGUIUtility.TrTextContent("Detail Normal Map");
                public static readonly GUIContent cascadedShadowMaps = EditorGUIUtility.TrTextContent("Cascaded Shadows");
                public static readonly GUIContent prefer32BitShadowMaps = EditorGUIUtility.TrTextContent("Prefer 32 bit shadow maps");
                public static readonly GUIContent semitransparentShadows = EditorGUIUtility.TrTextContent("Enable Semitransparent Shadows");
                public static readonly GUIContent enableLPPV = EditorGUIUtility.TrTextContent("Enable Light Probe Proxy Volume");
                public static readonly GUIContent renderingPath = EditorGUIUtility.TrTextContent("Rendering Path");
                public static readonly GUIContent useHDR = EditorGUIUtility.TrTextContent("Use HDR");
                public static readonly GUIContent hdrMode = EditorGUIUtility.TrTextContent("HDR Mode");
                public static readonly GUIContent realtimeGICPUUsage = EditorGUIUtility.TrTextContent("Realtime Global Illumination CPU Usage", "How many CPU worker threads to create for Realtime Global Illumination lighting calculations in the Player. Increasing this makes the system react faster to changes in lighting at a cost of using more CPU time. The higher the CPU Usage value, the more worker threads are created for solving Realtime GI.");
            }
        }
    }
}
