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
            public static readonly GUIContent OtherSettings = EditorGUIUtility.TrTextContent("Other Settings");
            public static readonly GUIContent DebugSettings = EditorGUIUtility.TrTextContent("Debug Settings");
            public static readonly GUIContent LightProbeVisualization = EditorGUIUtility.TrTextContent("Light Probe Visualization");
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
    }
} // namespace
