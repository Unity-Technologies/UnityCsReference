// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Internal;

namespace UnityEditor.Search
{
    class SearchTableView : SearchBaseCollectionView<MultiColumnListView>, ITableView
    {
        internal struct ItemCellDescriptor
        {
            public string itemId;
            public string itemProvider;
            public int columnId;

            public bool valid => (itemId != null && itemProvider != null && columnId != 0);
            public string cellItemId => GetCellItemId(itemId, itemProvider, columnId).ToString();

            public static int GetCellItemId(SearchItem item, SearchColumn column)
            {
                return Utils.CombineHashCodes(item.id.GetHashCode(), item.provider.id.GetHashCode(), column.GetHashCode());
            }

            public static int GetCellItemId(string itemId, string itemProvider, int columnId)
            {
                return Utils.CombineHashCodes(itemId.GetHashCode(), itemProvider.GetHashCode(), columnId);
            }
        }

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
                UpdateColumns();
            }
        }

        bool ITableView.readOnly => false;
        internal static string resultViewId = "table";
        public override string ViewId => resultViewId;
        public override bool ShowNoResultMessage => m_ViewModel.displayMode != DisplayMode.Table;

        internal event Action<SearchTableView, SearchTableViewCell, object> cellValueChanged;
        internal SearchTableViewCell editingCell => m_EditedCell;

        private bool m_LastEventIsUndo;
        private SearchTableViewCell m_EditedCell;
        private ItemCellDescriptor m_EditedCellDescriptor;
        private Action<SearchTableViewCell> m_StopListening;

        Columns viewColumns => m_ListView.columns;

        const float k_DefaultItemHeight = 22f;

        public static SearchTableView Create(ISearchView viewModel)
        {
            return new SearchTableView(viewModel);
        }

        public static Texture2D FetchIcon()
        {
            return EditorGUIUtility.LoadIconRequired("TableView");
        }

        public static SearchResultViewDescriptor GetDescriptor()
        {
            return new SearchResultViewDescriptor(resultViewId, Create, FetchIcon,
                (float)DisplayMode.Table,
                description: "Table View",
                buttonClassName: "search-statusbar__table-mode-button");
        }

        public SearchTableView(ISearchView viewModel)
            : base("SearchTableView", viewModel, ussClassName)
        {
            var fixItemHeight = tableConfig != null ? MathF.Max(k_DefaultItemHeight, tableConfig.itemHeight) : k_DefaultItemHeight;
            m_ListView = new MultiColumnListView(BuildColumns(tableConfig))
            {
                fixedItemHeight = fixItemHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                selectionType = m_ViewModel.multiselect ? SelectionType.Multiple : SelectionType.Single,
                itemsSource = (IList)m_ViewModel.results,
                sortingMode = ColumnSortingMode.Custom
            };
            m_ListView.AddToClassList(resultsListClassName);
            Add(m_ListView);

            if (tableConfig == null)
                SetupColumns();

            SetupColumnSorting();
            m_EditedCellDescriptor = k_InvalidDescriptor;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                // This will ensure all Cells are unbound.
                m_ListView.itemsSource = null;
            }
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);
            m_EditedCellDescriptor = k_InvalidDescriptor;
            m_ListView.columnSortingChanged += OnSortColumn;
            m_ListView.columns.columnReordered += OnColumnReordered;

            var header = this.Q<MultiColumnCollectionHeader>();
            header.contextMenuPopulateEvent += OnSearchColumnContextualMenu;

            On(SearchEvent.SearchContextChanged, OnContextChanged);
            On(SearchEvent.RequestResultViewButtons, OnAddResultViewButtons);

            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            Undo.undoRedoEvent += OnUndoRedoEvent;
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterValueChange();

            Off(SearchEvent.SearchContextChanged, OnContextChanged);
            Off(SearchEvent.RequestResultViewButtons, OnAddResultViewButtons);

            Undo.undoRedoEvent -= OnUndoRedoEvent;
            UnregisterCallback<FocusInEvent>(OnFocusIn);
            UnregisterCallback<FocusOutEvent>(OnFocusOut);
            UnregisterCallback<KeyDownEvent>(OnKeyDown);

            var header = this.Q<MultiColumnCollectionHeader>();
            header.contextMenuPopulateEvent -= OnSearchColumnContextualMenu;

            m_ListView.columnSortingChanged -= OnSortColumn;
            m_ListView.columns.columnReordered -= OnColumnReordered;

            base.OnDetachFromPanel(evt);
        }

        public override void AddSaveQueryMenuItems(SearchContext context, GenericMenu menu)
        {
            menu.AddSeparator("");
            menu.AddItem(EditorGUIUtility.TrTextContent("Export Report..."), false, () => ExportJson(context));
            menu.AddItem(EditorGUIUtility.TrTextContent("Export CSV..."), false, () => ExportCsv(context));
        }

        private void ExportJson(SearchContext context)
        {
            SearchReport.Export(tableConfig.name, GetColumns(), m_ViewModel.results, context);
        }

        private void ExportCsv(SearchContext context)
        {
            SearchReport.ExportAsCsv(tableConfig.name, GetColumns(), m_ViewModel.results, context);
        }

        private bool IsTableViewRow(VisualElement ve)
        {
            return ve.ClassListContains("unity-multi-column-view__row-container");
        }

        #region ValueChange Event Handling
        protected override void OnPointerDown(PointerDownEvent evt)
        {
            m_LastEventIsUndo = false;

            if (evt.clickCount != 1 && evt.button != 0)
                return;

            if (evt.target is not VisualElement ve)
                return;

            if (!IsTableViewRow(ve) && ve.GetFirstAncestorWhere((ve) => { return IsTableViewRow(ve); }) == null)
                m_ListView.ClearSelection();
        }
        
        void OnFocusIn(FocusInEvent evt)
        {
            // This ensures we capture which cell is interacted with by our user.
            m_LastEventIsUndo = false;
            SetEditedCell(null);
            if (evt.target is VisualElement targetEl)
            {
                if (targetEl is not SearchTableViewCell cell)
                {
                    cell = targetEl.GetFirstAncestorOfType<SearchTableViewCell>();
                }
                if (cell != null)
                {
                    SetEditedCell(cell);
                }
            }
        }

        void OnFocusOut(FocusOutEvent evt)
        {
            m_LastEventIsUndo = false;
            SetEditedCell(null);
        }

        internal void SetEditedCell(SearchTableViewCell cell)
        {
            // EditedCell is the only cell that actively listens to valueChanged.
            UnregisterValueChange();

            m_EditedCell = cell;

            if (m_EditedCell != null)
            {
                RegisterValueChange(m_EditedCell);
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            m_LastEventIsUndo = false;
        }

        void OnUndoRedoEvent(in UndoRedoInfo info)
        {
            m_LastEventIsUndo = true;
            Refresh();
        }

        bool RegisterMaterialProperty(SearchTableViewCell cell, MaterialProperty matProp)
        {
            var successfulRegistration = true;
            switch (matProp.propertyType)
            {
                case UnityEngine.Rendering.ShaderPropertyType.Color:
                    RegisterValueChange<Color>(cell);
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    RegisterValueChange<Vector4>(cell);
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Float:
                    RegisterValueChange<float>(cell);
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Texture:
                    RegisterValueChange<UnityEngine.Object>(cell);
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Range:
                    RegisterValueChange<float>(cell);
                    break;
                default:
                    successfulRegistration = false;
                    break;
            }
            return successfulRegistration;
        }

        bool RegisterSerializedProperty(SearchTableViewCell cell, SerializedProperty prop)
        {
            var successfulRegistration = true;
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                {
                    RegisterValueChange<int>(cell);
                    break;
                }
                case SerializedPropertyType.Boolean:
                {
                    RegisterValueChange<bool>(cell);
                    break;
                }
                case SerializedPropertyType.Float:
                {
                    RegisterValueChange<float>(cell);
                    break;
                }
                case SerializedPropertyType.String:
                {
                    RegisterValueChange<string>(cell);
                    break;
                }
                case SerializedPropertyType.Enum:
                {
                    // Note: PropertyField emit ValueChanged on string and NOT on Enum.
                    RegisterValueChange<string>(cell);
                    break;
                }
                case SerializedPropertyType.Color:
                {
                    RegisterValueChange<Color>(cell);
                    break;
                }
                case SerializedPropertyType.Vector2:
                {
                    RegisterValueChange<Vector2>(cell);
                    break;
                }
                case SerializedPropertyType.Vector3:
                {
                    RegisterValueChange<Vector3>(cell);
                    break;
                }
                case SerializedPropertyType.Vector4:
                {
                    RegisterValueChange<Vector4>(cell);
                    break;
                }
                case SerializedPropertyType.Quaternion:
                {
                    RegisterValueChange<Quaternion>(cell);
                    break;
                }
                case SerializedPropertyType.Vector2Int:
                {
                    RegisterValueChange<Vector2Int>(cell);
                    break;
                }
                case SerializedPropertyType.Vector3Int:
                {
                    RegisterValueChange<Vector3Int>(cell);
                    break;
                }
                case SerializedPropertyType.BoundsInt:
                {
                    RegisterValueChange<BoundsInt>(cell);
                    break;
                }
                case SerializedPropertyType.Bounds:
                {
                    RegisterValueChange<Bounds>(cell);
                    break;
                }
                case SerializedPropertyType.Rect:
                {
                    RegisterValueChange<Rect>(cell);
                    break;
                }
                case SerializedPropertyType.RectInt:
                {
                    RegisterValueChange<RectInt>(cell);
                    break;
                }
                case SerializedPropertyType.ObjectReference:
                {
                    RegisterValueChange<UnityEngine.Object>(cell);
                    break;
                }
                case SerializedPropertyType.LoadableReference:
                {
                    RegisterValueChange<UnityEngine.Object>(cell);
                    break;
                }
                default:
                    successfulRegistration = false;
                    break;
            }
            return successfulRegistration;
        }

        bool RegisterValueChange(SearchTableViewCell cell, Type t, object value)
        {
            var successfulRegistration = true;
            if (t == typeof(SerializedProperty))
            {
                var prop = (SerializedProperty)value;
                return RegisterSerializedProperty(cell, prop);
            }
            else if (t == typeof(MaterialProperty))
            {
                var matProp = (MaterialProperty)value;
                return RegisterMaterialProperty(cell, matProp);
            }
            else if (t == typeof(bool))
            {
                RegisterValueChange<bool>(cell);
            }
            else if (t == typeof(int))
            {
                RegisterValueChange<int>(cell);
            }
            else if (t == typeof(float))
            {
                RegisterValueChange<float>(cell);
            }
            else if (t == typeof(double))
            {
                RegisterValueChange<double>(cell);
            }
            else if (t == typeof(uint))
            {
                RegisterValueChange<uint>(cell);
            }
            else if (t == typeof(string))
            {
                RegisterValueChange<string>(cell);
            }
            else if (t == typeof(Color))
            {
                RegisterValueChange<Color>(cell);
            }
            else if (t.IsEnum)
            {
                RegisterValueChange<Enum>(cell);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            {
                RegisterValueChange<UnityEngine.Object>(cell);
            }
            else if (t == typeof(Vector2))
            {
                RegisterValueChange<Vector2>(cell);
            }
            else if (t == typeof(Vector3))
            {
                RegisterValueChange<Vector3>(cell);
            }
            else if (t == typeof(Vector4))
            {
                RegisterValueChange<Vector4>(cell);
            }
            else if (t == typeof(Rect))
            {
                RegisterValueChange<Rect>(cell);
            }
            else if (t == typeof(RectInt))
            {
                RegisterValueChange<RectInt>(cell);
            }
            else if (t == typeof(AnimationCurve))
            {
                RegisterValueChange<AnimationCurve>(cell);
            }
            else if (t == typeof(Bounds))
            {
                RegisterValueChange<Bounds>(cell);
            }
            else if (t == typeof(BoundsInt))
            {
                RegisterValueChange<BoundsInt>(cell);
            }
            else if (t == typeof(Gradient))
            {
                RegisterValueChange<Gradient>(cell);
            }
            else if (t == typeof(Quaternion))
            {
                RegisterValueChange<Quaternion>(cell);
            }
            else if (t == typeof(Vector2Int))
            {
                RegisterValueChange<Vector2Int>(cell);
            }
            else if (t == typeof(Vector3Int))
            {
                RegisterValueChange<Vector3Int>(cell);
            }
            else if (t == typeof(Hash128))
            {
                RegisterValueChange<Hash128>(cell);
            }
            else
            {
                successfulRegistration = false;
            }
            return successfulRegistration;
        }

        void RegisterValueChange<T>(SearchTableViewCell cell)
        {
            cell.RegisterCallback<ChangeEvent<T>>(OnValueChanged);
            m_StopListening = _cell =>
            {
                _cell.UnregisterCallback<ChangeEvent<T>>(OnValueChanged);
            };
        }

        void RegisterValueChangeAllTypes(SearchTableViewCell cell)
        {
            // This is used as a fallback case if we cannot determine the valueType of a cell (because its value might be null)
            cell.RegisterCallback<ChangeEvent<SerializedProperty>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<bool>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<int>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<float>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<double>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<string>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<uint>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Color>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<UnityEngine.Object>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Enum>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Vector2>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Vector4>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Rect>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<AnimationCurve>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Bounds>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Gradient>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Quaternion>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Vector2Int>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Vector3Int>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<RectInt>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<BoundsInt>>(OnValueChanged);
            cell.RegisterCallback<ChangeEvent<Hash128>>(OnValueChanged);

            m_StopListening = UnregisterValueChangeAllTypes;
        }

        void RegisterValueChange(SearchTableViewCell cell)
        {
            cell.isEditingCell = true;

            if (cell.searchColumn.drawer != null)
                return;

            var currentValue = cell.GetValue();
            if (currentValue != null)
            {
                if (!RegisterValueChange(cell, currentValue.GetType(), currentValue))
                    RegisterValueChangeAllTypes(cell);
            }
            else
            {
                RegisterValueChangeAllTypes(cell);
            }
        }

        private SearchTableViewCell FindCell(VisualElement target)
        {
            return target.GetFirstAncestorOfType<SearchTableViewCell>();
        }

        private void OnValueChanged<ValueType>(ChangeEvent<ValueType> evt)
        {
            // IMPORTANT NOTE: OnValueChanged triggers at any time the model changes: undo/redo, user interaction, properties modifed through scripting.
            // In our cases we want to react to ValueChanged ONLY when a cell is being interacted by a user (to provide multi edit)
            
            if (m_LastEventIsUndo)
            {
                // ValueChanged trigger through undo: refuse it.
                m_LastEventIsUndo = false;
                return;
            }

            // If no editedCell: user has NOT interacted with the cell. Do not call SetValue (or else it could trigger multi-edit).
            if (m_EditedCell != null && m_EditedCell.SetValue(evt.newValue))
            {
                evt.StopPropagation();
                cellValueChanged?.Invoke(this, m_EditedCell, evt.newValue);
            }
        }

        void UnregisterValueChangeAllTypes(SearchTableViewCell cell)
        {
            cell.UnregisterCallback<ChangeEvent<SerializedProperty>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<bool>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<int>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<float>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<double>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<string>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<uint>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Color>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<UnityEngine.Object>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Enum>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Vector2>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Vector4>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Rect>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<AnimationCurve>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Bounds>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Gradient>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Quaternion>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Vector2Int>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Vector3Int>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<RectInt>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<BoundsInt>>(OnValueChanged);
            cell.UnregisterCallback<ChangeEvent<Hash128>>(OnValueChanged);
        }

        void UnregisterValueChange()
        {
            if (m_EditedCell == null)
                return;

            m_EditedCell.isEditingCell = false;
            m_StopListening?.Invoke(m_EditedCell);
            m_StopListening = null;
        }
        #endregion

        protected override void OnGroupChanged(string prevGroupId, string newGroupId)
        {
            OnSortColumn();
            base.OnGroupChanged(prevGroupId, newGroupId);
        }

        ItemCellDescriptor GetCellDescriptor(SearchTableViewCell cell)
        {
            if (cell == null)
                return default;
            var item = cell.GetItem();
            if (item == null || cell.rowIndex == -1)
                return default;
            var column = cell.searchColumn;
            if (column == null)
                return default;
            return new ItemCellDescriptor
            {
                itemId = item.id,
                itemProvider = item.provider.id,
                columnId = column.GetHashCode()
            };
        }

        static ItemCellDescriptor k_InvalidDescriptor = new();

        SearchTableViewCell GetCell(ItemCellDescriptor descriptor)
        {
            if (!descriptor.valid)
                return null;
            var cell = m_ListView.Q<SearchTableViewCell>(descriptor.cellItemId);
            if (cell != null)
                return cell;
            return null;
        }

        internal SearchTableViewCell GetCell(Vector2Int coord)
        {
            if (coord.x == -1 || coord.y == -1)
                return null;
            var row = m_ListView.GetRootElementForIndex(coord.x);
            if (row == null || coord.y >= row.childCount)
                return null;
            var columnIndex = coord.y;
            foreach (var rowItem in row.Children())
            {
                if (columnIndex == 0)
                {
                    var cell = rowItem.Q<SearchTableViewCell>();
                    return cell;
                }
                columnIndex--;
            }
            return null;
        }
        
        protected override void UpdateView()
        {
            m_LastEventIsUndo = false;
            if (UpdateNeeded == false)
                return;

            if (m_EditedCellDescriptor.valid)
            {
                base.UpdateView();

                // Trying to restore edited cell that fails previously because Search wasn't completed.

                var tryEditedCell = GetCell(m_EditedCellDescriptor);
                if (tryEditedCell != null)
                {
                    SetEditedCell(tryEditedCell);
                    m_EditedCellDescriptor = k_InvalidDescriptor;
                }
                else if (!m_ViewModel.context.searchInProgress)
                {
                    // Bailing out. Search is complete and we cannot reset the edited cell.
                    m_EditedCellDescriptor = k_InvalidDescriptor;
                }
            }
            else
            {
                m_EditedCellDescriptor = k_InvalidDescriptor;

                if (m_EditedCell != null)
                {
                    // Since the cells will all be re bound and the editedCell might be recycled:
                    // Remove edited Cell
                    // Keep its "coord"
                    // Try to restore the edited cell after refresh.

                    var editedCellDescriptor = GetCellDescriptor(m_EditedCell);
                    SetEditedCell(null);
                    base.UpdateView();
                    var tryEditedCell = GetCell(editedCellDescriptor);
                    if (tryEditedCell != null)
                    {
                        // Everything is working according to plan
                        SetEditedCell(tryEditedCell);
                    }
                    else if (m_ViewModel.context.searchInProgress && editedCellDescriptor.valid)
                    {
                        // We cannot find the cell at these coord BUT search is in progress. Try to postpone resetting this.
                        m_EditedCellDescriptor = editedCellDescriptor;
                    }
                    else
                    {
                        // Bailing out. Search is complete and we cannot reset the edited cell.
                        m_EditedCellDescriptor = k_InvalidDescriptor;
                    }
                }
                else
                {
                    base.UpdateView();
                }
            }
        }

        public IEnumerable<SearchItem> GetElements()
        {
            return m_ViewModel.results ?? (IEnumerable<SearchItem>)Array.Empty<SearchItem>();
        }

        float ITableView.GetRowHeight() => m_ListView.fixedItemHeight;
        IEnumerable<SearchItem> ITableView.GetRows() => m_ViewModel.results;
        SearchTable ITableView.GetSearchTable() => tableConfig;
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        void ITableView.SetSelection(IEnumerable<SearchItem> items) => m_ViewModel.SetSelection(items.Select(e => m_ViewModel.results.IndexOf(e)).Where(i => i != -1).ToArray());
#pragma warning restore UA2001
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
            if (flags.HasAny(RefreshFlags.ItemsChanged))
            {
                Refresh();
            }
            if (flags.HasAny(RefreshFlags.DisplayModeChanged))
            {
                UpdateColumns();
            }
        }

        private void UpdateColumns()
        {
            if (m_ListView != null)
                BuildColumns(viewColumns, tableConfig, clear: true);
            UpdateItemHeight();
            ExportTableConfig();
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

            using var pp = new EditorPerformanceMarker("Search.Table.SortColumns").Auto();

            // A lot of the Selectors and Table getters rely on propertyDatabase for fats access. Get a MonitorView before sorting all items.
            using var view = SearchMonitor.GetView();

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

            var visibleColumnCount = m_ListView.columns.visibleList.Count;
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
                // TODO: the currentGroup might be an invalid value and we might try to get the DefaultTableConfig from the wrong provider.
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var provider = providers.Count == 1 ? providers.FirstOrDefault() : SearchService.GetProvider(m_ViewModel.currentGroup);
#pragma warning restore UA2001
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var uniqueColumns = newColumns.Where(newColumn => Array.TrueForAll(oldColumns, c => c.selector != newColumn.selector)).ToList();
            if (uniqueColumns.Count == 0)
            {
                Debug.LogWarning($"Column already exists in table view: {string.Join(",", newColumns.Select(c => c.ToString()))}");
                return;
            }
            #pragma warning restore UA2001

            var searchColumns = new List<SearchColumn>(oldColumns);
            if (insertColumnAt == -1)
                insertColumnAt = searchColumns.Count;
            var columnCountBefore = searchColumns.Count;
            searchColumns.InsertRange(insertColumnAt, uniqueColumns);

            var columnAdded = searchColumns.Count - columnCountBefore;
            if (columnAdded > 0)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var firstColumn = uniqueColumns.First();
#pragma warning restore UA2001
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

            UpdateItemHeight();

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
                #pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (!clear && columns.Any(x => string.Equals(x.name, sc.path, StringComparison.Ordinal)))
#pragma warning restore UA2006
                    continue;

                // FIXME: Each call to Add here does a Rebuild of the table view
                columns.Add(new SearchTableViewColumn(sc, m_ViewModel, this));
            }

            UpdateItemHeight();
        }

        void UpdateItemHeight()
        {
            var fixItemHeight = tableConfig != null ? MathF.Max(k_DefaultItemHeight, tableConfig.itemHeight) : k_DefaultItemHeight;
            if (m_ListView != null && m_ListView.fixedItemHeight != fixItemHeight)
            {
                m_ListView.fixedItemHeight = fixItemHeight;
                m_ListView.Rebuild();
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
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                fields.UnionWith(e.GetFields().Where(f => f.value != null));
#pragma warning restore UA2001

            if (fields.Count > 0)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                currentTableConfig.columns = fields.Select(f => ItemSelectors.CreateColumn(f.label, f.name, options: options)).ToArray();
#pragma warning restore UA2001

            tableConfig = currentTableConfig;
            UpdateItemHeight();
        }

        internal void SetupColumns(IList<SearchField> fields)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var searchColumns = tableConfig?.columns != null ? new List<SearchColumn>(tableConfig.columns.Where(c =>
#pragma warning restore UA2001
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
                return Array.Empty<SearchColumn>();
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
