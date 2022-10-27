// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for the horizontal ports of a node, with the input ports on the left and the output ports on the right.
    /// </summary>
    class InOutPortContainerPart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-in-out-port-container-part";
        public static readonly string inputPortsUssName = "inputs";
        public static readonly string outputPortsUssName = "outputs";

        /// <summary>
        /// Initializes a new instance of the <see cref="InOutPortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="InOutPortContainerPart"/>.</returns>
        public static InOutPortContainerPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is PortNodeModel)
            {
                return new InOutPortContainerPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected PortContainer m_InputPortContainer;

        protected PortContainer m_OutputPortContainer;

        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <summary>
        /// Initializes a new instance of the <see cref="InOutPortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected InOutPortContainerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is PortNodeModel)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(ussClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_InputPortContainer = new PortContainer { name = inputPortsUssName };
                m_InputPortContainer.AddToClassList(m_ParentClassName.WithUssElement(inputPortsUssName));
                m_Root.Add(m_InputPortContainer);

                m_OutputPortContainer = new PortContainer { name = outputPortsUssName };
                m_OutputPortContainer.AddToClassList(m_ParentClassName.WithUssElement(outputPortsUssName));
                m_Root.Add(m_OutputPortContainer);

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
            switch (m_Model)
            {
                // TODO: Reinstate.
                // case ISingleInputPortNode inputPortHolder:
                //     m_InputPortContainer?.UpdatePorts(new[] { inputPortHolder.InputPort }, m_OwnerElement.GraphView, m_OwnerElement.CommandDispatcher);
                //     break;
                // case ISingleOutputPortNode outputPortHolder:
                //     m_OutputPortContainer?.UpdatePorts(new[] { outputPortHolder.OutputPort }, m_OwnerElement.GraphView, m_OwnerElement.CommandDispatcher);
                //     break;
                case InputOutputPortsNodeModel portHolder:
                    m_InputPortContainer?.UpdatePorts(portHolder.GetInputPorts().Where(p => p.Orientation == PortOrientation.Horizontal), m_OwnerElement.RootView);
                    m_OutputPortContainer?.UpdatePorts(portHolder.GetOutputPorts().Where(p => p.Orientation == PortOrientation.Horizontal), m_OwnerElement.RootView);
                    break;
            }
        }
    }
}
