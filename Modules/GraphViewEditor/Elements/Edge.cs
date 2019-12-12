// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;

namespace UnityEditor.Experimental.GraphView
{
    public class Edge : GraphElement
    {
        private const float k_EndPointRadius = 4.0f;
        private const float k_InterceptWidth = 6.0f;
        private static CustomStyleProperty<int> s_EdgeWidthProperty = new CustomStyleProperty<int>("--edge-width");
        private static CustomStyleProperty<Color> s_SelectedEdgeColorProperty = new CustomStyleProperty<Color>("--selected-edge-color");
        private static CustomStyleProperty<Color> s_GhostEdgeColorProperty = new CustomStyleProperty<Color>("--ghost-edge-color");
        private static CustomStyleProperty<Color> s_EdgeColorProperty = new CustomStyleProperty<Color>("--edge-color");

        private static readonly int s_DefaultEdgeWidth = 2;
        private static readonly Color s_DefaultSelectedColor = new Color(240 / 255f, 240 / 255f, 240 / 255f);
        private static readonly Color s_DefaultColor = new Color(146 / 255f, 146 / 255f, 146 / 255f);
        private static readonly Color s_DefaultGhostColor = new Color(85 / 255f, 85 / 255f, 85 / 255f);

        private GraphView m_GraphView;

        private Port m_OutputPort;
        private Port m_InputPort;

        private Vector2 m_CandidatePosition;
        private Vector2 m_GlobalCandidatePosition;

        public bool isGhostEdge { get; set; }

        public Port output
        {
            get { return m_OutputPort; }
            set
            {
                if (m_OutputPort != null && value != m_OutputPort)
                {
                    m_OutputPort.UpdateCapColor();
                    UntrackGraphElement(m_OutputPort);
                }

                if (value != m_OutputPort)
                {
                    m_OutputPort = value;
                    if (m_OutputPort != null)
                    {
                        TrackGraphElement(m_OutputPort);
                    }
                }

                edgeControl.drawFromCap = m_OutputPort == null;
                m_EndPointsDirty = true;
                OnPortChanged(false);
            }
        }

        public override bool showInMiniMap => false;

        public Port input
        {
            get { return m_InputPort; }
            set
            {
                if (m_InputPort != null && value != m_InputPort)
                {
                    m_InputPort.UpdateCapColor();
                    UntrackGraphElement(m_InputPort);
                }

                if (value != m_InputPort)
                {
                    m_InputPort = value;
                    if (m_InputPort != null)
                    {
                        TrackGraphElement(m_InputPort);
                    }
                }
                edgeControl.drawToCap = m_InputPort == null;
                m_EndPointsDirty = true;
                OnPortChanged(true);
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
                if (!Approximately(m_CandidatePosition, value))
                {
                    m_CandidatePosition = value;

                    m_GlobalCandidatePosition = this.WorldToLocal(m_CandidatePosition);

                    if (m_InputPort == null)
                    {
                        edgeControl.to = m_GlobalCandidatePosition;
                    }
                    if (m_OutputPort == null)
                    {
                        edgeControl.from = m_GlobalCandidatePosition;
                    }
                    UpdateEdgeControl();
                }
            }
        }

        int m_EdgeWidth = s_DefaultEdgeWidth;
        public int edgeWidth
        {
            get { return m_EdgeWidth; }
        }

        Color m_SelectedColor = s_DefaultSelectedColor;
        public Color selectedColor
        {
            get { return m_SelectedColor; }
        }

        Color m_DefaultColor = s_DefaultColor;
        public Color defaultColor
        {
            get { return m_DefaultColor; }
        }

        Color m_GhostColor = s_DefaultGhostColor;
        public Color ghostColor
        {
            get { return m_GhostColor; }
        }

        protected Vector2[] PointsAndTangents
        {
            get { return edgeControl.controlPoints; }
        }

        private bool m_EndPointsDirty;

