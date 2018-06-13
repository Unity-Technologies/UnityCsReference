// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class Edge : GraphElement
    {
        private const float k_EndPointRadius = 4.0f;
        private const float k_InterceptWidth = 6.0f;
        private const string k_EdgeWidthProperty = "edge-width";
        private const string k_SelectedEdgeColorProperty = "selected-edge-color";
        private const string k_GhostEdgeColorProperty = "ghost-edge-color";
        private const string k_EdgeColorProperty = "edge-color";

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
                    MarkDirtyRepaint();
                    UpdateEdgeControl();
                }
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

        private bool m_EndPointsDirty;

        public Edge()
        {
            ClearClassList();
            AddToClassList("edge");
            style.positionType = PositionType.Absolute;

            Add(edgeControl);

            capabilities |= Capabilities.Selectable | Capabilities.Deletable;

            this.AddManipulator(new EdgeManipulator());
            this.AddManipulator(new ContextualMenuManipulator(null));

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
            m_EndPointsDirty = true;
            if (!UpdateEdgeControl())
            {
                Profiler.EndSample();
                return false;
            }

            bool result =  edgeControl.ContainsPoint(this.ChangeCoordinatesTo(edgeControl, localPoint));

            Profiler.EndSample();

            return result;
        }

        public virtual void OnPortChanged(bool isInput)
        {
            edgeControl.outputOrientation = m_OutputPort?.orientation ?? (m_InputPort?.orientation ?? Orientation.Horizontal);
            edgeControl.inputOrientation = m_InputPort?.orientation ?? (m_OutputPort?.orientation ?? Orientation.Horizontal);
        }

        public bool UpdateEdgeControl()
        {
            // bounding box check succeeded, do more fine grained check by measuring distance to bezier points

            if (m_OutputPort == null && m_InputPort == null)
                return false;

            if (m_GraphView == null)
                m_GraphView = GetFirstOfType<GraphView>();

            if (m_GraphView == null)
                return false;

            UpdateEndPoints();
            edgeControl.UpdateLayout();

            return true;
        }

        protected override void DoRepaint(IStylePainter painter)
        {
            // Edges do NOT call base.DoRepaint. It would create a visual artifact.
            DrawEdge();
        }

        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);

            styles.ApplyCustomProperty(k_EdgeWidthProperty, ref m_EdgeWidth);
            styles.ApplyCustomProperty(k_SelectedEdgeColorProperty, ref m_SelectedColor);
            styles.ApplyCustomProperty(k_GhostEdgeColorProperty, ref m_GhostColor);
            styles.ApplyCustomProperty(k_EdgeColorProperty, ref m_DefaultColor);
        }

        protected virtual void DrawEdge()
        {
            if (!UpdateEdgeControl())
                return;

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

                edgeControl.inputColor = m_InputPort == null ? m_OutputPort.portColor : m_InputPort.portColor;
                edgeControl.outputColor = m_OutputPort == null ? m_InputPort.portColor : m_OutputPort.portColor;
                edgeControl.edgeWidth = edgeWidth;

                edgeControl.toCapColor = m_InputPort == null ? m_OutputPort.portColor : m_InputPort.portColor;
                edgeControl.fromCapColor = m_OutputPort == null ? m_InputPort.portColor : m_OutputPort.portColor;

                if (isGhostEdge)
                {
                    edgeControl.inputColor = new Color(edgeControl.inputColor.r, edgeControl.inputColor.g, edgeControl.inputColor.b, 0.5f);
                    edgeControl.outputColor = new Color(edgeControl.outputColor.r, edgeControl.outputColor.g, edgeControl.outputColor.b, 0.5f);
                }
            }
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

        void TrackGraphElement(VisualElement e)
        {
            while (e != null)
            {
                if (e is GraphView.Layer)
                {
                    return;
                }

                if (e is Port)
                {
                    e.RegisterCallback<GeometryChangedEvent>(OnPortGeometryChanged);
                }
                else
                {
                    e.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                }


                e = e.shadow.parent;
            }
        }

        void UntrackGraphElement(VisualElement e)
        {
            while (e != null)
            {
                if (e is GraphView.Layer)
                {
                    return;
                }

                if (e is Port)
                {
                    e.UnregisterCallback<GeometryChangedEvent>(OnPortGeometryChanged);
                }
                else
                {
                    e.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                }

                e = e.shadow.parent;
            }
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
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            m_EndPointsDirty = true;
        }

        private void UpdateEndPoints()
        {
            if (!m_EndPointsDirty)
            {
                return;
            }
            Profiler.BeginSample("Edge.UpdateEndPoints");

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
