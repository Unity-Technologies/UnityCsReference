// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// VisualElement that controls how a wire is displayed. Designed to be added as a children to an <see cref="Wire"/>
    /// </summary>
    class WireControl : VisualElement
    {
        struct WireCornerSweepValues
        {
            public Vector2 circleCenter;
            public double sweepAngle;
            public double startAngle;
            public double endAngle;
            public Vector2 crossPoint1;
            public Vector2 crossPoint2;
            public float radius;
        }

        static readonly CustomStyleProperty<int> k_WireWidthProperty = new CustomStyleProperty<int>("--wire-width");
        static readonly CustomStyleProperty<Color> k_WireColorProperty = new CustomStyleProperty<Color>("--wire-color");
        static readonly Gradient k_Gradient = new Gradient();

        static int DefaultWireWidth => 2;

        static Color DefaultWireColor {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(193 / 255f, 193 / 255f, 193 / 255f);
                }

                return new Color(90 / 255f, 90 / 255f, 90 / 255f);
            }
        }

        /// <summary>
        /// The current zoom level.
        /// </summary>
        public float Zoom
        {
            get => m_Zoom;
            set
            {
                m_Zoom = value;
                MarkDirtyRepaint();
            }
        }

        const float k_WireLengthFromPort = 12.0f;
        const float k_WireTurnDiameter = 16.0f;
        const float k_WireSweepResampleRatio = 4.0f;
        const float k_WireStraightLineSegmentMultiplier = 0.2f;
        const float k_MinWireWidth = 1.75f;
        const float k_MinOpacity = 0.6f;

        protected Wire m_Wire;

        float m_Zoom = 1.0f;

        protected Vector2[] m_ControlPoints = new Vector2[4];
        Vector2[] m_LastLocalControlPoints = new Vector2[4];


        protected PortOrientation m_InputOrientation;

        protected PortOrientation m_OutputOrientation;

        protected Color m_InputColor = Color.grey;

        protected Color m_OutputColor = Color.grey;

        protected bool m_ColorOverridden;

        protected bool m_WidthOverridden;

        protected int m_LineWidth = DefaultWireWidth;

        protected bool m_RenderPointsDirty = true;

        protected int StyleLineWidth { get; set; } = DefaultWireWidth;

        protected Color WireColor { get; set; } = DefaultWireColor;

        // The start of the wire in graph coordinates.
        protected Vector2 From => m_Wire?.From ?? Vector2.zero;

        // The end of the wire in graph coordinates.
        protected Vector2 To => m_Wire?.To ?? Vector2.zero;

        internal PortOrientation InputOrientation_Internal
        {
            get => m_InputOrientation;
            set => m_InputOrientation = value;
        }

        internal PortOrientation OutputOrientation_Internal
        {
            get => m_OutputOrientation;
            set => m_OutputOrientation = value;
        }

        public Color InputColor
        {
            get => m_InputColor;
            private set
            {
                if (m_InputColor != value)
                {
                    m_InputColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public Color OutputColor
        {
            get => m_OutputColor;
            private set
            {
                if (m_OutputColor != value)
                {
                    m_OutputColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        public int LineWidth
        {
            get => m_LineWidth;
            set
            {
                m_WidthOverridden = true;

                if (m_LineWidth == value)
                    return;

                m_LineWidth = value;
                UpdateLayout(); // The layout depends on the wires width
                MarkDirtyRepaint();
            }
        }

        // The points that will be rendered. Expressed in coordinates local to the element.
        public List<Vector2> RenderPoints { get; } = new List<Vector2>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WireControl"/> class.
        /// </summary>
        public WireControl(Wire wire)
        {
            m_Wire = wire;
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Position;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        protected void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(k_WireWidthProperty, out var wireWidthValue))
                StyleLineWidth = wireWidthValue;

            if (e.customStyle.TryGetValue(k_WireColorProperty, out var wireColorValue))
                WireColor = wireColorValue;

            if (!m_WidthOverridden)
            {
                m_LineWidth = StyleLineWidth;
                UpdateLayout(); // The layout depends on the wires width
                MarkDirtyRepaint();
            }

            if (!m_ColorOverridden)
            {
                m_InputColor = WireColor;
                m_OutputColor = WireColor;
                MarkDirtyRepaint();
            }
        }

        public void SetColor(Color inputColor, Color outputColor)
        {
            m_ColorOverridden = true;
            InputColor = inputColor;
            OutputColor = outputColor;
        }

        public void ResetColor()
        {
            m_ColorOverridden = false;
            InputColor = WireColor;
            OutputColor = WireColor;
        }

        protected void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            DrawWire(mgc);
        }

        static float SquaredDistanceToSegment(Vector2 p, Vector2 s0, Vector2 s1)
        {
            var x = p.x;
            var y = p.y;
            var x1 = s0.x;
            var y1 = s0.y;
            var x2 = s1.x;
            var y2 = s1.y;

            var a = x - x1;
            var b = y - y1;
            var c = x2 - x1;
            var d = y2 - y1;

            var dot = a * c + b * d;
            var lenSq = c * c + d * d;
            float param = -1;
            if (lenSq > float.Epsilon) //in case of 0 length line
                param = dot / lenSq;

            float xx, yy;

            if (param < 0)
            {
                xx = x1;
                yy = y1;
            }
            else if (param > 1)
            {
                xx = x2;
                yy = y2;
            }
            else
            {
                xx = x1 + param * c;
                yy = y1 + param * d;
            }

            var dx = x - xx;
            var dy = y - yy;
            return dx * dx + dy * dy;
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (!base.ContainsPoint(localPoint))
            {
                return false;
            }

            for (var index = 0; index < RenderPoints.Count - 1; index++)
            {
                var a = RenderPoints[index];
                var b = RenderPoints[index + 1];
                var squareDistance = SquaredDistanceToSegment(localPoint, a, b);
                if (squareDistance < (LineWidth + 1)*(LineWidth + 1))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Overlaps(Rect r)
        {
            if (base.Overlaps(r))
            {
                for (int a = 0; a < RenderPoints.Count - 1; a++)
                {
                    if (RectUtils_Internal.IntersectsSegment(r, RenderPoints[a], RenderPoints[a + 1]))
                        return true;
                }
            }

            return false;
        }

        static bool Approximately(Vector2 v1, Vector2 v2)
        {
            return Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y);
        }

        public void UpdateLayout()
        {
            if (parent != null)
                ComputeLayout();
        }

        void RenderStraightLines(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float safeSpan = OutputOrientation_Internal == PortOrientation.Horizontal
                ? Mathf.Abs((p1.x + k_WireLengthFromPort) - (p4.x - k_WireLengthFromPort))
                : Mathf.Abs((p1.y + k_WireLengthFromPort) - (p4.y - k_WireLengthFromPort));

            float safeSpan3 = safeSpan * k_WireStraightLineSegmentMultiplier;
            float nodeToP2Dist = Mathf.Min(safeSpan3, k_WireTurnDiameter);
            nodeToP2Dist = Mathf.Max(0, nodeToP2Dist);

            var offset = OutputOrientation_Internal == PortOrientation.Horizontal
                ? new Vector2(k_WireTurnDiameter - nodeToP2Dist, 0)
                : new Vector2(0, k_WireTurnDiameter - nodeToP2Dist);

            RenderPoints.Add(p1);
            RenderPoints.Add(p2 - offset);
            RenderPoints.Add(p3 + offset);
            RenderPoints.Add(p4);
        }

        protected virtual void UpdateRenderPoints()
        {
            if (m_RenderPointsDirty == false)
            {
                return;
            }

            var localToWorld = parent.worldTransform;
            var worldToLocal = worldTransformInverse;

            Vector2 ChangeCoordinates(Vector2 point)
            {
                Vector2 res;
                res.x = localToWorld.m00 * point.x + localToWorld.m01 * point.y + localToWorld.m03;
                res.y = localToWorld.m10 * point.x + localToWorld.m11 * point.y + localToWorld.m13;

                Vector2 res2;
                res2.x = worldToLocal.m00 * res.x + worldToLocal.m01 * res.y + worldToLocal.m03;
                res2.y = worldToLocal.m10 * res.x + worldToLocal.m11 * res.y + worldToLocal.m13;

                return res2;
            }

            Vector2 p1 = ChangeCoordinates(m_ControlPoints[0]);
            Vector2 p2 = ChangeCoordinates(m_ControlPoints[1]);
            Vector2 p3 = ChangeCoordinates(m_ControlPoints[2]);
            Vector2 p4 = ChangeCoordinates(m_ControlPoints[3]);

            // Only compute this when the "local" points have actually changed
            if (Approximately(p1, m_LastLocalControlPoints[0]) &&
                Approximately(p2, m_LastLocalControlPoints[1]) &&
                Approximately(p3, m_LastLocalControlPoints[2]) &&
                Approximately(p4, m_LastLocalControlPoints[3]))
            {
                m_RenderPointsDirty = false;
                return;
            }

            m_LastLocalControlPoints[0] = p1;
            m_LastLocalControlPoints[1] = p2;
            m_LastLocalControlPoints[2] = p3;
            m_LastLocalControlPoints[3] = p4;
            m_RenderPointsDirty = false;

            RenderPoints.Clear();

            float diameter = k_WireTurnDiameter;

            // We have to handle a special case of the wire when it is a straight line, but not
            // when going backwards in space (where the start point is in front in y to the end point).
            // We do this by turning the line into 3 linear segments with no curves. This also
            // avoids possible NANs in later angle calculations.
            bool sameOrientations = OutputOrientation_Internal == InputOrientation_Internal;
            if (sameOrientations &&
                ((OutputOrientation_Internal == PortOrientation.Horizontal && Mathf.Abs(p1.y - p4.y) < 2 && p1.x + k_WireLengthFromPort < p4.x - k_WireLengthFromPort) ||
                 (OutputOrientation_Internal == PortOrientation.Vertical && Mathf.Abs(p1.x - p4.x) < 2 && p1.y + k_WireLengthFromPort < p4.y - k_WireLengthFromPort)))
            {
                RenderStraightLines(p1, p2, p3, p4);
                return;
            }

            bool renderBothCorners = true;

            var corner1 = GetCornerSweepValues(p1, p2, p3, diameter, PortDirection.Output);
            var corner2 = GetCornerSweepValues(p2, p3, p4, diameter, PortDirection.Input);

            if (!ValidateCornerSweepValues(ref corner1, ref corner2))
            {
                if (sameOrientations)
                {
                    RenderStraightLines(p1, p2, p3, p4);
                    return;
                }

                renderBothCorners = false;

                //we try to do it with a single corner instead
                var px = (OutputOrientation_Internal == PortOrientation.Horizontal) ? new Vector2(p4.x, p1.y) : new Vector2(p1.x, p4.y);

                corner1 = GetCornerSweepValues(p1, px, p4, diameter, PortDirection.Output);
            }

            RenderPoints.Add(p1);

            if (!sameOrientations && renderBothCorners)
            {
                //if the 2 corners or endpoints are too close, the corner sweep angle calculations can't handle different orientations
                float minDistance = 2 * diameter * diameter;
                if ((p3 - p2).sqrMagnitude < minDistance ||
                    (p4 - p1).sqrMagnitude < minDistance)
                {
                    var px = (p2 + p3) * 0.5f;
                    corner1 = GetCornerSweepValues(p1, px, p4, diameter, PortDirection.Output);
                    renderBothCorners = false;
                }
            }

            GetRoundedCornerPoints(RenderPoints, corner1, PortDirection.Output);
            if (renderBothCorners)
                GetRoundedCornerPoints(RenderPoints, corner2, PortDirection.Input);

            RenderPoints.Add(p4);
        }

        bool ValidateCornerSweepValues(ref WireCornerSweepValues corner1, ref WireCornerSweepValues corner2)
        {
            // Get the midpoint between the two corner circle centers.
            Vector2 circlesMidpoint = (corner1.circleCenter + corner2.circleCenter) / 2;

            // Find the angle to the corner circles midpoint so we can compare it to the sweep angles of each corner.
            Vector2 p2CenterToCross1 = corner1.circleCenter - corner1.crossPoint1;
            Vector2 p2CenterToCirclesMid = corner1.circleCenter - circlesMidpoint;
            double angleToCirclesMid = OutputOrientation_Internal == PortOrientation.Horizontal
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

        WireCornerSweepValues GetCornerSweepValues(
            Vector2 p1, Vector2 cornerPoint, Vector2 p2, float diameter, PortDirection closestPortDirection)
        {
            var corner = new WireCornerSweepValues();

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
            // makes the corner turn radii get smaller as the wire corners get tighter.
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
            if (closestPortDirection == PortDirection.Input)
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

        static Vector2 GetCornerCircleCenter(Vector2 cornerPoint, Vector2 crossPoint1, Vector2 crossPoint2, float segment, float radius)
        {
            float dx = cornerPoint.x * 2 - crossPoint1.x - crossPoint2.x;
            float dy = cornerPoint.y * 2 - crossPoint1.y - crossPoint2.y;

            var cornerToCenterVector = new Vector2(dx, dy);

            float magnitude = cornerToCenterVector.magnitude;

            if (Mathf.Approximately(magnitude, 0))
            {
                return cornerPoint;
            }

            float d = new Vector2(segment, radius).magnitude;
            float factor = d / magnitude;

            return new Vector2(cornerPoint.x - cornerToCenterVector.x * factor, cornerPoint.y - cornerToCenterVector.y * factor);
        }

        void GetRoundedCornerPoints(List<Vector2> points, WireCornerSweepValues corner, PortDirection closestPortDirection)
        {
            // Calculate the number of points that will sample the arc from the sweep angle.
            int pointsCount = Mathf.CeilToInt((float)Math.Abs(corner.sweepAngle * k_WireSweepResampleRatio));
            int sign = Math.Sign(corner.sweepAngle);
            bool backwards = (closestPortDirection == PortDirection.Input);

            for (int i = 0; i < pointsCount; ++i)
            {
                // If we are computing the second corner (into the input port), the sweep is going backwards
                // but we still need to add the points to the list in the correct order.
                float sweepIndex = backwards ? i - pointsCount : i;

                double sweepedAngle = corner.startAngle + sign * sweepIndex / k_WireSweepResampleRatio;

                var pointX = (float)(corner.circleCenter.x + Math.Cos(sweepedAngle) * corner.radius);
                var pointY = (float)(corner.circleCenter.y + Math.Sin(sweepedAngle) * corner.radius);

                // Check if we overlap the previous point. If we do, we skip this point so that we
                // don't cause the wire polygons to twist.
                if (i == 0 && backwards)
                {
                    if (OutputOrientation_Internal == PortOrientation.Horizontal)
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

        void AssignControlPoint(ref Vector2 destination, Vector2 newValue)
        {
            if (!Approximately(destination, newValue))
            {
                destination = newValue;
                m_RenderPointsDirty = true;
            }
        }

        void ComputeControlPoints()
        {
            float offset = k_WireLengthFromPort + k_WireTurnDiameter;

            // This is to ensure we don't have the wire extending
            // left and right by the offset right when the `from`
            // and `to` are on top of each other.
            float fromToDistance = (To - From).magnitude;
            offset = Mathf.Min(offset, fromToDistance * 2);
            offset = Mathf.Max(offset, k_WireTurnDiameter);

            if (m_ControlPoints == null || m_ControlPoints.Length != 4)
                m_ControlPoints = new Vector2[4];

            AssignControlPoint(ref m_ControlPoints[0], From);

            if (OutputOrientation_Internal == PortOrientation.Horizontal)
                AssignControlPoint(ref m_ControlPoints[1], new Vector2(From.x + offset, From.y));
            else
                AssignControlPoint(ref m_ControlPoints[1], new Vector2(From.x, From.y + offset));

            if (InputOrientation_Internal == PortOrientation.Horizontal)
                AssignControlPoint(ref m_ControlPoints[2], new Vector2(To.x - offset, To.y));
            else
                AssignControlPoint(ref m_ControlPoints[2], new Vector2(To.x, To.y - offset));

            AssignControlPoint(ref m_ControlPoints[3], To);
        }

        void ComputeLayout()
        {
            ComputeControlPoints();

            // Compute VisualElement position and dimension.
            var wireModel = m_Wire?.WireModel;

            if (wireModel == null)
            {
                style.top = 0;
                style.left = 0;
                style.width = 0;
                style.height = 0;
                return;
            }

            Vector2 min = m_ControlPoints[0];
            Vector2 max = m_ControlPoints[0];

            for (int i = 1; i < m_ControlPoints.Length; ++i)
            {
                min.x = Math.Min(min.x, m_ControlPoints[i].x);
                min.y = Math.Min(min.y, m_ControlPoints[i].y);
                max.x = Math.Max(max.x, m_ControlPoints[i].x);
                max.y = Math.Max(max.y, m_ControlPoints[i].y);
            }

            var grow = LineWidth / 2.0f;
            min.x -= grow;
            max.x += grow;
            min.y -= grow;
            max.y += grow;

            var dim = max - min;
            style.left = min.x;
            style.top = min.y;
            style.width = dim.x;
            style.height = dim.y;
        }

        protected void DrawWire(MeshGenerationContext mgc)
        {
            if (LineWidth <= 0)
                return;

            UpdateRenderPoints();
            int cpt = RenderPoints.Count;
            if (cpt == 0)
                return; // Don't draw anything

            Color inColor = InputColor;
            Color outColor = OutputColor;

            inColor *= UIElementsUtility.editorPlayModeTintColor;
            outColor *= UIElementsUtility.editorPlayModeTintColor;

            var painter2D = mgc.painter2D;

            float width = StyleLineWidth;
            float alpha = 1.0f;

            if (StyleLineWidth * Zoom < k_MinWireWidth)
            {
                float t = StyleLineWidth * Zoom / k_MinWireWidth;

                alpha = Mathf.Lerp(k_MinOpacity,1.0f, t);
                width = k_MinWireWidth / Zoom;
            }

            k_Gradient.SetKeys(new[]{ new GradientColorKey(outColor, 0),new GradientColorKey(inColor, 1)},new []{new GradientAlphaKey(alpha, 0)});
            painter2D.BeginPath();
            painter2D.strokeGradient = k_Gradient;

            painter2D.lineWidth = width;
            painter2D.MoveTo(RenderPoints[0]);

            for(int i = 1 ; i < cpt ; ++i)
                if((RenderPoints[i-1] - RenderPoints[i]).sqrMagnitude > 0.1f*0.1f )
                    painter2D.LineTo(RenderPoints[i]);

            painter2D.Stroke();
        }
    }
}
