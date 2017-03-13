// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;


namespace UnityEditor.TreeViewExamples
{
    class TestDataSource : TreeViewDataSource
    {
        private BackendData m_Backend;
        public int itemCounter { get; private set; }

        public TestDataSource(TreeViewController treeView, BackendData data) : base(treeView)
        {
            m_Backend = data;
            FetchData();
        }

        public override void FetchData()
        {
            itemCounter = 1;
            m_RootItem = new FooTreeViewItem(m_Backend.root.id, 0, null, m_Backend.root.name, m_Backend.root);
            AddChildrenRecursive(m_Backend.root, m_RootItem);
            m_NeedRefreshRows = true;
        }

        void AddChildrenRecursive(BackendData.Foo source, TreeViewItem dest)
        {
            if (source.hasChildren)
            {
                dest.children = new List<TreeViewItem>(source.children.Count);
                for (int i = 0; i < source.children.Count; ++i)
                {
                    BackendData.Foo s = source.children[i];
                    dest.children.Add(new FooTreeViewItem(s.id, dest.depth + 1, dest, s.name, s));
                    itemCounter++;
                    AddChildrenRecursive(s, dest.children[i]);
                }
            }
        }

        public override bool CanBeParent(TreeViewItem item)
        {
            return true;
        }
    }
} // UnityEditor
