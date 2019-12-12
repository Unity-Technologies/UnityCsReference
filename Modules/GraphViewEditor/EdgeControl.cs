// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Unity.Collections;
using UnityEngine.UIElements.UIR;

namespace UnityEditor.Experimental.GraphView
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

        private static Stack<VisualElement> capPool = new Stack<VisualElement>();

        private static VisualElement GetCap()
        {
            VisualElement result = null;
            if (capPool.Count > 0)
            {
                result = capPool.Pop();
            }
            else
            {
                result = new VisualElement();
                result.AddToClassList("edgeCap");
            }

            return result;
        }

        private static void RecycleCap(VisualElement cap)
        {
            capPool.Push(cap);
        }

        public EdgeControl()
        {
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            m_FromCap = null;
            m_ToCap = null;

            pickingMode = PickingMode.Ignore;

            generateVisualContent += OnGenerateVisualContent;
        }

        private bool m_ControlPointsDirty = true;
        private bool m_RenderPointsDirty = true;

        Mesh m_Mesh;
        public const float k_MinEdgeWidth = 1.75f;

        private const float k_EdgeLengthFromPort = 12.0f;
        private const float k_EdgeTurnDiameter = 16.0f;
        private const float k_EdgeSweepResampleRatio = 4.0f;
        private const int k_EdgeStraightLineSegmentDivisor = 5;

        private Orientation m_InputOrientation;
        public Orientation inputOrientation
        {
            get { return m_InputOrientation; }
            set
            {
                if (m_InputOrientation == value)
                    return;
                m_InputOrientation = value;
                MarkDirtyRepaint();
            }
        }

        private Orientation m_OutputOrientation;
        public Orientation outputOrientation
        {
            get { return m_OutputOrientation; }
            set
            {
                if (m_OutputOrientation == value)
                    return;
                m_OutputOrientation = value;
                MarkDirtyRepaint();
            }
        }

        [Obsolete("Use inputColor and/or outputColor")]
        public Color edgeColor
        {
            get { return m_InputColor; }
            set
            {
                if (m_InputColor == value && m_OutputColor == value)
                    return;
                m_InputColor = value;
                m_OutputColor = value;
                MarkDirtyRepaint();
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
                    MarkDirtyRepaint();
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
                    MarkDirtyRepaint();
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

                if (m_FromCap != null)
                {
                    m_FromCap.style.backgroundColor = m_FromCapColor;
                }
                MarkDirtyRepaint();
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

                if (m_ToCap != null)
                {
                    m_ToCap.style.backgroundColor = m_ToCapColor;
                }
                MarkDirtyRepaint();
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
                MarkDirtyRepaint();
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
                MarkDirtyRepaint();
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


        public bool drawFromCap
        {
            get { return m_FromCap != null; }
            set
            {
                if (!value)
                {
                    if (m_FromCap != null)
                    {
                        m_FromCap.RemoveFromHierarchy();
                        RecycleCap(m_FromCap);
                        m_FromCap = null;
                    }
                }
                else
                {
                    if (m_FromCap == null)
                    {
                        m_FromCap = GetCap();
                        m_FromCap.style.backgroundColor = m_FromCapColor;
                        Add(m_FromCap);
                    }
                }
            }
        }

        public bool drawToCap
        {
            get { return m_ToCap != null; }
            set
            {
                if (!value)
                {
                    if (m_ToCap != null)
                    {
                        m_ToCap.RemoveFromHierarchy();
                        RecycleCap(m_ToCap);
                        m_ToCap = null;
                    }
                }
                else
                {
                    if (m_ToCap == null)
                    {
                        m_ToCap = GetCap();
                        m_ToCap.style.backgroundColor = m_ToCapColor;
                        Add(m_ToCap);
                    }
                }
            }
        }

        void UpdateEdgeCaps()
        {
            if (m_FromCap != null)
            {
                Vector2 size = m_FromCap.layout.size;
                if ((size.x > 0) && (size.y > 0))
                    m_FromCap.layout = new Rect(parent.ChangeCoordinatesTo(this, m_From) - (size / 2), size);
            }
            if (m_ToCap != null)
            {
                Vector2 size = m_ToCap.layout.size;
                if ((size.x > 0) && (size.y > 0))
                    m_ToCap.layout = new Rect(parent.ChangeCoordinatesTo(this, m_To) - (size / 2), size);
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawEdge");
            DrawEdge(mgc);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            Profiler.BeginSample("EdgeControl.ContainsPoint");

            if (!base.ContainsPoint(localPoint))
            {
                Profiler.EndSample();
                return false;
            }

            // bounding box check succeeded, do more fine grained check by measuring distance to bezier points
            // exclude endpoints

            float capMaxDist = 4 * capRadius * capRadius; //(2 * CapRadius)^2

            if ((from - localPoint).sqrMagnitude <= capMaxDist ||
                (to - localPoint).sqrMagnitude <= capMaxDist)
            {
                Profiler.EndSample();
                return false;
            }

            var allPoints = m_RenderPoints;

            if (allPoints.Count > 0)
            {
                //we use squareDistance to avoid sqrts
                float distance = (allPoints[0] - localPoint).sqrMagnitude;
                float interceptWidth2 = interceptWidth * interceptWidth;
                for (var i = 0; i < allPoints.Count - 1; i++)
                {
                    Vector2 currentPoint = allPoints[i];
                    Vector2 nextPoint = allPoints[i + 1];

                    Vector2 next2Current = nextPoint - currentPoint;
                    float distanceNext = (nextPoint - localPoint).sqrMagnitude;
                    float distanceLine = next2Current.sqrMagnitude;

                    // if the point is somewhere between the two points
                    if (distance < distanceLine && distanceNext < distanceLine)
                    {
                        //https://en.wikipedia.org/wiki/Distance_from_a_point_to_a_line
                        var d = next2Current.y * localPoint.x -
                            next2Current.x * localPoint.y + nextPoint.x * currentPoint.y -
                            nextPoint.y * currentPoint.x;
                        if (d * d < interceptWidth2 * distanceLine)
                        {
                            Profiler.EndSample();
                            return true;
                        }
                    }

                    distance = distanceNext;
                }
            }

            Profiler.EndSample();
            return false;
        }

        public override bool Overlaps(Rect rect)
        {
            if (base.Overlaps(rect))
            {
                for (int a = 0; a < m_RenderPoints.Count - 1; a++)
                {
                    if (RectUtils.IntersectsSegment(rect, m_RenderPoints[a], m_RenderPoints[a + 1]))
                        return true;
                }
            }

            return false;
        }

        protected virtual void PointsChanged()
        {
            m_ControlPointsDirty = true;
            MarkDirtyRepaint();
        }

        // The points that will be rendered. Expressed in coordinates local to the element.
        List<Vector2> m_RenderPoints = new List<Vector2>();

        static bool Approximately(Vector2 v1, Vector2 v2)
        {
            return Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y);
        }

        public virtual void UpdateLayout()
        {
            if (parent == null) return;
            if (m_ControlPointsDirty)
            {
                ComputeControlPoints(); // Computes the control points in parent ( graph ) coordinates
                ComputeLayout(); // Update the element layout based on the control points.
                m_ControlPointsDirty = false;
            }
            UpdateEdgeCaps();
            MarkDirtyRepaint();
        }

        private List<Vector2> lastLocalControlPoints = new List<Vector2>();

        void RenderStraightLines(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float safeSpan = outputOrientation == Orientation.Horizontal
                ? Mathf.Abs((p1.x + k_EdgeLengthFromPort) - (p4.x - k_EdgeLengthFromPort))
                : Mathf.Abs((p1.y + k_EdgeLengthFromPort) - (p4.y - k_EdgeLengthFromPort));

            float safeSpan3 = safeSpan / k_EdgeStraightLineSegmentDivisor;
            float nodeToP2Dist = Mathf.Min(safeSpan3, k_EdgeTurnDiameter);
            nodeToP2Dist = Mathf.Max(0, nodeToP2Dist);

            var offset = outputOrientation == Orientation.Horizontal
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

            Vector2 p1 = parent.ChangeCoordinatesTo(this, m_ControlPoints[0]);
            Vector2 p2 = parent.ChangeCoordinatesTo(this, m_ControlPoints[1]);
            Vector2 p3 = parent.ChangeCoordinatesTo(this, m_ControlPoints[2]);
            Vector2 p4 = parent.ChangeCoordinatesTo(this, m_ControlPoints[3]);

            // Only compute this when the "local" points have actually changed
            if (lastLocalControlPoints.Count == 4)
            {
                if (Approximately(p1, lastLocalControlPoints[0]) &&
                    Approximately(p2, lastLocalControlPoints[1]) &&
                    Approximately(p3, lastLocalControlPoints[2]) &&
                    Approximately(p4, lastLocalControlPoints[3]))
                {
                    m_RenderPointsDirty = false;
                    return;
                }
            }

            Profiler.BeginSample("EdgeControl.UpdateRenderPoints");
            lastLocalControlPoints.Clear();
            lastLocalControlPoints.Add(p1);
            lastLocalControlPoints.Add(p2);
            lastLocalControlPoints.Add(p3);
            lastLocalControlPoints.Add(p4);
            m_RenderPointsDirty = false;

            m_RenderPoints.Clear();

            float diameter = k_EdgeTurnDiameter;

            // We have to handle a special case of the edge when it is a straight line, but not
            // when going backwards in space (where the start point is in front in y to the end point).
            // We do this by turning the line into 3 linear segments with no curves. This also
            // avoids possible NANs in later angle calculations.
            bool sameOrientations = outputOrientation == inputOrientation;
            if (sameOrientations &&
                ((outputOrientation == Orientation.Horizontal && Mathf.Abs(p1.y - p4.y) < 2 && p1.x + k_EdgeLengthFromPort < p4.x - k_EdgeLengthFromPort) ||
                 (outputOrientation == Orientation.Vertical && Mathf.Abs(p1.x - p4.x) < 2 && p1.y + k_EdgeLengthFromPort < p4.y - k_EdgeLengthFromPort)))
            {
                RenderStraightLines(p1, p2, p3, p4);
                Profiler.EndSample();
                return;
            }

            bool renderBothCorners = true;

            EdgeCornerSweepValues corner1 = GetCornerSweepValues(p1, p2, p3, diameter, Direction.Output);
            EdgeCornerSweepValues corner2 = GetCornerSweepValues(p2, p3, p4, diameter, Direction.Input);

            if (!ValidateCornerSweepValues(ref corner1, ref corner2))
            {
                if (sameOrientations)
                {
                    RenderStraightLines(p1, p2, p3, p4);
                    Profiler.EndSample();
                    return;
                }

                renderBothCorners = false;

                //we try to do it with a single corner instead
                Vector2 px = (outputOrientation == Orientation.Horizontal) ? new Vector2(p4.x, p1.y) : new Vector2(p1.x, p4.y);

                corner1 = GetCornerSweepValues(p1, px, p4, diameter, Direction.Output);
            }

            m_RenderPoints.Add(p1);

            if (!sameOrientations && renderBothCorners)
            {
                //if the 2 corners or endpoints are too close, the corner sweep angle calculations can't handle different orientations
                float minDistance = 2 * diameter * diameter;
                if ((p3 - p2).sqrMagnitude < minDistance ||
                    (p4 - p1).sqrMagnitude < minDistance)
                {
                    Vector2 px = (p2 + p3) * 0.5f;
                    corner1 = GetCornerSweepValues(p1, px, p4, diameter, Direction.Output);
                    renderBothCorners = false;
                }
            }

            GetRoundedCornerPoints(m_RenderPoints, corner1, Direction.Output);
            if (renderBothCorners)
                GetRoundedCornerPoints(m_RenderPoints, corner2, Direction.Input);

            m_RenderPoints.Add(p4);
            Profiler.EndSample();
        }

        private bool ValidateCornerSweepValues(ref EdgeCornerSweepValues corner1, ref EdgeCornerSweepValues corner2)
        {
            // Get the midpoint between the two corner circle centers.
            Vector2 circlesMidpoint = (corner1.circleCenter + corner2.circleCenter) / 2;

            // Find the angle to the corner circles midpoint so we can compare it to the sweep angles of each corner.
            Vector2 p2CenterToCross1 = corner1.circleCenter - corner1.crossPoint1;
            Vector2 p2CenterToCirclesMid = corner1.circleCenter - circlesMidpoint;
            double angleToCirclesMid = outputOrientation == Orientation.Horizontal
                ? Math.Atan2(p2CenterToCross1.y, p2CenterToCross1.x) - Math.Atan2(p2CenterToCirclesMid.y, p2CenterToCirclesMid.x)
                : Math.Atan2(p2CenterToCross1.x, p2CenterToCross1.y) - Math.Atan2(p2CenterToCirclesMid.x, p2CenterToCirclesMid.y);

            if (double.IsNaN(angleToCirclesMid))
                return false;

            // We need the angle to the circles midpoint to match the turn direction of the first corner's sweep angle.
            angleToCirclesMid = Math.Sign(angleToCirclesMid) * 2 * Mathf.PI - angleToCirclesMid;
            if (Mathf.Abs((float)angleToCirclesMid) > 1.5 * Mathf.PI)
                angleToCirclesMid = -1 * Math.Sign(angleToCirclesMid) * 2 * Mathf.PI + angleToCirclesMid;

            // Calculate the maximum sweep angle so that both corner sweeps and with the tangents of the 2 circles meeting each other.
            float h = p2CenterToCirclesMid.magnitude;
            float p2AngleToMidTangent = Mathf.Acos(corner1.radius / h);

            if (double.IsNaN(p2AngleToMidTangent))
                return false;

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

            if (Mathf.Approximately(L, 0))
            {
                return cornerPoint;
            }

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
                    if (outputOrientation == Orientation.Horizontal)
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

        private void AssignControlPoint(ref Vector2 destination, Vector2 newValue)
        {
            if (!Approximately(destination, newValue))
            {
                destination = newValue;
                m_RenderPointsDirty = true;
            }
        }

        protected virtual void ComputeControlPoints()
        {
            if (m_ControlPointsDirty == false) return;

            Profiler.BeginSample("EdgeControl.ComputeControlPoints");

            float offset = k_EdgeLengthFromPort + k_EdgeTurnDiameter;

            // This is to ensure we don't have the edge extending
            // left and right by the offset right when the `from`
            // and `to` are on top of each other.
            float fromToDistance = (to - from).magnitude;
            offset = Mathf.Min(offset, fromToDistance * 2);
            offset = Mathf.Max(offset, k_EdgeTurnDiameter);

            if (m_ControlPoints == null || m_ControlPoints.Length != 4)
                m_ControlPoints = new Vector2[4];

            AssignControlPoint(ref m_ControlPoints[0], from);

            if (outputOrientation == Orientation.Horizontal)
                AssignControlPoint(ref m_ControlPoints[1], new Vector2(from.x + offset, from.y));
            else
                AssignControlPoint(ref m_ControlPoints[1], new Vector2(from.x, from.y + offset));

            if (inputOrientation == Orientation.Horizontal)
                AssignControlPoint(ref m_ControlPoints[2], new Vector2(to.x - offset, to.y));
            else
                AssignControlPoint(ref m_ControlPoints[2], new Vector2(to.x, to.y - offset));

            AssignControlPoint(ref m_ControlPoints[3], to);
            Profiler.EndSample();
        }

        void ComputeLayout()
        {
            Profiler.BeginSample("EdgeControl.ComputeLayout");
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
            rect.width += margin;
            rect.height += margin;

            if (layout != rect)
            {
                layout = rect;
                m_RenderPointsDirty = true;
            }
            Profiler.EndSample();
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

        void DrawEdge(MeshGenerationContext mgc)
        {
            if (edgeWidth <= 0)
                return;

            UpdateRenderPoints();
            if (m_RenderPoints.Count == 0)
                return; // Don't draw anything

            Color inColor = this.inputColor;
            Color outColor = this.outputColor;

            inColor *= UIElementsUtility.editorPlayModeTintColor;
            outColor *= UIElementsUtility.editorPlayModeTintColor;

            uint cpt = (uint)m_RenderPoints.Count;
            uint wantedLength = (cpt) * 2;
            uint indexCount = (wantedLength - 2) * 3;

            var md = mgc.Allocate((int)wantedLength, (int)indexCount, null, null, MeshGenerationContext.MeshFlags.UVisDisplacement);
            if (md.vertexCount == 0)
                return;

            float polyLineLength = 0;
            for (int i = 1; i < cpt; ++i)
                polyLineLength += (m_RenderPoints[i - 1] - m_RenderPoints[i]).sqrMagnitude;

            float halfWidth = edgeWidth * 0.5f;
            float currentLength = 0;
            Color32 flags = new Color32(0, 0, 0, (byte)VertexFlags.LastType);

            Vector2 unitPreviousSegment = Vector2.zero;
            for (int i = 0; i < cpt; ++i)
            {
                Vector2 dir;
                Vector2 unitNextSegment = Vector2.zero;
                Vector2 nextSegment = Vector2.zero;

                if (i < cpt - 1)
                {
                    nextSegment = (m_RenderPoints[i + 1] - m_RenderPoints[i]);
                    unitNextSegment = nextSegment.normalized;
                }


                if (i > 0 && i < cpt - 1)
                {
                    dir = unitPreviousSegment + unitNextSegment;
                    dir.Normalize();
                }
                else if (i > 0)
                {
                    dir = unitPreviousSegment;
                }
                else
                {
                    dir = unitNextSegment;
                }

                Vector2 pos = m_RenderPoints[i];
                Vector2 uv = new Vector2(dir.y * halfWidth, -dir.x * halfWidth); // Normal scaled by half width
                Color32 tint = Color.LerpUnclamped(outColor, inColor, currentLength / polyLineLength);

                md.SetNextVertex(new Vertex() { position = new Vector3(pos.x, pos.y, 1), uv = uv, tint = tint, idsFlags = flags });
                md.SetNextVertex(new Vertex() { position = new Vector3(pos.x, pos.y, -1), uv = uv, tint = tint, idsFlags = flags });

                if (i < cpt - 2)
                {
                    currentLength += nextSegment.sqrMagnitude;
                }
                else
                {
                    currentLength = polyLineLength;
                }

                unitPreviousSegment = unitNextSegment;
            }

            // Fill triangle indices as it is a triangle strip
            for (uint i = 0; i < wantedLength - 2; ++i)
            {
                if ((i & 0x01) == 0)
                {
                    md.SetNextIndex((UInt16)i);
                    md.SetNextIndex((UInt16)(i + 2));
                    md.SetNextIndex((UInt16)(i + 1));
                }
                else
                {
                    md.SetNextIndex((UInt16)i);
                    md.SetNextIndex((UInt16)(i + 1));
                    md.SetNextIndex((UInt16)(i + 2));
                }
            }
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
