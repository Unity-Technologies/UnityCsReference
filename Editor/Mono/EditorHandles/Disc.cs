// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class Disc
    {
        const int k_MaxSnapMarkers = 360 / 5;
        const float k_RotationUnitSnapMajorMarkerStep = 45;
        const float k_RotationUnitSnapMarkerSize = 0.1f;
        const float k_RotationUnitSnapMajorMarkerSize = 0.2f;
        const float k_GrabZoneScale = 0.3f;

        static Vector2 s_StartMousePosition, s_CurrentMousePosition;
        static Vector3 s_StartPosition, s_StartAxis;
        static Quaternion s_StartRotation;
        static float s_RotationDist;

        public static Quaternion Do(int id, Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, float snap)
        {
            return Do(id, rotation, position, axis, size, cutoffPlane, snap, true, true, Handles.secondaryColor);
        }

        public static Quaternion Do(int id, Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, float snap, bool enableRayDrag, bool showHotArc, Color fillColor)
        {
            if (Mathf.Abs(Vector3.Dot(Camera.current.transform.forward, axis)) > .999f)
                cutoffPlane = false;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    float d;
                    if (cutoffPlane)
                    {
                        Vector3 from = Vector3.Cross(axis, Camera.current.transform.forward).normalized;
                        d = HandleUtility.DistanceToArc(position, axis, from, 180, size) * k_GrabZoneScale;
                    }
                    else
                    {
                        d = HandleUtility.DistanceToDisc(position, axis, size) * k_GrabZoneScale;
                    }

                    HandleUtility.AddControl(id, d);
                    break;
                }
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;    // Grab mouse focus
                        Tools.LockHandlePosition();
                        if (cutoffPlane)
                        {
                            Vector3 from = Vector3.Cross(axis, Camera.current.transform.forward).normalized;
                            s_StartPosition = HandleUtility.ClosestPointToArc(position, axis, from, 180, size);
                        }
                        else
                        {
                            s_StartPosition = HandleUtility.ClosestPointToDisc(position, axis, size);
                        }
                        s_RotationDist = 0;
                        s_StartRotation = rotation;
                        s_StartAxis = axis;
                        s_CurrentMousePosition = s_StartMousePosition = Event.current.mousePosition;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        // handle look to point rotation
                        bool rayDrag = EditorGUI.actionKey && evt.shift && enableRayDrag;
                        if (rayDrag)
                        {
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();
                            object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null && Vector3.Dot(axis.normalized, rotation * Vector3.forward) < 0.999)
                            {
                                RaycastHit rh = (RaycastHit)hit;
                                Vector3 lookPoint = rh.point - position;
                                Vector3 lookPointProjected = lookPoint - Vector3.Dot(lookPoint, axis.normalized) * axis.normalized;
                                rotation = Quaternion.LookRotation(lookPointProjected, rotation * Vector3.up);
                            }
                        }
                        else
                        {
                            Vector3 direction = Vector3.Cross(axis, position - s_StartPosition).normalized;
                            s_CurrentMousePosition += evt.delta;
                            s_RotationDist = HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, s_StartPosition, direction) / size * 30;
                            s_RotationDist = Handles.SnapValue(s_RotationDist, snap);
                            rotation = Quaternion.AngleAxis(s_RotationDist * -1, s_StartAxis) * s_StartRotation;
                        }

                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        Tools.UnlockHandlePosition();
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
                case EventType.KeyDown:
                    if (evt.keyCode == KeyCode.Escape && GUIUtility.hotControl == id)
                    {
                        // We do not use the event nor clear hotcontrol to ensure auto revert value kicks in from native side
                        Tools.UnlockHandlePosition();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.Repaint:
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

                    // If we're dragging it, we'll go a bit further and draw a selection pie
                    if (GUIUtility.hotControl == id)
                    {
                        Color t = Handles.color;
                        Vector3 from = (s_StartPosition - position).normalized;
                        Handles.color = fillColor;
                        Handles.DrawLine(position, position + from * size);
                        var d = -Mathf.Sign(s_RotationDist) * Mathf.Repeat(Mathf.Abs(s_RotationDist), 360);
                        Vector3 to = Quaternion.AngleAxis(d, axis) * from;
                        Handles.DrawLine(position, position + to * size);

                        Handles.color = fillColor * new Color(1, 1, 1, .2f);
                        for (int i = 0, revolutions = (int)Mathf.Abs(s_RotationDist * 0.002777777778f); i < revolutions; ++i)
                            Handles.DrawSolidDisc(position, axis, size);
                        Handles.DrawSolidArc(position, axis, from, d, size);

                        // Draw snap markers
                        if (EditorGUI.actionKey && snap > 0)
                        {
                            DrawRotationUnitSnapMarkers(position, axis, size, k_RotationUnitSnapMarkerSize, snap, @from);
                            DrawRotationUnitSnapMarkers(position, axis, size, k_RotationUnitSnapMajorMarkerSize, k_RotationUnitSnapMajorMarkerStep, @from);
                        }
                        Handles.color = t;
                    }

                    if (showHotArc && GUIUtility.hotControl == id || GUIUtility.hotControl != id && !cutoffPlane)
                        Handles.DrawWireDisc(position, axis, size);
                    else if (GUIUtility.hotControl != id && cutoffPlane)
                    {
                        Vector3 from = Vector3.Cross(axis, Camera.current.transform.forward).normalized;
                        Handles.DrawWireArc(position, axis, from, 180, size);
                    }

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }

            return rotation;
        }

        static void DrawRotationUnitSnapMarkers(Vector3 position, Vector3 axis, float handleSize, float markerSize, float snap, Vector3 @from)
        {
            var iterationCount = Mathf.FloorToInt(360 / snap);
            var performFading = iterationCount > k_MaxSnapMarkers;
            var limitedIterationCount = Mathf.Min(iterationCount, k_MaxSnapMarkers);

            // center the markers around the current angle
            var count = Mathf.RoundToInt(limitedIterationCount * 0.5f);

            for (var i = -count; i < count; ++i)
            {
                var rot = Quaternion.AngleAxis(i * snap, axis);
                var u = rot * @from;
                var startPoint = position + (1 - markerSize) * handleSize * u;
                var endPoint = position + 1 * handleSize * u;
                Handles.color = Handles.selectedColor;
                if (performFading)
                {
                    var alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Abs(i / ((float)limitedIterationCount - 1) - 0.5f) * 2);
                    Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, alpha);
                }
                Handles.DrawLine(startPoint, endPoint);
            }
        }
    }
}
