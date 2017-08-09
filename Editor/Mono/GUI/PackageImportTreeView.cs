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
    internal class PackageImportTreeView
    {
        TreeViewController m_TreeView;
        List<PackageImportTreeViewItem> m_Selection = new List<PackageImportTreeViewItem>();
        static readonly bool s_UseFoldouts = true;
        public enum EnabledState
        {
            NotSet = -1,
            None = 0,
            All = 1,
            Mixed = 2
        };

        private PackageImport m_PackageImport;

        public bool canReInstall { get { return m_PackageImport.canReInstall; } }
        public bool doReInstall { get { return m_PackageImport.doReInstall; } }
        public ImportPackageItem[] packageItems { get { return m_PackageImport.packageItems; } }


        public PackageImportTreeView(PackageImport packageImport, TreeViewState treeViewState, Rect startRect)
        {
            m_PackageImport = packageImport;

            m_TreeView = new TreeViewController(m_PackageImport, treeViewState);
            var dataSource = new PackageImportTreeViewDataSource(m_TreeView, this);
            var gui        = new PackageImportTreeViewGUI(m_TreeView, this);

            m_TreeView.Init(startRect, dataSource, gui, null);
            m_TreeView.ReloadData();
            m_TreeView.selectionChangedCallback += SelectionChanged;
            gui.itemWasToggled += ItemWasToggled;

            ComputeEnabledStateForFolders();
        }

        void ComputeEnabledStateForFolders()
        {
            var root = m_TreeView.data.root as PackageImportTreeViewItem;
            var done = new HashSet<PackageImportTreeViewItem>();
            done.Add(root); // Dont compute for root: mark it as done
            RecursiveComputeEnabledStateForFolders(root, done);
        }

        void RecursiveComputeEnabledStateForFolders(PackageImportTreeViewItem pitem, HashSet<PackageImportTreeViewItem> done)
        {
            if (pitem.item != null && !pitem.item.isFolder)
                return;

            // Depth first recursion to allow parent folders be dependant on child folders

            // Recurse
            if (pitem.hasChildren)
            {
                foreach (var child in pitem.children)
                {
                    RecursiveComputeEnabledStateForFolders(child as PackageImportTreeViewItem, done);
                }
            }

            // Now do logic
            if (!done.Contains(pitem))
            {
                EnabledState amount = GetFolderChildrenEnabledState(pitem);
                pitem.enableState = amount;

                // If 'item' is mixed then all of its parents will also be mixed
                if (amount == EnabledState.Mixed)
                {
                    done.Add(pitem);
                    var current = pitem.parent as PackageImportTreeViewItem;
                    while (current != null)
                    {
                        if (!done.Contains(current))
                        {
                            current.enableState = EnabledState.Mixed;
                            done.Add(current);
                        }
                        current = current.parent as PackageImportTreeViewItem;
                    }
                }
            }
        }

        bool ItemShouldBeConsideredForEnabledCheck(PackageImportTreeViewItem pitem)
        {
            // Not even an item
            if (pitem == null)
                return false;

            // item was a folder that had to be created
            // in this treeview.
            if (pitem.item == null)
                return true;

            var item = pitem.item;
            // Its a package asset, its changed or we are doing a re-install
            if (item.projectAsset || !(item.isFolder || item.assetChanged || doReInstall))
                return false;

            return true;
        }

        EnabledState GetFolderChildrenEnabledState(PackageImportTreeViewItem folder)
        {
            if (folder.item != null && !folder.item.isFolder)
                Debug.LogError("Should be a folder item!");

            if (!folder.hasChildren)
                return EnabledState.None;

            EnabledState amount = EnabledState.NotSet;

            int i = 0;
            for (; i < folder.children.Count; ++i)
            {
                // We dont want to consider project assets in this calculation as they are
                // ignored
                var firstValidChild = folder.children[i] as PackageImportTreeViewItem;
                if (ItemShouldBeConsideredForEnabledCheck(firstValidChild))
                {
                    amount = firstValidChild.enableState;
                    break;
                }
            }

            ++i;
            for (; i < folder.children.Count; ++i)
            {
                // We dont want to consider project assets in this calculation as they are
                // ignored
                var childItem = folder.children[i] as PackageImportTreeViewItem;
                if (ItemShouldBeConsideredForEnabledCheck(childItem))
                {
                    if (amount != childItem.enableState)
                    {
                        amount = EnabledState.Mixed;
                        break;
                    }
                }
            }

            if (amount == EnabledState.NotSet)
                return EnabledState.None;

            return amount;
        }

        void SelectionChanged(int[] selectedIDs)
        {
            // Cache selected tree view items (from ids)
            m_Selection = new List<PackageImportTreeViewItem>();
            var visibleItems = m_TreeView.data.GetRows();
            foreach (var visibleItem in visibleItems)
            {
                if (selectedIDs.Contains(visibleItem.id))
                {
                    var pitem = visibleItem as PackageImportTreeViewItem;
                    if (pitem != null)
                        m_Selection.Add(pitem);
                }
            }

            // Show preview on selection
            var selectedItem = m_Selection[0].item;
            if (m_Selection.Count == 1 && selectedItem != null && !string.IsNullOrEmpty(selectedItem.previewPath))
            {
                var gui = m_TreeView.gui as PackageImportTreeViewGUI;
                gui.showPreviewForID = m_Selection[0].id;
            }
            else
            {
                PopupWindowWithoutFocus.Hide();
            }
        }

        public void OnGUI(Rect rect)
        {
            // Remove preview popup on mouse scroll wheel events
            if (Event.current.type == EventType.ScrollWheel)
                PopupWindowWithoutFocus.Hide();

            int keyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            m_TreeView.OnGUI(rect, keyboardControlID);

            // Keyboard space toggles selection enabledness
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space &&
                m_Selection != null && m_Selection.Count > 0 && GUIUtility.keyboardControl == keyboardControlID)
            {
                var pitem = m_Selection[0];
                if (pitem != null)
                {
                    EnabledState newEnabled = (pitem.enableState == EnabledState.None) ? EnabledState.All : EnabledState.None;
                    pitem.enableState = newEnabled;
                    ItemWasToggled(m_Selection[0]);
                }

                Event.current.Use();
            }
        }

        public void SetAllEnabled(EnabledState state)
        {
            EnableChildrenRecursive(m_TreeView.data.root, state);
            ComputeEnabledStateForFolders();
        }

        void ItemWasToggled(PackageImportTreeViewItem pitem)
        {
            if (m_Selection.Count <= 1)
            {
                EnableChildrenRecursive(pitem, pitem.enableState);
            }
            else
            {
                foreach (var childPItem in m_Selection)
                {
                    childPItem.enableState = pitem.enableState;
                }
            }

            ComputeEnabledStateForFolders();
        }

        void EnableChildrenRecursive(TreeViewItem parentItem, EnabledState state)
        {
            if (!parentItem.hasChildren)
                return;

            foreach (TreeViewItem tvitem in parentItem.children)
            {
                var pitem = tvitem as PackageImportTreeViewItem;
                pitem.enableState = state;

                EnableChildrenRecursive(pitem, state);
            }
        }

        // Item

        private class PackageImportTreeViewItem : TreeViewItem
        {
            public ImportPackageItem item { get; set; }

            public EnabledState enableState
            {
                get { return m_EnableState; }
                set
                {
                    // We only want to set the enabled state if the item
                    // is not a project asset.
                    if (item == null || !item.projectAsset)
                    {
                        m_EnableState = value;
                        if (item != null)
                            item.enabledStatus = (int)value;
                    }
                }
            }

            public PackageImportTreeViewItem(ImportPackageItem itemIn, int id, int depth, TreeViewItem parent, string displayName)
                : base(id, depth, parent, displayName)
            {
                item = itemIn;

                if (item == null)
                    m_EnableState = EnabledState.All;
                else
                    m_EnableState = (EnabledState)item.enabledStatus;
            }

            private EnabledState m_EnableState;
        }

        // Gui

        private class PackageImportTreeViewGUI : TreeViewGUI
        {
            internal static class Constants
            {
                public static Texture2D folderIcon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName);
                public static GUIContent badgeNew    = EditorGUIUtility.IconContent("PackageBadgeNew", "|This is a new Asset");
                public static GUIContent badgeDelete = EditorGUIUtility.IconContent("PackageBadgeDelete", "|These files will be deleted!");
                public static GUIContent badgeWarn   = EditorGUIUtility.IconContent("console.warnicon", "|Warning: File exists in project, but with different GUID. Will override existing asset which may be undesired.");
                public static GUIContent badgeChange = EditorGUIUtility.IconContent("playLoopOff", "|This file is new or has changed.");

                public static GUIStyle   paddinglessStyle;

                static Constants()
                {
                    paddinglessStyle = new GUIStyle();
                    paddinglessStyle.padding = new RectOffset(0, 0, 0, 0);
                }
            }

            public Action<PackageImportTreeViewItem> itemWasToggled;
            public int showPreviewForID { get; set; }

            private PackageImportTreeView m_PackageImportView;
            protected float k_FoldoutWidth = 12f;

            public PackageImportTreeViewGUI(TreeViewController treeView, PackageImportTreeView view)
                : base(treeView)
            {
                m_PackageImportView = view;

                k_BaseIndent = 4f;
                if (!s_UseFoldouts)
                    k_FoldoutWidth = 0f;
            }

            override public void OnRowGUI(Rect rowRect, TreeViewItem tvItem, int row, bool selected, bool focused)
            {
                k_IndentWidth = 18;
                k_FoldoutWidth = 18;
                const float k_ToggleWidth = 18f;

                var pitem = tvItem as PackageImportTreeViewItem;
                var item = pitem.item;

                bool repainting = Event.current.type == EventType.Repaint;

                // 0. Selection row rect
                if (selected && repainting)
                    Styles.selectionStyle.Draw(rowRect, false, false, true, focused);

                bool validItem    = (item != null);
                bool isFolder     = (item != null) ? item.isFolder     : true;
                bool assetChanged = (item != null) ? item.assetChanged : false;
                bool pathConflict = (item != null) ? item.pathConflict : false;
                bool exists       = (item != null) ? item.exists       : true;
                bool projectAsset = (item != null) ? item.projectAsset : false;
                bool doReInstall  = m_PackageImportView.doReInstall;

                // 1. Foldout
                if (m_TreeView.data.IsExpandable(tvItem))
                    DoFoldout(rowRect, tvItem, row);

                // 2. Toggle only for items that are actually in the package.
                Rect toggleRect = new Rect(k_BaseIndent + tvItem.depth * indentWidth + k_FoldoutWidth, rowRect.y, k_ToggleWidth, rowRect.height);

                if ((isFolder && !projectAsset) || (validItem && !projectAsset && (assetChanged || doReInstall)))
                    DoToggle(pitem, toggleRect);

                using (new EditorGUI.DisabledScope(!validItem || projectAsset))
                {
                    // 3. Icon & Text
                    Rect contentRect = new Rect(toggleRect.xMax, rowRect.y, rowRect.width, rowRect.height);
                    DoIconAndText(tvItem, contentRect, selected, focused);

                    // 4. Preview popup
                    DoPreviewPopup(pitem, rowRect);

                    // 4.5 Warning about file clashing.
                    if (repainting && validItem && pathConflict)
                    {
                        Rect labelRect = new Rect(rowRect.xMax - 58, rowRect.y, rowRect.height, rowRect.height);
                        EditorGUIUtility.SetIconSize(new Vector2(rowRect.height, rowRect.height));
                        GUI.Label(labelRect, Constants.badgeWarn);
                        EditorGUIUtility.SetIconSize(Vector2.zero);
                    }


                    // 5. Optional badge ("New")
                    if (repainting && validItem && !(exists || pathConflict))
                    {
                        // FIXME: Need to enable tooltips here.
                        Texture badge = Constants.badgeNew.image;
                        Rect labelRect = new Rect(rowRect.xMax - badge.width - 6, rowRect.y + (rowRect.height - badge.height) / 2, badge.width, badge.height);
                        GUI.Label(labelRect, Constants.badgeNew, Constants.paddinglessStyle);
                    }

                    // 5. Optional badge ("Delete")
                    if (repainting && doReInstall && projectAsset)
                    {
                        // FIXME: Need to enable tooltips here.
                        Texture badge = Constants.badgeDelete.image;
                        Rect labelRect = new Rect(rowRect.xMax - badge.width - 6, rowRect.y + (rowRect.height - badge.height) / 2, badge.width, badge.height);
                        GUI.Label(labelRect, Constants.badgeDelete, Constants.paddinglessStyle);
                    }

                    // 7. Show what stuff has changed
                    if (repainting && validItem && (exists || pathConflict) && assetChanged)
                    {
                        Texture badge = Constants.badgeChange.image;
                        Rect labelRect = new Rect(rowRect.xMax - badge.width - 6, rowRect.y, rowRect.height, rowRect.height);
                        GUI.Label(labelRect, Constants.badgeChange, Constants.paddinglessStyle);
                    }
                }
            }

            static void Toggle(ImportPackageItem[] items, PackageImportTreeViewItem pitem, Rect toggleRect)
            {
                bool enabled = (int)pitem.enableState > 0;
                bool isFolder = (pitem.item == null) || pitem.item.isFolder;

                GUIStyle style = EditorStyles.toggle;
                bool setMixed = isFolder && (pitem.enableState == EnabledState.Mixed);
                if (setMixed)
                    style = EditorStyles.toggleMixed;

                bool newEnabled =  GUI.Toggle(toggleRect, enabled, GUIContent.none, style);
                if (newEnabled != enabled)
                    pitem.enableState = newEnabled ? EnabledState.All : EnabledState.None;
            }

            void DoToggle(PackageImportTreeViewItem pitem, Rect toggleRect)
            {
                // Toggle on/off
                EditorGUI.BeginChangeCheck();
                Toggle(m_PackageImportView.packageItems, pitem, toggleRect);
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

            void DoPreviewPopup(PackageImportTreeViewItem pitem, Rect rowRect)
            {
                var item = pitem.item;

                if (item != null)
                {
                    // Ensure preview is shown when clicking on an already selected item (the preview might have been closed)
                    if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition) && !PopupWindowWithoutFocus.IsVisible())
                        showPreviewForID = pitem.id;

                    // Show preview
                    if (pitem.id == showPreviewForID && Event.current.type != EventType.Layout)
                    {
                        showPreviewForID = 0;
                        if (!string.IsNullOrEmpty(item.previewPath))
                        {
                            Texture2D preview = PackageImport.GetPreview(item.previewPath);
                            Rect buttonRect = rowRect;
                            buttonRect.width = EditorGUIUtility.currentViewWidth;
                            PopupWindowWithoutFocus.Show(buttonRect, new PreviewPopup(preview), new[] { PopupLocationHelper.PopupLocation.Right, PopupLocationHelper.PopupLocation.Left, PopupLocationHelper.PopupLocation.Below });
                        }
                    }
                }
            }

            void DoIconAndText(TreeViewItem item, Rect contentRect, bool selected, bool focused)
            {
                EditorGUIUtility.SetIconSize(new Vector2(k_IconWidth, k_IconWidth)); // If not set we see icons scaling down if text is being cropped
                GUIStyle lineStyle = Styles.lineStyle;
                lineStyle.padding.left = 0; // padding could have been set by other tree views
                contentRect.height += 5; // with the default row height, underscore and lower parts of characters like g, p, etc. were not visible
                if (Event.current.type == EventType.Repaint)
                    lineStyle.Draw(contentRect, GUIContent.Temp(item.displayName, GetIconForItem(item)), false, false, selected, focused);
                EditorGUIUtility.SetIconSize(Vector2.zero);
            }

            protected override Texture GetIconForItem(TreeViewItem tvItem)
            {
                var ourItem         = tvItem as PackageImportTreeViewItem;
                var item            = ourItem.item;

                // Indefined items are always folders.
                if (item == null || item.isFolder)
                {
                    return Constants.folderIcon;
                }

                // We are using this TreeViewGUI when importing and exporting a package, so handle both situations:

                // Exporting a package can use cached icons (icons we generate on import)
                Texture cachedIcon = AssetDatabase.GetCachedIcon(item.destinationAssetPath);
                if (cachedIcon != null)
                    return cachedIcon;

                // Importing a package have to use icons based on file extension
                return InternalEditorUtility.GetIconForFile(item.destinationAssetPath);
            }

            protected override void RenameEnded()
            {
            }
        }

        // Datasource

        private class PackageImportTreeViewDataSource : TreeViewDataSource
        {
            private PackageImportTreeView m_PackageImportView;

            public PackageImportTreeViewDataSource(TreeViewController treeView, PackageImportTreeView view)
                : base(treeView)
            {
                m_PackageImportView = view;
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
                m_RootItem = new PackageImportTreeViewItem(null, "Assets".GetHashCode(), rootDepth, null, "InvisibleAssetsFolder");

                bool initExpandedState = true;
                if (initExpandedState)
                    m_TreeView.state.expandedIDs.Add(m_RootItem.id);

                ImportPackageItem[] items = m_PackageImportView.packageItems;

                Dictionary<string, PackageImportTreeViewItem> treeViewFolders = new Dictionary<string, PackageImportTreeViewItem>();
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];

                    if (PackageImport.HasInvalidCharInFilePath(item.destinationAssetPath))
                        continue; // Do not add invalid paths (we already warn the user with a dialog in PackageImport.cs)

                    string filename   = Path.GetFileName(item.destinationAssetPath).ConvertSeparatorsToUnity();
                    string folderPath = Path.GetDirectoryName(item.destinationAssetPath).ConvertSeparatorsToUnity();

                    // Ensure folders. This is for when installed packages have been moved to other folders.
                    TreeViewItem targetFolder = EnsureFolderPath(folderPath, treeViewFolders, initExpandedState);

                    // Add file to folder
                    if (targetFolder != null)
                    {
                        int id = item.destinationAssetPath.GetHashCode();
                        var newItem = new PackageImportTreeViewItem(item, id, targetFolder.depth + 1, targetFolder, filename);
                        targetFolder.AddChild(newItem);

                        if (initExpandedState)
                            m_TreeView.state.expandedIDs.Add(id);

                        // We need to ensure that the folder is available for
                        // EnsureFolderPath on subsequent iterations.
                        if (item.isFolder)
                            treeViewFolders[item.destinationAssetPath] = newItem;
                    }
                }

                if (initExpandedState)
                    m_TreeView.state.expandedIDs.Sort();
            }

            TreeViewItem EnsureFolderPath(string folderPath, Dictionary<string, PackageImportTreeViewItem> treeViewFolders, bool initExpandedState)
            {
                //We're in the root folder, so just return the root item as the parent.
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

                    PackageImportTreeViewItem foundItem;
                    if (treeViewFolders.TryGetValue(currentPath, out foundItem))
                    {
                        currentItem = foundItem;
                    }
                    else
                    {
                        // If we do not have a tree view item for this folder we create one
                        var folderItem = new PackageImportTreeViewItem(null, id, folderDepth, currentItem, folder);

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


        class PreviewPopup : PopupWindowContent
        {
            readonly Texture2D m_Preview;
            readonly Vector2 kPreviewSize = new Vector2(128f, 128f);

            public PreviewPopup(Texture2D preview)
            {
                m_Preview = preview;
            }

            public override void OnGUI(Rect rect)
            {
                PackageImport.DrawTexture(rect, m_Preview, false);
            }

            public override Vector2 GetWindowSize()
            {
                return kPreviewSize;
            }
        }
    }
}
