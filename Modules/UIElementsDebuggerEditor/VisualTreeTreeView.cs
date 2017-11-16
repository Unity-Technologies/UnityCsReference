// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.Debugger
{
    class VisualTreeItem : TreeViewItem
    {
        public readonly VisualElement elt;

        public VisualTreeItem(VisualElement elt, int depth) : base((int)elt.controlid, depth, GetDisplayName(elt))
        {
            this.elt = elt;
        }

        private static string GetDisplayName(VisualElement elt)
        {
            string t = elt.GetType() == typeof(VisualElement) ? String.Empty : (elt.GetType().Name + " ");
            string n = String.IsNullOrEmpty(elt.name) ? String.Empty : ("#" + elt.name + " ");
            string res = t + n + (elt.GetClasses().Any() ? ("." + string.Join(",.", elt.GetClasses().ToArray())) : String.Empty);
            if (res == String.Empty)
                return elt.GetType().Name;
            return res;
        }

        public uint controlId { get { return elt.controlid; } }
    }

    class VisualTreeTreeView : TreeView
    {
        public VisualTreeTreeView(TreeViewState state)
            : base(state) {}

        public Panel panel;
        public bool includeShadowHierarchy;

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(0, -1);
            Recurse(root, panel.visualTree);
            return root;
        }

        private void Recurse(TreeViewItem tree, VisualElement elt)
        {
            var child = new VisualTreeItem(elt, tree.depth + 1);
            tree.AddChild(child);

            IEnumerable<VisualElement> childElements = includeShadowHierarchy
                ? elt.shadow.Children()
                : (elt.contentContainer == null ? Enumerable.Empty<VisualElement>() : elt.Children());
            foreach (VisualElement childElement in childElements)
            {
                Recurse(child, childElement);
            }
        }

        public VisualTreeItem GetNodeFor(int selectedId)
        {
            return FindRows(new List<int> { selectedId }).FirstOrDefault() as VisualTreeItem;
        }
    }
}
