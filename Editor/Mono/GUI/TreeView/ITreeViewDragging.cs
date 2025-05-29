// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.IMGUI.Controls
{
    // The TreeView requires implementations from the following three interfaces:
    //  ITreeViewDataSource:    Should handle data fetching and data structure
    //  ITreeViewGUI:           Should handle visual representation of TreeView and input handling
    //  ITreeViewDragging       Should handle dragging, temp expansion of items, allow/disallow dropping
    // The TreeView handles:    Navigation, Item selection and initiates dragging


    // DragNDrop interface for tree views
    internal interface ITreeViewDragging<TIdentifier>
    {
        void OnInitialize();
        bool CanStartDrag(TreeViewItem<TIdentifier> targetItem, List<TIdentifier> draggedItemIDs, Vector2 mouseDownPosition);
        void StartDrag(TreeViewItem<TIdentifier> draggedItem, List<TIdentifier> draggedItemIDs);
        bool DragElement(TreeViewItem<TIdentifier> targetItem, Rect targetItemRect, int row);             // 'targetItem' is null when not hovering over any target Item.  Returns true if drag was handled.
        void DragCleanup(bool revertExpanded);
        int GetDropTargetControlID();
        int GetRowMarkerControlID();
        bool drawRowMarkerAbove { get; set; }
    }
}
