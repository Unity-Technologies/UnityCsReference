// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class NodeAnchor : GraphElement
    {
        protected EdgeConnector m_EdgeConnector;

        protected VisualElement m_ConnectorBox;
        protected VisualElement m_ConnectorText;

        public string anchorName
        {
            get { return m_ConnectorText.text; }
            set { m_ConnectorText.text = value; }
        }

        public Direction direction { get; private set; }
        public Orientation orientation { get; private set; }

        private Type m_AnchorType;
        public Type anchorType
        {
            get { return m_AnchorType; }
            private set
            {
                m_AnchorType = value;
                Type genericClass = typeof(PortSource<>);
                Type constructedClass = genericClass.MakeGenericType(m_AnchorType);
                source = Activator.CreateInstance(constructedClass);

                if (string.IsNullOrEmpty(m_ConnectorText.text))
                    m_ConnectorText.text = m_AnchorType.Name;
            }
        }

        public object source { get; set; }

        private bool m_Highlight;
        public bool highlight
        {
            get
            {
                // TODO: Remove when removing presenters.
                NodeAnchorPresenter anchorPresenter = GetPresenter<NodeAnchorPresenter>();
                if (anchorPresenter == null)
                    return m_Highlight;

                return anchorPresenter.highlight;
            }
            set
            {
                // TODO: Remove when removing presenters.
                NodeAnchorPresenter anchorPresenter = GetPresenter<NodeAnchorPresenter>();
                if (anchorPresenter != null)
                    anchorPresenter.highlight = value;

                if (m_Highlight == value)
                    return;

                m_Highlight = value;

                if (m_Highlight)
                {
                    m_ConnectorBox.AddToClassList("anchorHighlight");
                }
                else
                {
                    m_ConnectorBox.RemoveFromClassList("anchorHighlight");
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
                NodeAnchorPresenter anchorPresenter = GetPresenter<NodeAnchorPresenter>();
                if (anchorPresenter != null)
                    return anchorPresenter.connected;

                return m_Connections.Count > 0;
            }
        }

        public virtual bool collapsed
        {
            get
            {
                // TODO: Remove when removing presenters.
                NodeAnchorPresenter anchorPresenter = GetPresenter<NodeAnchorPresenter>();
                if (anchorPresenter != null)
                    return anchorPresenter.collapsed;

                return false;
            }
        }

        public virtual void Connect(Edge edge)
        {
            if (edge == null)
            {
                throw new ArgumentException("The value passed to NodeAnchor.Connect is null");
            }

            // TODO: Remove when removing presenters.
            var presenter = GetPresenter<NodeAnchorPresenter>();
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
                throw new ArgumentException("The value passed to NodeAnchorPresenter.Disconnect is null");
            }

            // TODO: Remove when removing presenters.
            var presenter = GetPresenter<NodeAnchorPresenter>();
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

            public void OnDropOutsideAnchor(Edge edge, Vector2 position) {}
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
            public void OnDropOutsideAnchor(Edge edge, Vector2 position) {}
            public void OnDrop(GraphView graphView, Edge edge)
            {
                if (graphView == null || edge == null)
                    return;

                if (graphView.presenter == null)
                    return;

                TEdgePresenter edgePresenter = ScriptableObject.CreateInstance<TEdgePresenter>();

                edgePresenter.output = edge.output.GetPresenter<NodeAnchorPresenter>();
                edgePresenter.input = edge.input.GetPresenter<NodeAnchorPresenter>();

                edgePresenter.output.Connect(edgePresenter);
                edgePresenter.input.Connect(edgePresenter);

                graphView.presenter.AddElement(edgePresenter);
            }
        }

        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static NodeAnchor Create<TEdge>(Orientation orientation, Direction direction, Type type) where TEdge : Edge, new()
        {
            var connectorListener = new DefaultEdgeConnectorListener();
            var anchor = new NodeAnchor(orientation, direction, type)
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(connectorListener),
            };
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        // TODO: Remove when removing presenters.
        public static NodeAnchor Create<TEdgePresenter, TEdge>(NodeAnchorPresenter presenter)
            where TEdgePresenter : EdgePresenter
            where TEdge : Edge, new()
        {
            var connectorListener = new DefaultEdgePresenterConnectorListener<TEdgePresenter>();
            var anchor = new NodeAnchor(Orientation.Horizontal, Direction.Input, typeof(object))
            {
                m_EdgeConnector = new EdgeConnector<TEdge>(connectorListener),
                presenter = presenter
            };
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        public virtual void UpdateClasses(bool fakeConnection)
        {
            NodeAnchorPresenter anchorPresenter = GetPresenter<NodeAnchorPresenter>();

            if (anchorPresenter.connected || fakeConnection)
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

        protected NodeAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type)
        {
            // currently we don't want to be styled as .graphElement since we're contained in a Node
            ClearClassList();

            var tpl = EditorGUIUtility.Load("UXML/GraphView/NodeAnchor.uxml") as VisualTreeAsset;
            tpl.CloneTree(this, null);
            m_ConnectorBox = this.Q(name: "connector");
            m_ConnectorBox.AddToClassList("connector");

            m_ConnectorText = this.Q(name: "type");
            m_ConnectorText.AddToClassList("type");

            m_Connections = new HashSet<Edge>();

            orientation = anchorOrientation;
            direction = anchorDirection;
            anchorType = type;
        }

        private void UpdateConnector()
        {
            if (m_EdgeConnector == null)
                return;

            var anchorPresenter = GetPresenter<NodeAnchorPresenter>();

            if (m_EdgeConnector.target == null || !m_EdgeConnector.target.HasMouseCapture())  // if the edge connector has capture, it means that an edge is being created. so don't remove the manipulator at the moment.
            {
                if (!anchorPresenter.connected || anchorPresenter.direction != Direction.Input)
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
            get { return this.GetFirstAncestorOfType<Node>(); }
        }

        public bool IsConnectable()
        {
            // TODO: Remove when removing presenters.
            NodeAnchorPresenter anchorPresenter = presenter as NodeAnchorPresenter;
            if (anchorPresenter != null)
                return anchorPresenter.IsConnectable();

            return true;
        }

        public override void OnDataChanged()
        {
            UpdateConnector();
            UpdateClasses(false);

            var anchorPresenter = GetPresenter<NodeAnchorPresenter>();
            Type anchorType = anchorPresenter.anchorType;
            Type genericClass = typeof(PortSource<>);
            try
            {
                Type constructedClass = genericClass.MakeGenericType(anchorType);
                anchorPresenter.source = Activator.CreateInstance(constructedClass);
            }
            catch (Exception e)
            {
                Debug.Log("Couldn't build PortSouce<" + (anchorType == null ? "null" : anchorType.Name) + "> " + e.Message);
            }

            if (anchorPresenter.highlight)
            {
                m_ConnectorBox.AddToClassList("anchorHighlight");
            }
            else
            {
                m_ConnectorBox.RemoveFromClassList("anchorHighlight");
            }

            string anchorName = string.IsNullOrEmpty(anchorPresenter.name) ? anchorType.Name : anchorPresenter.name;
            m_ConnectorText.text = anchorName;

            anchorPresenter.capabilities &= ~Capabilities.Selectable;

            // Cache some stuff for easier access from the outside.
            direction = anchorPresenter.direction;
            orientation = anchorPresenter.orientation;
            anchorType = anchorPresenter.anchorType;
            source = anchorPresenter.source;
        }

        public override Vector3 GetGlobalCenter()
        {
            return m_ConnectorBox.LocalToWorld(m_ConnectorBox.rect.center);
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return m_ConnectorBox.ContainsPoint(this.ChangeCoordinatesTo(m_ConnectorBox, localPoint));
        }
    }
}
