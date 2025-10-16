// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for a single port.
    /// </summary>
    [UnityRestricted]
    internal class SinglePortContainerPart : GraphViewPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="SinglePortContainerPart"/>.</returns>
        public static SinglePortContainerPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is PortModel)
            {
                return new SinglePortContainerPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected PortContainer m_PortContainer;

        /// <summary>
        /// The port in this container.
        /// </summary>
        public Port Port => m_PortContainer.childCount > 0 ? m_PortContainer.ElementAt(0) as Port : null;

        /// <inheritdoc />
        public override VisualElement Root => m_PortContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected SinglePortContainerPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            if (m_Model is PortModel)
            {
                m_PortContainer = new PortContainer { name = PartName };
                m_PortContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(m_PortContainer);
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Model is PortModel portModel)
                m_PortContainer?.UpdatePorts(visitor, new[] { portModel }, m_OwnerElement.RootView);
        }

        /// <inheritdoc />
        public override void SetCullingState(GraphViewCullingState cullingState)
        {
            if (cullingState == GraphViewCullingState.Enabled)
            {
                Port?.PrepareCulling(m_OwnerElement);
            }
            base.SetCullingState(cullingState);

            if (cullingState == GraphViewCullingState.Disabled)
            {
                Port?.ClearCulling();
            }
        }
    }
}
