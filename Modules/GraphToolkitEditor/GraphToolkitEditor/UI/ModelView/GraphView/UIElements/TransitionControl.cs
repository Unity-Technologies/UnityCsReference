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
    /// The visual element that renders a transition.
    /// </summary>
    [UnityRestricted]
    internal class TransitionControl : VisualElement
    {
        static CustomStyleProperty<float> s_TransitionWidthProperty = new("--wire-width");
        static CustomStyleProperty<float> s_TransitionPaddingProperty = new("--wire-padding");
        static CustomStyleProperty<Color> s_TransitionColorProperty = new("--wire-color");

        static readonly float k_DefaultPadding = 10.0f;

        protected Transition m_Transition;

        // The points that will be rendered. Expressed in coordinates local to the element.
        protected Vector2[] m_ControlPoints = new Vector2[4];

        bool IsSelfTransition => m_Transition?.TransitionModel?.IsSingleStateTransition ?? false;

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

        float m_Zoom = 1.0f;

        protected Color m_Color = Color.grey;
        protected bool m_ColorOverridden;

        protected float m_LineWidth = WireUtilities.DefaultWireWidth;
        protected bool m_WidthOverridden;

        protected float m_Padding = k_DefaultPadding;
        protected bool m_PaddingOverridden;

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
        /// The color of the wire.
        /// </summary>
        public Color Color
        {
            get => m_Color;
            set
            {
                m_ColorOverridden = true;

                if (m_Color != value)
                {
                    m_Color = value;
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
        /// The length of the line coming straight out of the node.
        /// </summary>
        public float Padding
        {
            get => m_Padding;
            set
            {
                m_PaddingOverridden = true;

                if (Math.Abs(m_Padding - value) < 0.05)
                    return;

                m_Padding = value;
                UpdateLayout();
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The transition arrow.
        /// </summary>
        public TransitionArrow TransitionArrow { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionControl"/> class.
        /// </summary>
        /// <param name="transition">The transition this control is attached to.</param>
        public TransitionControl(Transition transition)
        {
            m_Transition = transition;
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Position;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (e.customStyle.TryGetValue(s_TransitionWidthProperty, out var wireWidthValue))
                StyleLineWidth = wireWidthValue;

            if (e.customStyle.TryGetValue(s_TransitionColorProperty, out var wireColorValue))
                StyleColor = wireColorValue;

            if (e.customStyle.TryGetValue(s_TransitionPaddingProperty, out var paddingValue))
                StylePadding = paddingValue;

            var updateLayout = false;
            var repaint = false;
            if (!m_WidthOverridden)
            {
                m_LineWidth = StyleLineWidth;
                updateLayout = true; // The layout depends on the wires width
                repaint = true;
            }

            if (!m_ColorOverridden)
            {
                m_Color = StyleColor;
                repaint = true;
            }

            if (!m_PaddingOverridden)
            {
                m_Padding = StylePadding;
                updateLayout = true;
                repaint = true;
            }

            if (updateLayout)
                UpdateLayout();

            if (repaint)
                MarkDirtyRepaint();
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
        /// Resets the color to the default value.
        /// </summary>
        public void ResetColor()
        {
            m_ColorOverridden = false;
            Color = StyleColor;
        }

        /// <summary>
        /// Resets the padding to the default value.
        /// </summary>
        public void ResetPadding()
        {
            m_PaddingOverridden = false;
            Padding = StylePadding;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            DrawWire(mgc);
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

            Color color = Color;

            color *= this.GetPlayModeTintColor();

            var painter2D = mgc.painter2D;

            float width = LineWidth;
            if (LineWidth * Zoom < k_MinWireWidth)
                width = k_MinWireWidth / Zoom;

            painter2D.BeginPath();
            painter2D.strokeColor = color;
            painter2D.miterLimit = 2;

            painter2D.lineWidth = width;
            painter2D.MoveTo(parent.ChangeCoordinatesTo(this, m_ControlPoints[0]));

            for (int i = 1; i < 4; ++i)
                if ((m_ControlPoints[i - 1] - m_ControlPoints[i]).sqrMagnitude > 0.1f * 0.1f)
                    painter2D.LineTo(parent.ChangeCoordinatesTo(this, m_ControlPoints[i]));

            painter2D.Stroke();
        }
    }
}
