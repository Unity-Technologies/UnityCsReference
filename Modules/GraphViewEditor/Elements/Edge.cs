// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class Edge : GraphElement
    {
        private const float k_EndPointRadius = 4.0f;
        private const float k_InterceptWidth = 3.0f;
        private const string k_EdgeWidthProperty = "edge-width";
        private const string k_SelectedEdgeColorProperty = "selected-edge-color";
        private const string k_EdgeColorProperty = "edge-color";

        private GraphView m_GraphView;

        private NodeAnchorPresenter m_OutputPresenter;
        private NodeAnchorPresenter m_InputPresenter;
        private NodeAnchor m_LeftAnchor;
        private NodeAnchor m_RightAnchor;

        EdgeControl m_EdgeControl;
        public EdgeControl edgeControl
        {
            get
            {
                if (m_EdgeControl == null)
                {
                    m_EdgeControl = CreateEdgeControl();
                }
                return m_EdgeControl;
            }
        }

        StyleValue<int> m_EdgeWidth;
        public int edgeWidth
        {
            get
            {
                return m_EdgeWidth.GetSpecifiedValueOrDefault(2);
            }
        }

        StyleValue<Color> m_SelectedColor;
        public Color selectedColor
        {
            get
            {
                return m_SelectedColor.GetSpecifiedValueOrDefault(new Color(240 / 255f, 240 / 255f, 240 / 255f));
            }
        }

        StyleValue<Color> m_DefaultColor;
        public Color defaultColor
        {
            get
            {
                return m_DefaultColor.GetSpecifiedValueOrDefault(new Color(146 / 255f, 146 / 255f, 146 / 255f));
            }
        }

        protected Vector3[] PointsAndTangents
        {
            get { return edgeControl.controlPoints; }
        }

        public Edge()
        {
            clippingOptions = ClippingOptions.NoClipping;

            ClearClassList();
            AddToClassList("edge");

            Add(edgeControl);
        }

        public override bool Overlaps(Rect rectangle)
        {
            if (!UpdateEdgeControl())
                return false;

            return edgeControl.Overlaps(rectangle);
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (!UpdateEdgeControl())
                return false;

            return edgeControl.ContainsPoint(localPoint);
        }

        protected bool UpdateEdgeControl()
        {
            // bounding box check succeeded, do more fine grained check by measuring distance to bezier points
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;

            if (outputPresenter == null && inputPresenter == null)
                return false;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(edgePresenter, outputPresenter, inputPresenter, ref from, ref to);
            edgeControl.from = from;
            edgeControl.to = to;
            edgeControl.orientation = outputPresenter != null ? outputPresenter.orientation : inputPresenter.orientation;

            return true;
        }

        public override void DoRepaint()
        {
            // Edges do NOT call base.DoRepaint. It would create a visual artifact.
            DrawEdge();
        }

        protected void GetFromToPoints(EdgePresenter edgePresenter, NodeAnchorPresenter outputPresenter, NodeAnchorPresenter inputPresenter, ref Vector2 from, ref Vector2 to)
        {
            if (outputPresenter == null && inputPresenter == null)
                return;

            if (m_GraphView == null)
                m_GraphView = GetFirstOfType<GraphView>();

            if (outputPresenter != null)
            {
                if (m_OutputPresenter != outputPresenter)
                {
                    m_LeftAnchor = m_GraphView.Query<NodeAnchor>().Where(e => e.direction == Direction.Output && e.GetPresenter<NodeAnchorPresenter>() == outputPresenter).First();
                    m_OutputPresenter = outputPresenter;
                }

                if (m_LeftAnchor != null)
                {
                    from = m_LeftAnchor.GetGlobalCenter();
                    from = worldTransform.inverse.MultiplyPoint3x4(from);
                }
            }
            else
            {
                from = worldTransform.inverse.MultiplyPoint3x4(new Vector3(edgePresenter.candidatePosition.x, edgePresenter.candidatePosition.y));
            }

            if (inputPresenter != null)
            {
                if (m_InputPresenter != inputPresenter)
                {
                    m_RightAnchor = m_GraphView.Query<NodeAnchor>().Where(e => e.direction == Direction.Input && e.GetPresenter<NodeAnchorPresenter>() == inputPresenter).First();
                    m_InputPresenter = inputPresenter;
                }

                if (m_RightAnchor != null)
                {
                    to = m_RightAnchor.GetGlobalCenter();
                    to = worldTransform.inverse.MultiplyPoint3x4(to);
                }
            }
            else
            {
                to = worldTransform.inverse.MultiplyPoint3x4(new Vector3(edgePresenter.candidatePosition.x, edgePresenter.candidatePosition.y));
            }
        }

        public override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);

            styles.ApplyCustomProperty(k_EdgeWidthProperty, ref m_EdgeWidth);
            styles.ApplyCustomProperty(k_SelectedEdgeColorProperty, ref m_SelectedColor);
            styles.ApplyCustomProperty(k_EdgeColorProperty, ref m_DefaultColor);
        }

        protected virtual void DrawEdge()
        {
            if (!UpdateEdgeControl())
                return;

            var edgePresenter = GetPresenter<EdgePresenter>();

            Color edgeColor = edgePresenter.selected ? selectedColor : defaultColor;
            edgeControl.edgeColor = edgeColor;
            edgeControl.startCapColor = edgeColor;
            edgeControl.endCapColor = edgePresenter.input == null ? edgeControl.startCapColor : edgeColor;
        }

        protected virtual EdgeControl CreateEdgeControl()
        {
            return new EdgeControl
            {
                capRadius = k_EndPointRadius,
                interceptWidth = k_InterceptWidth
            };
        }
    }
}
