// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for the horizontal ports of a node, with the input ports on the left and the output ports on the right.
    /// </summary>
    [UnityRestricted]
    internal class InOutPortContainerPart : BasePortContainerPart
    {
        /// <summary>
        /// uss class name for this part.
        /// </summary>
        public static readonly string ussClassName = "ge-in-out-port-container-part";

        /// <summary>
        /// uss name for the input section.
        /// </summary>
        public static readonly string inputPortsUssName = "inputs";

        /// <summary>
        /// uss name for the output section.
        /// </summary>
        public static readonly string outputPortsUssName = "outputs";

        /// <summary>
        /// uss modifier class name when there is no input.
        /// </summary>
        public static readonly string noInputUssClassName = ussClassName.WithUssModifier("no-input");

        /// <summary>
        /// uss modifier class name when all input ports have a capacity of <see cref="PortCapacity.None"/>.
        /// </summary>
        public static readonly string noInputCapacityUssClassName = ussClassName.WithUssModifier("no-input-capacity");

        /// <summary>
        /// uss modifier class name when there is not output.
        /// </summary>
        public static readonly string noOutputUssClassName = ussClassName.WithUssModifier("no-output");

        /// <summary>
        /// uss modifier class name when there is no visible port when the node is collapsed.
        /// </summary>
        public static readonly string noVisiblePortUssClassName = ussClassName.WithUssModifier("no-visible-port");

        /// <summary>
        /// Initializes a new instance of the <see cref="InOutPortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="maxInputLabelWidth">Maximum input ports label width.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="portFilter">A filter used to select the ports to display in the container.</param>
        /// <returns>A new instance of <see cref="InOutPortContainerPart"/>.</returns>
        public static InOutPortContainerPart Create(string name, Model model, ChildView ownerElement,
            float maxInputLabelWidth, string parentClassName, Func<PortModel, bool> portFilter = null)
        {
            return model is PortNodeModel
                ? new InOutPortContainerPart(name, model, ownerElement, maxInputLabelWidth, parentClassName, portFilter)
                : null;
        }

        protected PortContainer InputPortContainer
        {
            get => PortContainer;
            set => PortContainer = value;
        }

        protected PortContainer OutputPortContainer { get; set; }

        readonly float m_MaxInputLabelWidth;

        /// <summary>
        /// Initializes a new instance of the <see cref="InOutPortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="maxInputLabelWidth">Maximum width in pixels for the port label.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="portFilter">A filter used to select the ports to display in the container.</param>
        protected InOutPortContainerPart(string name, Model model, ChildView ownerElement, float maxInputLabelWidth, string parentClassName, Func<PortModel, bool> portFilter)
            : base(name, model, ownerElement, parentClassName, null, null,
                   portFilter == null ? horizontalPortFilter : p => horizontalPortFilter(p) && portFilter(p))
        {
            m_MaxInputLabelWidth = maxInputLabelWidth;
        }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            if (m_Model is PortNodeModel)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(ussClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                InputPortContainer = new PortHierarchyContainer(true, m_MaxInputLabelWidth) { name = inputPortsUssName };
                InputPortContainer.AddToClassList(m_ParentClassName.WithUssElement(inputPortsUssName));
                m_Root.Add(InputPortContainer);

                OutputPortContainer = new PortHierarchyContainer { name = outputPortsUssName };
                OutputPortContainer.AddToClassList(m_ParentClassName.WithUssElement(outputPortsUssName));
                m_Root.Add(OutputPortContainer);

                container.Add(m_Root);
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
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
                    using (ListPool<PortModel>.Get(out List<PortModel> filteredPorts))
                    {
                        bool anyVisible = false;
                        bool found = false;
                        foreach (var port in portHolder.VisibleInputsByDisplayOrder)
                        {
                            if (PortFilter(port))
                            {
                                if (port.IsConnected())
                                    anyVisible = true;
                                filteredPorts.Add(port);
                                found = true;
                            }
                        }
                        InputPortContainer?.UpdatePorts(visitor, filteredPorts, m_OwnerElement.RootView);
                        m_Root.EnableInClassList(noInputUssClassName, !found);

                        filteredPorts.Clear();
                        found = false;
                        foreach (var port in portHolder.VisibleOutputsByDisplayOrder)
                        {
                            if (PortFilter(port))
                            {
                                if (port.IsConnected())
                                    anyVisible = true;
                                filteredPorts.Add(port);
                                found = true;
                            }
                        }

                        OutputPortContainer?.UpdatePorts(visitor, filteredPorts, m_OwnerElement.RootView);

                        m_Root.EnableInClassList(noOutputUssClassName, !found);

                        if (!portHolder.IsCollapsible() || portHolder is not ICollapsible { Collapsed: true } collapsibleNode)
                        {
                            anyVisible = true;
                        }
                        m_Root.EnableInClassList(noVisiblePortUssClassName, !anyVisible);
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public override void SetCullingState(GraphViewCullingState cullingState)
        {
            if (cullingState == GraphViewCullingState.Enabled)
            {
                OutputPortContainer.PrepareCulling(m_OwnerElement);
            }
            base.SetCullingState(cullingState);

            if (cullingState == GraphViewCullingState.Disabled)
            {
                OutputPortContainer.ClearCulling();
            }
        }
    }
}
