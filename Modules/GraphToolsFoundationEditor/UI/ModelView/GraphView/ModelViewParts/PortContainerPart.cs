// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for the horizontal ports of a node.
    /// </summary>
    class PortContainerPart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-port-container-part";
        public static readonly string portsUssName = "ports";

        /// <summary>
        /// Creates a new instance of the <see cref="PortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortContainerPart"/>.</returns>
        public static PortContainerPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is PortNodeModel)
            {
                return new PortContainerPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected VisualElement m_Root;

        protected PortContainer PortContainer { get; set; }

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected PortContainerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is PortNodeModel portHolder)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(ussClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                PortContainer = new PortContainer { name = portsUssName };
                PortContainer.AddToClassList(m_ParentClassName.WithUssElement(portsUssName));
                m_Root.Add(PortContainer);

                var ports = portHolder.Ports.Where(p => p.Orientation == PortOrientation.Horizontal);
                PortContainer?.UpdatePorts(ports, m_OwnerElement.RootView);

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
            if (m_Model is PortNodeModel portHolder)
            {
                var ports = portHolder.Ports.Where(p => p.Orientation == PortOrientation.Horizontal);
                PortContainer?.UpdatePorts(ports, m_OwnerElement.RootView);
            }
        }
    }
}
