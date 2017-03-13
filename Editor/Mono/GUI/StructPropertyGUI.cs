// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class StructPropertyGUILayout
    {
        internal static void GenericStruct(SerializedProperty property, params GUILayoutOption[] options)
        {
            float height = EditorGUI.kStructHeaderLineHeight + EditorGUI.kSingleLineHeight * GetChildrenCount(property);
            Rect rect = GUILayoutUtility.GetRect(EditorGUILayout.kLabelFloatMinW, EditorGUILayout.kLabelFloatMaxW,
                    height, height, EditorStyles.layerMaskField, options);

            StructPropertyGUI.GenericStruct(rect, property);
        }

        internal static int GetChildrenCount(SerializedProperty property)
        {
            int count = 0;
            SerializedProperty iterator = property.Copy();
            var end = iterator.GetEndProperty();
            while (!SerializedProperty.EqualContents(iterator, end))
            {
                count++;
                iterator.NextVisible(true);
            }

            return count;
        }
    }

    internal class StructPropertyGUI
    {
        internal static void GenericStruct(Rect position, SerializedProperty property)
        {
            GUI.Label(EditorGUI.IndentedRect(position), property.displayName, EditorStyles.label);
            position.y += EditorGUI.kStructHeaderLineHeight;

            DoChildren(position, property);
        }

        private static void DoChildren(Rect position, SerializedProperty property)
        {
            position.height = EditorGUI.kSingleLineHeight;

            EditorGUI.indentLevel++;

            SerializedProperty iterator = property.Copy();
            var end = iterator.GetEndProperty();
            iterator.NextVisible(true);
            while (!SerializedProperty.EqualContents(iterator, end))
            {
                EditorGUI.PropertyField(position, iterator);
                position.y += EditorGUI.kSingleLineHeight;
                if (!iterator.NextVisible(false))
                    break;
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
        }
    }
}
