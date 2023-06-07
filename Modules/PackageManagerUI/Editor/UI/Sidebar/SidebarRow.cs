// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class SidebarRow : VisualElement
{
    public const string k_SelectedClassName = "selected";
    public const string k_IndentedClassName = "indented";
    public const string k_SidebarIconClassName = "sidebarIcon";
    public const string k_SidebarTitleClassName = "sidebarTitle";

    public string pageId { get; }
    private Label m_RowTitle;
    private VisualElement m_RowIcon;

    public SidebarRow(string pageId, string rowTitle, Icon icon = Icon.None, bool isIndented = false)
    {
        tooltip = rowTitle;
        this.pageId = pageId;

        m_RowIcon = new VisualElement();
        m_RowIcon.classList.Add(k_SidebarIconClassName);
        m_RowIcon.classList.Add(icon.ClassName());
        Add(m_RowIcon);
        m_RowTitle = new Label { text = rowTitle };
        m_RowTitle.classList.Add(k_SidebarTitleClassName);
        Add(m_RowTitle);

        EnableInClassList(k_IndentedClassName, isIndented);
    }

    public void SetSelected(bool select)
    {
        EnableInClassList(k_SelectedClassName, select);
    }
}
