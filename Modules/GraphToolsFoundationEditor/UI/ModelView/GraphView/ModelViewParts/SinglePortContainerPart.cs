// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for a single port.
    /// </summary>
    class SinglePortContainerPart : BaseModelViewPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="SinglePortContainerPart"/>.</returns>
        public static SinglePortContainerPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is PortModel)
            {
                return new SinglePortContainerPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected PortContainer m_PortContainer;

        /// <inheritdoc />
        public override VisualElement Root => m_PortContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SinglePortContainerPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected SinglePortContainerPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is PortModel)
            {
                m_PortContainer = new PortContainer { name = PartName };
                m_PortContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(m_PortContainer);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is PortModel portModel)
                m_PortContainer?.UpdatePorts(new[] { portModel }, m_OwnerElement.RootView);
        }
    }
}
