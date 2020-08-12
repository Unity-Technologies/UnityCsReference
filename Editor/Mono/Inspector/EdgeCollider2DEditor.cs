// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(EdgeCollider2D))]
    [CanEditMultipleObjects]
    class EdgeCollider2DEditor : Collider2DEditorBase
    {
        SerializedProperty m_EdgeRadius;
        SerializedProperty m_Points;

        public override void OnEnable()
        {
            base.OnEnable();

            m_EdgeRadius = serializedObject.FindProperty("m_EdgeRadius");
            m_Points = serializedObject.FindProperty("m_Points");
            m_Points.isExpanded = false;
        }

        public override void OnInspectorGUI()
        {
            BeginColliderInspector();
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_EdgeRadius);

            if (targets.Length == 1)
            {
                EditorGUI.BeginDisabledGroup(editingCollider);
                EditorGUILayout.PropertyField(m_Points, true);
                EditorGUI.EndDisabledGroup();
            }

            EndColliderInspector();

            FinalizeInspectorGUI();
        }
    }
}
