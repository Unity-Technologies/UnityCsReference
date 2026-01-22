// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    class ViewSelectionTreeView : TreeView
    {
        public Action<Tab, bool> OnSelectedNonAnalyzedTab;

        readonly Tab[] m_Tabs;
        readonly ViewManager m_ViewManager;

        Dictionary<int, IssueCategory> m_ItemIdToCategory = new Dictionary<int, IssueCategory>();
        Dictionary<IssueCategory, TreeViewItem> m_CategoryToItem = new Dictionary<IssueCategory, TreeViewItem>();
        Dictionary<int, Tab> m_ItemIdToTab = new Dictionary<int, Tab>();

        const float k_NonAnalyzedIconWidth = 16f;
        const float k_AnalysisInProgressIconWidth = 16f;

        int m_CurrentlySelectedItemID;
        TreeViewItem m_RootItem;
        TreeViewItem m_FirstItem;
        GUIContent m_NonAnalyzedIcon;

        public ViewSelectionTreeView(TreeViewState treeViewState, Tab[] tabs,
                                     ViewManager viewManager)
            : base(treeViewState)
        {
            m_Tabs = tabs;
            m_ViewManager = viewManager;
            m_NonAnalyzedIcon = Utility.GetIcon(Utility.IconType.AdditionalAnalysis, "Not Analyzed");

            Reload();
        }

        protected override bool CanMultiSelect(TreeViewItem item) => false;

        protected override TreeViewItem BuildRoot()
        {
            int id = 0;
            m_RootItem = new TreeViewItem { id = id++, depth = -1, displayName = "Root" };

            m_CategoryToItem.Clear();
            m_ItemIdToCategory.Clear();
            m_ItemIdToTab.Clear();

            foreach (var tab in m_Tabs)
            {
                var tabItem = new TreeViewItem { id = id++, displayName = tab.name };

                if (!m_RootItem.hasChildren)
                    m_FirstItem = tabItem;

                m_RootItem.AddChild(tabItem);

                m_ItemIdToTab.Add(tabItem.id, tab);

                // Only add child items if there's more than one view. Otherwise this item represents the only view.
                if (tab.categories.Length > 1)
                {
                    var anyChildrenAnalyzed = false;
                    if (m_ViewManager.Report != null)
                    {
                        foreach (var childCategory in tab.categories)
                        {
                            if (m_ViewManager.Report.HasCategory(childCategory) || m_ViewManager.HasPendingCategory(childCategory))
                            {
                                anyChildrenAnalyzed = true;
                                break;
                            }
                        }
                    }

                    if (!anyChildrenAnalyzed)
                    {
                        // Increase the id anyway, so item ids stay same once we add more children later
                        id += tab.categories.Length;

                        // Map the sub categories to the tab (top-level) item
                        foreach (var cat in tab.categories)
                        {
                            m_CategoryToItem.Add(cat, tabItem);
                        }

                        continue;
                    }

                    foreach (var cat in tab.categories)
                    {
                        var view = m_ViewManager.GetView(cat);
                        if (view != null)
                        {
                            var categoryItem = new TreeViewItem { id = id++, displayName = view.Desc.DisplayName };
                            tabItem.AddChild(categoryItem);

                            m_ItemIdToCategory.Add(categoryItem.id, cat);
                            m_CategoryToItem.Add(cat, categoryItem);
                            m_ItemIdToTab.Add(categoryItem.id, tab);
                        }
                    }
                }
                else
                {
                    m_ItemIdToCategory.Add(tabItem.id, tab.categories[0]);
                    m_CategoryToItem.Add(tab.categories[0], tabItem);
                }
            }

            SetupDepthsFromParentsAndChildren(m_RootItem);

            return m_RootItem;
        }

        public override void OnGUI(Rect rect)
        {
            // Ensure we have an initial selection
            var selection = GetSelection();
            if (selection.Count == 0)
            {
                SelectItem(m_FirstItem);
            }

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyUp)
            {
                EditorApplication.delayCall += CheckNewSelection;
            }

            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);

            // Only show parent items as non-analyzed, not children (= views / categories)
            if (args.item.parent == m_RootItem)
            {
                if (NeedsAnalysis(args.item))
                {
                    Rect iconRect = new Rect(args.rowRect);
                    iconRect.x = iconRect.xMax - k_NonAnalyzedIconWidth;
                    iconRect.width = k_NonAnalyzedIconWidth;

                    GUI.Label(iconRect, m_NonAnalyzedIcon, SharedStyles.LabelWithDynamicSize);
                }
                else if (AnalysisInProgress(args.item))
                {
                    Rect iconRect = new Rect(args.rowRect);
                    iconRect.x = iconRect.xMax - k_AnalysisInProgressIconWidth;
                    iconRect.width = k_AnalysisInProgressIconWidth;

                    GUI.Label(iconRect, Utility.GetIcon(Utility.IconType.StatusWheel), SharedStyles.LabelWithDynamicSize);
                }
            }
        }

        bool NeedsAnalysis(TreeViewItem item)
        {
            if (m_ViewManager.Report == null)
                return true;

            if (m_ItemIdToTab.TryGetValue(item.id, out Tab foundTab))
            {
                foreach (var category in foundTab.categories)
                {
                    if (!m_ViewManager.Report.HasCategory(category) && !m_ViewManager.HasPendingCategory(category))
                        return true;
                }

                return false;
            }

            return true;
        }

        bool AnalysisInProgress(TreeViewItem item)
        {
            if (m_ViewManager.Report == null)
                return false;

            if (m_ItemIdToTab.TryGetValue(item.id, out Tab foundTab))
            {
                foreach (var category in foundTab.categories)
                {
                    if (m_ViewManager.HasPendingCategory(category))
                        return true;
                }

                return false;
            }

            return true;
        }

        void CheckNewSelection()
        {
            var selection = GetSelection();

            // We only use one selection at a time, so we chose the last one (even if user managed to do multiple selections)
            if (selection.Count > 0 && selection[0] != m_CurrentlySelectedItemID)
            {
                var item = FindItem(selection[0], rootItem);
                m_CurrentlySelectedItemID = selection[0];

                OnNewSelection(item, true, true);
            }
        }

        internal void SelectNonAnalyzedCategory(IssueCategory categoryToSelect)
        {
            var item = m_CategoryToItem[categoryToSelect];

            OnNewSelection(item, true, false);
        }

        void OnNewSelection(TreeViewItem item, bool changeView, bool expandItem)
        {
            if (expandItem)
            {
                var parent = item.parent;
                if (parent != null && parent != rootItem)
                    SetExpanded(item.parent.id, true);

                if (item.hasChildren && !IsExpanded(item.id))
                    SetExpanded(item.id, true);
            }

            if (changeView)
            {
                // Leaf items select a view using their category
                if (m_ItemIdToCategory.TryGetValue(item.id, out IssueCategory foundCategory))
                {
                    m_ViewManager.ChangeView(foundCategory);
                    if (!m_ViewManager.Report.HasCategory(foundCategory))
                    {
                        if (m_ItemIdToTab.TryGetValue(item.id, out var foundTab))
                            OnSelectedNonAnalyzedTab(foundTab, false);
                    }
                }
                // Parent items with children select a view using their first child's category
                else if (item.hasChildren)
                {
                    OnNewSelection(item.children[0], changeView, expandItem);
                    return;
                }
                else
                {
                    if (m_ItemIdToTab.TryGetValue(item.id, out var foundTab))
                        OnSelectedNonAnalyzedTab(foundTab, true);
                }
            }
        }

        void SelectItem(TreeViewItem item, bool changeView = false, bool expandItem = true)
        {
            m_CurrentlySelectedItemID = item.id;

            SetSelection(new List<int> { m_CurrentlySelectedItemID });

            OnNewSelection(item, changeView, expandItem);
        }

        public void SelectItemByCategory(IssueCategory cat)
        {
            if (m_CategoryToItem.TryGetValue(cat, out TreeViewItem item))
            {
                SelectItem(item);
            }
        }
    }
}
