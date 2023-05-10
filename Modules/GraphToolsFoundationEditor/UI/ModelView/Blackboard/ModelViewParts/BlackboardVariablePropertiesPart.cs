// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for the variable properties.
    /// </summary>
    class BlackboardVariablePropertiesPart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-blackboard-variable-properties-part";

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardVariablePropertiesPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardVariablePropertiesPart"/>.</returns>
        public static BlackboardVariablePropertiesPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is VariableDeclarationModel)
            {
                return new BlackboardVariablePropertiesPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected BlackboardElement m_VariablePropertiesView;

        /// <inheritdoc />
        public override VisualElement Root => m_VariablePropertiesView;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardVariablePropertiesPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardVariablePropertiesPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_VariablePropertiesView = ModelViewFactory.CreateUI<BlackboardElement>(m_OwnerElement.RootView,
                m_Model, BlackboardCreationContext.VariablePropertyCreationContext);
            m_VariablePropertiesView.AddToClassList(ussClassName);
            m_VariablePropertiesView.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            if (parent is BlackboardRow row)
                row.PropertiesSlot.Add(m_VariablePropertiesView);
            else
                parent.Add(m_VariablePropertiesView);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
        }

        /// <inheritdoc />
        protected override void PartOwnerAddedToView()
        {
            m_VariablePropertiesView?.AddToRootView(m_OwnerElement.RootView);
            base.PartOwnerAddedToView();
        }

        /// <inheritdoc />
        protected override void PartOwnerRemovedFromView()
        {
            m_VariablePropertiesView?.RemoveFromRootView();
            base.PartOwnerRemovedFromView();
        }
    }
}
