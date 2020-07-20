// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CustomEditor(typeof(EdgeCollider2D))]
    [CanEditMultipleObjects]
    class EdgeCollider2DEditor : Collider2DEditorBase
    {
        SerializedProperty m_EdgeRadius;
        SerializedProperty m_Points;
        SerializedProperty m_UseAdjacentStartPoint;
        SerializedProperty m_UseAdjacentEndPoint;
        SerializedProperty m_AdjacentStartPoint;
        SerializedProperty m_AdjacentEndPoint;

        AnimBool m_ShowAdjacentStartPoint;
        AnimBool m_ShowAdjacentEndPoint;

        public override void OnEnable()
        {
            base.OnEnable();

            m_EdgeRadius = serializedObject.FindProperty("m_EdgeRadius");
            m_Points = serializedObject.FindProperty("m_Points");
            m_Points.isExpanded = false;
            m_UseAdjacentStartPoint = serializedObject.FindProperty("m_UseAdjacentStartPoint");
            m_UseAdjacentEndPoint = serializedObject.FindProperty("m_UseAdjacentEndPoint");
            m_AdjacentStartPoint = serializedObject.FindProperty("m_AdjacentStartPoint");
            m_AdjacentEndPoint = serializedObject.FindProperty("m_AdjacentEndPoint");

            m_ShowAdjacentStartPoint = new AnimBool(m_UseAdjacentStartPoint.boolValue);
            m_ShowAdjacentStartPoint.valueChanged.AddListener(Repaint);
            m_ShowAdjacentEndPoint = new AnimBool(m_UseAdjacentEndPoint.boolValue);
            m_ShowAdjacentEndPoint.valueChanged.AddListener(Repaint);
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

            // Adjacent start point.
            m_ShowAdjacentStartPoint.target = m_UseAdjacentStartPoint.boolValue;
            EditorGUILayout.PropertyField(m_UseAdjacentStartPoint);
            if (EditorGUILayout.BeginFadeGroup(m_ShowAdjacentStartPoint.faded))
            {
                EditorGUILayout.PropertyField(m_AdjacentStartPoint);
            }
            EditorGUILayout.EndFadeGroup();

            // Adjacent end point.
            m_ShowAdjacentEndPoint.target = m_UseAdjacentEndPoint.boolValue;
            EditorGUILayout.PropertyField(m_UseAdjacentEndPoint);
            if (EditorGUILayout.BeginFadeGroup(m_ShowAdjacentEndPoint.faded))
            {
                EditorGUILayout.PropertyField(m_AdjacentEndPoint);
            }
            EditorGUILayout.EndFadeGroup();

            EndColliderInspector();

            FinalizeInspectorGUI();
        }
    }
}
