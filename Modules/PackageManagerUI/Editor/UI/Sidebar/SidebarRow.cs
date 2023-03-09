// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class SidebarRow : VisualElement
{
    public const string k_SelectedClassName = "selected";

    public string pageId { get; }
    private Label m_RowLabel;

    public SidebarRow(string pageId, string rowName)
    {
        tooltip = rowName;
        this.pageId = pageId;

        m_RowLabel = new Label { text = rowName };
        Add(m_RowLabel);
    }

    public void SetSelected(bool select)
    {
        EnableInClassList(k_SelectedClassName, select);
    }
}
