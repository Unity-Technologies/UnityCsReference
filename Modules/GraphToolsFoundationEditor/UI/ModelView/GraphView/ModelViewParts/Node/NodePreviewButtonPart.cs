// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A <see cref="NodePreviewButtonPart"/> that is only visible if the node has a preview.
    /// </summary>
    class NodePreviewButtonPart : BaseModelViewPart
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodePreviewButtonPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="NodePreviewButtonPart"/>.</returns>
        public static NodePreviewButtonPart Create(string name, Model model, ModelView ownerElement, string parentClassName)
        {
            return new NodePreviewButtonPart(name, model, ownerElement, parentClassName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodePreviewButtonPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected NodePreviewButtonPart(string name, Model model, ModelView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        public override VisualElement Root => ShowNodePreviewButton;

        ShowNodePreviewButton ShowNodePreviewButton { get; set; }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is ICollapsible)
            {
                ShowNodePreviewButton = new ShowNodePreviewButton { name = PartName };
                ShowNodePreviewButton.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(ShowNodePreviewButton);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (ShowNodePreviewButton != null)
            {
                var showPreview = (m_Model as AbstractNodeModel)?.NodePreviewModel?.ShowNodePreview ?? false;
                ShowNodePreviewButton.SetValueWithoutNotify(showPreview);
            }
        }
    }
}
