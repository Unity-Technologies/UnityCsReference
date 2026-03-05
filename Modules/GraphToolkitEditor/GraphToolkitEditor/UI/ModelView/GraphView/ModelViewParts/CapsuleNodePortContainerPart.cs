// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The part of a CapsuleNode that contains the ports.
    /// </summary>
    [UnityRestricted]
    internal class CapsuleNodePortContainerPart : GraphElementPart
    {
        /// <summary>
        /// The USS class name added to this element part.
        /// </summary>
        public static readonly string ussClassName = "ge-capsule-node-port-container-part";

        /// <summary>
        /// The USS class name added to the first port.
        /// </summary>
        public static readonly string firstPortUssClassName = Port.ussClassName.WithUssModifier("first-port");

        readonly string m_OutputPortsContainerUssName;
        readonly string m_InputPortsContainerUssName;

        VisualElement m_Root;
        PortHierarchyContainer m_PortHierarchyContainer;

        NodeTitlePart m_NodeTitlePart;
        ConstantNodeEditorPart m_ConstantNodeEditorPart;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        internal Port FirstPort => m_PortHierarchyContainer.childCount > 0 ? m_PortHierarchyContainer?.ElementAt(0) as Port : null;

        /// <summary>
        /// Creates a new <see cref="CapsuleNodePortContainerPart"/>.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="model">The model which the part represents.</param>
        /// <param name="ownerElement">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A new instance of <see cref="CapsuleNodePortContainerPart"/>.</returns>
        public CapsuleNodePortContainerPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
            m_OutputPortsContainerUssName = parentClassName.WithUssElement(InOutPortContainerPart.outputPortsUssName);
            m_InputPortsContainerUssName = parentClassName.WithUssElement(InOutPortContainerPart.inputPortsUssName);
            if (((model is ISingleOutputPortNodeModel singleOutputPortNodeModel && singleOutputPortNodeModel.OutputPort != null) || (model is ISingleInputPortNodeModel singleInputPortNodeModel && singleInputPortNodeModel.InputPort != null)))
            {
                if (model is NodeModel nodeModel)
                {
                    if (nodeModel is ConstantNodeModel)
                    {
                        m_ConstantNodeEditorPart = ConstantNodeEditorPart.Create(Port.constantEditorPartName, nodeModel, ownerElement, NodeView.ussClassName);
                        PartList.AppendPart(m_ConstantNodeEditorPart);
                    }
                    else
                    {
                        m_NodeTitlePart = NodeTitlePart.Create(CollapsibleInOutNodeView.titleIconContainerPartName, nodeModel, ownerElement, NodeView.ussClassName, EditableTitlePart.Options.UseEllipsis | EditableTitlePart.Options.SetWidth);
                        PartList.AppendPart(m_NodeTitlePart);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
        {
            m_Root = new VisualElement(){name = PartName};
            m_Root.AddToClassList(ussClassName);

            if (m_Model is ISingleOutputPortNodeModel singleOutputPortNodeModel && singleOutputPortNodeModel.OutputPort != null)
            {
                m_PortHierarchyContainer = new PortHierarchyContainer(noLineOnFirstPort:true);
            }
            else if (m_Model is ISingleInputPortNodeModel singleInputPortNodeModel && singleInputPortNodeModel.InputPort != null)
            {
                m_PortHierarchyContainer = new PortHierarchyContainer(true, float.MaxValue,noLineOnFirstPort:true);
            }
            m_Root.Add(m_PortHierarchyContainer);
            parent.Add(m_Root);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Model is NodeModel nodeModel && m_PortHierarchyContainer != null)
            {
                IReadOnlyList<PortModel> portList;
                bool isOutput = false;
                PortModel mainPortModel;
                if (nodeModel is ISingleOutputPortNodeModel singleOutputPortNodeModel && singleOutputPortNodeModel.OutputPort != null)
                {
                    isOutput = true;
                    mainPortModel = singleOutputPortNodeModel.OutputPort;
                    portList = nodeModel.VisibleOutputsByDisplayOrder;
                }
                else
                {
                    mainPortModel = (nodeModel as ISingleInputPortNodeModel)?.InputPort;
                    portList = nodeModel.VisibleInputsByDisplayOrder;
                }


                m_PortHierarchyContainer.UpdatePorts(visitor, portList, m_OwnerElement.RootView);
                m_PortHierarchyContainer.EnableInClassList(m_OutputPortsContainerUssName, isOutput);
                m_PortHierarchyContainer.EnableInClassList(m_InputPortsContainerUssName, !isOutput);

                var extraPart = (ModelViewPart)m_NodeTitlePart ?? m_ConstantNodeEditorPart;
                if (mainPortModel != null && extraPart != null)
                {
                    var firstPort = FirstPort;
                    if (firstPort != null)
                    {
                        firstPort.AddToClassList(firstPortUssClassName);

                        if (firstPort.PartList.GetPart(Port.connectorPartName) is PortConnectorPart connectorPart && extraPart.Root.parent != connectorPart.Root)
                        {
                            connectorPart.SetHitBoxLimitElement(extraPart.Root);
                        }
                    }
                }
            }
        }
    }
}
