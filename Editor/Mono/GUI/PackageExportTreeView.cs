// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Utils;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class PackageExportTreeView
    {
        TreeViewController m_TreeView;
        List<PackageExportTreeViewItem> m_Selection = new List<PackageExportTreeViewItem>();
        static readonly bool s_UseFoldouts = true;
        public enum EnabledState
        {
            NotSet = -1,
            None = 0,
            All = 1,
            Mixed = 2
        };

        private PackageExport m_PackageExport;

        public ExportPackageItem[] items { get { return m_PackageExport.items; } }

        public PackageExportTreeView(PackageExport packageExport, TreeViewState treeViewState, Rect startRect)
        {
            m_PackageExport = packageExport;

            m_TreeView     = new TreeViewController(m_PackageExport, treeViewState);
            var dataSource = new PackageExportTreeViewDataSource(m_TreeView, this);
            var gui        = new PackageExportTreeViewGUI(m_TreeView, this);

            m_TreeView.Init(startRect, dataSource, gui, null);
            m_TreeView.ReloadData();
            m_TreeView.selectionChangedCallback += SelectionChanged;
            gui.itemWasToggled += ItemWasToggled;

            ComputeEnabledStateForFolders();
        }

        void ComputeEnabledStateForFolders()
        {
            var root = m_TreeView.data.root as PackageExportTreeViewItem;
            var done = new HashSet<PackageExportTreeViewItem>();
            done.Add(root); // Dont compute for root: mark it as done
            RecursiveComputeEnabledStateForFolders(root, done);
        }

        void RecursiveComputeEnabledStateForFolders(PackageExportTreeViewItem pitem, HashSet<PackageExportTreeViewItem> done)
        {
            if (!pitem.isFolder)
                return;

            // Depth first recursion to allow parent folders be dependant on child folders

            // Recurse
            if (pitem.hasChildren)
            {
                foreach (var child in pitem.children)
                {
                    RecursiveComputeEnabledStateForFolders(child as PackageExportTreeViewItem, done);
                }
            }

            // Now do logic
            if (!done.Contains(pitem))
            {
                EnabledState enabledState = GetFolderChildrenEnabledState(pitem);
                pitem.enabledState = enabledState;

                // If 'item' is mixed then all of its parents will also be mixed
                if (enabledState == EnabledState.Mixed)
                {
                    done.Add(pitem);
                    var current = pitem.parent as PackageExportTreeViewItem;
                    while (current != null)
                    {
                        if (!done.Contains(current))
                        {
                            current.enabledState = EnabledState.Mixed;
                            done.Add(current);
                        }
                        current = current.parent as PackageExportTreeViewItem;
                    }
                }
            }
        }

        EnabledState GetFolderChildrenEnabledState(PackageExportTreeViewItem folder)
        {
            if (!folder.isFolder)
                Debug.LogError("Should be a folder item!");

            if (!folder.hasChildren)
                return EnabledState.None;

            EnabledState amount = EnabledState.NotSet;

            var firstChild = folder.children[0] as PackageExportTreeViewItem;
            EnabledState initial = firstChild.enabledState;
            for (int i = 1; i < folder.children.Count; ++i)
            {
                var child = folder.children[i] as PackageExportTreeViewItem;
                if (initial != child.enabledState)
                {
                    amount = EnabledState.Mixed;
                    break;
                }
            }

            if (amount == EnabledState.NotSet)
            {
                amount = initial == EnabledState.All ? EnabledState.All : EnabledState.None;
            }

            return amount;
        }

        void SelectionChanged(int[] selectedIDs)
        {
            // Cache selected tree view items (from ids)
            m_Selection = new List<PackageExportTreeViewItem>();
            var visibleItems = m_TreeView.data.GetRows();
            foreach (var visibleItem in visibleItems)
            {
                if (selectedIDs.Contains(visibleItem.id))
                {
                    var pitem = visibleItem as PackageExportTreeViewItem;
                    if (pitem != null)
                        m_Selection.Add(pitem);
                }
            }
        }

        public void OnGUI(Rect rect)
        {
            int keyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            m_TreeView.OnGUI(rect, keyboardControlID);

            // Keyboard space toggles selection enabledness
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space &&
                m_Selection != null && m_Selection.Count > 0 && GUIUtility.keyboardControl == keyboardControlID)
            {
                EnabledState newEnabled = m_Selection[0].enabledState != EnabledState.All ? EnabledState.All : EnabledState.None;
                m_Selection[0].enabledState = newEnabled;
                ItemWasToggled(m_Selection[0]);

                Event.current.Use();
            }
        }

        public void SetAllEnabled(EnabledState enabled)
        {
            EnableChildrenRecursive(m_TreeView.data.root, enabled);
            ComputeEnabledStateForFolders();
        }

        void ItemWasToggled(PackageExportTreeViewItem pitem)
        {
            if (m_Selection.Count <= 1)
            {
                EnableChildrenRecursive(pitem, pitem.enabledState);
            }
            else
            {
                foreach (var i in m_Selection)
                    i.enabledState = pitem.enabledState;
            }

            ComputeEnabledStateForFolders();
        }

        void EnableChildrenRecursive(TreeViewItem parentItem, EnabledState enabled)
        {
            if (!parentItem.hasChildren)
                return;

            foreach (TreeViewItem tvitem in parentItem.children)
            {
                var pitem = tvitem as PackageExportTreeViewItem;
                pitem.enabledState = enabled;
                EnableChildrenRecursive(pitem, enabled);
            }
        }

        // Item
        private class PackageExportTreeViewItem : TreeViewItem
        {
            public ExportPackageItem item { get; set; }

            private EnabledState m_EnabledState = EnabledState.NotSet;
            public EnabledState enabledState
            {
                get { return item != null ? (EnabledState)item.enabledStatus : m_EnabledState; }
                set { if (item != null) item.enabledStatus = (int)value; else m_EnabledState = value; }
            }

            // We assume that items that don't have ExportPackageItem assigned, are folders.
            public bool isFolder
            {
                get { return item != null ? item.isFolder : true; }
            }

            public PackageExportTreeViewItem(ExportPackageItem itemIn, int id, int depth, TreeViewItem parent, string displayName)
                : base(id, depth, parent, displayName)
            {
                item = itemIn;
            }
        }

        // Gui

        private class PackageExportTreeViewGUI : TreeViewGUI
        {
            internal static class Constants
            {
                public static Texture2D folderIcon   = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName);
            }

            public Action<PackageExportTreeViewItem> itemWasToggled;
            public int showPreviewForID { get; set; }

            private PackageExportTreeView m_PackageExportView;
            protected float k_FoldoutWidth = 12f;

            public PackageExportTreeViewGUI(TreeViewController treeView, PackageExportTreeView view)
                : base(treeView)
            {
                m_PackageExportView = view;

                k_BaseIndent = 4f;
                if (!s_UseFoldouts)
                    k_FoldoutWidth = 0f;
            }

            override public void OnRowGUI(Rect rowRect, TreeViewItem tvItem, int row, bool selected, bool focused)
            {
                k_IndentWidth = 18;
                k_FoldoutWidth = 18;
                const float k_ToggleWidth = 18f;

                var pitem = tvItem as PackageExportTreeViewItem;

                bool repainting = Event.current.type == EventType.Repaint;

                // 0. Selection row rect
                if (selected && repainting)
                    Styles.selectionStyle.Draw(rowRect, false, false, true, focused);

                // 1. Foldout
                if (m_TreeView.data.IsExpandable(tvItem))
                    DoFoldout(rowRect, tvItem, row);

                // 2. Toggle only for items that are actually in the package.
                Rect toggleRect = new Rect(k_BaseIndent + tvItem.depth * indentWidth + k_FoldoutWidth, rowRect.y, k_ToggleWidth, rowRect.height);

                DoToggle(pitem, toggleRect);

                // 3. Icon & Text
                // Display folders that will not be included into the package as disabled.
                using (new EditorGUI.DisabledScope(pitem.item == null))
                {
                    Rect contentRect = new Rect(toggleRect.xMax, rowRect.y, rowRect.width, rowRect.height);
                    DoIconAndText(pitem, contentRect, selected, focused);
                }
            }

            static void Toggle(ExportPackageItem[] items, PackageExportTreeViewItem pitem, Rect toggleRect)
            {
                bool enabled = pitem.enabledState > EnabledState.None;

                GUIStyle style = EditorStyles.toggle;
                bool setMixed = pitem.isFolder && (pitem.enabledState == EnabledState.Mixed);
                if (setMixed)
                    style = EditorStyles.toggleMixed;

                bool newEnabled =  GUI.Toggle(toggleRect, enabled, GUIContent.none, style);
                if (newEnabled != enabled)
                    pitem.enabledState = newEnabled ? EnabledState.All : EnabledState.None;
            }

            void DoToggle(PackageExportTreeViewItem pitem, Rect toggleRect)
            {
                // Toggle on/off
                EditorGUI.BeginChangeCheck();
                Toggle(m_PackageExportView.items, pitem, toggleRect);
                if (EditorGUI.EndChangeCheck())
                {
                    // Only change selection if we already have single selection (Keep multi-selection when toggling)
                    if (m_TreeView.GetSelection().Length <= 1 || !m_TreeView.GetSelection().Contains(pitem.id))
                    {
                        m_TreeView.SetSelection(new int[] { pitem.id }, false);
                        m_TreeView.NotifyListenersThatSelectionChanged();
                    }
                    if (itemWasToggled != null)
                        itemWasToggled(pitem);
                    Event.current.Use();
                }
            }

            void DoIconAndText(PackageExportTreeViewItem item, Rect contentRect, bool selected, bool focused)
            {
                EditorGUIUtility.SetIconSize(new Vector2(k_IconWidth, k_IconWidth)); // If not set we see icons scaling down if text is being cropped
                GUIStyle lineStyle = Styles.lineStyle;
                lineStyle.padding.left = 0; // padding could have been set by other tree views
                contentRect.height += 5; // with the default row height, underscore and lower parts of characters like g, p, etc. were not visible
                if (Event.current.type == EventType.Repaint)
                    lineStyle.Draw(contentRect, GUIContent.Temp(item.displayName, GetIconForItem(item)), false, false, selected, focused);
                EditorGUIUtility.SetIconSize(Vector2.zero);
            }

            protected override Texture GetIconForItem(TreeViewItem tItem)
            {
                var pItem = tItem as PackageExportTreeViewItem;
                var item  = pItem.item;

                // Undefined items are always folders.
                if (item == null || item.isFolder)
                {
                    return Constants.folderIcon;
                }

                // We are using this TreeViewGUI when importing and exporting a package, so handle both situations:

                // Exporting a package can use cached icons (icons we generate on import)
                Texture cachedIcon = AssetDatabase.GetCachedIcon(item.assetPath);
                if (cachedIcon != null)
                    return cachedIcon;

                // Importing a package have to use icons based on file extension
                return InternalEditorUtility.GetIconForFile(item.assetPath);
            }

            protected override void RenameEnded()
            {
            }
        }

        // Datasource

        private class PackageExportTreeViewDataSource : TreeViewDataSource
        {
            private PackageExportTreeView m_PackageExportView;

            public PackageExportTreeViewDataSource(TreeViewController treeView, PackageExportTreeView view)
                : base(treeView)
            {
                m_PackageExportView = view;
                rootIsCollapsable = false;
                showRootItem = false;
            }

            public override bool IsRenamingItemAllowed(TreeViewItem item)
            {
                return false;
            }

            public override bool IsExpandable(TreeViewItem item)
            {
                if (!s_UseFoldouts)
                    return false;

                return base.IsExpandable(item);
            }

            public override void FetchData()
            {
                int rootDepth = -1; // -1 so its children will have 0 depth
                m_RootItem = new PackageExportTreeViewItem(null, "Assets".GetHashCode(), rootDepth, null, "InvisibleAssetsFolder");

                bool initExpandedState = true;
                if (initExpandedState)
                    m_TreeView.state.expandedIDs.Add(m_RootItem.id);

                ExportPackageItem[] items = m_PackageExportView.items;

                Dictionary<string, PackageExportTreeViewItem> treeViewFolders = new Dictionary<string, PackageExportTreeViewItem>();
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];

                    if (PackageImport.HasInvalidCharInFilePath(item.assetPath))
                        continue; // Do not add invalid paths (we already warn the user with a dialog in PackageImport.cs)

                    string filename   = Path.GetFileName(item.assetPath).ConvertSeparatorsToUnity();
                    string folderPath = Path.GetDirectoryName(item.assetPath).ConvertSeparatorsToUnity();

                    // Ensure folders. This is for when installed packages have been moved to other folders.
                    TreeViewItem targetFolder = EnsureFolderPath(folderPath, treeViewFolders, initExpandedState);

                    // Add file to folder
                    if (targetFolder != null)
                    {
                        int id = item.assetPath.GetHashCode();
                        var newItem = new PackageExportTreeViewItem(item, id, targetFolder.depth + 1, targetFolder, filename);
                        targetFolder.AddChild(newItem);

                        if (initExpandedState)
                            m_TreeView.state.expandedIDs.Add(id);

                        // We need to ensure that the folder is available for
                        // EnsureFolderPath on subsequent iterations.
                        if (item.isFolder)
                            treeViewFolders[item.assetPath] = newItem;
                    }
                }

                if (initExpandedState)
                    m_TreeView.state.expandedIDs.Sort();
            }

            TreeViewItem EnsureFolderPath(string folderPath, Dictionary<string, PackageExportTreeViewItem> treeViewFolders, bool initExpandedState)
            {
                // We're in the root folder, so just return the root item as the parent.
                if (folderPath == "")
                    return m_RootItem;

                // Does folder path exist?
                int id = folderPath.GetHashCode();
                TreeViewItem item = TreeViewUtility.FindItem(id, m_RootItem);

                if (item != null)
                    return item;

                // Add folders as needed
                string[] splitPath = folderPath.Split('/');
                string currentPath = "";
                TreeViewItem currentItem = m_RootItem;
                int folderDepth = -1; // Will be incremented to the right depth in the loop.

                for (int depth = 0; depth < splitPath.Length; ++depth)
                {
                    string folder = splitPath[depth];
                    if (currentPath != "")
                        currentPath += '/';

                    currentPath += folder;

                    // Dont create a 'Assets' folder (we already have that as a hidden root)
                    if (depth == 0 && currentPath == "Assets")
                        continue;

                    // Only increment the folder depth if we are past the root "Assets" folder.
                    ++folderDepth;

                    id = currentPath.GetHashCode();

                    PackageExportTreeViewItem foundItem;
                    if (treeViewFolders.TryGetValue(currentPath, out foundItem))
                    {
                        currentItem = foundItem;
                    }
                    else
                    {
                        // If we do not have a tree view item for this folder we create one
                        var folderItem = new PackageExportTreeViewItem(null, id, folderDepth, currentItem, folder);

                        // Add to children array of the parent
                        currentItem.AddChild(folderItem);
                        currentItem = folderItem;

                        // Auto expand all folder items
                        if (initExpandedState)
                            m_TreeView.state.expandedIDs.Add(id);

                        // For faster finding of folders
                        treeViewFolders[currentPath] = folderItem;
                    }
                }

                return currentItem;
            }
        }
    }
}
