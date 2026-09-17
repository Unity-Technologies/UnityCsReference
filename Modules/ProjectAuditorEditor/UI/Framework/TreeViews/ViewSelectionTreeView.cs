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
        // Invoked when the user selects a page node. The window decides what to show.
        public Action<Page> OnSelectedPage;

        readonly Page[] m_Pages;
        readonly ViewManager m_ViewManager;

        Dictionary<int, Page> m_ItemIdToPage = new Dictionary<int, Page>();
        Dictionary<IssueCategory, TreeViewItem> m_CategoryToItem = new Dictionary<IssueCategory, TreeViewItem>();
        Dictionary<Page, TreeViewItem> m_PageToItem = new Dictionary<Page, TreeViewItem>();

        // Item ids of the "tab" pages (direct children of a top-level page) that display the
        // analysis status icon.
        HashSet<int> m_AnalyzeUnitItemIds = new HashSet<int>();

        const float k_NonAnalyzedIconWidth = 16f;
        const float k_AnalysisInProgressIconWidth = 16f;

        int m_CurrentlySelectedItemID;
        TreeViewItem m_RootItem;
        TreeViewItem m_FirstItem;
        GUIContent m_NonAnalyzedIcon;

        public ViewSelectionTreeView(TreeViewState treeViewState, Page[] pages, ViewManager viewManager)
            : base(treeViewState)
        {
            m_Pages = pages;
            m_ViewManager = viewManager;
            m_NonAnalyzedIcon = Utility.GetIcon(Utility.IconType.AdditionalAnalysis, "Not Analyzed");

            Reload();
        }

        protected override void CommandEventHandling()
        {
            // We don't support the "SelectAll" shortcut in this view
            var evt = Event.current;
            if ((evt.type == EventType.ExecuteCommand || evt.type == EventType.ValidateCommand)
                && HasFocus() && evt.commandName == "SelectAll")
            {
                evt.Use();
                return;
            }
            base.CommandEventHandling();
        }

        protected override bool CanMultiSelect(TreeViewItem item) => false;

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);

            if (item == null || !CanChangeExpandedState(item))
                return;

            SetExpanded(id, !IsExpanded(id));
        }

        protected override TreeViewItem BuildRoot()
        {
            int id = 0;
            m_RootItem = new TreeViewItem { id = id++, depth = -1, displayName = "Root" };

            m_ItemIdToPage.Clear();
            m_CategoryToItem.Clear();
            m_PageToItem.Clear();
            m_AnalyzeUnitItemIds.Clear();
            m_FirstItem = null;

            foreach (var page in m_Pages)
                BuildPage(m_RootItem, page, ref id, 0);

            SetupDepthsFromParentsAndChildren(m_RootItem);

            return m_RootItem;
        }

        void BuildPage(TreeViewItem parentItem, Page page, ref int id, int depth)
        {
            var item = new TreeViewItem { id = id++, displayName = page.name };
            parentItem.AddChild(item);

            if (depth == 0 && m_FirstItem == null)
                m_FirstItem = item;

            m_ItemIdToPage[item.id] = page;
            m_PageToItem[page] = item;

            if (!page.isHome)
                m_CategoryToItem[page.category] = item;

            // "Tab" pages (direct children of a top-level page) show the analysis status icon.
            if (depth == 1)
                m_AnalyzeUnitItemIds.Add(item.id);

            if (page.children == null || page.children.Length == 0)
                return;

            // Top-level pages always reveal their children (the per-area tabs). Deeper pages only
            // reveal their category children once at least one has been analyzed (or is pending), to
            // avoid showing empty views.
            if (depth >= 1 && !AnyChildAnalyzed(page))
            {
                // Reserve the ids the children would use, so item ids stay stable once they appear.
                id += page.children.Length;

                // Map the hidden child categories to this page so selection still resolves to it.
                foreach (var child in page.children)
                {
                    if (!child.isHome)
                        m_CategoryToItem[child.category] = item;
                }

                return;
            }

            foreach (var child in page.children)
                BuildPage(item, child, ref id, depth + 1);
        }

        bool AnyChildAnalyzed(Page page)
        {
            if (m_ViewManager.Report == null)
                return false;

            foreach (var child in page.children)
            {
                if (!child.isHome && (m_ViewManager.Report.HasCategory(child.category) || m_ViewManager.HasPendingCategory(child.category)))
                    return true;
            }

            return false;
        }

        public override void OnGUI(Rect rect)
        {
            // Ensure we have an initial selection (the first top-level page).
            var selection = GetSelection();
            if (selection.Count == 0)
                SelectItem(m_FirstItem, false);

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyUp)
                EditorApplication.delayCall += CheckNewSelection;

            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);

            // Only the "tab" pages show the analysis status icon.
            if (!m_AnalyzeUnitItemIds.Contains(args.item.id))
                return;

            var page = m_ItemIdToPage[args.item.id];

            if (NeedsAnalysis(page))
            {
                Rect iconRect = new Rect(args.rowRect);
                iconRect.x = iconRect.xMax - k_NonAnalyzedIconWidth;
                iconRect.width = k_NonAnalyzedIconWidth;

                GUI.Label(iconRect, m_NonAnalyzedIcon, SharedStyles.LabelWithDynamicSize);
            }
            else if (AnalysisInProgress(page))
            {
                Rect iconRect = new Rect(args.rowRect);
                iconRect.x = iconRect.xMax - k_AnalysisInProgressIconWidth;
                iconRect.width = k_AnalysisInProgressIconWidth;

                GUI.Label(iconRect, Utility.GetIcon(Utility.IconType.StatusWheel), SharedStyles.LabelWithDynamicSize);
            }
        }

        bool NeedsAnalysis(Page page)
        {
            if (m_ViewManager.Report == null)
                return true;

            foreach (var category in page.AllCategories)
            {
                if (category.IsPopulatedByPlayerBuild())
                    continue;

                if (!m_ViewManager.Report.HasCategory(category) && !m_ViewManager.HasPendingCategory(category))
                    return true;
            }

            return false;
        }

        bool AnalysisInProgress(Page page)
        {
            if (m_ViewManager.Report == null)
                return false;

            foreach (var category in page.AllCategories)
            {
                if (m_ViewManager.HasPendingCategory(category))
                    return true;
            }

            return false;
        }

        void CheckNewSelection()
        {
            var selection = GetSelection();

            // We only use one selection at a time, so we chose the last one (even if user managed to do multiple selections)
            if (selection.Count > 0 && selection[0] != m_CurrentlySelectedItemID)
            {
                m_CurrentlySelectedItemID = selection[0];
                var item = FindItem(selection[0], rootItem);
                InvokeSelection(item);
            }
        }

        void InvokeSelection(TreeViewItem item)
        {
            ExpandFor(item);

            if (m_ItemIdToPage.TryGetValue(item.id, out var page))
                OnSelectedPage?.Invoke(page);
        }

        void ExpandFor(TreeViewItem item)
        {
            var parent = item.parent;
            if (parent != null && parent != rootItem)
                SetExpanded(parent.id, true);
        }

        // Keeps the tree in sync with the active view's category. Does nothing if the current
        // selection already shows that category, so a shared page (the same category under two parents)
        // isn't clobbered by its duplicate.
        public void SelectItemByCategory(IssueCategory category)
        {
            if (m_ItemIdToPage.TryGetValue(m_CurrentlySelectedItemID, out var currentPage)
                && !currentPage.isHome && currentPage.category == category)
                return;

            if (m_CategoryToItem.TryGetValue(category, out var item))
                SelectItem(item, false);
        }

        // Resolves the page mapped to a tree item id (e.g. the serialized selection after a reload).
        public bool TryGetPage(int itemId, out Page page) => m_ItemIdToPage.TryGetValue(itemId, out page);

        // Selects a specific page (by reference), optionally invoking the selection callback.
        public void SelectPage(Page page, bool invokeCallback = false)
        {
            if (m_PageToItem.TryGetValue(page, out var item))
                SelectItem(item, invokeCallback);
        }

        void SelectItem(TreeViewItem item, bool invokeCallback)
        {
            m_CurrentlySelectedItemID = item.id;
            SetSelection(new List<int> { item.id });
            ExpandFor(item);

            if (invokeCallback && m_ItemIdToPage.TryGetValue(item.id, out var page))
                OnSelectedPage?.Invoke(page);
        }
    }
}
