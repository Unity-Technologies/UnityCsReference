// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
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

    [EditorTool("Edit Edge Collider", typeof(EdgeCollider2D))]
    class EdgeCollider2DTool : EditorTool
    {
        public override GUIContent toolbarIcon { get { return PrimitiveBoundsHandle.editModeButton; } }
        PolygonEditorUtility m_PolyUtility = new PolygonEditorUtility();
        AdjacentEdgeUtility m_AdjacentEdgeUtility = new AdjacentEdgeUtility();

        public void OnEnable()
        {
            EditorTools.EditorTools.activeToolChanged += OnActiveToolChanged;
            EditorTools.EditorTools.activeToolChanging += OnActiveToolChanging;
        }

        public void OnDisable()
        {
            EditorTools.EditorTools.activeToolChanged -= OnActiveToolChanged;
            EditorTools.EditorTools.activeToolChanging -= OnActiveToolChanging;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            m_PolyUtility.OnSceneGUI();
            m_AdjacentEdgeUtility.OnSceneGUI();
        }

        void OnActiveToolChanged()
        {
            if (EditorTools.EditorTools.IsActiveTool(this))
            {
                m_PolyUtility.StartEditing(target as Collider2D);
                m_AdjacentEdgeUtility.StartEditing(target as EdgeCollider2D);
            }
        }

        void OnActiveToolChanging()
        {
            if (EditorTools.EditorTools.IsActiveTool(this))
            {
                m_PolyUtility.StopEditing();
                m_AdjacentEdgeUtility.StopEditing();
            }
        }
    }

    internal class AdjacentEdgeUtility
    {
        const float k_HandlePointSnap = 10f;
        const float k_HandlePickDistance = 50f;

        private EdgeCollider2D m_ActiveCollider;

        public void StartEditing(EdgeCollider2D collider)
        {
            if (collider == null)
                throw new NullReferenceException("AdjacentEdgeUtility cannot start editing a NULL collider.");

            m_ActiveCollider = collider;

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public void StopEditing()
        {
            m_ActiveCollider = null;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        public void OnSceneGUI()
        {
            if (m_ActiveCollider == null)
                return;

            if (!m_ActiveCollider.useAdjacentStartPoint && !m_ActiveCollider.useAdjacentEndPoint)
                return;

            // Handles.Slider2D will render active point as yellow if there is keyboardControl set. We don't want that happening.
            GUIUtility.keyboardControl = 0;
            HandleUtility.s_CustomPickDistance = k_HandlePickDistance;

            Vector3 newLocalPoint;
            if (m_ActiveCollider.useAdjacentStartPoint && AdjustPoint(m_ActiveCollider.adjacentStartPoint, out newLocalPoint))
                m_ActiveCollider.adjacentStartPoint = newLocalPoint;

            if (m_ActiveCollider.useAdjacentEndPoint && AdjustPoint(m_ActiveCollider.adjacentEndPoint, out newLocalPoint))
                m_ActiveCollider.adjacentEndPoint = newLocalPoint;
        }

        bool AdjustPoint(Vector3 localPoint, out Vector3 newLocalPoint)
        {
            newLocalPoint = localPoint;

            Vector3 colliderOffset = m_ActiveCollider.offset;
            localPoint += colliderOffset;

            var transform = m_ActiveCollider.transform;
            var worldPoint = transform.TransformPoint(localPoint);
            var guiSize = HandleUtility.GetHandleSize(worldPoint) * 0.04f;

            EditorGUI.BeginChangeCheck();

            Handles.color = Color.green;

            var newWorldPoint = Handles.Slider2D(
                worldPoint,
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                guiSize,
                Handles.DotHandleCap,
                Vector3.zero);

            Handles.color = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(m_ActiveCollider, "Edit Collider");

                newLocalPoint = transform.InverseTransformPoint(newWorldPoint);
                newLocalPoint -= colliderOffset;
                return true;
            }

            return false;
        }

        private void UndoRedoPerformed()
        {
            if (m_ActiveCollider != null)
            {
                EdgeCollider2D collider = m_ActiveCollider;
                StopEditing();
                StartEditing(collider);
            }
        }
    }
}
