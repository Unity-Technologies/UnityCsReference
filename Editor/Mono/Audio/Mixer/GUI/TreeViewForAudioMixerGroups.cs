// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Audio;
using UnityEditorInternal;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Audio;

namespace UnityEditor
{
    internal static class TreeViewForAudioMixerGroup
    {
        public static void CreateAndSetTreeView(ObjectTreeForSelector.TreeSelectorData data)
        {
            var ignoreController = InternalEditorUtility.GetObjectFromInstanceID(data.userData) as AudioMixerController;

            // Create treeview
            var treeView = new TreeViewController(data.editorWindow, data.state);
            var treeGui = new GroupTreeViewGUI(treeView);
            var dataSource = new TreeViewDataSourceForMixers(treeView, ignoreController);
            dataSource.onVisibleRowsChanged += treeGui.CalculateRowRects;
            treeView.deselectOnUnhandledMouseDown = false;
            treeView.Init(data.treeViewRect, dataSource, treeGui, null);

            // Set
            data.objectTreeForSelector.SetTreeView(treeView);
        }

        static readonly int kNoneItemID = 0;
        static string s_NoneText = "None";

        // GUI

        class GroupTreeViewGUI : TreeViewGUI
        {
            private readonly Texture2D k_AudioGroupIcon = EditorGUIUtility.FindTexture("AudioMixerGroup Icon");
            private readonly Texture2D k_AudioListenerIcon = EditorGUIUtility.FindTexture("AudioListener Icon");

            private const float k_SpaceBetween = 25f;
            private const float k_HeaderHeight = 20f;

            private List<Rect> m_RowRects = new List<Rect>();

            public GroupTreeViewGUI(TreeViewController treeView) : base(treeView)
            {
            }

            public override Rect GetRowRect(int row, float rowWidth)
            {
                if (m_TreeView.isSearching)
                {
                    return base.GetRowRect(row, rowWidth);
                }

                if (m_TreeView.data.rowCount != m_RowRects.Count)
                    CalculateRowRects();

                return m_RowRects[row];
            }

            public override void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused)
            {
                // Use normal line height when searching (its just a list)
                if (m_TreeView.isSearching)
                {
                    base.OnRowGUI(rowRect, item, row, selected, focused);
                    return;
                }

                DoItemGUI(rowRect, row, item, selected, focused, false);
                bool isRootItem = item.parent == m_TreeView.data.root;
                bool isNoneItem = item.id == kNoneItemID;
                if (isRootItem && !isNoneItem)
                {
                    AudioMixerController controller = ((MixerTreeViewItem)item).group.controller;
                    GUI.Label(new Rect(rowRect.x + 2f, rowRect.y - 18f, rowRect.width, 18f), GUIContent.Temp(controller.name), EditorStyles.boldLabel);
                }
            }

            protected override Texture GetIconForItem(TreeViewItem item)
            {
                if (item != null && item.icon != null)
                    return item.icon;

                if (item.id == kNoneItemID)
                    return k_AudioListenerIcon;
                return k_AudioGroupIcon;
            }

            protected override void SyncFakeItem()
            {
            }

            protected override void RenameEnded()
            {
            }

            // ------------------
            // Size section

            private bool IsController(TreeViewItem item)
            {
                return (item.parent == m_TreeView.data.root) && (item.id != kNoneItemID);
            }

            public void CalculateRowRects()
            {
                if (m_TreeView.isSearching)
                    return;
                const float startY = 2f;
                float rowWidth = GUIClip.visibleRect.width;
                var rows = m_TreeView.data.GetRows();
                m_RowRects = new List<Rect>(rows.Count);
                float curY = startY;
                for (int i = 0; i < rows.Count; ++i)
                {
                    bool isController = IsController(rows[i]);
                    float spacing = (isController ? k_SpaceBetween : 0f);
                    curY += spacing;
                    float height = k_LineHeight;
                    m_RowRects.Add(new Rect(0, curY, rowWidth, height));
                    curY += height;
                }
            }

            // Calc correct width if horizontal scrollbar is wanted return new Vector2(1, height)
            public override Vector2 GetTotalSize()
            {
                if (m_TreeView.isSearching)
                {
                    Vector2 size =  base.GetTotalSize();
                    size.x = 1;// GetMaxWidth (rows);
                    return size;
                }

                if (m_RowRects.Count == 0)
                    return new Vector2(1, 1);

                //float maxWidth = GUIClip.visibleRect.width; // GetMaxWidth (rows);
                return new Vector2(1, m_RowRects[m_RowRects.Count - 1].yMax);
            }

            public override int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView)
            {
                if (m_TreeView.isSearching)
                    return base.GetNumRowsOnPageUpDown(fromItem, pageUp, heightOfTreeView);
                return (int)Mathf.Floor(heightOfTreeView / k_LineHeight);   // good enough though not exact due to k_SpaceBetween
            }

