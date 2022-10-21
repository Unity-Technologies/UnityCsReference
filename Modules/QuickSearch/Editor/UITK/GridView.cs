// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Search.GridView.KeyboardGridNavigationManipulator;

namespace UnityEditor.Search
{
    internal class GridView : VisualElement
    {
        private enum ScrollingDirection
        {
            None = 0,
            Up,
            Down
        };

        private Func<VisualElement> m_MakeItem;
        private Action<VisualElement, int> m_BindItem;

        private bool m_IsRangeSelectionDirectionUp;

        private KeyboardGridNavigationManipulator m_NavigationManipulator;

        private int m_RowCount = 0;
        private int m_ColumnCount = 0;
        private int m_FirstVisibleRowIndex = 0;
        private int m_VisibleItemCount = 0;
        private int m_RangeSelectionOrigin = -1;
        private const int k_ExtraRows = 2;

        private float m_FixedItemHeight;
        private float m_FixedItemWidth;
        private float m_MaximumScrollViewHeight;
        private const float k_DefaultItemSize = 30f;
        private IList m_ItemsSource;

        private List<ReusableGridViewRow> m_RowPool;
        private List<int> m_ItemsSourceIds;
        private ScrollView m_ScrollView;

        private const string k_GridViewStyleClassName = "grid-view";
        private const string k_GridViewItemsScrollViewStyleClassName = "grid-view-rows";

        private Vector2 m_ScrollOffset = Vector2.zero;
        private Vector3 m_TouchDownPosition;

        private readonly List<int> m_SelectedIndices = new List<int>();
        private readonly List<int> m_SelectedIds = new List<int>();
        private readonly List<object> m_SelectedItems = new List<object>();
        private SelectionType m_SelectionType;

        public event Action<IEnumerable<object>> itemsChosen;
        public event Action<IEnumerable<object>> selectionChanged;
        public event Action<IEnumerable<int>> selectedIndicesChanged;
        public event Action itemsBuilt;

        public Action<VisualElement, int> unbindItem { get; set; }

        public Action<VisualElement> destroyItem { get; set; }

        public Func<VisualElement> makeItem
        {
            get { return m_MakeItem; }
            set
            {
                if (m_MakeItem == value)
                    return;
                m_MakeItem = value;
                Rebuild();
            }
        }

        public Action<VisualElement, int> bindItem
        {
            get { return m_BindItem; }
            set
            {
                if (m_BindItem == value)
                    return;
                m_BindItem = value;
                RefreshItems();
            }
        }

        public float fixedItemHeight
        {
            get => m_FixedItemHeight;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(fixedItemHeight), L10n.Tr("Value needs to be positive for virtualization."));

