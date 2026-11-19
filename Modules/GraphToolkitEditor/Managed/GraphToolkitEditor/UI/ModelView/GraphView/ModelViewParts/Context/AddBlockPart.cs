// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The <see cref="ModelViewPart"/> that contains the button to add blocks in a context node.
    /// </summary>
    /// <remarks>
    /// 'AddBlockPart' is a <see cref="ModelViewPart"/> that contains the button used to add blocks within a context node.
    /// This button is positioned at the bottom of the context node, so you can easily create new blocks by opening an item library.
    /// </remarks>
    [UnityRestricted]
    internal class AddBlockPart : BaseModelViewPart
    {
        static readonly string k_AddBlockPlusName = "add-block-plus";
        static readonly string k_AddBlockLabelName = "add-block-label";

        /// <summary>
        /// The USS class name for the add block label element.
        /// </summary>
        public string AddBlockLabelUssClassName => m_ParentClassName.WithUssElement(k_AddBlockLabelName);

        /// <summary>
        /// The USS class name for the add block plus element.
        /// </summary>
        public string AddBlockPlusUssClassName => m_ParentClassName.WithUssElement(k_AddBlockPlusName);

        protected VisualElement m_Root;
        protected Button m_AddBlock;
        protected Color m_BkgndColor;

        /// <summary>
        /// The USS class name for the add block name element
        /// </summary>
        public string AddBlockNameUssClassName => m_ParentClassName.WithUssElement(PartName);

        /// <summary>
        /// Create a instance of the <see cref="AddBlockPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        public AddBlockPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
        {
            if (m_Model is not ContextNodeModel contextNodeModel)
                return;

            m_Root = new VisualElement() { name = PartName };
            m_Root.AddToClassList(AddBlockNameUssClassName);

            m_AddBlock = new Button();
            m_AddBlock.text = contextNodeModel.AddBlockText;
            m_AddBlock.clickable.clicked += MouseUpOnAddBlock;
            m_AddBlock.focusable = true;

            m_Root.Add(m_AddBlock);

            m_Root.AddPackageStylesheet("ContextAddBlock.uss");

            parent.Add(Root);
        }

        /// <summary>
        /// Method called when the Add Block button has been pressed
        /// </summary>
        protected virtual void MouseUpOnAddBlock()
        {
            ((ContextNodeView)m_OwnerElement).ShowItemLibrary(m_AddBlock.worldBound.position, -1);
            m_AddBlock.schedule.Execute(() => { }).ExecuteLater(10);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor) { }
    }
}
