// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    class SearchReportWindow : EditorWindow, ITableView, IHasCustomMenu
    {
        [SerializeField] private string m_WindowId;
        private SearchReport m_Report;
        private string m_ReportName;
        [SerializeField] private string m_ReportPath;
        [SerializeField] private string m_SearchText;
        private bool m_FocusSearchField = true;
        private List<SearchItem> m_Items;
        private QueryEngine<int> m_QueryEngine = null;
        private static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions { validateFilters = true, skipNestedQueries = true };

        [MenuItem("Window/Search/Open Report...")]
        static void OpenWindow()
        {
            OpenWindow(SearchReport.Import());
        }

        internal static void OpenWindow(string reportPath)
        {
            if (string.IsNullOrEmpty(reportPath))
                return;

            var window = CreateInstance<SearchReportWindow>();
            window.m_WindowId = GUID.Generate().ToString();
            try
            {
                window.InitializeReport(reportPath);
                window.position = Utils.GetMainWindowCenteredPosition(new Vector2(760f, 500f));
                window.Show();
                SearchAnalytics.SendEvent(window.m_WindowId, SearchAnalytics.GenericEventType.ReportViewOpen);
            }
            catch
            {
                Debug.LogError($"Failed to load search report <a>{reportPath}</a>. Make sure <a>{reportPath}</a> is a valid JSON search table report (*.{SearchReport.extension})");
                DestroyImmediate(window);
            }
        }

        private void InitializeReport(string path)
        {
            m_ReportPath = path;
            m_Report = SearchReport.LoadFromFile(path);
            m_ReportName = Path.GetFileNameWithoutExtension(path);
            var searchExpressionProvider = SearchService.GetProvider("expression");
            m_Items = m_Report.CreateSearchItems(searchExpressionProvider).ToList();
            titleContent = new GUIContent($"{m_ReportName} ({m_Items.Count})", m_ReportPath);

            m_FocusSearchField = true;

            // ITableView
            m_TableConfig = new SearchTable(m_ReportName, m_Report.columns);
            for (int i = 0; i < m_TableConfig.columns.Length; ++i)
                InitializeColumn(m_TableConfig.columns[i]);

            m_QueryEngine = new QueryEngine<int>(k_QueryEngineOptions);
            foreach (var column in m_Report.columns)
            {
                var filterName = column.content.text.Replace(" ", "");
                m_QueryEngine.AddFilter(filterName, i => AddFilter(i, column.selector));
                if (filterName != filterName.ToLowerInvariant())
                    m_QueryEngine.AddFilter(filterName.ToLowerInvariant(), i => AddFilter(i, column.selector));

                SearchValue.SetupEngine(m_QueryEngine);
            }
            m_QueryEngine.SetSearchDataCallback(i => m_Items[i].GetFields().Select(f => (f.value ?? "").ToString()), StringComparison.OrdinalIgnoreCase);

            UpdatePropertyTable();
        }

        private SearchValue AddFilter(int itemIndex, string fieldName)
        {
            foreach (var field in m_Report.items[itemIndex].fields)
                if (string.Equals(field.name, fieldName, StringComparison.Ordinal))
                    return ToSearchValue(field.value);
            return SearchValue.invalid;
        }

        private SearchValue ToSearchValue(SearchReport.Value value)
        {
            switch (value.type)
            {
                case SearchReport.ExportedType.None: return SearchValue.invalid;
                case SearchReport.ExportedType.Bool: return new SearchValue((bool)value.TryConvert());
                case SearchReport.ExportedType.Number: return new SearchValue((double)value.TryConvert());
                case SearchReport.ExportedType.String: return new SearchValue(value.data);
                case SearchReport.ExportedType.ObjectReference: return new SearchValue(value.TryConvert()?.ToString() ?? string.Empty);
                case SearchReport.ExportedType.Color: return new SearchValue((Color)value.TryConvert());
            }

            throw new Exception($"Invalid filter for value type {value.type}");
        }

        internal void OnEnable()
        {
            if (m_ReportPath != null)
                InitializeReport(m_ReportPath);
        }

        internal void OnGUI()
        {
            if (m_ReportName != null)
            {
                FocusSearchField();
                using (new EditorGUILayout.VerticalScope(GUIStyle.none, GUILayout.ExpandHeight(true)))
                {
                    using (new GUILayout.HorizontalScope(Styles.searchReportField))
                    {
                        var searchFieldText = string.IsNullOrEmpty(m_SearchText) ? m_Report.query : m_SearchText;
                        var searchTextRect = SearchField.GetRect(searchFieldText, position.width, (Styles.toolbarButton.fixedWidth + Styles.toolbarButton.margin.left) + Styles.toolbarButton.margin.right);
                        var searchClearButtonRect = Styles.searchFieldBtn.margin.Remove(searchTextRect);
                        searchClearButtonRect.xMin = searchClearButtonRect.xMax - 20f;

                        if (Event.current.type == EventType.MouseUp && searchClearButtonRect.Contains(Event.current.mousePosition))
                            ClearSearch();

                        var previousSearchText = m_SearchText;
                        if (Event.current.type != EventType.KeyDown || Event.current.keyCode != KeyCode.None || Event.current.character != '\r')
                        {
                            m_SearchText = SearchField.Draw(searchTextRect, m_SearchText, Styles.searchField);
                            if (!string.Equals(previousSearchText, m_SearchText, StringComparison.Ordinal))
                            {
                                UpdatePropertyTable();
                            }
                        }

                        if (string.IsNullOrEmpty(m_SearchText))
                        {
                            GUI.Label(searchTextRect, m_Report.query, Styles.placeholderTextStyle);
                        }
                        else
                        {
                            if (GUI.Button(searchClearButtonRect, Icons.clear, Styles.searchFieldBtn))
                                ClearSearch();
                            EditorGUIUtility.AddCursorRect(searchClearButtonRect, MouseCursor.Arrow);
                        }

                        EditorGUIUtility.AddCursorRect(searchClearButtonRect, MouseCursor.Arrow);
                    }
                    using (var s = new EditorGUILayout.VerticalScope(GUIStyle.none, GUILayout.ExpandHeight(true)))
                    {
                        TableViewOnGUI(s.rect);
                    }
                }
            }
        }

        internal void Update()
        {
            if (SearchField.UpdateBlinkCursorState(EditorApplication.timeSinceStartup))
                Repaint();
        }

        private void ClearSearch()
        {
            m_SearchText = "";
            m_FocusSearchField = true;
            UpdatePropertyTable();
            GUI.changed = true;
            GUI.FocusControl(null);
            GUIUtility.ExitGUI();
        }

        private void FocusSearchField()
        {
            if (Event.current.type != EventType.Repaint)
                return;
            if (m_FocusSearchField)
            {
                SearchField.Focus();
                m_FocusSearchField = false;
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Import Report"), false, () =>
            {
                var reportPath = SearchReport.Import();
                if (string.IsNullOrEmpty(reportPath))
                    return;
                m_SearchText = string.Empty;
                Focus();
                InitializeReport(reportPath);
            });
            menu.AddItem(new GUIContent("Export CSV"), false, () =>
            {
                SearchReport.ExportAsCsv(GetSearchTable().name, GetColumns(), GetRows(), context);
                Focus();
            });
        }

        public static void InitializeColumn(SearchColumn column)
        {
            column.provider = "";
            column.getter = args => { args.item.TryGetValue(args.column.selector, out var field); return field.value; };
            column.setter = null;
            column.drawer = args =>
            {
                if (args.value == null)
                    return null;
                if (args.value is SearchReport.ObjectValue v)
                {
                    if (v.objectValue == null)
                        PropertyTable.DefaultDrawing(args.rect, args.column, v.fallbackString);
                    else
                        PropertySelectors.DrawObjectReference(args.rect, v.objectValue);
                }
                else if (args.value is Color c)
                    return EditorGUI.ColorField(args.rect, c, showEyedropper: false, showAlpha: true);
                else
                    PropertyTable.DefaultDrawing(args.rect, args.column, args.value);
                return args.value;
            };
            column.comparer = null;
        }

        ////////////////////////////////////////////
        ///              ITableView
        ////////////////////////////////////////////
        private PropertyTable m_PropertyTable;
        private SearchTable m_TableConfig;

        public SearchContext context { get; } = new SearchContext(new SearchExpressionProvider());

        public IEnumerable<SearchItem> GetElements()
        {
            if (string.IsNullOrEmpty(m_SearchText) || m_QueryEngine == null)
                return m_Items;
            var query = m_QueryEngine.Parse(m_SearchText);
            if (!query.valid)
                return m_Items;
            return query.Apply(Enumerable.Range(0, m_Items.Count), false).Select(i => m_Items[i]);
        }

        public IEnumerable<SearchColumn> GetColumns()
        {
            if (m_TableConfig == null || m_TableConfig.columns == null)
                return Enumerable.Empty<SearchColumn>();
            return m_TableConfig.columns;
        }

        public void SwapColumns(int columnIndex, int swappedColumnIndex)
        {
            if (m_TableConfig == null || swappedColumnIndex == -1)
                return;

            var temp = m_TableConfig.columns[columnIndex];
            m_TableConfig.columns[columnIndex] = m_TableConfig.columns[swappedColumnIndex];
            m_TableConfig.columns[swappedColumnIndex] = temp;
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

            SearchColumnSettings.Save(searchColumn);
        }

        internal void UpdatePropertyTable()
        {
            m_PropertyTable?.Dispose();
            if (m_TableConfig != null)
            {
                var tableId = $"SearchPropertyTable_V90_{m_TableConfig?.id ?? "none" }";
                m_PropertyTable = new PropertyTable(tableId, this);
            }
        }

        public SearchTable GetSearchTable()
        {
            return m_TableConfig;
        }

        public IEnumerable<SearchItem> GetRows()
        {
            return m_PropertyTable.GetRows().Select(ti => ((PropertyItem)ti).GetData());
        }

        internal void TableViewOnGUI(Rect tableRect)
        {
            if (m_PropertyTable != null)
                m_PropertyTable.OnGUI(tableRect);
        }

        public void AddColumn(Vector2 mousePosition, int activeColumnIndex) => throw new NotImplementedException();
        public void AddColumns(IEnumerable<SearchColumn> newColumns, int insertColumnAt) => throw new NotImplementedException();
        public void RemoveColumn(int removeColumnAt) => throw new NotImplementedException();
        public void AddColumnHeaderContextMenuItems(GenericMenu menu, SearchColumn sourceColumn) => throw new NotImplementedException();
        public void SetupColumns(IEnumerable<SearchItem> items = null) => throw new NotImplementedException();

        public void SetSelection(IEnumerable<SearchItem> items)
        {
            // Selection not handled
        }

        public bool OpenContextualMenu(Event evt, SearchItem item)
        {
            return false;
        }
    }
}
