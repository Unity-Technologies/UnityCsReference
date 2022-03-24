// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    class TableView : ResultView, ITableView
    {
        private PropertyTable m_PropertyTable;
        private bool m_Disposed = false;
        private SearchTable m_TableConfig;
        private string m_TableId;

        private event Action m_NextFrame;

        // Keep a static table for when we open a new table view
        static private SearchTable s_ActiveSearchTable { get; set; }

        public override bool showNoResultMessage => context.empty;

        public TableView(ISearchView hostView, SearchTable tableConfig)
            : base(hostView)
        {
            SetSearchTable(tableConfig ?? s_ActiveSearchTable);
        }

        ~TableView() => Dispose(false);

        public override void Dispose()
        {
            Dispose(true);
        }

        public bool readOnly => false;

        public IEnumerable<SearchItem> GetElements()
        {
            return items;
        }

        public override void Draw(Rect screenRect, ICollection<int> selection)
        {
            if (Event.current.type == EventType.Repaint)
                AdvancedDropdownGUI.LoadStyles();

            if (Event.current.type == EventType.Repaint)
            {
                m_NextFrame?.Invoke();
                m_NextFrame = null;
            }

            if (m_PropertyTable != null)
            {
                using (SearchMonitor.GetView())
                {
                    m_PropertyTable.OnGUI(screenRect);
                }
            }
            else
                GUI.Label(screenRect, L10n.Tr("No table configuration selected"), Styles.noResult);
        }

        public override void Refresh(RefreshFlags flags = RefreshFlags.Default)
        {
            base.Refresh(flags);
            Update(flags);
        }

        public override SearchViewState SaveViewState(string name)
        {
            var viewState = base.SaveViewState(name);
            viewState.tableConfig = m_TableConfig.Clone();
            viewState.tableConfig.name = name;
            return viewState;
        }

        public override void SetViewState(SearchViewState viewState)
        {
            if (viewState.tableConfig == null)
                return;
            SetSearchTable(viewState.tableConfig.Clone());
        }

        protected override void HandleKeyEvent(Event evt, List<int> selection)
        {
            if (evt.isKey)
                return;
            base.HandleKeyEvent(evt, selection);
        }

        public SearchColumn FindColumnBySelector(string selector)
        {
            var columnIndex = GetColumnIndex(selector);
            if (columnIndex == -1)
                return null;
            return m_TableConfig.columns[columnIndex];
        }

        public int GetColumnIndex(string name)
        {
            if (m_TableConfig == null || m_TableConfig.columns == null)
                return -1;

            for (int i = 0; i < m_TableConfig.columns.Length; ++i)
            {
                if (string.Equals(m_TableConfig.columns[i].selector, name, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        public IEnumerable<SearchColumn> GetColumns()
        {
            if (m_TableConfig == null || m_TableConfig.columns == null)
                return Enumerable.Empty<SearchColumn>();
            return m_TableConfig.columns;
        }

        public IEnumerable<SearchItem> GetRows()
        {
            return m_PropertyTable.GetRows().Select(ti => ((PropertyItem)ti).GetData());
        }

        /// Mainly used for test
        internal IEnumerable<object> GetValues(int columnIdx)
        {
            if (m_PropertyTable == null || m_TableConfig == null || m_TableConfig.columns == null)
                yield break;

            var column = m_TableConfig.columns[columnIdx];
            foreach (var ltd in m_PropertyTable.GetRows().Select(ti => ((PropertyItem)ti).GetData()))
                yield return column.ResolveValue(ltd, context);
        }

        public void AddColumn(Vector2 mousePosition, int activeColumnIndex)
        {
            var columns = SearchColumn.Enumerate(context, GetElements());
            Schedule(() => ColumnSelector.AddColumns(AddColumns, columns, mousePosition, activeColumnIndex));
        }

        public void AddColumns(IEnumerable<SearchColumn> newColumns, int insertColumnAt)
        {
            if (m_TableConfig == null)
                return;

            var columns = new List<SearchColumn>(m_TableConfig.columns);
            if (insertColumnAt == -1)
                insertColumnAt = columns.Count;
            var columnCountBefore = columns.Count;
            columns.InsertRange(insertColumnAt, newColumns);

            var columnAdded = columns.Count - columnCountBefore;
            if (columnAdded > 0)
            {
                var firstColumn = newColumns.First();
                var e = SearchAnalytics.GenericEvent.Create(null, SearchAnalytics.GenericEventType.QuickSearchTableAddColumn, firstColumn.name);
                e.intPayload1 = columnAdded;
                e.message = firstColumn.provider;
                e.description = firstColumn.selector;
                SearchAnalytics.SendEvent(e);

                m_TableConfig.columns = columns.ToArray();
                UpdatePropertyTable();

                m_PropertyTable?.FrameColumn(insertColumnAt - 1);
            }
        }

        public void RemoveColumn(int removeColumnAt)
        {
            if (m_TableConfig == null || removeColumnAt == -1)
                return;

            var columns = new List<SearchColumn>(m_TableConfig.columns);
            var columnToRemove = columns[removeColumnAt];
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchTableRemoveColumn, columnToRemove.name, columnToRemove.provider, columnToRemove.selector);
            columns.RemoveAt(removeColumnAt);
            m_TableConfig.columns = columns.ToArray();
            UpdatePropertyTable();
        }

        public void SwapColumns(int columnIndex, int swappedColumnIndex)
        {
            if (m_TableConfig == null || swappedColumnIndex == -1)
                return;

            var temp = m_TableConfig.columns[columnIndex];
            m_TableConfig.columns[columnIndex] = m_TableConfig.columns[swappedColumnIndex];
            m_TableConfig.columns[swappedColumnIndex] = temp;
            SetDirty();
        }

        public void UpdateColumnSettings(int columnIndex, MultiColumnHeaderState.Column columnSettings)
        {
            if (m_TableConfig == null || columnIndex >= m_TableConfig.columns.Length)
                return;

            var searchColumn = m_TableConfig.columns[columnIndex];
            searchColumn.width = columnSettings.width;
            searchColumn.content = columnSettings.headerContent;
            searchColumn.options &= ~SearchColumnFlags.TextAligmentMask;
            switch (columnSettings.headerTextAlignment)
            {
                case TextAlignment.Left: searchColumn.options |= SearchColumnFlags.TextAlignmentLeft; break;
                case TextAlignment.Center: searchColumn.options |= SearchColumnFlags.TextAlignmentCenter; break;
                case TextAlignment.Right: searchColumn.options |= SearchColumnFlags.TextAlignmentRight; break;
            }

            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchTableEditColumn, searchColumn.name, searchColumn.provider, searchColumn.selector);

            SearchColumnSettings.Save(searchColumn);
            SetDirty();
        }

        public void AddColumnHeaderContextMenuItems(GenericMenu menu, SearchColumn sourceColumn)
        {
            menu.AddItem(new GUIContent("Column Format/Default"), string.IsNullOrEmpty(sourceColumn.provider), () => sourceColumn.SetProvider(null));
            foreach (var scp in SearchColumnProvider.providers)
            {
                var provider = scp.provider;
                var selected = string.Equals(sourceColumn.provider, provider, StringComparison.Ordinal);
                var menuContent = new GUIContent("Column Format/" + ObjectNames.NicifyVariableName(provider));
                menu.AddItem(menuContent, selected, () => sourceColumn.SetProvider(provider));
            }
        }

        public bool AddColumnHeaderContextMenuItems(GenericMenu menu)
        {
            return false;
        }

        public void SetSelection(IEnumerable<SearchItem> items)
        {
            searchView.SetSelection(items.Select(e => searchView.results.IndexOf(e)).Where(i => i != -1).ToArray());
        }

        public void OnItemExecuted(SearchItem item)
        {
            searchView.ExecuteSelection();
        }

        public void SetDirty()
        {
            s_ActiveSearchTable = m_TableConfig;
            if (m_TableConfig == null)
                return;
            searchView.Repaint();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            m_NextFrame = null;
            if (!disposing)
                return;

            m_PropertyTable?.Dispose();

            if (m_TableConfig != null)
                ExportTableConfig();

            m_Disposed = true;
        }

        private void Schedule(Action nextFrame)
        {
            m_NextFrame += nextFrame;
            searchView?.Repaint();
        }

        private void Update(RefreshFlags flags)
        {
            if (items == null)
                return;

            // Set a default configuration if none
            if (m_TableConfig == null)
                SetSearchTable(LoadDefaultTableConfig(reset: false));

            if ((flags & RefreshFlags.ItemsChanged) == RefreshFlags.ItemsChanged)
                UpdatePropertyTable();
        }

        string GetDefaultGroupId(string id = null)
        {
            var key = "CurrentSearchTableConfig_V2";
            if (id == null && searchView is QuickSearch qs)
            {
                var providers = qs.context.GetProviders();
                if (providers.Count == 1)
                    key += "_" + providers[0].id;
                else if (!string.IsNullOrEmpty(qs.currentGroup))
                    key += "_" + qs.currentGroup;
            }
            else if (!string.IsNullOrEmpty(id))
                key += "_" + id;
            return key;
        }

        void ExportTableConfig(string id = null)
        {
            if (m_TableConfig == null)
                return;
            SessionState.SetString(GetDefaultGroupId(id), m_TableConfig.Export());
        }

        SearchTable LoadDefaultTableConfig(bool reset, string id = null, SearchTable defaultConfig = null)
        {
            if (!reset)
            {
                var sessionSearchTableData = SessionState.GetString(GetDefaultGroupId(id), null);
                if (!string.IsNullOrEmpty(sessionSearchTableData))
                    return SearchTable.Import(sessionSearchTableData);
            }

            if (searchView is QuickSearch qs)
            {
                var providers = qs.context.GetProviders();
                var provider = providers.Count == 1 ? providers.FirstOrDefault() : SearchService.GetProvider(qs.currentGroup);
                if (provider?.tableConfig != null)
                    return provider.tableConfig(context);
            }

            return defaultConfig ?? SearchTable.CreateDefault();
        }

        private void UpdatePropertyTable()
        {
            m_PropertyTable?.Dispose();
            if (m_TableConfig != null)
                m_PropertyTable = new PropertyTable(m_TableId, this);
            searchView.Repaint();
        }

        public bool SetSearchTable(SearchTable tableConfig)
        {
            if (tableConfig == m_TableConfig)
                return false;

            m_TableId = Guid.NewGuid().ToString("N");
            m_TableConfig = tableConfig;
            if (m_TableConfig != null)
            {
                if (m_TableConfig.columns.Length == 0)
                    SetupColumns();
                else
                    UpdatePropertyTable();

                return true;
            }

            return false;
        }

        public SearchTable GetSearchTable()
        {
            return m_TableConfig;
        }

        public void SetupColumns(IEnumerable<SearchItem> items = null)
        {
            SetupColumns(items, SearchColumnFlags.Default);
        }

        public void SetupColumns(IEnumerable<SearchItem> items, SearchColumnFlags options)
        {
            var fields = new HashSet<SearchField>();
            foreach (var e in items ?? GetElements())
                fields.UnionWith(e.GetFields().Where(f => f.value != null));

            if (m_TableConfig != null && fields.Count > 0)
            {
                m_TableConfig.columns = fields.Select(f => ItemSelectors.CreateColumn(f.label, f.name, options: options)).ToArray();
                SetSearchTable(m_TableConfig);
            }
            else
                SetSearchTable(LoadDefaultTableConfig(reset: true));
        }

        public void SetupColumns(IList<SearchField> fields)
        {
            SearchTable tableConfig = GetSearchTable();

            var columns = new List<SearchColumn>(tableConfig.columns.Where(c =>
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
            }));

            foreach (var f in fields)
            {
                var c = ItemSelectors.CreateColumn(f.label, f.name);
                c.options |= SearchColumnFlags.Volatile;
                columns.Add(c);
            }

            if (columns.Count > 0)
            {
                tableConfig.columns = columns.ToArray();
                SetSearchTable(tableConfig);
            }
        }

        public override void OnGroupChanged(string prevGroupId, string newGroupId)
        {
            base.OnGroupChanged(prevGroupId, newGroupId);
            if (!SetSearchTable(LoadDefaultTableConfig(reset: false, newGroupId, m_TableConfig)))
                UpdatePropertyTable();
        }

        public static void SetupColumns(SearchContext context, SearchExpression expression)
        {
            if (!(context.searchView is QuickSearch qs) || !(qs.resultView is TableView tv))
                return;

            if (expression.evaluator.name == nameof(Evaluators.Select))
            {
                var selectors = expression.parameters.Skip(1).Where(e => Evaluators.IsSelectorLiteral(e));
                var tableViewFields = new List<SearchField>(selectors.Select(s => new SearchField(s.innerText.ToString(), s.alias.ToString())));
                tv.SetupColumns(tableViewFields);
            }
        }

        public bool OpenContextualMenu(Event evt, SearchItem item)
        {
            var selection = searchView.selection;
            if (selection.Count <= 0 && item == null)
                return false;

            var contextRect = new Rect(evt.mousePosition, new Vector2(1, 1));
            searchView.ShowItemContextualMenu(item, contextRect);
            return true;
        }

        public override void AddSaveQueryMenuItems(SearchContext context, GenericMenu menu)
        {
            menu.AddSeparator("");
            menu.AddItem(EditorGUIUtility.TrTextContent("Export Report..."), false, () => ExportJson(context));
            menu.AddItem(EditorGUIUtility.TrTextContent("Export CSV..."), false, () => ExportCsv(context));
        }

        private void ExportJson(SearchContext context)
        {
            SearchReport.Export(GetSearchTable().name, GetColumns(), GetRows(), context);
        }

        private void ExportCsv(SearchContext context)
        {
            SearchReport.ExportAsCsv(GetSearchTable().name, GetColumns(), GetRows(), context);
        }


        public override void DrawTabsButtons()
        {
            if (EditorGUILayout.DropdownButton(Styles.resetSearchColumnsContent, FocusType.Keyboard, Styles.tabButton))
            {
                SetupColumns();
                SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchTableReset, context.searchQuery);
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
        }
    }
}
