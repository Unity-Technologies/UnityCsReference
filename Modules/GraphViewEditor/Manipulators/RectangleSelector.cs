// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class RectangleSelector : MouseManipulator
    {
        private readonly RectangleSelect m_Rectangle;
        bool m_Active;

        public RectangleSelector()
        {
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            m_Rectangle = new RectangleSelect();
            m_Rectangle.style.positionType = PositionType.Absolute;
            m_Rectangle.style.positionTop = 0;
            m_Rectangle.style.positionLeft = 0;
            m_Rectangle.style.positionBottom = 0;
            m_Rectangle.style.positionRight = 0;
            m_Active = false;
        }

        // get the axis aligned bound
        public Rect ComputeAxisAlignedBound(Rect position, Matrix4x4 transform)
        {
            Vector3 min = transform.MultiplyPoint3x4(position.min);
            Vector3 max = transform.MultiplyPoint3x4(position.max);
            return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (e.target != target)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (CanStartManipulation(e))
            {
                if (!e.ctrlKey)
                {
                    graphView.ClearSelection();
                }

                graphView.Add(m_Rectangle);

                m_Rectangle.start = e.localMousePosition;
                m_Rectangle.end = m_Rectangle.start;

                m_Active = true;
                target.TakeCapture(); // We want to receive events even when mouse is not over ourself.
                e.StopPropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (!CanStopManipulation(e))
                return;

            graphView.Remove(m_Rectangle);

            m_Rectangle.end = e.localMousePosition;

            var selectionRect = new Rect()
            {
                min = new Vector2(Math.Min(m_Rectangle.start.x, m_Rectangle.end.x), Math.Min(m_Rectangle.start.y, m_Rectangle.end.y)),
                max = new Vector2(Math.Max(m_Rectangle.start.x, m_Rectangle.end.x), Math.Max(m_Rectangle.start.y, m_Rectangle.end.y))
            };

            selectionRect = ComputeAxisAlignedBound(selectionRect, graphView.viewTransform.matrix.inverse);

            List<ISelectable> selection = graphView.selection;

            // a copy is necessary because Add To selection might cause a SendElementToFront which will change the order.
            List<ISelectable> newSelection = new List<ISelectable>();
            graphView.graphElements.ForEach(child =>
                {
                    Matrix4x4 selectableTransform = child.transform.matrix.inverse;
                    var localSelRect = new Rect(selectableTransform.MultiplyPoint3x4(selectionRect.position),
                            selectableTransform.MultiplyPoint3x4(selectionRect.size));
                    if (child.IsSelectable() && child.Overlaps(localSelRect))
                    {
                        newSelection.Add(child);
                    }
                });

            foreach (var selectable in newSelection)
            {
                if (selection.Contains(selectable))
                {
                    if (e.ctrlKey) // invert selection on ctrl only
                        graphView.RemoveFromSelection(selectable);
                }
                else
                    graphView.AddToSelection(selectable);
            }

            m_Active = false;
            target.ReleaseCapture();
            e.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            m_Rectangle.end = e.localMousePosition;
            e.StopPropagation();
        }

        private class RectangleSelect : VisualElement
        {
            public Vector2 start { get; set; }
            public Vector2 end { get; set; }

            public override void DoRepaint()
            {
                VisualElement t = parent;
                Vector2 screenStart = start;
                Vector2 screenEnd = end;

                // Avoid drawing useless information
                if (start == end)
                    return;

                // Apply offset
                screenStart += t.layout.position;
                screenEnd += t.layout.position;

                var r = new Rect
                {
                    min = new Vector2(Math.Min(screenStart.x, screenEnd.x), Math.Min(screenStart.y, screenEnd.y)),
                    max = new Vector2(Math.Max(screenStart.x, screenEnd.x), Math.Max(screenStart.y, screenEnd.y))
                };

                var lineColor = new Color(1.0f, 0.6f, 0.0f, 1.0f);
                var segmentSize = 5f;

                Vector3[] points =
                {
                    new Vector3(r.xMin, r.yMin, 0.0f),
                    new Vector3(r.xMax, r.yMin, 0.0f),
                    new Vector3(r.xMax, r.yMax, 0.0f),
                    new Vector3(r.xMin, r.yMax, 0.0f)
                };

                DrawDottedLine(points[0], points[1], segmentSize, lineColor);
                DrawDottedLine(points[1], points[2], segmentSize, lineColor);
                DrawDottedLine(points[2], points[3], segmentSize, lineColor);
                DrawDottedLine(points[3], points[0], segmentSize, lineColor);

                var str = "(" + String.Format("{0:0}", start.x) + ", " + String.Format("{0:0}", start.y) + ")";
                GUI.skin.label.Draw(new Rect(screenStart.x, screenStart.y - 18.0f, 200.0f, 20.0f), new GUIContent(str), 0);
                str = "(" + String.Format("{0:0}", end.x) + ", " + String.Format("{0:0}", end.y) + ")";
                GUI.skin.label.Draw(new Rect(screenEnd.x - 80.0f, screenEnd.y + 5.0f, 200.0f, 20.0f), new GUIContent(str), 0);
            }

            private void DrawDottedLine(Vector3 p1, Vector3 p2, float segmentsLength, Color col)
            {
                HandleUtility.ApplyWireMaterial();

                GL.Begin(GL.LINES);
                GL.Color(col);

                float length = Vector3.Distance(p1, p2); // ignore z component
                int count = Mathf.CeilToInt(length / segmentsLength);
                for (int i = 0; i < count; i += 2)
                {
                    GL.Vertex((Vector3.Lerp(p1, p2, i * segmentsLength / length)));
                    GL.Vertex((Vector3.Lerp(p1, p2, (i + 1) * segmentsLength / length)));
                }

                GL.End();
            }
        }
    }
}
