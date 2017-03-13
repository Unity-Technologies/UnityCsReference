// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor
{
    internal class PolygonEditorUtility
    {
        const float k_HandlePointSnap = 10f;
        const float k_HandlePickDistance = 50f;

        private Collider2D m_ActiveCollider;
        private bool m_LoopingCollider = false;
        private int m_MinPathPoints = 3;

        private int m_SelectedPath = -1;
        private int m_SelectedVertex = -1;
        private int m_SelectedEdgePath = -1;
        private int m_SelectedEdgeVertex0 = -1;
        private int m_SelectedEdgeVertex1 = -1;
        private bool m_LeftIntersect = false;
        private bool m_RightIntersect = false;
        private bool m_DeleteMode = false;

        private bool m_FirstOnSceneGUIAfterReset;

        private bool m_HandlePoint = false;
        private bool m_HandleEdge = false;

        public void Reset()
        {
            m_SelectedPath = -1;
            m_SelectedVertex = -1;
            m_SelectedEdgePath = -1;
            m_SelectedEdgeVertex0 = -1;
            m_SelectedEdgeVertex1 = -1;
            m_LeftIntersect = false;
            m_RightIntersect = false;
            m_FirstOnSceneGUIAfterReset = true;
            m_HandlePoint = false;
            m_HandleEdge = false;
        }

        private void UndoRedoPerformed()
        {
            if (m_ActiveCollider != null)
            {
                Collider2D collider = m_ActiveCollider;
                StopEditing();
                StartEditing(collider);
            }
        }

        public void StartEditing(Collider2D collider)
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;

            Reset();

            PolygonCollider2D polygon = collider as PolygonCollider2D;
            if (polygon)
            {
                m_ActiveCollider = collider;
                m_LoopingCollider = true;
                m_MinPathPoints = 3;
                PolygonEditor.StartEditing(polygon);
                return;
            }

            EdgeCollider2D edge = collider as EdgeCollider2D;
            if (edge)
            {
                m_ActiveCollider = collider;
                m_LoopingCollider = false;
                m_MinPathPoints = 2;
                PolygonEditor.StartEditing(edge);
                return;
            }

            throw new NotImplementedException(string.Format("PolygonEditorUtility does not support {0}", collider));
        }

        public void StopEditing()
        {
            PolygonEditor.StopEditing();
            m_ActiveCollider = null;

            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        public void OnSceneGUI()
        {
            if (m_ActiveCollider == null)
                return;

            // Skip editing of polygon if view tool is active i.e. while panning the scene view
            if (Tools.viewToolActive)
                return;

            // Fetch the collider offset.
            var colliderOffset = m_ActiveCollider.offset;

            Event evt = Event.current;
            m_DeleteMode = evt.command || evt.control;
            Transform transform = m_ActiveCollider.transform;

            // Handles.Slider2D will render active point as yellow if there is keyboardControl set. We don't want that happening.
            GUIUtility.keyboardControl = 0;

            HandleUtility.s_CustomPickDistance = k_HandlePickDistance;

            // Find mouse positions in local and world space
            Plane plane = new Plane(-transform.forward, Vector3.zero);
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
            float dist;
            plane.Raycast(mouseRay, out dist);

            Vector3 mouseWorldPos = mouseRay.GetPoint(dist);
            Vector2 mouseLocalPos = transform.InverseTransformPoint(mouseWorldPos);

            // Select the active vertex and edge
            if (evt.type == EventType.MouseMove || m_FirstOnSceneGUIAfterReset)
            {
                int pathIndex;
                int pointIndex0, pointIndex1;
                float distance;
                if (PolygonEditor.GetNearestPoint(mouseLocalPos - colliderOffset, out pathIndex, out pointIndex0, out distance))
                {
                    m_SelectedPath = pathIndex;
                    m_SelectedVertex = pointIndex0;
                }
                else
                {
                    m_SelectedPath = -1;
                }

                if (PolygonEditor.GetNearestEdge(mouseLocalPos - colliderOffset, out pathIndex, out pointIndex0, out pointIndex1, out distance, m_LoopingCollider))
                {
                    m_SelectedEdgePath = pathIndex;
                    m_SelectedEdgeVertex0 = pointIndex0;
                    m_SelectedEdgeVertex1 = pointIndex1;
                }
                else
                {
                    m_SelectedEdgePath = -1;
                }

                if (evt.type == EventType.MouseMove)
                    evt.Use();
            }
            else if (evt.type == EventType.MouseUp)
            {
                m_LeftIntersect = false;
                m_RightIntersect = false;
            }

            // Do we handle point or line?
            // TODO: there probably isn't a case when selectedPath is valid and selectedEdge is invalid. This needs a refactor.


            if (GUIUtility.hotControl == 0)
            {
                if (m_SelectedPath != -1 && m_SelectedEdgePath != -1)
                {
                    // Calculate snapping distance
                    Vector2 point;
                    PolygonEditor.GetPoint(m_SelectedPath, m_SelectedVertex, out point);
                    point += colliderOffset;
                    Vector3 worldPos = transform.TransformPoint(point);
                    m_HandleEdge = (HandleUtility.WorldToGUIPoint(worldPos) - Event.current.mousePosition).sqrMagnitude > k_HandlePointSnap * k_HandlePointSnap;
                    m_HandlePoint = !m_HandleEdge;
                }
                else if (m_SelectedPath != -1)
                    m_HandlePoint = true;
                else if (m_SelectedEdgePath != -1)
                    m_HandleEdge = true;

                if (m_DeleteMode && m_HandleEdge)
                {
                    m_HandleEdge = false;
                    m_HandlePoint = true;
                }
            }

            bool applyToCollider = false;

            // Edge handle
            if (m_HandleEdge && !m_DeleteMode)
            {
                Vector2 p0, p1;
                PolygonEditor.GetPoint(m_SelectedEdgePath, m_SelectedEdgeVertex0, out p0);
                PolygonEditor.GetPoint(m_SelectedEdgePath, m_SelectedEdgeVertex1, out p1);
                p0 += colliderOffset;
                p1 += colliderOffset;
                Vector3 worldPosV0 = transform.TransformPoint(p0);
                Vector3 worldPosV1 = transform.TransformPoint(p1);
                worldPosV0.z = worldPosV1.z = 0;

                Handles.color = Color.green;
                Handles.DrawAAPolyLine(4.0f, new Vector3[] { worldPosV0, worldPosV1 });
                Handles.color = Color.white;

                Vector2 newPoint = GetNearestPointOnEdge(transform.TransformPoint(mouseLocalPos), worldPosV0, worldPosV1);

                EditorGUI.BeginChangeCheck();
                float guiSize = HandleUtility.GetHandleSize(newPoint) * 0.04f;
                Handles.color = Color.green;

                newPoint = Handles.Slider2D(
                        newPoint,
                        new Vector3(0, 0, 1),
                        new Vector3(1, 0, 0),
                        new Vector3(0, 1, 0),
                        guiSize,
                        Handles.DotHandleCap,
                        Vector3.zero);
                Handles.color = Color.white;
                if (EditorGUI.EndChangeCheck())
                {
                    PolygonEditor.InsertPoint(m_SelectedEdgePath, m_SelectedEdgeVertex1, ((p0 + p1) / 2) - colliderOffset);
                    m_SelectedPath = m_SelectedEdgePath;
                    m_SelectedVertex = m_SelectedEdgeVertex1;
                    m_HandleEdge = false;
                    m_HandlePoint = true;
                    applyToCollider = true;
                }
            }

            // Point handle
            if (m_HandlePoint)
            {
                Vector2 point;
                PolygonEditor.GetPoint(m_SelectedPath, m_SelectedVertex, out point);
                point += colliderOffset;
                Vector3 worldPos = transform.TransformPoint(point);
                worldPos.z = 0;
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);

                float guiSize = HandleUtility.GetHandleSize(worldPos) * 0.04f;

                if (m_DeleteMode && evt.type == EventType.MouseDown &&
                    Vector2.Distance(screenPos, Event.current.mousePosition) < k_HandlePickDistance ||
                    DeleteCommandEvent(evt))
                {
                    if (evt.type != EventType.ValidateCommand)
                    {
                        int pathPointCount = PolygonEditor.GetPointCount(m_SelectedPath);
                        if (pathPointCount > m_MinPathPoints)
                        {
                            PolygonEditor.RemovePoint(m_SelectedPath, m_SelectedVertex);
                            Reset();
                            applyToCollider = true;
                        }
                    }
                    evt.Use();
                }

                EditorGUI.BeginChangeCheck();
                Handles.color = m_DeleteMode ? Color.red : Color.green;
                Vector3 newWorldPos = Handles.Slider2D(
                        worldPos,
                        new Vector3(0, 0, 1),
                        new Vector3(1, 0, 0),
                        new Vector3(0, 1, 0),
                        guiSize,
                        Handles.DotHandleCap,
                        Vector3.zero);
                Handles.color = Color.white;
                if (EditorGUI.EndChangeCheck() && !m_DeleteMode)
                {
                    point = transform.InverseTransformPoint(newWorldPos);
                    point -= colliderOffset;
                    PolygonEditor.TestPointMove(m_SelectedPath, m_SelectedVertex, point, out m_LeftIntersect, out m_RightIntersect, m_LoopingCollider);
                    PolygonEditor.SetPoint(m_SelectedPath, m_SelectedVertex, point);
                    applyToCollider = true;
                }

                if (!applyToCollider)
                    DrawEdgesForSelectedPoint(newWorldPos, transform, m_LeftIntersect, m_RightIntersect, m_LoopingCollider);
            }

            // Apply changes
            if (applyToCollider)
            {
                Undo.RecordObject(m_ActiveCollider, "Edit Collider");
                PolygonEditor.ApplyEditing(m_ActiveCollider);
            }

            if (DeleteCommandEvent(evt))
                Event.current.Use();  // If we don't use the delete event in all cases, it sceneview might delete the entire object

            m_FirstOnSceneGUIAfterReset = false;
        }

        private bool DeleteCommandEvent(Event evt)
        {
            return (evt.type == EventType.ExecuteCommand || evt.type == EventType.ValidateCommand) && (evt.commandName == "Delete" || evt.commandName == "SoftDelete");
        }

        private void DrawEdgesForSelectedPoint(Vector3 worldPos, Transform transform, bool leftIntersect, bool rightIntersect, bool loop)
        {
            bool drawLeft = true;
            bool drawRight = true;

            int pathPointCount = UnityEditor.PolygonEditor.GetPointCount(m_SelectedPath);
            int v0 = m_SelectedVertex - 1;
            if (v0 == -1)
            {
                v0 = pathPointCount - 1;
                drawLeft = loop;
            }
            int v1 = m_SelectedVertex + 1;
            if (v1 == pathPointCount)
            {
                v1 = 0;
                drawRight = loop;
            }

            // Fetch the collider offset.
            var colliderOffset = m_ActiveCollider.offset;

            Vector2 p0, p1;
            UnityEditor.PolygonEditor.GetPoint(m_SelectedPath, v0, out p0);
            UnityEditor.PolygonEditor.GetPoint(m_SelectedPath, v1, out p1);
            p0 += colliderOffset;
            p1 += colliderOffset;
            Vector3 worldPosV0 = transform.TransformPoint(p0);
            Vector3 worldPosV1 = transform.TransformPoint(p1);
            worldPosV0.z = worldPosV1.z = worldPos.z;

            float lineWidth = 4.0f;
            if (drawLeft)
            {
                Handles.color = leftIntersect || m_DeleteMode ? Color.red : Color.green;
                Handles.DrawAAPolyLine(lineWidth, new Vector3[] { worldPos, worldPosV0 });
            }
            if (drawRight)
            {
                Handles.color = rightIntersect || m_DeleteMode ? Color.red : Color.green;
                Handles.DrawAAPolyLine(lineWidth, new Vector3[] { worldPos, worldPosV1 });
            }
            Handles.color = Color.white;
        }

        Vector2 GetNearestPointOnEdge(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 startToPoint = point - start;
            Vector2 startToEnd = (end - start).normalized;
            float dot = Vector2.Dot(startToEnd, startToPoint);

            if (dot <= 0)
                return start;

            if (dot >= Vector2.Distance(start, end))
                return end;

            Vector2 offsetToPoint = startToEnd * dot;
            return start + offsetToPoint;
        }
    }
}
