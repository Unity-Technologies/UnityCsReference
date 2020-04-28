// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class Slider2D
    {
        private static Vector2 s_CurrentMousePosition;
        private static Vector3 s_StartPosition;
        private static Vector2 s_StartPlaneOffset;

        // Returns the new handlePos
        public static Vector3 Do(
            int id,
            Vector3 handlePos,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.CapFunction capFunction,
            float snap,
            bool drawHelper)
        {
            return Do(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, capFunction, new Vector2(snap, snap), drawHelper);
        }

        // Returns the new handlePos
        public static Vector3 Do(
            int id,
            Vector3 handlePos,
            Vector3 offset,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.CapFunction capFunction,
            float snap,
            bool drawHelper)
        {
            return Do(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, new Vector2(snap, snap), drawHelper);
        }

        // Returns the new handlePos
        public static Vector3 Do(
            int id,
            Vector3 handlePos,
            Vector3 offset,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.CapFunction capFunction,
            Vector2 snap,
            bool drawHelper)
        {
            bool orgGuiChanged = GUI.changed;
            GUI.changed = false;

            Vector2 delta = CalcDeltaAlongDirections(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snap, drawHelper);
            if (GUI.changed)
            {
                handlePos = s_StartPosition + slideDir1 * delta.x + slideDir2 * delta.y;

                if (EditorSnapSettings.gridSnapActive)
                {
                    var normal = Vector3.Cross(slideDir1, slideDir2);

                    if (Snapping.IsCardinalDirection(normal))
                    {
                        var worldSpace = Handles.matrix.MultiplyPoint(handlePos);
                        worldSpace = Snapping.Snap(worldSpace, GridSettings.size, (SnapAxis) ~new SnapAxisFilter(normal));
                        handlePos = Handles.inverseMatrix.MultiplyPoint(worldSpace);
                    }
                }
            }

            GUI.changed |= orgGuiChanged;
            return handlePos;
        }

        // Returns the distance the new position has moved along slideDir1 and slideDir2
        private static Vector2 CalcDeltaAlongDirections(
            int id,
            Vector3 handlePos,
            Vector3 offset,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.CapFunction capFunction,
            Vector2 snap,
            bool drawHelper)
        {
            Vector3 position = handlePos + offset;
            Quaternion rotation = Quaternion.LookRotation(handleDir, slideDir1);
            Vector2 deltaDistanceAlongDirections = new Vector2(0, 0);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    if (capFunction != null)
                        capFunction(id, position, rotation, handleSize, EventType.Layout);
                    else
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(handlePos + offset, handleSize * .5f));
                    break;

                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0 && GUIUtility.hotControl == 0 && !evt.alt)
                    {
                        s_CurrentMousePosition = evt.mousePosition;
                        bool success = true;
                        Vector3 localMousePoint = Handles.inverseMatrix.MultiplyPoint(GetMousePosition(handleDir, handlePos, ref success));
                        if (success)
                        {
                            GUIUtility.hotControl = id; // Grab mouse focus
                            s_StartPosition = handlePos;

                            Vector3 clickOffset = localMousePoint - handlePos;
                            s_StartPlaneOffset.x = Vector3.Dot(clickOffset, slideDir1);
                            s_StartPlaneOffset.y = Vector3.Dot(clickOffset, slideDir2);

                            evt.Use();
                            EditorGUIUtility.SetWantsMouseJumping(1);
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        bool success = true;
                        Vector3 localMousePoint = Handles.inverseMatrix.MultiplyPoint(GetMousePosition(handleDir, handlePos, ref success));
                        if (success)
                        {
                            // Determine hitpos projection onto slideDirs
                            deltaDistanceAlongDirections.x = HandleUtility.PointOnLineParameter(localMousePoint, s_StartPosition, slideDir1);
                            deltaDistanceAlongDirections.y = HandleUtility.PointOnLineParameter(localMousePoint, s_StartPosition, slideDir2);
                            deltaDistanceAlongDirections -= s_StartPlaneOffset;
                            deltaDistanceAlongDirections.x = Handles.SnapValue(deltaDistanceAlongDirections.x, snap.x);
                            deltaDistanceAlongDirections.y = Handles.SnapValue(deltaDistanceAlongDirections.y, snap.y);

                            GUI.changed = true;
                        }
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;

                case EventType.Repaint:
                {
                    if (capFunction == null)
                        break;

                    Handles.SetupHandleColor(id, evt, out var prevColor, out var thickness);
                    capFunction(id, position, rotation, handleSize, EventType.Repaint);
                    Handles.color = prevColor;

                    // Draw a helper rectangle to show what plane we are dragging in
                    if (drawHelper && GUIUtility.hotControl == id)
                    {
                        Vector3[] verts = new Vector3[4];
                        float helperSize = handleSize * 10.0f;
                        verts[0] = position + (slideDir1 * helperSize + slideDir2 * helperSize);
                        verts[1] = verts[0] - slideDir1 * helperSize * 2.0f;
                        verts[2] = verts[1] - slideDir2 * helperSize * 2.0f;
                        verts[3] = verts[2] + slideDir1 * helperSize * 2.0f;
                        Handles.color = Color.white;
                        float outline = 0.6f;
                        Handles.DrawSolidRectangleWithOutline(verts, new Color(1, 1, 1, 0.05f), new Color(outline, outline, outline, 0.4f));
                        Handles.color = prevColor;
                    }
                }

                break;
            }

            return deltaDistanceAlongDirections;
        }

        private static Vector3 GetMousePosition(Vector3 handleDirection, Vector3 handlePosition, ref bool success)
        {
            if (Camera.current != null)
            {
                Plane plane = new Plane(Handles.matrix.MultiplyVector(handleDirection), Handles.matrix.MultiplyPoint(handlePosition));
                Ray mouseRay = HandleUtility.GUIPointToWorldRay(s_CurrentMousePosition);
                float dist = 0.0f;
                success = plane.Raycast(mouseRay, out dist);
                return mouseRay.GetPoint(dist);
            }
            else
            {
                success = true;
                return s_CurrentMousePosition;
            }
        }
    }
}
