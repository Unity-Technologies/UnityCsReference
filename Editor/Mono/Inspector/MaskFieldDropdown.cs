// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.MaskDropDownUtils;

namespace UnityEditor
{
    internal class MaskFieldDropDown : PopupWindowContent
    {
        SerializedProperty m_SerializedProperty;
        int m_LayerCount = 32;
        int m_MaxMaskValue = 0;

        SelectionModes[] m_SelectionMatch;
        string[] m_OptionNames;
        int[] m_OptionMaskValues;
        int[] m_SelectionMaskValues;

        public MaskFieldDropDown(SerializedProperty property)
        {
            m_SerializedProperty = property;
        }

        public override Vector2 GetWindowSize()
        {
            var rowCount = 2;
            for (var i = 0; i < m_LayerCount; i++)
            {
                var s = InternalEditorUtility.GetLayerName(i);
                if (s != string.Empty)
                    rowCount++;
            }
            var size = Styles.menuItem.CalcSize(new GUIContent("TransparentFX"));
            return new Vector2(size.x, (EditorGUI.kSingleLineHeight + 2) * rowCount);
        }

        void DrawEverythingOrNothingSelectedToggle(bool state, string label, GUIStyle style, int value)
        {
            var guiRect = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight);
            guiRect.width = GetWindowSize().x;
            guiRect.x = 0;
            DrawListBackground(guiRect, value == 0);
            EditorGUI.BeginChangeCheck();
            GUI.Toggle(guiRect, state, label, style);
            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedProperty.intValue = value;
                m_SerializedProperty.serializedObject.ApplyModifiedProperties();
                m_SerializedProperty.serializedObject.SetIsDifferentCacheDirty();
                m_SerializedProperty.serializedObject.Update();

