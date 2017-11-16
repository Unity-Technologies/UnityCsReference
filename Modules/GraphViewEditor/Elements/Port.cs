// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class Port : GraphElement
    {
        protected EdgeConnector m_EdgeConnector;

        protected VisualElement m_ConnectorBox;
        protected VisualElement m_ConnectorText;

        protected Color m_HoleColor;
        protected VisualElement m_ConnectorBoxCap;

        internal Color capColor
        {
            get
            {
                if (m_ConnectorBoxCap == null)
                    return Color.black;
                return m_ConnectorBoxCap.style.backgroundColor;
            }

            set
            {
                if (m_ConnectorBoxCap != null)
                    m_ConnectorBoxCap.style.backgroundColor = value;
            }
        }

        public string portName
        {
            get { return m_ConnectorText.text; }
            set { m_ConnectorText.text = value; }
        }

        public Direction direction { get; private set; }
        public Orientation orientation { get; private set; }

        private Type m_PortType;
        public Type portType
        {
            get { return m_PortType; }
            private set
            {
                m_PortType = value;
                Type genericClass = typeof(PortSource<>);
                Type constructedClass = genericClass.MakeGenericType(m_PortType);
                source = Activator.CreateInstance(constructedClass);

                if (string.IsNullOrEmpty(m_ConnectorText.text))
                    m_ConnectorText.text = m_PortType.Name;
            }
        }

        public EdgeConnector edgeConnector
        {
            get { return m_EdgeConnector; }
        }

        public object source { get; set; }

        private bool m_Highlight;
        public bool highlight
        {
            get
            {
                // TODO: Remove when removing presenters.
                PortPresenter portPresenter = GetPresenter<PortPresenter>();
                if (portPresenter == null)
                    return m_Highlight;

                return portPresenter.highlight;
            }
            set
            {
                // TODO: Remove when removing presenters.
                PortPresenter portPresenter = GetPresenter<PortPresenter>();
                if (portPresenter != null)
                    portPresenter.highlight = value;

                if (m_Highlight == value)
                    return;

                m_Highlight = value;

                if (m_Highlight)
                {
                    m_ConnectorBox.AddToClassList("portHighlight");
                }
                else
                {
                    m_ConnectorBox.RemoveFromClassList("portHighlight");
                }
            }
        }

        private HashSet<Edge> m_Connections;
        public virtual IEnumerable<Edge> connections
        {
            get
            {
                return m_Connections;
            }
        }

        public virtual bool connected
        {
            get
            {
                // TODO: Remove when removing presenters.
                PortPresenter portPresenter = GetPresenter<PortPresenter>();
                if (portPresenter != null)
                    return portPresenter.connected;

                return m_Connections.Count > 0;
            }
        }

        public virtual bool collapsed
        {
            get
            {
                // TODO: Remove when removing presenters.
                PortPresenter portPresenter = GetPresenter<PortPresenter>();
                if (portPresenter != null)
                    return portPresenter.collapsed;

                return false;
            }
        }

        public virtual void Connect(Edge edge)
        {
            if (edge == null)
            {
                throw new ArgumentException("The value passed to Port.Connect is null");
            }

            // TODO: Remove when removing presenters.
            var presenter = GetPresenter<PortPresenter>();
            if (presenter != null)
            {
                var edgePresenter = edge.GetPresenter<EdgePresenter>();
                presenter.Connect(edgePresenter);
                return;
            }

            if (!m_Connections.Contains(edge))
            {
                m_Connections.Add(edge);
            }
        }

        public virtual void Disconnect(Edge edge)
        {
            if (edge == null)
            {
                throw new ArgumentException("The value passed to PortPresenter.Disconnect is null");
            }

            // TODO: Remove when removing presenters.
            var presenter = GetPresenter<PortPresenter>();
            if (presenter != null)
            {
                var edgePresenter = edge.GetPresenter<EdgePresenter>();
                presenter.Disconnect(edgePresenter);
                return;
            }

            m_Connections.Remove(edge);
        }

        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            private GraphViewChange m_GraphViewChange;
            private List<Edge> m_EdgesToCreate;

            public DefaultEdgeConnectorListener()
            {
                m_EdgesToCreate = new List<Edge>();
                m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position) {}
            public void OnDrop(GraphView graphView, Edge edge)
            {
                m_EdgesToCreate.Clear();
                m_EdgesToCreate.Add(edge);

                var edgesToCreate = m_EdgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
                }

                foreach (Edge e in edgesToCreate)
                {
                    graphView.AddElement(e);
                    edge.input.Connect(e);
                    edge.output.Connect(e);
                }
            }
        }

        // TODO: Remove when removing presenters.
        protected class DefaultEdgePresenterConnectorListener<TEdgePresenter> : IEdgeConnectorListener where TEdgePresenter : EdgePresenter
        {
            public void OnDropOutsidePort(Edge edge, Vector2 position) {}
            public void OnDrop(GraphView graphView, Edge edge)
            {
                if (graphView == null || edge == null)
                    return;

                if (graphView.presenter == null)
                    return;

                // Check if the edge already has a presenter then do not create it
                EdgePresenter edgePresenter = edge.GetPresenter<EdgePresenter>();

                if (edgePresenter == null)
                {
                    edgePresenter = ScriptableObject.CreateInstance<TEdgePresenter>();
                }

                edgePresenter.output = edge.output.GetPresenter<PortPresenter>();
                edgePresenter.input = edge.input.GetPresenter<PortPresenter>();

                edgePresenter.output.Connect(edgePresenter);
                edgePresenter.input.Connect(edgePresenter);

                graphView.presenter.AddElement(edgePresenter);
            }
        }

        // TODO This is a workaround to avoid having a generic type for the port as generic types mess with USS.
        public static Port Create<TEdge>(Orientation orientation, Direction direction, Type type) where TEdge : Edge, new()
        {
            var connectorListener = new DefaultEdgeConnectorListener();
            var port = new Port(orientation, direction, type)
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }

        // TODO: Remove when removing presenters.
        public static Port Create<TEdgePresenter, TEdge>(PortPresenter presenter)
            where TEdgePresenter : EdgePresenter
            where TEdge : Edge, new()
        {
            var connectorListener = new DefaultEdgePresenterConnectorListener<TEdgePresenter>();
            var port = new Port(Orientation.Horizontal, Direction.Input, typeof(object))
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(connectorListener),
                presenter = presenter
            };
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }

        public virtual void UpdateClasses(bool fakeConnection)
        {
            PortPresenter portPresenter = GetPresenter<PortPresenter>();

            if (portPresenter.connected || fakeConnection)
            {
                AddToClassList("connected");
            }
            else
            {
                RemoveFromClassList("connected");
            }
        }

        protected virtual VisualElement CreateConnector()
        {
            return new VisualElement();
        }

        protected Port(Orientation portOrientation, Direction portDirection, Type type)
        {
            // currently we don't want to be styled as .graphElement since we're contained in a Node
            ClearClassList();

            var tpl = EditorGUIUtility.Load("UXML/GraphView/Port.uxml") as VisualTreeAsset;
            tpl.CloneTree(this, null);
            m_ConnectorBox = this.Q(name: "connector");
            m_ConnectorBox.AddToClassList("connector");

            m_ConnectorText = this.Q(name: "type");
            m_ConnectorText.AddToClassList("type");

            m_ConnectorBoxCap = this.Q(name: "cap");

            VisualElement hole = this.Q(name: "hole");
            if (hole != null)
            {
                m_HoleColor = hole.style.backgroundColor;
            }

            m_Connections = new HashSet<Edge>();

            orientation = portOrientation;
            direction = portDirection;
            portType = type;
        }

        private void UpdateConnector()
        {
            if (m_EdgeConnector == null)
                return;

            var portPresenter = GetPresenter<PortPresenter>();

            if (m_EdgeConnector.target == null || !m_EdgeConnector.target.HasMouseCapture())  // if the edge connector has capture, it means that an edge is being created. so don't remove the manipulator at the moment.
            {
                if (!portPresenter.connected || portPresenter.direction != Direction.Input)
                {
                    this.AddManipulator(m_EdgeConnector);
                }
                else
                {
                    this.RemoveManipulator(m_EdgeConnector);
                }
            }
        }

        public Node node
        {
            get { return GetFirstAncestorOfType<Node>(); }
        }

        public bool IsConnectable()
        {
            // TODO: Remove when removing presenters.
            PortPresenter portPresenter = presenter as PortPresenter;
            if (portPresenter != null)
                return portPresenter.IsConnectable();

            return true;
        }

        public override void OnDataChanged()
        {
            UpdateConnector();
            UpdateClasses(false);

            var portPresenter = GetPresenter<PortPresenter>();
            Type portType = portPresenter.portType;
            Type genericClass = typeof(PortSource<>);
            try
            {
                Type constructedClass = genericClass.MakeGenericType(portType);
                portPresenter.source = Activator.CreateInstance(constructedClass);
            }
            catch (Exception e)
            {
                Debug.Log("Couldn't build PortSouce<" + (portType == null ? "null" : portType.Name) + "> " + e.Message);
            }

            if (portPresenter.highlight)
            {
                m_ConnectorBox.AddToClassList("portHighlight");
            }
            else
            {
                m_ConnectorBox.RemoveFromClassList("portHighlight");
            }

            string portName = string.IsNullOrEmpty(portPresenter.name) ? portType.Name : portPresenter.name;
            m_ConnectorText.text = portName;

            portPresenter.capabilities &= ~Capabilities.Selectable;

            // Cache some stuff for easier access from the outside.
            direction = portPresenter.direction;
            orientation = portPresenter.orientation;
            portType = portPresenter.portType;
            source = portPresenter.source;
        }

        public override Vector3 GetGlobalCenter()
        {
            return m_ConnectorBox.LocalToWorld(m_ConnectorBox.rect.center);
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return m_ConnectorBox.ContainsPoint(this.ChangeCoordinatesTo(m_ConnectorBox, localPoint));
        }

        internal void ResetCapColor()
        {
            m_ConnectorBoxCap.style.backgroundColor = StyleValue<Color>.nil;
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == MouseEnterEvent.TypeId())
            {
                m_ConnectorBox.pseudoStates |= PseudoStates.Hover;
                m_ConnectorBoxCap.pseudoStates |= PseudoStates.Hover;
            }
            else if (evt.GetEventTypeId() == MouseLeaveEvent.TypeId())
            {
                m_ConnectorBox.pseudoStates &= ~PseudoStates.Hover;
                m_ConnectorBoxCap.pseudoStates &= ~PseudoStates.Hover;
            }
            else if (evt.GetEventTypeId() == MouseUpEvent.TypeId())
            {
                // When an edge connect ends, we need to clear out the hover states
                var mouseUp = (MouseUpEvent)evt;
                if (!layout.Contains(mouseUp.localMousePosition))
                {
                    m_ConnectorBox.pseudoStates &= ~PseudoStates.Hover;
                    m_ConnectorBoxCap.pseudoStates &= ~PseudoStates.Hover;
                }
            }
        }
    }
}
