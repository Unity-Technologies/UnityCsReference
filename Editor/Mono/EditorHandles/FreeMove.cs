// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Snap;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class FreeMove
    {
        private static Vector2 s_StartMousePosition, s_CurrentMousePosition;
        private static Vector3 s_StartPosition;

        public static Vector3 Do(int id, Vector3 position, Quaternion rotation, float size, Vector3 snap, Handles.CapFunction handleFunction)
        {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 origMatrix = Handles.matrix;

            VertexSnapping.HandleMouseMove(id);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseMove:
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction(id, worldPosition, Camera.current.transform.rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;
                    break;

                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        HandleUtility.ignoreRaySnapObjects = null;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        bool rayDrag = EditorGUI.actionKey && evt.shift;

                        if (rayDrag)
                        {
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();

                            object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null)
                            {
                                RaycastHit rh = (RaycastHit)hit;
                                float offset = 0;
                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    float geomOffset = HandleUtility.CalcRayPlaceOffset(HandleUtility.ignoreRaySnapObjects, rh.normal);
                                    if (geomOffset != Mathf.Infinity)
                                    {
                                        offset = Vector3.Dot(position, rh.normal) - geomOffset;
                                    }
                                }
                                position = Handles.inverseMatrix.MultiplyPoint(rh.point + (rh.normal * offset));
                            }
                            else
                            {
                                rayDrag = false;
                            }
                        }

                        if (!rayDrag)
                        {
                            // normal drag
                            s_CurrentMousePosition += new Vector2(evt.delta.x, -evt.delta.y) * EditorGUIUtility.pixelsPerPoint;
                            Vector3 screenPos = Camera.current.WorldToScreenPoint(Handles.matrix.MultiplyPoint(s_StartPosition));
                            screenPos += (Vector3)(s_CurrentMousePosition - s_StartMousePosition);
                            position = Handles.inverseMatrix.MultiplyPoint(Camera.current.ScreenToWorldPoint(screenPos));

                            if (Tools.vertexDragging)
                            {
                                if (HandleUtility.ignoreRaySnapObjects == null)
                                    Handles.SetupIgnoreRaySnapObjects();
                                Vector3 near;
                                if (HandleUtility.FindNearestVertex(evt.mousePosition, null, out near))
                                {
                                    position = Handles.inverseMatrix.MultiplyPoint(near);
                                }
                            }

                            if (EditorSnapSettings.incrementalSnapActive && !evt.shift)
                            {
                                Vector3 delta = position - s_StartPosition;
                                delta = Handles.SnapValue(delta, snap);
                                position = s_StartPosition + delta;
                            }
                        }
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        HandleUtility.ignoreRaySnapObjects = null;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;

                case EventType.Repaint:
                    Handles.SetupHandleColor(id, evt, out var prevColor, out var thickness);
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction(id, worldPosition, Camera.current.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;
                    Handles.color = prevColor;
                    break;
            }
            return position;
        }
    }
}