                var valueToPopulate = value == 0 ? SelectionModes.None : SelectionModes.All;
                m_SelectionMatch = new SelectionModes[m_LayerCount];
                m_SelectionMatch = m_SelectionMatch.Select(el => el = valueToPopulate).ToArray();
            }
        }

        private void DrawListBackground(Rect rect, bool even)
        {
            GUIStyle backgroundStyle = even ? Styles.listEvenBg : Styles.listOddBg;
            GUI.Label(rect, GUIContent.none, backgroundStyle);
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            if (m_SerializedProperty.propertyType != SerializedPropertyType.LayerMask)
                return;

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            var isNothing = m_SerializedProperty.intValue == 0;
            var isEverything = m_SerializedProperty.intValue == -1;

            GUILayout.Space(2);

            var toggleStyle = m_SerializedProperty.hasMultipleDifferentValues && isNothing ? Styles.menuItemMixed : Styles.menuItem;
            DrawEverythingOrNothingSelectedToggle(isNothing, "Nothing", toggleStyle, 0);
            toggleStyle = m_SerializedProperty.hasMultipleDifferentValues && isEverything ? Styles.menuItemMixed : Styles.menuItem;
            DrawEverythingOrNothingSelectedToggle(isEverything, "Everything", toggleStyle, -1);

            for (int i = 0; i < m_OptionNames.Length; i++)
            {
                var index = (int)Math.Log(m_OptionMaskValues[i], 2);

                bool toggleVal = m_SelectionMatch[index] == SelectionModes.All || m_SelectionMatch[index] == SelectionModes.Mixed ? true : false;
                toggleStyle = m_SelectionMatch[index] == SelectionModes.Mixed ? Styles.menuItemMixed : Styles.menuItem;

                EditorGUI.BeginChangeCheck();
                var guiRect = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight);
                guiRect.width = GetWindowSize().x;
                guiRect.x = 0;
                DrawListBackground(guiRect, i % 2 == 0);
                var value = GUI.Toggle(guiRect, toggleVal, m_OptionNames[i], toggleStyle);

                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectionMatch[index] = value ? SelectionModes.All : SelectionModes.None;
                    ChangeMaskValues(index, value);
                }
            }
        }

        void ChangeMaskValues(int maskIndex, bool add)
        {
            var selectionCount = m_SerializedProperty.serializedObject.targetObjects.Length;

            m_SelectionMaskValues = new int[selectionCount];
            for (int i = 0; i < selectionCount; i++)
            {
                var serializedObject = new SerializedObject(m_SerializedProperty.serializedObject.targetObjects[i]);
                var property = serializedObject.FindProperty(m_SerializedProperty.propertyPath);

                if (property.intValue == ~0)
                {
                    property.intValue = 0;
                    for (int j = 0; j < m_OptionMaskValues.Length; j++)
                    {
                        var slotsToShift = (int)Math.Log(m_OptionMaskValues[j], 2);
                        property.intValue |= 1 << slotsToShift;
                    }
                }
                else if ((property.intValue |= 1 << maskIndex) == m_MaxMaskValue && add)
                {
                    property.intValue = -1;
                }
                else if (add)
                    property.intValue = property.intValue |= 1 << maskIndex;
                else
                {
                    property.intValue = property.intValue |= 1 << maskIndex;
                    property.intValue = property.intValue &= ~(1 << maskIndex);
                }

                m_SelectionMaskValues[i] = property.intValue;
                serializedObject.ApplyModifiedProperties();
            }
        }

        public override void OnOpen()
        {
            m_SelectionMatch = new SelectionModes[m_LayerCount];
            GetMultiSelectionValues(m_SerializedProperty, out m_SelectionMaskValues, out m_SelectionMatch, m_LayerCount);
            TagManager.GetDefinedLayers(ref m_OptionNames, ref m_OptionMaskValues);

            for (int i = 0; i < m_OptionMaskValues.Length; i++)
                m_MaxMaskValue += m_OptionMaskValues[i];

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public override void OnClose()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            base.OnClose();
        }

        void OnUndoRedoPerformed()
        {
            editorWindow.Close();
        }
    }

    internal class StaticFieldDropdown : PopupWindowContent
    {
        SerializedProperty m_SerializedProperty;

        SelectionModes[] m_SelectionMatch;
        string[] m_OptionNames;
        int[] m_SelectionMaskValues;
        int m_OptionCount;

        public StaticFieldDropdown(SerializedProperty property)
        {
            m_SerializedProperty = property;
        }

        public override Vector2 GetWindowSize()
        {
            var size = Styles.menuItem.CalcSize(new GUIContent("Off Mesh Link Generation"));
            return new Vector2(size.x, size.y * (m_OptionCount + 2) + 2);
        }

        void DrawEverythingOrNothingSelectedToggle(bool state, string label, GUIStyle style, bool changedTo)
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.Toggle(state, label, style);
            if (EditorGUI.EndChangeCheck())
            {
                ChangeMaskValues(-1, changedTo);
                var valueToPopulate = changedTo ? SelectionModes.All : SelectionModes.None;
                m_SelectionMatch = new SelectionModes[m_OptionCount];
                m_SelectionMatch = m_SelectionMatch.Select(el => el = valueToPopulate).ToArray();
                m_SerializedProperty.serializedObject.SetIsDifferentCacheDirty();
            }
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            if (m_SerializedProperty.propertyType != SerializedPropertyType.Integer)
                return;

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            var isNothing = m_SerializedProperty.intValue == 0;
            var isEverything = m_SerializedProperty.intValue == -1;

            GUILayout.Space(Styles.menuItem.margin.bottom);

            var toggleStyle = m_SerializedProperty.hasMultipleDifferentValues && isNothing ? Styles.menuItemMixed : Styles.menuItem;
            DrawEverythingOrNothingSelectedToggle(isNothing, "Nothing", toggleStyle, false);
            toggleStyle = m_SerializedProperty.hasMultipleDifferentValues && isEverything ? Styles.menuItemMixed : Styles.menuItem;
            DrawEverythingOrNothingSelectedToggle(isEverything, "Everything", toggleStyle, true);

            for (int i = 0; i < m_OptionCount; i++)
            {
                bool toggleVal = m_SelectionMatch[i] == SelectionModes.All || m_SelectionMatch[i] == SelectionModes.Mixed ? true : false;
                toggleStyle = m_SelectionMatch[i] == SelectionModes.Mixed ? Styles.menuItemMixed : Styles.menuItem;

                EditorGUI.BeginChangeCheck();
                int flagIndex = (int)Math.Pow(2, i);
                var value = GUILayout.Toggle(toggleVal, new GUIContent(m_OptionNames[i]), toggleStyle);

                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectionMatch[i] = value ? SelectionModes.All : SelectionModes.None;
                    ChangeMaskValues(flagIndex, value);
                }
            }
        }

        void ChangeMaskValues(int maskIndex, bool add)
        {
            var selectionCount = m_SerializedProperty.serializedObject.targetObjects.Length;
            m_SelectionMaskValues = new int[selectionCount];

            SceneModeUtility.SetStaticFlags(m_SerializedProperty.serializedObject.targetObjects, maskIndex, add);

            for (int i = 0; i < selectionCount; i++)
                m_SelectionMaskValues[i] = m_SerializedProperty.intValue;
        }

        public override void OnOpen()
        {
            m_OptionCount = Enum.GetValues(typeof(StaticEditorFlags)).Length - 1;
            m_OptionNames = new string[m_OptionCount];

            for (int i = 0; i < m_OptionCount; i++)
            {
                int flagIndex = (int)Math.Pow(2, i);
                m_OptionNames[i] = ObjectNames.NicifyVariableName(((StaticEditorFlags)(flagIndex)).ToString());
            }

            GetMultiSelectionValues(m_SerializedProperty, out m_SelectionMaskValues, out m_SelectionMatch, m_OptionCount);
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public override void OnClose()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            base.OnClose();
        }

        void OnUndoRedoPerformed()
        {
            editorWindow.Close();
        }
    }

    internal class MaskDropDownUtils
    {
        internal static class Styles
        {
            internal static GUIStyle menuItem = new GUIStyle("MenuItem");
            internal static GUIStyle menuItemMixed = new GUIStyle("MenuItemMixed");
            internal static GUIStyle listEvenBg = "ObjectPickerResultsOdd";
            internal static GUIStyle listOddBg = "ObjectPickerResultsEven";

            static Styles()
            {
                menuItem.padding = new RectOffset(menuItem.padding.left, menuItem.padding.right, 0, 0);
                menuItemMixed.padding = new RectOffset(menuItemMixed.padding.left, menuItemMixed.padding.right, 0, 0);
            }
        }

        internal enum SelectionModes
        {
            None = 0,
            All = 1,
            Mixed = 2
        };

        internal static void GetSelected(int maskValue, out int[] selected, int size)
        {
            if (maskValue == 0)
                selected = new int[0];
            else if (maskValue == -1)
                selected = Enumerable.Range(1, size).ToArray();
            else
            {
                List<int> selectedMaskToIndex = new List<int>();
                for (int i = 0; i < size; i++)
                {
                    if (((1 << i) & maskValue) > 0)
                    {
                        selectedMaskToIndex.Add(i + 1);
                    }
                }

                selected = selectedMaskToIndex.ToArray();
            }
        }

        internal static void GetSingleSelectionValues(int maskValue, out SelectionModes[] selectionMatch, int layerCount)
        {
            selectionMatch = new SelectionModes[layerCount];
            int[] selected;
            GetSelected(maskValue, out selected, layerCount);

            for (int i = 0; i < selectionMatch.Length; i++)
            {
                if (Array.Exists(selected, el => el == i + 1))
                    selectionMatch[i] = SelectionModes.All;
            }
        }

        internal static void GetMultiSelectionValues(SerializedProperty serializedProperty, out int[] selectionMaskValues, out SelectionModes[] selectionMatch, int layerCount)
        {
            var selectionCount = serializedProperty.serializedObject.targetObjects.Length;
            selectionMaskValues = new int[selectionCount];
            selectionMatch = new SelectionModes[layerCount];

            for (int i = 0; i < selectionCount; i++)
            {
                var serializedObject = new SerializedObject(serializedProperty.serializedObject.targetObjects[i]);
                var property = serializedObject.FindProperty(serializedProperty.propertyPath);
                selectionMaskValues[i] = property.intValue;
            }

            if (selectionCount == 1)
            {
                GetSingleSelectionValues(selectionMaskValues[0], out selectionMatch, layerCount);
                return;
            }

            int[] firstSelected;
            GetSelected(selectionMaskValues[0], out firstSelected, layerCount);

            for (int i = 1; i < selectionCount; i++)
            {
                int[] secondSelected;
                GetSelected(selectionMaskValues[i], out secondSelected, layerCount);

                for (int j = 0; j < layerCount; j++)
                {
                    var firstExists = Array.Exists(firstSelected, element => element == j + 1);
                    var secondExists = Array.Exists(secondSelected, element => element == j + 1);

                    if (firstExists && secondExists && selectionMatch[j] != SelectionModes.Mixed)
                        selectionMatch[j] = SelectionModes.All;
                    else if (!firstExists && !secondExists && selectionMatch[j] != SelectionModes.Mixed)
                        selectionMatch[j] = SelectionModes.None;
                    else
                        selectionMatch[j] = SelectionModes.Mixed;
                }
            }
        }
    }
}
