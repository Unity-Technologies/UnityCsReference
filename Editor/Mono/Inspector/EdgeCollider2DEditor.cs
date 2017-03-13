// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor
{
    [CustomEditor(typeof(EdgeCollider2D))]
    [CanEditMultipleObjects]
    internal class EdgeCollider2DEditor : Collider2DEditorBase
    {
        private PolygonEditorUtility m_PolyUtility = new PolygonEditorUtility();

        private SerializedProperty m_EdgeRadius;
        private SerializedProperty m_Points;

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

        protected override void OnEditStart()
        {
            m_PolyUtility.StartEditing(target as Collider2D);
        }

        protected override void OnEditEnd()
        {
            m_PolyUtility.StopEditing();
        }

        public void OnSceneGUI()
        {
            if (!editingCollider)
                return;

            m_PolyUtility.OnSceneGUI();
        }
    }
}
