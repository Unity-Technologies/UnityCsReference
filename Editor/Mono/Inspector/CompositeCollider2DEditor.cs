// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(CompositeCollider2D))]
    [CanEditMultipleObjects]
    internal class CompositeCollider2DEditor : Collider2DEditorBase
    {
        private SerializedProperty m_GeometryType;
        private SerializedProperty m_GenerationType;
        private SerializedProperty m_VertexDistance;
        private SerializedProperty m_EdgeRadius;
        readonly AnimBool m_ShowEdgeRadius = new AnimBool();
        readonly AnimBool m_ShowManualGenerationButton = new AnimBool();

        public override void OnEnable()
        {
            base.OnEnable();

            m_GeometryType = serializedObject.FindProperty("m_GeometryType");
            m_GenerationType = serializedObject.FindProperty("m_GenerationType");
            m_VertexDistance = serializedObject.FindProperty("m_VertexDistance");
            m_EdgeRadius = serializedObject.FindProperty("m_EdgeRadius");
            m_ShowEdgeRadius.value = targets.Where(x => (x as CompositeCollider2D).geometryType == CompositeCollider2D.GeometryType.Polygons).Count() == 0;
            m_ShowEdgeRadius.valueChanged.AddListener(Repaint);
            m_ShowManualGenerationButton.value = targets.Where(x => (x as CompositeCollider2D).generationType != CompositeCollider2D.GenerationType.Manual).Count() == 0;
            m_ShowManualGenerationButton.valueChanged.AddListener(Repaint);
        }

        public override void OnDisable()
        {
            m_ShowEdgeRadius.valueChanged.RemoveListener(Repaint);
            m_ShowManualGenerationButton.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_GeometryType);
            EditorGUILayout.PropertyField(m_GenerationType);
            EditorGUILayout.PropertyField(m_VertexDistance);

            m_ShowManualGenerationButton.target = targets.Where(x => (x as CompositeCollider2D).generationType != CompositeCollider2D.GenerationType.Manual).Count() == 0;
            if (EditorGUILayout.BeginFadeGroup(m_ShowManualGenerationButton.faded))
            {
                if (GUILayout.Button("Regenerate Collider"))
                    foreach (var composite in targets)
                        (composite as CompositeCollider2D).GenerateGeometry();
            }
            EditorGUILayout.EndFadeGroup();

            m_ShowEdgeRadius.target = targets.Where(x => (x as CompositeCollider2D).geometryType == CompositeCollider2D.GeometryType.Polygons).Count() == 0;
            if (EditorGUILayout.BeginFadeGroup(m_ShowEdgeRadius.faded))
                EditorGUILayout.PropertyField(m_EdgeRadius);
            EditorGUILayout.EndFadeGroup();

            if (targets.Where(x =>
                    (x as CompositeCollider2D).geometryType == CompositeCollider2D.GeometryType.Outlines &&
                    (x as CompositeCollider2D).attachedRigidbody != null &&
                    (x as CompositeCollider2D).attachedRigidbody.bodyType == RigidbodyType2D.Dynamic).Count() > 0)
                EditorGUILayout.HelpBox("Outline geometry is composed of edges and will not preserve the original collider's center-of-mass or rotational inertia.  The CompositeCollider2D is attached to a Dynamic Rigidbody2D so you may need to explicitly set these if they are required.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();

            FinalizeInspectorGUI();
        }
    }
}
