// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

// ReSharper disable InconsistentNaming

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The part to build the UI for vertical port containers.
    /// </summary>
    class VerticalPortContainerPart : BaseModelViewPart
    {
        /// <summary>
        /// The USS class name for the part.
        /// </summary>
        public static readonly string ussClassName = "ge-vertical-port-container-part";

        /// <summary>
        /// The USS class name for the port container.
        /// </summary>
        public static readonly string portsUssName = "vertical-port-container";

        /// <summary>
        /// Creates a new <see cref="VerticalPortContainerPart"/>.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="portDirection">The direction of the ports the container will hold.</param>
        /// <param name="model">The model which the part represents.</param>
        /// <param name="ownerUI">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A new instance of <see cref="VerticalPortContainerPart"/>.</returns>
        public static VerticalPortContainerPart Create(string name, PortDirection portDirection, Model model, ModelView ownerUI, string parentClassName)
        {
            if (model is PortNodeModel)
            {
                return new VerticalPortContainerPart(name, portDirection, model, ownerUI, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// The port container associated to this part.
        /// </summary>
        protected PortContainer m_PortContainer;

        protected VisualElement m_Root;

        protected PortDirection m_PortDirection;

        public override VisualElement Root => m_Root;

        /// <summary>
        /// Creates a new VerticalPortContainerPart.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="portDirection">The direction of the ports the container will hold.</param>
        /// <param name="model">The model which the part represents.</param>
        /// <param name="ownerUI">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A newly created VerticalPortContainerPart.</returns>
        protected VerticalPortContainerPart(string name, PortDirection portDirection, Model model,
                                            ModelView ownerUI, string parentClassName)
            : base(name, model, ownerUI, parentClassName)
        {
            m_PortDirection = portDirection;
        }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is InputOutputPortsNodeModel portNode)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(ussClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_PortContainer = new PortContainer { name = portsUssName };
                m_PortContainer.AddToClassList(m_ParentClassName.WithUssElement(portsUssName));
                m_Root.Add(m_PortContainer);

                var ports = (m_PortDirection == PortDirection.Input ? portNode.GetInputPorts() : portNode.GetOutputPorts())
                    .Where(p => p.Orientation == PortOrientation.Vertical);

                m_PortContainer?.UpdatePorts(ports, m_OwnerElement.RootView);

                container.Add(m_Root);
            }
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet_Internal("PortContainerPart.uss");
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (!(m_Model is InputOutputPortsNodeModel portNode))
                return;

            var ports = (m_PortDirection == PortDirection.Input ? portNode.GetInputPorts() : portNode.GetOutputPorts())
                .Where(p => p.Orientation == PortOrientation.Vertical);

            m_PortContainer?.UpdatePorts(ports, m_OwnerElement.RootView);
        }
    }
}
