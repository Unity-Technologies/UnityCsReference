// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Grid))]
    [CanEditMultipleObjects]
    internal class GridEditor : Editor
    {
        private SerializedProperty m_CellSize;
        private SerializedProperty m_CellGap;
        private SerializedProperty m_CellLayout;
        private SerializedProperty m_CellSwizzle;

        private void OnEnable()
        {
            m_CellSize = serializedObject.FindProperty("m_CellSize");
            m_CellGap = serializedObject.FindProperty("m_CellGap");
            m_CellLayout = serializedObject.FindProperty("m_CellLayout");
            m_CellSwizzle = serializedObject.FindProperty("m_CellSwizzle");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_CellSize);
            using (new EditorGUI.DisabledGroupScope(m_CellLayout.enumValueIndex == (int)Grid.CellLayout.Hexagon))
                EditorGUILayout.PropertyField(m_CellGap);
            EditorGUILayout.PropertyField(m_CellLayout);
            EditorGUILayout.PropertyField(m_CellSwizzle);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
