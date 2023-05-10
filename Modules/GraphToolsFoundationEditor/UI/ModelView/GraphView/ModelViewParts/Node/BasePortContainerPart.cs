// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// An abstract part to build the UI for the ports of a node.
/// </summary>
abstract class BasePortContainerPart : BaseModelViewPart
{
    public static readonly Func<PortModel, bool> horizontalPortFilter = p => p.Orientation == PortOrientation.Horizontal;
    public static readonly Func<PortModel, bool> verticalPortFilter = p => p.Orientation == PortOrientation.Vertical;
    public static readonly Func<PortModel, bool> inputPortFilter = p => p.Direction == PortDirection.Input;
    public static readonly Func<PortModel, bool> outputPortFilter = p => p.Direction == PortDirection.Output;

    protected string m_UssClassName;
    protected string m_PortUssClassName;

    protected VisualElement m_Root;

    protected PortContainer PortContainer { get; set; }

    public Func<PortModel, bool> PortFilter { get; set; }

    /// <inheritdoc />
    public override VisualElement Root => m_Root;


    /// <summary>
    /// Should the parent of the PortContainer get the modifier class for the number of elements as well ?
    /// </summary>
    protected virtual bool SetCountClassOnParent => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasePortContainerPart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    /// <param name="portUssClassName">The uss class name to apply on the <see cref="PortContainer"/> element.</param>
    /// <param name="ussClassName">The uss class name to apply on the root element.</param>
    /// <param name="portFilter">A filter used to select the ports to display in the container.</param>
    protected BasePortContainerPart(string name, Model model, ModelView ownerElement, string parentClassName, string portUssClassName, string ussClassName, Func<PortModel, bool> portFilter)
        : base(name, model, ownerElement, parentClassName)
    {
        m_PortUssClassName = portUssClassName;
        m_UssClassName = ussClassName;
        PortFilter = portFilter;
    }

    /// <inheritdoc />
    protected override void BuildPartUI(VisualElement container)
    {
        if (m_Model is PortNodeModel)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(m_UssClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            PortContainer = new PortContainer(false, float.PositiveInfinity, SetCountClassOnParent) { name = m_PortUssClassName };
            PortContainer.AddToClassList(m_ParentClassName.WithUssElement(m_PortUssClassName));
            m_Root.Add(PortContainer);

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
            var ports = portHolder.Ports.Where(PortFilter);
            PortContainer?.UpdatePorts(ports, m_OwnerElement.RootView);
        }
    }
}
