// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

// The TreeView requires implementations from the following three interfaces:
//  ITreeViewDataSource:    Should handle data fetching and data structure
//  ITreeViewGUI:           Should handle visual representation of TreeView and input handling
//  ITreeViewDragging       Should handle dragging, temp expansion of items, allow/disallow dropping
// The TreeView handles:    Navigation, Item selection and initiates dragging


namespace UnityEditor.IMGUI.Controls
{
    internal interface ITreeViewGUI
    {
        void OnInitialize();

        // Should return the size of the entire visible content (in pixels).
        Vector2 GetTotalSize();

        // Should return the row number of the first and last row thats fits between top pixel and the height of the window
        // If the treeview contains items with varying heights then use the minium height for determining the lastRowVisible
        // this is needed when animating the treeview to ensure all items are rendered while animating.
        // Can use TreeView.GetTotalRect and m_TreeView.state.scrollPos.y for calculating first and last values
        void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible);

        Rect GetRowRect(int row, float rowWidth);
        Rect GetRectForFraming(int row);

        int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView);

        // OnGUI: Implement to handle TreeView OnGUI
        void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused);
        void BeginRowGUI();                                                                     // use for e.g clearing state before OnRowGUI calls
        void EndRowGUI();                                                                       // use for handling stuff after all rows have had their OnRowGUI

        // Ping Item interface (implement a rendering of a 'ping' for a Item).
        void BeginPingItem(TreeViewItem item, float topPixelOfRow, float availableWidth);
        void EndPingItem();

        // Rename interface (BeginRename should return true if rename is handled)
        bool BeginRename(TreeViewItem item, float delay);
        void EndRename();
        Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item);

        float GetContentIndent(TreeViewItem item);

        float halfDropBetweenHeight { get; }
        float topRowMargin { get; }
        float bottomRowMargin { get; }
    }
}
