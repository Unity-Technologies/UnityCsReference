// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;

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
        public EdgeControl()
        {
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
        }

        private bool m_ControlPointsDirty = true;
        private bool m_RenderPointsDirty = true;
        private bool m_MeshDirty = true;

        Mesh m_Mesh;
        public const float k_MinEdgeWidth = 1.75f;

        Color m_InputColor = Color.grey;
        Color m_OutputColor = Color.grey;

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
        public Color edgeColor
        {
            get { return m_InputColor; }
            set
            {
                if (m_InputColor == value && m_OutputColor == value)
                    return;
                m_InputColor = value;
                m_OutputColor = value;
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
                UpdateLayout(); // The layout depends on the edges width
                Dirty(ChangeType.Repaint);
            }
        }

        private float m_InterceptWidth = 5;
        public float interceptWidth
        {
            get { return m_InterceptWidth; }
            set { m_InterceptWidth = value; }
        }

        // The start of the edge in graph coordinates.
        private Vector2 m_From;
        public Vector2 from
        {
            get { return m_From; }
            set
            {
                if ((m_From - value).sqrMagnitude > 0.25f)
                {
                    m_From = value;
                    PointsChanged();
                }
            }
        }


        // The end of the edge in graph coordinates.
        private Vector2 m_To;
        public Vector2 to
        {
            get { return m_To; }
            set
            {
                if ((m_To - value).sqrMagnitude > 0.25f)
                {
                    m_To = value;
                    PointsChanged();
                }
            }
        }


        // The control points in graph coordinates.
        private Vector2[] m_ControlPoints;
        public Vector2[] controlPoints
        {
            get
            {
                return m_ControlPoints;
            }
        }
        public Vector2[] renderPoints
        {
            get
            {
                UpdateRenderPoints();
                return m_RenderPoints.ToArray();
            }
        }

        protected virtual void DrawEndpoint(Vector2 pos, bool start)
        {
            Handles.DrawSolidDisc(pos, new Vector3(0.0f, 0.0f, -1.0f), capRadius);
        }

        public override void DoRepaint()
        {
            // Edges do NOT call base.DoRepaint. It would create a visual artifact.
            Color oldColor = Handles.color;
            DrawEdge();

            Handles.color = startCapColor;
            DrawEndpoint(parent.ChangeCoordinatesTo(this, from), true);
            Handles.color = endCapColor;
            DrawEndpoint(parent.ChangeCoordinatesTo(this, to), true);
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

            Vector2[] allPoints = renderPoints;

            float minDistance = Mathf.Infinity;
            for (var i = 0; i < allPoints.Length; i++)
            {
                Vector2 currentPoint = allPoints[i];
                float distance = Vector2.Distance(currentPoint, localPoint);
                minDistance = Mathf.Min(minDistance, distance);
                if (minDistance < interceptWidth)
                    return true;
            }

            return false;
        }

        public override bool Overlaps(Rect rect)
        {
            for (int a = 0; a < m_RenderPoints.Count - 1; a++)
            {
                var segmentA = new Vector2(m_RenderPoints[a].x, m_RenderPoints[a].y);
                var segmentB = new Vector2(m_RenderPoints[a + 1].x, m_RenderPoints[a + 1].y);

                if (RectUtils.IntersectsSegment(rect, segmentA, segmentB))
                    return true;
            }

            return false;
        }

        protected virtual void PointsChanged()
        {
            m_ControlPointsDirty = true;
            Dirty(ChangeType.Repaint);
        }

        // The points that will be rendered. Expressed in coordinates local to the element.
        List<Vector2> m_RenderPoints = new List<Vector2>();

        public virtual void UpdateLayout()
        {
            if (parent == null) return;
            if (m_ControlPointsDirty == false) return;


            ComputeControlPoints(); // Computes the control points in parent ( graph ) coordinates
            ComputeLayout(); // Update the element layout based on the control points.
            m_ControlPointsDirty = false;
        }

        protected virtual void UpdateRenderPoints()
        {
            ComputeControlPoints(); // This should have been updated before : make sure anyway.

            if (m_RenderPointsDirty == false && m_ControlPoints != null)
            {
                return;
            }

            m_RenderPointsDirty = false;
            m_MeshDirty = true;

            m_RenderPoints.Clear();
            switch (lineType)
            {
                case LineType.Bezier:


                    Vector3 start = controlPoints[0];
                    Vector3 tStart = controlPoints[1];
                    Vector3 end = controlPoints[3];
                    Vector3 tEnd = controlPoints[2];

                    BezierSubdiv.GetBezierSubDiv(m_RenderPoints, parent.ChangeCoordinatesTo(this, start), parent.ChangeCoordinatesTo(this, end), parent.ChangeCoordinatesTo(this, tStart), parent.ChangeCoordinatesTo(this, tEnd));

                    break;
                case LineType.PolyLine:
                    m_RenderPoints.Add(parent.ChangeCoordinatesTo(this, m_ControlPoints[0]));
                    m_RenderPoints.Add(parent.ChangeCoordinatesTo(this, m_ControlPoints[1]));
                    m_RenderPoints.Add(parent.ChangeCoordinatesTo(this, m_ControlPoints[2]));
                    m_RenderPoints.Add(parent.ChangeCoordinatesTo(this, m_ControlPoints[3]));
                    break;

                case LineType.StraightLine:
                    m_RenderPoints.Add(parent.ChangeCoordinatesTo(this, m_ControlPoints[0]));
                    m_RenderPoints.Add(parent.ChangeCoordinatesTo(this, m_ControlPoints[1]));
                    break;
            }
        }

        protected virtual void ComputeControlPoints()
        {
            if (m_ControlPointsDirty == false) return;

            Vector2 usedTo = to;
            Vector2 usedFrom = from;

            if (orientation == Orientation.Horizontal && from.x < to.x)
            {
                Vector2 tmp = usedTo;
                usedTo = usedFrom;
                usedFrom = tmp;
            }

            if (lineType == LineType.StraightLine)
            {
                if (m_ControlPoints == null || m_ControlPoints.Length != 2)
                    m_ControlPoints = new Vector2[2];

                m_ControlPoints[0] = usedTo;
                m_ControlPoints[1] = usedFrom;

                return;
            }

            if (m_ControlPoints == null || m_ControlPoints.Length != 4)
                m_ControlPoints = new Vector2[4];

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
                    break;

                case LineType.StraightLine:

                    break;

                default:
                    throw new ArgumentOutOfRangeException("Unsupported LineType: " + lineType);
            }
        }

        void ComputeLayout()
        {
            Vector2 to = m_ControlPoints[m_ControlPoints.Length - 1];
            Vector2 from = m_ControlPoints[0];

            Rect rect = new Rect(Vector2.Min(to, from), new Vector2(Mathf.Abs(from.x - to.x), Mathf.Abs(from.y - to.y)));

            // Make sure any control points (including tangents, are included in the rect)
            for (int i = 1; i < m_ControlPoints.Length - 1; ++i)
            {
                if (!rect.Contains(m_ControlPoints[i]))
                {
                    Vector2 pt = m_ControlPoints[i];
                    rect.xMin = Math.Min(rect.xMin, pt.x);
                    rect.yMin = Math.Min(rect.yMin, pt.y);
                    rect.xMax = Math.Max(rect.xMax, pt.x);
                    rect.yMax = Math.Max(rect.yMax, pt.y);
                }
            }

            GraphView graphView = GetFirstAncestorOfType<GraphView>();

            if (graphView != null)
            {
                //Make sure that we have the place to display Edges with EdgeControl.k_MinEdgeWidth at the lowest level of zoom.
                float margin = Mathf.Max(edgeWidth * 0.5f + 1, EdgeControl.k_MinEdgeWidth / graphView.minScale.x);

                rect.xMin -= margin;
                rect.yMin -= margin;
                rect.width += margin * 2;
                rect.height += margin * 2;

                if (layout != rect)
                {
                    layout = rect;
                    m_RenderPointsDirty = true;
                }
            }
        }

        public Color inputColor
        {
            get
            {
                return m_InputColor;
            }
            set
            {
                if (m_InputColor != value)
                {
                    m_InputColor = value;
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        public Color outputColor
        {
            get
            {
                return m_OutputColor;
            }
            set
            {
                if (m_OutputColor != value)
                {
                    m_OutputColor = value;
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        static Material s_LineMat;

        static Material lineMat
        {
            get
            {
                if (s_LineMat == null)
                    s_LineMat = new Material(EditorGUIUtility.LoadRequired("GraphView/AAEdge.shader") as Shader);
                return s_LineMat;
            }
        }

        protected virtual void DrawEdge()
        {
            if (edgeWidth <= 0)
                return;

            UpdateRenderPoints();

            Vector2[] points = controlPoints;

            Color inputColor = this.inputColor;
            Color outputColor = this.outputColor;

            GraphView view = this.GetFirstAncestorOfType<GraphView>();

            float realWidth = edgeWidth;
            if (realWidth * view.scale < k_MinEdgeWidth)
            {
                realWidth = k_MinEdgeWidth / view.scale;

                // make up for bigger edge by fading it.
                inputColor.a = outputColor.a = edgeWidth / realWidth;
            }

            if (m_MeshDirty || m_Mesh == null)
            {
                m_MeshDirty = false;

                int cpt = m_RenderPoints.Count;

                float polyLineLength = 0;

                for (int i = 1; i < cpt; ++i)
                {
                    polyLineLength += (m_RenderPoints[i - 1] - m_RenderPoints[i]).magnitude;
                }

                if (m_Mesh == null)
                {
                    m_Mesh = new Mesh();
                    m_Mesh.hideFlags = HideFlags.HideAndDontSave;
                }

                Vector3[] vertices = m_Mesh.vertices;
                Vector2[] uvs = m_Mesh.uv;
                Vector3[] normals = m_Mesh.normals;
                bool newIndices = false;
                int wantedLength = (cpt) * 2;
                if (vertices == null || vertices.Length != wantedLength)
                {
                    vertices = new Vector3[wantedLength];
                    uvs = new Vector2[wantedLength];
                    normals = new Vector3[wantedLength];
                    newIndices = true;
                    m_Mesh.triangles = new int[] {};
                }

                float halfWidth = edgeWidth * 0.5f;

                float vertexHalfWidth = halfWidth + 2;

                float currentLength = 0;

                for (int i = 0; i < cpt; ++i)
                {
                    Vector2 dir;
                    if (i > 0 && i < cpt - 1)
                    {
                        dir = (m_RenderPoints[i] - m_RenderPoints[i - 1]).normalized + (m_RenderPoints[i + 1] - m_RenderPoints[i]).normalized;
                        dir.Normalize();
                    }
                    else if (i > 0)
                    {
                        dir = (m_RenderPoints[i] - m_RenderPoints[i - 1]).normalized;
                    }
                    else
                    {
                        dir = (m_RenderPoints[i + 1] - m_RenderPoints[i]).normalized;
                    }

                    Vector2 norm = new Vector3(dir.y, -dir.x, 0);

                    Vector2 border = -norm * vertexHalfWidth;

                    uvs[i * 2] = new Vector2(-vertexHalfWidth, halfWidth);
                    vertices[i * 2] = m_RenderPoints[i];
                    // normals store the Vector2 normal in x,y and the progress in the edge in z ( which drive the gradient ).
                    normals[i * 2] = new Vector3(-border.x, -border.y, currentLength / polyLineLength);

                    uvs[i * 2 + 1] = new Vector2(vertexHalfWidth, halfWidth);
                    vertices[i * 2 + 1] = m_RenderPoints[i];
                    normals[i * 2 + 1] = new Vector3(border.x, border.y, currentLength / polyLineLength);

                    if (i < cpt - 2)
                    {
                        currentLength += (m_RenderPoints[i + 1] - m_RenderPoints[i]).magnitude;
                    }
                    else
                    {
                        currentLength = polyLineLength;
                    }
                }

                m_Mesh.vertices = vertices;
                m_Mesh.normals = normals;
                m_Mesh.uv = uvs;

                if (newIndices)
                {
                    //fill triangle indices as it is a triangle strip
                    int[] indices = new int[(wantedLength - 2) * 3];

                    for (int i = 0; i < wantedLength - 2; ++i)
                    {
                        if ((i % 2) == 0)
                        {
                            indices[i * 3] = i;
                            indices[i * 3 + 1] = i + 1;
                            indices[i * 3 + 2] = i + 2;
                        }
                        else
                        {
                            indices[i * 3] = i + 1;
                            indices[i * 3 + 1] = i;
                            indices[i * 3 + 2] = i + 2;
                        }
                    }

                    m_Mesh.triangles = indices;
                }

                m_Mesh.RecalculateBounds();
            }

            // Send the view zoom factor so that the antialias width do not grow when zooming in.
            lineMat.SetFloat("_ZoomFactor", view.scale * realWidth / edgeWidth * EditorGUIUtility.pixelsPerPoint);

            // Send the view zoom correction so that the vertex shader can scale the edge triangles when below m_MinWidth.
            lineMat.SetFloat("_ZoomCorrection", realWidth / edgeWidth);

            lineMat.SetColor("_InputColor", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? inputColor.gamma : inputColor);
            lineMat.SetColor("_OutputColor", (QualitySettings.activeColorSpace == ColorSpace.Linear) ? outputColor.gamma : outputColor);
            lineMat.SetPass(0);

            Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (m_Mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Mesh);
                m_Mesh = null;
            }
        }

        class BezierSubdiv
        {
            public static void GetBezierSubDiv(List<Vector2> points, Vector2 start, Vector2 end, Vector2 tStart, Vector2 tEnd)
            {
                points.Clear();

                points.Add(start);

                AddBezierRecurse(points, start, tStart, tEnd, end, 0);

                points.Add(end);
            }

            const float k_DistanceTolerance = 0.5f;

            const int k_MaxRecursion = 20;

            static void AddBezierRecurse(List<Vector2> points, Vector2 start, Vector2 tangentStart, Vector2 tangentEnd, Vector2 end, int level)
            {
                // Prevention of infinite recursion.
                if (level > k_MaxRecursion)
                {
                    return;
                }

                float x1 = start.x;
                float y1 = start.y;
                float x2 = tangentStart.x;
                float y2 = tangentStart.y;
                float x3 = tangentEnd.x;
                float y3 = tangentEnd.y;
                float x4 = end.x;
                float y4 = end.y;
                // Calculate all the mid-points of the line segments
                //----------------------
                float x12 = (x1 + x2) / 2;
                float y12 = (y1 + y2) / 2;
                float x23 = (x2 + x3) / 2;
                float y23 = (y2 + y3) / 2;
                float x34 = (x3 + x4) / 2;
                float y34 = (y3 + y4) / 2;
                float x123 = (x12 + x23) / 2;
                float y123 = (y12 + y23) / 2;
                float x234 = (x23 + x34) / 2;
                float y234 = (y23 + y34) / 2;
                float x1234 = (x123 + x234) / 2;
                float y1234 = (y123 + y234) / 2;

                if (level > 0) // Enforce subdivision first time
                {
                    // Try to approximate the full cubic curve by a single straight line
                    //------------------
                    float dx = x4 - x1;
                    float dy = y4 - y1;

                    float d2 = Mathf.Abs(((x2 - x4) * dy - (y2 - y4) * dx));
                    float d3 = Mathf.Abs(((x3 - x4) * dy - (y3 - y4) * dx));

                    if ((d2 + d3) * (d2 + d3) <= k_DistanceTolerance * (dx * dx + dy * dy))
                    {
                        points.Add(new Vector2(x1234, y1234));
                        return;
                    }
                }

                // Continue subdivision
                //----------------------
                AddBezierRecurse(points, new Vector2(x1, y1), new Vector2(x12, y12), new Vector2(x123, y123), new Vector2(x1234, y1234), level + 1);
                AddBezierRecurse(points, new Vector2(x1234, y1234), new Vector2(x234, y234), new Vector2(x34, y34), new Vector2(x4, y4), level + 1);
            }
        }
    }
}
