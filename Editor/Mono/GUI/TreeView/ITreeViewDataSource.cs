// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
    internal interface ITreeViewDataSource<TIdentifier> where TIdentifier : unmanaged, System.IEquatable<TIdentifier>
    {
        void OnInitialize();

        // Return root of tree
        TreeViewItem<TIdentifier> root { get; }

        // For data sources where GetRows() might be an expensive operation
        int rowCount { get; }

        // Reload data
        void ReloadData();

        void InitIfNeeded();

        // Find Item by id
        TreeViewItem<TIdentifier> FindItem(TIdentifier id);

        // Get current row of an item (using the current expanded state in TreeViewState)
        // Returns -1 if not found
        int GetRow(TIdentifier id);

        // Check rowCount before requesting
        TreeViewItem<TIdentifier> GetItem(int row);

        // Get the flattened tree of visible items. If possible use GetItem(int row) instead
        IList<TreeViewItem<TIdentifier>> GetRows();

        bool IsRevealed(TIdentifier id);

        void RevealItem(TIdentifier id);
        void RevealItems(TIdentifier[] ids);

        // Expand / collapse interface
        // The DataSource has the interface for this because it should be able to rebuild
        // tree when expanding
        void SetExpandedWithChildren(TreeViewItem<TIdentifier> item, bool expand);
        void SetExpanded(TreeViewItem<TIdentifier> item, bool expand);
        bool IsExpanded(TreeViewItem<TIdentifier> item);
        bool IsExpandable(TreeViewItem<TIdentifier> item);
        void SetExpandedWithChildren(TIdentifier id, bool expand);
        TIdentifier[] GetExpandedIDs();
        void SetExpandedIDs(TIdentifier[] ids);
        bool SetExpanded(TIdentifier id, bool expand);
        bool IsExpanded(TIdentifier id);

        // Selection
        bool CanBeMultiSelected(TreeViewItem<TIdentifier> item);
        bool CanBeParent(TreeViewItem<TIdentifier> item);
        List<TIdentifier> GetNewSelection(TreeViewItem<TIdentifier> clickedItem, TreeViewSelectState<TIdentifier> selectState);

        // Renaming
        bool IsRenamingItemAllowed(TreeViewItem<TIdentifier> item);
        void InsertFakeItem(TIdentifier id, TIdentifier parentID, string name, Texture2D icon);
        void RemoveFakeItem();
        bool HasFakeItem();

        // Search
        void OnSearchChanged();
    }
} // namespace UnityEditor
