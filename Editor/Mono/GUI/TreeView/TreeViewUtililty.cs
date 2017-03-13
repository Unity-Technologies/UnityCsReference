// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.IMGUI.Controls
{
    internal static class TreeViewUtility
    {
        internal static void SetParentAndChildrenForItems(IList<TreeViewItem> rows, TreeViewItem root)
        {
            SetChildParentReferences(rows, root);
        }

        // For setting depths values based on children state of the items
        internal static void SetDepthValuesForItems(TreeViewItem root)
        {
            if (root == null)
                throw new ArgumentNullException("root", "The root is null");

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
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

        internal static List<TreeViewItem> FindItemsInList(IEnumerable<int> itemIDs, IList<TreeViewItem> treeViewItems)
        {
            return (from x in treeViewItems where itemIDs.Contains(x.id) select x).ToList();
        }

        internal static TreeViewItem FindItemInList<T>(int id, IList<T> treeViewItems) where T : TreeViewItem
        {
            return treeViewItems.FirstOrDefault(t => t.id == id);
        }

        // Assumes full tree
        internal static TreeViewItem FindItem(int id, TreeViewItem searchFromThisItem)
        {
            return FindItemRecursive(id, searchFromThisItem);
        }

        static TreeViewItem FindItemRecursive(int id, TreeViewItem item)
        {
            if (item == null)
                return null;

            if (item.id == id)
                return item;

            if (!item.hasChildren)
                return null;

            foreach (TreeViewItem child in item.children)
            {
                TreeViewItem result = FindItemRecursive(id, child);
                if (result != null)
                    return result;
            }
            return null;
        }

        // Assumes full tree
        internal static HashSet<int> GetParentsAboveItem(TreeViewItem fromItem)
        {
            if (fromItem == null)
                throw new ArgumentNullException("fromItem");

            var hashSet = new HashSet<int>();
            TreeViewItem parent = fromItem.parent;
            while (parent != null)
            {
                hashSet.Add(parent.id);
                parent = parent.parent;
            }
            return hashSet;
        }

        // Assumes full tree
        internal static HashSet<int> GetParentsBelowItem(TreeViewItem fromItem)
        {
            if (fromItem == null)
                throw new ArgumentNullException("fromItem");

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            stack.Push(fromItem);

            HashSet<int> parents = new HashSet<int>();
            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                if (current.hasChildren)
                {
                    parents.Add(current.id);
                    if (LazyTreeViewDataSource.IsChildListForACollapsedParent(current.children))
                        throw new InvalidOperationException("Invalid tree for finding descendants: Ensure a complete tree when using this utillity method.");

                    foreach (var foo in current.children)
                    {
                        stack.Push(foo);
                    }
                }
            }
            return parents;
        }

        internal static void DebugPrintToEditorLogRecursive(TreeViewItem item)
        {
            if (item == null)
                return;
            System.Console.WriteLine(new System.String(' ', item.depth * 3) + item.displayName);

            if (!item.hasChildren)
                return;

            foreach (TreeViewItem child in item.children)
            {
                DebugPrintToEditorLogRecursive(child);
            }
        }

        // Setup child and parent references based on the depth of the tree view items in 'visibleItems'
        internal static void SetChildParentReferences(IList<TreeViewItem> visibleItems, TreeViewItem root)
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
                var rootChildren = new List<TreeViewItem>(rootChildCount);
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
                root.children = new List<TreeViewItem>();
        }

        static void SetChildren(TreeViewItem item, List<TreeViewItem> newChildList)
        {
            // Do not touch children if we have a LazyParent and did not find any children == keep lazy children
            if (LazyTreeViewDataSource.IsChildListForACollapsedParent(item.children) && newChildList == null)
                return;

            item.children = newChildList;
        }

        static void SetChildParentReferences(int parentIndex, IList<TreeViewItem> visibleItems)
        {
            TreeViewItem parent = visibleItems[parentIndex];
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
            List<TreeViewItem> childList = null;
            if (childCount != 0)
            {
                childList = new List<TreeViewItem>(childCount); // Allocate once
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
