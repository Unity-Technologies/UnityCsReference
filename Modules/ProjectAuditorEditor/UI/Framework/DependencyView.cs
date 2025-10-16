// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class DependencyView : TreeView
    {
        readonly Dictionary<int, DependencyNode> m_NodeDictionary = new Dictionary<int, DependencyNode>();
        readonly Action<Location> m_OnDoubleClick;
        DependencyNode m_Root;

        public DependencyView(TreeViewState treeViewState, Action<Location> onDoubleClick)
            : base(treeViewState)
        {
            m_OnDoubleClick = onDoubleClick;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Hidden Root"};
            var allItems = new List<TreeViewItem>();

            if (m_Root != null)
            {
                m_NodeDictionary.Clear();

                var namesStack = new Stack<string>();
                AddNode(allItems, namesStack, m_Root, 0);
            }

            // Utility method that initializes the TreeViewItem.children and -parent for all items.
            SetupParentsAndChildrenFromDepths(root, allItems);

            // Return root of the tree
            return root;
        }

        public void SetRoot(DependencyNode root)
        {
            if (m_Root != root)
            {
                m_Root = root;

                Reload();
            }
        }

        void AddNode(List<TreeViewItem> items, Stack<string> namesStack, DependencyNode node, int depth)
        {
            var name = node.GetPrettyName();
            if (namesStack.Contains(name))
            {
                // circular dependency
                return;
            }

            var id = items.Count;
            items.Add(new TreeViewItem {id = id, depth = depth, displayName = name}); // TODO add assembly name

            m_NodeDictionary.Add(id, node);

            // if the tree is too deep, serialization will exceed the 7 levels limit.
            if (!node.HasValidChildren)
                items.Add(new TreeViewItem {id = id + 1, depth = depth + 1, displayName = "<Serialization Limit>"});
            else
            {
                namesStack.Push(name);

                node.SortChildren();
                for (int i = 0; i < node.NumChildren; i++)
                {
                    AddNode(items, namesStack, node.GetChild(i), depth + 1);
                }

                namesStack.Pop();
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            if (m_NodeDictionary.ContainsKey(id))
            {
                var node = m_NodeDictionary[id];
                if (node.Location != null)
                    m_OnDoubleClick(node.Location);
            }
        }
    }
}
