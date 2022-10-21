// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        private bool m_InitiateDrag = false;
        private Vector3 m_InitiateDragPosition;
        private VisualElement m_ValueElement;
        private string m_LastProvider;

        public SearchTableViewCell(SearchColumn column, ISearchView searchView, ITableView tableView)
        {
            m_SearchColumn = column;
            m_ViewModel = searchView;
            m_TableView = tableView;

            Create();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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
            pp?.RegisterCallback<ContextClickEvent>(OnItemContextualClicked);

            if (m_SearchColumn.setter == null)
            {
                RegisterCallback<ClickEvent>(OnDoubleClick);
                RegisterCallback<PointerDownEvent>(OnItemPointerDown);
                RegisterCallback<PointerUpEvent>(OnItemPointerUp);
                RegisterCallback<DragExitedEvent>(OnDragExited);
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent detachEvent)
        {
            var pp = parent?.parent;
            pp?.UnregisterCallback<ContextClickEvent>(OnItemContextualClicked);

            UnregisterCallback<ClickEvent>(OnDoubleClick);
            UnregisterCallback<PointerDownEvent>(OnItemPointerDown);
            UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            UnregisterCallback<PointerUpEvent>(OnItemPointerUp);
            UnregisterCallback<DragExitedEvent>(OnDragExited);
        }

        public void Bind(SearchItem item)
        {
            UnbindEvents();

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

            m_BindedItem = item;
            UpdateStyles();
            BindEvents();

            m_DeferredUpdateOff?.Invoke();
            m_DeferredUpdateOff = null;
            if (item.options.HasAny(SearchItemOptions.AlwaysRefresh))
                m_DeferredUpdateOff = Utils.CallDelayed(() => Bind(m_BindedItem), 1d);
        }

        private void BindEvents()
        {
            if (m_ValueElement is not IBindable bindable || bindable is not VisualElement be)
                return;

            be.RegisterCallback<ChangeEvent<SerializedProperty>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<bool>>(OnValueChanged); // checked
            be.RegisterCallback<ChangeEvent<int>>(OnValueChanged); // checked
            be.RegisterCallback<ChangeEvent<float>>(OnValueChanged); // checked
            be.RegisterCallback<ChangeEvent<double>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<string>>(OnValueChanged); // checked
            be.RegisterCallback<ChangeEvent<Color>>(OnValueChanged); // checked
            be.RegisterCallback<ChangeEvent<UnityEngine.Object>>(OnValueChanged); // checked
            be.RegisterCallback<ChangeEvent<Enum>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Vector2>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged); // checked
            be.RegisterCallback<ChangeEvent<Vector4>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Rect>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<AnimationCurve>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Bounds>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Gradient>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Quaternion>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Vector2Int>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Vector3Int>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Vector3Int>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<RectInt>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<BoundsInt>>(OnValueChanged);
            be.RegisterCallback<ChangeEvent<Hash128>>(OnValueChanged);
        }

        private void OnValueChanged(IChangeEvent evt)
        {
            if (m_BindedItem == null || evt == null || m_SearchColumn.setter == null)
                return;

            var getValueProperty = evt.GetType().GetProperty("newValue");
            if (getValueProperty == null)
                throw new Exception($"Cannot fetch value for {m_BindedItem} using {m_SearchColumn}");
            var value = getValueProperty.GetValue(evt);
            Debug.LogWarning($"{m_SearchColumn.path} > {m_BindedItem} > {m_ColumnInvokeArgs.value} > {value} >  {value?.GetType()}");

            if (SetValue(value) && evt is EventBase eb)
            {
                eb.PreventDefault();
                eb.StopPropagation();
            }
        }

        private bool SetValue(object newValue)
        {
            if (m_ColumnInvokeArgs.value is not SerializedProperty prop)
                m_ColumnInvokeArgs.value = newValue;
            m_SearchColumn.setter(m_ColumnInvokeArgs);

            if (m_ViewModel.selection.Count <= 1)
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

        private void UnbindEvents()
        {
            m_ValueElement.UnregisterCallback<ChangeEvent<bool>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<SerializedProperty>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<int>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<float>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<double>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<string>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Color>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<UnityEngine.Object>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Enum>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Vector2>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Vector4>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Rect>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<AnimationCurve>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Bounds>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Gradient>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Quaternion>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Vector2Int>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Vector3Int>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Vector3Int>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<RectInt>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<BoundsInt>>(OnValueChanged);
            m_ValueElement.UnregisterCallback<ChangeEvent<Hash128>>(OnValueChanged);
        }

        private void UpdateStyles()
        {
            var options = m_SearchColumn.options;

            m_ValueElement.EnableInClassList(k_HiddenClassName, options.HasAny(SearchColumnFlags.Hidden));
            m_ValueElement.EnableInClassList(k_AlignLeftClassName, options.HasAny(SearchColumnFlags.TextAlignmentLeft));
            m_ValueElement.EnableInClassList(k_AlignCenterClassName, options.HasAny(SearchColumnFlags.TextAlignmentCenter));
            m_ValueElement.EnableInClassList(k_AlignRightClassName, options.HasAny(SearchColumnFlags.TextAlignmentRight));
        }

        public void Unbind()
        {
            ResetDrag();
            UnbindEvents();
            m_BindedItem = null;
            m_DeferredUpdateOff?.Invoke();
            m_DeferredUpdateOff = null;
        }

        private void OnDoubleClick(ClickEvent evt)
        {
            if (evt.clickCount != 2)
                return;

            m_ViewModel.ExecuteAction(null, new[] { m_BindedItem }, endSearch: false);
        }

        private void OnItemContextualClicked(ContextClickEvent evt)
        {
            if (m_BindedItem == null)
                return;

            m_ViewModel.ShowItemContextualMenu(m_BindedItem, worldBound);
        }

        private void OnItemPointerDown(PointerDownEvent evt)
        {
            // dragging is initiated only by left mouse clicks
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            var cell = evt.target as SearchTableViewCell;
            if (cell?.m_ValueElement is TextElement textElement && textElement.selection.isSelectable)
                return;

            m_InitiateDrag = m_BindedItem.provider.startDrag != null;
            m_InitiateDragPosition = evt.localPosition;

            UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            RegisterCallback<PointerMoveEvent>(OnItemPointerMove);

            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
            RegisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
        }

        void OnItemPointerLeave(PointerLeaveEvent evt)
        {
            // If we enter here, it means the mouse left the element before any mouse
            // move, so the item jumped around to be repositioned in the window.
            // This will cause an issue with drag and drop
            m_InitiateDrag = false;

            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
        }

        private void OnItemPointerMove(PointerMoveEvent evt)
        {
            if (!m_InitiateDrag)
                return;

            if ((evt.localPosition - m_InitiateDragPosition).sqrMagnitude < 5f)
                return;

            UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);

            DragAndDrop.PrepareStartDrag();
            m_BindedItem.provider.startDrag(m_BindedItem, m_ViewModel.context);
            m_InitiateDrag = false;
        }

        private void OnDragExited(DragExitedEvent evt) => ResetDrag();
        private void OnItemPointerUp(PointerUpEvent evt) => ResetDrag();

        private void ResetDrag()
        {
            m_InitiateDrag = false;
            UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
        }
    }
}
