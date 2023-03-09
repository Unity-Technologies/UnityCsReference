// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.MaskDropDownUtils;
using System.Reflection;

namespace UnityEditor
{
    internal class MaskFieldDropDown : PopupWindowContent
    {
        internal const int m_LayerCount = 32;

        SerializedProperty m_SerializedProperty;

        SelectionModes[] m_SelectionMatch;
        string[] m_OptionNames;
        int[] m_flagValues;
        int[] m_OptionMaskValues;
        int[] m_SelectionMaskValues;

         int m_AllLayersMask = 0;

        bool m_SingleSelection = false;
        EditorUtility.SelectMenuItemFunction m_MaskChangeCallback;

        float m_windowSize = 100.0f;
        public MaskFieldDropDown(SerializedProperty property)
        {
            m_SerializedProperty = property;
            m_SingleSelection = false;
        }

        public MaskFieldDropDown(string[] optionNames, int[] optionMaskValues, int mask, EditorUtility.SelectMenuItemFunction maskChangeCallback)
            : this(optionNames, null, optionMaskValues, mask, maskChangeCallback)
        {
        }

        public MaskFieldDropDown(string[] optionNames, int[] flagValues, int[] optionMaskValues, int mask, EditorUtility.SelectMenuItemFunction maskChangeCallback)
        {
            // these are not flag values, i.e. 1, 2, 4...
            // but the mask & flagValue[0..n] for each possible flag value
            // this is to ensure backwards compatibility with everything that uses MaskFieldGUI.GetSelectedValueForControl
            m_OptionMaskValues = (int[])optionMaskValues.Clone();
            m_OptionNames = (string[])optionNames.Clone();

            m_flagValues = new int[optionNames.Length];
            m_flagValues[0] = 0;
            m_flagValues[1] = -1;

            if (flagValues == null)
            {
                for (int i = 2; i < m_flagValues.Length; ++i)
                    m_flagValues[i] = (1 << i);
            }
            else
            {
                int index = 0;
                int length = flagValues.Length;

                if (flagValues[0] == 0)
                {
                    index = 1;
                    length--;
                }

                if (flagValues[flagValues.Length - 1] == ~0)
                    length--;

                Array.Copy(flagValues, index, m_flagValues, 2, length);
            }

            m_SelectionMatch = new SelectionModes[] { SelectionModes.All };
            m_SelectionMaskValues = new int[] { mask };

            m_SingleSelection = true;
            m_MaskChangeCallback = maskChangeCallback;

            m_AllLayersMask = 0;
            foreach (var val in optionMaskValues)
                if (val != -1 && val != int.MaxValue)
                    m_AllLayersMask |= val;
        }

        public override Vector2 GetWindowSize()
        {
            var rowCount = m_OptionNames[0] == "Nothing" ? m_OptionNames.Length : m_OptionNames.Length + 2;
            return new Vector2(m_windowSize, (EditorGUI.kSingleLineHeight + 2) * rowCount);
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

        void DrawGUIForArrays()
        {
            for (int i = 0; i < m_OptionNames.Length; i++)
            {
                bool toggleVal = (m_SelectionMaskValues[0] & m_OptionMaskValues[i]) == m_OptionMaskValues[i];
                if (m_SelectionMaskValues[0] != 0 && i == 0)
                    toggleVal = false;
                if ((m_SelectionMaskValues[0] == int.MaxValue || m_SelectionMaskValues[0] == m_AllLayersMask) && i == 1)
                    toggleVal = true;

                var guiRect = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight);
                guiRect.width = GetWindowSize().x;
                guiRect.x = 0;
                DrawListBackground(guiRect, i % 2 == 0);

                EditorGUI.BeginChangeCheck();
                bool value = GUI.Toggle(guiRect, toggleVal, m_OptionNames[i], Styles.menuItem);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectionMaskValues[0] = m_OptionMaskValues[i];
                    var oldMaskValues = (uint[])m_OptionMaskValues.Clone();
                    MaskFieldGUI.CalculateMaskValues(m_SelectionMaskValues[0], m_flagValues, ref m_OptionMaskValues);

                    // If all flag options are selected the mask becomes everythingValue to be consistent with the "Everything" option
                    if (oldMaskValues[i] == (uint)m_AllLayersMask)
                        oldMaskValues[i] = ~0u;

                    m_MaskChangeCallback.Invoke(oldMaskValues, null, i);
                }
            }
        }

