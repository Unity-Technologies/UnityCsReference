// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

internal class SelectionWindowTreeView : TreeView
{
    public SelectionWindowTreeView(int itemHeight, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem)
        : base(itemHeight, makeItem, bindItem)
    {
        selectionType = SelectionType.None;
    }

    private void ResetExpansionAndScroll()
    {
        ExpandAll();
        // We need to do a delay call because there's a bug with UI toolkit that causes the scrollView
        // to be set back to the saved position even after the OnViewDataReady is called.
        EditorApplication.delayCall += () => scrollView.scrollOffset = Vector2.zero;
    }

    private static List<TreeViewItemData<SelectionWindowData.Node>> CreateTreeViewData(SelectionWindowData data, SelectionWindowData.Node parentNode)
    {
        if (data?.nodes == null || data.nodes.Count == 0)
            return new List<TreeViewItemData<SelectionWindowData.Node>>();

        var results = new List<TreeViewItemData<SelectionWindowData.Node>>();
        foreach (var node in data.GetChildren(parentNode))
        {
            var treeViewItemData = new TreeViewItemData<SelectionWindowData.Node>(node.index, node);
            treeViewItemData.AddChildren(CreateTreeViewData(data, node));
            results.Add(treeViewItemData);
        }
        return results;
    }

    public void SetData(SelectionWindowData data, bool resetExpansion)
    {
        SetRootItems(CreateTreeViewData(data, data.hiddenRootNode));
        if (resetExpansion)
            ResetExpansionAndScroll();
    }
}