        public Edge()
        {
            ClearClassList();
            AddToClassList("edge");
            style.position = Position.Absolute;

            Add(edgeControl);

            capabilities |= Capabilities.Selectable | Capabilities.Deletable;

            this.AddManipulator(new EdgeManipulator());
            this.AddManipulator(new ContextualMenuManipulator(null));

            RegisterCallback<AttachToPanelEvent>(OnEdgeAttach);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            AddStyleSheetPath("StyleSheets/GraphView/Edge.uss");
        }

        public override bool Overlaps(Rect rectangle)
        {
            if (!UpdateEdgeControl())
                return false;

            return edgeControl.Overlaps(this.ChangeCoordinatesTo(edgeControl, rectangle));
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            Profiler.BeginSample("Edge.ContainsPoint");

            var result = UpdateEdgeControl() &&
                edgeControl.ContainsPoint(this.ChangeCoordinatesTo(edgeControl, localPoint));

            Profiler.EndSample();

            return result;
        }

        public virtual void OnPortChanged(bool isInput)
        {
            edgeControl.outputOrientation = m_OutputPort?.orientation ?? (m_InputPort?.orientation ?? Orientation.Horizontal);
            edgeControl.inputOrientation = m_InputPort?.orientation ?? (m_OutputPort?.orientation ?? Orientation.Horizontal);
            UpdateEdgeControl();
        }

        internal bool ForceUpdateEdgeControl()
        {
            m_EndPointsDirty = true;
            return UpdateEdgeControl();
        }

        public virtual bool UpdateEdgeControl()
        {
            if (m_OutputPort == null && m_InputPort == null)
                return false;

            if (m_GraphView == null)
                m_GraphView = GetFirstOfType<GraphView>();

            if (m_GraphView == null)
                return false;

            UpdateEdgeControlEndPoints();
            edgeControl.UpdateLayout();
            UpdateEdgeControlColorsAndWidth();

            return true;
        }

        protected virtual void DrawEdge() {}

        void UpdateEdgeControlColorsAndWidth()
        {
            if (selected)
            {
                if (isGhostEdge)
                    Debug.Log("Selected Ghost Edge: this should never be");

                edgeControl.inputColor = selectedColor;
                edgeControl.outputColor = selectedColor;
                edgeControl.edgeWidth = edgeWidth;

                if (m_InputPort != null)
                    m_InputPort.capColor = selectedColor;

                if (m_OutputPort != null)
                    m_OutputPort.capColor = selectedColor;
            }
            else
            {
                if (m_InputPort != null)
                    m_InputPort.UpdateCapColor();

                if (m_OutputPort != null)
                    m_OutputPort.UpdateCapColor();

                if (m_InputPort != null)
                    edgeControl.inputColor = m_InputPort.portColor;
                else if (m_OutputPort != null)
                    edgeControl.inputColor = m_OutputPort.portColor;

                if (m_OutputPort != null)
                    edgeControl.outputColor = m_OutputPort.portColor;
                else if (m_InputPort != null)
                    edgeControl.outputColor = m_InputPort.portColor;

                edgeControl.edgeWidth = edgeWidth;

                edgeControl.toCapColor = edgeControl.inputColor;
                edgeControl.fromCapColor = edgeControl.outputColor;

                if (isGhostEdge)
                {
                    edgeControl.inputColor = new Color(edgeControl.inputColor.r, edgeControl.inputColor.g, edgeControl.inputColor.b, 0.5f);
                    edgeControl.outputColor = new Color(edgeControl.outputColor.r, edgeControl.outputColor.g, edgeControl.outputColor.b, 0.5f);
                }
            }
        }

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);

            int edgeWidthValue = 0;
            Color selectColorValue = Color.clear;
            Color edgeColorValue = Color.clear;
            Color ghostColorValue = Color.clear;

            if (styles.TryGetValue(s_EdgeWidthProperty, out edgeWidthValue))
                m_EdgeWidth = edgeWidthValue;

            if (styles.TryGetValue(s_SelectedEdgeColorProperty, out selectColorValue))
                m_SelectedColor = selectColorValue;

            if (styles.TryGetValue(s_EdgeColorProperty, out edgeColorValue))
                m_DefaultColor = edgeColorValue;

            if (styles.TryGetValue(s_GhostEdgeColorProperty, out ghostColorValue))
                m_GhostColor = ghostColorValue;

