// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore.Text;


namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(TextStyle))]
    public class TextStylePropertyDrawer : PropertyDrawer
    {
        public static readonly float height = 95f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty nameProperty = property.FindPropertyRelative("m_Name");
            SerializedProperty hashCodeProperty = property.FindPropertyRelative("m_HashCode");
            SerializedProperty openingDefinitionProperty = property.FindPropertyRelative("m_OpeningDefinition");
            SerializedProperty closingDefinitionProperty = property.FindPropertyRelative("m_ClosingDefinition");
            SerializedProperty openingDefinitionArray = property.FindPropertyRelative("m_OpeningTagArray");
            SerializedProperty closingDefinitionArray = property.FindPropertyRelative("m_ClosingTagArray");


            EditorGUIUtility.labelWidth = 86;
            position.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float labelHeight = position.height + 2f;

            EditorGUI.BeginChangeCheck();
            Rect rect0 = new Rect(position.x, position.y, (position.width) / 2 + 5, position.height);
            EditorGUI.PropertyField(rect0, nameProperty);
            if (EditorGUI.EndChangeCheck())
            {
                // Recompute HashCode if name has changed.
                hashCodeProperty.intValue = TextUtilities.GetHashCodeCaseInSensitive(nameProperty.stringValue);

                property.serializedObject.ApplyModifiedProperties();

                // Dictionary needs to be updated since HashCode has changed.
                TextStyleSheet styleSheet = property.serializedObject.targetObject as TextStyleSheet;
                styleSheet.RefreshStyles();
            }

            // HashCode
            Rect rect1 = new Rect(rect0.x + rect0.width + 5, position.y, 65, position.height);
            GUI.Label(rect1, "HashCode");
            GUI.enabled = false;
            rect1.x += 65;
            rect1.width = position.width / 2 - 75;
            EditorGUI.PropertyField(rect1, hashCodeProperty, GUIContent.none);

            GUI.enabled = true;

            // Text Tags
            EditorGUI.BeginChangeCheck();

            // Opening Tags
            position.y += labelHeight;
            GUI.Label(position, "Opening Tags");
            Rect textRect1 = new Rect(110, position.y, position.width - 86, 35);
            openingDefinitionProperty.stringValue = EditorGUI.TextArea(textRect1, openingDefinitionProperty.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                // If any properties have changed, we need to update the Opening and Closing Arrays.
                int size = openingDefinitionProperty.stringValue.Length;

                // Adjust array size to match new string length.
                if (openingDefinitionArray.arraySize != size) openingDefinitionArray.arraySize = size;

                for (int i = 0; i < size; i++)
                {
                    SerializedProperty element = openingDefinitionArray.GetArrayElementAtIndex(i);
                    element.intValue = openingDefinitionProperty.stringValue[i];
                }
            }

            EditorGUI.BeginChangeCheck();

            // Closing Tags
            position.y += 38;
            GUI.Label(position, "Closing Tags");
            Rect textRect2 = new Rect(110, position.y, position.width - 86, 35);
            closingDefinitionProperty.stringValue = EditorGUI.TextArea(textRect2, closingDefinitionProperty.stringValue);

            if (EditorGUI.EndChangeCheck())
            {
                // If any properties have changed, we need to update the Opening and Closing Arrays.
                int size = closingDefinitionProperty.stringValue.Length;

                // Adjust array size to match new string length.
                if (closingDefinitionArray.arraySize != size) closingDefinitionArray.arraySize = size;

                for (int i = 0; i < size; i++)
                {
                    SerializedProperty element = closingDefinitionArray.GetArrayElementAtIndex(i);
                    element.intValue = closingDefinitionProperty.stringValue[i];
                }
            }
        }
    }


    [CustomEditor(typeof(TextStyleSheet)), CanEditMultipleObjects]
    public class TextStyleSheetEditor : Editor
    {
        TextStyleSheet m_StyleSheet;
        SerializedProperty m_StyleListProp;

        int m_SelectedElement = -1;
        int m_Page;

        bool m_IsStyleSheetDirty;


        void OnEnable()
        {
            m_StyleSheet = target as TextStyleSheet;
            m_StyleListProp = serializedObject.FindProperty("m_StyleList");
        }

        public override void OnInspectorGUI()
        {
            Event currentEvent = Event.current;

            serializedObject.Update();

            m_IsStyleSheetDirty = false;
            int arraySize = m_StyleListProp.arraySize;
            int itemsPerPage = (Screen.height - 100) / 110;

            if (arraySize > 0)
            {
                // Display each Style entry using the StyleDrawer PropertyDrawer.
                for (int i = itemsPerPage * m_Page; i < arraySize && i < itemsPerPage * (m_Page + 1); i++)
                {
                    // Define the start of the selection region of the element.
                    Rect elementStartRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    SerializedProperty styleProperty = m_StyleListProp.GetArrayElementAtIndex(i);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(styleProperty);
                    EditorGUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                        //
                    }

                    // Define the end of the selection region of the element.
                    Rect elementEndRegion = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));

                    // Check for Item selection
                    Rect selectionArea = new Rect(elementStartRegion.x, elementStartRegion.y, elementEndRegion.width, elementEndRegion.y - elementStartRegion.y);
                    if (DoSelectionCheck(selectionArea))
                    {
                        if (m_SelectedElement == i)
                        {
                            m_SelectedElement = -1;
                        }
                        else
                        {
                            m_SelectedElement = i;
                            GUIUtility.keyboardControl = 0;
                        }
                    }

                    // Handle Selection Highlighting
                    if (m_SelectedElement == i)
                        TextCoreEditorUtilities.DrawBox(selectionArea, 2f, new Color32(40, 192, 255, 255));
                }
            }

            // STYLE LIST MANAGEMENT
            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            float totalWidth = rect.width;
            rect.width = totalWidth * 0.175f;

            // Move Style up.
            bool guiEnabled = GUI.enabled;
            if (m_SelectedElement == -1 || m_SelectedElement == 0) { GUI.enabled = false; }
            if (GUI.Button(rect, "Up"))
            {
                SwapStyleElements(m_SelectedElement, m_SelectedElement - 1);
            }
            GUI.enabled = guiEnabled;

            // Move Style down.
            rect.x += rect.width;
            if (m_SelectedElement == arraySize - 1) { GUI.enabled = false; }
            if (GUI.Button(rect, "Down"))
            {
                SwapStyleElements(m_SelectedElement, m_SelectedElement + 1);
            }
            GUI.enabled = guiEnabled;

            // Add Style
            rect.x += rect.width + totalWidth * 0.3f;
            if (GUI.Button(rect, "+"))
            {
                int index = m_SelectedElement == -1 ? 0 : m_SelectedElement;

                if (index > arraySize)
                    index = arraySize;

                // Copy selected element
                m_StyleListProp.InsertArrayElementAtIndex(index);

                // Select newly inserted element
                m_SelectedElement = index + 1;

                serializedObject.ApplyModifiedProperties();
                m_StyleSheet.RefreshStyles();
            }

            // Delete style
            rect.x += rect.width;
            if (m_SelectedElement == -1 || m_SelectedElement >= arraySize) GUI.enabled = false;
            if (GUI.Button(rect, "-"))
            {
                int index = m_SelectedElement == -1 ? 0 : m_SelectedElement;

                m_StyleListProp.DeleteArrayElementAtIndex(index);

                m_SelectedElement = -1;
                serializedObject.ApplyModifiedProperties();
                m_StyleSheet.RefreshStyles();
                return;
            }

            // Return if we can't display any items.
            if (itemsPerPage == 0) return;

            // DISPLAY PAGE CONTROLS
            int shiftMultiplier = currentEvent.shift ? 10 : 1; // Page + Shift goes 10 page forward

            Rect pagePos = EditorGUILayout.GetControlRect(false, 20);
            pagePos.width = totalWidth * 0.35f;

            // Previous Page
            if (m_Page > 0) GUI.enabled = true;
            else GUI.enabled = false;

            if (GUI.Button(pagePos, "Previous"))
                m_Page -= 1 * shiftMultiplier;

            // PAGE COUNTER
            GUI.enabled = true;
            pagePos.x += pagePos.width;
            pagePos.width = totalWidth * 0.30f;
            int totalPages = (int)(arraySize / (float)itemsPerPage + 0.999f);
            GUI.Label(pagePos, "Page " + (m_Page + 1) + " / " + totalPages, TM_EditorStyles.centeredLabel);

            // Next Page
            pagePos.x += pagePos.width;
            pagePos.width = totalWidth * 0.35f;
            if (itemsPerPage * (m_Page + 1) < arraySize) GUI.enabled = true;
            else GUI.enabled = false;

            if (GUI.Button(pagePos, "Next"))
                m_Page += 1 * shiftMultiplier;

            // Clamp page range
            m_Page = Mathf.Clamp(m_Page, 0, arraySize / itemsPerPage);


            if (serializedObject.ApplyModifiedProperties())
            {
                TextEventManager.ON_TEXT_STYLE_PROPERTY_CHANGED(true);

                if (m_IsStyleSheetDirty)
                {
                    m_IsStyleSheetDirty = false;
                    m_StyleSheet.RefreshStyles();
                }
            }

            // Clear selection if mouse event was not consumed.
            GUI.enabled = true;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                m_SelectedElement = -1;
        }

        // Check if any of the Style elements were clicked on.
        static bool DoSelectionCheck(Rect selectionArea)
        {
            Event currentEvent = Event.current;

            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (selectionArea.Contains(currentEvent.mousePosition) && currentEvent.button == 0)
                    {
                        currentEvent.Use();
                        return true;
                    }
                    break;
            }

            return false;
        }

        void SwapStyleElements(int selectedIndex, int newIndex)
        {
            m_StyleListProp.MoveArrayElement(selectedIndex, newIndex);
            m_SelectedElement = newIndex;
            m_IsStyleSheetDirty = true;
        }
    }
}
