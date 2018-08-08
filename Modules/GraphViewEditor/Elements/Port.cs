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
    public class Port : GraphElement
    {
        private const string k_PortColorProperty = "port-color";
        private const string k_DisabledPortColorProperty = "disabled-port-color";

        protected EdgeConnector m_EdgeConnector;

        protected VisualElement m_ConnectorBox;
        protected Label m_ConnectorText;

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

        private bool m_portCapLit;
        public bool portCapLit
        {
            get
            {
                return m_portCapLit;
            }
            set
            {
                if (value == m_portCapLit)
                    return;
                m_portCapLit = value;
                UpdateCapColor();
            }
        }

        public Direction direction
        {
            get { return m_Direction; }
            private set
            {
                if (m_Direction != value)
                {
                    RemoveFromClassList(m_Direction.ToString().ToLower());
                    m_Direction = value;
                    AddToClassList(m_Direction.ToString().ToLower());
                }
            }
        }

        public Orientation orientation { get; private set; }

        public enum Capacity
        {
            Single,
            Multi
        }

        public Capacity capacity { get; private set; }

        private string m_VisualClass;
        public string visualClass
        {
            get { return m_VisualClass; }
            set
            {
                if (value == m_VisualClass)
                    return;

                // Clean whatever class we previously had
                if (!string.IsNullOrEmpty(m_VisualClass))
                    RemoveFromClassList(m_VisualClass);
                else
                    ManageTypeClassList(m_PortType, RemoveFromClassList);

                m_VisualClass = value;

                // Add the given class if not null or empty. Use the auto class otherwise.
                if (!string.IsNullOrEmpty(m_VisualClass))
                    AddToClassList(m_VisualClass);
                else
                    ManageTypeClassList(m_PortType, AddToClassList);
            }
        }

        private Type m_PortType;
        public Type portType
        {
            get { return m_PortType; }
            set
            {
                if (m_PortType == value)
                    return;

                ManageTypeClassList(m_PortType, RemoveFromClassList);

                m_PortType = value;
                Type genericClass = typeof(PortSource<>);
                Type constructedClass = genericClass.MakeGenericType(m_PortType);
                source = Activator.CreateInstance(constructedClass);

                if (string.IsNullOrEmpty(m_ConnectorText.text))
                    m_ConnectorText.text = m_PortType.Name;

                ManageTypeClassList(m_PortType, AddToClassList);
            }
        }

        private void ManageTypeClassList(Type type, Action<string> classListAction)
        {
            // If there's an visual class explicitly set, don't set an automatic one.
            if (type == null || !string.IsNullOrEmpty(m_VisualClass))
                return;

            if (type.IsSubclassOf(typeof(Component)))
                classListAction("typeComponent");
            else if (type.IsSubclassOf(typeof(GameObject)))
                classListAction("typeGameObject");
            else
                classListAction("type" + type.Name);
        }

        public EdgeConnector edgeConnector
        {
            get { return m_EdgeConnector; }
        }

        public object source { get; set; }

        private bool m_Highlight = true;
        public bool highlight
        {
            get
            {
                return m_Highlight;
            }
            set
            {
                if (m_Highlight == value)
                    return;

                m_Highlight = value;

                UpdateConnectorColorAndEnabledState();
            }
        }
        public virtual void OnStartEdgeDragging()
        {
            highlight = false;
        }

        public virtual void OnStopEdgeDragging()
        {
            highlight = true;
        }

        private HashSet<Edge> m_Connections;
        private Direction m_Direction;

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
                return m_Connections.Count > 0;
            }
        }

        public virtual bool collapsed
        {
            get
            {
                return false;
            }
        }

        StyleValue<Color> m_PortColor;
        public Color portColor
        {
            get
            {
                return m_PortColor.GetSpecifiedValueOrDefault(new Color(240 / 255f, 240 / 255f, 240 / 255f));
            }
            set
            {
                m_PortColor = value;
            }
        }

        StyleValue<Color> m_DisabledPortColor;
        public Color disabledPortColor
        {
            get
            {
                return m_PortColor.GetSpecifiedValueOrDefault(new Color(70 / 255f, 70 / 255f, 70 / 255f));
            }
        }

        internal Action<Port> OnConnect;
        internal Action<Port> OnDisconnect;

        public Edge ConnectTo(Port other)
        {
            return ConnectTo<Edge>(other);
        }

        public T ConnectTo<T>(Port other) where T : Edge, new()
        {
            if (other == null)
                throw new ArgumentNullException("Port.ConnectTo<T>() other argument is null");

            if (other.direction == this.direction)
                throw new ArgumentException("Cannot connect two ports with the same direction");

            var edge = new T();

            edge.output = direction == Direction.Output ? this : other;
            edge.input = direction == Direction.Input ? this : other;

            this.Connect(edge);
            other.Connect(edge);

            return edge;
        }

        public virtual void Connect(Edge edge)
        {
            if (edge == null)
                throw new ArgumentException("The value passed to Port.Connect is null");

            if (!m_Connections.Contains(edge))
                m_Connections.Add(edge);

            OnConnect?.Invoke(this);
        }

        public virtual void Disconnect(Edge edge)
        {
            if (edge == null)
                throw new ArgumentException("The value passed to PortPresenter.Disconnect is null");

            m_Connections.Remove(edge);

            OnDisconnect?.Invoke(this);
        }

        public virtual void DisconnectAll()
        {
            m_Connections.Clear();

            OnDisconnect?.Invoke(this);
        }

        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            private GraphViewChange m_GraphViewChange;
            private List<Edge> m_EdgesToCreate;
            private List<GraphElement> m_EdgesToDelete;

            public DefaultEdgeConnectorListener()
            {
                m_EdgesToCreate = new List<Edge>();
                m_EdgesToDelete = new List<GraphElement>();

                m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position) {}
            public void OnDrop(GraphView graphView, Edge edge)
            {
                m_EdgesToCreate.Clear();
                m_EdgesToCreate.Add(edge);

                // We can't just add these edges to delete to the m_GraphViewChange
                // because we want the proper deletion code in GraphView to also
                // be called. Of course, that code (in DeleteElements) also
                // sends a GraphViewChange.
                m_EdgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                    foreach (Edge edgeToDelete in edge.input.connections)
                        if (edgeToDelete != edge)
                            m_EdgesToDelete.Add(edgeToDelete);
                if (edge.output.capacity == Capacity.Single)
                    foreach (Edge edgeToDelete in edge.output.connections)
                        if (edgeToDelete != edge)
                            m_EdgesToDelete.Add(edgeToDelete);
                if (m_EdgesToDelete.Count > 0)
                    graphView.DeleteElements(m_EdgesToDelete);

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

        // TODO This is a workaround to avoid having a generic type for the port as generic types mess with USS.
        public static Port Create<TEdge>(Orientation orientation, Direction direction, Capacity capacity, Type type) where TEdge : Edge, new()
        {
            var connectorListener = new DefaultEdgeConnectorListener();
            var port = new Port(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }

        protected Port(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type)
        {
            // currently we don't want to be styled as .graphElement since we're contained in a Node
            ClearClassList();

            var tpl = EditorGUIUtility.Load("UXML/GraphView/Port.uxml") as VisualTreeAsset;

            tpl.CloneTree(this, null);
            m_ConnectorBox = this.Q(name: "connector");
            m_ConnectorText = this.Q<Label>(name: "type");

            m_ConnectorBoxCap = this.Q(name: "cap");

            m_Connections = new HashSet<Edge>();

            orientation = portOrientation;
            direction = portDirection;
            portType = type;
            capacity = portCapacity;

            AddToClassList("port");
            AddToClassList(portDirection.ToString().ToLower());
            AddStyleSheetPath("StyleSheets/GraphView/Port.uss");
        }

        public Node node
        {
            get { return GetFirstAncestorOfType<Node>(); }
        }

        public override Vector3 GetGlobalCenter()
        {
            return m_ConnectorBox.LocalToWorld(m_ConnectorBox.rect.center);
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            Rect lRect = m_ConnectorBox.layout;

            Rect boxRect;
            if (direction == Direction.Input)
            {
                boxRect = new Rect(-lRect.xMin, -lRect.yMin,
                    lRect.width + lRect.xMin, rect.height);

                boxRect.width += m_ConnectorText.layout.xMin - lRect.xMax;
            }
            else
            {
                boxRect = new Rect(0, -lRect.yMin,
                    rect.width - lRect.xMin, rect.height);
                float leftSpace = lRect.xMin - m_ConnectorText.layout.xMax;

                boxRect.xMin -= leftSpace;
                boxRect.width += leftSpace;
            }

            return boxRect.Contains(this.ChangeCoordinatesTo(m_ConnectorBox, localPoint));
        }

        internal void UpdateCapColor()
        {
            if (portCapLit || connected)
            {
                m_ConnectorBoxCap.style.backgroundColor = portColor;
            }
            else
            {
                m_ConnectorBoxCap.style.backgroundColor = StyleValue<Color>.nil;
            }
        }

        private void UpdateConnectorColorAndEnabledState()
        {
            if (m_ConnectorBox == null)
                return;

            m_ConnectorBox.style.borderColor = highlight ? m_PortColor.value : m_DisabledPortColor.value;
            m_ConnectorBox.SetEnabled(highlight);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (m_ConnectorBox == null || m_ConnectorBoxCap == null)
            {
                return;
            }

            // Only update the box cap background if the port is enabled or highlighted.
            if (highlight)
            {
                if (evt.GetEventTypeId() == MouseEnterEvent.TypeId())
                {
                    m_ConnectorBoxCap.style.backgroundColor = portColor;
                }
                else if (evt.GetEventTypeId() == MouseLeaveEvent.TypeId())
                {
                    UpdateCapColor();
                }
            }
            else if (evt.GetEventTypeId() == MouseUpEvent.TypeId())
            {
                // When an edge connect ends, we need to clear out the hover states
                var mouseUp = (MouseUpEvent)evt;
                if (!layout.Contains(mouseUp.localMousePosition))
                {
                    UpdateCapColor();
                }
            }
        }

        protected override void OnStyleResolved(ICustomStyle styles)
        {
            base.OnStyleResolved(styles);

            styles.ApplyCustomProperty(k_PortColorProperty, ref m_PortColor);
            styles.ApplyCustomProperty(k_DisabledPortColorProperty, ref m_DisabledPortColor);

            UpdateConnectorColorAndEnabledState();
        }
    }
}