            // Should return the row number of the first and last row thats fits in the pixel rect defined by top and height
            public override void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
            {
                if (m_TreeView.isSearching)
                {
                    base.GetFirstAndLastRowVisible(out firstRowVisible, out lastRowVisible);
                    return;
                }
                var rowCount = m_TreeView.data.rowCount;
                if (rowCount != m_RowRects.Count)
                    Debug.LogError("Mismatch in state: rows vs cached rects");

                int firstVisible = -1;
                int lastVisible = -1;

                float topPixel = m_TreeView.state.scrollPos.y;
                float heightInPixels = m_TreeView.GetTotalRect().height;
                for (int i = 0; i < m_RowRects.Count; ++i)
                {
                    bool visible = ((m_RowRects[i].y > topPixel && (m_RowRects[i].y < topPixel + heightInPixels))) ||
                        ((m_RowRects[i].yMax > topPixel && (m_RowRects[i].yMax < topPixel + heightInPixels)));

                    if (visible)
                    {
                        if (firstVisible == -1)
                            firstVisible = i;
                        lastVisible = i;
                    }
                }

                if (firstVisible != -1 && lastVisible != -1)
                {
                    firstRowVisible = firstVisible;
                    lastRowVisible = lastVisible;
                }
                else
                {
                    firstRowVisible = 0;
                    lastRowVisible = rowCount - 1;
                }
            }
        }

        class MixerTreeViewItem : TreeViewItem
        {
            public MixerTreeViewItem(int id, int depth, TreeViewItem parent, string displayName, AudioMixerGroupController groupController)
                : base(id, depth, parent, displayName)
            {
                group = groupController;
            }

            public AudioMixerGroupController group { get; set; }
        }

        // Datasource

        class TreeViewDataSourceForMixers : TreeViewDataSource
        {
            public AudioMixerController ignoreThisController { get; private set; }

            public TreeViewDataSourceForMixers(TreeViewController treeView, AudioMixerController ignoreController)
                : base(treeView)
            {
                showRootItem = false;
                rootIsCollapsable = false;
                ignoreThisController = ignoreController;
                alwaysAddFirstItemToSearchResult = true;
            }

            bool ShouldShowController(AudioMixerController controller, List<int> allowedInstanceIDs)
            {
                if (!controller)
                    return false;

                if (allowedInstanceIDs != null && allowedInstanceIDs.Count > 0)
                    return allowedInstanceIDs.Contains(controller.GetInstanceID());

                return true;
            }

            public override void FetchData()
            {
                int depth = -1;
                m_RootItem = new TreeViewItem(1010101010, depth, null, "InvisibleRoot");
                SetExpanded(m_RootItem.id, true);

                List<int> allowedInstanceIDs = ObjectSelector.get.allowedInstanceIDs;

                HierarchyProperty prop = new HierarchyProperty(HierarchyType.Assets);
                prop.SetSearchFilter(new SearchFilter() {classNames = new[] {"AudioMixerController"}});
                var controllers = new List<AudioMixerController>();
                while (prop.Next(null))
                {
                    var controller = prop.pptrValue as AudioMixerController;
                    if (ShouldShowController(controller, allowedInstanceIDs))
                        controllers.Add(controller);
                }

                var roots = new List<TreeViewItem>();

                // First add the 'None' item, then add all groups
                roots.Add(new TreeViewItem(kNoneItemID, 0, m_RootItem, s_NoneText));

                foreach (var controller in controllers)
                    roots.Add(BuildSubTree(controller));

                m_RootItem.children = roots;

                // If we only have one controller then just expand that entirely to the user doesnt have to. (If we have more than one root keep them collapsed and let the user expand)
                if (controllers.Count == 1)
                    m_TreeView.data.SetExpandedWithChildren(m_RootItem, true);

                m_NeedRefreshRows = true;
            }

            private TreeViewItem BuildSubTree(AudioMixerController controller)
            {
                AudioMixerGroupController masterGroup = controller.masterGroup;
                var masterItem = new MixerTreeViewItem(masterGroup.GetInstanceID(), 0, m_RootItem, masterGroup.name, masterGroup);
                AddChildrenRecursive(masterGroup, masterItem);
                return masterItem;
            }

            private void AddChildrenRecursive(AudioMixerGroupController group, TreeViewItem item)
            {
                item.children = new List<TreeViewItem>(group.children.Length);
                for (int i = 0; i < group.children.Length; ++i)
                {
                    item.children.Add(new MixerTreeViewItem(group.children[i].GetInstanceID(), item.depth + 1, item, group.children[i].name, group.children[i]));
                    AddChildrenRecursive(group.children[i], item.children[i]);
                }
            }

            public override bool CanBeMultiSelected(TreeViewItem item)
            {
                return false;
            }

            public override bool IsRenamingItemAllowed(TreeViewItem item)
            {
                return false;
            }
        }
    }
} // namespace
