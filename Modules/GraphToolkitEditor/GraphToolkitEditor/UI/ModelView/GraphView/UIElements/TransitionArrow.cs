// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A visual element representing a transition arrow.
    /// </summary>
    [UnityRestricted]
    internal class TransitionArrow : VisualElement
    {
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<Color> s_ArrowWireColorProperty = new("--wire-color");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> s_ArrowWireWidthProperty = new("--wire-width");

        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<Color> s_ArrowInnerLineColorProperty = new("--inner-line-color");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> s_ArrowInnerLineWidthProperty = new("--inner-line-width");

        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<Color> s_ArrowFillColorProperty = new("--arrow-fill-color");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> s_ArrowWidthProperty = new("--arrow-width");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> s_ArrowLengthProperty = new("--arrow-length");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> s_ArrowBorderRadiusProperty = new("--arrow-border-radius");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> s_ArrowTriangleLengthProperty = new("--arrow-triangle-length");

        static readonly float k_DefaultArrowWidth = 12.0f;
        static readonly float k_DefaultArrowLength = 16.0f;
        static readonly float k_DefaultArrowBorderRadius = 2.5f;
        static readonly float k_DefaultArrowWireWidth = 2.5f;
        static readonly float k_DefaultArrowTriangleLength = 5f;

        Color m_OuterLineColor = Color.grey;
        bool m_OuterColorOverridden;

        float m_OuterLineWidth = k_DefaultArrowWireWidth;
        bool m_OuterWidthOverridden;

        Color m_InnerLineColor = Color.grey;
        bool m_InnerLineColorOverridden;

        float m_InnerLineWidth;
        bool m_InnerLineWidthOverridden;

        Color m_FillColor = Color.grey;
        bool m_FillColorOverridden;

        float m_ArrowWidth = k_DefaultArrowWidth;
        float m_ArrowLength = k_DefaultArrowLength;
        float m_ArrowBorderRadius = k_DefaultArrowBorderRadius;
        float m_ArrowTriangleLength = k_DefaultArrowTriangleLength;

        // The points that will be rendered. Expressed in coordinates local to the element.
        Vector2[] m_RenderPoints;

        /// <summary>
        /// The color of the outer contour of the arrow.
        /// </summary>
        public Color OuterLineColor
        {
            get => m_OuterLineColor;
            set
            {
                m_OuterColorOverridden = true;

                if (m_OuterLineColor != value)
                {
                    m_OuterLineColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// The width of the outer contour of the arrow.
        /// </summary>
        public float OuterLineWidth
        {
            get => m_OuterLineWidth;
            set
            {
                m_OuterWidthOverridden = true;

                if (Math.Abs(m_OuterLineWidth - value) < 0.05)
                    return;

                m_OuterLineWidth = value;
                UpdateLayout(); // The layout depends on the wires width
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The color of the inner contour of the arrow.
        /// </summary>
        public Color InnerLineColor
        {
            get => m_InnerLineColor;
            set
            {
                m_InnerLineColorOverridden = true;

                if (m_InnerLineColor != value)
                {
                    m_InnerLineColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// The width of the inner contour of the arrow.
        /// </summary>
        public float InnerLineWidth
        {
            get => m_InnerLineWidth;
            set
            {
                m_InnerLineWidthOverridden = true;

                if (Math.Abs(m_InnerLineWidth - value) < 0.05)
                    return;

                m_InnerLineWidth = value;
                UpdateLayout(); // The layout depends on the wires width
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The fill color of the arrow.
        /// </summary>
        /// <remarks>Setting this overrides the fill color resolved from the style sheet.</remarks>
        public Color FillColor
        {
            get => m_FillColor;
            set
            {
                if (m_FillColor != value && value != default)
                {
                    m_FillColorOverridden = true;
                    m_FillColor = value;
                    MarkDirtyRepaint();
                }
            }
        }

        Color StyleOuterLineColor { get; set; } = WireUtilities.DefaultWireColor;
        Color StyleInnerLineColor { get; set; } = WireUtilities.DefaultWireColor;
        Color StyleFillColor { get; set; } = Color.grey;
        float StyleOuterLineWidth { get; set; } = k_DefaultArrowWireWidth;
        float StyleInnerLineWidth { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionArrow"/> class.
        /// </summary>
        public TransitionArrow()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(_ => UpdateLayout());
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            bool shouldRepaint = false;
            bool shouldLayout = false;

            if (e.customStyle.TryGetValue(s_ArrowWireColorProperty, out var wireColorValue))
            {
                StyleOuterLineColor = wireColorValue;

                if (!m_OuterColorOverridden)
                {
                    m_OuterLineColor = StyleOuterLineColor;
                    shouldRepaint = true;
                }
            }

            if (e.customStyle.TryGetValue(s_ArrowInnerLineColorProperty, out var innerLineColor))
            {
                StyleInnerLineColor = innerLineColor;

                if (!m_InnerLineColorOverridden)
                {
                    m_InnerLineColor = StyleInnerLineColor;
                    shouldRepaint = true;
                }
            }

            if (e.customStyle.TryGetValue(s_ArrowFillColorProperty, out var fillColorValue))
            {
                StyleFillColor = fillColorValue;

                if (!m_FillColorOverridden)
                {
                    m_FillColor = StyleFillColor;
                    shouldRepaint = true;
                }
            }

            if (e.customStyle.TryGetValue(s_ArrowWidthProperty, out var arrowWidthValue))
            {
                m_ArrowWidth = arrowWidthValue;
                shouldRepaint = true;
                shouldLayout = true;
            }

            if (e.customStyle.TryGetValue(s_ArrowLengthProperty, out var arrowLengthValue))
            {
                m_ArrowLength = arrowLengthValue;
                shouldRepaint = true;
                shouldLayout = true;
            }

            if (e.customStyle.TryGetValue(s_ArrowBorderRadiusProperty, out var arrowBorderRadiusValue))
            {
                m_ArrowBorderRadius = arrowBorderRadiusValue;
                shouldRepaint = true;
            }

            if (e.customStyle.TryGetValue(s_ArrowWireWidthProperty, out var wireWidthValue))
            {
                StyleOuterLineWidth = wireWidthValue;

                if (!m_OuterWidthOverridden)
                {
                    m_OuterLineWidth = StyleOuterLineWidth;
                    shouldRepaint = true;
                    shouldLayout = true; // The layout depends on the wires width
                }
            }

            if (e.customStyle.TryGetValue(s_ArrowInnerLineWidthProperty, out var innerLineWidth))
            {
                StyleInnerLineWidth = innerLineWidth;

                if (!m_InnerLineWidthOverridden)
                {
                    m_InnerLineWidth = StyleInnerLineWidth;
                    shouldRepaint = true;
                    shouldLayout = true; // The layout depends on the wires width
                }
            }

            if (e.customStyle.TryGetValue(s_ArrowTriangleLengthProperty, out var arrowTriangleLengthValue))
            {
                m_ArrowTriangleLength = arrowTriangleLengthValue;
                shouldRepaint = true;
            }

            if (shouldLayout)
                UpdateLayout();

            if (shouldRepaint)
                MarkDirtyRepaint();
        }

        /// <summary>
        /// Resets the outer line width to the default value.
        /// </summary>
        public void ResetOuterLineWidth()
        {
            m_OuterWidthOverridden = false;
            m_OuterLineWidth = StyleOuterLineWidth;
        }

        /// <summary>
        /// Resets the inner line width to the default value.
        /// </summary>
        public void ResetInnerLineWidth()
        {
            m_InnerLineWidthOverridden = false;
            m_InnerLineWidth = StyleInnerLineWidth;
        }

        /// <summary>
        /// Resets the outer line color to the default value.
        /// </summary>
        public void ResetOuterLineColor()
        {
            m_OuterColorOverridden = false;
            OuterLineColor = StyleOuterLineColor;
        }

        /// <summary>
        /// Resets the inner line color to the default value.
        /// </summary>
        public void ResetInnerLineColor()
        {
            m_InnerLineColorOverridden = false;
            InnerLineColor = StyleInnerLineColor;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            DrawArrow(mgc);
        }

        (Vector2, Vector2, Vector2) GetParentTransform()
        {
            switch (parent)
            {
                case TransitionControl control:
                    return control.GetMiddleToLocal();
                case Transition transition:
                {
                    // Transition is vertical pointing down:
                    var center = transition.GetTo() + new Vector2(0, -m_ArrowLength / 2);
                    return (Vector2.up, Vector2.right, center);
                }
                default:
                    return (Vector2.up, Vector2.right, Vector2.down * m_ArrowLength / 2);
            }
        }

        public void UpdateLayout()
        {
            if (parent == null)
                return;

            var halfSize = 0.5f * new Vector2(m_ArrowLength, m_ArrowWidth);
            var tr = GetParentTransform();

            var transformedCornerA = MathUtils.Multiply2X3(tr, new Vector3(halfSize.x, halfSize.y, 1));
            var transformedCornerB = MathUtils.Multiply2X3(tr, new Vector3(-halfSize.x, halfSize.y, 1));
            var transformedCornerC = MathUtils.Multiply2X3(tr, new Vector3(halfSize.x, -halfSize.y, 1));
            var transformedCornerD = MathUtils.Multiply2X3(tr, new Vector3(-halfSize.x, -halfSize.y, 1));

            var bounds = new Rect();
            bounds.xMin = Mathf.Min(transformedCornerA.x, transformedCornerB.x, transformedCornerC.x, transformedCornerD.x);
            bounds.yMin = Mathf.Min(transformedCornerA.y, transformedCornerB.y, transformedCornerC.y, transformedCornerD.y);
            bounds.xMax = Mathf.Max(transformedCornerA.x, transformedCornerB.x, transformedCornerC.x, transformedCornerD.x);
            bounds.yMax = Mathf.Max(transformedCornerA.y, transformedCornerB.y, transformedCornerC.y, transformedCornerD.y);

            style.left = bounds.x;
            style.top = bounds.y;
            style.width = bounds.width;
            style.height = bounds.height;

            var counter = this.Q<TransitionCounter>();
            counter?.SetCenteringOffset(tr.Item1 * CentroidOffsetAlongWire());
        }

        float CentroidOffsetAlongWire()
        {
            var body = m_ArrowLength - m_ArrowTriangleLength;
            var tip = 0.5f * m_ArrowTriangleLength;
            var bodyCenter = -0.5f * m_ArrowTriangleLength;
            var tipCenter = 0.5f * m_ArrowLength - m_ArrowTriangleLength * (2f / 3f);
            return (body * bodyCenter + tip * tipCenter) / (body + tip);
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (parent == null)
                return false;
            UpdateRenderPoints();
            return IsPointInConvexPolygon(localPoint, m_RenderPoints, m_OuterLineWidth + m_ArrowBorderRadius);
        }

        static bool IsPointInConvexPolygon(Vector2 point, Vector2[] polygon, float padding)
        {
            // Determine winding from vertices alone (Shoelace formula) so the sign
            // is not influenced by where the test point happens to fall.
            float area = 0f;
            var p0 = polygon[polygon.Length - 1];
            foreach (var p1 in polygon)
            {
                area += p0.x * p1.y - p1.x * p0.y;
                p0 = p1;
            }
            float sign = area >= 0f ? 1f : -1f;

            var prev = polygon[polygon.Length - 1];
            foreach (var curr in polygon)
            {
                var edge = curr - prev;
                var cross = edge.x * (point.y - prev.y) - edge.y * (point.x - prev.x);
                if (sign * cross < -padding * edge.magnitude)
                    return false;
                prev = curr;
            }
            return true;
        }

        void UpdateRenderPoints()
        {
            var arrowLength = m_ArrowLength - 2 * m_ArrowBorderRadius;
            var arrowWidth = m_ArrowWidth - 2 * m_ArrowBorderRadius;

            // Position in local space; x is along the line and y is perpendicular to it.
            var front = new Vector2(arrowLength / 2, 0);
            var middleRight = new Vector2(arrowLength / 2 - m_ArrowTriangleLength, arrowWidth / 2);
            var middleLeft = new Vector2(middleRight.x, -middleRight.y);
            var backRight = new Vector2(-arrowLength / 2, arrowWidth / 2);
            var backLeft = new Vector2(backRight.x, -backRight.y);

            var tr = GetParentTransform();

            m_RenderPoints ??= new Vector2[5];
            m_RenderPoints[0] = LocalToLine(front);
            m_RenderPoints[1] = LocalToLine(middleRight);
            m_RenderPoints[2] = LocalToLine(backRight);
            m_RenderPoints[3] = LocalToLine(backLeft);
            m_RenderPoints[4] = LocalToLine(middleLeft);
            return;

            Vector2 LocalToLine(Vector2 localPoint) =>
                parent.ChangeCoordinatesTo(this, MathUtils.Multiply2X3(tr, new Vector3(localPoint.x, localPoint.y, 1)));
        }

        void DrawArrow(MeshGenerationContext mgc)
        {
            UpdateRenderPoints();

            var color = OuterLineColor;
            var innerLineColor = InnerLineColor;
            var fillColor = m_FillColor;

            color *= this.GetPlayModeTintColor();
            innerLineColor *= this.GetPlayModeTintColor();
            fillColor *= this.GetPlayModeTintColor();

            if (m_RenderPoints == null)
                return; // nothing to draw

            var painter = mgc.painter2D;
            painter.fillColor = fillColor;
            painter.lineJoin = LineJoin.Round;

            if (OuterLineWidth > 0)
            {
                painter.strokeColor = color;
                painter.lineWidth = 2.0f * (InnerLineWidth + OuterLineWidth + m_ArrowBorderRadius);

                painter.BeginPath();
                painter.MoveTo(m_RenderPoints[0]);
                for (int i = 1; i < 5; ++i)
                    painter.LineTo(m_RenderPoints[i]);
                painter.ClosePath();
                painter.Stroke();
            }

            if (InnerLineWidth > 0)
            {
                painter.strokeColor = innerLineColor;
                painter.lineWidth = 2.0f * (InnerLineWidth + m_ArrowBorderRadius);

                painter.BeginPath();
                painter.MoveTo(m_RenderPoints[0]);
                for (int i = 1; i < 5; ++i)
                    painter.LineTo(m_RenderPoints[i]);
                painter.ClosePath();
                painter.Stroke();
            }

            painter.strokeColor = fillColor;
            painter.lineWidth = 2 * m_ArrowBorderRadius;

            painter.BeginPath();
            painter.MoveTo(m_RenderPoints[0]);
            for (int i = 1; i < 5; ++i)
                painter.LineTo(m_RenderPoints[i]);
            painter.ClosePath();
            painter.Fill();

            painter.BeginPath();
            painter.MoveTo(m_RenderPoints[0]);
            for (int i = 1; i < 5; ++i)
                painter.LineTo(m_RenderPoints[i]);
            painter.ClosePath();
            painter.Stroke();
        }
    }
}
