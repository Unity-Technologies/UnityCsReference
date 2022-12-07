// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor;

class PlacematTitlePart : EditableTitlePart
{
    const float k_TitleToHeight = 4;
    const float k_MinHeightAt12pt = 57;

    float m_LastZoom;

    /// <summary>
    /// Creates a new instance of the <see cref="PlacematTitlePart"/> class.
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
    /// Initializes a new instance of the <see cref="PlacematTitlePart"/> class.
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


    public static readonly string leftAlignClassName = ussClassName.WithUssModifier("left-align");
    public static readonly string rightAlignClassName = ussClassName.WithUssModifier("right-align");

    /// <inheritdoc />
    protected override void UpdatePartFromModel()
    {
        base.UpdatePartFromModel();

        if (m_Model is PlacematModel placematModel)
        {
            var color = placematModel.Color;
            color.a = 0.5f;
            TitleContainer.style.backgroundColor = color;

            WantedTextSize = placematModel.TitleFontSize;


            TitleLabel.style.height = Mathf.Max(k_MinHeightAt12pt, k_TitleToHeight + WantedTextSize*1.22f);

            TitleLabel.EnableInClassList(leftAlignClassName,placematModel.TitleAlignment == TextAlignment.Left );
            TitleLabel.EnableInClassList(rightAlignClassName,placematModel.TitleAlignment == TextAlignment.Right );

            if( m_LastZoom != 0 )
                base.SetLevelOfDetail(m_LastZoom, GraphViewZoomMode.Normal, GraphViewZoomMode.Normal);
        }
    }

    /// <inheritdoc />
    protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
    {
        if (e.customStyle.TryGetValue(k_LodMinTextSize, out var value))
            LodMinTextSize = value;
    }

    /// <inheritdoc />
    public override void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
    {
        m_LastZoom = zoom;
        base.SetLevelOfDetail(zoom, newZoomMode, oldZoomMode);
    }
}
