// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditor.StyleSheets;

namespace UnityEditor
{
    internal class SettingsTreeView : TreeView
    {
        private struct SettingsNode
        {
            public string path;
            public Dictionary<string, SettingsNode> children;
        }

        private static class Styles
        {
            public static StyleBlock tree => EditorResources.GetStyle("settings-tree");
            public static GUIStyle listItem = "SettingsListItem";
            public static GUIStyle treeItem = "SettingsTreeItem";
        }

        private bool m_ListViewMode;

        public SettingsProvider[] providers { get; }
        public SettingsProvider currentProvider { get; private set; }
        public string searchContext { get; set; }

        public delegate void ProviderChangedHandler(SettingsProvider lastSelectedProvider, SettingsProvider newlySelectedProvider);
        public event ProviderChangedHandler currentProviderChanged;

        public SettingsTreeView(SettingsProvider[] providers)
            : base(new TreeViewState())
        {
            this.providers = providers;
            Reload();
            ExpandAll();
        }

        public void FocusSelection(int selectedId)
        {
            List<int> selectedIDs = new List<int> { selectedId };
            SetSelection(selectedIDs);
            SelectionChanged(selectedIDs);
        }

        public List<SettingsProvider> GetChildrenAtPath(string path)
        {
            List<SettingsProvider> children = null;
            var pathItem = FindItem(path.GetHashCode(), rootItem);
            if (pathItem != null)
                children = pathItem.children.Select(item => FindProviderById(item.id)).Where(p => p != null).ToList();
            return children ?? new List<SettingsProvider>();
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            SettingsProvider selectedProvider = GetFirstValidProvider(selectedIds.Count > 0 ? selectedIds.First() : -1);
            currentProviderChanged?.Invoke(currentProvider, selectedProvider);
            currentProvider = selectedProvider;
        }

        protected SettingsProvider GetFirstValidProvider(int id)
        {
            if (id == -1)
                return null;

            var treeViewItem = FindItem(id, rootItem);
            var provider = FindProviderById(id);
            while (provider == null && treeViewItem != null)
            {
                if (treeViewItem.children.Count <= 0)
                    break;

                treeViewItem = treeViewItem.children.First();
                provider = FindProviderById(treeViewItem.id);
            }

            return provider;
        }

        private SettingsProvider FindProviderById(int id)
        {
            return providers.FirstOrDefault(p => p.settingsPath.GetHashCode() == id);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var labelRect = args.rowRect;
            var contentIndent = GetContentIndent(args.item);
            if (!m_ListViewMode)
            {
                labelRect.xMin += contentIndent;
            }

            if (Styles.tree.GetBool("-unity-show-icon") &&  args.item.icon != null)
            {
                const float k_IconSize = 16.0f;
                var iconRect = labelRect;
                iconRect.xMin -= k_IconSize;
                iconRect.xMax = iconRect.xMin + k_IconSize;
                GUI.DrawTexture(iconRect, args.item.icon);
            }

            EditorGUI.LabelField(labelRect, args.item.displayName, m_ListViewMode ? Styles.listItem : Styles.treeItem);
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (base.DoesItemMatchSearch(item, search))
                return true;

            var provider = FindProviderById(item.id);
            return provider != null && provider.HasSearchInterest(search);
        }

        protected override void SearchChanged(string newSearch)
        {
            base.SearchChanged(newSearch);
            var rows = GetRows();
            if (rows.Count == 1 || (rows.Count > 1 && !GetSelection().Any(selectedId => rows.Any(r => r.id == selectedId))))
                SetSelection(new[] { rows[0].id }, TreeViewSelectionOptions.FireSelectionChanged);
        }

        protected override TreeViewItem BuildRoot()
        {
            SettingsNode rootNode = new SettingsNode() { children = new Dictionary<string, SettingsNode>() };
            BuildSettingsNodeTree(rootNode);

            var allItems = new List<TreeViewItem>();
            AppendSettingsNode(rootNode, "", 0, allItems);

            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        private void BuildSettingsNodeTree(SettingsNode rootNode)
        {
            // If all provider have same root, hide the root name:
            var allChildrenUnderSameRoot = true;
            m_ListViewMode = true;
            string rootName = null;
            foreach (var provider in providers)
            {
                if (rootName == null)
                {
                    rootName = provider.pathTokens[0];
                }
                else if (rootName != provider.pathTokens[0])
                {
                    allChildrenUnderSameRoot = false;
                    m_ListViewMode = false;
                }
                else if (provider.pathTokens.Length > 2)
                {
                    m_ListViewMode = false;
                }
            }

            foreach (var provider in providers)
            {
                SettingsNode current = rootNode;
                var nodePath = allChildrenUnderSameRoot ? rootName : "";
                for (var tokenIndex = allChildrenUnderSameRoot ?  1 : 0; tokenIndex < provider.pathTokens.Length; ++tokenIndex)
                {
                    var token = provider.pathTokens[tokenIndex];
                    if (nodePath.Length > 0)
                        nodePath += "/";
                    nodePath += token;
                    if (!current.children.ContainsKey(token))
                    {
                        current.children[token] = new SettingsNode() { path = nodePath, children = new Dictionary<string, SettingsNode>() };
                    }

                    current = current.children[token];
                }
            }
        }

        private void AppendSettingsNode(SettingsNode node, string rootPath, int depth, ICollection<TreeViewItem> items)
        {
            var sortedChildNames = node.children.Keys.ToList();
            sortedChildNames.Sort();
            foreach (var nodeName in sortedChildNames)
            {
                var childNode = node.children[nodeName];
                var childNodePath = rootPath.Length == 0 ? nodeName : rootPath + "/" + nodeName;
                var id = childNode.path.GetHashCode();
                var provider = FindProviderById(id);
                items.Add(new TreeViewItem { id = id, depth = depth, displayName = provider != null ? provider.label : nodeName, icon = provider?.icon });
                AppendSettingsNode(childNode, childNodePath, depth + 1, items);
            }
        }
    }
}
