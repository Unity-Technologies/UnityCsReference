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
        const string TableConfigSessionKey = "CurrentSearchTableConfig_V1";

        private PropertyTable m_PropertyTable;
        private bool m_Disposed = false;
        private SearchTable m_TableConfig;
        private string m_TableId;

        private event Action m_NextFrame;

        // Keep a static table for when we open a new table view
        static private SearchTable s_ActiveSearchTable { get; set; }

        public TableView(ISearchView hostView)
            : base(hostView)
        {
            m_TableConfig = s_ActiveSearchTable;
            m_TableId = Guid.NewGuid().ToString("N");
        }

        ~TableView() => Dispose(false);

        public override void Dispose()
        {
            Dispose(true);
        }

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
                {
                    m_PropertyTable.OnGUI(screenRect);
                }
            }
            else
                GUI.Label(screenRect, "No table configuration selected", Styles.noResult);
        }

        public override void Refresh(RefreshFlags flags = RefreshFlags.Default)
        {
            base.Refresh(flags);
            Update();
        }

        public override ResultViewState SaveViewState(string name)
        {
            var viewState = base.SaveViewState(name);
            viewState.tableConfig = m_TableConfig.Clone();
            viewState.tableConfig.name = name;
            return viewState;
        }

        public override void SetViewState(ResultViewState viewState)
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

            var currentNames = m_TableConfig.columns.Select(c => c.name).ToList();
            var columns = new List<SearchColumn>(m_TableConfig.columns);
            if (insertColumnAt == -1)
                insertColumnAt = columns.Count;
            var columnCountBefore = columns.Count;
            columns.InsertRange(insertColumnAt, newColumns.Where(n => !currentNames.Contains(n.name)));

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
            if (m_TableConfig == null)
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
            menu.AddItem(GUIContent.Temp("Column Format/Default"), string.IsNullOrEmpty(sourceColumn.provider), () => sourceColumn.SetProvider(null));
            foreach (var scp in SearchColumnProvider.providers)
            {
                var provider = scp.provider;
                var selected = string.Equals(sourceColumn.provider, provider, StringComparison.Ordinal);
                var menuContent = new GUIContent("Column Format/" + ObjectNames.NicifyVariableName(provider));
                menu.AddItem(menuContent, selected, () => sourceColumn.SetProvider(provider));
            }
        }

        public void SetSelection(IEnumerable<SearchItem> items)
        {
            searchView.SetSelection(items.Select(e => searchView.results.IndexOf(e)).Where(i => i != -1).ToArray());
        }

        public void SetDirty()
        {
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
                SessionState.SetString(TableConfigSessionKey, m_TableConfig.Export());

            m_Disposed = true;
        }

        private void Schedule(Action nextFrame)
        {
            m_NextFrame += nextFrame;
            searchView?.Repaint();
        }

        private void Update()
        {
            if (items == null)
                return;

            // Set a default configuration if none
            if (m_TableConfig == null)
            {
                var sessionSearchTableData = SessionState.GetString(TableConfigSessionKey, null);
                if (string.IsNullOrEmpty(sessionSearchTableData))
                    SetSearchTable(SearchTable.CreateDefault());
                else
                    SetSearchTable(SearchTable.Import(sessionSearchTableData));
            }

            UpdatePropertyTable();
        }

        private void UpdatePropertyTable()
        {
            m_PropertyTable?.Dispose();
            if (m_TableConfig != null)
                m_PropertyTable = new PropertyTable(m_TableId, this);
            searchView.Repaint();
        }

        public void SetSearchTable(SearchTable tableConfig)
        {
            m_TableId = Guid.NewGuid().ToString("N");
            s_ActiveSearchTable = m_TableConfig = tableConfig;
            UpdatePropertyTable();
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
            var fields = new HashSet<SearchItem.Field>();
            foreach (var e in items ?? GetElements())
                fields.UnionWith(e.GetFields().Where(f => f.value != null));

            if (m_TableConfig != null && fields.Count > 0)
            {
                m_TableConfig.columns = fields.Select(f => ItemSelectors.CreateColumn(f.label, f.name, options: options)).ToArray();
                SetSearchTable(m_TableConfig);
            }
            else
                SetSearchTable(SearchTable.CreateDefault());
        }

        public void SetupColumns(IList<SearchItem.Field> fields)
        {
            SearchTable tableConfig = GetSearchTable();

            var columns = new List<SearchColumn>(tableConfig.columns.Where(c =>
            {
                var fp = fields.IndexOf(new SearchItem.Field(c.selector));
                if (fp != -1)
                {
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

        public static void SetupColumns(SearchContext context, SearchExpression expression)
        {
            if (!(context.searchView is QuickSearch qs) || !(qs.resultView is TableView tv))
                return;

            if (expression.evaluator.name == nameof(Evaluators.Select))
            {
                var selectors = expression.parameters.Skip(1).Where(e => Evaluators.IsSelectorLiteral(e));
                var tableViewFields = new List<SearchItem.Field>(selectors.Select(s => new SearchItem.Field(s.innerText.ToString(), s.alias.ToString())));
                tv.SetupColumns(tableViewFields);
            }
        }

        public bool OpenContextualMenu(Event evt, SearchItem item)
        {
            var selection = searchView.selection;
            if (selection.Count > 1)
                return false;

            var contextRect = new Rect(evt.mousePosition, new Vector2(1, 1));
            searchView.ShowItemContextualMenu(item, contextRect);
            return true;
        }
    }
}
