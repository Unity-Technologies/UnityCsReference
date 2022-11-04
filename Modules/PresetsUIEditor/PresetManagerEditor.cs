// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Presets
{
    [CustomEditor(typeof(PresetManager))]
    internal sealed class PresetManagerEditor : Editor
    {
        class Content
        {
            public static GUIContent presetManager = EditorGUIUtility.TrTextContent("Preset management");
        }

        static class Style
        {
            public static GUIContent managerIcon = EditorGUIUtility.IconContent("GameManager Icon");
            public static GUIStyle centerStyle = new GUIStyle() {alignment = TextAnchor.MiddleCenter};

            public static GUIContent addDefault = EditorGUIUtility.TrTextContent("Add Default Preset");
            public static GUIStyle addComponentButtonStyle = "AC Button";
        }

        string m_Search = string.Empty;
        SerializedProperty m_DefaultPresets;
        List<DefaultPresetReorderableList> m_Defaults;
        Vector2 m_ScrollPosition;

        internal override void OnHeaderIconGUI(Rect iconRect)
        {
            GUI.Label(iconRect, Style.managerIcon, Style.centerStyle);
        }

        internal override void OnHeaderTitleGUI(Rect titleRect, string header)
        {
            header = "PresetManager";
            base.OnHeaderTitleGUI(titleRect, header);
        }

        void OnEnable()
        {
            SetupDefaultList();
        }

        void SetupDefaultList()
        {
            m_DefaultPresets = serializedObject.FindProperty("m_DefaultPresets");
            m_Defaults = new List<DefaultPresetReorderableList>(m_DefaultPresets.arraySize);
            for (int i = 0; i < m_DefaultPresets.arraySize; ++i)
            {
                SerializedProperty defaultPreset = m_DefaultPresets.GetArrayElementAtIndex(i);
                var presetType = new PresetType(defaultPreset.FindPropertyRelative("first"));
                var list = new DefaultPresetReorderableList(serializedObject, defaultPreset.FindPropertyRelative("second"), presetType);
                m_Defaults.Add(list);
            }

            m_Defaults.Sort((a, b) => a.className.CompareTo(b.className));
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject.UpdateIfRequiredOrScript() || m_DefaultPresets.arraySize != m_Defaults.Count)
                SetupDefaultList();

            m_Search = EditorGUI.SearchField(EditorGUILayout.GetControlRect(), m_Search);
            EditorGUILayout.Space();

            using (var scope = new EditorGUILayout.VerticalScrollViewScope(m_ScrollPosition))
            {
                for (int i = 0; i < m_DefaultPresets.arraySize; ++i)
                {
                    if (string.IsNullOrEmpty(m_Search) || m_Defaults[i].fullClassName.ToLower().Contains(m_Search.ToLower()))
                        m_Defaults[i].DoLayoutList();
                }

                m_ScrollPosition = scope.scrollPosition;
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                Rect rect = GUILayoutUtility.GetRect(Style.addDefault, Style.addComponentButtonStyle);
                if (EditorGUI.DropdownButton(rect, Style.addDefault, FocusType.Passive, Style.addComponentButtonStyle))
                {
                    if (AddPresetTypeWindow.Show(rect, OnPresetTypeWindowSelection, string.IsNullOrEmpty(m_Search) ? null : m_Search))
                    {
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
            serializedObject.ApplyModifiedProperties();
        }

        void OnPresetTypeWindowSelection(PresetType type)
        {
            Undo.RecordObjects(targets, "Preset Manager");
            foreach (var manager in targets.Cast<PresetManager>())
            {
                manager.AddPresetType(type);
            }
            Undo.FlushUndoRecordObjects();
        }

        [SettingsProvider]
        static SettingsProvider CreatePresetManagerProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Preset Manager", "ProjectSettings/PresetManager.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Content>());
            provider.inspectorUpdateHandler += () =>
            {
                if (provider.settingsEditor != null &&
                    provider.settingsEditor.serializedObject.UpdateIfRequiredOrScript())
                {
                    provider.settingsWindow.Repaint();
                }
            };
            return provider;
        }
    }
}
