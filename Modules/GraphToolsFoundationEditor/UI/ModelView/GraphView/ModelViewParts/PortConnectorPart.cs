// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for a port connector, a circle where the wires connect.
    /// </summary>
    class PortConnectorPart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-port-connector-part";
        public static readonly string connectorUssName = "connector";
        public static readonly string labelName = "label";

        /// <summary>
        /// Creates a new instance of the <see cref="PortConnectorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortConnectorPart"/>.</returns>
        public static PortConnectorPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is PortModel)
            {
                return new PortConnectorPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected Label m_ConnectorLabel;

        protected VisualElement m_ConnectorBox;

        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        public VisualElement Connector => m_ConnectorBox;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConnectorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected PortConnectorPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_ConnectorBox = new VisualElement { name = connectorUssName };
            m_ConnectorBox.AddToClassList(ussClassName.WithUssElement(connectorUssName));
            m_ConnectorBox.AddToClassList(m_ParentClassName.WithUssElement(connectorUssName));
            m_Root.Add(m_ConnectorBox);

            if (m_Model is IHasTitle)
            {
                m_ConnectorLabel = new Label { name = labelName };
                m_ConnectorLabel.AddToClassList(ussClassName.WithUssElement(labelName));
                m_ConnectorLabel.AddToClassList(m_ParentClassName.WithUssElement(labelName));
                m_Root.Add(m_ConnectorLabel);
            }

            container.Add(m_Root);
            container.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            container.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_OwnerElement is not Port port)
                return;

            var mouseIsOnHitBox = Port.GetPortHitBoxBounds(port, true).Contains(evt.mousePosition);
            if (mouseIsOnHitBox != port.Hovering)
                port.Hovering = mouseIsOnHitBox;
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (m_OwnerElement is not Port port)
                return;

            port.Hovering = false;
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet_Internal("PortConnectorPart.uss");
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_ConnectorLabel != null)
                m_ConnectorLabel.text = (m_Model as IHasTitle)?.DisplayTitle ?? String.Empty;

            m_ConnectorBox.MarkDirtyRepaint();
        }
    }
}
