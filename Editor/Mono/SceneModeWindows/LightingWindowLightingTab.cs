// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Globalization;

namespace UnityEditor
{
    internal class LightingWindowLightingTab
    {
        class Styles
        {
            public static readonly GUIContent DebugSettings = EditorGUIUtility.TrTextContent("Debug Settings");
            public static readonly GUIContent LightProbeVisualization = EditorGUIUtility.TrTextContent("Light Probe Visualization");
            public static readonly GUIContent NewLightingSettings = EditorGUIUtility.TrTextContent("New Lighting Settings");

            public static readonly GUIContent DisplayWeights = EditorGUIUtility.TrTextContent("Display Weights");
            public static readonly GUIContent DisplayOcclusion = EditorGUIUtility.TrTextContent("Display Occlusion");
            public static readonly GUIContent HighlightInvalidCells = EditorGUIUtility.TrTextContent("Highlight Invalid Cells", "Highlight the invalid cells that cannot be used for probe interpolation.");
        }

        SavedBool m_ShowDebugSettings;
        SavedBool m_ShowProbeDebugSettings;
        Vector2 m_ScrollPosition = Vector2.zero;

        LightingWindowBakeSettings m_BakeSettings;

        SerializedObject m_LightmapSettings;
        SerializedProperty m_LightingSettingsAsset;

        SerializedObject lightmapSettings
        {
            get
            {
                // if we set a new scene as the active scene, we need to make sure to respond to those changes
                if (m_LightmapSettings == null || m_LightmapSettings.targetObject != LightmapEditorSettings.GetLightmapSettings())
                {
                    m_LightmapSettings = new SerializedObject(LightmapEditorSettings.GetLightmapSettings());
                    m_LightingSettingsAsset = m_LightmapSettings.FindProperty("m_LightingSettings");
                }

                return m_LightmapSettings;
            }
        }

        public void OnEnable()
        {
            m_BakeSettings = new LightingWindowBakeSettings();
            m_BakeSettings.OnEnable();

            m_ShowDebugSettings = new SavedBool("LightingWindow.ShowDebugSettings", false);
            m_ShowProbeDebugSettings = new SavedBool("LightingWindow.ShowProbeDebugSettings", false);
        }

        public void OnDisable()
        {
            m_BakeSettings.OnDisable();
        }

        public void OnGUI()
        {
            EditorGUIUtility.hierarchyMode = true;

            lightmapSettings.Update();

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            EditorGUILayout.PropertyField(m_LightingSettingsAsset);

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Styles.NewLightingSettings, GUILayout.Width(170)))
            {
                Lightmapping.lightingSettingsInternal = new LightingSettings();
                Lightmapping.lightingSettingsInternal.CreateAsset();
            }

            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            m_BakeSettings.OnGUI();
            DebugSettingsGUI();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            lightmapSettings.ApplyModifiedProperties();
        }

        void DebugSettingsGUI()
        {
            m_ShowDebugSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowDebugSettings.value, Styles.DebugSettings, true);

            if (m_ShowDebugSettings.value)
            {
                EditorGUI.indentLevel++;

                m_ShowProbeDebugSettings.value = EditorGUILayout.Foldout(m_ShowProbeDebugSettings.value, Styles.LightProbeVisualization, true);

                if (m_ShowProbeDebugSettings.value)
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUI.indentLevel++;
                    LightProbeVisualization.lightProbeVisualizationMode = (LightProbeVisualization.LightProbeVisualizationMode)EditorGUILayout.EnumPopup(LightProbeVisualization.lightProbeVisualizationMode);
                    LightProbeVisualization.showInterpolationWeights = EditorGUILayout.Toggle(Styles.DisplayWeights, LightProbeVisualization.showInterpolationWeights);
                    LightProbeVisualization.showOcclusions = EditorGUILayout.Toggle(Styles.DisplayOcclusion, LightProbeVisualization.showOcclusions);
                    LightProbeVisualization.highlightInvalidCells = EditorGUILayout.Toggle(Styles.HighlightInvalidCells, LightProbeVisualization.highlightInvalidCells);
                    EditorGUI.indentLevel--;

                    if (EditorGUI.EndChangeCheck())
                        EditorApplication.SetSceneRepaintDirty();
                }
                m_BakeSettings.DeveloperBuildSettingsGUI();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }
        }
    }
} // namespace
