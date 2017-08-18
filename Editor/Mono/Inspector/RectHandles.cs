// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class RectHandles
    {
        static Styles s_Styles;
        class Styles
        {
            public readonly GUIStyle dragdot = "U2D.dragDot";
            public readonly GUIStyle pivotdot = "U2D.pivotDot";
            public readonly GUIStyle dragdotactive = "U2D.dragDotActive";
            public readonly GUIStyle pivotdotactive = "U2D.pivotDotActive";
        }

        private static int s_LastCursorId;

        internal static bool RaycastGUIPointToWorldHit(Vector2 guiPoint, Plane plane, out Vector3 hit)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPoint);
            float dist = 0f;
            bool isHit = plane.Raycast(ray, out dist);
            hit = isHit ? ray.GetPoint(dist) : Vector3.zero;
            return isHit;
        }

        internal static void DetectCursorChange(int id)
        {
            if (HandleUtility.nearestControl == id)
            {
                // Don't optimize this to only use event if s_LastCursorId wasn't already id.
                // Cursor can sometimes change for the same handle.
                s_LastCursorId = id;
                Event.current.Use();
            }
            else if (s_LastCursorId == id)
            {
                s_LastCursorId = 0;
                Event.current.Use();
            }
        }

        internal static Vector3 SideSlider(int id, Vector3 position, Vector3 sideVector, Vector3 direction, float size, Handles.CapFunction capFunction, float snap)
        {
            return SideSlider(id, position, sideVector, direction, size, capFunction, snap, 0);
        }

        internal static Vector3 SideSlider(int id, Vector3 position, Vector3 sideVector, Vector3 direction, float size, Handles.CapFunction capFunction, float snap, float bias)
        {
            Event evt = Event.current;
            Vector3 handleDir = Vector3.Cross(sideVector, direction).normalized;
            Vector3 pos = Handles.Slider2D(id, position, handleDir, direction, sideVector, 0, capFunction, Vector2.one * snap);
            pos = position + Vector3.Project(pos - position, direction);

            switch (evt.type)
            {
                case EventType.Layout:
                    Vector3 sideDir = sideVector.normalized;
                    HandleUtility.AddControl(id, HandleUtility.DistanceToLine(position + sideVector * 0.5f - sideDir * size * 2, position - sideVector * 0.5f + sideDir * size * 2) - bias);
                    break;

                case EventType.MouseMove:
                    DetectCursorChange(id);
                    break;

                case EventType.Repaint:
                    if ((HandleUtility.nearestControl == id && GUIUtility.hotControl == 0) || GUIUtility.hotControl == id)
                        HandleDirectionalCursor(position, handleDir, direction);
                    break;
            }

            return pos;
        }

        internal static Vector3 CornerSlider(int id, Vector3 cornerPos, Vector3 handleDir, Vector3 outwardsDir1, Vector3 outwardsDir2, float handleSize, Handles.CapFunction drawFunc, Vector2 snap)
        {
            Event evt = Event.current;
            Vector3 pos = Handles.Slider2D(id, cornerPos, handleDir, outwardsDir1, outwardsDir2, handleSize, drawFunc, snap);

            switch (evt.type)
            {
                case EventType.MouseMove:
                    DetectCursorChange(id);
                    break;

                case EventType.Repaint:
                    if ((HandleUtility.nearestControl == id && GUIUtility.hotControl == 0) || GUIUtility.hotControl == id)
                        HandleDirectionalCursor(cornerPos, handleDir, outwardsDir1 + outwardsDir2);
                    break;
            }
            return pos;
        }

        private static void HandleDirectionalCursor(Vector3 handlePosition, Vector3 handlePlaneNormal, Vector3 direction)
        {
            Vector2 mousePosition = Event.current.mousePosition;

            // Find cursor direction (supports perspective camera)
            Plane guiPlane = new Plane(handlePlaneNormal, handlePosition);
            Vector3 mousePosWorld;
            if (RaycastGUIPointToWorldHit(mousePosition, guiPlane, out mousePosWorld))
            {
                Vector2 cursorDir = WorldToScreenSpaceDir(mousePosWorld, direction);
                // 200px x 200px rect around mousepos to switch cursor via fake cursorRect.
                Rect mouseScreenRect = new Rect(mousePosition.x - 100f, mousePosition.y - 100f, 200f, 200f);
                EditorGUIUtility.AddCursorRect(mouseScreenRect, GetScaleCursor(cursorDir));
            }
        }

        public static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
        {
            // Project A and B onto the plane orthogonal target axis
            dirA = Vector3.ProjectOnPlane(dirA, axis);
            dirB = Vector3.ProjectOnPlane(dirB, axis);

            // Find (positive) angle between A and B
            float angle = Vector3.Angle(dirA, dirB);

            // Return angle multiplied with 1 or -1
            return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
        }

        public static float RotationSlider(int id, Vector3 cornerPos, float rotation, Vector3 pivot, Vector3 handleDir, Vector3 outwardsDir1, Vector3 outwardsDir2, float handleSize, Handles.CapFunction drawFunc, Vector2 snap)
        {
            Vector3 diagonal = (outwardsDir1 + outwardsDir2);
            Vector2 screenCorner = HandleUtility.WorldToGUIPoint(cornerPos);
            Vector2 screenOffset = HandleUtility.WorldToGUIPoint(cornerPos + diagonal) - screenCorner;
            screenOffset = screenOffset.normalized * 15;
            RaycastGUIPointToWorldHit(screenCorner + screenOffset, new Plane(handleDir, cornerPos), out cornerPos);

            Event evt = Event.current;
            Vector3 pos = Handles.Slider2D(id, cornerPos, handleDir, outwardsDir1, outwardsDir2, handleSize, drawFunc, Vector2.zero);

            if (evt.type == EventType.MouseMove)
                DetectCursorChange(id);

            if (evt.type == EventType.Repaint)
            {
                if ((HandleUtility.nearestControl == id && GUIUtility.hotControl == 0) || GUIUtility.hotControl == id)
                {
                    Rect mouseScreenRect = new Rect(evt.mousePosition.x - 100f, evt.mousePosition.y - 100f, 200f, 200f);
                    EditorGUIUtility.AddCursorRect(mouseScreenRect, MouseCursor.RotateArrow);
                }
            }

            return rotation - AngleAroundAxis(pos - pivot, cornerPos - pivot, handleDir);
        }

        static Vector2 WorldToScreenSpaceDir(Vector3 worldPos, Vector3 worldDir)
        {
            Vector3 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
            Vector3 screenPosPlusDirection = HandleUtility.WorldToGUIPoint(worldPos + worldDir);
            Vector2 screenSpaceDir = screenPosPlusDirection - screenPos;
            screenSpaceDir.y *= -1;
            return screenSpaceDir;
        }

        private static MouseCursor GetScaleCursor(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

            if (angle < 0f)
                angle = 360f + angle;

            if (angle < 0f + 27.5f)
                return MouseCursor.ResizeVertical;
            if (angle < 45f + 27.5f)
                return MouseCursor.ResizeUpRight;
            if (angle < 90f + 27.5f)
                return MouseCursor.ResizeHorizontal;
            if (angle < 135f + 27.5f)
                return MouseCursor.ResizeUpLeft;
            if (angle < 180f + 27.5f)
                return MouseCursor.ResizeVertical;
            if (angle < 225f + 27.5f)
                return MouseCursor.ResizeUpRight;
            if (angle < 270f + 27.5f)
                return MouseCursor.ResizeHorizontal;
            if (angle < 315f + 27.5f)
                return MouseCursor.ResizeUpLeft;
            else
                return MouseCursor.ResizeVertical;
        }

        public static void RectScalingHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            switch (eventType)
            {
                case EventType.Layout:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * .5f));
                    break;
                case EventType.Repaint:
                    DrawImageBasedCap(controlID, position, rotation, size, s_Styles.dragdot, s_Styles.dragdotactive);
                    break;
            }
        }

        public static void PivotHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            switch (eventType)
            {
                case EventType.Layout:
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * .5f));
                    break;
                case EventType.Repaint:
                    DrawImageBasedCap(controlID, position, rotation, size, s_Styles.pivotdot, s_Styles.pivotdotactive);
                    break;
            }
        }

        static void DrawImageBasedCap(int controlID, Vector3 position, Quaternion rotation, float size, GUIStyle normal, GUIStyle active)
        {
            // Don't draw positions behind the camera
            if (Camera.current && Vector3.Dot(position - Camera.current.transform.position, Camera.current.transform.forward) < 0)
                return;

            Vector3 screenPos = HandleUtility.WorldToGUIPoint(position);

            Handles.BeginGUI();
            float w = normal.fixedWidth;
            float h = normal.fixedHeight;
            Rect r = new Rect(screenPos.x - w / 2f, screenPos.y - h / 2f, w, h);
            if (GUIUtility.hotControl == controlID)
                active.Draw(r, GUIContent.none, controlID);
            else
                normal.Draw(r, GUIContent.none, controlID);

            Handles.EndGUI();
        }

        public static void RenderRectWithShadow(bool active, params Vector3[] corners)
        {
            Vector3[] verts = new Vector3[] { corners[0], corners[1], corners[2], corners[3], corners[0] };

            Color oldColor = Handles.color;
            Handles.color = new Color(1f, 1f, 1f, active ? 1f : 0.5f);
            DrawPolyLineWithShadow(new Color(0f, 0f, 0f, active ? 1f : 0.5f), new Vector2(1f, -1f), verts);
            Handles.color = oldColor;
        }

        static Vector3[] s_TempVectors = new Vector3[0];
        public static void DrawPolyLineWithShadow(Color shadowColor, Vector2 screenOffset, params Vector3[] points)
        {
            Camera cam = Camera.current;
            if (!cam || Event.current.type != EventType.Repaint)
                return;

            if (s_TempVectors.Length != points.Length)
                s_TempVectors = new Vector3[points.Length];

            for (int i = 0; i < points.Length; i++)
                s_TempVectors[i] = cam.ScreenToWorldPoint(cam.WorldToScreenPoint(points[i]) + (Vector3)screenOffset);

            Color oldColor = Handles.color;

            // shadow
            shadowColor.a = shadowColor.a * oldColor.a;
            Handles.color = shadowColor;
            Handles.DrawPolyLine(s_TempVectors);

            // line itself
            Handles.color = oldColor;
            Handles.DrawPolyLine(points);
        }

        public static void DrawDottedLineWithShadow(Color shadowColor, Vector2 screenOffset, Vector3 p1, Vector3 p2, float screenSpaceSize)
        {
            Camera cam = Camera.current;
            if (!cam || Event.current.type != EventType.Repaint)
                return;

            Color oldColor = Handles.color;

            // shadow
            shadowColor.a = shadowColor.a * oldColor.a;
            Handles.color = shadowColor;
            Handles.DrawDottedLine(
                cam.ScreenToWorldPoint(cam.WorldToScreenPoint(p1) + (Vector3)screenOffset),
                cam.ScreenToWorldPoint(cam.WorldToScreenPoint(p2) + (Vector3)screenOffset), screenSpaceSize);

            // line itself
            Handles.color = oldColor;
            Handles.DrawDottedLine(p1, p2, screenSpaceSize);
        }
    }
}
