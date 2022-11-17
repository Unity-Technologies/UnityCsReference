// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor;

class PlacematTitlePart : EditableTitlePart
{
    /// <summary>
    /// Creates a new instance of the <see cref="EditableTitlePart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    /// <param name="multiline">Whether the text should be displayed on multiple lines.</param>
    /// <returns>A new instance of <see cref="EditableTitlePart"/>.</returns>
    public new static PlacematTitlePart Create(string name, Model model, ModelView ownerElement, string parentClassName, bool multiline = false, bool useEllipsis = false, bool setWidth = true)
    {
        if (model is PlacematModel)
        {
            return new PlacematTitlePart(name, model, ownerElement, parentClassName, multiline, useEllipsis, setWidth);
        }

        return null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EditableTitlePart"/> class.
    /// </summary>
    /// <param name="name">The name of the part.</param>
    /// <param name="model">The model displayed in this part.</param>
    /// <param name="ownerElement">The owner of the part.</param>
    /// <param name="parentClassName">The class name of the parent.</param>
    /// <param name="multiline">Whether the text should be displayed on multiple lines.</param>
    protected PlacematTitlePart(string name, Model model, ModelView ownerElement, string parentClassName, bool multiline = false, bool useEllipsis = false, bool setWidth = true)
        : base(name, model, ownerElement, parentClassName, multiline, useEllipsis, setWidth)
    {
    }

    /// <inheritdoc />
    protected override void UpdatePartFromModel()
    {
        base.UpdatePartFromModel();

        if (m_Model is PlacematModel placematModel)
        {
            var color = placematModel.Color;
            color.a = 0.5f;
            TitleContainer.style.backgroundColor = color;
        }
    }
}
