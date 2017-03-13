// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;


namespace UnityEditor.TreeViewExamples
{
    class LazyTestDataSource : LazyTreeViewDataSource
    {
        private BackendData m_Backend;
        public int itemCounter { get; private set; }

        public LazyTestDataSource(TreeViewController treeView, BackendData data)
            : base(treeView)
        {
            m_Backend = data;
            FetchData();
        }

        public override void FetchData()
        {
            // For LazyTreeViewDataSources we just generate the 'm_VisibleRows' items:
            itemCounter = 1;
            m_RootItem = new FooTreeViewItem(m_Backend.root.id, 0, null, m_Backend.root.name, m_Backend.root);
            AddVisibleChildrenRecursive(m_Backend.root, m_RootItem);

            m_Rows = new List<TreeViewItem>();
            GetVisibleItemsRecursive(m_RootItem, m_Rows);
            m_NeedRefreshRows = false;
        }

        void AddVisibleChildrenRecursive(BackendData.Foo source, TreeViewItem dest)
        {
            if (IsExpanded(source.id))
            {
                if (source.children != null && source.children.Count > 0)
                {
                    dest.children = new List<TreeViewItem>(source.children.Count);
                    for (int i = 0; i < source.children.Count; ++i)
                    {
                        BackendData.Foo s = source.children[i];
                        dest.children.Add(new FooTreeViewItem(s.id, dest.depth + 1, dest, s.name, s));
                        ++itemCounter;
                        AddVisibleChildrenRecursive(s, dest.children[i]);
                    }
                }
            }
            else
            {
                if (source.hasChildren)
                {
                    dest.children = CreateChildListForCollapsedParent(); // ensure we show the collapse arrow (because we do not fetch data for collapsed items)
                }
            }
        }

        public override bool CanBeParent(TreeViewItem item)
        {
            return item.hasChildren;
        }

        protected override HashSet<int> GetParentsAbove(int id)
        {
            HashSet<int> parentsAbove = new HashSet<int>();
            BackendData.Foo target = BackendData.FindItemRecursive(m_Backend.root, id);

            while (target != null)
            {
                if (target.parent != null)
                    parentsAbove.Add(target.parent.id);
                target = target.parent;
            }
            return parentsAbove;
        }

        protected override HashSet<int> GetParentsBelow(int id)
        {
            HashSet<int> parents = m_Backend.GetParentsBelow(id);
            return parents;
        }
    }
} // UnityEditor
