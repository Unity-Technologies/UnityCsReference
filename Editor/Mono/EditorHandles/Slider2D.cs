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

        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        public static Vector3 Do(
            int id,
            Vector3 handlePos,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.DrawCapFunction drawFunc,
            float snap,
            bool drawHelper)
        #pragma warning restore 618
        {
            return Do(id, handlePos, new Vector3(0, 0, 0), handleDir, slideDir1, slideDir2, handleSize, drawFunc, new Vector2(snap, snap), drawHelper);
        }

        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        public static Vector3 Do(
            int id,
            Vector3 handlePos,
            Vector3 offset,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.DrawCapFunction drawFunc,
            float snap,
            bool drawHelper)
        #pragma warning restore 618
        {
            return Do(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, drawFunc, new Vector2(snap, snap), drawHelper);
        }

        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        public static Vector3 Do(
            int id,
            Vector3 handlePos,
            Vector3 offset,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.DrawCapFunction drawFunc,
            Vector2 snap,
            bool drawHelper)
        #pragma warning restore 618
        {
            bool orgGuiChanged = GUI.changed;
            GUI.changed = false;

            Vector2 delta = CalcDeltaAlongDirections(id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, drawFunc, snap, drawHelper);
            if (GUI.changed)
                handlePos = s_StartPosition + slideDir1 * delta.x + slideDir2 * delta.y;

            GUI.changed |= orgGuiChanged;
            return handlePos;
        }

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
                handlePos = s_StartPosition + slideDir1 * delta.x + slideDir2 * delta.y;

            GUI.changed |= orgGuiChanged;
            return handlePos;
        }

        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        private static Vector2 CalcDeltaAlongDirections(
            int id,
            Vector3 handlePos,
            Vector3 offset,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.DrawCapFunction drawFunc,
            Vector2 snap,
            bool drawHelper)
        #pragma warning restore 618
        {
            Vector2 deltaDistanceAlongDirections = new Vector2(0, 0);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    // This is an ugly hack. It would be better if the drawFunc can handle it's own layout.
                    if (drawFunc == Handles.ArrowCap)
                    {
                        HandleUtility.AddControl(id, HandleUtility.DistanceToLine(handlePos + offset, handlePos + handleDir * handleSize));
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle((handlePos + offset) + handleDir * handleSize, handleSize * .2f));
                    }
                    else if (drawFunc == Handles.RectangleCap)
                    {
                        HandleUtility.AddControl(id, HandleUtility.DistanceToRectangle(handlePos + offset, Quaternion.LookRotation(handleDir, slideDir1), handleSize));
                    }
                    else
                    {
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(handlePos + offset, handleSize * .5f));
                    }
                    break;

                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0 && GUIUtility.hotControl == 0)
                    {
                        Plane plane = new Plane(Handles.matrix.MultiplyVector(handleDir), Handles.matrix.MultiplyPoint(handlePos));
                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                        float dist = 0.0f;
                        plane.Raycast(mouseRay, out dist);

                        GUIUtility.hotControl = id; // Grab mouse focus
                        s_CurrentMousePosition = evt.mousePosition;
                        s_StartPosition = handlePos;

                        Vector3 localMousePoint = Handles.inverseMatrix.MultiplyPoint(mouseRay.GetPoint(dist));
                        Vector3 clickOffset = localMousePoint - handlePos;
                        s_StartPlaneOffset.x = Vector3.Dot(clickOffset, slideDir1);
                        s_StartPlaneOffset.y = Vector3.Dot(clickOffset, slideDir2);

                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        Vector3 worldPosition = Handles.matrix.MultiplyPoint(handlePos);
                        Vector3 worldSlideDir1 = Handles.matrix.MultiplyVector(slideDir1).normalized;
                        Vector3 worldSlideDir2 = Handles.matrix.MultiplyVector(slideDir2).normalized;

                        // Detect hit with plane (ray from campos to cursor)
                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(s_CurrentMousePosition);
                        Plane plane = new Plane(worldPosition, worldPosition + worldSlideDir1, worldPosition + worldSlideDir2);
                        float dist = 0.0f;
                        if (plane.Raycast(mouseRay, out dist))
                        {
                            Vector3 hitpos = Handles.inverseMatrix.MultiplyPoint(mouseRay.GetPoint(dist));

                            // Determine hitpos projection onto slideDirs
                            deltaDistanceAlongDirections.x = HandleUtility.PointOnLineParameter(hitpos, s_StartPosition, slideDir1);
                            deltaDistanceAlongDirections.y = HandleUtility.PointOnLineParameter(hitpos, s_StartPosition, slideDir2);
                            deltaDistanceAlongDirections -= s_StartPlaneOffset;
                            if (snap.x > 0 || snap.y > 0)
                            {
                                deltaDistanceAlongDirections.x = Handles.SnapValue(deltaDistanceAlongDirections.x, snap.x);
                                deltaDistanceAlongDirections.y = Handles.SnapValue(deltaDistanceAlongDirections.y, snap.y);
                            }

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
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
                case EventType.Repaint:
                {
                    if (drawFunc == null)
                        break;

                    Vector3 position = handlePos + offset;
                    Quaternion rotation = Quaternion.LookRotation(handleDir, slideDir1);

                    Color temp = Color.white;

                    if (id == GUIUtility.hotControl)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.preselectionColor;
                    }

                    drawFunc(id, position, rotation, handleSize);

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;

                    // Draw a helper rectangle to show what plane we are dragging in
                    if (drawHelper && GUIUtility.hotControl == id)
                    {
                        Vector3[] verts = new Vector3[4];
                        float helperSize = handleSize * 10.0f;
                        verts[0] = position + (slideDir1 * helperSize + slideDir2 * helperSize);
                        verts[1] = verts[0] - slideDir1 * helperSize * 2.0f;
                        verts[2] = verts[1] - slideDir2 * helperSize * 2.0f;
                        verts[3] = verts[2] + slideDir1 * helperSize * 2.0f;
                        Color prevColor = Handles.color;
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
                    if (capFunction != null)
                        capFunction(id, position, rotation, handleSize, EventType.Layout);
                    else
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(handlePos + offset, handleSize * .5f));
                    break;
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0 && GUIUtility.hotControl == 0)
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
                            if (snap.x > 0 || snap.y > 0)
                            {
                                deltaDistanceAlongDirections.x = Handles.SnapValue(deltaDistanceAlongDirections.x, snap.x);
                                deltaDistanceAlongDirections.y = Handles.SnapValue(deltaDistanceAlongDirections.y, snap.y);
                            }

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
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
                case EventType.Repaint:
                {
                    if (capFunction == null)
                        break;

                    Color temp = Color.white;
                    if (id == GUIUtility.hotControl)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.preselectionColor;
                    }

                    capFunction(id, position, rotation, handleSize, EventType.Repaint);

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;

                    // Draw a helper rectangle to show what plane we are dragging in
                    if (drawHelper && GUIUtility.hotControl == id)
                    {
                        Vector3[] verts = new Vector3[4];
                        float helperSize = handleSize * 10.0f;
                        verts[0] = position + (slideDir1 * helperSize + slideDir2 * helperSize);
                        verts[1] = verts[0] - slideDir1 * helperSize * 2.0f;
                        verts[2] = verts[1] - slideDir2 * helperSize * 2.0f;
                        verts[3] = verts[2] + slideDir1 * helperSize * 2.0f;
                        Color prevColor = Handles.color;
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
