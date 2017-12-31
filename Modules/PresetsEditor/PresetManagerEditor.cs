// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Presets
{
    [CustomEditor(typeof(PresetManager))]
    internal sealed class PresetManagerEditor : Editor
    {
        static class Style
        {
            public static GUIContent managerIcon = EditorGUIUtility.IconContent("GameManager Icon");
            public static GUIStyle centerStyle = new GUIStyle() {alignment = TextAnchor.MiddleCenter};
        }

        new PresetManager target { get {return base.target as PresetManager; } }

        Dictionary<string, List<Preset>> m_DiscoveredPresets = new Dictionary<string, List<Preset>>();

        SerializedProperty m_DefaultPresets;
        HashSet<string> m_AddedTypes = new HashSet<string>();

        ReorderableList m_List;
        GenericMenu m_AddingMenu;

        static GUIContent s_DropIcon = null;

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
            m_DefaultPresets = serializedObject.FindProperty("m_DefaultList");
            m_List = new ReorderableList(serializedObject, m_DefaultPresets);
            m_List.draggable = false;
            m_List.drawHeaderCallback = rect => EditorGUI.LabelField(rect, GUIContent.Temp("Default Presets"));
            m_List.drawElementCallback = DrawElementCallback;
            m_List.onAddDropdownCallback = OnAddDropdownCallback;
            m_List.onRemoveCallback = OnRemoveCallback;

            RefreshAddList();

            m_List.onCanAddCallback = (list => m_AddingMenu.GetItemCount() > 0);
        }

        void OnRemoveCallback(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            RefreshAddList();
        }

        void OnFocus()
        {
            RefreshAddList();
        }

        void RefreshAddList()
        {
            m_AddedTypes.Clear();
            for (int i = 0; i < m_DefaultPresets.arraySize; i++)
            {
                m_AddedTypes.Add(target.GetPresetTypeNameAtIndex(i));
            }

            var assets = AssetDatabase.FindAssets("t:Preset")
                .Select(a => AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(a)));

            m_DiscoveredPresets.Clear();
            foreach (var preset in assets)
            {
                string presetclass = preset.GetTargetFullTypeName();
                if (preset.IsValid() && !Preset.IsPresetExcludedFromDefaultPresets(preset))
                {
                    if (!m_DiscoveredPresets.ContainsKey(presetclass))
                        m_DiscoveredPresets.Add(presetclass, new List<Preset>());
                    m_DiscoveredPresets[presetclass].Add(preset);
                }
            }

            m_AddingMenu = new GenericMenu();
            foreach (var discoveredPreset in m_DiscoveredPresets)
            {
                if (!m_AddedTypes.Contains(discoveredPreset.Key))
                {
                    foreach (var preset in discoveredPreset.Value)
                    {
                        m_AddingMenu.AddItem(new GUIContent(discoveredPreset.Key.Replace(".", "/") + "/" + preset.name), false, OnAddingPreset, preset);
                    }
                }
            }
        }

        void OnAddDropdownCallback(Rect buttonRect, ReorderableList list)
        {
            m_AddingMenu.DropDown(buttonRect);
        }

        void OnAddingPreset(object userData)
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(target, "Inspector");
            target.SetAsDefaultInternal((Preset)userData);
            serializedObject.Update();
            RefreshAddList();
        }

        static string FullTypeNameToFriendlyName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return "Unsupported Type";
            int lastDot = fullTypeName.LastIndexOf(".");
            if (lastDot == -1)
                return fullTypeName;
            return string.Format("{0} ({1})", fullTypeName.Substring(lastDot + 1), fullTypeName.Substring(0, lastDot));
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 2f;
            var presetProperty = m_DefaultPresets
                .GetArrayElementAtIndex(index)
                .FindPropertyRelative("defaultPresets.Array.data[0].m_Preset");
            var presetObject = (Preset)presetProperty.objectReferenceValue;
            var keyType = target.GetPresetTypeNameAtIndex(index);
            var guicontent = GUIContent.Temp(FullTypeNameToFriendlyName(keyType));
            using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(keyType)))
            {
                // lets hack a bit the ObjectField because we don't want user to put any Preset in each field of the manager
                var buttonRect = new Rect(rect.xMax - rect.height, rect.y, rect.height, rect.height);
                switch (Event.current.type)
                {
                    case EventType.MouseDown:
                        if (buttonRect.Contains(Event.current.mousePosition))
                        {
                            var menu = new GenericMenu();
                            if (m_DiscoveredPresets.ContainsKey(keyType))
                            {
                                foreach (var preset in m_DiscoveredPresets[keyType])
                                {
                                    menu.AddItem(new GUIContent(preset.name), preset == presetObject, OnAddingPreset, preset);
                                }
                            }
                            else
                            {
                                menu.AddItem(new GUIContent("None"), false, null);
                            }
                            menu.ShowAsContext();
                            Event.current.Use();
                        }
                        break;
                }
                var controlID = GUIUtility.GetControlID(guicontent, FocusType.Passive);
                var controlRect = EditorGUI.PrefixLabel(rect, guicontent);
                var dropRect = controlRect;
                dropRect.xMax -= controlRect.height;
                EditorGUI.DoObjectField(controlRect, dropRect, controlID, presetObject, typeof(Preset), presetProperty, PresetFieldDropValidator, false);

                // todo : having a single icon here could be nice
                buttonRect.x += 7f;
                buttonRect.width = 9f;
                buttonRect.height = 10f;
                buttonRect.y += 11f;
                EditorGUI.LabelField(buttonRect, s_DropIcon);
            }
        }

        Object PresetFieldDropValidator(Object[] references, Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
        {
            if (references.Length == 1)
            {
                var preset = references[0] as Preset;
                string propertyPath = property.propertyPath;
                var numberStart = propertyPath.IndexOf("[") + 1;
                var numberLenght = propertyPath.IndexOf("]") - numberStart;
                var propertyPosition = int.Parse(propertyPath.Substring(numberStart, numberLenght));
                if (preset != null && target.GetPresetTypeNameAtIndex(propertyPosition) == preset.GetTargetFullTypeName())
                {
                    return references[0];
                }
            }
            return null;
        }

        public override void OnInspectorGUI()
        {
            if (Event.current.type == EventType.MouseEnterWindow)
            {
                RefreshAddList();
                return;
            }
            if (s_DropIcon == null)
                s_DropIcon = EditorGUIUtility.IconContent("Icon Dropdown");

            serializedObject.Update();

            m_List.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("Edit/Project Settings/Preset Manager", false, 300)]
        static void ShowManagerInspector()
        {
            Selection.activeObject = Resources.FindObjectsOfTypeAll<PresetManager>().First();
        }
    }
}
