// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    [VisibleToOtherModules]
    internal interface IHierarchyEditorNodeTypeHandler
    {
        bool CanCut(HierarchyView view);
        bool OnCut(HierarchyView view);
        bool CanCopy(HierarchyView view);
        bool OnCopy(HierarchyView view);
        bool CanPaste(HierarchyView view);
        bool OnPaste(HierarchyView view);
        bool CanPasteAsChild(HierarchyView view);
        bool OnPasteAsChild(HierarchyView view, bool keepWorldPos);
        bool CanSetName(HierarchyView view, in HierarchyNode node);
        bool OnSetName(HierarchyView view, in HierarchyNode node, string name);
        string GetDisplayName(HierarchyView view, in HierarchyNode node);
        bool CanDuplicate(HierarchyView view);
        bool OnDuplicate(HierarchyView view);
        bool CanDelete(HierarchyView view);
        bool OnDelete(HierarchyView view);
        bool CanFindReferences(HierarchyView view);
        bool OnFindReferences(HierarchyView view);
        bool CanDoubleClick(HierarchyView view, in HierarchyNode node);
        bool OnDoubleClick(HierarchyView view, in HierarchyNode node);
        void GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip);
        void PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu);
        bool AcceptParent(HierarchyView view, in HierarchyNode parent);
        bool AcceptChild(HierarchyView view, in HierarchyNode child);
        bool CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes);
        void OnStartDrag(in HierarchyViewDragAndDropSetupData data);
        DragVisualMode CanDrop(in HierarchyViewDragAndDropHandlingData data);
        DragVisualMode OnDrop(in HierarchyViewDragAndDropHandlingData data);
    }
}
