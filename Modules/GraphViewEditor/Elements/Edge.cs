// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class Edge : GraphElement
    {
        const float k_EndPointRadius = 4.0f;
        const float k_InterceptWidth = 3.0f;
        const string k_EdgeWidthProperty = "edge-width";
        const string k_SelectedEdgeColorProperty = "selected-edge-color";
        const string k_EdgeColorProperty = "edge-color";

        private GraphView m_GraphView;

        public Edge()
        {
            clipChildren = false;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            if (parent != null)
            {
                Vector2 from = new Vector2(), to = new Vector2();
                GetFromToPoints(ref from, ref to);

                SetPosition(
                    Rect.MinMaxRect(from.x > to.x ? to.x : from.x,
                        from.y > to.y ? to.y : from.y,
                        from.x < to.x ? to.x : from.x,
                        from.y < to.y ? to.y : from.y));
            }
        }

        // TODO lots of redundant code in here that could be factorized
        // TODO The tangents are calculated way to often. We should compute them on repaint only.

        protected static void GetTangents(Orientation orientation, Vector2 from, Vector2 to, out Vector3[] points, out Vector3[] tangents)
        {
            if ((orientation == Orientation.Horizontal && from.x < to.x))
            {
                Vector3 t = to;
                to = from;
                from = t;
            }

            points = new Vector3[] {to, from};
            tangents = new Vector3[2];

            const float minTangent = 30;

            float weight = .5f;
            float weight2 = 1 - weight;
            float y = 0;

            float cleverness = Mathf.Clamp01(((to - from).magnitude - 10) / 50);

            if (orientation == Orientation.Horizontal)
            {
                tangents[0] = to + new Vector2((from.x - to.x) * weight + minTangent, y) * cleverness;
                tangents[1] = from + new Vector2((from.x - to.x) * -weight2 - minTangent, -y) * cleverness;
            }
            else
            {
                float tangentSize = to.y - from.y + 100.0f;
                tangentSize = Mathf.Min((to - from).magnitude, tangentSize);
                if (tangentSize < 0.0f)
                    tangentSize = -tangentSize;

                tangents[0] = to + new Vector2(0, tangentSize * -0.5f);
                tangents[1] = from + new Vector2(0, tangentSize * 0.5f);
            }
        }

        public override bool Overlaps(Rect rect)
        {
            // bounding box check succeeded, do more fine grained check by checking intersection between the rectangles' diagonal
            // and the line segments
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;

            if (outputPresenter == null && inputPresenter == null)
                return false;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(ref from, ref to);

            Orientation orientation = outputPresenter != null ? outputPresenter.orientation : inputPresenter.orientation;

            Vector3[] points, tangents;

            GetTangents(orientation, from, to, out points, out tangents);
            Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);

            for (int a = 0; a < allPoints.Length; a++)
            {
                if (a >= allPoints.Length - 1)
                {
                    break;
                }

                var segmentA = new Vector2(allPoints[a].x, allPoints[a].y);
                var segmentB = new Vector2(allPoints[a + 1].x, allPoints[a + 1].y);

                if (RectUtils.IntersectsSegment(rect, segmentA, segmentB))
                    return true;
            }

            return false;
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            // bounding box check succeeded, do more fine grained check by measuring distance to bezier points
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;

            if (outputPresenter == null && inputPresenter == null)
                return false;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(ref from, ref to);

            // exclude endpoints
            if (Vector2.Distance(from, localPoint) <= 2 * k_EndPointRadius ||
                Vector2.Distance(to, localPoint) <= 2 * k_EndPointRadius)
            {
                return false;
            }

            Orientation orientation = outputPresenter != null ? outputPresenter.orientation : inputPresenter.orientation;

            Vector3[] points, tangents;
            GetTangents(orientation, from, to, out points, out tangents);
            Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);

            float minDistance = Mathf.Infinity;
            foreach (Vector3 currentPoint in allPoints)
            {
                float distance = Vector3.Distance(currentPoint, localPoint);
                minDistance = Mathf.Min(minDistance, distance);
                if (minDistance < k_InterceptWidth)
                {
                    return true;
                }
            }

            return false;
        }

        public override void DoRepaint()
        {
            // Edges do NOT call base.DoRepaint. It would create a visual artifact.
            DrawEdge();
        }

        protected void GetFromToPoints(ref Vector2 from, ref Vector2 to)
        {
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;
            if (outputPresenter == null && inputPresenter == null)
                return;

            if (m_GraphView == null)
                m_GraphView = this.GetFirstOfType<GraphView>();

            if (outputPresenter != null)
            {
                NodeAnchor leftAnchor = m_GraphView.Query<NodeAnchor>().Where(e => e.direction == Direction.Output && e.GetPresenter<NodeAnchorPresenter>() == outputPresenter).First();
                if (leftAnchor != null)
                {
                    from = leftAnchor.GetGlobalCenter();
                    from = worldTransform.inverse.MultiplyPoint3x4(from);
                }
            }
            else
            {
                from = worldTransform.inverse.MultiplyPoint3x4(new Vector3(edgePresenter.candidatePosition.x, edgePresenter.candidatePosition.y));
            }

            if (inputPresenter != null)
            {
                NodeAnchor rightAnchor = m_GraphView.Query<NodeAnchor>().Where(e => e.direction == Direction.Input && e.GetPresenter<NodeAnchorPresenter>() == inputPresenter).First();
                if (rightAnchor != null)
                {
                    to = rightAnchor.GetGlobalCenter();
                    to = worldTransform.inverse.MultiplyPoint3x4(to);
                }
            }
            else
            {
                to = worldTransform.inverse.MultiplyPoint3x4(new Vector3(edgePresenter.candidatePosition.x, edgePresenter.candidatePosition.y));
            }
        }

        StyleValue<int> m_EdgeWidth;
        public int edgeWidth
        {
            get
            {
                return m_EdgeWidth.GetSpecifiedValueOrDefault(2);
            }
        }

        StyleValue<Color> m_SelectedColor;
        public Color selectedColor
        {
            get
            {
                return m_SelectedColor.GetSpecifiedValueOrDefault(new Color(240 / 255f, 240 / 255f, 240 / 255f));
            }
        }

        StyleValue<Color> m_DefaultColor;
        public Color defaultColor
        {
            get
            {
                return m_DefaultColor.GetSpecifiedValueOrDefault(new Color(146 / 255f, 146 / 255f, 146 / 255f));
            }
        }

        public override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);

            styles.ApplyCustomProperty(k_EdgeWidthProperty, ref m_EdgeWidth);
            styles.ApplyCustomProperty(k_SelectedEdgeColorProperty, ref m_SelectedColor);
            styles.ApplyCustomProperty(k_EdgeColorProperty, ref m_DefaultColor);
        }

        protected virtual void DrawEdge()
        {
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;

            if (outputPresenter == null && inputPresenter == null)
                return;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(ref from, ref to);

            Color edgeColor = edgePresenter.selected ? selectedColor : defaultColor;

            Orientation orientation = outputPresenter != null ? outputPresenter.orientation : inputPresenter.orientation;

            Vector3[] points, tangents;
            GetTangents(orientation, from, to, out points, out tangents);
            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, edgeWidth);

            Color oldColor = Handles.color;

            // TODO need to fix color of unconnected ends now that we've changed how the connection being built work (i.e. left is not always guaranteed to be the connected end... in fact, left doesn't exist anymore)
            // dot on top of anchor showing it's connected
            Handles.color = defaultColor;
            Handles.DrawSolidDisc(from, new Vector3(0.0f, 0.0f, -1.0f), k_EndPointRadius);
            if (edgePresenter.input == null)
                Handles.color = oldColor;
            Handles.DrawSolidDisc(to, new Vector3(0.0f, 0.0f, -1.0f), k_EndPointRadius);
            Handles.color = oldColor;
        }
    }
}
