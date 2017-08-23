// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

        public Direction direction { get; private set; }

        // TODO This is a workaround to avoid having a generic type for the anchor as generic types mess with USS.
        public static NodeAnchor Create<TEdgePresenter>(NodeAnchorPresenter presenter) where TEdgePresenter : EdgePresenter
        {
            var anchor = new NodeAnchor
            {
                m_EdgeConnector = new EdgeConnector<TEdgePresenter>(null),
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

        protected NodeAnchor()
        {
            // currently we don't want to be styled as .graphElement since we're contained in a Node
            ClearClassList();

            var tpl = EditorGUIUtility.Load("UXML/GraphView/NodeAnchor.uxml") as VisualTreeAsset;
            tpl.CloneTree(this, null);
            m_ConnectorBox = this.Q(name: "connector");
            m_ConnectorBox.AddToClassList("connector");

            m_ConnectorText = this.Q(name: "type");
            m_ConnectorText.AddToClassList("type");
        }

        private void UpdateConnector()
        {
            if (m_EdgeConnector == null)
                return;

            var anchorPresenter = GetPresenter<NodeAnchorPresenter>();

            if (m_EdgeConnector.target == null || !m_EdgeConnector.target.HasCapture())  // if the edge connector has capture, it means that an edge is being created. so don't remove the manipulator at the moment.
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

            // Cache direction for easier access from the outside.
            direction = anchorPresenter.direction;
        }

        public override Vector3 GetGlobalCenter()
        {
            var center = m_ConnectorBox.layout.center;
            center = m_ConnectorBox.transform.matrix.MultiplyPoint3x4(center);
            return this.LocalToWorld(center);
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            // Here local point comes without position offset...
            localPoint -= layout.position;
            return m_ConnectorBox.ContainsPoint(m_ConnectorBox.transform.matrix.MultiplyPoint3x4(localPoint));
        }
    }
}
