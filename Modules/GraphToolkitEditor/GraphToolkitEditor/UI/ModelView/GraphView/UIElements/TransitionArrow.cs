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
        static readonly CustomStyleProperty<Color> s_ArrowHighlightColorProperty = new("--wire-highlight-color");

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

        const float k_DashLength = 6f;
        const float k_GapLength = 4f;

        Color? m_OuterLineColorOverride;
        Color? m_ModelOuterLineColor;

        float? m_OuterLineWidthOverride;
        float? m_ModelOuterLineWidth;

        bool? m_IsDashedOverride;
        bool m_ModelIsDashed;

        Color? m_FillColorOverride;
        Color? m_ModelFillColor;

        float m_ModelOpacity = 1f;

        float m_ArrowWidth = k_DefaultArrowWidth;
        float m_ArrowLength = k_DefaultArrowLength;
        float m_ArrowBorderRadius = k_DefaultArrowBorderRadius;
        float m_ArrowTriangleLength = k_DefaultArrowTriangleLength;

        float m_OpacityMultiplier = 1f;

        // The points that will be rendered. Expressed in coordinates local to the element.
        Vector2[] m_RenderPoints;

        /// <summary>
        /// A zero-size element positioned at the arrow's visual forward tip (including the border)
        /// in the element's local coordinate space.
        /// Useful for attaching markers so their tip aligns with the arrowhead tip.
        /// </summary>
        internal VisualElement ForwardTipElement { get; }

        /// <summary>
        /// The color of the outer contour of the arrow: the override when one is set, else the model color, else the
        /// USS value.
        /// </summary>
        public Color OuterLineColor => m_OuterLineColorOverride ?? m_ModelOuterLineColor ?? StyleOuterLineColor;

        /// <summary>
        /// The outer line color override. Set to null to fall back to <see cref="ModelOuterLineColor"/> or the USS value.
        /// </summary>
        public Color? OuterLineColorOverride
        {
            get => m_OuterLineColorOverride;
            set
            {
                if (m_OuterLineColorOverride == value)
                    return;

                m_OuterLineColorOverride = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The outer line color read from the model, used when no <see cref="OuterLineColorOverride"/> is set. Set to
        /// null to fall back to the USS value.
        /// </summary>
        public Color? ModelOuterLineColor
        {
            get => m_ModelOuterLineColor;
            set
            {
                if (m_ModelOuterLineColor == value)
                    return;

                m_ModelOuterLineColor = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The width of the outer contour of the arrow: the override when one is set, else the model width, else the
        /// USS value.
        /// </summary>
        public float OuterLineWidth => m_OuterLineWidthOverride ?? m_ModelOuterLineWidth ?? StyleOuterLineWidth;

        /// <summary>
        /// The outer line width override. Set to null to fall back to <see cref="ModelOuterLineWidth"/> or the USS value.
        /// </summary>
        public float? OuterLineWidthOverride
        {
            get => m_OuterLineWidthOverride;
            set
            {
                if (m_OuterLineWidthOverride == value)
                    return;

                m_OuterLineWidthOverride = value;
                UpdateLayout(); // The layout depends on the line's width
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The outer line width read from the model, used when no <see cref="OuterLineWidthOverride"/> is set. Set to
        /// null to fall back to the USS value.
        /// </summary>
        public float? ModelOuterLineWidth
        {
            get => m_ModelOuterLineWidth;
            set
            {
                if (m_ModelOuterLineWidth == value)
                    return;

                m_ModelOuterLineWidth = value;
                UpdateLayout(); // The layout depends on the line's width
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Whether the outer contour of the arrow is drawn with a dashed pattern: the override when one is set, else
        /// the model value.
        /// </summary>
        public bool IsDashed => m_IsDashedOverride ?? m_ModelIsDashed;

        /// <summary>
        /// The dashed state override. Set to null to fall back to <see cref="ModelIsDashed"/>.
        /// </summary>
        public bool? IsDashedOverride
        {
            get => m_IsDashedOverride;
            set
            {
                if (m_IsDashedOverride == value)
                    return;

                m_IsDashedOverride = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Whether the outer contour of the arrow is dashed as read from the model, used when no <see cref="IsDashedOverride"/> is set.
        /// </summary>
        public bool ModelIsDashed
        {
            get => m_ModelIsDashed;
            set
            {
                if (m_ModelIsDashed == value)
                    return;

                m_ModelIsDashed = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The fill color of the arrow: the override when one is set, else the model color, else the USS value.
        /// </summary>
        public Color FillColor => m_FillColorOverride ?? m_ModelFillColor ?? StyleFillColor;

        /// <summary>
        /// The fill color override. Set to null to fall back to <see cref="ModelFillColor"/> or the USS value.
        /// </summary>
        public Color? FillColorOverride
        {
            get => m_FillColorOverride;
            set
            {
                if (m_FillColorOverride == value)
                    return;

                m_FillColorOverride = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The fill color read from the model, used when no <see cref="FillColorOverride"/> is set. Set to null to
        /// fall back to the USS value.
        /// </summary>
        public Color? ModelFillColor
        {
            get => m_ModelFillColor;
            set
            {
                if (m_ModelFillColor == value)
                    return;

                m_ModelFillColor = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The opacity of the arrow as read from the model, used when no <see cref="OpacityMultiplier"/> override is set.
        /// </summary>
        /// <remarks>Painted only by a self transition's arrow, and only on its outer line. See <see cref="OpacityMultiplier"/>.</remarks>
        public float ModelOpacity
        {
            get => m_ModelOpacity;
            set
            {
                if (m_ModelOpacity != value)
                {
                    m_ModelOpacity = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// The opacity multiplier of the arrow.
        /// </summary>
        /// <remarks>
        /// The opacity multiplier is clamped to the [0, 1] range. Only a self transition's arrow paints with it, and
        /// only on its outer line: the arrow head stays solid, and a state-to-state arrow ignores it entirely because
        /// its line already carries the fade.
        /// </remarks>
        public float OpacityMultiplier
        {
            get => m_OpacityMultiplier;
            set
            {
                var clampedValue = Mathf.Clamp01(value);
                if (Mathf.Approximately(m_OpacityMultiplier, clampedValue))
                    return;

                m_OpacityMultiplier = clampedValue;
                MarkDirtyRepaint();
            }
        }

        // The highlight color the style sheet resolves under :hover and :checked.
        Color m_StyleHighlightColor;
        bool m_StyleHighlightColorSet;

        Color StyleOuterLineColor { get; set; } = WireUtilities.DefaultWireColor;
        Color StyleFillColor { get; set; } = Color.grey;
        float StyleOuterLineWidth { get; set; } = k_DefaultArrowWireWidth;

        internal GradientFlowAnimator FlowAnimator => parent switch
        {
            TransitionControl control => control.FlowAnimator,
            TransitionView transition => transition.FlowAnimator,
            _ => null,
        };

        internal bool IsSelfTransition => parent is TransitionView;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionArrow"/> class.
        /// </summary>
        public TransitionArrow()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<GeometryChangedEvent>(_ => UpdateLayout());

            ForwardTipElement = new VisualElement();
            ForwardTipElement.style.position = Position.Absolute;
            ForwardTipElement.style.width = 0;
            ForwardTipElement.style.height = 0;
            ForwardTipElement.pickingMode = PickingMode.Ignore;
            Add(ForwardTipElement);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var shouldRepaint = false;
            var shouldLayout = false;

            var highlightColorSet = e.customStyle.TryGetValue(s_ArrowHighlightColorProperty, out var highlightColorValue);
            if (highlightColorSet != m_StyleHighlightColorSet || m_StyleHighlightColor != highlightColorValue)
            {
                m_StyleHighlightColorSet = highlightColorSet;
                m_StyleHighlightColor = highlightColorValue;
                shouldRepaint = true;
            }

            if (e.customStyle.TryGetValue(s_ArrowWireColorProperty, out var wireColorValue))
            {
                StyleOuterLineColor = wireColorValue;
                shouldRepaint = true;
            }

            if (e.customStyle.TryGetValue(s_ArrowFillColorProperty, out var fillColorValue))
            {
                StyleFillColor = fillColorValue;
                shouldRepaint = true;
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
                shouldRepaint = true;
                shouldLayout = true; // The layout depends on the line's width
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
                case TransitionView transition:
                {
                    // Transition is vertical pointing down:
                    var center = transition.GetTo() + new Vector2(0, -m_ArrowLength / 2);
                    return (Vector2.up, Vector2.right, center);
                }
                default:
                    return (Vector2.up, Vector2.right, Vector2.down * m_ArrowLength / 2);
            }
        }

        /// <summary>
        /// Computes the arrow's bounding rect in the parent element's coordinate space.
        /// </summary>
        public Rect ComputeBounds()
        {
            if (parent == null)
                return Rect.zero;

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
            return bounds;
        }

        public void UpdateLayout()
        {
            if (parent == null)
                return;

            var bounds = ComputeBounds();
            style.left = bounds.x;
            style.top = bounds.y;
            style.width = bounds.width;
            style.height = bounds.height;

            var tr = GetParentTransform();

            // Position ForwardTipElement at the arrowhead's visual forward tip, including the border.
            var forwardTipOffset = m_ArrowLength / 2 + OuterLineWidth;
            var forwardTipInParent = MathUtils.Multiply2X3(tr, new Vector3(forwardTipOffset, 0, 1));
            ForwardTipElement.style.left = forwardTipInParent.x - bounds.x;
            ForwardTipElement.style.top = forwardTipInParent.y - bounds.y;

            var counter = this.Q<TransitionCounter>();
            counter?.SetCenteringOffset(GetParentTransform().Item1 * CentroidOffsetAlongWire());
        }

        float CentroidOffsetAlongWire()
        {
            var body = m_ArrowLength - m_ArrowTriangleLength;
            var tip = 0.5f * m_ArrowTriangleLength;
            var bodyCenter = -0.5f * m_ArrowTriangleLength;
            var tipCenter = 0.5f * m_ArrowLength - m_ArrowTriangleLength * (2f / 3f);
            return (body * bodyCenter + tip * tipCenter) / (body + tip);
        }

        Color ResolveOuterLineColor()
        {
            return m_StyleHighlightColorSet ? m_StyleHighlightColor : OuterLineColor;
        }

        float ResolveOpacity()
        {
            // A self transition has no line, so the opacity that would normally fade the line fades the arrow's outer
            // line instead. A state-to-state arrow ignores it: its line already carries that signal.
            if (!IsSelfTransition)
                return 1f;

            return m_OpacityMultiplier != 1f ? m_OpacityMultiplier : m_ModelOpacity;
        }

        Gradient ResolveOuterLineGradient(Color outerLineColor)
        {
            if (!IsSelfTransition || !(FlowAnimator?.IsAnimating ?? false))
                return null;

            // A transition has no start/end color distinction, unlike a wire's two port colors, so both
            // gradient keys are the same resolved outer line color.
            return FlowAnimator.BuildStrokeGradient(outerLineColor, outerLineColor, outerLineColor.a);
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (parent == null)
                return false;
            UpdateRenderPoints();
            return IsPointInConvexPolygon(localPoint, m_RenderPoints, OuterLineWidth + m_ArrowBorderRadius);
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
            m_RenderPoints[0] = LocalToLine(middleRight);
            m_RenderPoints[1] = LocalToLine(backRight);
            m_RenderPoints[2] = LocalToLine(backLeft);
            m_RenderPoints[3] = LocalToLine(middleLeft);
            m_RenderPoints[4] = LocalToLine(front);
            return;

            Vector2 LocalToLine(Vector2 localPoint) =>
                parent.ChangeCoordinatesTo(this, MathUtils.Multiply2X3(tr, new Vector3(localPoint.x, localPoint.y, 1)));
        }

        void DrawArrow(MeshGenerationContext mgc)
        {
            UpdateRenderPoints();

            var color = ResolveOuterLineColor();
            var fillColor = FillColor;

            color *= this.GetPlayModeTintColor();
            fillColor *= this.GetPlayModeTintColor();

            color.a *= ResolveOpacity();

            if (m_RenderPoints == null)
                return; // nothing to draw

            var painter = mgc.painter2D;
            painter.fillColor = fillColor;
            painter.lineJoin = LineJoin.Round;

            var outerLineWidth = OuterLineWidth;
            if (outerLineWidth > 0)
            {
                // Set the dash pattern for self transition arrows
                var dashed = IsSelfTransition && IsDashed;
                if (dashed)
                    painter.SetDashPattern(k_DashLength, k_GapLength);

                // Set the gradient animation for self transition arrows
                var outerLineGradient = ResolveOuterLineGradient(color);
                if (outerLineGradient != null)
                    painter.strokeGradient = outerLineGradient;
                else
                    painter.strokeColor = color;

                painter.lineWidth = 2.0f * (outerLineWidth + m_ArrowBorderRadius);

                painter.BeginPath();
                // Reverse draw order to ensure clockwise gradient animation.
                painter.MoveTo(m_RenderPoints[4]);
                for (int i = 3; i >= 0; --i)
                    painter.LineTo(m_RenderPoints[i]);
                painter.ClosePath();
                painter.Stroke();

                if (dashed)
                    painter.SetDashPattern(ReadOnlySpan<float>.Empty);
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

        internal class TestAccess
        {
            readonly TransitionArrow m_Arrow;
            public TestAccess(TransitionArrow arrow) { m_Arrow = arrow; }

            public bool IsFillColorOverridden => m_Arrow.m_FillColorOverride.HasValue;
            public Color ResolveOuterLineColor() => m_Arrow.ResolveOuterLineColor();
            public bool IsHighlighted => m_Arrow.m_StyleHighlightColorSet;
            public float ResolveOpacity() => m_Arrow.ResolveOpacity();
            public Gradient ResolveOuterLineGradient(Color outerLineColor) => m_Arrow.ResolveOuterLineGradient(outerLineColor);
        }
    }
}
