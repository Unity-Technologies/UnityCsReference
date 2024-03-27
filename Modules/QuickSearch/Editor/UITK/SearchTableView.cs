// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Internal;

namespace UnityEditor.Search
{
    class SearchTableView : SearchBaseCollectionView<MultiColumnListView>, ITableView
    {
        public static readonly string ussClassName = "search-table-view";
        public static readonly string resultsListClassName = ussClassName.WithUssElement("results-list");
        public static readonly string addColumnButtonClassName = ussClassName.WithUssElement("add-column-button");
        public static readonly string resetColumnsButtonClassName = ussClassName.WithUssElement("reset-columns-button");

        public static readonly string addMoreColumnsTooltip = L10n.Tr("Add column...");
        public static readonly string resetSearchColumnsTooltip = L10n.Tr("Reset search result columns.");

        private Action m_DeferredSortColumnOff;

        public SearchTable tableConfig
        {
            get => viewState.tableConfig;
            private set
            {
                viewState.tableConfig = value;
                Refresh(RefreshFlags.DisplayModeChanged);
            }
        }

        bool ITableView.readOnly => false;
        public override bool showNoResultMessage => m_ViewModel.displayMode != DisplayMode.Table;

        Columns viewColumns => m_ListView.columns;

        public SearchTableView(ISearchView viewModel)
            : base("SearchTableView", viewModel, ussClassName)
        {
            m_ListView = new MultiColumnListView(BuildColumns(tableConfig))
            {
                fixedItemHeight = 22f,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                selectionType = m_ViewModel.multiselect ? SelectionType.Multiple : SelectionType.Single,
                itemsSource = (IList)m_ViewModel.results,
                sortingEnabled = true
            };
            m_ListView.AddToClassList(resultsListClassName);
            Add(m_ListView);

            if (tableConfig == null)
                SetupColumns();

            SetupColumnSorting();
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            m_ListView.columnSortingChanged += OnSortColumn;
            m_ListView.columns.columnReordered += OnColumnReordered;

            var header = this.Q<MultiColumnCollectionHeader>();
            header.contextMenuPopulateEvent += OnSearchColumnContextualMenu;

            On(SearchEvent.SearchContextChanged, OnContextChanged);
            On(SearchEvent.RequestResultViewButtons, OnAddResultViewButtons);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Off(SearchEvent.SearchContextChanged, OnContextChanged);
            Off(SearchEvent.RequestResultViewButtons, OnAddResultViewButtons);

            var header = this.Q<MultiColumnCollectionHeader>();
            header.contextMenuPopulateEvent -= OnSearchColumnContextualMenu;

            m_ListView.columnSortingChanged -= OnSortColumn;
            m_ListView.columns.columnReordered -= OnColumnReordered;

            base.OnDetachFromPanel(evt);
        }

        private bool IsTableViewRow(VisualElement ve)
        {
            return ve.ClassListContains("unity-multi-column-view__row-container");
        }

        protected override void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.clickCount != 1 && evt.button != 0)
                return;

            if (evt.target is not VisualElement ve)
                return;

