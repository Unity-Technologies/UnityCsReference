// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    enum LineType
    {
        Bezier,
        PolyLine,
        StraightLine,
    }

    internal
    class EdgeControl : VisualElement
    {
        private Orientation m_Orientation;
        public Orientation orientation
        {
            get { return m_Orientation; }
            set
            {
                if (m_Orientation == value)
                    return;
                m_Orientation = value;
                Dirty(ChangeType.Repaint);
            }
        }

        private LineType m_LineType;
        public LineType lineType
        {
            get { return m_LineType; }
            set
            {
                if (m_LineType == value)
                    return;
                m_LineType = value;
                PointsChanged();
            }
        }

        private Color m_EdgeColor;
        public Color edgeColor
        {
            get { return m_EdgeColor; }
            set
            {
                if (m_EdgeColor == value)
                    return;
                m_EdgeColor = value;
                Dirty(ChangeType.Repaint);
            }
        }

        private Color m_StartCapColor;
        public Color startCapColor
        {
            get { return m_StartCapColor; }
            set
            {
                if (m_StartCapColor == value)
                    return;
                m_StartCapColor = value;
                Dirty(ChangeType.Repaint);
            }
        }

        private Color m_EndCapColor;
        public Color endCapColor
        {
            get { return m_EndCapColor; }
            set
            {
                if (m_EndCapColor == value)
                    return;
                m_EndCapColor = value;
                Dirty(ChangeType.Repaint);
            }
        }

        private float m_CapRadius = 5;
        public float capRadius
        {
            get { return m_CapRadius; }
            set
            {
                if (m_CapRadius == value)
                    return;
                m_CapRadius = value;
                Dirty(ChangeType.Repaint);
            }
        }

        private int m_EdgeWidth = 2;
        public int edgeWidth
        {
            get { return m_EdgeWidth; }
            set
            {
                if (m_EdgeWidth == value)
                    return;
                m_EdgeWidth = value;
                Dirty(ChangeType.Repaint);
            }
        }

        private float m_InterceptWidth = 5;
        public float interceptWidth
        {
            get { return m_InterceptWidth; }
            set { m_InterceptWidth = value; }
        }

        private Vector2 m_From;
        public Vector2 from
        {
            get { return m_From; }
            set
            {
                if (m_From != value)
                {
                    m_From = value;
                    PointsChanged();
                }
            }
        }

        private Vector2 m_To;
        public Vector2 to
        {
            get { return m_To; }
            set
            {
                if (m_To != value)
                {
                    m_To = value;
                    PointsChanged();
                }
            }
        }

        private bool m_TangentsDirty;

        private Vector3[] m_ControlPoints;
        public Vector3[] controlPoints
        {
            get
            {
                if (m_TangentsDirty || m_ControlPoints == null)
                {
                    CacheLineData();
                    m_TangentsDirty = false;
                }
                return m_ControlPoints;
            }
        }

        private Vector3[] m_RenderPoints;
        public Vector3[] renderPoints
        {
            get
            {
                if (m_TangentsDirty || m_RenderPoints == null)
                {
                    CacheLineData();
                    m_TangentsDirty = false;
                }
                return m_RenderPoints;
            }
        }

        protected virtual void DrawEdge()
        {
            Vector3[] points = controlPoints;
            switch (lineType)
            {
                case LineType.Bezier:
                    Handles.DrawBezier(points[0], points[3], points[1], points[2], edgeColor, null, edgeWidth);
                    break;

                case LineType.PolyLine:
                case LineType.StraightLine:
                    Handles.color = edgeColor;
                    Handles.DrawAAPolyLine(edgeWidth, renderPoints);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Unsupported LineType: " + lineType);
            }
        }

        protected virtual void DrawEndpoint(Vector2 pos)
        {
            Handles.DrawSolidDisc(pos, new Vector3(0.0f, 0.0f, -1.0f), capRadius);
        }

        public override void DoRepaint()
        {
            // Edges do NOT call base.DoRepaint. It would create a visual artifact.
            Color oldColor = Handles.color;
            DrawEdge();

            Handles.color = startCapColor;
            DrawEndpoint(from);
            Handles.color = endCapColor;
            DrawEndpoint(to);
            Handles.color = oldColor;
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            // bounding box check succeeded, do more fine grained check by measuring distance to bezier points
            // exclude endpoints
            if (Vector2.Distance(from, localPoint) <= 2 * capRadius ||
                Vector2.Distance(to, localPoint) <= 2 * capRadius)
            {
                return false;
            }

            Vector3[] allPoints = renderPoints;

            float minDistance = Mathf.Infinity;
            for (var i = 0; i < allPoints.Length; i++)
            {
                Vector3 currentPoint = allPoints[i];
                float distance = Vector3.Distance(currentPoint, localPoint);
                minDistance = Mathf.Min(minDistance, distance);
                if (minDistance < interceptWidth)
                    return true;
            }

            return false;
        }

        public override bool Overlaps(Rect rect)
        {
            Vector3[] allPoints = renderPoints;

            for (int a = 0; a < allPoints.Length - 1; a++)
            {
                var segmentA = new Vector2(allPoints[a].x, allPoints[a].y);
                var segmentB = new Vector2(allPoints[a + 1].x, allPoints[a + 1].y);

                if (RectUtils.IntersectsSegment(rect, segmentA, segmentB))
                    return true;
            }

            return false;
        }

        private void PointsChanged()
        {
            m_TangentsDirty = true;
            layout = new Rect(Vector2.Min(m_To, m_From), new Vector2(Mathf.Abs(m_From.x - m_To.x), Mathf.Abs(m_From.y - m_To.y)));
            Dirty(ChangeType.Repaint);
        }

        private void CacheLineData()
        {
            // Don't store the values in the actual `to` and `from` member as this will trigger infinite repaints if to and from are switched.
            Vector2 usedTo = to;
            Vector2 usedFrom = from;
            if (orientation == Orientation.Horizontal && from.x < to.x)
            {
                usedTo = from;
                usedFrom = to;
            }

            if (lineType == LineType.StraightLine)
            {
                if (m_ControlPoints == null || m_ControlPoints.Length != 2)
                    m_ControlPoints = new Vector3[2];

                m_ControlPoints[0] = usedTo;
                m_ControlPoints[1] = usedFrom;
                m_RenderPoints = m_ControlPoints;
                return;
            }

            if (m_ControlPoints == null || m_ControlPoints.Length != 4)
                m_ControlPoints = new Vector3[4];

            m_ControlPoints[0] = usedTo;
            m_ControlPoints[3] = usedFrom;

            switch (lineType)
            {
                case LineType.Bezier:
                    const float minTangent = 30;

                    float weight = .5f;
                    float weight2 = 1 - weight;
                    float y = 0;
                    float cleverness = Mathf.Clamp01(((usedTo - usedFrom).magnitude - 10) / 50);

                    if (orientation == Orientation.Horizontal)
                    {
                        m_ControlPoints[1] = usedTo + new Vector2((usedFrom.x - usedTo.x) * weight + minTangent, y) * cleverness;
                        m_ControlPoints[2] = usedFrom + new Vector2((usedFrom.x - usedTo.x) * -weight2 - minTangent, -y) * cleverness;
                    }
                    else
                    {
                        float tangentSize = usedTo.y - usedFrom.y + 100.0f;
                        tangentSize = Mathf.Min((usedTo - usedFrom).magnitude, tangentSize);
                        if (tangentSize < 0.0f)
                            tangentSize = -tangentSize;

                        m_ControlPoints[1] = usedTo + new Vector2(0, tangentSize * -0.5f);
                        m_ControlPoints[2] = usedFrom + new Vector2(0, tangentSize * 0.5f);
                    }

                    m_RenderPoints = Handles.MakeBezierPoints(m_ControlPoints[0], m_ControlPoints[3], m_ControlPoints[1], m_ControlPoints[2], 20);
                    break;

                case LineType.PolyLine:
                    if (orientation == Orientation.Horizontal)
                    {
                        m_ControlPoints[2] = new Vector2((usedTo.x + usedFrom.x) / 2, usedFrom.y);
                        m_ControlPoints[1] = new Vector2((usedTo.x + usedFrom.x) / 2, usedTo.y);
                    }
                    else
                    {
                        m_ControlPoints[2] = new Vector2(usedFrom.x, (usedTo.y + usedFrom.y) / 2);
                        m_ControlPoints[1] = new Vector2(usedTo.x, (usedTo.y + usedFrom.y) / 2);
                    }

                    m_RenderPoints = m_ControlPoints;
                    break;

                case LineType.StraightLine:
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Unsupported LineType: " + lineType);
            }
        }
    }
}
