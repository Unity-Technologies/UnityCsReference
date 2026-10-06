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
    /// The visual element that renders a transition.
    /// </summary>
    [UnityRestricted]
    internal class TransitionControl : VisualElement
    {
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> s_TransitionWidthProperty = new("--wire-width");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<float> s_TransitionPaddingProperty = new("--wire-padding");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<Color> s_TransitionColorProperty = new("--wire-color");
        [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
        static readonly CustomStyleProperty<Color> s_TransitionHighlightColorProperty = new("--wire-highlight-color");

        static readonly float k_DefaultPadding = 10.0f;

        protected TransitionView m_Transition;

        // The points that will be rendered. Expressed in coordinates local to the element.
        protected Vector2[] m_ControlPoints = new Vector2[4];

        bool IsSelfTransition => m_Transition?.TransitionModel?.IsSelfTransition ?? false;

        bool ShowCounter => m_Transition.TransitionModel?.Transitions.Count > 1;

        float TargetStateTransitionHeight => ShowCounter ? 30.0f : 20.0f;

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

        static readonly float k_TargetStateTransitionPadding = 10.0f;
        static readonly float k_LineSelectionPadding = 7.5f;

        const float k_MinWireWidth = 1.75f;
        const float k_DashLength = 10f;
        const float k_GapLength = 6f;

        float m_Zoom = 1.0f;

        Color? m_ColorOverride;
        Color? m_ModelColor;

        // The highlight color the style sheet resolves under :hover and :checked.
        Color m_StyleHighlightColor;
        bool m_StyleHighlightColorSet;

        float? m_LineWidthOverride;
        float? m_ModelWidth;

        float? m_PaddingOverride;

        bool? m_IsDashedOverride;

        protected float m_OpacityMultiplier = 1f;

        protected float StyleLineWidth { get; set; } = WireUtilities.DefaultWireWidth;

        protected Color StyleColor { get; set; } = WireUtilities.DefaultWireColor;

        protected float StylePadding { get; set; } = k_DefaultPadding;

        // The start of the wire in graph coordinates.
        Vector2 From
        {
            get
            {
                if (IsSelfTransition)
                {
                    var toPt = m_Transition.GetTo();
                    return new Vector2(toPt.x, toPt.y - (TargetStateTransitionHeight + k_TargetStateTransitionPadding));
                }
                return m_Transition.GetFrom();
            }
        }

        // The end of the wire in graph coordinates.
        Vector2 To => m_Transition.GetTo();

        /// <summary>
        /// The color of the wire: the override when one is set, else the model color, else the USS value.
        /// </summary>
        public Color Color => m_ColorOverride ?? m_ModelColor ?? StyleColor;

        /// <summary>
        /// The wire color override. Set to null to fall back to <see cref="ModelColor"/> or the USS value.
        /// </summary>
        public Color? ColorOverride
        {
            get => m_ColorOverride;
            set
            {
                if (m_ColorOverride == value)
                    return;

                m_ColorOverride = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The wire color read from the transition model, used when no <see cref="ColorOverride"/> is set. Set to null
        /// to fall back to the USS value.
        /// </summary>
        public Color? ModelColor
        {
            get => m_ModelColor;
            set
            {
                if (m_ModelColor == value)
                    return;

                m_ModelColor = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The width of the wire: the override when one is set, else the model width, else the USS value.
        /// </summary>
        public float LineWidth => m_LineWidthOverride ?? m_ModelWidth ?? StyleLineWidth;

        /// <summary>
        /// The wire width override. Set to null to fall back to <see cref="ModelWidth"/> or the USS value.
        /// </summary>
        public float? LineWidthOverride
        {
            get => m_LineWidthOverride;
            set
            {
                if (m_LineWidthOverride == value)
                    return;

                m_LineWidthOverride = value;
                UpdateLayout(); // The layout depends on the wire's width
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The wire width read from the transition model, used when no <see cref="LineWidthOverride"/> is set. Set to
        /// null to fall back to the USS value.
        /// </summary>
        public float? ModelWidth
        {
            get => m_ModelWidth;
            set
            {
                if (m_ModelWidth == value)
                    return;

                m_ModelWidth = value;
                UpdateLayout(); // The layout depends on the wire's width
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Whether the wire is drawn with a dashed pattern: the override when one is set, else the model value.
        /// </summary>
        public bool IsDashed => m_IsDashedOverride ?? (m_Transition?.WireModel?.IsDashed ?? false);

        /// <summary>
        /// The dashed state override. Set to null to fall back to the model value.
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
        /// The opacity multiplier of the wire.
        /// </summary>
        /// <remarks>The opacity multiplier is clamped to the [0, 1] range</remarks>
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

        /// <summary>
        /// The length of the line coming straight out of the node: the override when one is set, else the USS value.
        /// </summary>
        public float Padding => m_PaddingOverride ?? StylePadding;

        /// <summary>
        /// The padding override. Set to null to fall back to the USS value.
        /// </summary>
        public float? PaddingOverride
        {
            get => m_PaddingOverride;
            set
            {
                if (m_PaddingOverride == value)
                    return;

                m_PaddingOverride = value;
                UpdateLayout();
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The transition arrow.
        /// </summary>
        public TransitionArrow TransitionArrow { get; set; }

        /// <summary>
        /// The gradient flow animator that drives this transition's line sweep, owned by the parent <see cref="TransitionView"/>
        /// so its arrow shares the same clock.
        /// </summary>
        internal GradientFlowAnimator FlowAnimator => m_Transition.FlowAnimator;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionControl"/> class.
        /// </summary>
        /// <param name="transition">The transition this control is attached to.</param>
        public TransitionControl(TransitionView transition)
        {
            m_Transition = transition;
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Position;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var updateLayout = false;
            var repaint = false;

            if (e.customStyle.TryGetValue(s_TransitionWidthProperty, out var wireWidthValue))
            {
                StyleLineWidth = wireWidthValue;
                updateLayout = true; // The layout depends on the wire's width
                repaint = true;
            }

            if (e.customStyle.TryGetValue(s_TransitionColorProperty, out var wireColorValue))
            {
                StyleColor = wireColorValue;
                repaint = true;
            }

            if (e.customStyle.TryGetValue(s_TransitionPaddingProperty, out var paddingValue))
            {
                StylePadding = paddingValue;
                updateLayout = true;
                repaint = true;
            }

            var highlightColorSet = e.customStyle.TryGetValue(s_TransitionHighlightColorProperty, out var highlightColorValue);
            if (highlightColorSet != m_StyleHighlightColorSet || m_StyleHighlightColor != highlightColorValue)
            {
                m_StyleHighlightColorSet = highlightColorSet;
                m_StyleHighlightColor = highlightColorValue;
                repaint = true;
            }

            if (updateLayout)
                UpdateLayout();

            if (repaint)
                MarkDirtyRepaint();
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            DrawWire(mgc);
        }

        Color ResolveColor()
        {
            return m_StyleHighlightColorSet ? m_StyleHighlightColor : Color;
        }

        float ResolveOpacity()
        {
            return !Mathf.Approximately(m_OpacityMultiplier, 1f) ? m_OpacityMultiplier : m_Transition.WireModel.Opacity;
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (TransitionArrow != null && TransitionArrow.ContainsPoint(this.ChangeCoordinatesTo(TransitionArrow, localPoint)))
            {
                return true;
            }

            return base.ContainsPoint(localPoint) &&
                WireUtilities.IsPointOnLine(this.ChangeCoordinatesTo(parent, localPoint), m_ControlPoints, LineWidth / 2 + k_LineSelectionPadding);
        }

        /// <inheritdoc />
        public override bool Overlaps(Rect rect)
        {
            return base.Overlaps(rect) && WireUtilities.RectIntersectsLine(this.ChangeCoordinatesTo(parent, rect), m_ControlPoints);
        }

        /// <summary>
        /// Recomputes the layout of the wire.
        /// </summary>
        public void UpdateLayout()
        {
            if (parent != null)
                ComputeLayout();
        }

        Vector2 GetFromDirection(Vector2 fromPoint, Vector2 toPoint)
        {
            var fromSide = m_Transition.TransitionModel?.FromNodeAnchorSide ?? AnchorSide.None;
            return fromSide switch
            {
                AnchorSide.Top => Vector2.down,
                AnchorSide.Right => Vector2.right,
                AnchorSide.Bottom => Vector2.up,
                AnchorSide.Left => Vector2.left,
                _ => (toPoint - fromPoint).normalized,
            };
        }

        Vector2 GetToDirection(Vector2 fromPoint, Vector2 toPoint)
        {
            var toSide = m_Transition.TransitionModel?.ToNodeAnchorSide ?? AnchorSide.None;
            return toSide switch
            {
                AnchorSide.Top => Vector2.down,
                AnchorSide.Right => Vector2.right,
                AnchorSide.Bottom => Vector2.up,
                AnchorSide.Left => Vector2.left,
                _ => (fromPoint - toPoint).normalized,
            };
        }

        // Returns a float 2x3 matrix that transforms a vector 2 from the middle of the transition to the local space
        // of the transition.
        internal (Vector2, Vector2, Vector2) GetMiddleToLocal()
        {
            var fromPoint = parent.ChangeCoordinatesTo(this, From);
            var toPoint = parent.ChangeCoordinatesTo(this, To);

            if (Vector2.Distance(fromPoint, toPoint) > Padding * 2)
            {
                var fromDirection = GetFromDirection(fromPoint, toPoint);
                var toDirection = GetToDirection(fromPoint, toPoint);

                fromPoint += fromDirection * Padding;
                toPoint += toDirection * Padding;
            }

            var arrowMiddle = (fromPoint + toPoint) / 2;
            Vector2 dir = toPoint - fromPoint;
            var dirNormalized = dir.normalized;
            var perpendicularDirNormalized = new Vector2(-dirNormalized.y, dirNormalized.x);

            return (dirNormalized, perpendicularDirNormalized, arrowMiddle);
        }

        void UpdateRenderPoints()
        {
            var fromPoint = From;
            var toPoint = To;

            if (m_ControlPoints == null || m_ControlPoints.Length != 4)
                m_ControlPoints = new Vector2[4];

            if (m_Transition.Model is not IGhostWireModel && IsSelfTransition)
            {
                return;
            }

            var fromDirection = GetFromDirection(fromPoint, toPoint);
            var toDirection = GetToDirection(fromPoint, toPoint);

            m_ControlPoints[0] = fromPoint + fromDirection;
            m_ControlPoints[1] = fromPoint + fromDirection * Padding;
            m_ControlPoints[2] = toPoint + toDirection * Padding;
            m_ControlPoints[3] = toPoint + toDirection;
        }

        const float k_WidthOnEachSideOfTargetStateTransition = 7.5f;

        void ComputeLayout()
        {
            UpdateRenderPoints();

            // Compute VisualElement position and dimension.
            var transitionModel = m_Transition.WireModel;

            if (transitionModel == null)
            {
                style.top = 0;
                style.left = 0;
                style.width = 0;
                style.height = 0;
                return;
            }

            var rect = new Rect
            {
                xMin = Math.Min(From.x, To.x),
                xMax = Math.Max(From.x, To.x),
                yMin = Math.Min(From.y, To.y),
                yMax = Math.Max(From.y, To.y)
            };

            var p = rect.position;
            var dim = rect.size;

            if (IsSelfTransition)
            {
                p.x -= k_WidthOnEachSideOfTargetStateTransition;
                dim.x += k_WidthOnEachSideOfTargetStateTransition * 2;
            }
            else
            {
                var width = LineWidth + 2 * k_LineSelectionPadding + Padding * 2;
                p.x -= width * 0.5f;
                dim.x += width;
                p.y -= width * 0.5f;
                dim.y += width;
            }

            style.left = p.x;
            style.top = p.y;
            style.width = dim.x;
            style.height = dim.y;
        }

        void DrawWire(MeshGenerationContext mgc)
        {
            if (LineWidth <= 0)
                return;

            var color = ResolveColor();

            color *= this.GetPlayModeTintColor();

            var painter2D = mgc.painter2D;

            if (IsDashed)
                painter2D.SetDashPattern(k_DashLength, k_GapLength);

            float width = LineWidth;

            if (width * Zoom < k_MinWireWidth)
                width = k_MinWireWidth / Zoom;

            color.a *= ResolveOpacity();

            painter2D.BeginPath();

            if (FlowAnimator.IsAnimating)
                painter2D.strokeGradient = FlowAnimator.BuildStrokeGradient(color, color, color.a);
            else
                painter2D.strokeColor = color;

            painter2D.miterLimit = 2;

            painter2D.lineWidth = width;
            painter2D.MoveTo(parent.ChangeCoordinatesTo(this, m_ControlPoints[0]));

            for (int i = 1; i < 4; ++i)
                if ((m_ControlPoints[i - 1] - m_ControlPoints[i]).sqrMagnitude > 0.1f * 0.1f)
                    painter2D.LineTo(parent.ChangeCoordinatesTo(this, m_ControlPoints[i]));

            painter2D.Stroke();
        }

        internal class TestAccess
        {
            readonly TransitionControl m_TransitionControl;
            public TestAccess(TransitionControl transitionControl) { m_TransitionControl = transitionControl; }

            public Color ResolveColor() => m_TransitionControl.ResolveColor();
            public float ResolveOpacity() => m_TransitionControl.ResolveOpacity();
            public bool IsHighlighted => m_TransitionControl.m_StyleHighlightColorSet;
        }
    }
}