        public override void OnGUI(Rect rect)
        {
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            if (m_SingleSelection)
            {
                DrawGUIForArrays();
                return;
            }

            if (m_SerializedProperty.propertyType != SerializedPropertyType.LayerMask)
                return;

            var isNothing = m_SerializedProperty.intValue == 0;
            var isEverything = m_SerializedProperty.intValue == int.MaxValue;

            GUILayout.Space(2);

            var toggleStyle = m_SerializedProperty.hasMultipleDifferentValues && isNothing ? Styles.menuItemMixed : Styles.menuItem;
            DrawEverythingOrNothingSelectedToggle(isNothing, "Nothing", toggleStyle, 0);
            toggleStyle = m_SerializedProperty.hasMultipleDifferentValues && isEverything ? Styles.menuItemMixed : Styles.menuItem;
            DrawEverythingOrNothingSelectedToggle(isEverything, "Everything", toggleStyle, int.MaxValue);

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

                // Second condition is for backward compatibility, currently we use int.MaxValue to represent all flags set, previously we used -1 and the same value is stored in YAML files
                // So when we read the SerializedProperty we are getting -1, and the behaviour is not as expected.
                if (property.intValue == int.MaxValue || property.intValue == -1)
                {
                    property.intValue = 0;
                    for (int j = 0; j < m_OptionMaskValues.Length; j++)
                    {
                        var slotsToShift = (int)Math.Log(m_OptionMaskValues[j], 2);
                        property.intValue |= 1 << slotsToShift;
                    }
                }
                if (add)
                    property.intValue = property.intValue |= 1 << maskIndex;
                else
                {
                    property.intValue = property.intValue |= 1 << maskIndex;
                    property.intValue = property.intValue &= ~(1 << maskIndex);
                }

                if (property.intValue == m_AllLayersMask)
                    property.intValue = int.MaxValue;

                m_SelectionMaskValues[i] = property.intValue;
                serializedObject.ApplyModifiedProperties();
            }

