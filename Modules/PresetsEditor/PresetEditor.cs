// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Preset))]
    [CanEditMultipleObjects]
    internal class PresetEditor : Editor
    {
        static class Style
        {
            public static GUIContent presetType = EditorGUIUtility.TextContent("Preset Type|The Object type this Preset can be applied to.");
        }

        bool m_DisplayErrorPreset;
        string m_SelectedPresetTypeName;
        Dictionary<string, List<Object>> m_InspectedTypes = new Dictionary<string, List<Object>>();

        void OnEnable()
        {
            m_InspectedTypes.Clear();
            foreach (var o in targets)
            {
                var preset = (Preset)o;
                string type = preset.GetTargetTypeName();
                if (!m_InspectedTypes.ContainsKey(type))
                {
                    m_InspectedTypes.Add(type, new List<Object>());
                }
                m_InspectedTypes[type].Add(o);
            }
            if (m_InspectedTypes.Count == 1)
            {
                var preset = (Preset)target;
                if (preset.IsValid())
                {
                    m_SelectedPresetTypeName = preset.GetTargetTypeName();
                }
                else
                {
                    m_SelectedPresetTypeName = "Invalid";
                    m_DisplayErrorPreset = true;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (m_DisplayErrorPreset)
            {
                EditorGUILayout.HelpBox("Unable to load this Preset, the type is not supported.", MessageType.Error);
            }
            else
            {
                DrawPresetData();
            }
        }

        void DrawPresetData()
        {
            string presetType = m_InspectedTypes.Count > 1 ? EditorGUI.mixedValueContent.text : m_SelectedPresetTypeName;
            var rect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(true), Style.presetType);
            EditorGUI.SelectableLabel(rect, presetType);
        }

        internal override void OnHeaderTitleGUI(Rect titleRect, string header)
        {
            if (m_InspectedTypes.Count > 1)
                header = string.Format("Multiple Types Presets ({0})", targets.Length);
            else if (targets.Length > 1)
                header = string.Format("{0} Presets ({1})", m_SelectedPresetTypeName, targets.Length);

            base.OnHeaderTitleGUI(titleRect, header);
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }
    }
}