            if (!IsTableViewRow(ve) && ve.GetFirstAncestorWhere((ve) => { return IsTableViewRow(ve); }) == null)
                m_ListView.ClearSelection();
        }

        protected override void OnGroupChanged(string prevGroupId, string newGroupId)
        {
            OnSortColumn();
            base.OnGroupChanged(prevGroupId, newGroupId);
        }

        public IEnumerable<SearchItem> GetElements()
        {
            return m_ViewModel.results ?? Enumerable.Empty<SearchItem>();
        }

        float ITableView.GetRowHeight() => m_ListView.fixedItemHeight;
        IEnumerable<SearchItem> ITableView.GetRows() => m_ViewModel.results;
        SearchTable ITableView.GetSearchTable() => tableConfig;
        void ITableView.SetSelection(IEnumerable<SearchItem> items) => m_ViewModel.SetSelection(items.Select(e => m_ViewModel.results.IndexOf(e)).Where(i => i != -1).ToArray());
        void ITableView.OnItemExecuted(SearchItem item) => m_ViewModel.ExecuteSelection();
        void ITableView.SetDirty() => Refresh();
        int ITableView.GetColumnIndex(string name) => GetColumnIndex(name);

        bool ITableView.OpenContextualMenu(Event evt, SearchItem item)
        {
            var selection = m_ViewModel.selection;
            if (selection.Count <= 0 && item == null)
                return false;

            var contextRect = new Rect(evt.mousePosition, new Vector2(1, 1));
            m_ViewModel.ShowItemContextualMenu(item, contextRect);
            return true;
        }

        SearchColumn ITableView.FindColumnBySelector(string selector)
        {
            var columnIndex = GetColumnIndex(selector);
            if (columnIndex == -1)
                return null;
            return tableConfig.columns[columnIndex];
        }

        IEnumerable<object> ITableView.GetValues(int columnIdx)
        {
            if (m_ViewModel?.state?.tableConfig?.columns == null)
                yield break;

            var column = tableConfig.columns[columnIdx];
            foreach (var ltd in m_ViewModel.results)
                yield return column.ResolveValue(ltd, context);
        }

        void ITableView.UpdateColumnSettings(int columnIndex, IMGUI.Controls.MultiColumnHeaderState.Column columnSettings) => throw new NotSupportedException("Search table view IMGUI is not supported anymore");
        bool ITableView.AddColumnHeaderContextMenuItems(GenericMenu menu) => throw new NotSupportedException("Search table view IMGUI is not supported anymore");
        void ITableView.AddColumnHeaderContextMenuItems(GenericMenu menu, SearchColumn sourceColumn) => throw new NotSupportedException("Search table view IMGUI is not supported anymore");

        public override void Refresh(RefreshFlags flags)
        {
            if (flags.HasAny(RefreshFlags.ItemsChanged | RefreshFlags.GroupChanged | RefreshFlags.QueryCompleted))
            {
                Refresh();
            }
            else if (flags.HasAny(RefreshFlags.DisplayModeChanged))
            {
                if (m_ListView != null)
                    BuildColumns(viewColumns, tableConfig, clear: true);

                ExportTableConfig();
            }
        }

        protected override float GetItemHeight() => m_ListView.fixedItemHeight;

        private void SetupColumnSorting()
        {
            bool applySorting = false;
            foreach (var tc in m_ListView.columns)
            {
                if (tc is not SearchTableViewColumn stvc)
                    continue;
                if (stvc.searchColumn.options.HasAny(SearchColumnFlags.Sorted))
                {
                    applySorting = true;
                    var sortedBy = SortDirection.Ascending;
                    if (stvc.searchColumn.options.HasAny(SearchColumnFlags.SortedDescending))
                        sortedBy = SortDirection.Descending;
                    m_ListView.sortColumnDescriptions.Add(new SortColumnDescription(tc.index, sortedBy));
                }
            }

            if (applySorting)
                OnSortColumn();
        }

        private void OnSortColumn()
        {
            m_DeferredSortColumnOff?.Invoke();
            m_DeferredSortColumnOff = Utils.CallDelayed(SortColumns);
        }

        private void SortColumns()
        {
            if (tableConfig?.columns == null)
                return;

            var sorter = new SearchTableViewColumnSorter();

            foreach (var c in tableConfig.columns)
                c.options &= ~SearchColumnFlags.Sorted;

            foreach (var sortC in m_ListView.sortedColumns)
            {
                var sc = ((SearchTableViewColumn)sortC.column).searchColumn;
                sc.options |= SearchColumnFlags.Sorted;
                if (sortC.direction == SortDirection.Ascending)
                    sc.options &= ~SearchColumnFlags.SortedDescending;
                else
                    sc.options |= SearchColumnFlags.SortedDescending;
                sorter.Add(sc, sortC.direction);
            }

            m_ViewModel.results.SortBy(sorter);
            Refresh();
        }

        private void OnAddResultViewButtons(ISearchEvent evt)
        {
            var container = evt.GetArgument<VisualElement>(0);

            var addColumnButton = new Button { tooltip = addMoreColumnsTooltip };
            addColumnButton.RegisterCallback<ClickEvent>(OnAddColumn);
            addColumnButton.AddToClassList(SearchGroupBar.groupBarButtonClassName);
            addColumnButton.AddToClassList(addColumnButtonClassName);
            container.Add(addColumnButton);

            var resetColumnsButton = new Button(ResetColumnLayout) { tooltip = resetSearchColumnsTooltip };
            resetColumnsButton.AddToClassList(SearchGroupBar.groupBarButtonClassName);
            resetColumnsButton.AddToClassList(resetColumnsButtonClassName);
            container.Add(resetColumnsButton);
        }

        private void OnColumnReordered(Column arg1, int arg2, int arg3)
        {
            SwapColumns(arg2, arg3);
        }

        private void OnContextChanged(ISearchEvent evt)
        {
            if (evt.sourceViewState != m_ViewModel.state)
                return;

            m_ListView.itemsSource = (IList)m_ViewModel.results;
            if (tableConfig == null)
                SetupColumns();
            else
                BuildColumns(viewColumns, tableConfig, clear: true);
        }

        private void OnSearchColumnContextualMenu(ContextualMenuPopulateEvent evt, Column columnUnderMouse)
        {
            evt.menu.ClearItems();

            var visibleColumnCount = m_ListView.columns.visibleList.Count();
            if (viewColumns.Count > 1)
            {
                for (int i = 0; i < viewColumns.Count; ++i)
                {
                    var column = viewColumns[i];
                    var menuContent = "Show Columns/" + GetDisplayLabel(column);
                    if (visibleColumnCount == 1 && column.visible)
                        evt.menu.AppendAction(menuContent, (a) => { }, DropdownMenuAction.Status.Disabled);
                    else
                        evt.menu.AppendAction(menuContent, (a) => { ToggleColumnVisibility(column); },
                            column.visible ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                }
            }

            if (columnUnderMouse is SearchTableViewColumn pc)
                AddColumnHeaderContextMenuItems(evt.menu, pc.searchColumn);

            var mp = evt.mousePosition;
            var activeColumnIndex = columnUnderMouse != null ? FindColumnIndex(columnUnderMouse) : -1;
            evt.menu.AppendSeparator();
            evt.menu.AppendAction(EditorGUIUtility.TrTextContent("Add Column...").text, (a) => AddColumn(mp, activeColumnIndex));

            if (columnUnderMouse != null)
            {
                var colName = (columnUnderMouse as SearchTableViewColumn)?.title ?? columnUnderMouse.title;
                evt.menu.AppendAction(EditorGUIUtility.TrTextContent($"Edit {colName}...").text, (a) => EditColumn(activeColumnIndex));
                evt.menu.AppendAction(EditorGUIUtility.TrTextContent($"Remove {colName}").text, (a) => RemoveColumn(activeColumnIndex));
            }

            evt.menu.AppendSeparator();
            evt.menu.AppendAction(EditorGUIUtility.TrTextContent("Reset Columns").text, (a) => ResetColumnLayout());
        }

        private static string GetDisplayLabel(Column col)
        {
            if (!string.IsNullOrEmpty(col.title))
                return col.title;

            if (!string.IsNullOrEmpty(col.name))
                return col.name;

            if (col.icon.texture != null && !string.IsNullOrEmpty(col.icon.texture.name))
                return col.icon.texture.name;

            return "Unnamed";
        }

        private void ToggleColumnVisibility(Column column)
        {
            column.visible = !column.visible;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchTableToggleColumnVisibility);
        }

        private void EditColumn(int columnIndex)
        {
            if (tableConfig?.columns == null) return;
            if (TryGetViewModelColumn(tableConfig.columns[columnIndex], out var viewColumn))
                ColumnEditor.ShowWindow(viewColumn, (_column) => UpdateColumnSettings(columnIndex, _column));
        }

        private bool TryGetViewModelColumn(in SearchColumn sc, out SearchTableViewColumn viewColumn)
        {
            foreach (var c in viewColumns)
            {
                if (c is not SearchTableViewColumn stvc)
                    continue;
                if (stvc.searchColumn == sc)
                {
                    viewColumn = stvc;
                    return true;
                }
            }

            viewColumn = null;
            return false;
        }

        internal void ResetColumnLayout()
        {
            tableConfig = LoadDefaultTableConfig(reset: true);
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchTableReset, context.searchQuery);
        }

        private void OnAddColumn(ClickEvent evt)
        {
            var searchColumns = SearchColumn.Enumerate(context, GetElements());
            var windowMousePosition = Utils.Unclip(new Rect(evt.position, Vector2.zero)).position;
            SearchUtils.ShowColumnSelector(AddColumns, searchColumns, windowMousePosition, -1);
        }

        void ExportTableConfig(string id = null)
        {
            if (viewState.tableConfig == null)
                return;
            SessionState.SetString(GetDefaultGroupId(id), viewState.tableConfig.Export());
        }

        private SearchTable LoadDefaultTableConfig(bool reset, string id = null, SearchTable defaultConfig = null)
        {
            if (!reset)
            {
                var sessionSearchTableData = SessionState.GetString(GetDefaultGroupId(id), null);
                if (!string.IsNullOrEmpty(sessionSearchTableData))
                    return SearchTable.Import(sessionSearchTableData);
            }

            if (m_ViewModel != null)
            {
                var providers = m_ViewModel.context.GetProviders();
                var provider = providers.Count == 1 ? providers.FirstOrDefault() : SearchService.GetProvider(m_ViewModel.currentGroup);
                if (provider?.tableConfig != null)
                    return provider.tableConfig(context);
            }

            return defaultConfig ?? SearchTable.CreateDefault();
        }

        private string GetDefaultGroupId(string id = null)
        {
            var key = "CurrentSearchTableConfig_V2";
            if (id == null)
            {
                var providers = m_ViewModel.context.GetProviders();
                if (providers.Count == 1)
                    key += "_" + providers[0].id;
                else if (!string.IsNullOrEmpty(m_ViewModel.currentGroup))
                    key += "_" + m_ViewModel.currentGroup;
            }
            else if (!string.IsNullOrEmpty(id))
                key += "_" + id;
            return key;
        }

        public void AddColumns(IEnumerable<SearchColumn> newColumns, int insertColumnAt)
        {
            if (tableConfig == null)
                return;

            var oldColumns = tableConfig.columns ?? Array.Empty<SearchColumn>();
            var uniqueColumns = newColumns.Where(newColumn => oldColumns.All(c => c.selector != newColumn.selector)).ToList();
            if (uniqueColumns.Count == 0)
                return;

            var searchColumns = new List<SearchColumn>(oldColumns);
            if (insertColumnAt == -1)
                insertColumnAt = searchColumns.Count;
            var columnCountBefore = searchColumns.Count;
            searchColumns.InsertRange(insertColumnAt, uniqueColumns);

            var columnAdded = searchColumns.Count - columnCountBefore;
            if (columnAdded > 0)
            {
                var firstColumn = uniqueColumns.First();
                var e = SearchAnalytics.GenericEvent.Create(null, SearchAnalytics.GenericEventType.QuickSearchTableAddColumn, firstColumn.name);
                e.intPayload1 = columnAdded;
                e.message = firstColumn.provider;
                e.description = firstColumn.selector;
                SearchAnalytics.SendEvent(e);

                for (int ca = 0; ca < columnAdded; ++ca)
                {
                    var newViewColumn = new SearchTableViewColumn(searchColumns[insertColumnAt + ca], m_ViewModel, this);
                    viewColumns.Insert(insertColumnAt + ca, newViewColumn);
                }

                tableConfig.columns = searchColumns.ToArray();
                ExportTableConfig();
                Refresh();
            }
        }

        private Columns BuildColumns(SearchTable tableConfig)
        {
            var columns = new Columns();

            if (tableConfig?.columns == null)
                return columns;

            foreach (var sc in tableConfig.columns)
                columns.Add(new SearchTableViewColumn(sc, m_ViewModel, this));

            return columns;
        }

        private void BuildColumns(Columns columns, SearchTable tableConfig, bool clear = false)
        {
            if (clear)
                columns.Clear();

            if (tableConfig?.columns == null)
                return;

            foreach (var sc in tableConfig.columns)
            {
                if (!clear && columns.Any(x => string.Equals(x.name, sc.path, StringComparison.Ordinal)))
                    continue;

                // FIXME: Each call to Add here does a Rebuild of the table view
                columns.Add(new SearchTableViewColumn(sc, m_ViewModel, this));
            }
        }

        public void AddColumn(Vector2 mousePosition, int activeColumnIndex)
        {
            var columns = SearchColumn.Enumerate(context, GetElements());
            SearchUtils.ShowColumnSelector(AddColumns, columns, mousePosition, activeColumnIndex);
        }

        public void SetupColumns(IEnumerable<SearchItem> elements = null)
        {
            SetupColumns(elements ?? m_ViewModel.results, SearchColumnFlags.Default);
        }

        public void SetupColumns(IEnumerable<SearchItem> items, SearchColumnFlags options)
        {
            var currentTableConfig = tableConfig;
            if (currentTableConfig == null)
                currentTableConfig = LoadDefaultTableConfig(reset: false);

            var fields = new HashSet<SearchField>();
            foreach (var e in items ?? GetElements())
                fields.UnionWith(e.GetFields().Where(f => f.value != null));

            if (fields.Count > 0)
                currentTableConfig.columns = fields.Select(f => ItemSelectors.CreateColumn(f.label, f.name, options: options)).ToArray();

            tableConfig = currentTableConfig;
        }

        internal void SetupColumns(IList<SearchField> fields)
        {
            var searchColumns = tableConfig?.columns != null ? new List<SearchColumn>(tableConfig.columns.Where(c =>
            {
                var fp = fields.IndexOf(new SearchField(c.selector));
                if (fp != -1)
                {
                    if (!string.IsNullOrEmpty(fields[fp].alias))
                        c.content.text = fields[fp].alias;
                    else if (fields[fp].value is string alias && !string.IsNullOrEmpty(alias))
                        c.content.text = alias;
                    fields.RemoveAt(fp);
                    return true;
                }

                return (c.options & SearchColumnFlags.Volatile) == 0;
            })) : new List<SearchColumn>();

            foreach (var f in fields)
            {
                var c = ItemSelectors.CreateColumn(f.label, f.name);
                string alias = null;
                if (!string.IsNullOrEmpty(f.alias))
                    alias = f.alias;
                else if (f.value is string a && !string.IsNullOrEmpty(a))
                    alias = a;
                c.content.text = alias ?? c.content.text;

                c.options |= SearchColumnFlags.Volatile;
                searchColumns.Add(c);
            }

            if (searchColumns.Count > 0)
            {
                if (tableConfig != null)
                    tableConfig.columns = searchColumns.ToArray();
                BuildColumns(viewColumns, tableConfig, clear: true);
                ExportTableConfig();
            }
        }

        public void RemoveColumn(int removeColumnAt)
        {
            if (tableConfig?.columns == null || removeColumnAt == -1)
                return;

            var columnToRemove = tableConfig.columns[removeColumnAt];
            if (TryGetViewModelColumn(columnToRemove, out var viewColumn) && viewColumns.Remove(viewColumn))
            {
                var columns = new List<SearchColumn>(tableConfig.columns);
                columns.RemoveAt(removeColumnAt);
                tableConfig.columns = columns.ToArray();
                ExportTableConfig();

                Refresh();
                SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchTableRemoveColumn, columnToRemove.name, columnToRemove.provider, columnToRemove.selector);
            }
        }

        public void SwapColumns(int columnIndex, int swappedColumnIndex)
        {
            if (tableConfig?.columns == null || swappedColumnIndex == -1)
                return;

            var temp = tableConfig.columns[columnIndex];
            tableConfig.columns[columnIndex] = tableConfig.columns[swappedColumnIndex];
            tableConfig.columns[swappedColumnIndex] = temp;

            Refresh();
        }

        private void UpdateColumnSettings(int columnIndex, Column columnSettings)
        {
            if (tableConfig?.columns == null || columnIndex >= tableConfig.columns.Length)
                return;

            var searchColumn = tableConfig.columns[columnIndex];
            searchColumn.width = columnSettings.width.value;
            searchColumn.content = new GUIContent(columnSettings.title, columnSettings.icon.texture);
            if (columnSettings.sortable)
                searchColumn.options |= SearchColumnFlags.CanSort;
            else
                searchColumn.options &= ~SearchColumnFlags.CanSort;
            if (columnSettings.optional)
                searchColumn.options |= SearchColumnFlags.CanHide;
            else
                searchColumn.options &= ~SearchColumnFlags.CanHide;

            // Hack trigger rebinding
            columnSettings.optional = !columnSettings.optional;
            columnSettings.optional = !columnSettings.optional;

            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchTableEditColumn, searchColumn.name, searchColumn.provider, searchColumn.selector);
            SearchColumnSettings.Save(searchColumn);
            Refresh();
        }

        public IEnumerable<SearchColumn> GetColumns()
        {
            if (tableConfig == null || tableConfig.columns == null)
                return Enumerable.Empty<SearchColumn>();
            return tableConfig.columns;
        }

        private void AddColumnHeaderContextMenuItems(DropdownMenu menu, SearchColumn sourceColumn)
        {
            menu.AppendAction("Column Format/Default", (a) =>
            {
                sourceColumn.SetProvider(null);
                Refresh();
            }, string.IsNullOrEmpty(sourceColumn.provider) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            foreach (var scp in SearchColumnProvider.providers)
            {
                var provider = scp.provider;
                var selected = string.Equals(sourceColumn.provider, provider, StringComparison.Ordinal);
                var menuContent = new GUIContent("Column Format/" + ObjectNames.NicifyVariableName(provider));
                menu.AppendAction(menuContent.text, (a) =>
                {
                    sourceColumn.SetProvider(provider);
                    Refresh();
                }, selected ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        private int GetColumnIndex(in string name)
        {
            if (tableConfig.columns == null || tableConfig.columns.Length == 0)
                return -1;

            // Search by selector name first
            for (int i = 0; i < tableConfig.columns.Length; ++i)
            {
                if (string.Equals(tableConfig.columns[i].selector, name, StringComparison.Ordinal))
                    return i;
            }

            // Continue with column name if nothing was found.
            for (int i = 0; i < tableConfig.columns.Length; ++i)
            {
                if (string.Equals(tableConfig.columns[i].name, name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        public int FindColumnIndex(in Column viewColumn)
        {
            if (tableConfig.columns == null || tableConfig.columns.Length == 0)
                return -1;

            if (viewColumn is not SearchTableViewColumn stvc)
                return -1;

            for (int i = 0; i < tableConfig.columns.Length; ++i)
            {
                if (stvc.searchColumn == tableConfig.columns[i])
                    return i;
            }

            return -1;
        }
    }
}
