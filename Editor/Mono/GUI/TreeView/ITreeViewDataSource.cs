// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

// The TreeView requires implementations from the following three interfaces:
//  ITreeViewDataSource:    Should handle data fetching, build the tree/data structure and hold expanded state
//  ITreeViewGUI:           Should handle visual representation of TreeView and input handling
//  ITreeViewDragging       Should handle dragging, temp expansion of items, allow/disallow dropping
// The TreeView handles:    Navigation, Item selection and initiates dragging


namespace UnityEditor.IMGUI.Controls
{
    // Represents a complete data tree
    internal interface ITreeViewDataSource
    {
        void OnInitialize();

        // Return root of tree
        TreeViewItem root { get; }

        // For data sources where GetRows() might be an expensive operation
        int rowCount { get; }

        // Reload data
        void ReloadData();

        void InitIfNeeded();

        // Find Item by id
        TreeViewItem FindItem(int id);

        // Get current row of an item (using the current expanded state in TreeViewState)
        // Returns -1 if not found
        int GetRow(int id);

        // Check rowCount before requesting
        TreeViewItem GetItem(int row);

        // Get the flattened tree of visible items. If possible use GetItem(int row) instead
        IList<TreeViewItem> GetRows();

        bool IsRevealed(int id);

        void RevealItem(int id);

        // Expand / collapse interface
        // The DataSource has the interface for this because it should be able to rebuild
        // tree when expanding
        void SetExpandedWithChildren(TreeViewItem item, bool expand);
        void SetExpanded(TreeViewItem item, bool expand);
        bool IsExpanded(TreeViewItem item);
        bool IsExpandable(TreeViewItem item);
        void SetExpandedWithChildren(int id, bool expand);
        int[] GetExpandedIDs();
        void SetExpandedIDs(int[] ids);
        bool SetExpanded(int id, bool expand);
        bool IsExpanded(int id);

        // Selection
        bool CanBeMultiSelected(TreeViewItem item);
        bool CanBeParent(TreeViewItem item);

        // Renaming
        bool IsRenamingItemAllowed(TreeViewItem item);
        void InsertFakeItem(int id, int parentID, string name, Texture2D icon);
        void RemoveFakeItem();
        bool HasFakeItem();

        // Search
        void OnSearchChanged();
    }
} // namespace UnityEditor
