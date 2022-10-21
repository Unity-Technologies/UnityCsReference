// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    interface ISearchQueryItemHandler
    {
        void PopulateContextualMenu(SearchContext context, ISearchQuery query, SearchQueryListViewItem item, DropdownMenu menu);
        void RenameQuery(ISearchQuery query, SearchQueryListViewItem item, string newName);
    }

    class BaseSearchQueryListViewItemHandler : ISearchQueryItemHandler
    {
        protected static readonly string k_SaveMenuLabel = L10n.Tr("Save");
        protected static readonly string k_OpenInNewWindowMenuLabel = L10n.Tr("Open in new window");
        protected static readonly string k_RenameMenuLabel = L10n.Tr("Rename");
        protected static readonly string k_SetIconMenuLabel = L10n.Tr("Set Icon...");
        protected static readonly string k_SearchTemplateMenuLabel = L10n.Tr("Search Template");
        protected static readonly string k_DeleteMenuLabel = L10n.Tr("Delete");
        protected static readonly string k_EditInInspectorMenuLabel = L10n.Tr("Edit in Inspector");

        public virtual void PopulateContextualMenu(SearchContext context, ISearchQuery query, SearchQueryListViewItem item, DropdownMenu menu)
        {
            if (item.viewState.activeQuery == query && !context.empty)
            {
                menu.AppendAction(k_SaveMenuLabel, (_) => item.Emit(SearchEvent.SaveActiveSearchQuery));
                menu.AppendSeparator();
            }
            menu.AppendAction(k_OpenInNewWindowMenuLabel, (action) =>
            {
                SearchQuery.Open(query, SearchFlags.None);
            });
            menu.AppendSeparator();
            menu.AppendAction(k_RenameMenuLabel, (_) => item.Rename());
        }

        public virtual void RenameQuery(ISearchQuery query, SearchQueryListViewItem item, string newName)
        {
        }
    }

    class SearchUserQueryListViewItemHandler : BaseSearchQueryListViewItemHandler
    {
        public override void PopulateContextualMenu(SearchContext context, ISearchQuery query, SearchQueryListViewItem item, DropdownMenu menu)
        {
            base.PopulateContextualMenu(context, query, item, menu);

            menu.AppendAction(k_SetIconMenuLabel, (_) => SearchUtils.ShowIconPicker((newIcon, canceled) =>
            {
                if (canceled)
                    return;
                query.thumbnail = newIcon;
                SearchQuery.SaveSearchQuery((SearchQuery)query);
            }));
            menu.AppendAction(k_SearchTemplateMenuLabel, (_) => ((SearchQuery)query).isSearchTemplate = !query.isSearchTemplate, action =>
            {
                if (query.isSearchTemplate)
                    return DropdownMenuAction.Status.Checked;
                return DropdownMenuAction.Status.Normal;
            });
            menu.AppendAction(Utils.GetRevealInFinderLabel(), (_) => EditorUtility.RevealInFinder(query.filePath));
            menu.AppendSeparator();
            menu.AppendAction(k_DeleteMenuLabel, (_) =>
            {
                if (item.viewState.activeQuery == query)
                    item.viewState.activeQuery = null;
                SearchQuery.RemoveSearchQuery((SearchQuery)query);
            });
        }

        public override void RenameQuery(ISearchQuery query, SearchQueryListViewItem item, string newName)
        {
            var userQuery = (SearchQuery)query;
            userQuery.name = newName;
            SearchQuery.SaveSearchQuery(userQuery);
        }
    }

    class SearchProjectQueryListViewItemHandler : BaseSearchQueryListViewItemHandler
    {
        public override void PopulateContextualMenu(SearchContext context, ISearchQuery query, SearchQueryListViewItem item, DropdownMenu menu)
        {
            base.PopulateContextualMenu(context, query, item, menu);

            var queryAsset = (SearchQueryAsset)query;
            menu.AppendAction(k_SetIconMenuLabel,(_) => SearchUtils.ShowIconPicker((newIcon, canceled) =>
            {
                if (canceled)
                    return;
                queryAsset.icon = newIcon;
                EditorUtility.SetDirty(queryAsset);
                item.Emit(SearchEvent.SearchQueryChanged, query);
            }));
            menu.AppendAction(k_SearchTemplateMenuLabel, (_) => queryAsset.isSearchTemplate = !queryAsset.isSearchTemplate, action =>
            {
                if (query.isSearchTemplate)
                    return DropdownMenuAction.Status.Checked;
                return DropdownMenuAction.Status.Normal;
            });
            menu.AppendAction(k_EditInInspectorMenuLabel, (_) => Selection.activeObject = queryAsset);
            menu.AppendAction(Utils.GetRevealInFinderLabel(), (_) => EditorUtility.RevealInFinder(AssetDatabase.GetAssetPath(queryAsset)));
            menu.AppendSeparator();
            menu.AppendAction(k_DeleteMenuLabel, (_) =>
            {
                if (item.viewState.activeQuery == query)
                    item.viewState.activeQuery = null;
                SearchQueryAsset.RemoveQuery(queryAsset);
            });
        }

        public override void RenameQuery(ISearchQuery query, SearchQueryListViewItem item, string newName)
        {
            var queryAsset = (SearchQueryAsset)query;
            queryAsset.name = newName;
            item.Emit(SearchEvent.SearchQueryChanged, query);
        }
    }

    class SearchQueryListViewItem : SearchElement
    {
        RenamableLabel m_Label;
        VisualElement m_Icon;
        Label m_CountLabel;
        IManipulator m_ContextualMenuManipulator;
        ISearchQuery m_SearchQuery;
        ISearchQueryItemHandler m_ItemHandler;
        bool m_Selected = false;
        bool m_IsRenaming = false;

        internal ISearchQuery searchQuery => m_SearchQuery;

        internal static readonly string tabCountTextColorFormat = EditorGUIUtility.isProSkin ? "<color=#7B7B7B>{0}</color>" : "<color=#6A6A6A>{0}</color>";

        public static readonly string ussClassName = "search-query-listview-item";
        public static readonly string nameLabelClassName = ussClassName.WithUssElement("label");
        public static readonly string countLabelClassName = ussClassName.WithUssElement("count");

        public bool selected
        {
            get => m_Selected;
            set => m_Selected = value;
        }

        public SearchQueryListViewItem(ISearchView viewModel, params string[] classes)
            : base(nameof(SearchQueryListViewItem), viewModel, classes)
        {
            AddToClassList(ussClassName);
            m_Label = new RenamableLabel();
            m_Label.AddToClassList(nameLabelClassName);
            m_Label.renameFinished += HandleRenameFinished;
            m_Icon = new VisualElement();
            m_Icon.AddToClassList(SearchQueryPanelView.iconClassName);
            m_CountLabel = new Label();
            m_CountLabel.AddToClassList(countLabelClassName);
            m_CountLabel.pickingMode = PickingMode.Ignore;

            style.flexDirection = FlexDirection.Row;
            Add(m_Icon);
            Add(m_Label);
            Add(m_CountLabel);

            m_ContextualMenuManipulator = new ContextualMenuManipulator(HandleContextualMenu);
            this.AddManipulator(m_ContextualMenuManipulator);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        protected virtual void HandleContextualMenu(ContextualMenuPopulateEvent evt)
        {
            m_ItemHandler.PopulateContextualMenu(context, m_SearchQuery, this, evt.menu);
        }

        protected virtual void HandleRenameFinished(string newName)
        {
            m_ItemHandler.RenameQuery(m_SearchQuery, this, newName);
            var rootPanel = this.GetFirstAncestorOfType<SearchQueryPanelView>();
            if (rootPanel != null)
            {
                rootPanel.selectedQuery = searchQuery;
            }

            m_IsRenaming = false;
        }

        public void BindItem(ISearchQuery sq, ISearchQueryItemHandler itemHandler)
        {
            m_SearchQuery = sq;
            m_Label.text = sq.displayName;
            m_Icon.style.backgroundImage = new StyleBackground(SearchQuery.GetIcon(sq));
            UpdateSearchQueryItemCount(sq.itemCount);
            ClearHandlers();
            OnAll(SearchEvent.SearchQueryItemCountUpdated, HandleSearchQueryItemCountUpdated);
            m_ItemHandler = itemHandler;
        }

        public void UnbindItem()
        {
            ClearHandlers();
            m_SearchQuery = null;
        }

        public void DestroyItem()
        {
            UnbindItem();
            m_Label.renameFinished -= HandleRenameFinished;
            this.RemoveManipulator(m_ContextualMenuManipulator);
            m_ContextualMenuManipulator = null;
            UnregisterCallback<PointerDownEvent>(OnPointerDown);
        }

        void ClearHandlers()
        {
            Off(SearchEvent.SearchQueryItemCountUpdated, HandleSearchQueryItemCountUpdated);
            m_ItemHandler = null;
        }

        void HandleSearchQueryItemCountUpdated(ISearchEvent evt)
        {
            if (m_SearchQuery == null)
                return;

            var guid = evt.GetArgument<string>(0);
            if (!m_SearchQuery.guid.Equals(guid, StringComparison.Ordinal))
                return;
            var count = evt.GetArgument<int>(1);
            UpdateSearchQueryItemCount(count);
        }

        void UpdateSearchQueryItemCount(int itemCount)
        {
            var hidden = itemCount < 0;
            m_CountLabel.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;

            if (hidden)
                return;

            string formattedCount = Utils.FormatCount(Convert.ToUInt64(itemCount));
            m_CountLabel.text = string.Format(tabCountTextColorFormat, formattedCount);
        }

        public void Rename()
        {
            m_Label.StartRename();
            m_IsRenaming = true;
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.clickCount != 1 && evt.button != 0)
                return;

            if (!m_Selected)
            {
                m_Selected = true;
                return;
            }

            if (m_Selected && !m_IsRenaming)
            {
                Rename();
                evt.StopImmediatePropagation();
                evt.PreventDefault();
            }
        }
    }

    class SearchQueryListView : SearchElement
    {
        public int id { get; }
        string m_Label;
        Label m_TitleLabel;
        bool m_ShowHeader;
        List<ISearchQuery> m_Source;
        Foldout m_Header;
        VisualElement m_SaveButton;

        ListView m_ListView;
        Dictionary<int, SearchQueryListViewItem> m_VisibleItems = new Dictionary<int, SearchQueryListViewItem>();

        public Action<SearchQueryListView, ISearchQuery> itemSelected { get; set; }
        public Action saveSearchQuery { get; set; }
        public Action<ISearchQuery, SearchQueryListViewItem> bindItem { get; set; }
        public Action<SearchQueryListView, bool> expandStateChanged { get; set; }

        public const string headerIdName = "SearchQueryListViewHeader";
        public const string saveIconIdName = "SaveQueryButton";
        public static readonly string ussClassName = "search-query-listview";
        public static readonly string headerClassName = ussClassName.WithUssElement("header");
        public static readonly string saveIconClassName = ussClassName.WithUssElement("save-icon");
        public static readonly string headerElementClassName = ussClassName.WithUssElement("header-element");

        public SearchQueryListView(int id, string label, string tooltip, IEnumerable<ISearchQuery> source, string name, ISearchView viewModel, params string[] classes)
            : this(id, label, tooltip, source, true, name, viewModel, classes)
        {}

        public SearchQueryListView(int id, string label, string tooltip, IEnumerable<ISearchQuery> source, bool visibleHeader, string name, ISearchView viewModel, params string[] classes)
            : base(name, viewModel, classes)
        {
            AddToClassList(ussClassName);
            this.id = id;
            m_Label = label;
            m_ShowHeader = visibleHeader;
            m_Source = source.ToList();
            m_ListView = new ListView(m_Source, EditorGUIUtility.singleLineHeight + 4);
            m_ListView.makeItem = MakeItem;
            m_ListView.bindItem = BindItem;
            m_ListView.unbindItem = UnbindItem;
            m_ListView.destroyItem = DestroyItem;

            var scrollView = m_ListView.Q<ScrollView>();
            if (scrollView != null)
            {
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            }

            m_ListView.selectedIndicesChanged += HandleItemsSelected;
            RegisterCallback<KeyDownEvent>(HandleKeyDown);

            if (visibleHeader)
            {
                m_Header = CreateHeader(tooltip);
                Add(m_Header);
                m_Header.Add(m_ListView);
                UpdateSaveQueryIcon();
            }
            else
                Add(m_ListView);

            UpdateTitleLabel();
            m_ListView.Rebuild();
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);
            On(SearchEvent.SearchTextChanged, HandleSearchTextChanged);
            On(SearchEvent.SearchContextChanged, HandleSearchContextChanged);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Off(SearchEvent.SearchTextChanged, HandleSearchTextChanged);
            Off(SearchEvent.SearchContextChanged, HandleSearchContextChanged);
            base.OnDetachFromPanel(evt);
        }

        Foldout CreateHeader(string tooltip)
        {
            var foldout = new Foldout();
            foldout.RegisterValueChangedCallback(HandleFoldoutExpandStateChanged);

            var toggle = foldout.Q<Toggle>();

            // We need to remove the toggle from the hierarchy and insert a new element in its place.
            var toggleParent = toggle.parent;
            toggle.RemoveFromHierarchy();
            var visualElement = Create(headerIdName, headerClassName);
            visualElement.tooltip = tooltip;
            toggleParent.hierarchy.Insert(0, visualElement);
            visualElement.Add(toggle);

            // Set the classes on the toggle
            toggle.AddToClassList(headerElementClassName);
            m_TitleLabel = new Label();
            toggle.Children().First().Add(m_TitleLabel);

            // Create the save button
            m_SaveButton = Create(saveIconIdName, SearchQueryPanelView.iconClassName, SearchQueryPanelView.iconClickableClassName, saveIconClassName, headerElementClassName);
            m_SaveButton.style.backgroundImage = new StyleBackground(EditorGUIUtility.FindTexture("SaveAs"));
            m_SaveButton.RegisterCallback<ClickEvent>(HandleSaveQueryButtonClicked);
            visualElement.Add(m_SaveButton);

            return foldout;
        }

        void HandleFoldoutExpandStateChanged(ChangeEvent<bool> evt)
        {
            expandStateChanged?.Invoke(this, evt.newValue);
        }

        void HandleSaveQueryButtonClicked(ClickEvent evt)
        {
            saveSearchQuery?.Invoke();
        }

        void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.F2)
            {
                evt.StopImmediatePropagation();
                evt.PreventDefault();
                return;
            }

            if (m_ListView.selectedIndex < 0)
                return;

            if (!m_VisibleItems.TryGetValue(m_ListView.selectedIndex, out var selectedItem))
                return;
            selectedItem.Rename();
        }

        static void DestroyItem(VisualElement element)
        {
            var searchQueryListViewItem = element as SearchQueryListViewItem;
            searchQueryListViewItem?.DestroyItem();
        }

        void UnbindItem(VisualElement element, int index)
        {
            var searchQueryListViewItem = element as SearchQueryListViewItem;
            searchQueryListViewItem?.UnbindItem();
            m_VisibleItems.Remove(index);
        }

        void HandleItemsSelected(IEnumerable<int> selectedIndexes)
        {
            if (selectedIndexes == null || !selectedIndexes.Any())
                return;

            foreach (var item in m_VisibleItems)
            {
                if (selectedIndexes.Contains(item.Key))
                    continue;

                item.Value.selected = false;
            }

            itemSelected?.Invoke(this, m_Source[selectedIndexes.First()]);
        }

        public void ClearSelection()
        {
            m_ListView?.ClearSelection();
        }

        void BindItem(VisualElement element, int index)
        {
            var item = element as SearchQueryListViewItem;
            if (item == null)
                throw new ArgumentNullException(nameof(element), $"VisualElement is not a {nameof(SearchQueryListViewItem)}");
            if (index < 0 || index >= m_Source.Count)
                throw new IndexOutOfRangeException($"Index is outside the range of the {nameof(SearchQueryListView)}'s source.");

            bindItem?.Invoke(m_Source[index], item);
            m_VisibleItems[index] = item;
        }

        VisualElement MakeItem()
        {
            var item = new SearchQueryListViewItem(m_ViewModel);
            return item;
        }

        void UpdateTitleLabel()
        {
            if (!m_ShowHeader)
                return;

            m_TitleLabel.text = $"{m_Label} {(m_Source != null ? $"({m_Source.Count})" : "")}";
        }

        public void SortBy(IComparer<ISearchQuery> comparer)
        {
            if (m_Source == null)
                return;

            m_Source.Sort(comparer);

            m_ListView.RefreshItems();
        }

        public void SetItemSource(IEnumerable<ISearchQuery> items, bool rebuild)
        {
            if (m_Source == null)
            {
                m_Source = new List<ISearchQuery>();
                m_ListView.itemsSource = m_Source;
            }
            m_Source.Clear();
            m_Source.AddRange(items);
            UpdateTitleLabel();
            if (rebuild)
                m_ListView.Rebuild();
        }

        public void SetSelectedItem(ISearchQuery selectedItem)
        {
            if (m_Source == null)
                return;

            var index = m_Source.IndexOf(selectedItem);
            if (index < 0)
                return;

            m_ListView.SetSelectionWithoutNotify(new []{index});
        }

        public void AddItem(ISearchQuery item, IComparer<ISearchQuery> comparer)
        {
            var index = m_Source.BinarySearch(item, comparer);
            if (index >= 0)
                return; // Item already exists

            var insertIndex = ~index;
            m_Source.Insert(insertIndex, item);
            m_ListView.Rebuild();
        }

        public void RemoveItem(ISearchQuery item)
        {
            m_Source.Remove(item);
            m_ListView.Rebuild();
        }

        public void UpdateItem(ISearchQuery item)
        {
            var index = m_Source.IndexOf(item);
            if (index < 0)
                return;
            m_ListView.RefreshItem(index);
        }

        public void SetExpandedState(bool expanded, bool notify)
        {
            if (!m_ShowHeader || m_Header == null)
                return;

            if (notify)
                m_Header.value = expanded;
            else
                m_Header.SetValueWithoutNotify(expanded);
        }

        public bool GetExpandedState()
        {
            if (!m_ShowHeader || m_Header == null)
                return true;
            return m_Header.value;
        }

        void HandleSearchTextChanged(ISearchEvent evt)
        {
            UpdateSaveQueryIcon();
        }

        void HandleSearchContextChanged(ISearchEvent evt)
        {
            UpdateSaveQueryIcon();
        }

        void UpdateSaveQueryIcon()
        {
            if (!m_ShowHeader || m_SaveButton == null)
                return;

            m_SaveButton.SetEnabled(!context.empty);
        }

        public bool IsItemInList(ISearchQuery query)
        {
            if (m_Source == null)
                return false;
            return m_Source.IndexOf(query) >= 0;
        }

        public SearchQueryListViewItem GetSearchElementForSearchQuery(ISearchQuery query)
        {
            if (m_Source == null || m_ListView == null || m_VisibleItems == null)
                return null;
            var index = m_Source.IndexOf(query);
            if (index < 0 || index >= m_Source.Count)
                return null;
            if (!m_VisibleItems.TryGetValue(index, out var item))
                return null;
            return item;
        }
    }
}
