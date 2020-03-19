// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
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

            public static GUIContent addToDefault = EditorGUIUtility.TrTextContent("Add to {0} default", "The Preset will be added first in the default list with an empty filter.");
            public static GUIContent removeFromDefault = EditorGUIUtility.TrTextContent("Remove from {0} default", "All entry using this Preset will be removed from the default list.");

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

        List<int> m_PresetsInstanceIds = new List<int>();
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
            var preset = target as Preset;
            if (preset != null)
            {
                using (new EditorGUI.DisabledScope(targets.Length != 1 || !preset.GetPresetType().IsValidDefault()))
                {
                    var defaultList = Preset.GetDefaultPresetsForType(preset.GetPresetType()).Where(d => d.m_Preset == preset);
                    if (defaultList.Any())
                    {
                        if (GUILayout.Button(GUIContent.Temp(string.Format(Style.removeFromDefault.text, preset.GetTargetTypeName()), Style.removeFromDefault.tooltip), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        {
                            Undo.RecordObject(Resources.FindObjectsOfTypeAll<PresetManager>().First(), "Preset Manager");
                            Preset.RemoveFromDefault(preset);
                            Undo.FlushUndoRecordObjects();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(GUIContent.Temp(string.Format(Style.addToDefault.text, preset.GetTargetTypeName()), Style.addToDefault.tooltip), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        {
                            Undo.RecordObject(Resources.FindObjectsOfTypeAll<PresetManager>().First(), "Preset Manager");
                            var list = Preset.GetDefaultPresetsForType(preset.GetPresetType()).ToList();
                            list.Insert(0, new DefaultPreset()
                            {
                                m_Filter = string.Empty,
                                m_Preset = preset
                            });
                            Preset.SetDefaultPresetsForType(preset.GetPresetType(), list.ToArray());
                            Undo.FlushUndoRecordObjects();
                        }
                    }
                }
            }
        }

        internal override void OnHeaderTitleGUI(Rect titleRect, string header)
        {
            if (m_InspectedTypes.Count > 1)
                header = $"Multiple Types Presets ({targets.Length})";
            else if (targets.Length > 1)
                header = $"{m_SelectedPresetTypeName} Presets ({targets.Length})";

            base.OnHeaderTitleGUI(titleRect, header);
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        void GenerateInternalEditor()
        {
            if (m_InternalEditor == null)
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
                            // fast exit on NULL targets as we do not support their inspector in Preset.
                            m_NotSupportedEditorName = p.GetTargetTypeName();
                            return;
                        }
                        s_References.Add(p.GetInstanceID(), reference);
                        m_PresetsInstanceIds.Add(p.GetInstanceID());
                    }
                    reference.count++;
                    objs[index] = reference.reference;
                }
                m_InternalEditor = CreateEditor(objs);
            }
            else
            {
                //Coming back from an assembly reload... our references are probably broken and we need to fix them.
                for (var index = 0; index < targets.Length; index++)
                {
                    var instanceID = targets[index].GetInstanceID();
                    ReferenceCount reference = null;
                    if (!s_References.TryGetValue(instanceID, out reference))
                    {
                        reference = new ReferenceCount()
                        {
                            count = 0,
                            reference = m_InternalEditor.targets[index]
                        };
                        s_References.Add(instanceID, reference);
                    }
                    reference.count++;
                }
            }

            m_InternalEditor.firstInspectedEditor = true;
        }

        void DestroyInternalEditor()
        {
            if (m_InternalEditor != null)
            {
                // Do not destroy anything if we are just reloading assemblies or re-importing.
                bool shouldDestroyEverything = Unsupported.IsDestroyScriptableObject(this);
                if (shouldDestroyEverything)
                    DestroyImmediate(m_InternalEditor);

                // On Destroy, look for instances id instead of target because they may already be null.
                for (var index = 0; index < m_PresetsInstanceIds.Count; index++)
                {
                    var instanceId = m_PresetsInstanceIds[index];
                    if (--s_References[instanceId].count == 0)
                    {
                        if (shouldDestroyEverything)
                        {
                            if (s_References[instanceId].reference is Component)
                            {
                                var go = ((Component)s_References[instanceId].reference).gameObject;
                                go.hideFlags = HideFlags.None; // make sure we remove the don't destroy flag before calling destroy
                                DestroyImmediate(go);
                            }
                            else
                            {
                                DestroyImmediate(s_References[instanceId].reference);
                            }
                        }
                        s_References.Remove(instanceId);
                    }
                }
            }
        }

        void DrawInternalInspector()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
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
                if (InternalEditorUtility.GetIsInspectorExpanded(m_InternalEditor.target) || m_InternalEditor.HasLargeHeader())
                {
                    GUIStyle editorWrapper = (m_InternalEditor.UseDefaultMargins() && m_InternalEditor.CanBeExpandedViaAFoldoutWithoutUpdate()
                        ? EditorStyles.inspectorDefaultMargins
                        : GUIStyle.none);

                    using (new InspectorWindowUtils.LayoutGroupChecker())
                    {
                        using (new EditorGUILayout.VerticalScope(editorWrapper))
                        {
                            m_InternalEditor.OnInspectorGUI();
                        }
                    }
                }
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
            GUILayout.Space(k_HeaderHeight);
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