            UpdateEdgeControlColorsAndWidth();
        }

        public override void OnSelected()
        {
            UpdateEdgeControlColorsAndWidth();
        }

        public override void OnUnselected()
        {
            UpdateEdgeControlColorsAndWidth();
        }

        protected virtual EdgeControl CreateEdgeControl()
        {
            return new EdgeControl
            {
                capRadius = k_EndPointRadius,
                interceptWidth = k_InterceptWidth
            };
        }

        Vector2 GetPortPosition(Port p)
        {
            Vector2 pos = p.GetGlobalCenter();
            pos = this.WorldToLocal(pos);
            return pos;
        }

        void TrackGraphElement(Port port)
        {
            if (port.panel != null) // if the panel is null therefore the port is not yet attached to its hierarchy, so postpone the register
            {
                DoTrackGraphElement(port);
            }

            port.RegisterCallback<AttachToPanelEvent>(OnPortAttach);
            port.RegisterCallback<DetachFromPanelEvent>(OnPortDetach);
        }

        void OnPortDetach(DetachFromPanelEvent e)
        {
            Port port = (Port)e.target;
            DoUntrackGraphElement(port);
        }

        void OnPortAttach(AttachToPanelEvent e)
        {
            Port port = (Port)e.target;
            DoTrackGraphElement(port);
        }

        void OnEdgeAttach(AttachToPanelEvent e)
        {
            UpdateEdgeControl();
        }

        void UntrackGraphElement(Port port)
        {
            port.UnregisterCallback<AttachToPanelEvent>(OnPortAttach);
            port.UnregisterCallback<DetachFromPanelEvent>(OnPortDetach);
            DoUntrackGraphElement(port);
        }

        void DoTrackGraphElement(Port port)
        {
            port.RegisterCallback<GeometryChangedEvent>(OnPortGeometryChanged);

            VisualElement current = port.hierarchy.parent;
            while (current != null)
            {
                if (current is GraphView.Layer)
                {
                    break;
                }
                if (current != port.node) // if we encounter our node ignore it but continue in the case there are nodes inside nodes
                {
                    current.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                }

                current = current.hierarchy.parent;
            }
            if (port.node != null)
                port.node.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void DoUntrackGraphElement(Port port)
        {
            port.UnregisterCallback<GeometryChangedEvent>(OnPortGeometryChanged);

            VisualElement current = port.hierarchy.parent;
            while (current != null)
            {
                if (current is GraphView.Layer)
                {
                    break;
                }
                if (current != port.node) // if we encounter our node ignore it but continue in the case there are nodes inside nodes
                {
                    port.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                }

                current = current.hierarchy.parent;
            }
            if (port.node != null)
                port.node.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnPortGeometryChanged(GeometryChangedEvent evt)
        {
            Port p = evt.target as Port;

            if (p != null)
            {
                if (p == m_InputPort)
                {
                    edgeControl.to = GetPortPosition(p);
                }
                else if (p == m_OutputPort)
                {
                    edgeControl.from = GetPortPosition(p);
                }
            }

            UpdateEdgeControl();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ForceUpdateEdgeControl();
        }

        private void UpdateEdgeControlEndPoints()
        {
            if (!m_EndPointsDirty)
            {
                return;
            }
            Profiler.BeginSample("Edge.UpdateEdgeControlEndPoints");

            m_GlobalCandidatePosition = this.WorldToLocal(m_CandidatePosition);
            if (m_OutputPort != null || m_InputPort != null)
            {
                edgeControl.to = (m_InputPort != null) ? GetPortPosition(m_InputPort) : m_GlobalCandidatePosition;
                edgeControl.from = (m_OutputPort != null) ? GetPortPosition(m_OutputPort) : m_GlobalCandidatePosition;
            }
            m_EndPointsDirty = false;
            Profiler.EndSample();
        }

        static bool Approximately(Vector2 v1, Vector2 v2)
        {
            return Mathf.Approximately(v1.x, v2.x) && Mathf.Approximately(v1.y, v2.y);
        }
    }
}
