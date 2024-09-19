// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class SelectionWindowRoot : VisualElement
{
    public event Action<IEnumerable<Asset>> onSelectionCompleted = delegate {};

    private SelectionWindowData m_WindowData;

    // The rows' height must be set in code, not in uss
    private const int k_ItemHeight = 25;

    private readonly SelectionWindowHeader m_Header;
    private readonly SelectionWindowTreeView m_TreeView;
    private readonly SelectionWindowFooter m_Footer;

    private readonly IApplicationProxy m_ApplicationProxy;

    public SelectionWindowRoot(IResourceLoader resourceLoader, IApplicationProxy applicationProxy)
    {
        m_ApplicationProxy = applicationProxy;

        m_Header = new SelectionWindowHeader();
        Add(m_Header);

        m_TreeView = new SelectionWindowTreeView(k_ItemHeight, MakeItem, BindItem);
        Add(m_TreeView);

        m_Footer = new SelectionWindowFooter();
        m_Footer.onAllButtonClicked += OnClickAll;
        m_Footer.onNoneButtonClicked += OnClickNone;
        m_Footer.onActionButtonClicked += OnAction;
        m_Footer.onCancelButtonClicked += OnCancel;
        Add(m_Footer);

        styleSheets.Add(resourceLoader.selectionWindowStyleSheet);
    }

    public void SetData(SelectionWindowData data, bool resetExpansion)
    {
        m_WindowData = data;

        m_Header.SetData(data.headerTitle, data.headerDescription);
        m_TreeView.SetData(data, resetExpansion);
        m_Footer.SetData(data.actionLabel);

        RefreshItems();
    }

    private void RefreshItems()
    {
        m_TreeView.RefreshItems();
        m_Footer.RefreshButtons(m_WindowData.selectedNodesCount, m_WindowData.nodes.Count);
    }

    private VisualElement MakeItem()
    {
        var rowElement = new SelectionWindowRow();
        rowElement.toggle.RegisterValueChangedCallback(ToggleChanged);
        return rowElement;
    }

    private void BindItem(VisualElement item, int index)
    {
        var selectionWindowRow = item as SelectionWindowRow;
        if (selectionWindowRow == null)
            return;

        var node = m_TreeView.GetItemDataForIndex<SelectionWindowData.Node>(index);
        selectionWindowRow.SetData(m_WindowData, node, m_TreeView.IsExpanded(node.index));
    }

    private void OnClickAll()
    {
        m_WindowData.SelectAll();
        RefreshItems();
        AssetSelectionWindowAnalytics.SendEvent(m_WindowData, "selectAll");
    }

    private void OnClickNone()
    {
        m_WindowData.ClearSelection();
        RefreshItems();
        AssetSelectionWindowAnalytics.SendEvent(m_WindowData, "selectNone");
    }

    private void OnCancel()
    {
        onSelectionCompleted?.Invoke(Array.Empty<Asset>()); // We consider an empty list as a cancellation.
        AssetSelectionWindowAnalytics.SendEvent(m_WindowData, "cancel");
    }

    private void OnAction()
    {
        if (!m_ApplicationProxy.DisplayDialog("removeImported", L10n.Tr("Removing imported assets"),
                L10n.Tr("Remove the selected assets?\nAny changes you made to the assets will be lost."),
                L10n.Tr("Remove"), L10n.Tr("Cancel")))
            return;
        onSelectionCompleted?.Invoke(m_WindowData.selectedAssets);
        AssetSelectionWindowAnalytics.SendEvent(m_WindowData, "remove");
    }

    private void ToggleChanged(ChangeEvent<bool> evt)
    {
        var node = GetNodeFromElement(evt.currentTarget as VisualElement);
        if (node == null)
            return;

        if (evt.newValue)
        {
            m_TreeView.ExpandItem(node.index, true);
            m_WindowData.AddSelection(node.index);
        }
        else
        {
            m_WindowData.RemoveSelection(node.index);
        }
        RefreshItems();
    }

    private static SelectionWindowData.Node GetNodeFromElement(VisualElement element)
    {
        var parentElement = element?.parent;
        while (parentElement != null)
        {
            if (parentElement is SelectionWindowRow)
                return parentElement.userData as SelectionWindowData.Node;
            parentElement = parentElement.parent;
        }
        return null;
    }
}
