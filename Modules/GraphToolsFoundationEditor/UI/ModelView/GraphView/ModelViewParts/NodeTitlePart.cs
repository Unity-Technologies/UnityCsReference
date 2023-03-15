// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

class NodeTitlePart : IconTitleProgressPart
{
    /// <summary>
    /// The uss class name for this element part.
    /// </summary>
    public new static readonly string ussClassName = "ge-node-title-part";

    /// <summary>
    /// The name for the subtitle label.
    /// </summary>
    public static readonly string subtitleName = "subtitle";

    /// <summary>
    /// The modifier name for an empty subtitle.
    /// </summary>
    public static readonly string subTitleEmptyModifier = "empty";

    /// <summary>
    /// The uss class name for the subtitle label.
    /// </summary>
    public static readonly string subtitleUssClassName = ussClassName.WithUssElement(subtitleName);

    /// <summary>
    /// The uss class modifier name for an empty subtitle.
    /// </summary>
    public static readonly string emptySubtitleModifierUssClassName = ussClassName.WithUssModifier(subTitleEmptyModifier);

    Label m_SubTitle;

    /// <summary>
    /// Creates a new instance of the <see cref="EditableTitlePart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    /// <returns>A new instance of <see cref="EditableTitlePart"/>.</returns>
    public static NodeTitlePart Create(string name, AbstractNodeModel model, ModelView ownerElement, string parentClassName)
    {
        return new NodeTitlePart(name, model, ownerElement, parentClassName);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EditableTitlePart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    protected NodeTitlePart(string name, AbstractNodeModel model, ModelView ownerElement, string parentClassName)
        : base(name, model, ownerElement, parentClassName, true)
    {
    }

    /// <inheritdoc />
    protected override void BuildPartUI(VisualElement container)
    {
        base.BuildPartUI(container);

        Root.AddToClassList(ussClassName);
    }

    /// <inheritdoc />
    protected override void CreateTitleLabel()
    {
        base.CreateTitleLabel();

        m_SubTitle = new Label(){name = subtitleName,text = "subtitle"};
        m_SubTitle.AddToClassList(subtitleUssClassName);
        LabelContainer.Add(m_SubTitle);
    }

    /// <inheritdoc />
    protected override void UpdatePartFromModel()
    {
        base.UpdatePartFromModel();

        var subTitle =(m_Model as AbstractNodeModel)?.Subtitle;

        m_SubTitle.text = subTitle;
        m_OwnerElement.EnableInClassList(emptySubtitleModifierUssClassName,string.IsNullOrEmpty(subTitle));
    }
}
