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
        private const float k_InterceptWidth = 6.0f;
        private const string k_EdgeWidthProperty = "edge-width";
        private const string k_SelectedEdgeColorProperty = "selected-edge-color";
        private const string k_GhostEdgeColorProperty = "ghost-edge-color";
        private const string k_EdgeColorProperty = "edge-color";

        private GraphView m_GraphView;

        private NodeAnchor m_OutputAnchor;
        private NodeAnchor m_InputAnchor;

        private Vector2 m_CandidatePosition;

        public bool isGhostEdge { get; set; }

        public NodeAnchor output
        {
            get { return m_OutputAnchor; }
            set
            {
                m_OutputAnchor = value;
                OnAnchorChanged(false);
            }
        }

        public NodeAnchor input
        {
            get { return m_InputAnchor; }
            set
            {
                m_InputAnchor = value;
                OnAnchorChanged(true);
            }
        }

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

        public Vector2 candidatePosition
        {
            get { return m_CandidatePosition; }
            set
            {
                m_CandidatePosition = value;
                UpdateEdgeControl();
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

        StyleValue<Color> m_GhostColor;
        public Color ghostColor
        {
            get
            {
                return m_GhostColor.GetSpecifiedValueOrDefault(new Color(85 / 255f, 85 / 255f, 85 / 255f));
            }
        }

        protected Vector2[] PointsAndTangents
        {
            get { return edgeControl.controlPoints; }
        }

        public Edge()
        {
            clippingOptions = ClippingOptions.NoClipping;

            ClearClassList();
            AddToClassList("edge");

            Add(edgeControl);

            this.AddManipulator(new EdgeManipulator());
            capabilities |= Capabilities.Selectable | Capabilities.Deletable;
        }

        public override bool Overlaps(Rect rectangle)
        {
            if (!UpdateEdgeControl())
                return false;

            return edgeControl.Overlaps(this.ChangeCoordinatesTo(edgeControl, rectangle));
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            if (!UpdateEdgeControl())
                return false;

            return edgeControl.ContainsPoint(this.ChangeCoordinatesTo(edgeControl, localPoint));
        }

        public virtual void OnAnchorChanged(bool isInput)
        {
        }

        public bool UpdateEdgeControl()
        {
            // bounding box check succeeded, do more fine grained check by measuring distance to bezier points

            if (m_OutputAnchor == null && m_InputAnchor == null)
                return false;

            if (m_GraphView == null)
                m_GraphView = GetFirstOfType<GraphView>();

            if (m_GraphView == null)
                return false;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(ref from, ref to);

            edgeControl.from = from;
            edgeControl.to = to;

            edgeControl.UpdateLayout();

            return true;
        }

        public override void DoRepaint()
        {
            // Edges do NOT call base.DoRepaint. It would create a visual artifact.
            DrawEdge();
        }

        protected void GetFromToPoints(ref Vector2 from, ref Vector2 to)
        {
            if (m_OutputAnchor == null && m_InputAnchor == null)
            {
                return;
            }

            if (m_GraphView == null)
                m_GraphView = GetFirstOfType<GraphView>();

            if (m_OutputAnchor != null)
            {
                from = m_OutputAnchor.GetGlobalCenter();
                from = this.WorldToLocal(from);
            }
            else
            {
                from = this.WorldToLocal(new Vector2(m_CandidatePosition.x, m_CandidatePosition.y));
            }

            if (m_InputAnchor != null)
            {
                to = m_InputAnchor.GetGlobalCenter();
                to = this.WorldToLocal(to);
            }
            else
            {
                to = this.WorldToLocal(new Vector2(m_CandidatePosition.x, m_CandidatePosition.y));
            }
        }

        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);

            styles.ApplyCustomProperty(k_EdgeWidthProperty, ref m_EdgeWidth);
            styles.ApplyCustomProperty(k_SelectedEdgeColorProperty, ref m_SelectedColor);
            styles.ApplyCustomProperty(k_GhostEdgeColorProperty, ref m_GhostColor);
            styles.ApplyCustomProperty(k_EdgeColorProperty, ref m_DefaultColor);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            EdgePresenter edgePresenter = GetPresenter<EdgePresenter>();
            if (edgePresenter != null)
            {
                if (output == null || output.presenter != edgePresenter.output)
                {
                    GraphView view = GetFirstAncestorOfType<GraphView>();
                    if (view != null)
                    {
                        output = view.Query().OfType<NodeAnchor>().Where(t => t.presenter == edgePresenter.output);
                    }
                }
                if (input == null || input.presenter != edgePresenter.input)
                {
                    GraphView view = GetFirstAncestorOfType<GraphView>();
                    if (view != null)
                    {
                        input = view.Query().OfType<NodeAnchor>().Where(t => t.presenter == edgePresenter.input);
                    }
                }
                if (edgePresenter.output != null || edgePresenter.input != null)
                    edgeControl.orientation = edgePresenter.output != null ? edgePresenter.output.orientation : edgePresenter.input.orientation;
            }
        }

        protected virtual void DrawEdge()
        {
            if (!UpdateEdgeControl())
                return;

            Color edgeColor = isGhostEdge ? ghostColor : (selected ? selectedColor : defaultColor);
            edgeControl.edgeColor = edgeColor;
            edgeControl.startCapColor = edgeColor;
            edgeControl.edgeWidth = edgeWidth;

            var edgePresenter = GetPresenter<EdgePresenter>();
            if (edgePresenter == null)
                edgeControl.endCapColor = m_InputAnchor == null ? edgeControl.startCapColor : edgeColor;
            else
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
