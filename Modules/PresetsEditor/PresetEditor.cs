// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Presets
{
    [CustomEditor(typeof(Preset))]
    [CanEditMultipleObjects]
    internal class PresetEditor : Editor
    {
        static class Style
        {
            public static GUIContent presetType = EditorGUIUtility.TrTextContent("Preset Type", "The Object type this Preset can be applied to.");
            public static GUIStyle inspectorBig = new GUIStyle(EditorStyles.inspectorBig);
            public static GUIStyle centerStyle = new GUIStyle() { alignment = TextAnchor.MiddleCenter };

            static Style()
            {
                inspectorBig.padding.bottom -= 1;
            }
        }

        class ReferenceCount
        {
            public int count;
            public Object reference;
        }

        static Dictionary<int, ReferenceCount> s_References = new Dictionary<int, ReferenceCount>();

        bool m_DisplayErrorPreset;
        string m_SelectedPresetTypeName;
        Dictionary<string, List<Object>> m_InspectedTypes = new Dictionary<string, List<Object>>();

        Editor m_InternalEditor = null;
        string m_NotSupportedEditorName = null;

        void OnEnable()
        {
            m_InspectedTypes.Clear();
            foreach (var o in targets)
            {
                var preset = (Preset)o;
                string type = preset.GetTargetFullTypeName();
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
                    m_SelectedPresetTypeName = preset.GetTargetFullTypeName();
                    GenerateInternalEditor();
                }
                else
                {
                    m_SelectedPresetTypeName = "Invalid";
                    m_DisplayErrorPreset = true;
                }
            }
        }

        void OnDisable()
        {
            DestroyInternalEditor();
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

            if (m_InternalEditor != null)
            {
                DrawInternalInspector();
            }
            else if (!string.IsNullOrEmpty(m_NotSupportedEditorName))
            {
                DrawUnsupportedInspector();
            }
        }

        internal override void OnHeaderControlsGUI()
        {
            using (new EditorGUI.DisabledScope(targets.Length != 1 || Preset.IsPresetExcludedFromDefaultPresets(target as Preset)))
            {
                var preset = (Preset)target;
                if (Preset.GetDefaultForPreset(preset) == preset)
                {
                    if (GUILayout.Button(string.Format("Remove from {0} Default", preset.GetTargetTypeName()), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        Preset.RemoveFromDefault(preset);
                    }
                }
                else
                {
                    if (GUILayout.Button(string.Format("Set as {0} Default", preset.GetTargetTypeName()), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        Preset.SetAsDefault(preset);
                    }
                }
            }
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

        void GenerateInternalEditor()
        {
            Object[] objs = new Object[targets.Length];
            for (var index = 0; index < targets.Length; index++)
            {
                var p = (Preset)targets[index];
                ReferenceCount reference = null;
                if (!s_References.TryGetValue(p.GetInstanceID(), out reference))
                {
                    reference = new ReferenceCount()
                    {
                        count = 0,
                        reference = p.GetReferenceObject()
                    };
                    if (reference.reference == null)
                    {
                        // fast exit on NULL targets as we do not support their inspector in Preset yet.
                        m_NotSupportedEditorName = p.GetTargetTypeName();
                        return;
                    }
                    s_References.Add(p.GetInstanceID(), reference);
                }
                reference.count++;
                objs[index] = reference.reference;
            }
            m_InternalEditor = CreateEditor(objs);
        }

        void DestroyInternalEditor()
        {
            if (m_InternalEditor != null)
            {
                DestroyImmediate(m_InternalEditor);
                for (var index = 0; index < targets.Length; index++)
                {
                    var p = (Preset)targets[index];
                    if (--s_References[p.GetInstanceID()].count == 0)
                    {
                        if (s_References[p.GetInstanceID()].reference is Component)
                        {
                            var go = ((Component)s_References[p.GetInstanceID()].reference).gameObject;
                            DestroyImmediate(go);
                        }
                        else
                        {
                            DestroyImmediate(s_References[p.GetInstanceID()].reference);
                        }
                        s_References.Remove(p.GetInstanceID());
                    }
                }
            }
        }

        void DrawInternalInspector()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_InternalEditor.target.hideFlags = HideFlags.None;
                if (m_InternalEditor.target is Component)
                {
                    bool wasVisible = InternalEditorUtility.GetIsInspectorExpanded(m_InternalEditor.target);
                    bool isVisible = EditorGUILayout.InspectorTitlebar(wasVisible, m_InternalEditor.targets, m_InternalEditor.CanBeExpandedViaAFoldout());
                    if (isVisible != wasVisible)
                    {
                        InternalEditorUtility.SetIsInspectorExpanded(m_InternalEditor.target, isVisible);
                    }
                }
                else
                {
                    m_InternalEditor.DrawHeader();
                }
                if (InternalEditorUtility.GetIsInspectorExpanded(m_InternalEditor.target))
                {
                    EditorGUI.indentLevel++;
                    m_InternalEditor.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }
                m_InternalEditor.target.hideFlags = HideFlags.HideAndDontSave;
                if (change.changed || m_InternalEditor.isInspectorDirty)
                {
                    for (int i = 0; i < m_InternalEditor.targets.Length; i++)
                    {
                        ((Preset)targets[i]).UpdateProperties(m_InternalEditor.targets[i]);
                    }
                }
            }
        }

        void DrawUnsupportedInspector()
        {
            GUILayout.BeginHorizontal(Style.inspectorBig);
            GUILayout.Space(38);
            GUILayout.BeginVertical();
            GUILayout.Space(19);
            GUILayout.BeginHorizontal();
            EditorGUILayout.GetControlRect();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            Rect r = GUILayoutUtility.GetLastRect();

            // Icon
            Rect iconRect = new Rect(r.x + 6, r.y + 6, 32, 32);
            GUI.Label(iconRect, AssetPreview.GetMiniTypeThumbnail(typeof(Object)), Style.centerStyle);

            // Title
            Rect titleRect = new Rect(r.x + 44, r.y + 6, r.width - 86, 16);
            GUI.Label(titleRect, m_NotSupportedEditorName, EditorStyles.largeLabel);

            EditorGUILayout.HelpBox("Preset Inspectors are not supported for this type.", MessageType.Info);
        }
    }
}
