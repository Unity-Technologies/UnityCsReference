// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Numerics;
using UnityEngine;
using UnityEngine.UIElements;
using Vector2 = UnityEngine.Vector2;

namespace Unity.GraphToolsFoundation.Editor;

class AddBlockPart : BaseModelViewPart
{
    static readonly string k_AddBlockPlusName = "add-block-plus";
    static readonly string k_AddBlockLabelName = "add-block-label";

    /// <summary>
    /// The class name for the add block label element
    /// </summary>
    public string AddBlockLabelClassName => m_ParentClassName.WithUssElement(k_AddBlockLabelName);

    /// <summary>
    /// The class name for the add block plus element
    /// </summary>
    public string AddBlockPlusClassName => m_ParentClassName.WithUssElement(k_AddBlockPlusName);

    protected VisualElement m_Root;
    protected Button m_AddBlock;
    protected Color m_BkgndColor;

    /// <summary>
    /// The class name for the add block name element
    /// </summary>
    public string AddBlockNameClassName => m_ParentClassName.WithUssElement(PartName);

    /// <summary>
    /// Create a instance of the <see cref="AddBlockPart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    public AddBlockPart(string name, Model model, ModelView ownerElement, string parentClassName)
        : base(name, model, ownerElement, parentClassName)
    { }

    /// <inheritdoc />
    public override VisualElement Root => m_Root;

    /// <inheritdoc />
    protected override void BuildPartUI(VisualElement parent)
    {
        m_Root = new VisualElement(){name = PartName};
        m_Root.AddToClassList(AddBlockNameClassName);

        m_AddBlock = new Button();
        m_AddBlock.text = "Add a Block";
        m_AddBlock.clickable.clicked += MouseUpOnAddBlock;
        m_AddBlock.focusable = true;

        m_Root.Add(m_AddBlock);

        m_Root.AddStylesheet_Internal("ContextAddBlock.uss");

        parent.Add(Root);
    }

    void MouseUpOnAddBlock()
    {
        ((ContextNode) m_OwnerElement).ShowItemLibrary_Internal(m_AddBlock.worldBound.position, -1);
        m_AddBlock.schedule.Execute(()=>
        {
        }).ExecuteLater(10);
    }

    /// <inheritdoc />
    protected override void UpdatePartFromModel()
    {
    }
}
