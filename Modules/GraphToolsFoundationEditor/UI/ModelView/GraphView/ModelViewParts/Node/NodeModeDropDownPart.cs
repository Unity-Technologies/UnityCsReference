// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for the dropdown to select a node mode.
    /// </summary>
    class NodeModeDropDownPart : BaseModelViewPart
    {
        public static readonly string ussClassName = "ge-node-mode-dropdown-part";

        /// <summary>
        /// Creates a new instance of the <see cref="NodeModeDropDownPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="NodeModeDropDownPart"/>.</returns>
        public static NodeModeDropDownPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            return model is AbstractNodeModel ? new NodeModeDropDownPart(name, model, ownerElement, parentClassName) : null;
        }

        protected VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeModeDropDownPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected NodeModeDropDownPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        DropdownField m_DropdownField;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is not NodeModel nodeModel || !nodeModel.Modes.Any())
                return;

            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_DropdownField = new DropdownField
            {
                choices = nodeModel.Modes,
                index = nodeModel.CurrentModeIndex
            };
            m_DropdownField.RegisterValueChangedCallback(e =>
            {
                if (e.previousValue != e.newValue)
                {
                    var newIndex = nodeModel.Modes.IndexOf_Internal(e.newValue);
                    m_OwnerElement.RootView.Dispatch(new ChangeNodeModeCommand(nodeModel, newIndex));
                }
            });
            m_Root.Add(m_DropdownField);
            container.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is not NodeModel nodeModel || !nodeModel.Modes.Any())
                return;

            var currentMode = nodeModel.Modes.ElementAtOrDefault(nodeModel.CurrentModeIndex);
            if (currentMode != null && m_DropdownField.value != currentMode)
                m_DropdownField.SetValueWithoutNotify(currentMode);
        }
    }
}
