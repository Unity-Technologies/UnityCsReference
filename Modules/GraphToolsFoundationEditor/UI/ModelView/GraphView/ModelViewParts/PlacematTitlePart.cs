// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.GraphToolsFoundation.Editor;

class PlacematTitlePart : EditableTitlePart
{
    const float k_TitleToHeight = 4;
    const float k_MinHeightAt12pt = 57;

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

    static readonly string s_PlatformPath = (Application.platform == RuntimePlatform.WindowsEditor) ? "Windows/" : "macOS/";
    static readonly Cursor s_MoveTitle = new Cursor{texture = EditorGUIUtility.Load("Cursors/" + s_PlatformPath + "Grid.MoveTool.png") as Texture2D,
        hotspot = new Vector2(7, 6), defaultCursorId = (int)MouseCursor.CustomCursor};

    public static readonly string leftAlignClassName = ussClassName.WithUssModifier("left-align");
    public static readonly string rightAlignClassName = ussClassName.WithUssModifier("right-align");

    GraphViewZoomMode m_CurrentMode;

    protected override void BuildPartUI(VisualElement container)
    {
        base.BuildPartUI(container);

        TitleContainer.Q<Label>().style.cursor = TitleContainer.style.cursor = s_MoveTitle;
    }

    /// <inheritdoc />
    protected override void UpdatePartFromModel()
    {
        base.UpdatePartFromModel();

        if (m_Model is PlacematModel placematModel)
        {
            WantedTextSize = placematModel.TitleFontSize;

            TitleLabel.EnableInClassList(leftAlignClassName,placematModel.TitleAlignment == TextAlignment.Left );
            TitleLabel.EnableInClassList(rightAlignClassName,placematModel.TitleAlignment == TextAlignment.Right );

            if (m_CurrentZoom != 0)
                base.SetLevelOfDetail(m_CurrentZoom, m_CurrentMode, GraphViewZoomMode.Normal);

            TitleContainer.tooltip = placematModel.Title;
        }
    }

    /// <inheritdoc />
    protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
    {
        if (e.customStyle.TryGetValue(k_LodMinTextSize, out var value))
            LodMinTextSize = value;
    }

    bool m_UpdateHeightRegistered;

    /// <inheritdoc />
    public override void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
    {
        m_CurrentMode = newZoomMode;
        base.SetLevelOfDetail(zoom, newZoomMode, oldZoomMode);

        float height = TitleContainer.layout.height;

        if (float.IsFinite(height))
            TitleContainer.style.marginTop = -height;

        if (!m_UpdateHeightRegistered)
        {
            TitleContainer.RegisterCallback<GeometryChangedEvent>(UpdateHeight);
            m_UpdateHeightRegistered = true;
        }
    }

    void UpdateHeight(GeometryChangedEvent e)
    {
        float height = TitleContainer.layout.height;
        if (float.IsFinite(height) && TitleContainer.resolvedStyle.marginTop != height)
            TitleContainer.style.marginTop = -height;
    }
}
