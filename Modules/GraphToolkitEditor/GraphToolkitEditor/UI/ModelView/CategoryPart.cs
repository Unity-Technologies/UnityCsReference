// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    class CategoryPart : GraphElementPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <param name="topCategory">Pass true if it is the top category, false for the bottom one.</param>
        public CategoryPart(string name, AbstractNodeModel model, ChildView ownerElement, string parentClassName, bool topCategory)
            : base(name, model, ownerElement, parentClassName)
        {
            m_TopCategory = topCategory;
        }

        bool m_TopCategory;
        VisualElement m_Root;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildUI(VisualElement parent)
        {
            m_Root = new VisualElement { pickingMode = PickingMode.Ignore };

            var ussClass = m_ParentClassName.WithUssElement("category");

            m_Root.AddToClassList(ussClass);
            m_Root.AddToClassList(ussClass.WithUssModifier(m_TopCategory ? "top" : "bottom"));

            if(m_Model is AbstractNodeModel model)
                m_Root.style.backgroundColor = model.ElementColor.HasUserColor ? model.ElementColor.Color : model.DefaultColor;

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Model is AbstractNodeModel model)
                UpdateLineColorFromModel(visitor, model);
        }

        /// <summary>
        /// When handling a style change for a context node, this method updates the color line based on the model's color.
        /// </summary>
        /// <param name="visitor"></param>
        /// <param name="nodeModel"></param>
        /// <returns>Returns true if the Root's background color is set</returns>
        bool UpdateLineColorFromModel(UpdateFromModelVisitor visitor, AbstractNodeModel nodeModel)
        {
            if (m_Root == null)
                return false;

            if (visitor.ChangeHints.HasChange(ChangeHint.Style))
            {
                if (nodeModel.ElementColor.HasUserColor)
                {
                    m_Root.style.backgroundColor = nodeModel.ElementColor.Color;
                    return true;
                }

                m_Root.style.backgroundColor = nodeModel.DefaultColor;
            }

            return false;
        }

        /// <inheritdoc />
        public override bool SupportsCulling() => false;

        public class TestAccess
        {
            public readonly CategoryPart categoryPart;

            public TestAccess(CategoryPart categoryPart)
            {
                this.categoryPart = categoryPart;
            }

            public bool UpdateLineColorFromModel(UpdateFromModelVisitor visitor, AbstractNodeModel nodeModel) => categoryPart.UpdateLineColorFromModel(visitor, nodeModel);
            public void BuildUI(VisualElement container) => categoryPart.BuildUI(container);
        }
    }
}
