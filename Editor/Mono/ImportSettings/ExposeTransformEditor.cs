// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class ExposeTransformEditor
    {
        private ReorderableList m_ExtraExposedTransformList;

        private string[] m_TransformPaths;
        private SerializedProperty m_ExtraExposedTransformPaths;

        public void OnEnable(string[] transformPaths, SerializedObject serializedObject)
        {
            m_TransformPaths = transformPaths;
            m_ExtraExposedTransformPaths = serializedObject.FindProperty("m_ExtraExposedTransformPaths");

            // Customized exposed transforms
            if (m_ExtraExposedTransformList == null)
            {
                m_ExtraExposedTransformList = new ReorderableList(serializedObject, m_ExtraExposedTransformPaths, false, true, true, true);
                m_ExtraExposedTransformList.onAddDropdownCallback = AddTransformPathInList;
                m_ExtraExposedTransformList.onRemoveCallback = RemoveTransformPathInList;
                m_ExtraExposedTransformList.drawElementCallback = DrawTransformPathElement;
                m_ExtraExposedTransformList.drawHeaderCallback = DrawTransformPathListHeader;
                m_ExtraExposedTransformList.elementHeight = 16;
            }
        }

        public void OnGUI()
        {
            m_ExtraExposedTransformList.DoLayoutList();
        }

        private void TransformPathSelected(object userData, string[] options, int selected)
        {
            string newExposedPath = options[selected];
            for (int i = 0; i < m_ExtraExposedTransformPaths.arraySize; i++)
            {
                string itStr = m_ExtraExposedTransformPaths.GetArrayElementAtIndex(i).stringValue;
                if (itStr == newExposedPath)
                    return;
            }

            m_ExtraExposedTransformPaths.InsertArrayElementAtIndex(m_ExtraExposedTransformPaths.arraySize);
            m_ExtraExposedTransformPaths.GetArrayElementAtIndex(m_ExtraExposedTransformPaths.arraySize - 1).stringValue = options[selected];
        }

        private void AddTransformPathInList(Rect rect, ReorderableList list)
        {
            EditorUtility.DisplayCustomMenu(rect, m_TransformPaths, null, TransformPathSelected, null);
        }

        private void RemoveTransformPathInList(ReorderableList list)
        {
            m_ExtraExposedTransformPaths.DeleteArrayElementAtIndex(list.index);
        }

        private void DrawTransformPathElement(Rect rect, int index, bool selected, bool focused)
        {
            string curPath = m_ExtraExposedTransformPaths.GetArrayElementAtIndex(index).stringValue;
            GUI.Label(rect, curPath.Substring(curPath.LastIndexOf("/") + 1), EditorStyles.label);
        }

        private void DrawTransformPathListHeader(Rect rect)
        {
            GUI.Label(rect, "Extra Transforms to Expose", EditorStyles.label);
        }
    }
}
