// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    internal class StringListView : TreeView
    {
        private string[] m_Models;

        public event Action<int> elementActivated;

        public StringListView(string[] models, TreeViewState treeViewState = null)
            : base(treeViewState ?? new TreeViewState())
        {
            m_Models = models;
            showAlternatingRowBackgrounds = true;
            Reload();
        }

        public bool IsFirstItemSelected()
        {
            var selection = GetSelection();
            if (selection.Count == 0)
                return false;

            var allRows = GetRows();
            if (allRows.Count == 0)
                return false;
            var selectedItems = FindRows(selection);
            return allRows[0] == selectedItems[0];
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>();
            for (var i = 0; i < m_Models.Length; i++)
            {
                allItems.Add(new TreeViewItem { id = i + 1, depth = 0, displayName = m_Models[i] });
            }
            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            elementActivated?.Invoke(item.id - 1);
        }

        protected override void KeyEvent()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                var selection = GetSelection();
                if (selection.Count == 0)
                    return;

                var item = FindItem(selection[0], rootItem);
                elementActivated?.Invoke(item.id - 1);
            }
        }
    }
}
