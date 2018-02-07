// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using System.Collections.Generic;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class EdgeControl : VisualElement
    {
        private struct EdgeCornerSweepValues
        {
            public Vector2 circleCenter;
            public double sweepAngle;
            public double startAngle;
            public double endAngle;
            public Vector2 crossPoint1;
            public Vector2 crossPoint2;
            public float radius;
        }

        private VisualElement m_FromCap;
        private VisualElement m_ToCap;
        private GraphView m_GraphView;

        public EdgeControl()
        {
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            m_FromCap = new VisualElement();
            m_FromCap.AddToClassList("edgeCap");
            m_FromCap.pseudoStates |= PseudoStates.Invisible;
            Add(m_FromCap);

            m_ToCap = new VisualElement();
            m_ToCap.AddToClassList("edgeCap");
            m_ToCap.pseudoStates |= PseudoStates.Invisible;
            Add(m_ToCap);

            m_DrawFromCap = false;
            m_DrawToCap = false;
        }

        private bool m_ControlPointsDirty = true;
        private bool m_RenderPointsDirty = true;
        private bool m_MeshDirty = true;

        Mesh m_Mesh;
        public const float k_MinEdgeWidth = 1.75f;

        private const float k_EdgeLengthFromPort = 12.0f;
        private const float k_EdgeTurnDiameter = 16.0f;
        private const float k_EdgeSweepResampleRatio = 4.0f;
        private const int k_EdgeStraightLineSegmentDivisor = 5;

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

        [Obsolete("Use inputEdgeColor and/or outputEdgeColor")]
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

        Color m_InputColor = Color.grey;
        public Color inputColor
        {
            get { return m_InputColor; }
            set
            {
                if (m_InputColor != value)
                {
                    m_InputColor = value;
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        Color m_OutputColor = Color.grey;
        public Color outputColor
        {
            get { return m_OutputColor; }
            set
            {
                if (m_OutputColor != value)
                {
                    m_OutputColor = value;
                    Dirty(ChangeType.Repaint);
                }
            }
        }

        private Color m_FromCapColor;
        public Color fromCapColor
        {
            get { return m_FromCapColor; }
            set
            {
                if (m_FromCapColor == value)
                    return;
                m_FromCapColor = value;
                m_FromCap.style.backgroundColor = m_FromCapColor;
                Dirty(ChangeType.Repaint);
            }
        }

        private Color m_ToCapColor;
        public Color toCapColor
        {
            get { return m_ToCapColor; }
            set
            {
                if (m_ToCapColor == value)
                    return;
                m_ToCapColor = value;
                m_ToCap.style.backgroundColor = m_ToCapColor;
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
                m_MeshDirty = true;
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

        private bool m_DrawFromCap;
        public bool drawFromCap
        {
            get { return m_DrawFromCap; }
            set
            {
                if (value == m_DrawFromCap)
                    return;
                m_DrawFromCap = value;
                if (!m_DrawFromCap)
                    m_FromCap.pseudoStates |= PseudoStates.Invisible;
                else
                    m_FromCap.pseudoStates &= ~PseudoStates.Invisible;
                Dirty(ChangeType.Layout);
            }
        }

        private bool m_DrawToCap;
        public bool drawToCap
        {
            get { return m_DrawToCap; }
            set
            {
                if (value == m_DrawToCap)
                    return;
                m_DrawToCap = value;
                if (!m_DrawToCap)
                    m_ToCap.pseudoStates |= PseudoStates.Invisible;
                else
                    m_ToCap.pseudoStates &= ~PseudoStates.Invisible;
                Dirty(ChangeType.Layout);
            }
        }

        public override void DoRepaint()
        {
            m_FromCap.layout = new Rect(parent.ChangeCoordinatesTo(this, m_From) - (m_FromCap.layout.size / 2), m_FromCap.layout.size);
            m_ToCap.layout = new Rect(parent.ChangeCoordinatesTo(this, m_To) - (m_ToCap.layout.size / 2), m_ToCap.layout.size);

            // Edges do NOT call base.DoRepaint. It would create a visual artifact.
            DrawEdge();
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

            for (var i = 0; i < allPoints.Length - 1; i++)
            {
                Vector2 currentPoint = allPoints[i];
                Vector2 nextPoint = allPoints[i + 1];
                float distance = Vector2.Distance(currentPoint, localPoint);
                float distanceNext = Vector2.Distance(nextPoint, localPoint);
                float distanceLine = Vector2.Distance(currentPoint, nextPoint);
                if (distance < distanceLine && distanceNext < distanceLine) // the point is somewhere between the two points
                {
                    //https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
                    if (Mathf.Abs((nextPoint.y - currentPoint.y) * localPoint.x -
                            (nextPoint.x - currentPoint.x) * localPoint.y + nextPoint.x * currentPoint.y -
                            nextPoint.y * currentPoint.x) / distanceLine < interceptWidth)
                    {
                        return true;
                    }
                }
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

        void RenderStraightLines(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float safeSpan = orientation == Orientation.Horizontal
                ? Mathf.Abs((p1.x + k_EdgeLengthFromPort) - (p4.x - k_EdgeLengthFromPort))
                : Mathf.Abs((p1.y + k_EdgeLengthFromPort) - (p4.y - k_EdgeLengthFromPort));

            float safeSpan3 = safeSpan / k_EdgeStraightLineSegmentDivisor;
            float nodeToP2Dist = Mathf.Min(safeSpan3, k_EdgeTurnDiameter);
            nodeToP2Dist = Mathf.Max(0, nodeToP2Dist);

            var offset = orientation == Orientation.Horizontal
                ? new Vector2(k_EdgeTurnDiameter - nodeToP2Dist, 0)
                : new Vector2(0, k_EdgeTurnDiameter - nodeToP2Dist);

            m_RenderPoints.Add(p1);
            m_RenderPoints.Add(p2 - offset);
            m_RenderPoints.Add(p3 + offset);
            m_RenderPoints.Add(p4);
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

            float diameter = k_EdgeTurnDiameter;

            Vector2 p1 = parent.ChangeCoordinatesTo(this, m_ControlPoints[0]);
            Vector2 p2 = parent.ChangeCoordinatesTo(this, m_ControlPoints[1]);
            Vector2 p3 = parent.ChangeCoordinatesTo(this, m_ControlPoints[2]);
            Vector2 p4 = parent.ChangeCoordinatesTo(this, m_ControlPoints[3]);

            // We have to handle a special case of the edge when it is a straight line, but not
            // when going backwards in space (where the start point is in front in y to the end point).
            // We do this by turning the line into 3 linear segments with no curves. This also
            // avoids possible NANs in later angle calculations.
            if ((orientation == Orientation.Horizontal && Mathf.Abs(p1.y - p4.y) < 2 && p1.x + k_EdgeLengthFromPort < p4.x - k_EdgeLengthFromPort) ||
                (orientation == Orientation.Vertical && Mathf.Abs(p1.x - p4.x) < 2 && p1.y + k_EdgeLengthFromPort < p4.y - k_EdgeLengthFromPort))
            {
                RenderStraightLines(p1, p2, p3, p4);

                return;
            }


            EdgeCornerSweepValues corner1 = GetCornerSweepValues(p1, p2, p3, diameter, Direction.Output);
            EdgeCornerSweepValues corner2 = GetCornerSweepValues(p2, p3, p4, diameter, Direction.Input);

            if (!ValidateCornerSweepValues(ref corner1, ref corner2))
            {
                RenderStraightLines(p1, p2, p3, p4);

                return;
            }

            m_RenderPoints.Add(p1);

            GetRoundedCornerPoints(m_RenderPoints, corner1, Direction.Output);
            GetRoundedCornerPoints(m_RenderPoints, corner2, Direction.Input);

            m_RenderPoints.Add(p4);
        }

        private bool ValidateCornerSweepValues(ref EdgeCornerSweepValues corner1, ref EdgeCornerSweepValues corner2)
        {
            // Get the midpoint between the two corner circle centers.
            Vector2 circlesMidpoint = (corner1.circleCenter + corner2.circleCenter) / 2;

            // Find the angle to the corner circles midpoint so we can compare it to the sweep angles of each corner.
            Vector2 p2CenterToCross1 = corner1.circleCenter - corner1.crossPoint1;
            Vector2 p2CenterToCirclesMid = corner1.circleCenter - circlesMidpoint;
            double angleToCirclesMid = orientation == Orientation.Horizontal
                ? Math.Atan2(p2CenterToCross1.y, p2CenterToCross1.x) -
                Math.Atan2(p2CenterToCirclesMid.y, p2CenterToCirclesMid.x)
                : Math.Atan2(p2CenterToCross1.x, p2CenterToCross1.y) -
                Math.Atan2(p2CenterToCirclesMid.x, p2CenterToCirclesMid.y);

            if (double.IsNaN(angleToCirclesMid))
            {
                return false;
            }

            // We need the angle to the circles midpoint to match the turn direction of the first corner's sweep angle.
            angleToCirclesMid = Math.Sign(angleToCirclesMid) * 2 * Mathf.PI - angleToCirclesMid;
            if (Mathf.Abs((float)angleToCirclesMid) > 1.5 * Mathf.PI)
                angleToCirclesMid = -1 * Math.Sign(angleToCirclesMid) * 2 * Mathf.PI + angleToCirclesMid;

            // Calculate the maximum sweep angle so that both corner sweeps and with the tangents of the 2 circles meeting each other.
            float h = p2CenterToCirclesMid.magnitude;
            float p2AngleToMidTangent = Mathf.Acos(corner1.radius / h);
            float maxSweepAngle = Mathf.Abs((float)corner1.sweepAngle) - p2AngleToMidTangent * 2;

            // If the angle to the circles midpoint is within the sweep angle, we need to apply our maximum sweep angle
            // calculated above, otherwise the maximum sweep angle is irrelevant.
            if (Mathf.Abs((float)angleToCirclesMid) < Mathf.Abs((float)corner1.sweepAngle))
            {
                corner1.sweepAngle = Math.Sign(corner1.sweepAngle) * Mathf.Min(maxSweepAngle, Mathf.Abs((float)corner1.sweepAngle));
                corner2.sweepAngle = Math.Sign(corner2.sweepAngle) * Mathf.Min(maxSweepAngle, Mathf.Abs((float)corner2.sweepAngle));
            }

            return true;
        }

        private EdgeCornerSweepValues GetCornerSweepValues(
            Vector2 p1, Vector2 cornerPoint, Vector2 p2, float diameter, Direction closestPortDirection)
        {
            EdgeCornerSweepValues corner = new EdgeCornerSweepValues();

            // Calculate initial radius. This radius can change depending on the sharpness of the corner.
            corner.radius = diameter / 2;

            // Calculate vectors from p1 to cornerPoint.
            Vector2 d1Corner = (cornerPoint - p1).normalized;
            Vector2 d1 = d1Corner * diameter;
            float dx1 = d1.x;
            float dy1 = d1.y;

            // Calculate vectors from p2 to cornerPoint.
            Vector2 d2Corner = (cornerPoint - p2).normalized;
            Vector2 d2 = d2Corner * diameter;
            float dx2 = d2.x;
            float dy2 = d2.y;

            // Calculate the angle of the corner (divided by 2).
            float angle = (float)(Math.Atan2(dy1, dx1) - Math.Atan2(dy2, dx2)) / 2;

            // Calculate the length of the segment between the cornerPoint and where
            // the corner circle with given radius meets the line.
            float tan = (float)Math.Abs(Math.Tan(angle));
            float segment = corner.radius / tan;

            // If the segment is larger than the diameter, we need to cap the segment
            // to the diameter and reduce the radius to match the segment. This is what
            // makes the corner turn radii get smaller as the edge corners get tighter.
            if (segment > diameter)
            {
                segment = diameter;
                corner.radius = diameter * tan;
            }

            // Calculate both cross points (where the circle touches the p1-cornerPoint line
            // and the p2-cornerPoint line).
            corner.crossPoint1 = cornerPoint - (d1Corner * segment);
            corner.crossPoint2 = cornerPoint - (d2Corner * segment);

            // Calculation of the coordinates of the circle center.
            corner.circleCenter = GetCornerCircleCenter(cornerPoint, corner.crossPoint1, corner.crossPoint2, segment, corner.radius);

            // Calculate the starting and ending angles.
            corner.startAngle = Math.Atan2(corner.crossPoint1.y - corner.circleCenter.y, corner.crossPoint1.x - corner.circleCenter.x);
            corner.endAngle = Math.Atan2(corner.crossPoint2.y - corner.circleCenter.y, corner.crossPoint2.x - corner.circleCenter.x);

            // Get the full sweep angle from the starting and ending angles.
            corner.sweepAngle = corner.endAngle - corner.startAngle;

            // If we are computing the second corner (into the input port), we want to start
            // the sweep going backwards.
            if (closestPortDirection == Direction.Input)
            {
                double endAngle = corner.endAngle;
                corner.endAngle = corner.startAngle;
                corner.startAngle = endAngle;
            }

            // Validate the sweep angle so it turns into the correct direction.
            if (corner.sweepAngle > Math.PI)
                corner.sweepAngle = -2 * Math.PI + corner.sweepAngle;
            else if (corner.sweepAngle < -Math.PI)
                corner.sweepAngle = 2 * Math.PI + corner.sweepAngle;

            return corner;
        }

        private Vector2 GetCornerCircleCenter(Vector2 cornerPoint, Vector2 crossPoint1, Vector2 crossPoint2, float segment, float radius)
        {
            float dx = cornerPoint.x * 2 - crossPoint1.x - crossPoint2.x;
            float dy = cornerPoint.y * 2 - crossPoint1.y - crossPoint2.y;

            var cornerToCenterVector = new Vector2(dx, dy);

            float L = cornerToCenterVector.magnitude;
            float d = new Vector2(segment, radius).magnitude;
            float factor = d / L;

            return new Vector2(cornerPoint.x - cornerToCenterVector.x * factor, cornerPoint.y - cornerToCenterVector.y * factor);
        }

        private void GetRoundedCornerPoints(List<Vector2> points, EdgeCornerSweepValues corner, Direction closestPortDirection)
        {
            // Calculate the number of points that will sample the arc from the sweep angle.
            int pointsCount = Mathf.CeilToInt((float)Math.Abs(corner.sweepAngle * k_EdgeSweepResampleRatio));
            int sign = Math.Sign(corner.sweepAngle);
            bool backwards = (closestPortDirection == Direction.Input);

            for (int i = 0; i < pointsCount; ++i)
            {
                // If we are computing the second corner (into the input port), the sweep is going backwards
                // but we still need to add the points to the list in the correct order.
                float sweepIndex = backwards ? i - pointsCount : i;

                double sweepedAngle = corner.startAngle + sign * sweepIndex / k_EdgeSweepResampleRatio;

                var pointX = (float)(corner.circleCenter.x + Math.Cos(sweepedAngle) * corner.radius);
                var pointY = (float)(corner.circleCenter.y + Math.Sin(sweepedAngle) * corner.radius);

                // Check if we overlap the previous point. If we do, we skip this point so that we
                // don't cause the edge polygons to twist.
                if (i == 0 && backwards)
                {
                    if (orientation == Orientation.Horizontal)
                    {
                        if (corner.sweepAngle < 0 && points[points.Count - 1].y > pointY)
                            continue;
                        else if (corner.sweepAngle >= 0 && points[points.Count - 1].y < pointY)
                            continue;
                    }
                    else
                    {
                        if (corner.sweepAngle < 0 && points[points.Count - 1].x < pointX)
                            continue;
                        else if (corner.sweepAngle >= 0 && points[points.Count - 1].x > pointX)
                            continue;
                    }
                }

                points.Add(new Vector2(pointX, pointY));
            }
        }

        protected virtual void ComputeControlPoints()
        {
            if (m_ControlPointsDirty == false) return;

            float offset = k_EdgeLengthFromPort + k_EdgeTurnDiameter;

            // This is to ensure we don't have the edge extending
            // left and right by the offset right when the `from`
            // and `to` are on top of each other.
            float fromToDistance = (to - from).magnitude;
            offset = Mathf.Min(offset, fromToDistance * 2);
            offset = Mathf.Max(offset, k_EdgeTurnDiameter);

            if (m_ControlPoints == null || m_ControlPoints.Length != 4)
                m_ControlPoints = new Vector2[4];

            m_ControlPoints[0] = from;

            if (orientation == Orientation.Horizontal)
            {
                m_ControlPoints[1] = new Vector2(from.x + offset, from.y);
                m_ControlPoints[2] = new Vector2(to.x - offset, to.y);
            }
            else
            {
                m_ControlPoints[1] = new Vector2(from.x, from.y + offset);
                m_ControlPoints[2] = new Vector2(to.x, to.y - offset);
            }

            m_ControlPoints[3] = to;
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

            if (m_GraphView == null)
            {
                m_GraphView = GetFirstAncestorOfType<GraphView>();
            }

            //Make sure that we have the place to display Edges with EdgeControl.k_MinEdgeWidth at the lowest level of zoom.
            float margin = Mathf.Max(edgeWidth * 0.5f + 1, EdgeControl.k_MinEdgeWidth / m_GraphView.minScale);

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

            float realWidth = edgeWidth;
            if (realWidth * m_GraphView.scale < k_MinEdgeWidth)
            {
                realWidth = k_MinEdgeWidth / m_GraphView.scale;

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
            lineMat.SetFloat("_ZoomFactor", m_GraphView.scale * realWidth / edgeWidth * EditorGUIUtility.pixelsPerPoint);

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
    }
}
