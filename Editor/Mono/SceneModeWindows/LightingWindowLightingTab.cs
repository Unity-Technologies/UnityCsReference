// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Globalization;

namespace UnityEditor
{
    internal class LightingWindowLightingTab : LightingWindow.WindowTab
    {
        class Styles
        {
            public static readonly GUIContent newLightingSettings = EditorGUIUtility.TrTextContent("New", "Create a new Lighting Settings Asset with default settings.");
            public static readonly GUIContent cloneLightingSettings = EditorGUIUtility.TrTextContent("Clone", "Create a new Lighting Settings Asset based on the current settings.");

            public static readonly GUIContent lightingSettings = EditorGUIUtility.TrTextContent("Lighting Settings");
        }

        SavedBool m_ShowLightingSettings;
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

            m_ShowLightingSettings = new SavedBool("LightingWindow.ShowLightingSettings", true);
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

            LightingSettingsGUI();

            m_BakeSettings.OnGUI();

            EditorGUILayout.EndScrollView();

            lightmapSettings.ApplyModifiedProperties();
        }

        public void OnSummaryGUI()
        {
            LightingWindow.Summary();
        }

        public void OnSelectionChange()
        {
        }

        private void CreateLightingSettings(LightingSettings from = null)
        {
            LightingSettings ls;
            if (from == null)
            {
                ls = new LightingSettings();
                ls.name = "New Lighting Settings";
            }
            else
            {
                ls = Object.Instantiate(from);
                ls.name = from.name;
            }
            Undo.RecordObject(m_LightmapSettings.targetObject, "New Lighting Settings");
            Lightmapping.lightingSettingsInternal = ls;
            ProjectWindowUtil.CreateAsset(ls, (ls.name + ".lighting"));
        }

        void LightingSettingsGUI()
        {
            m_ShowLightingSettings.value = EditorGUILayout.FoldoutTitlebar(m_ShowLightingSettings.value, Styles.lightingSettings, true);

            if (m_ShowLightingSettings.value)
            {
                ++EditorGUI.indentLevel;

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(m_LightingSettingsAsset, GUIContent.Temp("Lighting Settings Asset"));

                if (GUILayout.Button(Styles.newLightingSettings, EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                    CreateLightingSettings();
                else if (GUILayout.Button(Styles.cloneLightingSettings, EditorStyles.miniButtonRight, GUILayout.Width(50)))
                    CreateLightingSettings(Lightmapping.GetLightingSettingsOrDefaultsFallback());

                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                --EditorGUI.indentLevel;
            }
        }
    }
} // namespace
