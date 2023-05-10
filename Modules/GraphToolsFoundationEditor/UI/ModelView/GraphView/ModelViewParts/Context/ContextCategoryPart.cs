// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

class ContextCategoryPart : GraphElementPart
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextCategoryPart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    /// <param name="topCategory">Pass true if it is the top category, false for the bottom one.</param>
    public ContextCategoryPart(string name, ContextNodeModel model, ModelView ownerElement, string parentClassName, bool topCategory)
        : base(name, model, ownerElement, parentClassName)
    {
        m_TopCategory = topCategory;
    }

    bool m_TopCategory;
    VisualElement m_Root;

    /// <inheritdoc />
    public override VisualElement Root => m_Root;

    /// <inheritdoc />
    protected override void BuildPartUI(VisualElement parent)
    {
        m_Root = new VisualElement();

        string ussClass = m_ParentClassName.WithUssElement("category");

        m_Root.AddToClassList(ussClass);
        m_Root.AddToClassList(ussClass.WithUssModifier(m_TopCategory ? "top" : "bottom"));

        parent.Add(m_Root);
    }

    /// <inheritdoc />
    protected override void UpdatePartFromModel()
    {
    }
}
