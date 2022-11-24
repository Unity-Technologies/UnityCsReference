// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class SelectionWindowHeader : VisualElement
{
    private static Texture2D defaultHeaderIcon => EditorGUIUtility.FindTexture("UnityLogo");

    private readonly Label m_Title;
    private readonly Label m_Description;

    public SelectionWindowHeader()
    {
        var header = new VisualElement { name = "header", classList = { "header" } };
        Add(header);

        var headerIcon = new VisualElement
        {
            name = "headerIcon",
            classList = { "header-icon" },
            style = { backgroundImage = new StyleBackground(defaultHeaderIcon) }
        };
        header.Add(headerIcon);

        var headerText = new VisualElement { name = "headerText", classList = { "header-text" } };
        header.Add(headerText);

        m_Title = new Label { name = "title", text = "Title", tabIndex = -1, displayTooltipWhenElided = true };
        headerText.Add(m_Title);

        m_Description = new Label { name = "description", text = "description", tabIndex = -1, displayTooltipWhenElided = true };
        headerText.Add(m_Description);

        var separator = new VisualElement { name = "separator", classList = { "separator" } };
        Add(separator);
    }

    public void SetData(string title, string description)
    {
        m_Title.text = title;
        m_Description.text = description;
    }
}
