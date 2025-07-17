// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Buffers;
using System;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Extension to HierarchyView adding a bunch of useful selection methods.
    /// </summary>
    internal static class HierarchyViewSelectionExtension
    {
        internal static void SetSelection(this HierarchyView view, in HierarchyNode node, bool selected, bool recurse)
        {
            if (node == HierarchyNode.Null)
                return;

            if (selected)
            {
                if (recurse)
                    view.SetFlagsRecursive(in node, HierarchyNodeFlags.Selected, HierarchyTraversalDirection.Children);
                else
                    view.SetFlags(in node, HierarchyNodeFlags.Selected);
            }
            else
            {
                if (recurse)
                    view.ClearFlagsRecursive(in node, HierarchyNodeFlags.Selected, HierarchyTraversalDirection.Children);
                else
                    view.ClearFlags(in node, HierarchyNodeFlags.Selected);
            }
        }

        /// <summary>
        /// Select a list of nodes. Will clear the selection first.
        /// </summary>
        /// <param name="view">View accepting selection</param>
        /// <param name="nodes">Nodes to select</param>
        public static void SetSelection(this HierarchyView view, ReadOnlySpan<HierarchyNode> nodes)
        {
            using (var _ = new HierarchyViewModelFlagsChangeScope(view.ViewModel))
            {
                view.ViewModel.ClearFlags(HierarchyNodeFlags.Selected);
                view.ViewModel.SetFlags(nodes, HierarchyNodeFlags.Selected);
            }
            view.InvokeFlagsChanged(HierarchyViewFlagChangedEventType.Set, HierarchyNodeFlags.Selected, nodes);
            view.Update();
        }

        /// <summary>
        /// Select all children of the currently selected nodes.
        /// </summary>
        /// <param name="view">View accepting selection</param>
        public static void SelectChildrenForSelectedNodes(this HierarchyView view)
        {
            var count = view.ViewModel.HasAllFlagsCount(HierarchyNodeFlags.Selected);
            if (count == 0)
                return;

            using var nodes = new RentSpanUnmanaged<HierarchyNode>(count);
            view.ViewModel.GetNodesWithAllFlags(HierarchyNodeFlags.Selected, nodes);
            view.SetFlagsRecursive(nodes, HierarchyNodeFlags.Selected | HierarchyNodeFlags.Expanded, HierarchyTraversalDirection.Children);
        }

        /// <summary>
        /// Clear the selection.
        /// </summary>
        /// <param name="view">View accepting selection</param>
        public static void ClearSelection(this HierarchyView view)
        {
            view.ClearFlags(HierarchyNodeFlags.Selected);
        }

        /// <summary>
        /// Select all nodes.
        /// </summary>
        /// <param name="view">View accepting selection</param>
        public static void SelectAll(this HierarchyView view)
        {
            view.SetFlags(HierarchyNodeFlags.Selected);
        }

        /// <summary>
        /// Toggle current selection.
        /// </summary>
        /// <param name="view">View accepting selection</param>
        public static void InvertSelection(this HierarchyView view)
        {
            view.ToggleFlags(HierarchyNodeFlags.Selected);
        }

        /// <summary>
        /// Determine if a node is selected or a child of a selected node.
        /// </summary>
        /// <param name="view">Hierarchy view</param>
        /// <param name="node">Node to be checked</param>
        public static bool IsChildOfSelectionOrSelected(HierarchyView view, in HierarchyNode node)
        {
            if (node == HierarchyNode.Null)
                return false;

            var hierarchy = view.Source;
            var currentNode = node;
            while (true)
            {
                if (view.ViewModel.HasAllFlags(in currentNode, HierarchyNodeFlags.Selected))
                    return true;

                var parentNode = hierarchy.GetParent(in currentNode);
                if (parentNode == hierarchy.Root)
                    break;

                currentNode = parentNode;
            }

            return false;
        }
    }
}
