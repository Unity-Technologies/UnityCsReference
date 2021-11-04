// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    class SearchQueryTreeView : TreeView
    {
        public const string userQuery = "User";
        public const string projectQuery = "Project";

        public static readonly string userTooltip = L10n.Tr("Your saved searches available for all Unity projects on this machine.");
        public static readonly string projectTooltip = L10n.Tr("Shared searches available for all contributors on this project.");

        static class Styles
        {
            public static readonly GUIStyle toolbarButton = new GUIStyle("IN Title")
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                imagePosition = ImagePosition.ImageOnly,
                alignment = TextAnchor.MiddleCenter
            };

            public static readonly GUIStyle categoryLabel = new GUIStyle("IN Title")
            {
                richText = true,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 0, 3, 0),
            };

            public static readonly GUIStyle itemLabel = Utils.FromUSS(new GUIStyle()
            {
                wordWrap = false,
                stretchWidth = false,
                stretchHeight = false,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Overflow,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0)
            }, "quick-search-tree-view-item");
        }

        private SearchQueryCategoryTreeViewItem m_UserQueries;
        private SearchQueryCategoryTreeViewItem m_ProjectQueries;
        private SearchQuerySortOrder m_SortOrder;

        public QuickSearch searchView { get; private set; }
        public bool isRenaming { get; private set; }

        public SearchQueryTreeView(TreeViewState state, QuickSearch searchView)
            : base(state)
        {
            showBorder = false;
            showAlternatingRowBackgrounds = false;
            rowHeight = EditorGUIUtility.singleLineHeight + 4;
            this.searchView = searchView;
            Reload();
            SortBy(SearchSettings.savedSearchesSortOrder);
        }

        public ISearchQuery GetCurrentQuery()
        {
            var selection = GetSelection();
            if (selection.Count == 0)
                return null;

            var item = FindItem(selection[0], rootItem);
            if (item == null)
                return null;

            return ((SearchQueryTreeViewItem)item).query;
        }

        public void SetCurrentQuery(ISearchQuery query)
        {
            var currentItem = FindItemFromQuery(query, rootItem);
            if (currentItem == null)
                return;
            SetSelection(new[] { currentItem.id });
        }

        public override void OnGUI(Rect rect)
        {
            var evt = Event.current;

            // Ignore arrow keys for this tree view, these needs to be handled by the search result view (later)
            if (!isRenaming && Utils.IsNavigationKey(evt))
                return;

            base.OnGUI(rect);

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                // User has clicked in an area where there are no items: unselect.
                ClearSelection();
            }
        }

        public void SortBy(SearchQuerySortOrder order)
        {
            m_SortOrder = order;
            SortBy(m_UserQueries, order);
            SortBy(m_ProjectQueries, order);
            BuildRows(rootItem);
        }

        public void ClearSelection()
        {
            SetSelection(new int[0]);
        }

        private static void SortBy(TreeViewItem parent, SearchQuerySortOrder order)
        {
            switch (order)
            {
                case SearchQuerySortOrder.AToZ:
                    parent.children.Sort(SortAlpha);
                    break;
                case SearchQuerySortOrder.ZToA:
                    parent.children.Sort(SortAlphaDesc);
                    break;
                case SearchQuerySortOrder.CreationTime:
                    parent.children.Sort(SortCreationTime);
                    break;
            }
        }

        private static int SortAlpha(TreeViewItem a, TreeViewItem b)
        {
            return ((SearchQueryTreeViewItem)a).query.displayName.CompareTo(((SearchQueryTreeViewItem)b).query.displayName);
        }

        private static int SortAlphaDesc(TreeViewItem a, TreeViewItem b)
        {
            return -SortAlpha(a, b);
        }

        private static int SortCreationTime(TreeViewItem a, TreeViewItem b)
        {
            // Most recent is on top.
            return -((SearchQueryTreeViewItem)a).query.creationTime.CompareTo(((SearchQueryTreeViewItem)b).query.creationTime);
        }

        private static TreeViewItem FindItemFromQuery(ISearchQuery q, TreeViewItem i)
        {
            if (i is SearchQueryTreeViewItem sqtvi)
            {
                if (sqtvi.query == q)
                    return i;
            }

            foreach (var treeViewItem in i.children)
            {
                var found = FindItemFromQuery(q, treeViewItem);
                if (found != null)
                    return found;
            }

            return null;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (item is SearchQueryCategoryTreeViewItem)
                return false;

            return base.DoesItemMatchSearch(item, search);
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override TreeViewItem BuildRoot()
        {
            var id = 1;
            var root = new TreeViewItem { id = id++, depth = -1, displayName = "Root", children = new List<TreeViewItem>() };
            m_UserQueries = new SearchQueryCategoryTreeViewItem(this, () => searchView.SaveUserSearchQuery(), new GUIContent(userQuery, null, userTooltip));
            foreach (var searchQuery in SearchQuery.userQueries)
            {
                var userQueryNode = new SearchQueryUserTreeViewItem(this, searchQuery);
                m_UserQueries.AddChild(userQueryNode);
            }
            root.AddChild(m_UserQueries);

            m_ProjectQueries = new SearchQueryCategoryTreeViewItem(this, () => searchView.SaveProjectSearchQuery(), new GUIContent(projectQuery, null, projectTooltip));
            foreach (var searchQueryAsset in SearchQueryAsset.savedQueries)
            {
                var projectQueryNode = new SearchQueryAssetTreeViewItem(this, searchQueryAsset);
                m_ProjectQueries.AddChild(projectQueryNode);
            }
            root.AddChild(m_ProjectQueries);

            return root;
        }

        protected override void ExpandedStateChanged()
        {
            SearchSettings.expandedQueries = state.expandedIDs.ToArray();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var evt = Event.current;
            var rowRect = args.rowRect;

            isRenaming = isRenaming || args.isRenaming;
            if (args.item is SearchQueryCategoryTreeViewItem ctvi)
            {
                if (evt.type == EventType.Repaint)
                    Styles.categoryLabel.Draw(rowRect, rowRect.Contains(evt.mousePosition), false, false, false);

                EditorGUI.BeginDisabledGroup(!searchView.CanSaveQuery());
                var addBtn = new Rect(rowRect.xMax - 21f, rowRect.y, 22f, 22f);
                if (GUI.Button(addBtn, ctvi.addBtnContent, Styles.toolbarButton))
                {
                    switch (ctvi.content.text)
                    {
                        case userQuery:
                            searchView.SaveUserSearchQuery();
                            break;
                        case projectQuery:
                            searchView.SaveProjectSearchQuery();
                            break;
                    }
                }
                EditorGUI.EndDisabledGroup();

                var labelBtn = rowRect;
                labelBtn.width -= 20f;
                if (GUI.Button(labelBtn, Utils.GUIContentTemp($"{ctvi.content.text} ({ctvi.children.Count})", ctvi.content.tooltip), Styles.categoryLabel))
                    SetExpanded(args.item.id, !IsExpanded(args.item.id));
            }
            else if (args.item is SearchQueryTreeViewItem tvi)
            {
                if (!tvi.IsValid())
                {
                    SearchQueryAsset.ResetSearchQueryItems();
                    Reload();
                    return;
                }

                if (!args.isRenaming && evt.type == EventType.Repaint)
                {
                    var hovered = rowRect.Contains(evt.mousePosition);

                    DrawQueryLabelAndIcon(rowRect, args, tvi, hovered, false);
                }
            }
        }

        private void DrawItemCount(int itemCount, Rect rowRect, bool hovered, bool selected)
        {
            GUIContent itemCountContent = new GUIContent();

            if (itemCount != -1)
            {
                string formattedCount = Utils.FormatCount(Convert.ToUInt64(itemCount));
                itemCountContent = Utils.GUIContentTemp(string.Format(Search.Styles.tabCountTextColorFormat, formattedCount));
            }

            var itemCountRect = new Rect(Mathf.Floor(rowRect.xMax - 27f), rowRect.y, 28f, 22f);
            var oldAlignment = Styles.itemLabel.alignment;
            Styles.itemLabel.alignment = TextAnchor.MiddleCenter;
            Styles.itemLabel.Draw(itemCountRect, itemCountContent, hovered, selected, false, false);
            Styles.itemLabel.alignment = oldAlignment;
        }

        private void DrawQueryLabelAndIcon(Rect rowRect, RowGUIArgs args, SearchQueryTreeViewItem tvi, bool hovered, bool usePropertyDatabase)
        {
            var itemContent = Utils.GUIContentTemp(Utils.Simplify(tvi.query.displayName), SearchQuery.GetIcon(tvi.query));
            var oldLeftPadding = Styles.itemLabel.padding.left;
            Styles.itemLabel.padding.left += Mathf.RoundToInt(GetContentIndent(args.item) + extraSpaceBeforeIconAndLabel);

            if (usePropertyDatabase)
            {
                var itemRect = new Rect(rowRect.x, rowRect.y, Mathf.Floor(rowRect.xMax - 27f), 22f);
                var oldClipping = Styles.itemLabel.clipping;
                Styles.itemLabel.clipping = TextClipping.Clip;
                Styles.itemLabel.Draw(itemRect, itemContent, hovered, args.selected, false, false);
                Styles.itemLabel.clipping = oldClipping;
            }
            else
                Styles.itemLabel.Draw(rowRect, itemContent, hovered, args.selected, false, false);

            Styles.itemLabel.padding.left = oldLeftPadding;
        }

        protected override void SingleClickedItem(int id)
        {
            if (FindItem(id, rootItem) is SearchQueryTreeViewItem stvi)
                stvi.Open();
        }

        protected override void ContextClickedItem(int id)
        {
            if (FindItem(id, rootItem) is SearchQueryTreeViewItem stvi)
                OpenContextualMenu(() => stvi.OpenContextualMenu());
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return ((SearchQueryTreeViewItem)item).CanRename();
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            isRenaming = false;
            if (!args.acceptedRename)
                return;

            if (FindItem(args.itemID, rootItem) is SearchQueryTreeViewItem item && item.AcceptRename(args.originalName, args.newName))
            {
                SortBy(item.parent, m_SortOrder);
                BuildRows(rootItem);
            }
        }

        private static void OpenContextualMenu(Action handler)
        {
            handler();
            Event.current.Use();
        }

        public void RemoveItem(TreeViewItem item)
        {
            item.parent.children.Remove(item);
            BuildRows(rootItem);
        }

        public void Add(ISearchQuery query, bool select = true)
        {
            SearchQueryTreeViewItem item = null;
            if (query is SearchQueryAsset sqa)
            {
                item = new SearchQueryAssetTreeViewItem(this, sqa);
                m_ProjectQueries.AddChild(item);
                SetExpanded(m_ProjectQueries.id, true);
                SortBy(m_ProjectQueries, m_SortOrder);

                var evt = searchView.CreateEvent(SearchAnalytics.GenericEventType.QuickSearchCreateSearchQuery, query.searchText, query.filePath, "project");
                SearchAnalytics.SendEvent(evt);
            }
            else if (query is SearchQuery sq && SearchQuery.IsUserQuery(sq))
            {
                item = new SearchQueryUserTreeViewItem(this, sq);
                m_UserQueries.AddChild(item);
                SetExpanded(m_UserQueries.id, true);
                SortBy(m_UserQueries, m_SortOrder);

                var evt = searchView.CreateEvent(SearchAnalytics.GenericEventType.QuickSearchCreateSearchQuery, query.searchText, "", "user");
                SearchAnalytics.SendEvent(evt);
            }

            if (item != null)
            {
                BuildRows(rootItem);
                if (select)
                    SetSelection(new int[] { item.id });
            }
        }
    }
}
