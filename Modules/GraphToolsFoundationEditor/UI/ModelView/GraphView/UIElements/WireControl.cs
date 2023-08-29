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
        static readonly CustomStyleProperty<int> k_WireWidthProperty = new CustomStyleProperty<int>("--wire-width");
        static readonly CustomStyleProperty<Color> k_WireColorProperty = new CustomStyleProperty<Color>("--wire-color");
        static readonly Gradient k_Gradient = new Gradient();

        static int DefaultWireWidth => 2;

        static Color DefaultWireColor
        {
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
        const float k_WireTurnRadius = k_WireTurnDiameter * 0.5f;
        const float k_MinWireWidth = 1.75f;
        const float k_MinOpacity = 0.6f;

        protected Wire m_Wire;

        float m_Zoom = 1.0f;

        /// <summary>
        /// The control points of the wire expressed in the parent <see cref="Wire"/> coordinates.
        /// </summary>
        protected Vector2[] m_ControlPoints = new Vector2[4];

        protected PortOrientation m_InputOrientation;

        protected PortOrientation m_OutputOrientation;

        protected Color m_InputColor = Color.grey;

        protected Color m_OutputColor = Color.grey;

        protected bool m_ColorOverridden;

        protected bool m_WidthOverridden;

        protected int m_LineWidth = DefaultWireWidth;

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

            return MatchControlPoints(this.ChangeCoordinatesTo(parent, localPoint));
        }

        public bool MatchControlPoints(Vector2 parentPoint)
        {
            for (var index = 0; index < m_ControlPoints.Length - 1; index++)
            {
                var a = m_ControlPoints[index];
                var b = m_ControlPoints[index + 1];
                var squareDistance = SquaredDistanceToSegment(parentPoint, a, b);
                if (squareDistance < (LineWidth + 1) * (LineWidth + 1))
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
                return MatchControlPoints(this.ChangeCoordinatesTo(parent, r));
            }

            return false;
        }

        public bool MatchControlPoints(Rect r)
        {
            for (int a = 0; a < m_ControlPoints.Length - 1; a++)
            {
                if (RectUtils_Internal.IntersectsSegment(r, m_ControlPoints[a], m_ControlPoints[a + 1]))
                    return true;
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

        void AssignControlPoint(ref Vector2 destination, Vector2 newValue)
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


        static GradientColorKey[] s_ColorKeys = new GradientColorKey[2];
        static GradientAlphaKey[] s_AlphaKeys = new GradientAlphaKey[1];

        protected void DrawWire(MeshGenerationContext mgc)
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawWire");

            if (LineWidth <= 0)
                return;

            Color inColor = InputColor;
            Color outColor = OutputColor;

            inColor *= playModeTintColor;
            outColor *= playModeTintColor;

            var painter2D = mgc.painter2D;

            float width = StyleLineWidth;
            float alpha = 1.0f;

            if (StyleLineWidth * Zoom < k_MinWireWidth)
            {
                float t = StyleLineWidth * Zoom / k_MinWireWidth;

                alpha = Mathf.Lerp(k_MinOpacity,1.0f, t);
                width = k_MinWireWidth / Zoom;
            }

            s_ColorKeys[0] = new GradientColorKey(outColor, 0);
            s_ColorKeys[1] = new GradientColorKey(inColor, 1);

            s_AlphaKeys[0] = new GradientAlphaKey(alpha, 0);

            k_Gradient.SetKeys(s_ColorKeys,s_AlphaKeys);
            painter2D.BeginPath();
            painter2D.strokeGradient = k_Gradient;

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

            painter2D.lineWidth = width;
            painter2D.MoveTo(p1);
            painter2D.LineTo(p2 - (p2 - p1).normalized * k_WireTurnRadius);

            var slopeDirection = (p2 - p3).normalized;
            painter2D.BezierCurveTo(p2, p2, p2 - slopeDirection * k_WireTurnRadius);
            painter2D.LineTo(p3 + slopeDirection * k_WireTurnRadius);
            painter2D.BezierCurveTo(p3, p3, p4 - (p4 - p3).normalized * k_WireTurnRadius);
            painter2D.LineTo(p4);
            painter2D.Stroke();

            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