            m_SerializedProperty.serializedObject.SetIsDifferentCacheDirty();
            m_SerializedProperty.serializedObject.Update();
        }

        public override void OnOpen()
        {
            if (!m_SingleSelection)
            {
                m_SelectionMatch = new SelectionModes[m_LayerCount];
                GetMultiSelectionValues(m_SerializedProperty, out m_SelectionMaskValues, out m_SelectionMatch, m_LayerCount);
                m_OptionMaskValues = new int[m_SelectionMaskValues.Length];
                TagManager.GetDefinedLayers(ref m_OptionNames, ref m_OptionMaskValues);
                for (int i = 0; i < m_OptionMaskValues.Length; i++)
                    m_AllLayersMask |= m_OptionMaskValues[i];
            }

            for (int i = 0; i < m_OptionNames.Length; i++)
            {
                var size = Styles.menuItem.CalcSize(new GUIContent(m_OptionNames[i]));
                if (size.x > m_windowSize)
                    m_windowSize = size.x;
            }
            m_windowSize = Mathf.Clamp(m_windowSize, 100, Screen.currentResolution.width * 0.95f);

            Undo.undoRedoEvent += OnUndoRedoPerformed;
        }

        public override void OnClose()
        {
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
            Event.current?.Use();
            MaskFieldGUI.DestroyMaskCallBackInfo();
            base.OnClose();
        }

        void OnUndoRedoPerformed(in UndoRedoInfo info)
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
        int m_AllLayersMask;
        List<int> m_FunctioningOptions = new List<int>();

        public StaticFieldDropdown(UnityEngine.Object[] targetObjects, string propertyPath)
        {
            var so = new SerializedObject(targetObjects);
            m_SerializedProperty = so.FindProperty(propertyPath);
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
                ChangeMaskValues(int.MaxValue, changedTo);
                var valueToPopulate = changedTo ? SelectionModes.All : SelectionModes.None;
                m_SelectionMatch = new SelectionModes[m_OptionCount];
                m_SelectionMatch = m_SelectionMatch.Select(el => el = valueToPopulate).ToArray();
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
            var isEverything = ((uint)m_SerializedProperty.intValue & m_AllLayersMask) == m_AllLayersMask;

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
                bool isDeprecated = !m_FunctioningOptions.Contains(flagIndex);

                using (new EditorGUI.DisabledScope(isDeprecated))
                {
                    var value = GUILayout.Toggle(toggleVal, new GUIContent(m_OptionNames[i], (isDeprecated)?"This static value has been deprecated.":""), toggleStyle);

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_SelectionMatch[i] = value ? SelectionModes.All : SelectionModes.None;
                        ChangeMaskValues(flagIndex, value);
                    }
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

            m_SerializedProperty.serializedObject.ApplyModifiedProperties();
            m_SerializedProperty.serializedObject.SetIsDifferentCacheDirty();
            m_SerializedProperty.serializedObject.Update();
            editorWindow.Repaint();
        }

        public override void OnOpen()
        {
            m_FunctioningOptions.Clear();
            m_OptionCount = 0;
            List<FieldInfo> filteredFields = new List<FieldInfo>();
            var fields = typeof(StaticEditorFlags).GetFields();
            foreach (var field in fields)
            {
                if (!field.IsDefined(typeof(ObsoleteAttribute), true) && !field.IsSpecialName)
                {
                    filteredFields.Add(field);
                    m_FunctioningOptions.Add((int)(field.GetValue(null)));
                    m_OptionCount++;
                }
            }

            // Add Obsolete values that were not replaced by a new value
            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(ObsoleteAttribute), true) && !field.IsSpecialName)
                {
                    if (!m_FunctioningOptions.Contains((int)(field.GetValue(null))))
                    {
                        filteredFields.Add(field);
                        m_OptionCount++;
                    }
                }
            }

            m_OptionNames = new string[m_OptionCount];
            for (int i = 0; i < m_OptionCount; i++)
            {
                var val = (int)(filteredFields[i].GetValue(null));
                var index = (int)Math.Log(val, 2);

                m_OptionNames[index] = ObjectNames.NicifyVariableName(filteredFields[i].Name);
                m_AllLayersMask |= val;
            }

            GetMultiSelectionValues(m_SerializedProperty, out m_SelectionMaskValues, out m_SelectionMatch, m_OptionCount);
            Undo.undoRedoEvent += OnUndoRedoPerformed;
        }

        public override void OnClose()
        {
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
            base.OnClose();
        }

        void OnUndoRedoPerformed(in UndoRedoInfo info)
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

        internal static void GetSelected(int maskValue, out uint[] selected, int size)
        {
            if (maskValue == 0)
                selected = new uint[0];
            else if (maskValue == int.MaxValue)
                selected = Enumerable.Range(1, size).Select(i => (uint)i).ToArray();
            else
            {
                List<uint> selectedMaskToIndex = new List<uint>();
                for (int i = 0; i < size; i++)
                {
                    if (((1 << i) & (uint)maskValue) > 0)
                    {
                        selectedMaskToIndex.Add((uint)(i + 1));
                    }
                }

                selected = selectedMaskToIndex.ToArray();
            }
        }

        internal static void GetSingleSelectionValues(int maskValue, out SelectionModes[] selectionMatch, int layerCount)
        {
            selectionMatch = new SelectionModes[layerCount];
            uint[] selected;
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

            uint[] firstSelected;
            GetSelected(selectionMaskValues[0], out firstSelected, layerCount);

            for (int i = 1; i < selectionCount; i++)
            {
                uint[] secondSelected;
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
