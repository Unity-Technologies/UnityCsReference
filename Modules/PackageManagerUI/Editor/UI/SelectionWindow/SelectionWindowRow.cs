// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class SelectionWindowRow : VisualElement
{
    private static Texture2D s_FolderIcon;
    internal static Texture2D folderIcon => s_FolderIcon ??= EditorGUIUtility.FindTexture(EditorResources.folderIconName);

    private static readonly string k_RemoveIconTooltip = L10n.Tr("Remove");
    internal static readonly string k_InfoLabel = L10n.Tr("moved");
    internal static readonly string k_InfoLabelTooltip = L10n.Tr("This asset was moved. The original location is {0}.");

    private readonly Label m_RemoveIcon;
    private readonly Label m_InfoLabel;
    private readonly Label m_NameLabel;
    private readonly VisualElement m_FileIcon;
    private readonly Toggle m_Toggle;
    public Toggle toggle => m_Toggle;

    public SelectionWindowRow()
    {
        var rowContainer = new VisualElement { classList = { "row-container" } };
        Add(rowContainer);

        var toggleRow = new VisualElement { classList = { "toggle-row" } };
        rowContainer.Add(toggleRow);

        m_Toggle = new Toggle { name = "toggle", classList = { "toggle" }, label = "Toggle" };
        toggleRow.Add(m_Toggle);

        m_NameLabel = m_Toggle.Q<Label>();
        m_NameLabel.name = "name";

        m_InfoLabel = new Label { name = "info", classList = { "text", "label" } };
        m_Toggle.hierarchy.Insert(0, m_InfoLabel);

        m_FileIcon = new VisualElement { name = "icon" };
        m_Toggle.hierarchy.Insert(2, m_FileIcon);

        var labels = new VisualElement { classList = { "labels" } };
        rowContainer.Add(labels);

        m_RemoveIcon = new Label { name = "remove", classList = { "icon", "label" }, tooltip = k_RemoveIconTooltip, displayTooltipWhenElided = true};
        labels.Add(m_RemoveIcon);
    }

    public void SetData(SelectionWindowData windowData, SelectionWindowData.Node node, bool isExpanded)
    {
        userData = node;
        var selected = windowData.IsSelected(node.index);

        m_NameLabel.text = node.name;
        m_Toggle.SetValueWithoutNotify(selected);

        RefreshFileIcon(node);
        RefreshInfoLabel(node);

        var removeIconVisible = (!node.isFolder && selected) || (node.isFolder && !isExpanded &&
            windowData.GetChildren(node, true).Any(n => !n.isFolder && windowData.IsSelected(n.index)));
        UIUtils.SetElementDisplay(m_RemoveIcon, removeIconVisible);
    }

    private void RefreshFileIcon(SelectionWindowData.Node node)
    {
        var icon = node.isFolder
            ? folderIcon
            : InternalEditorUtility.GetIconForFile(!string.IsNullOrEmpty(node.path)
                ? node.path
                : "unknown.txt");
        m_FileIcon.style.backgroundImage = new StyleBackground(icon);
    }

    private void RefreshInfoLabel(SelectionWindowData.Node node)
    {
        if (node?.isMoved == true)
        {
            m_InfoLabel.text = k_InfoLabel;
            m_InfoLabel.tooltip = string.Format(k_InfoLabelTooltip, node.asset.origin.assetPath);
        }
        else
        {
            m_InfoLabel.text = string.Empty;
            m_InfoLabel.tooltip = string.Empty;
        }
    }
}
