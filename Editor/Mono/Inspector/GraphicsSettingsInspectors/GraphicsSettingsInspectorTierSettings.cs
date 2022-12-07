// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.AnimatedValues;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class TierSettingsWindow : EditorWindow
    {
        static TierSettingsWindow s_Instance;

        public static void CreateWindow()
        {
            s_Instance = GetWindow<TierSettingsWindow>();
            s_Instance.minSize = new Vector2(600, 300);
            s_Instance.titleContent = GraphicsSettingsInspectorTierSettings.Styles.tierSettings;
        }

        internal static TierSettingsWindow GetInstance()
        {
            return s_Instance;
        }

        SerializedObject m_SerializedObject;

        void OnEnable()
        {
            s_Instance = this;
            m_SerializedObject = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var graphicsSettingsInspectorTierSettings = new GraphicsSettingsInspectorTierSettings()
            {
                style =
                {
                    marginTop = 5,
                    marginBottom = 5,
                    marginLeft = 5,
                    marginRight = 5
                },
                UseAnimation = false
            };
            graphicsSettingsInspectorTierSettings.Initialize(m_SerializedObject);
            rootVisualElement.Add(graphicsSettingsInspectorTierSettings);
            RenderPipelineManager.activeRenderPipelineAssetChanged += RenderPipelineAssetChanged;
        }

        void RenderPipelineAssetChanged(RenderPipelineAsset previous, RenderPipelineAsset next)
        {
            if (next != null)
                Close();
        }

        void OnDisable()
        {
            RenderPipelineManager.activeRenderPipelineAssetChanged -= RenderPipelineAssetChanged;
            rootVisualElement.Clear();

            if (s_Instance == this)
                s_Instance = null;
        }
    }

    internal class GraphicsSettingsInspectorTierSettings : GraphicsSettingsElement
    {
        public new class UxmlFactory : UxmlFactory<GraphicsSettingsInspectorTierSettings, UxmlTraits>
        {
        }

        internal class Styles
        {
            public static readonly GUIContent[] shaderQualityName =
                { EditorGUIUtility.TrTextContent("Low"), EditorGUIUtility.TrTextContent("Medium"), EditorGUIUtility.TrTextContent("High") };

            public static readonly int[] shaderQualityValue =
                { (int)ShaderQuality.Low, (int)ShaderQuality.Medium, (int)ShaderQuality.High };

            public static readonly GUIContent[] renderingPathName =
                { EditorGUIUtility.TrTextContent("Forward"), EditorGUIUtility.TrTextContent("Deferred"), EditorGUIUtility.TrTextContent("Legacy Vertex Lit") };

            public static readonly int[] renderingPathValue =
                { (int)RenderingPath.Forward, (int)RenderingPath.DeferredShading, (int)RenderingPath.VertexLit };

            public static readonly GUIContent[] hdrModeName =
                { EditorGUIUtility.TrTextContent("FP16"), EditorGUIUtility.TrTextContent("R11G11B10") };

            public static readonly int[] hdrModeValue =
                { (int)CameraHDRMode.FP16, (int)CameraHDRMode.R11G11B10 };

            public static readonly GUIContent[] realtimeGICPUUsageName =
                { EditorGUIUtility.TrTextContent("Low"), EditorGUIUtility.TrTextContent("Medium"), EditorGUIUtility.TrTextContent("High"), EditorGUIUtility.TrTextContent("Unlimited") };

            public static readonly int[] realtimeGICPUUsageValue =
                { (int)RealtimeGICPUUsage.Low, (int)RealtimeGICPUUsage.Medium, (int)RealtimeGICPUUsage.High, (int)RealtimeGICPUUsage.Unlimited };

            public static readonly GUIContent showEditorWindow = EditorGUIUtility.TrTextContent("Open Editor...");
            public static readonly GUIContent closeEditorWindow = EditorGUIUtility.TrTextContent("Close Editor");
            public static readonly GUIContent tierSettings = EditorGUIUtility.TrTextContent("Tier Settings");

            public static readonly GUIContent[] tierName =
            {
                EditorGUIUtility.TrTextContent("Low (Tier 1)"), EditorGUIUtility.TrTextContent("Medium (Tier 2)"), EditorGUIUtility.TrTextContent("High (Tier 3)")
            };

            public static readonly GUIContent empty = EditorGUIUtility.TextContent("");
            public static readonly GUIContent autoSettingsLabel = EditorGUIUtility.TrTextContent("Use Defaults");

            public static readonly GUIContent standardShaderSettings = EditorGUIUtility.TrTextContent("Standard Shader");
            public static readonly GUIContent renderingSettings = EditorGUIUtility.TrTextContent("Rendering");

            public static readonly GUIContent standardShaderQuality = EditorGUIUtility.TrTextContent("Standard Shader Quality");

            public static readonly GUIContent reflectionProbeBoxProjection =
                EditorGUIUtility.TrTextContent("Reflection Probes Box Projection", "Enable projection for reflection UV mappings on Reflection Probes.");

            public static readonly GUIContent reflectionProbeBlending = EditorGUIUtility.TrTextContent("Reflection Probes Blending",
                "Gradually fade out one probe's cubemap while fading in the other's as the reflective object passes from one zone to the other.");

            public static readonly GUIContent detailNormalMap =
                EditorGUIUtility.TrTextContent("Detail Normal Map", "Enable Detail (secondary) Normal Map sampling for up-close viewing, if assigned.");

            public static readonly GUIContent cascadedShadowMaps = EditorGUIUtility.TrTextContent("Cascaded Shadows");

            public static readonly GUIContent prefer32BitShadowMaps =
                EditorGUIUtility.TrTextContent("Prefer 32-bit shadow maps", "Enable 32-bit float shadow map when you are targeting PS4 or platforms using DX11 or DX12.");

            public static readonly GUIContent semitransparentShadows = EditorGUIUtility.TrTextContent("Enable Semitransparent Shadows");

            public static readonly GUIContent enableLPPV =
                EditorGUIUtility.TrTextContent("Enable Light Probe Proxy Volume", "Enable rendering a 3D grid of interpolated Light Probes inside a Bounding Volume.");

            public static readonly GUIContent renderingPath = EditorGUIUtility.TrTextContent("Rendering Path",
                "Choose how Unity should render graphics. Different rendering paths affect the performance of your game, and how lighting and shading are calculated.");

            public static readonly GUIContent useHDR = EditorGUIUtility.TrTextContent("Use HDR", "Enable High Dynamic Range rendering for this tier.");
            public static readonly GUIContent hdrMode = EditorGUIUtility.TrTextContent("HDR Mode", "Color render texture format for the HDR buffer to use when HDR is enabled.");

            public static readonly GUIContent realtimeGICPUUsage = EditorGUIUtility.TrTextContent("Realtime Global Illumination CPU Usage",
                "How many CPU worker threads to create for Realtime Global Illumination lighting calculations in the Player. Increasing this makes the system react faster to changes in lighting at a cost of using more CPU time. The higher the CPU Usage value, the more worker threads are created for solving Realtime GI.");
        }

        public override bool BuiltinOnly => true;
        public bool UseAnimation { get; set; } = true;

        bool verticalLayout = true;

        // this is category animation is blatantly copied from PlayerSettingsEditor.cs
        bool m_ShowTierSettingsUI = true; // show by default, as otherwise users are confused
        AnimBool m_TierSettingsAnimator;

        protected override void Initialize()
        {
            var container = new IMGUIContainer(Draw);
            Add(container);

            if (UseAnimation)
                m_TierSettingsAnimator = new AnimBool(m_ShowTierSettingsUI, container.MarkDirtyRepaint);
        }

        void Draw()
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            if (GraphicsSettings.isScriptableRenderPipelineEnabled)
            {
                var window = TierSettingsWindow.GetInstance();
                if (window != null)
                    window.Close();
            }
            else
            {
                if (m_TierSettingsAnimator == null)
                    OnInspectorGUI();
                else
                    TierSettingsGUI();
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        void HandleEditorWindowButton()
        {
            var window = TierSettingsWindow.GetInstance();
            var text = window == null ? Styles.showEditorWindow : Styles.closeEditorWindow;
            if (!GUILayout.Button(text, EditorStyles.miniButton, GUILayout.Width(110)))
                return;

            if (window)
            {
                window.Close();
            }
            else
            {
                TierSettingsWindow.CreateWindow();
                TierSettingsWindow.GetInstance().Show();
            }
        }

        void TierSettingsGUI()
        {
            var enabled = GUI.enabled;
            GUI.enabled = true; // we don't want to disable the expand behavior
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(20));

            EditorGUILayout.BeginHorizontal();
            var r = GUILayoutUtility.GetRect(20, 21);
            r.x += 3;
            r.width += 6;
            m_ShowTierSettingsUI = EditorGUI.FoldoutTitlebar(r, Styles.tierSettings, m_ShowTierSettingsUI, true, EditorStyles.inspectorTitlebarFlat, EditorStyles.inspectorTitlebarText);
            HandleEditorWindowButton();
            EditorGUILayout.EndHorizontal();

            m_TierSettingsAnimator.target = m_ShowTierSettingsUI;
            GUI.enabled = enabled;

            if (EditorGUILayout.BeginFadeGroup(m_TierSettingsAnimator.faded) && TierSettingsWindow.GetInstance() == null)
                OnInspectorGUI();
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndVertical();
        }

        void OnInspectorGUI()
        {
            using var highlightScope = new EditorGUI.LabelHighlightScope(m_SettingsWindow.GetSearchText(), HighlightSelectionColor, HighlightColor);
            var validPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();
            var platform = validPlatforms[EditorGUILayout.BeginPlatformGrouping(validPlatforms, null, EditorStyles.frameBox)];

            if (verticalLayout) OnGuiVertical(platform);
            else OnGuiHorizontal(platform);

            EditorGUILayout.EndPlatformGrouping();
        }

        void OnFieldLabelsGUI(bool vertical)
        {
            var usingSRP = GraphicsSettings.currentRenderPipeline != null;
            if (!vertical)
                EditorGUILayout.LabelField(Styles.standardShaderSettings, EditorStyles.boldLabel);

            if (!usingSRP)
            {
                EditorGUILayout.LabelField(Styles.standardShaderQuality);
                EditorGUILayout.LabelField(Styles.reflectionProbeBoxProjection);
                EditorGUILayout.LabelField(Styles.reflectionProbeBlending);
                EditorGUILayout.LabelField(Styles.detailNormalMap);
                EditorGUILayout.LabelField(Styles.semitransparentShadows);
            }

            if (SupportedRenderingFeatures.active.lightProbeProxyVolumes)
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
                EditorGUILayout.LabelField(Styles.hdrMode);
                EditorGUILayout.LabelField(Styles.renderingPath);
            }

            if (SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime))
                EditorGUILayout.LabelField(Styles.realtimeGICPUUsage);
        }


        // custom enum handling
        ShaderQuality ShaderQualityPopup(ShaderQuality sq) =>
            (ShaderQuality)EditorGUILayout.IntPopup((int)sq, Styles.shaderQualityName, Styles.shaderQualityValue);

        RenderingPath RenderingPathPopup(RenderingPath rp) =>
            (RenderingPath)EditorGUILayout.IntPopup((int)rp, Styles.renderingPathName, Styles.renderingPathValue);

        CameraHDRMode HDRModePopup(CameraHDRMode mode) =>
            (CameraHDRMode)EditorGUILayout.IntPopup((int)mode, Styles.hdrModeName, Styles.hdrModeValue);

        RealtimeGICPUUsage RealtimeGICPUUsagePopup(RealtimeGICPUUsage usage) =>
            (RealtimeGICPUUsage)EditorGUILayout.IntPopup((int)usage, Styles.realtimeGICPUUsageName, Styles.realtimeGICPUUsageValue);


        void OnTierGUI(BuildPlatform platform, GraphicsTier tier, bool vertical)
        {
            var ts = EditorGraphicsSettings.GetTierSettings(platform.namedBuildTarget, tier);

            EditorGUI.BeginChangeCheck();


            if (!vertical)
                EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);

            var usingSRP = GraphicsSettings.currentRenderPipeline != null;
            if (!usingSRP)
            {
                ts.standardShaderQuality = ShaderQualityPopup(ts.standardShaderQuality);
                ts.reflectionProbeBoxProjection = EditorGUILayout.Toggle(ts.reflectionProbeBoxProjection);
                ts.reflectionProbeBlending = EditorGUILayout.Toggle(ts.reflectionProbeBlending);
                ts.detailNormalMap = EditorGUILayout.Toggle(ts.detailNormalMap);
                ts.semitransparentShadows = EditorGUILayout.Toggle(ts.semitransparentShadows);
            }

            if (SupportedRenderingFeatures.active.lightProbeProxyVolumes)
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
                ts.hdrMode = HDRModePopup(ts.hdrMode);
                ts.renderingPath = RenderingPathPopup(ts.renderingPath);
            }

            if (SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime))
                ts.realtimeGICPUUsage = RealtimeGICPUUsagePopup(ts.realtimeGICPUUsage);

            if (EditorGUI.EndChangeCheck())
            {
                // TODO: it should be doable in c# now as we "expose" GraphicsSettings anyway
                EditorGraphicsSettings.RegisterUndo();
                EditorGraphicsSettings.SetTierSettings(platform.namedBuildTarget, tier, ts);
            }
        }

        void OnGuiHorizontal(BuildPlatform platform)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = 140;
            EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
            OnFieldLabelsGUI(false);
            EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Styles.autoSettingsLabel, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUIUtility.labelWidth = 50;
            foreach (GraphicsTier tier in Enum.GetValues(typeof(GraphicsTier)))
            {
                bool autoSettings = EditorGraphicsSettings.AreTierSettingsAutomatic(platform.namedBuildTarget.ToBuildTargetGroup(), tier);

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(Styles.tierName[(int)tier], EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(autoSettings))
                    OnTierGUI(platform, tier, false);

                EditorGUILayout.LabelField(Styles.empty, EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                autoSettings = EditorGUILayout.Toggle(autoSettings);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorGraphicsSettings.RegisterUndo();
                    EditorGraphicsSettings.MakeTierSettingsAutomatic(platform.namedBuildTarget.ToBuildTargetGroup(), tier, autoSettings);
                    EditorGraphicsSettings.OnUpdateTierSettings(platform.namedBuildTarget.ToBuildTargetGroup(), true);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUIUtility.labelWidth = 0;

            EditorGUILayout.EndHorizontal();
        }

        void OnGuiVertical(BuildPlatform platform)
        {
            EditorGUILayout.BeginVertical();
            foreach (GraphicsTier tier in Enum.GetValues(typeof(GraphicsTier)))
            {
                var autoSettings = EditorGraphicsSettings.AreTierSettingsAutomatic(platform.namedBuildTarget.ToBuildTargetGroup(), tier);
                EditorGUI.BeginChangeCheck();
                {
                    GUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 80;
                    EditorGUILayout.LabelField(Styles.tierName[(int)tier], EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUIUtility.labelWidth = 80;
                    autoSettings = EditorGUILayout.Toggle(Styles.autoSettingsLabel, autoSettings);
                    GUILayout.EndHorizontal();
                }

                if (EditorGUI.EndChangeCheck())
                {
                    EditorGraphicsSettings.RegisterUndo();
                    EditorGraphicsSettings.MakeTierSettingsAutomatic(platform.namedBuildTarget.ToBuildTargetGroup(), tier, autoSettings);
                    EditorGraphicsSettings.OnUpdateTierSettings(platform.namedBuildTarget.ToBuildTargetGroup(), true);
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

            GUILayout.EndVertical();
            EditorGUIUtility.labelWidth = 0;
        }
    }
}
