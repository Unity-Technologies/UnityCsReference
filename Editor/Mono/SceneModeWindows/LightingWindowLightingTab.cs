// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class LightingWindowLightingTab
    {
        static class Styles
        {
            public static readonly GUIContent OtherSettings = EditorGUIUtility.TextContent("Other Settings");
            public static readonly GUIContent DebugSettings = EditorGUIUtility.TextContent("Debug Settings");
            public static readonly GUIContent LightProbeVisualization = EditorGUIUtility.TextContent("Light Probe Visualization");
            public static readonly GUIContent StatisticsCategory = EditorGUIUtility.TextContent("Category");
            public static readonly GUIContent StatisticsEnabled = EditorGUIUtility.TextContent("Enabled");
            public static readonly GUIContent StatisticsDisabled = EditorGUIUtility.TextContent("Disabled|The Light’s GameObject is active, but the Light component is disabled. These lights have no effect on the Scene.");
            public static readonly GUIContent StatisticsInactive = EditorGUIUtility.TextContent("Inactive|The Light’s GameObject is inactive. These lights have no effect on the Scene.");
            public static readonly GUIContent UpdateStatistics = EditorGUIUtility.TextContent("Update Statistics|Turn off to prevent statistics from being updated during play mode to improve performance.");

            public static readonly string StatisticsWarning = "Statistics are not updated during play mode. This behavior can be changed via Settings -> Debug Settings -> \"Update Statistics\"";
            public static GUIStyle StatsTableHeader  = new GUIStyle("preLabel");
            public static GUIStyle StatsTableContent = new GUIStyle(EditorStyles.whiteLabel);

            public static readonly GUIContent RealtimeLights = EditorGUIUtility.TextContent("Realtime Lights");
            public static readonly GUIContent MixedLights = EditorGUIUtility.TextContent("Mixed Lights");
            public static readonly GUIContent BakedLights = EditorGUIUtility.TextContent("Baked Lights");
            public static readonly GUIContent DynamicMeshes = EditorGUIUtility.TextContent("Dynamic Meshes");
            public static readonly GUIContent StaticMeshes = EditorGUIUtility.TextContent("Static Meshes");
            public static readonly GUIContent StaticMeshesIconWarning = EditorGUIUtility.TextContentWithIcon("|Baked Global Illumination is Enabled but there are no Static Meshes or Terrains in the Scene. Please enable the Lightmap Static property on the meshes you want included in baked lighting.", "console.warnicon");

            public static readonly GUIContent RealtimeEmissiveMaterials = EditorGUIUtility.TextContent("Realtime Emissive Materials");
            public static readonly GUIContent BakedEmissiveMaterials = EditorGUIUtility.TextContent("Baked Emissive Materials");
            public static readonly GUIContent LightProbeGroups = EditorGUIUtility.TextContent("Light Probe Groups");
            public static readonly GUIContent ReflectionProbes = EditorGUIUtility.TextContent("Reflection Probes");
        }

        Editor          m_LightingEditor;
        Editor          m_FogEditor;
        Editor          m_OtherRenderingEditor;
        bool            m_ShowOtherSettings = true;
        bool            m_ShowDebugSettings = false;
        bool            m_ShowProbeDebugSettings = false;
        bool            m_ShouldUpdateStatistics = true;
        const string    kShowOtherSettings = "kShowOtherSettings";
        const string    kShowDebugSettings = "kShowDebugSettings";
        const string    kUpdateStatistics = "kUpdateStatistics";
        Object          m_RenderSettings = null;

        LightModeValidator.Stats m_Stats;
        LightingWindowBakeSettings m_BakeSettings;

        Object renderSettings
        {
            get
            {
                if (m_RenderSettings == null)
                    m_RenderSettings = RenderSettings.GetRenderSettings();

                return m_RenderSettings;
            }
        }

        Editor lightingEditor
        {
            get
            {
                if (m_LightingEditor == null || m_LightingEditor.target == null)
                {
                    Editor.CreateCachedEditor(renderSettings, typeof(LightingEditor), ref m_LightingEditor);
                }

                return m_LightingEditor;
            }
        }

        Editor fogEditor
        {
            get
            {
                if (m_FogEditor == null || m_FogEditor.target == null)
                {
                    Editor.CreateCachedEditor(renderSettings, typeof(FogEditor), ref m_FogEditor);
                }

                return m_FogEditor;
            }
        }

        Editor otherRenderingEditor
        {
            get
            {
                if (m_OtherRenderingEditor == null || m_OtherRenderingEditor.target == null)
                {
                    Editor.CreateCachedEditor(renderSettings, typeof(OtherRenderingEditor), ref m_OtherRenderingEditor);
                }

                return m_OtherRenderingEditor;
            }
        }

        public void OnEnable()
        {
            m_BakeSettings = new LightingWindowBakeSettings();
            m_BakeSettings.OnEnable();

            m_ShowOtherSettings = SessionState.GetBool(kShowOtherSettings, true);
            m_ShowDebugSettings = SessionState.GetBool(kShowDebugSettings, false);
            m_ShouldUpdateStatistics = SessionState.GetBool(kUpdateStatistics, false);
        }

        public void OnDisable()
        {
            m_BakeSettings.OnDisable();

            SessionState.SetBool(kShowOtherSettings, m_ShowOtherSettings);
            SessionState.SetBool(kShowDebugSettings, m_ShowDebugSettings);
            SessionState.SetBool(kUpdateStatistics, m_ShouldUpdateStatistics);

            ClearCachedProperties();
        }

        void ClearCachedProperties()
        {
            if (m_LightingEditor != null)
            {
                Object.DestroyImmediate(m_LightingEditor);
                m_LightingEditor = null;
            }
            if (m_FogEditor != null)
            {
                Object.DestroyImmediate(m_FogEditor);
                m_FogEditor = null;
            }
            if (m_OtherRenderingEditor != null)
            {
                Object.DestroyImmediate(m_OtherRenderingEditor);
                m_OtherRenderingEditor = null;
            }
        }

        void DebugSettingsGUI()
        {
            m_ShowDebugSettings = EditorGUILayout.FoldoutTitlebar(m_ShowDebugSettings, Styles.DebugSettings, true);

            if (m_ShowDebugSettings)
            {
                EditorGUI.indentLevel++;

//                EditorGUI.BeginChangeCheck();
//                bool updateStatistics = EditorGUILayout.Toggle(Styles.UpdateStatistics, m_ShouldUpdateStatistics);
//
//                if (EditorGUI.EndChangeCheck())
//                {
//                    SessionState.SetBool(kUpdateStatistics, updateStatistics);
//                    m_ShouldUpdateStatistics = updateStatistics;
//                }

                m_ShowProbeDebugSettings = EditorGUILayout.Foldout(m_ShowProbeDebugSettings, Styles.LightProbeVisualization, true);

                if (m_ShowProbeDebugSettings)
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUI.indentLevel++;
                    LightProbeVisualization.lightProbeVisualizationMode = (LightProbeVisualization.LightProbeVisualizationMode)EditorGUILayout.EnumPopup(LightProbeVisualization.lightProbeVisualizationMode);
                    LightProbeVisualization.showInterpolationWeights = EditorGUILayout.Toggle("Display Weights", LightProbeVisualization.showInterpolationWeights);
                    LightProbeVisualization.showOcclusions = EditorGUILayout.Toggle("Display Occlusion", LightProbeVisualization.showOcclusions);
                    EditorGUI.indentLevel--;

                    if (EditorGUI.EndChangeCheck())
                        EditorApplication.SetSceneRepaintDirty();
                }
                EditorGUILayout.Space();

                m_BakeSettings.DeveloperBuildSettingsGUI();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }

        void OtherSettingsGUI()
        {
            m_ShowOtherSettings = EditorGUILayout.FoldoutTitlebar(m_ShowOtherSettings, Styles.OtherSettings, true);

            if (m_ShowOtherSettings)
            {
                EditorGUI.indentLevel++;

                fogEditor.OnInspectorGUI();
                otherRenderingEditor.OnInspectorGUI();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }

        public void OnGUI()
        {
            EditorGUIUtility.hierarchyMode = true;

            lightingEditor.OnInspectorGUI();
            m_BakeSettings.OnGUI();

            OtherSettingsGUI();
            DebugSettingsGUI();
        }

        delegate void DrawStats(GUIContent icon, GUIContent label, int enabled, int active, int inactive);

        public void StatisticsPreview(Rect r)
        {
            GUI.Box(r, "", "PreBackground");

            Styles.StatsTableHeader.alignment = TextAnchor.MiddleLeft;

            EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.Height(r.height));

            bool bUpdateStats = m_ShouldUpdateStatistics || !EditorApplication.isPlayingOrWillChangePlaymode;

            if (bUpdateStats)
                LightModeUtil.Get().AnalyzeScene(ref m_Stats);
            else
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(Styles.StatisticsWarning, MessageType.Info);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool markStaticMeshes = LightModeUtil.Get().AreBakedLightmapsEnabled() && m_Stats.receiverMask == 0;

            using (new EditorGUI.DisabledScope(!bUpdateStats))
            {
                GUILayoutOption[] opts_icon = { GUILayout.MinWidth(16), GUILayout.MaxWidth(16) };
                GUILayoutOption[] opts_name = { GUILayout.MinWidth(175), GUILayout.MaxWidth(200) };
                GUILayoutOption[] opts = { GUILayout.MinWidth(10), GUILayout.MaxWidth(65) };

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GUIContent.none, Styles.StatsTableHeader, opts_icon);
                    EditorGUILayout.LabelField(Styles.StatisticsCategory, Styles.StatsTableHeader, opts_name);
                    EditorGUILayout.LabelField(Styles.StatisticsEnabled, Styles.StatsTableHeader, opts);
                    EditorGUILayout.LabelField(Styles.StatisticsDisabled, Styles.StatsTableHeader, opts);
                    EditorGUILayout.LabelField(Styles.StatisticsInactive, Styles.StatsTableHeader, opts);
                }

                DrawStats drawStats = (GUIContent icon, GUIContent label, int enabled, int active, int inactive) =>
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(icon, Styles.StatsTableContent, opts_icon);
                            EditorGUILayout.LabelField(label, Styles.StatsTableContent, opts_name);
                            Rect r1 = GUILayoutUtility.GetLastRect();
                            EditorGUILayout.LabelField(enabled.ToString(), Styles.StatsTableContent, opts);
                            r1.xMax = GUILayoutUtility.GetLastRect().xMax;
                            EditorGUILayout.LabelField(active.ToString(), Styles.StatsTableContent, opts);
                            EditorGUILayout.LabelField(inactive.ToString(), Styles.StatsTableContent, opts);
                        }
                    };

                drawStats(GUIContent.none, Styles.RealtimeLights, (int)m_Stats.enabled.realtimeLightsCount, (int)m_Stats.active.realtimeLightsCount, (int)m_Stats.inactive.realtimeLightsCount);
                drawStats(GUIContent.none, Styles.MixedLights, (int)m_Stats.enabled.mixedLightsCount, (int)m_Stats.active.mixedLightsCount, (int)m_Stats.inactive.mixedLightsCount);
                drawStats(GUIContent.none, Styles.BakedLights, (int)m_Stats.enabled.bakedLightsCount, (int)m_Stats.active.bakedLightsCount, (int)m_Stats.inactive.bakedLightsCount);
                drawStats(GUIContent.none, Styles.DynamicMeshes, (int)m_Stats.enabled.dynamicMeshesCount, (int)m_Stats.active.dynamicMeshesCount, (int)m_Stats.inactive.dynamicMeshesCount);
                drawStats(markStaticMeshes ? Styles.StaticMeshesIconWarning : GUIContent.none, Styles.StaticMeshes, (int)m_Stats.enabled.staticMeshesCount, (int)m_Stats.active.staticMeshesCount, (int)m_Stats.inactive.staticMeshesCount);
                drawStats(GUIContent.none, Styles.RealtimeEmissiveMaterials, (int)m_Stats.enabled.staticMeshesRealtimeEmissive, (int)m_Stats.active.staticMeshesRealtimeEmissive, (int)m_Stats.inactive.staticMeshesRealtimeEmissive);
                drawStats(GUIContent.none, Styles.BakedEmissiveMaterials, (int)m_Stats.enabled.staticMeshesBakedEmissive, (int)m_Stats.active.staticMeshesBakedEmissive, (int)m_Stats.inactive.staticMeshesBakedEmissive);
                drawStats(GUIContent.none, Styles.LightProbeGroups, (int)m_Stats.enabled.lightProbeGroupsCount, (int)m_Stats.active.lightProbeGroupsCount, (int)m_Stats.inactive.lightProbeGroupsCount);
                drawStats(GUIContent.none, Styles.ReflectionProbes, (int)m_Stats.enabled.reflectionProbesCount, (int)m_Stats.active.reflectionProbesCount, (int)m_Stats.inactive.reflectionProbesCount);
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndScrollView();
        }
    }
} // namespace
