// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.IMGUI.Controls
{
    internal static class TreeViewUtility<TIdentifier> where TIdentifier : unmanaged, IEquatable<TIdentifier>
    {
        internal static void SetParentAndChildrenForItems(IList<TreeViewItem<TIdentifier>> rows, TreeViewItem<TIdentifier> root)
        {
            SetChildParentReferences(rows, root);
        }

        // For setting depths values based on children state of the items
        internal static void SetDepthValuesForItems(TreeViewItem<TIdentifier> root)
        {
            if (root == null)
                throw new ArgumentNullException("root", "The root is null");

            Stack<TreeViewItem<TIdentifier>> stack = new Stack<TreeViewItem<TIdentifier>>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                TreeViewItem<TIdentifier> current = stack.Pop();
                if (current.children != null)
                {
                    foreach (var child in current.children)
                    {
                        if (child != null)
                        {
                            child.depth = current.depth + 1;
                            stack.Push(child);
                        }
                    }
                }
            }
        }

        internal static List<TreeViewItem<TIdentifier>> FindItemsInList(IEnumerable<TIdentifier> itemIDs, IList<TreeViewItem<TIdentifier>> treeViewItems)
        {
            return (from x in treeViewItems where itemIDs.Contains(x.id) select x).ToList();
        }

        internal static TreeViewItem<TIdentifier> FindItemInList<T>(TIdentifier id, IList<T> treeViewItems) where T : TreeViewItem<TIdentifier>
        {
            return treeViewItems.FirstOrDefault(t => t.id.Equals(id));
        }

        // Assumes full tree
        internal static TreeViewItem<TIdentifier> FindItem(TIdentifier id, TreeViewItem<TIdentifier> searchFromThisItem)
        {
            return FindItemRecursive(id, searchFromThisItem);
        }

        static TreeViewItem<TIdentifier> FindItemRecursive(TIdentifier id, TreeViewItem<TIdentifier> item)
        {
            if (item == null)
                return null;

            if (item.id.Equals(id))
                return item;

            if (!item.hasChildren)
                return null;

            foreach (TreeViewItem<TIdentifier> child in item.children)
            {
                TreeViewItem<TIdentifier> result = FindItemRecursive(id, child);
                if (result != null)
                    return result;
            }
            return null;
        }

        // Assumes full tree
        internal static void GetParentsAboveItem(TreeViewItem<TIdentifier> fromItem, HashSet<TIdentifier> parentsAbove)
        {
            if (fromItem == null)
                throw new ArgumentNullException("fromItem");

            TreeViewItem<TIdentifier> parent = fromItem.parent;
            while (parent != null)
            {
                parentsAbove.Add(parent.id);
                parent = parent.parent;
            }
        }

        // Assumes full tree
        internal static void GetParentsBelowItem(TreeViewItem<TIdentifier> fromItem, HashSet<TIdentifier> parentsBelow)
        {
            if (fromItem == null)
                throw new ArgumentNullException("fromItem");

            Stack<TreeViewItem<TIdentifier>> stack = new Stack<TreeViewItem<TIdentifier>>();
            stack.Push(fromItem);

            while (stack.Count > 0)
            {
                TreeViewItem<TIdentifier> current = stack.Pop();
                if (current.hasChildren)
                {
                    parentsBelow.Add(current.id);
                    if (LazyTreeViewDataSource<TIdentifier>.IsChildListForACollapsedParent(current.children))
                        throw new InvalidOperationException("Invalid tree for finding descendants: Ensure a complete tree when using this utillity method.");

                    foreach (var foo in current.children)
                    {
                        stack.Push(foo);
                    }
                }
            }
        }

        internal static void DebugPrintToEditorLogRecursive(TreeViewItem<TIdentifier> item)
        {
            if (item == null)
                return;
            System.Console.WriteLine(new System.String(' ', item.depth * 3) + item.displayName);

            if (!item.hasChildren)
                return;

            foreach (TreeViewItem<TIdentifier> child in item.children)
            {
                DebugPrintToEditorLogRecursive(child);
            }
        }

        // Setup child and parent references based on the depth of the tree view items in 'visibleItems'
        internal static void SetChildParentReferences(IList<TreeViewItem<TIdentifier>> visibleItems, TreeViewItem<TIdentifier> root)
        {
            for (int i = 0; i < visibleItems.Count; i++)
                visibleItems[i].parent = null;

            // Set child and parent references using depth info
            int rootChildCount = 0;
            for (int i = 0; i < visibleItems.Count; i++)
            {
                SetChildParentReferences(i, visibleItems);

                if (visibleItems[i].parent == null)
                    rootChildCount++;
            }

            // Ensure items without a parent gets 'root' as parent
            if (rootChildCount > 0)
            {
                var rootChildren = new List<TreeViewItem<TIdentifier>>(rootChildCount);
                for (int i = 0; i < visibleItems.Count; i++)
                {
                    if (visibleItems[i].parent == null)
                    {
                        rootChildren.Add(visibleItems[i]);
                        visibleItems[i].parent = root;
                    }
                }
                root.children = rootChildren;
            }
            else
                root.children = new List<TreeViewItem<TIdentifier>>();
        }

        static void SetChildren(TreeViewItem<TIdentifier> item, List<TreeViewItem<TIdentifier>> newChildList)
        {
            // Do not touch children if we have a LazyParent and did not find any children == keep lazy children
            if (LazyTreeViewDataSource<TIdentifier>.IsChildListForACollapsedParent(item.children) && newChildList == null)
                return;

            item.children = newChildList;
        }

        static void SetChildParentReferences(int parentIndex, IList<TreeViewItem<TIdentifier>> visibleItems)
        {
            TreeViewItem<TIdentifier> parent = visibleItems[parentIndex];
            bool alreadyHasValidChildren = parent.children != null && parent.children.Count > 0 && parent.children[0] != null;
            if (alreadyHasValidChildren)
                return;

            int parentDepth = parent.depth;
            int childCount = 0;

            // Count children based depth value, we are looking at children until it's the same depth as this object
            for (int i = parentIndex + 1; i < visibleItems.Count; i++)
            {
                if (visibleItems[i].depth == parentDepth + 1)
                    childCount++;
                if (visibleItems[i].depth <= parentDepth)
                    break;
            }

            // Fill child array
            List<TreeViewItem<TIdentifier>> childList = null;
            if (childCount != 0)
            {
                childList = new List<TreeViewItem<TIdentifier>>(childCount); // Allocate once
                childCount = 0;
                for (int i = parentIndex + 1; i < visibleItems.Count; i++)
                {
                    if (visibleItems[i].depth == parentDepth + 1)
                    {
                        visibleItems[i].parent = parent;
                        childList.Add(visibleItems[i]);
                        childCount++;
                    }

                    if (visibleItems[i].depth <= parentDepth)
                        break;
                }
            }

            SetChildren(parent, childList);
        }
    }
}
