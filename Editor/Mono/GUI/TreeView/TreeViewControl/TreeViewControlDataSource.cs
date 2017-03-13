// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public partial class TreeView
    {
        internal class TreeViewControlDataSource : LazyTreeViewDataSource
        {
            readonly TreeView m_Owner;

            public TreeViewControlDataSource(TreeViewController treeView, TreeView owner) : base(treeView)
            {
                m_Owner = owner;

                // The user should just create the visible rows, we create the hidden root
                showRootItem = false;
            }

            public override void ReloadData()
            {
                // Clear root item to ensure client gets a call to BuildRoot every time Reload is called
                m_RootItem = null;
                base.ReloadData();
            }

            void ValidateRootItem()
            {
                if (m_RootItem == null)
                {
                    throw new NullReferenceException("BuildRoot should set a valid root item.");
                }
                if (m_RootItem.depth != -1)
                {
                    Debug.LogError("BuildRoot should ensure the root item has a depth == -1. The visible items start at depth == 0.");
                    m_RootItem.depth = -1;
                }
                if (m_RootItem.children == null && !m_Owner.m_OverriddenMethods.hasBuildRows)
                {
                    throw new InvalidOperationException("TreeView: 'rootItem.children == null'. Did you forget to add children? If you intend to only create the list of rows (not the full tree) then you need to override: BuildRows, GetAncestors and GetDescendantsThatHaveChildren.");
                }
            }

            public override void FetchData()
            {
                // Set before BuildRoot and BuildRows so we can call GetRows in them without recursion
                m_NeedRefreshRows = false;

                // Root
                if (m_RootItem == null)
                {
                    m_RootItem = m_Owner.BuildRoot();
                    ValidateRootItem();
                }

                // Rows
                m_Rows = m_Owner.BuildRows(m_RootItem);
                if (m_Rows == null)
                    throw new NullReferenceException("RefreshRows should set valid list of rows.");

                // Custom row rects
                if (m_Owner.m_OverriddenMethods.hasGetCustomRowHeight)
                    m_Owner.m_GUI.RefreshRowRects(m_Rows);
            }

            public void SearchFullTree(string search, List<TreeViewItem> result)
            {
                if (string.IsNullOrEmpty(search))
                    throw new ArgumentException("Invalid search: cannot be null or empty", "search");

                if (result == null)
                    throw new ArgumentException("Invalid list: cannot be null", "result");

                var stack = new Stack<TreeViewItem>();
                stack.Push(m_RootItem);
                while (stack.Count > 0)
                {
                    TreeViewItem current = stack.Pop();
                    if (current.children != null)
                    {
                        foreach (var child in current.children)
                        {
                            if (child != null)
                            {
                                if (m_Owner.DoesItemMatchSearch(child, search))
                                    result.Add(child);

                                stack.Push(child);
                            }
                        }
                    }
                }

                result.Sort((x, y) => EditorUtility.NaturalCompare(x.displayName, y.displayName));
            }

            protected override HashSet<int> GetParentsAbove(int id)
            {
                return new HashSet<int>(m_Owner.GetAncestors(id));
            }

            protected override HashSet<int> GetParentsBelow(int id)
            {
                return new HashSet<int>(m_Owner.GetDescendantsThatHaveChildren(id));
            }

            public override bool IsExpandable(TreeViewItem item)
            {
                return m_Owner.CanChangeExpandedState(item);
            }

            public override bool CanBeMultiSelected(TreeViewItem item)
            {
                return m_Owner.CanMultiSelect(item);
            }

            public override bool CanBeParent(TreeViewItem item)
            {
                return m_Owner.CanBeParent(item);
            }

            public override bool IsRenamingItemAllowed(TreeViewItem item)
            {
                return m_Owner.CanRename(item);
            }
        }
    }
}
