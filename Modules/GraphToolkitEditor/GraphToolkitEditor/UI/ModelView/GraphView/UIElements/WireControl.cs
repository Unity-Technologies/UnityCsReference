// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// VisualElement that controls how a wire is displayed. Designed to be added as a children to a <see cref="Wire"/>
    /// </summary>
    [UnityRestricted]
    internal class WireControl : VisualElement
    {
        static readonly CustomStyleProperty<float> k_WireWidthProperty = new("--wire-width");
        static readonly CustomStyleProperty<Color> k_WireColorProperty = new("--wire-color");
        static readonly Gradient k_Gradient = new Gradient();

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
        const float k_WireTurnRadius = k_WireTurnDiameter * 0.5f;
        const float k_MinWireWidth = 1.75f;
        const float k_MinOpacity = 0.6f;

        protected Wire m_Wire;

        float m_Zoom = 1.0f;

        /// <summary>
        /// The control points of the wire expressed in the parent <see cref="Wire"/> coordinates.
        /// </summary>
        protected Vector2[] m_ControlPoints = new Vector2[4];

        protected PortOrientation m_ToOrientation;

        protected PortOrientation m_FromOrientation;

        protected PortDirection m_FromDirection;

        protected PortDirection m_ToDirection;

        protected Color m_ToColor = Color.grey;

        protected Color m_FromColor = Color.grey;

        protected bool m_ColorOverridden;

        protected bool m_WidthOverridden;

        protected float m_LineWidth = WireUtilities.DefaultWireWidth;

        protected float StyleLineWidth { get; set; } = WireUtilities.DefaultWireWidth;

        protected Color WireColor { get; set; } = WireUtilities.DefaultWireColor;

        // The start of the wire in graph coordinates.
        protected Vector2 From => m_Wire?.GetFrom() ?? Vector2.zero;

        // The end of the wire in graph coordinates.
        protected Vector2 To => m_Wire?.GetTo() ?? Vector2.zero;

        internal PortOrientation ToOrientation
        {
            get => m_ToOrientation;
            set => m_ToOrientation = value;
        }

        internal PortOrientation FromOrientation
        {
            get => m_FromOrientation;
            set => m_FromOrientation = value;
        }


        internal PortDirection FromDirection
        {
            get => m_FromDirection;
            set => m_FromDirection = value;
        }
        internal PortDirection ToDirection
        {
            get => m_ToDirection;
            set => m_ToDirection = value;
        }

        /// <summary>
        /// The color of the wire at the input port.
        /// </summary>
        public Color InputColor
        {
            get => m_ToColor;
            private set
            {
                if (m_ToColor != value)
                {
                    m_ToColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// The color of the wire at the output port.
        /// </summary>
        public Color OutputColor
        {
            get => m_FromColor;
            private set
            {
                if (m_FromColor != value)
                {
                    m_FromColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// The width of the wire.
        /// </summary>
        public float LineWidth
        {
            get => m_LineWidth;
            set
            {
                m_WidthOverridden = true;

                if (Math.Abs(m_LineWidth - value) < 0.05)
                    return;

                m_LineWidth = value;
                UpdateLayout(); // The layout depends on the wires width
                MarkDirtyRepaint();
            }
        }

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
                m_ToColor = WireColor;
                m_FromColor = WireColor;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Resets the line width to the default value.
        /// </summary>
        public void ResetLineWidth()
        {
            m_WidthOverridden = false;
            m_LineWidth = StyleLineWidth;
        }

        /// <summary>
        /// Sets the color of the wire.
        /// </summary>
        /// <param name="inputColor">The color of the wire at the input port.</param>
        /// <param name="outputColor">The color of the wire at the output port.</param>
        public void SetColor(Color inputColor, Color outputColor)
        {
            m_ColorOverridden = true;
            InputColor = inputColor;
            OutputColor = outputColor;
        }

        /// <summary>
        /// Resets the color of the wire to the default value.
        /// </summary>
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

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            return base.ContainsPoint(localPoint) && IsPointOnLine(this.ChangeCoordinatesTo(parent, localPoint));
        }

        /// <summary>
        /// Tests if a point is on the wire.
        /// </summary>
        /// <param name="parentPoint">The point to test.</param>
        /// <returns>True if the point lies on the wire, false otherwise.</returns>
        public bool IsPointOnLine(Vector2 parentPoint)
        {
            return WireUtilities.IsPointOnLine(parentPoint, m_ControlPoints, LineWidth + 1);
        }

        /// <inheritdoc />
        public override bool Overlaps(Rect r)
        {
            return base.Overlaps(r) && RectIntersectsLine(this.ChangeCoordinatesTo(parent, r));
        }

        /// <summary>
        /// Tests if a rectangle intersects the wire.
        /// </summary>
        /// <param name="r">The rectangle to test.</param>
        /// <returns>True if the rectangle intersects the wire, false otherwise.</returns>
        public bool RectIntersectsLine(Rect r)
        {
            return WireUtilities.RectIntersectsLine(r, m_ControlPoints);
        }

        static bool Approximately(Vector2 v1, Vector2 v2)
        {
            return Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y);
        }

        /// <summary>
        /// Recomputes the layout of the wire.
        /// </summary>
        public void UpdateLayout()
        {
            if (parent != null)
                ComputeLayout();
        }

        static void AssignControlPoint(ref Vector2 destination, Vector2 newValue)
        {
            if (!Approximately(destination, newValue))
            {
                destination = newValue;
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

            if (FromDirection == PortDirection.Output)
            {
                if (FromOrientation == PortOrientation.Horizontal)
                    AssignControlPoint(ref m_ControlPoints[1], new Vector2(From.x + offset, From.y));
                else
                    AssignControlPoint(ref m_ControlPoints[1], new Vector2(From.x, From.y + offset));
            }
            else
            {
                if (FromOrientation == PortOrientation.Horizontal)
                    AssignControlPoint(ref m_ControlPoints[1], new Vector2(From.x - offset, From.y));
                else
                    AssignControlPoint(ref m_ControlPoints[1], new Vector2(From.x, From.y - offset));
            }

            if (ToDirection == PortDirection.Input)
            {
                if (ToOrientation == PortOrientation.Horizontal)
                    AssignControlPoint(ref m_ControlPoints[2], new Vector2(To.x - offset, To.y));
                else
                    AssignControlPoint(ref m_ControlPoints[2], new Vector2(To.x, To.y - offset));
            }
            else
            {
                if (ToOrientation == PortOrientation.Horizontal)
                    AssignControlPoint(ref m_ControlPoints[2], new Vector2(To.x + offset, To.y));
                else
                    AssignControlPoint(ref m_ControlPoints[2], new Vector2(To.x, To.y + offset));
            }

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

        static GradientColorKey[] s_ColorKeys = new GradientColorKey[2];
        static GradientAlphaKey[] s_AlphaKeys = new GradientAlphaKey[1];

        protected void DrawWire(MeshGenerationContext mgc)
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawWire");

            if (LineWidth <= 0)
                return;

            Color inColor = InputColor;
            Color outColor = OutputColor;

            inColor *= this.GetPlayModeTintColor();
            outColor *= this.GetPlayModeTintColor();

            var painter2D = mgc.painter2D;

            float width = m_WidthOverridden ? LineWidth : StyleLineWidth;
            float alpha = 1.0f;

            if (width * Zoom < k_MinWireWidth)
            {
                float t = width * Zoom / k_MinWireWidth;

                alpha = Mathf.Lerp(k_MinOpacity, 1.0f, t);
                width = k_MinWireWidth / Zoom;
            }

            s_ColorKeys[0] = new GradientColorKey(outColor, 0);
            s_ColorKeys[1] = new GradientColorKey(inColor, 1);

            s_AlphaKeys[0] = new GradientAlphaKey(alpha, 0);

            k_Gradient.SetKeys(s_ColorKeys, s_AlphaKeys);
            painter2D.BeginPath();
            painter2D.strokeGradient = k_Gradient;

            var localToWorld = parent.worldTransform;
            var worldToLocal = this.GetWorldTransformInverse();

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

            painter2D.lineWidth = width;
            painter2D.MoveTo(p1);

            var threshold = Vector2.Distance(p1, p2) + Vector2.Distance(p3, p4);
            if (Vector2.Distance(p1, p4) < threshold || Vector2.Distance(p2, p3) < k_WireTurnDiameter)
            {
                painter2D.LineTo(p4);
            }
            else
            {
                var slopeDirection = (p2 - p3).normalized;
                painter2D.BezierCurveTo(p2, p2, p2 - slopeDirection * k_WireTurnRadius);
                painter2D.LineTo(p3 + slopeDirection * k_WireTurnRadius);
                painter2D.BezierCurveTo(p3, p3, p4);
            }
            painter2D.Stroke();

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
