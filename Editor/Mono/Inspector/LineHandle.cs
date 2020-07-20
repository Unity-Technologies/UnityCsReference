// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    class LineHandle
    {
        public enum LineIntersectionHighlight
        {
            // don't draw intersecting line segments with a red highlight
            None,
            // only draw the highlight when a new point is being inserted
            Inserting,
            // always draw the highlight on intersecting edges
            Always
        }

        const float k_LinePickDistance = 50f;
        const float k_PointPickDistance = 15f;
        const float k_ActiveLineSegmentWidth = 5f;
        static readonly int s_InsertPointHash = "s_InsertPointHash".GetHashCode();
        static readonly int s_LineControlHash = "s_LineControlHash".GetHashCode();
        static Vector3 s_InsertedPointPosition;
        static int s_InsertedIndex;
        const float k_DotHandleSize = .04f;
        const float k_ProximityDotHandleSize = .05f;
        static Vector3[] s_AAPolyLinePoints = new Vector3[2];

        Vector3[] m_Points;
        bool m_Loop;
        LineIntersectionHighlight m_IntersectionHighlight;

        public Vector3[] points
        {
            get => m_Points;
            set => m_Points = value;
        }

        public LineHandle(IList<Vector2> points, bool loop, LineIntersectionHighlight intersectionHighlight = LineIntersectionHighlight.None)
        {
            int count = points.Count;
            m_Points = new Vector3[count];
            for (int i = 0; i < count; i++)
                m_Points[i] = points[i];
            m_Loop = loop;
            m_IntersectionHighlight = intersectionHighlight;
        }

        public LineHandle(IList<Vector3> points, bool loop, LineIntersectionHighlight intersectionHighlight = LineIntersectionHighlight.None)
        {
            int count = points.Count;
            m_Points = new Vector3[count];
            for (int i = 0; i < count; i++)
                m_Points[i] = points[i];
            m_Loop = loop;
            m_IntersectionHighlight = intersectionHighlight;
        }

        public void OnGUI(Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2)
        {
            Do(GUIUtility.GetControlID(s_LineControlHash, FocusType.Passive),
                ref m_Points,
                handleDir,
                slideDir1,
                slideDir2,
                k_DotHandleSize,
                Handles.DotHandleCap,
                EditorSnapSettings.move,
                false,
                m_IntersectionHighlight,
                m_Loop);

            for (int i = 0, c = m_Points.Length; i < c; i++)
            {
                float guiSize = HandleUtility.GetHandleSize(m_Points[i]) * k_ProximityDotHandleSize;
                m_Points[i] = Handles.Slider2D(m_Points[i], Vector3.forward, Vector3.right, Vector3.up, guiSize, ProximityDotHandleCap, EditorSnapSettings.move);
            }

            HandleUtility.s_CustomPickDistance = HandleUtility.kPickDistance;
        }

        static void ProximityDotHandleCap(
            int controlID,
            Vector3 position,
            Quaternion rotation,
            float size,
            EventType eventType)
        {
            // Draw the handle cap only if it is the hotcontrol or is the nearest control with no hotcontrol active.
            if (eventType == EventType.Repaint
                && !(GUIUtility.hotControl == controlID || HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0))
                return;

            if (eventType == EventType.Layout)
                HandleUtility.s_CustomPickDistance = k_PointPickDistance;

            Handles.DotHandleCap(controlID, position, rotation, size, eventType);

            HandleUtility.s_CustomPickDistance = HandleUtility.kPickDistance;
        }

        static bool IsRemoveEvent(Event evt)
        {
            // this is matching existing 2d collider editor behaviour
            return GUIUtility.hotControl == 0 && (evt.command || evt.control);
        }

        public static void Do(
            ref Vector3[] line,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.CapFunction capFunction,
            LineIntersectionHighlight intersectionHighlight,
            bool loop = false
        )
        {
            int id = GUIUtility.GetControlID(s_InsertPointHash, FocusType.Passive);
            Do(id, ref line, handleDir, slideDir1, slideDir2, handleSize, capFunction, EditorSnapSettings.move, false, intersectionHighlight, loop);
        }

        public static void Do(
            int id,
            ref Vector3[] line,
            Vector3 handleDir,
            Vector3 slideDir1,
            Vector3 slideDir2,
            float handleSize,
            Handles.CapFunction capFunction,
            Vector2 snap,
            bool drawHelper,
            LineIntersectionHighlight intersectionHighlight,
            bool loop
        )
        {
            var evt = Event.current;
            var activeIndex = GUIUtility.hotControl == id ? s_InsertedIndex : -1;
            var evtType = evt.GetTypeForControl(id);

            switch (evtType)
            {
                case EventType.Layout:
                case EventType.MouseMove:
                {
                    HandleUtility.s_CustomPickDistance = k_LinePickDistance;
                    if (line != null && line.Length > 1)
                        HandleUtility.AddControl(id, HandleUtility.DistanceToPolyLine(line, loop, out _));
                    break;
                }

                case EventType.Repaint:
                {
                    HandleUtility.DistanceToPolyLine(line, loop, out int hoveredEdgeIndex);

                    var color = Handles.color;
                    int lineCount = line.Length;

                    for (int n = 0, lineIterCount = loop ? line.Length : line.Length - 1; n < lineIterCount; n++)
                    {
                        int a = n, b = Wrap(n + 1, lineCount);

                        // Inserting a point has special handling to draw the inserted lines thicker, and optionally
                        // draw a red warning line if intersections are detected
                        if (activeIndex > -1 && RangeContains(n, lineCount, activeIndex - 1, 2))
                        {
                            Handles.color = intersectionHighlight != LineIntersectionHighlight.None &&
                                LineIntersectsPath(line, line[a], line[b], slideDir1, slideDir2, loop,
                                a - 1, 3)
                                ? Color.red
                                : color;

                            s_AAPolyLinePoints[0] = line[a];
                            s_AAPolyLinePoints[1] = line[b];
                            Handles.DrawAAPolyLine(k_ActiveLineSegmentWidth, s_AAPolyLinePoints);
                        }
                        else
                        // Draw the hovered line segment with a thicker line. If this is a remove event, we also handle
                        // drawing the next segment.
                        if (GUIUtility.hotControl == 0
                            && HandleUtility.nearestControl == id
                            && (a == hoveredEdgeIndex || (IsRemoveEvent(evt) && a == hoveredEdgeIndex + 1)))
                        {
                            Handles.color = intersectionHighlight == LineIntersectionHighlight.Always &&
                                LineIntersectsPath(
                                line, line[a], line[b], slideDir1, slideDir2, loop, a - 1, 3)
                                ? Color.red
                                : color;

                            // If we're in 'delete' mode, both the hovered edge and the following edge are highlighted
                            // as removal candidates
                            if (IsRemoveEvent(evt))
                            {
                                Handles.color = Color.red;
                                Handles.DrawAAPolyLine(k_ActiveLineSegmentWidth, line[hoveredEdgeIndex], line[Wrap(hoveredEdgeIndex + 1, lineCount)]);
                                Handles.DrawAAPolyLine(k_ActiveLineSegmentWidth, line[Wrap(hoveredEdgeIndex + 1, lineCount)], line[loop ? Wrap(hoveredEdgeIndex + 2, lineCount) : Mathf.Clamp(hoveredEdgeIndex + 2, 0, lineCount - 1)]);
                            }
                            else
                            {
                                Handles.DrawAAPolyLine(k_ActiveLineSegmentWidth, line[a], line[b]);
                            }
                        }
                        else
                        {
                            Handles.color = intersectionHighlight == LineIntersectionHighlight.Always &&
                                LineIntersectsPath(
                                line, line[a], line[b], slideDir1, slideDir2, loop, a - 1, 3)
                                ? Color.red
                                : color;

                            Handles.DrawLine(line[a], line[b]);
                        }

                        Handles.color = color;
                    }

                    // If this is the active hotcontrol, let Slider2D do the drawing
                    if (GUIUtility.hotControl == id)
                        goto default;

                    // If no hotControl is active and we are the nearest control, show a preview of the insertion point
                    if (GUIUtility.hotControl != 0 || HandleUtility.nearestControl != id || capFunction == null)
                        break;

                    var ls = Handles.matrix.MultiplyPoint3x4(line[Wrap(hoveredEdgeIndex, lineCount)]);
                    var le = Handles.matrix.MultiplyPoint3x4(line[Wrap(hoveredEdgeIndex + 1, lineCount)]);

                    var direction = le - ls;
                    var constraint = Vector3.Normalize(direction);

                    if (HandleUtility.CalcParamOnConstraint(Camera.current, evt.mousePosition, ls, constraint,
                        out float param))
                    {
                        var handlePosition = ls + constraint * Mathf.Clamp(param, 0f, direction.magnitude);
                        var localSpace = Handles.inverseMatrix.MultiplyPoint3x4(handlePosition);
                        capFunction(id,
                            localSpace,
                            Quaternion.identity,
                            HandleUtility.GetHandleSize(localSpace) * handleSize,
                            EventType.Repaint);
                    }

                    break;
                }

                case EventType.MouseDown:
                {
                    if (HandleUtility.nearestControl == id && evt.button == 0 && GUIUtility.hotControl == 0 && !evt.alt)
                    {
                        HandleUtility.DistanceToPolyLine(line, loop, out int nearestEdgeIndex);
                        int count = line.Length;

                        if (IsRemoveEvent(evt))
                        {
                            ArrayUtility.RemoveAt(ref line, (nearestEdgeIndex + 1) % count);
                            GUI.changed = true;
                            evt.Use();
                            return;
                        }

                        var ls = Handles.matrix.MultiplyPoint3x4(line[nearestEdgeIndex]);
                        var le = Handles.matrix.MultiplyPoint3x4(line[(nearestEdgeIndex + 1) % count]);
                        activeIndex = nearestEdgeIndex;

                        var direction = le - ls;
                        var constraint = Vector3.Normalize(direction);

                        if (!HandleUtility.CalcParamOnConstraint(Camera.current, evt.mousePosition, ls, constraint,
                            out float param))
                            break;

                        var handlePosition = ls + constraint * Mathf.Clamp(param, 0f, direction.magnitude);
                        s_InsertedPointPosition = Handles.inverseMatrix.MultiplyPoint(handlePosition);

                        if (activeIndex != -1)
                        {
                            s_InsertedIndex = (activeIndex + 1) % line.Length;
                            ArrayUtility.Insert(ref line, s_InsertedIndex, s_InsertedPointPosition);
                            GUI.changed = true;
                        }

                        goto default;
                    }

                    break;
                }

                case EventType.MouseUp:
                    s_InsertedIndex = -1;
                    goto default;

                default:
                {
                    s_InsertedPointPosition = Slider2D.Do(
                        id,
                        s_InsertedPointPosition,
                        Vector3.zero,
                        handleDir,
                        slideDir1,
                        slideDir2,
                        HandleUtility.GetHandleSize(s_InsertedPointPosition) * handleSize,
                        capFunction,
                        snap,
                        drawHelper);

                    if (GUIUtility.hotControl == id && s_InsertedIndex > -1)
                        line[s_InsertedIndex] = s_InsertedPointPosition;

                    break;
                }
            }
        }

        static int Wrap(int i, int c)
        {
            return ((i % c) + c) % c;
        }

        static bool RangeContains(int index, int count, int rangeStart, int rangeCount)
        {
            return (index >= rangeStart && index < rangeStart + rangeCount)
                || rangeStart + rangeCount >= count && index < Wrap(rangeStart + rangeCount, count);
        }

        internal static bool LineIntersectsPath(Vector3[] path,
            Vector3 lineA,
            Vector3 lineB,
            Vector3 slideDir1,
            Vector3 slideDir2,
            bool loop,
            int ignoreIndex,
            int ignoreCount)
        {
            Vector3 tan = slideDir1.normalized;
            Vector3 bitan = slideDir2.normalized;

            Vector2 a = Project(lineA, tan, bitan);
            Vector2 b = Project(lineB, tan, bitan);
            Vector2 prev = Project(path[0], tan, bitan);

            for (int i = 1, c = path.Length; i < (loop ? c + 1 : c); i++)
            {
                Vector2 next = Project(path[i % c], tan, bitan);
                int edge = (i % c) - 1;
                // ignore intersections between the source edge it's neighbours
                if (!RangeContains(edge, c, ignoreIndex, ignoreCount) && GetLineSegmentIntersect(a, b, prev, next))
                    return true;
                prev = next;
            }

            return false;
        }

        static Vector2 Project(Vector3 point, Vector3 tan, Vector3 bitan)
        {
            return new Vector2(Vector3.Dot(tan, point), Vector3.Dot(bitan, point));
        }

        internal static bool GetLineSegmentIntersect(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 s1, s2;
            s1.x = p1.x - p0.x;
            s1.y = p1.y - p0.y;
            s2.x = p3.x - p2.x;
            s2.y = p3.y - p2.y;

            float s, t;
            s = (-s1.y * (p0.x - p2.x) + s1.x * (p0.y - p2.y)) / (-s2.x * s1.y + s1.x * s2.y);
            t = (s2.x * (p0.y - p2.y) - s2.y * (p0.x - p2.x)) / (-s2.x * s1.y + s1.x * s2.y);

            return s > 0f && s < 1 && t > 0f && t < 1f;
        }
    }
}
