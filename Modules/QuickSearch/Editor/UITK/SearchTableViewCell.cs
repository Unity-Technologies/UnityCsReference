// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search.Providers;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    interface ISearchTableViewCellValue
    {
        void Update(object newValue);
    }

    class SearchTableViewCell : Label
    {
        public new static readonly string ussClassName = "search-table-view-cell";
        static readonly string k_HiddenClassName = ussClassName.WithUssModifier("hidden");
        static readonly string k_SimpleClassName = ussClassName.WithUssModifier("simple");
        static readonly string k_AlignLeftClassName = ussClassName.WithUssModifier("align-left");
        static readonly string k_AlignCenterClassName = ussClassName.WithUssModifier("align-center");
        static readonly string k_AlignRightClassName = ussClassName.WithUssModifier("align-right");

        private readonly SearchColumn m_SearchColumn;
        private readonly ISearchView m_ViewModel;
        private readonly ITableView m_TableView;

        private SearchItem m_BindedItem;
        private Action m_DeferredUpdateOff;
        private SearchColumnEventArgs m_ColumnInvokeArgs;
        private SearchResultViewDragHandler m_DragHandler;
        private VisualElement m_ValueElement;
        private string m_LastProvider;

        internal int rowIndex { get; set; }
        internal SearchColumn searchColumn => m_SearchColumn;

        bool m_IsEditingCell;
        internal bool isEditingCell
        {
            get
            {
                return m_IsEditingCell;
            }
            set
            {
                m_IsEditingCell = value;
                EnableInClassList("search-table-view-cell-editing", value);
            }
        }

        public SearchTableViewCell(SearchColumn column, ISearchView searchView, ITableView tableView)
        {
            m_SearchColumn = column;
            m_ViewModel = searchView;
            m_TableView = tableView;
            rowIndex = -1;

            Create();

            m_DragHandler = new SearchResultViewDragHandler(searchView, this)
            {
                CanStartDrag = CanStartDrag,
                StartDrag = StartDrag,
                GetDraggedItem = evt => m_BindedItem
            };

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            // Important Note: Assume if the cell was clicked, it is because we want to interact with it and possible perform multi-edit.
            // StopPropagation prevents the multiColumnListView from clearing the selection.
            if (!searchColumn.readOnly && evt.button == 0)
                evt.StopPropagation();
        }

        public void Create()
        {
            if (m_SearchColumn.cellCreator != null && m_SearchColumn.drawer != null)
                throw new Exception($"Search column should not define a IMGUI drawer and a UITK cell creator for {m_SearchColumn.path} ({m_SearchColumn.provider})");

            Clear();
            text = null;
            tooltip = null;
            m_ValueElement = null;

            if (m_SearchColumn.cellCreator != null)
            {
                if (m_SearchColumn.binder == null)
                    throw new NullReferenceException($"Search column binder must be defined for {m_SearchColumn.path} ({m_SearchColumn.provider})");
                m_ValueElement = m_SearchColumn.cellCreator(m_SearchColumn);
            }
            else if (m_SearchColumn.drawer != null)
            {
                m_ValueElement = new IMGUIContainer();
            }

            if (m_ValueElement != null)
                Add(m_ValueElement);
            else
            {
                AddToClassList(k_SimpleClassName);
                m_ValueElement = this;
            }

            m_LastProvider = m_SearchColumn.provider;
            m_ValueElement.AddToClassList(ussClassName);
        }

        private void OnAttachToPanel(AttachToPanelEvent attachEvent)
        {
            // the contextual click is registered on the row container to ensure that there are no gaps between cells in the click target
            var pp = parent?.parent;
            if (m_TableView.GetColumnIndex(m_SearchColumn.name) == 0)
            {
                // Ensure we only register a single context click handler.
                pp?.RegisterCallback<ContextClickEvent>(OnItemContextualClicked);
            }

            if (m_SearchColumn.setter == null)
            {
                m_DragHandler.RegisterDragCallbacks();
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            var pp = parent?.parent;
            pp?.UnregisterCallback<ContextClickEvent>(OnItemContextualClicked);

            m_DragHandler.UnregisterDragCallbacks();
        }

        public void Bind(SearchItem item, int rowIndex)
        {
            // If the cell is already bound, it means it wasn't properly cleaned up before recycling it.
            UnsetEditedCell();

            this.rowIndex = rowIndex;
            if (string.CompareOrdinal(m_LastProvider, m_SearchColumn.provider) != 0)
                Create();

            m_ColumnInvokeArgs = new SearchColumnEventArgs(item, item.context, m_SearchColumn);
            if (m_SearchColumn.getter != null)
                m_ColumnInvokeArgs.value = m_SearchColumn.getter(m_ColumnInvokeArgs);

            if (m_SearchColumn.binder != null)
            {
                m_SearchColumn.binder(m_ColumnInvokeArgs, m_ValueElement);
            }
            else
            {
                if (m_SearchColumn.drawer != null && m_ValueElement is IMGUIContainer imc)
                {
                    if (imc.onGUIHandler == null)
                    {
                        imc.onGUIHandler = () =>
                        {
                            m_ColumnInvokeArgs.rect = new Rect(0, 0, imc.worldBound.width, imc.worldBound.height);
                            using (var c = new EditorGUI.ChangeCheckScope())
                            {
                                var newValue = m_SearchColumn.drawer?.Invoke(m_ColumnInvokeArgs);
                                if (c.changed)
                                    SetValue(newValue);
                            }
                        };
                    }
                }
                else if (m_ValueElement is TextElement te)
                {
                    te.text = m_ColumnInvokeArgs.value?.ToString() ?? string.Empty;
                }
            }

            name = SearchTableView.ItemCellDescriptor.GetCellItemId(item, m_SearchColumn).ToString();

            m_BindedItem = item;
            UpdateStyles();

            m_DeferredUpdateOff?.Invoke();
            m_DeferredUpdateOff = null;
            if (item.options.HasAny(SearchItemOptions.AlwaysRefresh))
                m_DeferredUpdateOff = Utils.CallDelayed(() => Bind(m_BindedItem, rowIndex), 1d);
        }

        internal SearchItem GetItem()
        {
            return m_BindedItem;
        }

        internal string GetItemLabel()
        {
            if (m_BindedItem != null)
                return m_BindedItem.GetLabel(m_ViewModel.context);
            return string.Empty;
        }

        internal object GetValue()
        {
            return m_SearchColumn.getter(m_ColumnInvokeArgs);
        }

        internal bool TrySetValue(object newValue)
        {
            // Example of how to set a value and checking if it changed.
            var currentValue = m_SearchColumn.getter(m_ColumnInvokeArgs);
            m_ColumnInvokeArgs.value = newValue;
            m_SearchColumn.setter(m_ColumnInvokeArgs);
            var afterSetValue = m_SearchColumn.getter(m_ColumnInvokeArgs);
            return !Equals(currentValue, afterSetValue);
        }

        internal bool SetValue(object newValue)
        {
            if (m_BindedItem == null || m_SearchColumn.setter == null)
            {
                return false;
            }

            if (m_ColumnInvokeArgs.value is not SerializedProperty prop)
            {
                m_ColumnInvokeArgs.value = newValue;
                m_SearchColumn.setter(m_ColumnInvokeArgs);
            }
            else
            {
                m_SearchColumn.setter(m_ColumnInvokeArgs);
            }

            if (m_ViewModel.selection.Count <= 1)
                return true;

            // If current edited item is NOT in selection, do not modify the selection (similar to legacy IMGUI TableView)
            if (!m_ViewModel.selection.Contains(m_BindedItem))
                return true;

            foreach (var se in m_ViewModel.selection)
            {
                if (se == m_BindedItem)
                    continue;

                var multipleSetterArgs = new SearchColumnEventArgs(se, m_ViewModel.context, m_SearchColumn) { value = m_ColumnInvokeArgs.value, multiple = true };
                m_SearchColumn.setter(multipleSetterArgs);
            }

            m_TableView.SetDirty();
            return true;
        }

        private void UpdateStyles()
        {
            var options = m_SearchColumn.options;

            m_ValueElement.EnableInClassList(k_HiddenClassName, options.HasAny(SearchColumnFlags.Hidden));
            m_ValueElement.EnableInClassList(k_AlignLeftClassName, options.HasAny(SearchColumnFlags.TextAlignmentLeft));
            m_ValueElement.EnableInClassList(k_AlignCenterClassName, options.HasAny(SearchColumnFlags.TextAlignmentCenter));
            m_ValueElement.EnableInClassList(k_AlignRightClassName, options.HasAny(SearchColumnFlags.TextAlignmentRight));
        }

        void UnsetEditedCell()
        {
            if (isEditingCell)
            {
                ((SearchTableView)m_TableView).SetEditedCell(null);
            }
        }

        public void Unbind()
        {
            UnsetEditedCell();
            name = "";

            rowIndex = -1;
            m_DragHandler.ResetDrag();
            m_BindedItem = null;
            m_DeferredUpdateOff?.Invoke();
            m_DeferredUpdateOff = null;
        }

        private void OnItemContextualClicked(ContextClickEvent evt)
        {
            if (m_BindedItem == null)
                return;

            m_ViewModel.ShowItemContextualMenu(m_BindedItem, new Rect(evt.mousePosition, worldBound.size));
        }

        bool CanStartDrag(PointerDownEvent evt)
        {
            var cell = evt.target as SearchTableViewCell;
            if (cell?.m_ValueElement is TextElement textElement && textElement.selection.isSelectable)
                return false;
            return m_BindedItem != null && m_BindedItem.provider.startDrag != null;
        }

        void StartDrag(SearchItem _)
        {
            DragAndDrop.PrepareStartDrag();
            m_BindedItem.provider.startDrag(m_BindedItem, m_ViewModel.context);
        }

        public override string ToString()
        {
            if (m_BindedItem == null)
            {
                return $"null - {m_SearchColumn}";
            }
            return $"{m_BindedItem.id} - {m_SearchColumn}";
        }
    }
}
