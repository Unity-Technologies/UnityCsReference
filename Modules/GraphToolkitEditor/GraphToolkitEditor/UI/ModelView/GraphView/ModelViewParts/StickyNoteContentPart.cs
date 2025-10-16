// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for the text content of a sticky note.
    /// </summary>
    [UnityRestricted]
    internal class StickyNoteContentPart : BaseModelViewPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StickyNoteContentPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="StickyNoteContentPart"/>.</returns>
        public static StickyNoteContentPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is StickyNoteModel)
            {
                return new StickyNoteContentPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected EditableLabel TextLabel { get; set; }

        /// <inheritdoc />
        public override VisualElement Root => TextLabel;

        /// <summary>
        /// Initializes a new instance of the <see cref="StickyNoteContentPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected StickyNoteContentPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            if (m_Model is StickyNoteModel)
            {
                TextLabel = new EditableLabel { name = PartName, EditActionName = "Rename" };
                TextLabel.Multiline = true;
                TextLabel.RegisterCallback<ChangeEvent<string>>(OnChange);
                TextLabel.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                container.Add(TextLabel);
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (TextLabel != null && visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                var value = (m_Model as StickyNoteModel)?.Contents ?? string.Empty;
                TextLabel.SetValueWithoutNotify(value);
            }
        }

        protected void OnChange(ChangeEvent<string> e)
        {
            m_OwnerElement.RootView.Dispatch(new UpdateStickyNoteCommand(m_Model as StickyNoteModel, null, e.newValue));
        }
    }
}
