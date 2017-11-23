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
        private SerializedProperty m_CellSwizzle;

        private void OnEnable()
        {
            m_CellSize = serializedObject.FindProperty("m_CellSize");
            m_CellGap = serializedObject.FindProperty("m_CellGap");
            m_CellSwizzle = serializedObject.FindProperty("m_CellSwizzle");
            SceneViewGridManager.FlushCachedGridProxy();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_CellSize);
            EditorGUILayout.PropertyField(m_CellGap);
            EditorGUILayout.PropertyField(m_CellSwizzle);

            if (serializedObject.ApplyModifiedProperties())
                SceneViewGridManager.FlushCachedGridProxy();
        }
    }
}
