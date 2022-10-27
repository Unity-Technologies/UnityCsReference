// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A part to build the UI for a collapse button.
    /// </summary>
    class CollapseButtonPart : BaseModelViewPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CollapseButtonPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="CollapseButtonPart"/>.</returns>
        public static CollapseButtonPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            if (model is ICollapsible)
            {
                return new CollapseButtonPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <inheritdoc />
        public override VisualElement Root => CollapseButton;

        protected CollapseButton CollapseButton { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseButtonPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected CollapseButtonPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is ICollapsible)
            {
                CollapseButton = new CollapseButton { name = PartName };
                CollapseButton.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(CollapseButton);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (CollapseButton != null)
            {
                var collapsed = (m_Model as ICollapsible)?.Collapsed ?? false;
                CollapseButton.SetValueWithoutNotify(collapsed);
            }
        }
    }
}
