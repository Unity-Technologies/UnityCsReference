// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class Slider1D
    {
        // Used for plane intersection translation
        static Vector3 s_ConstraintOrigin, s_ConstraintDirection, s_HandleOffset;
        static float s_StartHandleSize;

        // Used for 2D translation (fallback when ray plane intersection fails)
        static Vector2 s_StartMousePosition;
        static Vector3 s_StartPosition;

        internal static Vector3 Do(int id, Vector3 position, Vector3 direction, float size, Handles.CapFunction capFunction, float snap)
        {
            return Do(id, position, Vector3.zero, direction, direction, size, capFunction, snap);
        }

        internal static Vector3 Do(int id, Vector3 position, Vector3 offset, Vector3 handleDirection, Vector3 slideDirection, float size, Handles.CapFunction capFunction, float snap)
        {
            Event evt = Event.current;
            var eventType = evt.GetTypeForControl(id);
            switch (eventType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    if (capFunction != null)
                        capFunction(id, position + offset, Quaternion.LookRotation(handleDirection), size, eventType);
                    else
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position + offset, size * .2f));
                    break;

                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0 && GUIUtility.hotControl == 0 && !evt.alt)
                    {
                        GUIUtility.hotControl = id;    // Grab mouse focus
                        s_StartMousePosition = evt.mousePosition;
                        s_ConstraintOrigin = Handles.matrix.MultiplyPoint3x4(position);
                        s_StartPosition = position;
                        s_ConstraintDirection = Handles.matrix.MultiplyVector(slideDirection);
                        s_HandleOffset = HandleUtility.CalcPositionOnConstraint(Camera.current, evt.mousePosition, s_ConstraintOrigin, s_ConstraintDirection, out Vector3 point)
                            ? s_ConstraintOrigin - point
                            : Vector3.zero;
                        evt.Use();
                        s_StartHandleSize = HandleUtility.GetHandleSize(point);
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        // First try to calculate the translation by casting a mouse ray against a world position plane
                        // oriented towards the camera. This gives more accurate results than doing the line translation
                        // in 2D space, but is more prone towards skewing extreme values when the ray is near parallel
                        // to the plane. To address this, CalcPositionOnConstraint will fail if the mouse ray is close
                        // to parallel (see HandleUtility.k_MinRayConstraintDot) and fall back to 2D based movement.
                        if (HandleUtility.CalcPositionOnConstraint(Camera.current, evt.mousePosition, s_ConstraintOrigin, s_ConstraintDirection, out Vector3 worldPosition))
                        {
                            var handleOffset = s_HandleOffset * (HandleUtility.GetHandleSize(worldPosition) / s_StartHandleSize);
                            worldPosition += handleOffset;

                            if (EditorSnapSettings.incrementalSnapActive)
                            {
                                Vector3 dir = worldPosition - s_ConstraintOrigin;
                                float dist = Handles.SnapValue(dir.magnitude, snap) * Mathf.Sign(Vector3.Dot(s_ConstraintDirection, dir));
                                worldPosition = s_ConstraintOrigin + s_ConstraintDirection.normalized * dist;
                            }
                            else if (EditorSnapSettings.gridSnapActive)
                            {
                                worldPosition = Snapping.Snap(worldPosition, GridSettings.size, (SnapAxis) new SnapAxisFilter(s_ConstraintDirection));
                            }

                            position = Handles.inverseMatrix.MultiplyPoint(worldPosition);
                            s_StartPosition = position;
                            s_StartMousePosition = evt.mousePosition;
                        }
                        else
                        {
                            // Unlike HandleUtility.CalcPositionOnConstraint, CalcLineTranslation _does_ multiply constraint
                            // origin and direction by Handles.matrix, so make sure to pass in unmodified vectors here
                            float dist = HandleUtility.CalcLineTranslation(s_StartMousePosition, evt.mousePosition, s_StartPosition, slideDirection);
                            dist = Handles.SnapValue(dist, snap);
                            worldPosition = Handles.matrix.MultiplyPoint(s_StartPosition) + s_ConstraintDirection * dist;
                            if (EditorSnapSettings.gridSnapActive)
                                worldPosition = Snapping.Snap(worldPosition, GridSettings.size, (SnapAxis) new SnapAxisFilter(s_ConstraintDirection));
                            position = Handles.inverseMatrix.MultiplyPoint(worldPosition);
                        }

                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;

                case EventType.Repaint:
                    Handles.SetupHandleColor(id, evt, out var prevColor, out var thickness);
                    capFunction(id, position + offset, Quaternion.LookRotation(handleDirection), size, EventType.Repaint);
                    Handles.color = prevColor;
                    break;
            }
            return position;
        }
    }
}