                if (Math.Abs(m_FixedItemHeight - value) > float.Epsilon)
                {
                    m_FixedItemHeight = value;
                    RefreshItems();
                }
            }
        }

        public float fixedItemWidth
        {
            get => m_FixedItemWidth;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(fixedItemWidth), L10n.Tr("Value needs to be positive for virtualization."));

                if (Math.Abs(m_FixedItemWidth - value) > float.Epsilon)
                {
                    m_FixedItemWidth = value;
                    RefreshItems();
                }
            }
        }

        public int selectedIndex
        {
            get { return m_SelectedIndices.Count == 0 ? -1 : m_SelectedIndices.First(); }
            set { SetSelection(value); }
        }

        public IEnumerable<int> selectedIndices => m_SelectedIndices;

        public object selectedItem => m_SelectedItems.Count == 0 ? null : m_SelectedItems.First();

        public IEnumerable<object> selectedItems => m_SelectedItems;

        public IEnumerable<int> selectedIds => m_SelectedIds;

        public List<ReusableGridViewItem> activeItems => GetActiveItems();

        public int visibleItemCount => m_VisibleItemCount;

        public int firstVisibleIndex => m_FirstVisibleRowIndex * m_ColumnCount;

        public int lastVisibleIndex => m_FirstVisibleRowIndex * m_ColumnCount + (m_VisibleItemCount - 1);

        public SelectionType selectionType
        {
            get { return m_SelectionType; }
            set
            {
                m_SelectionType = value;
                if (m_SelectionType == SelectionType.None)
                {
                    ClearSelection();
                }
                else if (m_SelectionType == SelectionType.Single)
                {
                    if (m_SelectedIndices.Count > 1)
                    {
                        SetSelection(m_SelectedIndices.First());
                    }
                }
            }
        }

        public IList itemsSource
        {
            get { return m_ItemsSource; }
            set
            {
                if (m_ItemsSource is INotifyCollectionChanged oldCollection)
                    oldCollection.CollectionChanged -= OnItemsSourceCollectionChanged;

                m_ItemsSource = value;
                if (m_ItemsSource is INotifyCollectionChanged newCollection)
                    newCollection.CollectionChanged += OnItemsSourceCollectionChanged;

                RefreshItems();
            }
        }

        public GridView(IList itemsSource, float itemFixedWidth, float itemFixedHeight,
            Func<VisualElement> makeItem = null, Action<VisualElement, int> bindItem = null)
        {
            if (itemFixedWidth < 0)
                throw new ArgumentOutOfRangeException(nameof(fixedItemWidth), L10n.Tr("Value needs to be positive for virtualization."));

            if (itemFixedHeight < 0)
                throw new ArgumentOutOfRangeException(nameof(itemFixedHeight), L10n.Tr("Value needs to be positive for virtualization."));

            if (itemFixedWidth == 0)
                itemFixedWidth = k_DefaultItemSize;

            if (itemFixedHeight == 0)
                itemFixedHeight = k_DefaultItemSize;

            m_ItemsSource = itemsSource;
            m_FixedItemHeight = itemFixedHeight;
            m_FixedItemWidth = itemFixedWidth;
            m_BindItem = bindItem;
            m_MakeItem = makeItem;

            AddToClassList(k_GridViewStyleClassName);
            RegisterCallback<GeometryChangedEvent>(OnSizeChanged);

            m_ScrollView = new ScrollView();
            m_ScrollView.AddToClassList(k_GridViewItemsScrollViewStyleClassName);
            m_ScrollView.verticalScroller.valueChanged += offset => OnScroll(new Vector2(0, offset));

            m_ScrollView.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_ScrollView.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            hierarchy.Add(m_ScrollView);

            m_ScrollView.contentContainer.focusable = true;
            m_ScrollView.contentContainer.usageHints &= ~UsageHints.GroupTransform;
            m_ScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            focusable = true;
            isCompositeRoot = true;
            delegatesFocus = true;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            m_ScrollView.contentContainer.AddManipulator(m_NavigationManipulator = new KeyboardGridNavigationManipulator(Apply));
            m_ScrollView.RegisterCallback<PointerDownEvent>(OnPointerDown);
            m_ScrollView.RegisterCallback<PointerUpEvent>(OnPointerUp);

            BuildItems();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_ScrollView.contentContainer.RemoveManipulator(m_NavigationManipulator);
            m_ScrollView.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_ScrollView.UnregisterCallback<PointerUpEvent>(OnPointerUp);

            ResetGridViewState();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!HasValidDataAndBindings() || m_RowPool == null)
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse)
            {
                m_TouchDownPosition = evt.position;
                return;
            }

            DoSelect(evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!HasValidDataAndBindings() || m_RowPool == null)
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse)
            {
                var delta = evt.position - m_TouchDownPosition;
                if (delta.sqrMagnitude <= ScrollView.ScrollThresholdSquared)
                    DoSelect(evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
            }
            else
            {
                var clickedIndex = GetIndexByPosition(evt.localPosition);
                var itemIndex = clickedIndex + m_FirstVisibleRowIndex * m_ColumnCount;
                if (selectionType == SelectionType.Multiple
                    && !evt.shiftKey
                    && !evt.actionKey
                    && m_SelectedIndices.Count > 1
                    && m_SelectedIndices.Contains(itemIndex))
                {
                    ProcessSingleClick(itemIndex);
                }
            }
        }

        private bool Apply(KeyboardGridNavigationOperation operation, bool shiftKey)
        {
            void HandleSelectionAndScroll(int itemIndex)
            {
                if (selectionType == SelectionType.Multiple && shiftKey && m_SelectedIndices.Count != 0)
                    DoRangeSelection(itemIndex);
                else
                    selectedIndex = itemIndex;

                ScrollToItem(itemIndex);
            }

            switch (operation)
            {
                case KeyboardGridNavigationOperation.None:
                    break;
                case KeyboardGridNavigationOperation.SelectAll:
                    SelectAll();
                    return true;
                case KeyboardGridNavigationOperation.Cancel:
                    ClearSelection();
                    return true;
                case KeyboardGridNavigationOperation.Left:
                    {
                        if (selectedIndex - 1 < 0)
                            break;

                        var newIndex = Mathf.Max(selectedIndex - 1, 0);
                        if (newIndex != selectedIndex)
                        {
                            HandleSelectionAndScroll(newIndex);
                            return true;
                        }
                    }
                    break;
                case KeyboardGridNavigationOperation.Right:
                    {
                        if (selectedIndex + 1 >= m_ItemsSource.Count)
                            break;

                        var newIndex = Mathf.Min(selectedIndex + 1, m_ItemsSource.Count);
                        if (newIndex != selectedIndex)
                        {
                            HandleSelectionAndScroll(newIndex);
                            return true;
                        }
                    }
                    break;
                case KeyboardGridNavigationOperation.Up:
                    {
                        if (selectedIndex - m_ColumnCount < 0)
                            break;

                        var newIndex = Mathf.Max(selectedIndex - m_ColumnCount, 0);
                        if (newIndex != selectedIndex)
                        {
                            HandleSelectionAndScroll(newIndex);
                            return true;
                        }
                    }
                    break;
                case KeyboardGridNavigationOperation.Down:
                    {
                        if (selectedIndex + m_ColumnCount > m_ItemsSource.Count - 1)
                            break;

                        var newIndex = Mathf.Min(selectedIndex + m_ColumnCount, m_ItemsSource.Count - 1);
                        if (newIndex != selectedIndex)
                        {
                            HandleSelectionAndScroll(newIndex);
                            return true;
                        }
                    }
                    break;
                case KeyboardGridNavigationOperation.Begin:
                    HandleSelectionAndScroll(0);
                    return true;
                case KeyboardGridNavigationOperation.End:
                    HandleSelectionAndScroll(m_ItemsSource.Count - 1);
                    return true;
                case KeyboardGridNavigationOperation.PageDown:
                    {
                        if (m_SelectedIndices.Count > 0)
                        {
                            m_RangeSelectionOrigin = m_IsRangeSelectionDirectionUp ? m_SelectedIndices.Min() : m_SelectedIndices.Max();
                            HandleSelectionAndScroll(Mathf.Min(m_ItemsSource.Count - 1, m_RangeSelectionOrigin + (m_VisibleItemCount - 1)));
                        }
                        return true;
                    }
                case KeyboardGridNavigationOperation.PageUp:
                    {
                        if (m_SelectedIndices.Count > 0)
                        {
                            m_RangeSelectionOrigin = m_IsRangeSelectionDirectionUp ? m_SelectedIndices.Min() : m_SelectedIndices.Max();
                            HandleSelectionAndScroll(Mathf.Max(0, m_RangeSelectionOrigin - (m_VisibleItemCount - 1)));
                        }
                        return true;
                    }
                case KeyboardGridNavigationOperation.Submit:
                    itemsChosen?.Invoke(m_SelectedItems);
                    ScrollToItem(selectedIndex);
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            }

            return false;
        }

        private List<ReusableGridViewItem> GetActiveItems()
        {
            if (m_RowPool == null)
                return null;

            var activeItems = new List<ReusableGridViewItem>();
            foreach (var reusableRow in m_RowPool)
            {
                var items = reusableRow.GetItems();
                if (items == null)
                    continue;

                activeItems.AddRange(items);
            }

            return activeItems;
        }

        public void ScrollToItem(int itemIndex)
        {
            if (!HasValidDataAndBindings() || m_RowPool == null)
                return;

            if (m_RowPool.Count == 0 || itemIndex < -1)
                return;

            var rowIndex = itemIndex / m_ColumnCount;

            if (itemIndex == -1)
            {
                if (m_ItemsSource.Count < m_VisibleItemCount)
                    m_ScrollView.scrollOffset = new Vector2(0, 0);
                else
                    m_ScrollView.scrollOffset = new Vector2(0, m_MaximumScrollViewHeight);
            }
            else if (itemIndex == m_ItemsSource.Count - 1) // End.
            {
                m_ScrollView.scrollOffset = new Vector2(0, m_MaximumScrollViewHeight);
            }
            else if (itemIndex == 0) // Home.
            {
                m_ScrollView.scrollOffset = new Vector2(0, 0);
            }
            else if (m_FirstVisibleRowIndex >= rowIndex) // Moving up.
            {
                m_ScrollView.scrollOffset = Vector2.up * (m_FixedItemHeight * Mathf.FloorToInt(itemIndex / (float)m_ColumnCount));
            }
            else
            {
                var visibleRowCount = Mathf.Ceil((float)m_VisibleItemCount / m_ColumnCount);
                if (rowIndex < m_FirstVisibleRowIndex + visibleRowCount - 1)
                    return;

                var itemRow = Mathf.Ceil((float)(itemIndex + 1) / m_ColumnCount);
                var yScrollOffset = m_FixedItemHeight * (itemRow - visibleRowCount + 1);

                m_ScrollView.scrollOffset = new Vector2(m_ScrollView.scrollOffset.x, yScrollOffset);
            }

            m_ScrollOffset = m_ScrollView.scrollOffset;
        }

        internal void Apply(KeyboardGridNavigationOperation operation, EventBase sourceEvent)
        {
            var shiftKey = sourceEvent is KeyDownEvent kde && kde.shiftKey ||
                           sourceEvent is INavigationEvent ne && ne.shiftKey;
            if (Apply(operation, shiftKey))
            {
                sourceEvent?.StopPropagation();
                sourceEvent?.PreventDefault();
            }
        }

        private bool HasValidDataAndBindings()
        {
            return m_ItemsSource != null && m_MakeItem != null && m_BindItem != null;
        }

        private void NotifyOfSelectionChange()
        {
            if (!HasValidDataAndBindings() || m_RowPool == null)
                return;

            selectionChanged?.Invoke(m_SelectedItems);
            selectedIndicesChanged?.Invoke(m_SelectedIndices);
        }

        private void DoRangeSelection(int rangeSelectionFinalIndex)
        {
            m_RangeSelectionOrigin = m_IsRangeSelectionDirectionUp ? m_SelectedIndices.Max() : m_SelectedIndices.Min();
            ClearSelectionWithoutValidation();

            var range = new List<int>();
            m_IsRangeSelectionDirectionUp = rangeSelectionFinalIndex < m_RangeSelectionOrigin;
            if (m_IsRangeSelectionDirectionUp)
            {
                for (var i = rangeSelectionFinalIndex; i <= m_RangeSelectionOrigin; i++)
                    range.Add(i);
            }
            else
            {
                for (var i = rangeSelectionFinalIndex; i >= m_RangeSelectionOrigin; i--)
                    range.Add(i);
            }

            AddToSelection(range);
        }

        public void AddToSelection(int index)
        {
            AddToSelection(new[] { index });
        }

        public void AddToSelection(IList<int> indexes)
        {
            if (!HasValidDataAndBindings() || m_RowPool == null || indexes == null || indexes.Count == 0)
                return;

            foreach (var index in indexes)
                AddToSelectionWithoutValidation(index);

            NotifyOfSelectionChange();
        }

        public void RemoveFromSelection(int index)
        {
            if (!HasValidDataAndBindings() || m_RowPool == null)
                return;

            RemoveFromSelectionWithoutValidation(index);
            NotifyOfSelectionChange();
        }

        public void ClearSelection()
        {
            ClearSelectionWithoutNotify();
            NotifyOfSelectionChange();
        }

        private void ClearSelectionWithoutValidation()
        {
            foreach (var reusableItem in activeItems)
                reusableItem.SetSelected(false);

            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();
            m_SelectedIds.Clear();
        }

        public void ClearSelectionWithoutNotify()
        {
            if (!HasValidDataAndBindings() || m_RowPool == null || m_SelectedIds.Count == 0)
                return;

            ClearSelectionWithoutValidation();
        }

        public void SetSelection(int itemIndex)
        {
            if (itemIndex < 0 || m_ItemsSource == null || itemIndex >= m_ItemsSource.Count)
            {
                ClearSelection();
                return;
            }

            SetSelection(new[] { itemIndex });
        }

        public void SetSelection(IEnumerable<int> indices)
        {
            switch (selectionType)
            {
                case SelectionType.None:
                    return;
                case SelectionType.Single:
                    if (indices != null)
                        indices = new[] { indices.Last() };
                    break;
                case SelectionType.Multiple:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            SetSelectionInternal(indices, true);
        }

        public void SetSelectionWithoutNotify(IEnumerable<int> indices)
        {
            SetSelectionInternal(indices, false);
        }

        internal void SetSelectionInternal(IEnumerable<int> indices, bool sendNotification)
        {
            if (!HasValidDataAndBindings() || m_RowPool == null || indices == null)
                return;

            ClearSelectionWithoutValidation();

            foreach (var index in indices)
                AddToSelectionWithoutValidation(index);

            if (sendNotification)
                NotifyOfSelectionChange();
        }

        private void SelectAll()
        {
            if (!HasValidDataAndBindings() || m_RowPool == null)
                return;

            if (selectionType != SelectionType.Multiple)
                return;

            for (var itemIndex = 0; itemIndex < m_ItemsSource.Count; itemIndex++)
            {
                var item = m_ItemsSource[itemIndex];
                var id = item.GetHashCode();
                if (!m_SelectedIds.Contains(id))
                {
                    m_SelectedIndices.Add(itemIndex);
                    m_SelectedItems.Add(item);
                    m_SelectedIds.Add(id);
                }
            }

            foreach (var reusableItem in activeItems)
                reusableItem.SetSelected(true);

            NotifyOfSelectionChange();
        }

        private void AddToSelectionWithoutValidation(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= m_ItemsSource.Count || m_SelectedIndices.Contains(itemIndex))
                return;

            var item = m_ItemsSource[itemIndex];
            m_SelectedIndices.Add(itemIndex);
            m_SelectedItems.Add(item);
            m_SelectedIds.Add(item.GetHashCode());

            var elementIndex = itemIndex - m_FirstVisibleRowIndex * m_ColumnCount;
            if (elementIndex >= activeItems.Count || elementIndex < 0)
                return;

            var reusableItem = activeItems[elementIndex];
            reusableItem.SetSelected(true);
        }

        private void RemoveFromSelectionWithoutValidation(int itemIndex)
        {
            if (!m_SelectedIndices.Contains(itemIndex))
                return;

            var item = m_ItemsSource[itemIndex];
            m_SelectedIndices.Remove(itemIndex);
            m_SelectedItems.Remove(item);
            m_SelectedIds.Remove(item.GetHashCode());

            var elementIndex = itemIndex - m_FirstVisibleRowIndex * m_ColumnCount;
            if (elementIndex >= activeItems.Count || elementIndex < 0)
                return;

            var reusableItem = activeItems[elementIndex];
            reusableItem.SetSelected(false);
        }

        private void DoSelect(Vector2 localPosition, int clickCount, bool actionKey, bool shiftKey)
        {
            var clickedIndex = GetIndexByPosition(localPosition);
            var itemIndex = clickedIndex + m_FirstVisibleRowIndex * m_ColumnCount;

            if (itemIndex > m_ItemsSource.Count - 1 || clickedIndex > m_ItemsSource.Count - 1)
                return;

            switch (clickCount)
            {
                case 1:
                    DoSelectOnSingleClick(itemIndex, actionKey, shiftKey);
                    break;
                case 2:
                    {
                        if (itemsChosen != null)
                            ProcessSingleClick(itemIndex);

                        itemsChosen?.Invoke(m_SelectedItems);
                    }
                    break;
                default:
                    break;
            }
        }

        private void DoSelectOnSingleClick(int itemIndex, bool actionKey, bool shiftKey)
        {
            if (selectionType == SelectionType.None)
                return;

            if (selectionType == SelectionType.Multiple && actionKey)
            {
                m_RangeSelectionOrigin = itemIndex;

                // Add/remove single clicked element
                var id = m_ItemsSourceIds[itemIndex];
                if (m_SelectedIds.Contains(id))
                    RemoveFromSelection(itemIndex);
                else
                    AddToSelection(itemIndex);
            }
            else if (selectionType == SelectionType.Multiple && shiftKey)
            {
                if (m_RangeSelectionOrigin == -1 || !selectedItems.Any())
                {
                    m_RangeSelectionOrigin = itemIndex;
                    SetSelection(itemIndex);
                }
                else
                {
                    DoRangeSelection(itemIndex);
                }
            }
            else if (selectionType == SelectionType.Multiple && m_SelectedIndices.Contains(itemIndex))
            {
                // Do noting, selection will be processed OnPointerUp.
            }
            else // single
            {
                m_RangeSelectionOrigin = itemIndex;
                SetSelection(itemIndex);
            }
        }

        private void ProcessSingleClick(int itemIndex)
        {
            m_RangeSelectionOrigin = itemIndex;
            SetSelection(itemIndex);
        }

        internal int GetIndexByPosition(Vector2 localPosition)
        {
            var resolvedRowWidth = m_ScrollView.contentContainer.resolvedStyle.width;
            var calculatedRowWidth = m_ColumnCount * m_FixedItemWidth;
            var delta = resolvedRowWidth - calculatedRowWidth;
            var extraElementPadding = Mathf.Ceil(delta / (m_ColumnCount - 1));

            var offset = m_ScrollOffset.y - Mathf.FloorToInt(m_ScrollOffset.y / m_FixedItemHeight) * m_FixedItemHeight;

            if (offset == 0)
            {
                var index = Mathf.FloorToInt(localPosition.y / m_FixedItemHeight) * m_ColumnCount + Mathf.FloorToInt(localPosition.x / (m_FixedItemWidth + extraElementPadding));
                if (index >= m_ItemsSource.Count)
                    index = -1;

                return index;
            }

            var visibleOffset = m_FixedItemHeight - offset;
            var visibleRowCount = m_VisibleItemCount / m_ColumnCount;

            var lowerBound = 0f;
            for (int i = 0; i <= visibleRowCount; i++)
            {
                var upperBound = visibleOffset + i * m_FixedItemHeight;
                if (localPosition.y >= lowerBound && localPosition.y < upperBound)
                    return i * m_ColumnCount + Mathf.FloorToInt(localPosition.x / (m_FixedItemWidth + extraElementPadding));
                else
                    lowerBound = upperBound;
            }

            return -1;
        }

        private void OnScroll(Vector2 offset)
        {
            var newFirstVisibleRowIndex = (int)(offset.y / m_FixedItemHeight);
            m_ScrollOffset.y = offset.y;

            m_ScrollView.contentContainer.style.paddingTop = newFirstVisibleRowIndex * m_FixedItemHeight;
            m_ScrollView.contentContainer.style.height = m_MaximumScrollViewHeight;

            if (m_FirstVisibleRowIndex == newFirstVisibleRowIndex)
                return;

            var delta = Math.Abs(newFirstVisibleRowIndex - m_FirstVisibleRowIndex);
            if (delta >= m_RowCount)
            {
                RebindActiveItems(newFirstVisibleRowIndex);
            }
            else if (m_FirstVisibleRowIndex > newFirstVisibleRowIndex)
            {
                for (int i = m_FirstVisibleRowIndex - 1; i >= newFirstVisibleRowIndex; i--)
                    OnScrollBindItems(ScrollingDirection.Up);
            }
            else if (m_FirstVisibleRowIndex < newFirstVisibleRowIndex)
            {
                for (int i = m_FirstVisibleRowIndex + 1; i <= newFirstVisibleRowIndex; i++)
                    OnScrollBindItems(ScrollingDirection.Down);
            }

            m_FirstVisibleRowIndex = newFirstVisibleRowIndex;
        }

        private void RebindActiveItems(int firstVisibleItemIndex)
        {
            var itemIndex = firstVisibleItemIndex * m_ColumnCount;
            foreach (var reusableItem in activeItems)
            {
                if (reusableItem.index < m_ItemsSource.Count && reusableItem.index != ReusableGridViewItem.UndefinedIndex)
                    UnbindItem(reusableItem, reusableItem.index);

                if (itemIndex >= m_ItemsSource.Count)
                {
                    reusableItem.bindableElement.style.visibility = Visibility.Hidden;
                }
                else
                {
                    BindItem(reusableItem, itemIndex, m_ItemsSourceIds[itemIndex]);
                    itemIndex++;
                }
            }
        }

        private void OnScrollBindItems(ScrollingDirection scrollingDirection)
        {
            switch (scrollingDirection)
            {
                case ScrollingDirection.None:
                    break;
                case ScrollingDirection.Down:
                    ScrollingDown();
                    break;
                case ScrollingDirection.Up:
                    ScrollingUp();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void ScrollingDown()
        {
            var nextElementIndexToBind = m_RowPool.Last().GetLastItemInRow().index + 1;
            var row = m_RowPool.First();
            for (int i = 0; i < m_ColumnCount; i++)
            {
                var reusableItem = row.GetFirstItemInRow();
                row.RemoveItemAt(0);
                UnbindItem(reusableItem, reusableItem.index);

                row.AddItem(reusableItem);
                if (nextElementIndexToBind < m_ItemsSource.Count)
                {
                    BindItem(reusableItem, nextElementIndexToBind, m_ItemsSourceIds[nextElementIndexToBind]);
                    nextElementIndexToBind++;
                }
            }

            m_RowPool.RemoveAt(0);
            m_RowPool.Add(row);
            row.bindableElement.BringToFront();
            row.SetRowVisibility();
        }

        private void ScrollingUp()
        {
            var itemIndex = m_RowPool.First().GetFirstItemInRow().index - 1;
            var row = m_RowPool.Last();
            for (int i = 0; i < m_ColumnCount; i++)
            {
                var reusableItem = row.GetLastItemInRow();
                row.RemoveItemAt(row.bindableElement.childCount - 1);

                if (reusableItem.index < m_ItemsSource.Count && reusableItem.index != ReusableGridViewItem.UndefinedIndex)
                    UnbindItem(reusableItem, reusableItem.index);

                row.InsertItemAt(0, reusableItem);
                BindItem(reusableItem, itemIndex, m_ItemsSourceIds[itemIndex]);

                itemIndex--;
            }

            m_RowPool.RemoveAt(m_RowPool.Count - 1);
            m_RowPool.Insert(0, row);
            row.bindableElement.SendToBack();
            row.bindableElement.style.display = DisplayStyle.Flex;
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            RefreshItems();
        }

        public void RefreshItems()
        {
            if (!HasValidDataAndBindings() || m_RowPool == null || m_ItemsSourceIds == null)
                return;

            m_ItemsSourceIds.Clear();
            foreach (var item in m_ItemsSource)
                m_ItemsSourceIds.Add(item.GetHashCode());

            RefreshSelection();

            var newRowCount = Mathf.CeilToInt(m_ScrollView.contentViewport.layout.height / m_FixedItemHeight) + k_ExtraRows;
            m_ColumnCount = Mathf.FloorToInt(m_ScrollView.contentViewport.layout.width / m_FixedItemWidth);
            ResizeScrollView(newRowCount);
            ResizeColumns();
            ResizeRows();

            ReplaceActiveItems();
        }

        private void RefreshSelection()
        {
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();

            if (m_SelectedIds.Count > 0)
            {
                // Add selected objects to working lists.
                for (var index = 0; index < m_ItemsSource.Count; ++index)
                {
                    if (!m_SelectedIds.Contains(m_ItemsSourceIds[index]))
                        continue;

                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(m_ItemsSource[index]);
                }

                m_SelectedIds.Clear();
                foreach (var item in m_SelectedItems)
                    m_SelectedIds.Add(item.GetHashCode());
            }
        }

        private void ReplaceActiveItems()
        {
            // Unbind and bind elements in the pool only when necessary.
            var firstVisibleItemIndex = m_FirstVisibleRowIndex * m_ColumnCount;
            var endIndex = firstVisibleItemIndex + activeItems.Count;
            var activeItemIndex = 0;
            for (int i = firstVisibleItemIndex; i < endIndex; i++)
            {
                var reusableItem = activeItems[activeItemIndex];
                activeItemIndex++;

                if (m_SelectedIds.Contains(reusableItem.id))
                    reusableItem.SetSelected(true);
                else
                    reusableItem.SetSelected(false);

                if (i >= m_ItemsSource.Count)
                {
                    if (reusableItem.id != ReusableGridViewItem.UndefinedIndex)
                        UnbindItem(reusableItem, reusableItem.index);

                    continue;
                }

                if (m_ItemsSourceIds[i] == reusableItem.id)
                    continue;

                UnbindItem(reusableItem, i);
                BindItem(reusableItem, i, m_ItemsSourceIds[i]);
            }

            // Hide empty rows that appear in the scrollview.
            foreach (var row in m_RowPool)
                row.SetRowVisibility();
        }

        private void ResizeColumns()
        {
            var previousColumnCount = m_RowPool[0].bindableElement.childCount;
            if (previousColumnCount > m_ColumnCount) // Column Shrink
            {
                var removeColumnCount = previousColumnCount - m_ColumnCount;
                foreach (var row in m_RowPool)
                {
                    row.UpdateRow(m_FixedItemWidth, m_FixedItemHeight, m_ColumnCount);
                    for (int i = 0; i < removeColumnCount; i++)
                    {
                        var lastItemInRow = row.GetLastItemInRow();
                        UnbindItem(lastItemInRow, lastItemInRow.index);
                        destroyItem?.Invoke(lastItemInRow.bindableElement);
                        row.RemoveItem(lastItemInRow.bindableElement);
                    }
                }
            }
            else if (previousColumnCount < m_ColumnCount) // Column Grow
            {
                var addColumnCount = m_ColumnCount - previousColumnCount;
                foreach (var row in m_RowPool)
                {
                    row.UpdateRow(m_FixedItemWidth, m_FixedItemHeight, m_ColumnCount);
                    for (int i = 0; i < addColumnCount; i++)
                        CreateReusableGridViewItem(row);
                }
            }
        }

        private void ResizeRows()
        {
            var previousRowCount = m_RowPool.Count;
            if (previousRowCount > m_RowCount) // Row Shrink
            {
                var removeRowCount = previousRowCount - m_RowCount;
                for (int i = 0; i < removeRowCount; i++)
                {
                    var reusableRow = m_RowPool.Last();
                    for (int j = 0; j < m_ColumnCount; j++)
                    {
                        var reusableItem = reusableRow.GetLastItemInRow();
                        UnbindItem(reusableItem, reusableItem.index);
                        destroyItem?.Invoke(reusableItem.bindableElement);
                        reusableRow.RemoveItemAt(reusableRow.bindableElement.childCount - 1);
                    }

                    m_RowPool.RemoveAt(m_RowPool.Count - 1);
                    m_ScrollView.contentContainer.RemoveAt(m_ScrollView.contentContainer.childCount - 1);
                }
            }
            else if (previousRowCount < m_RowCount) // Row Grow
            {
                var addRowCount = m_RowCount - previousRowCount;
                for (int i = 0; i < addRowCount; i++)
                {
                    var row = CreateReusableGridViewRow();
                    for (int j = 0; j < m_ColumnCount; j++)
                        CreateReusableGridViewItem(row);
                }
            }
        }

        private void ResizeScrollView(int newRowCount)
        {
            var minRowCount = Mathf.CeilToInt((float)m_ItemsSource.Count / m_ColumnCount);
            m_RowCount = Math.Min(minRowCount, newRowCount);

            m_MaximumScrollViewHeight = minRowCount * m_FixedItemHeight;
            m_ScrollView.contentContainer.style.height = m_MaximumScrollViewHeight;

            var minVisibleItemCount = Mathf.CeilToInt(m_ScrollView.contentViewport.layout.height / m_FixedItemHeight) * m_ColumnCount;
            m_VisibleItemCount = Math.Min(minVisibleItemCount, m_ItemsSource.Count);

            var scrollableHeight = Mathf.Max(0, m_MaximumScrollViewHeight - m_ScrollView.contentViewport.layout.height);
            var scrollOffset = Mathf.Min(m_ScrollOffset.y, scrollableHeight);

            m_ScrollOffset.y = scrollOffset;
            m_FirstVisibleRowIndex = (int)(scrollOffset / m_FixedItemHeight);
            m_ScrollView.verticalScroller.slider.highValue = scrollableHeight;
            m_ScrollView.verticalScroller.slider.value = scrollOffset;
            m_ScrollView.contentContainer.style.paddingTop = m_FirstVisibleRowIndex * m_FixedItemHeight;
        }

        private bool CreateReusableGridViewItem(ReusableGridViewRow row)
        {
            var element = m_MakeItem.Invoke();
            if (element == null)
                return false;

            if (m_RowCount == 1)
                element.style.flexGrow = 1f;

            row.AddItem(element);

            return true;
        }

        private ReusableGridViewRow CreateReusableGridViewRow()
        {
            var row = new ReusableGridViewRow();
            row.Init(m_FixedItemWidth, m_FixedItemHeight, m_ColumnCount);
            m_ScrollView.contentContainer.Add(row.bindableElement);
            m_RowPool.Add(row);

            return row;
        }

        private void DestroyItems()
        {
            if (m_RowPool == null)
                return;

            foreach (var reusableItem in activeItems)
            {
                UnbindItem(reusableItem, reusableItem.index);
                destroyItem?.Invoke(reusableItem.bindableElement);
            }

            m_RowPool.Clear();
            m_RowPool = null;
        }

        private void BindItem(ReusableGridViewItem reusableItem, int itemIndex, int id)
        {
            m_BindItem?.Invoke(reusableItem.bindableElement, itemIndex);
            reusableItem.id = id;
            reusableItem.index = itemIndex;
            reusableItem.bindableElement.style.visibility = Visibility.Visible;
            reusableItem.bindableElement.style.flexGrow = 0f;

            if (m_SelectedIds.Contains(id))
                reusableItem.SetSelected(true);
        }

        private void UnbindItem(ReusableGridViewItem reusableItem, int itemIndex)
        {
            var id = reusableItem.id;
            unbindItem?.Invoke(reusableItem.bindableElement, itemIndex);
            reusableItem.id = reusableItem.index = ReusableGridViewItem.UndefinedIndex;
            reusableItem.bindableElement.style.visibility = Visibility.Hidden;

            if (m_RowCount == 1)
                reusableItem.bindableElement.style.flexGrow = 1f;

            if (m_SelectedIds.Contains(id))
                reusableItem.SetSelected(false);
        }

        public void Rebuild()
        {
            if (m_ItemsSource.Count == 0)
            {
                ResetGridViewState();
                return;
            }

            BuildItems();
        }

        private void ResetGridViewState()
        {
            m_FirstVisibleRowIndex = 0;
            m_VisibleItemCount = 0;
            m_RowCount = 0;
            m_ColumnCount = 0;
            m_RangeSelectionOrigin = -1;
            m_IsRangeSelectionDirectionUp = false;

            ClearSelectionWithoutNotify();

            DestroyItems();
            m_ScrollView.contentContainer.Clear();
        }

        private void BuildItems()
        {
            ResetGridViewState();

            var scrollViewWidth = m_ScrollView.contentViewport.resolvedStyle.width;
            var scrollViewHeight = m_ScrollView.contentViewport.resolvedStyle.height;
            BuildItems(scrollViewWidth, scrollViewHeight);
        }

        private void OnSizeChanged(GeometryChangedEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (Mathf.Approximately(evt.newRect.width, evt.oldRect.width) &&
                Mathf.Approximately(evt.newRect.height, evt.oldRect.height))
                return;

            if (m_RowPool == null)
                BuildItems();
            else
                RefreshItems();
        }

        private void BuildItems(float gridViewWidth, float gridViewHeight)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!float.IsNaN(gridViewHeight))
                m_RowCount = Mathf.CeilToInt(gridViewHeight / m_FixedItemHeight) + k_ExtraRows;

            if (!float.IsNaN(gridViewWidth))
                m_ColumnCount = Mathf.FloorToInt(gridViewWidth / m_FixedItemWidth);

            if (m_RowCount == 0 || m_ColumnCount == 0)
                return;

            ResizeScrollView(m_RowCount);

            m_ItemsSourceIds = new List<int>();
            foreach (var item in m_ItemsSource)
                m_ItemsSourceIds.Add(item.GetHashCode());

            m_RowPool = new List<ReusableGridViewRow>();
            var itemIndex = m_FirstVisibleRowIndex * m_ColumnCount;
            for (int i = 0; i < m_RowCount; i++)
            {
                var row = CreateReusableGridViewRow();
                for (int j = 0; j < m_ColumnCount; j++)
                {
                    if (!CreateReusableGridViewItem(row))
                        continue;

                    var reusableItem = row.GetLastItemInRow();
                    if (itemIndex >= m_ItemsSource.Count)
                    {
                        reusableItem.bindableElement.style.visibility = Visibility.Hidden;
                    }
                    else
                    {
                        BindItem(reusableItem, itemIndex, m_ItemsSourceIds[itemIndex]);
                        itemIndex++;
                    }
                }
            }

            OnScroll(m_ScrollOffset);
            itemsBuilt?.Invoke();
        }

        internal class ReusableGridViewItem : ReusableCollectionItem
        {
            private const string k_GridViewSelectedItemStyleClassName = "grid-view-items__selected";

            public void Init(VisualElement element, float itemWidth, float itemHeight)
            {
                base.Init(element);
                SetupItem(itemWidth, itemHeight);
            }

            public void SetupItem(float itemWidth, float itemHeight)
            {
                bindableElement.style.height = itemHeight;
                bindableElement.style.width = itemWidth;
                bindableElement.style.flexShrink = 0;
                bindableElement.style.visibility = Visibility.Hidden;
            }

            public override void SetSelected(bool selected)
            {
                if (selected)
                    bindableElement.AddToClassList(k_GridViewSelectedItemStyleClassName);
                else
                    bindableElement.RemoveFromClassList(k_GridViewSelectedItemStyleClassName);
            }
        }

        internal class ReusableGridViewRow : ReusableCollectionItem
        {
            private float m_ItemHeight;
            private float m_ItemWidth;
            private int m_MaxItemCount;
            private List<ReusableGridViewItem> m_Items;

            public void Init(float itemWidth, float itemHeight, int itemCount)
            {
                m_ItemWidth = itemWidth;
                m_ItemHeight = itemHeight;
                m_MaxItemCount = itemCount;
                m_Items = new List<ReusableGridViewItem>();
                var row = CreateRow(itemHeight);
                base.Init(row);
            }

            public void UpdateRow(float itemWidth, float itemHeight, int itemCount)
            {
                m_ItemWidth = itemWidth;
                m_ItemHeight = itemHeight;
                m_MaxItemCount = itemCount;
            }

            public VisualElement CreateRow(float itemHeight)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.flexShrink = 0;
                row.style.height = itemHeight;
                row.style.justifyContent = Justify.SpaceBetween;

                return row;
            }

            public void AddItem(ReusableGridViewItem reusableItem)
            {
                if (bindableElement.childCount > m_MaxItemCount)
                    return;

                reusableItem.Init(reusableItem.bindableElement, m_ItemWidth, m_ItemHeight);
                m_Items.Add(reusableItem);
                bindableElement.Add(reusableItem.bindableElement);
            }

            public void AddItem(VisualElement element)
            {
                if (bindableElement.childCount > m_MaxItemCount)
                    return;

                var reusableItem = new ReusableGridViewItem();
                reusableItem.Init(element, m_ItemWidth, m_ItemHeight);
                m_Items.Add(reusableItem);
                bindableElement.Add(reusableItem.bindableElement);
            }

            public void RemoveItem(VisualElement element)
            {
                if (m_Items == null)
                    return;

                foreach (var item in m_Items)
                {
                    if (item.bindableElement == element)
                    {
                        m_Items.Remove(item);
                        bindableElement.Remove(element);
                        return;
                    }
                }
            }

            public void RemoveItemAt(int indexInRow)
            {
                if (m_Items == null)
                    return;

                m_Items.RemoveAt(indexInRow);
                bindableElement.RemoveAt(indexInRow);
            }

            public void InsertItemAt(int indexInRow, ReusableGridViewItem item)
            {
                if (bindableElement.childCount > m_MaxItemCount)
                    return;

                m_Items.Insert(indexInRow, item);
                bindableElement.Insert(indexInRow, item.bindableElement);
            }

            public bool IsEmpty()
            {
                if (m_Items == null)
                    return true;

                if (m_Items.Count == 0 || ContainsUnboundItems())
                    return true;

                return false;
            }

            private bool ContainsUnboundItems()
            {
                if (m_Items == null)
                    return true;

                foreach (var item in m_Items)
                {
                    if (item.index == UndefinedIndex)
                        continue;

                    return false;
                }

                return true;
            }

            public void SetRowVisibility()
            {
                if (IsEmpty())
                    bindableElement.style.display = DisplayStyle.None;
                else
                    bindableElement.style.display = DisplayStyle.Flex;
            }

            public List<ReusableGridViewItem> GetItems()
            {
                return m_Items;
            }

            public ReusableGridViewItem GetItemAt(int indexInRow)
            {
                if (m_Items == null || m_Items.Count == 0)
                    return null;

                return m_Items[indexInRow];
            }

            public ReusableGridViewItem GetLastItemInRow()
            {
                if (m_Items == null || m_Items.Count == 0)
                    return null;

                return m_Items.Last();
            }

            public ReusableGridViewItem GetFirstItemInRow()
            {
                if (m_Items == null || m_Items.Count == 0)
                    return null;

                return m_Items.First();
            }
        }

        internal class KeyboardGridNavigationManipulator : Manipulator
        {
            public enum KeyboardGridNavigationOperation
            {
                None = 0,
                SelectAll,
                Cancel,
                Left,
                Right,
                Up,
                Down,
                Begin,
                End,
                PageUp,
                PageDown,
                Submit
            }

            readonly Action<KeyboardGridNavigationOperation, EventBase> m_Action;

            public KeyboardGridNavigationManipulator(Action<KeyboardGridNavigationOperation, EventBase> action)
            {
                m_Action = action;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
                target.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
                target.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
                target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
                target.UnregisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
                target.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
                target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }

            internal void OnKeyDown(KeyDownEvent evt)
            {
                // At the moment these actions are not mapped dynamically in the InputSystemEventSystem component.
                // When that becomes the case in the future, remove the following and use corresponding Navigation events.
                KeyboardGridNavigationOperation GetOperation()
                {
                    switch (evt.keyCode)
                    {
                        case KeyCode.A when evt.actionKey: return KeyboardGridNavigationOperation.SelectAll;
                        case KeyCode.Home: return KeyboardGridNavigationOperation.Begin;
                        case KeyCode.End: return KeyboardGridNavigationOperation.End;
                        case KeyCode.PageUp: return KeyboardGridNavigationOperation.PageUp;
                        case KeyCode.PageDown: return KeyboardGridNavigationOperation.PageDown;
                    }
                    return KeyboardGridNavigationOperation.None;
                }

                var op = GetOperation();
                if (op != KeyboardGridNavigationOperation.None)
                {
                    Invoke(op, evt);
                }
            }

            void OnNavigationSubmit(NavigationSubmitEvent evt)
            {
                Invoke(KeyboardGridNavigationOperation.Submit, evt);
            }

            void OnNavigationCancel(NavigationCancelEvent evt)
            {
                Invoke(KeyboardGridNavigationOperation.Cancel, evt);
            }

            void OnNavigationMove(NavigationMoveEvent evt)
            {
                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Up:
                        Invoke(KeyboardGridNavigationOperation.Up, evt);
                        break;
                    case NavigationMoveEvent.Direction.Down:
                        Invoke(KeyboardGridNavigationOperation.Down, evt);
                        break;
                    case NavigationMoveEvent.Direction.Left:
                        Invoke(KeyboardGridNavigationOperation.Left, evt);
                        break;
                    case NavigationMoveEvent.Direction.Right:
                        Invoke(KeyboardGridNavigationOperation.Right, evt);
                        break;
                }
            }

            void Invoke(KeyboardGridNavigationOperation operation, EventBase evt)
            {
                m_Action?.Invoke(operation, evt);
            }
        }
    }
}
