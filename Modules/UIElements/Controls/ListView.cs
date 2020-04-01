// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    public enum AlternatingRowBackground
    {
        None,
        ContentOnly,
        All
    }

    public class ListView : BindableElement, ISerializationCallbackReceiver
    {
        public new class UxmlFactory : UxmlFactory<ListView, UxmlTraits> {}

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private readonly UxmlIntAttributeDescription m_ItemHeight = new UxmlIntAttributeDescription { name = "item-height", obsoleteNames = new[] {"itemHeight"}, defaultValue = s_DefaultItemHeight };
            private readonly UxmlBoolAttributeDescription m_ShowBorder = new UxmlBoolAttributeDescription { name = "show-border", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType = new UxmlEnumAttributeDescription<SelectionType> { name = "selection-type", defaultValue = SelectionType.Single };
            private readonly UxmlEnumAttributeDescription<AlternatingRowBackground> m_ShowAlternatingRowBackgrounds = new UxmlEnumAttributeDescription<AlternatingRowBackground> { name = "show-alternating-row-backgrounds", defaultValue = AlternatingRowBackground.None };
            private readonly UxmlBoolAttributeDescription m_Reorderable = new UxmlBoolAttributeDescription { name = "reorderable", defaultValue = false };
            private readonly UxmlBoolAttributeDescription m_ShowBoundCollectionSize = new UxmlBoolAttributeDescription { name = "show-bound-collection-size", defaultValue = true };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var itemHeight = 0;
                var listView = (ListView)ve;
                listView.reorderable = m_Reorderable.GetValueFromBag(bag, cc);

                // Avoid setting itemHeight unless it's explicitly defined.
                // Setting itemHeight property will activate inline property mode.
                if (m_ItemHeight.TryGetValueFromBag(bag, cc, ref itemHeight))
                {
                    listView.itemHeight = itemHeight;
                }

                listView.showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
                listView.selectionType = m_SelectionType.GetValueFromBag(bag, cc);
                listView.showAlternatingRowBackgrounds = m_ShowAlternatingRowBackgrounds.GetValueFromBag(bag, cc);
                listView.showBoundCollectionSize = m_ShowBoundCollectionSize.GetValueFromBag(bag, cc);
            }
        }

        internal class RecycledItem
        {
            public const int kUndefinedIndex = -1;
            public VisualElement element { get; private set; }
            public int index;
            public int id;

            public RecycledItem(VisualElement element)
            {
                this.element = element;
                index = id = kUndefinedIndex;
                element.AddToClassList(itemUssClassName);
            }

            public void DetachElement()
            {
                element.RemoveFromClassList(itemUssClassName);
                element = null;
            }

            public void SetSelected(bool selected)
            {
                if (element != null)
                {
                    if (selected)
                    {
                        element.AddToClassList(itemSelectedVariantUssClassName);
                        element.pseudoStates |= PseudoStates.Checked;
                    }
                    else
                    {
                        element.RemoveFromClassList(itemSelectedVariantUssClassName);
                        element.pseudoStates &= ~PseudoStates.Checked;
                    }
                }
            }
        }

        [Obsolete("onItemChosen is obsolete, use onItemsChosen instead")]
        public event Action<object> onItemChosen;
        public event Action<IEnumerable<object>> onItemsChosen;

        [Obsolete("onSelectionChanged is obsolete, use onSelectionChange instead")]
        public event Action<List<object>> onSelectionChanged;
        public event Action<IEnumerable<object>> onSelectionChange;


        private IList m_ItemsSource;
        public IList itemsSource
        {
            get { return m_ItemsSource; }
            set
            {
                m_ItemsSource = value;
                Refresh();
            }
        }

        Func<VisualElement> m_MakeItem;
        public Func<VisualElement> makeItem
        {
            get { return m_MakeItem; }
            set
            {
                if (m_MakeItem == value)
                    return;
                m_MakeItem = value;
                Refresh();
            }
        }

        public Action<VisualElement, int> unbindItem { get; set; }

        private Action<VisualElement, int> m_BindItem;
        public Action<VisualElement, int> bindItem
        {
            get { return m_BindItem; }
            set
            {
                m_BindItem = value;
                Refresh();
            }
        }

        private Func<int, int> m_GetItemId;
        internal Func<int, int> getItemId
        {
            get { return m_GetItemId; }
            set
            {
                m_GetItemId = value;
                Refresh();
            }
        }

        public float resolvedItemHeight
        {
            get
            {
                var dpiScaling = scaledPixelsPerPoint;
                return Mathf.Round(itemHeight * dpiScaling) / dpiScaling;
            }
        }

        internal List<RecycledItem> Pool
        {
            get { return m_Pool; }
        }

        [SerializeField]
        internal int m_ItemHeight = s_DefaultItemHeight;

        [SerializeField]
        internal bool m_ItemHeightIsInline;

        public int itemHeight
        {
            get { return m_ItemHeight; }
            set
            {
                m_ItemHeightIsInline = true;
                if (m_ItemHeight != value)
                {
                    m_ItemHeight = value;
                    Refresh();
                }
            }
        }

        public bool showBorder
        {
            get { return ClassListContains(borderUssClassName); }
            set { EnableInClassList(borderUssClassName, value); }
        }

        public bool reorderable
        {
            get
            {
                var controller = m_Dragger?.dragAndDropController;
                return controller != null && controller.enableReordering;
            }
            set
            {
                if (m_Dragger?.dragAndDropController == null)
                {
                    if (value)
                        SetDragAndDropController(new ListViewReorderableDragAndDropController(this));

                    return;
                }

                var controller = m_Dragger.dragAndDropController;
                if (controller != null)
                    controller.enableReordering = value;
            }
        }


        // Persisted.
        [SerializeField]
        private float m_ScrollOffset;

        // Persisted. It's why this can't be a HashSet(). :(
        [SerializeField]
        private readonly List<int> m_SelectedIds = new List<int>();

        internal List<int> currentSelectionIds => m_SelectedIds;

        // Not persisted! Just used for fast lookups of selected indices and object references.
        // This is to avoid also having a mapping from index/object ref to index for the entire
        // items source.
        private readonly List<int> m_SelectedIndices = new List<int>();
        private readonly List<object> m_SelectedItems = new List<object>();

        private int m_RangeSelectionOrigin = -1;
        private ListViewDragger m_Dragger;

        public int selectedIndex
        {
            get { return m_SelectedIndices.Count == 0 ? -1 : m_SelectedIndices.First(); }
            set { SetSelection(value); }
        }

        public IEnumerable<int> selectedIndices => m_SelectedIndices;

        public object selectedItem => m_SelectedItems.Count == 0 ? null : m_SelectedItems.First();
        public IEnumerable<object> selectedItems => m_SelectedItems;

        public override VisualElement contentContainer => m_ScrollView.contentContainer;

        private SelectionType m_SelectionType;
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
            }
        }

        [SerializeField] private AlternatingRowBackground m_ShowAlternatingRowBackgrounds = AlternatingRowBackground.None;

        public AlternatingRowBackground showAlternatingRowBackgrounds
        {
            get { return m_ShowAlternatingRowBackgrounds; }
            set
            {
                if (m_ShowAlternatingRowBackgrounds == value)
                    return;

                m_ShowAlternatingRowBackgrounds = value;
                Refresh();
            }
        }

        public bool showBoundCollectionSize { get; set; } = true;

        internal static readonly int s_DefaultItemHeight = 30;
        internal static CustomStyleProperty<int> s_ItemHeightProperty = new CustomStyleProperty<int>("--unity-item-height");

        private int m_FirstVisibleIndex;
        private float m_LastHeight;
        private List<RecycledItem> m_Pool = new List<RecycledItem>();
        internal readonly ScrollView m_ScrollView;

        private readonly VisualElement m_EmptyRows;
        private int m_LastItemIndex;

        // we keep this list in order to minimize temporary gc allocs
        private List<RecycledItem> m_ScrollInsertionList = new List<RecycledItem>();

        private const int k_ExtraVisibleItems = 2;
        private int m_VisibleItemCount;

        public static readonly string ussClassName = "unity-list-view";
        public static readonly string borderUssClassName = ussClassName + "--with-border";
        public static readonly string itemUssClassName = ussClassName + "__item";
        public static readonly string dragHoverBarUssClassName = ussClassName + "__drag-hover-bar";
        public static readonly string itemDragHoverUssClassName = itemUssClassName + "--drag-hover";
        public static readonly string itemSelectedVariantUssClassName = itemUssClassName + "--selected";
        public static readonly string itemAlternativeBackgroundUssClassName = itemUssClassName + "--alternative-background";

        internal static readonly string s_BackgroundFillUssClassName  = ussClassName + "__background";

        public ListView()
        {
            AddToClassList(ussClassName);

            selectionType = SelectionType.Single;
            m_ScrollOffset = 0.0f;

            m_ScrollView = new ScrollView();
            m_ScrollView.viewDataKey = "list-view__scroll-view";
            m_ScrollView.StretchToParentSize();
            m_ScrollView.verticalScroller.valueChanged += OnScroll;

            RegisterCallback<GeometryChangedEvent>(OnSizeChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            m_ScrollView.contentContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_ScrollView.contentContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            hierarchy.Add(m_ScrollView);

            m_ScrollView.contentContainer.focusable = true;
            m_ScrollView.contentContainer.usageHints &= ~UsageHints.GroupTransform; // Scroll views with virtualized content shouldn't have the "view transform" optimization

            m_EmptyRows = new VisualElement();
            m_EmptyRows.AddToClassList(s_BackgroundFillUssClassName);

            focusable = true;
            isCompositeRoot = true;
            delegatesFocus = true;
        }

        public ListView(IList itemsSource, int itemHeight, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem) : this()
        {
            m_ItemsSource = itemsSource;
            m_ItemHeight = itemHeight;
            m_ItemHeightIsInline = true;

            m_MakeItem = makeItem;
            m_BindItem = bindItem;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
            {
                return;
            }

            if (evt.destinationPanel.contextType == ContextType.Editor)
            {
                m_ScrollView.contentContainer.RegisterCallback<MouseDownEvent>(OnMouseDown);
                m_ScrollView.contentContainer.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }
            else if (evt.destinationPanel.contextType == ContextType.Player)
            {
                m_ScrollView.contentContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
                m_ScrollView.contentContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
            }

            m_ScrollView.contentContainer.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
            {
                return;
            }

            if (evt.originPanel.contextType == ContextType.Editor)
            {
                m_ScrollView.contentContainer.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                m_ScrollView.contentContainer.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }
            else if (evt.originPanel.contextType == ContextType.Player)
            {
                m_ScrollView.contentContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                m_ScrollView.contentContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            }

            m_ScrollView.contentContainer.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void OnKeyDown(KeyDownEvent evt)
        {
            if (evt == null || !HasValidDataAndBindings())
                return;

            var shouldStopPropagation = true;
            var shouldScroll = true;

            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                    if (selectedIndex > 0)
                        selectedIndex = selectedIndex - 1;
                    break;
                case KeyCode.DownArrow:
                    if (selectedIndex + 1 < itemsSource.Count)
                        selectedIndex = selectedIndex + 1;
                    break;
                case KeyCode.Home:
                    selectedIndex = 0;
                    break;
                case KeyCode.End:
                    selectedIndex = itemsSource.Count - 1;
                    break;
                case KeyCode.Return:
#pragma warning disable 618
                    onItemChosen?.Invoke(m_ItemsSource[selectedIndex]);
#pragma warning restore 618
                    onItemsChosen?.Invoke(m_SelectedItems);
                    break;
                case KeyCode.PageDown:
                    selectedIndex = Math.Min(itemsSource.Count - 1, selectedIndex + (int)(m_LastHeight / resolvedItemHeight));
                    break;
                case KeyCode.PageUp:
                    selectedIndex = Math.Max(0, selectedIndex - (int)(m_LastHeight / resolvedItemHeight));
                    break;
                case KeyCode.A:
                    if (evt.actionKey)
                    {
                        SelectAll();
                        shouldScroll = false;
                    }
                    break;
                case KeyCode.Escape:
                    ClearSelection();
                    shouldScroll = false;
                    break;
                default:
                    shouldStopPropagation = false;
                    shouldScroll = false;
                    break;
            }

            if (shouldStopPropagation)
                evt.StopPropagation();

            if (shouldScroll)
            {
                ScrollToItem(selectedIndex);
            }
        }

        public void ScrollToItem(int index)
        {
            if (!HasValidDataAndBindings())
                throw new InvalidOperationException("Can't scroll without valid source, bind method, or factory method.");

            if (m_VisibleItemCount == 0 || index < -1)
                return;

            var pixelAlignedItemHeight = resolvedItemHeight;
            if (index == -1)
            {
                // Scroll to last item
                int actualCount = (int)(m_LastHeight / pixelAlignedItemHeight);
                if (itemsSource.Count < actualCount)
                    m_ScrollView.scrollOffset = new Vector2(0, 0);
                else
                    m_ScrollView.scrollOffset = new Vector2(0, itemsSource.Count * pixelAlignedItemHeight);
            }
            else if (m_FirstVisibleIndex > index)
            {
                m_ScrollView.scrollOffset = Vector2.up * pixelAlignedItemHeight * index;
            }
            else // index >= first
            {
                int actualCount = (int)(m_LastHeight / pixelAlignedItemHeight);
                if (index < m_FirstVisibleIndex + actualCount)
                    return;

                bool someItemIsPartiallyVisible = (int)(m_LastHeight - actualCount * pixelAlignedItemHeight) != 0;
                int d = index - actualCount;

                // we're scrolling down in that case
                // if the list view size is not an integer multiple of the item height
                // the selected item might be the last visible and truncated one
                // in that case, increment by one the index
                if (someItemIsPartiallyVisible)
                    d++;

                m_ScrollView.scrollOffset = Vector2.up * pixelAlignedItemHeight * d;
            }
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            DoSelect(evt.localMousePosition, evt.clickCount, evt.actionKey, evt.shiftKey);
        }

        private long m_TouchDownTime = 0;
        private Vector3 m_TouchDownPosition;

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                m_TouchDownTime = evt.timestamp;
                m_TouchDownPosition = evt.position;
                return;
            }

            DoSelect(evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                var delay = evt.timestamp - m_TouchDownTime;
                var delta = evt.position - m_TouchDownPosition;
                if (delay < 500 && delta.sqrMagnitude <= 100)
                {
                    DoSelect(evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
                }
            }
        }

        private void DoSelect(Vector2 localPosition, int clickCount, bool actionKey, bool shiftKey)
        {
            var clickedIndex = (int)(localPosition.y / resolvedItemHeight);
            if (clickedIndex > m_ItemsSource.Count - 1)
                return;

            var clickedItemId = GetIdFromIndex(clickedIndex);
            switch (clickCount)
            {
                case 1:
                    if (selectionType == SelectionType.None)
                        return;

                    if (selectionType == SelectionType.Multiple && actionKey)
                    {
                        m_RangeSelectionOrigin = clickedIndex;

                        // Add/remove single clicked element
                        if (m_SelectedIds.Contains(clickedItemId))
                            RemoveFromSelection(clickedIndex);
                        else
                            AddToSelection(clickedIndex);
                    }
                    else if (selectionType == SelectionType.Multiple && shiftKey)
                    {
                        if (m_RangeSelectionOrigin == -1)
                        {
                            m_RangeSelectionOrigin = clickedIndex;
                            SetSelection(clickedIndex);
                        }
                        else
                        {
                            ClearSelectionWithoutValidation();

                            // Add range
                            if (clickedIndex < m_RangeSelectionOrigin)
                            {
                                for (int i = clickedIndex; i <= m_RangeSelectionOrigin; i++)
                                    AddToSelection(i);
                            }
                            else
                            {
                                for (int i = m_RangeSelectionOrigin; i <= clickedIndex; i++)
                                    AddToSelection(i);
                            }
                        }
                    }
                    else if (selectionType == SelectionType.Multiple && m_SelectedIndices.Contains(clickedIndex))
                    {
                        // Do noting, selection will be processed OnMouseUp
                        // If drag and drop will be started listview dragger will capture the mouse and ListView will not receive the mouse up event
                    }
                    else // single
                    {
                        m_RangeSelectionOrigin = clickedIndex;
                        SetSelection(clickedIndex);
                    }
                    break;
                case 2:
                    if (onItemsChosen != null)
                    {
                        ProcessSingleClick(clickedIndex);
                    }

                    onItemsChosen?.Invoke(m_SelectedItems);
                    break;
            }
        }

        private void ProcessSingleClick(int clickedIndex)
        {
            m_RangeSelectionOrigin = clickedIndex;
            SetSelection(clickedIndex);
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            var clickedIndex = (int)(evt.localMousePosition.y / itemHeight);
            if (selectionType == SelectionType.Multiple
                && !evt.shiftKey
                && !evt.actionKey
                && m_SelectedIndices.Count > 1
                && m_SelectedIndices.Contains(clickedIndex))
            {
                ProcessSingleClick(clickedIndex);
            }
        }

        internal void SelectAll()
        {
            if (!HasValidDataAndBindings())
                return;

            if (selectionType != SelectionType.Multiple)
            {
                return;
            }

            for (var index = 0; index < itemsSource.Count; index++)
            {
                var id = GetIdFromIndex(index);
                var item = m_ItemsSource[index];

                foreach (var recycledItem in m_Pool)
                    if (recycledItem.id == id)
                        recycledItem.SetSelected(true);

                if (!m_SelectedIds.Contains(id))
                {
                    m_SelectedIds.Add(id);
                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(item);
                }
            }

            NotifyOfSelectionChange();
            SaveViewData();
        }

        private int GetIdFromIndex(int index)
        {
            if (m_GetItemId == null)
                return index;
            else
                return m_GetItemId(index);
        }

        public void AddToSelection(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            AddToSelectionWithoutValidation(index);
            NotifyOfSelectionChange();
            SaveViewData();
        }

        private void AddToSelectionWithoutValidation(int index)
        {
            if (m_SelectedIndices.Contains(index))
                return;

            var id = GetIdFromIndex(index);
            var item = m_ItemsSource[index];

            foreach (var recycledItem in m_Pool)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(true);

            m_SelectedIds.Add(id);
            m_SelectedIndices.Add(index);
            m_SelectedItems.Add(item);
        }

        public void RemoveFromSelection(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            RemoveFromSelectionWithoutValidation(index);
            NotifyOfSelectionChange();
            SaveViewData();
        }

        private void RemoveFromSelectionWithoutValidation(int index)
        {
            if (!m_SelectedIndices.Contains(index))
                return;

            var id = GetIdFromIndex(index);
            var item = m_ItemsSource[index];

            foreach (var recycledItem in m_Pool)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(false);

            m_SelectedIds.Remove(id);
            m_SelectedIndices.Remove(index);
            m_SelectedItems.Remove(item);
        }

        public void SetSelection(int index)
        {
            if (index < 0)
            {
                ClearSelection();
                return;
            }

            SetSelection(new[] {index});
        }

        public void SetSelection(IEnumerable<int> indices)
        {
            SetSelectionInternal(indices, true);
        }

        public void SetSelectionWithoutNotify(IEnumerable<int> indices)
        {
            SetSelectionInternal(indices, false);
        }

        internal void SetSelectionInternal(IEnumerable<int> indices, bool sendNotification)
        {
            if (!HasValidDataAndBindings() || indices == null)
                return;

            ClearSelectionWithoutValidation();
            foreach (var index in indices)
                AddToSelectionWithoutValidation(index);

            if (sendNotification)
                NotifyOfSelectionChange();

            SaveViewData();
        }

        private void NotifyOfSelectionChange()
        {
            if (!HasValidDataAndBindings())
                return;

            onSelectionChange?.Invoke(m_SelectedItems);
#pragma warning disable 618
            onSelectionChanged?.Invoke(m_SelectedItems);
#pragma warning restore 618
        }

        public void ClearSelection()
        {
            if (!HasValidDataAndBindings())
                return;

            ClearSelectionWithoutValidation();
            NotifyOfSelectionChange();
        }

        private void ClearSelectionWithoutValidation()
        {
            foreach (var recycledItem in m_Pool)
                recycledItem.SetSelected(false);
            m_SelectedIds.Clear();
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();
        }

        public void ScrollTo(VisualElement visualElement)
        {
            m_ScrollView.ScrollTo(visualElement);
        }

        internal void SetDragAndDropController(IListViewDragAndDropController dragAndDropController)
        {
            if (m_Dragger == null)
                m_Dragger = new ListViewDragger(this);

            m_Dragger.dragAndDropController = dragAndDropController;
        }

        //Used for unit testing
        internal IListViewDragAndDropController GetDragAndDropController()
        {
            return m_Dragger?.dragAndDropController;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            var key = GetFullHierarchicalViewDataKey();
            OverwriteFromViewData(this, key);
        }

        private void OnScroll(float offset)
        {
            if (!HasValidDataAndBindings())
                return;

            m_ScrollOffset = offset;
            var pixelAlignedItemHeight = resolvedItemHeight;
            int fistVisibleItem = (int)(offset / pixelAlignedItemHeight);
            m_ScrollView.contentContainer.style.height = itemsSource.Count * pixelAlignedItemHeight;

            if (fistVisibleItem != m_FirstVisibleIndex)
            {
                m_FirstVisibleIndex = fistVisibleItem;

                if (m_Pool.Count > 0)
                {
                    // we try to avoid rebinding a few items
                    if (m_FirstVisibleIndex < m_Pool[0].index) //we're scrolling up
                    {
                        //How many do we have to swap back
                        int count = m_Pool[0].index - m_FirstVisibleIndex;

                        var inserting = m_ScrollInsertionList;

                        for (int i = 0; i < count && m_Pool.Count > 0; ++i)
                        {
                            var last = m_Pool[m_Pool.Count - 1];
                            inserting.Add(last);
                            m_Pool.RemoveAt(m_Pool.Count - 1); //we remove from the end

                            last.element.SendToBack();  //We send the element to the top of the list (back in z-order)
                        }

                        m_ScrollInsertionList = m_Pool;
                        m_Pool = inserting;
                        m_Pool.AddRange(m_ScrollInsertionList);
                        m_ScrollInsertionList.Clear();
                    }
                    else //down
                    {
                        if (m_FirstVisibleIndex < m_Pool[m_Pool.Count - 1].index)
                        {
                            var inserting = m_ScrollInsertionList;

                            int checkIndex = 0;
                            while (m_FirstVisibleIndex > m_Pool[checkIndex].index)
                            {
                                var first = m_Pool[checkIndex];
                                inserting.Add(first);
                                checkIndex++;

                                first.element.BringToFront();  //We send the element to the bottom of the list (front in z-order)
                            }

                            m_Pool.RemoveRange(0, checkIndex); //we remove them all at once
                            m_Pool.AddRange(inserting); // add them back to the end
                            inserting.Clear();
                        }
                    }

                    //Let's rebind everything
                    for (var i = 0; i < m_Pool.Count && i + m_FirstVisibleIndex < itemsSource.Count; i++)
                        Setup(m_Pool[i], i + m_FirstVisibleIndex);
                }
            }
        }

        private bool HasValidDataAndBindings()
        {
            return itemsSource != null && makeItem != null && bindItem != null;
        }

        public void Refresh()
        {
            foreach (var recycledItem in m_Pool)
                recycledItem.DetachElement();

            m_Pool.Clear();
            m_ScrollView.Clear();
            m_VisibleItemCount = 0;

            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();

            // O(n)
            if (m_SelectedIds.Count > 0)
            {
                // Add selected objects to working lists.
                for (var index = 0; index < m_ItemsSource.Count; ++index)
                {
                    if (!m_SelectedIds.Contains(GetIdFromIndex(index))) continue;

                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(m_ItemsSource[index]);
                }
            }

            if (!HasValidDataAndBindings())
                return;

            m_LastHeight = m_ScrollView.layout.height;

            if (float.IsNaN(m_LastHeight))
                return;

            m_FirstVisibleIndex = (int)(m_ScrollOffset / resolvedItemHeight);
            ResizeHeight(m_LastHeight);
        }

        private void ResizeHeight(float height)
        {
            var pixelAlignedItemHeight = resolvedItemHeight;
            var contentHeight = itemsSource.Count * pixelAlignedItemHeight;
            m_ScrollView.contentContainer.style.height = contentHeight;

            // Restore scroll offset and preemptively update the highValue
            // in case this is the initial restore from persistent data and
            // the ScrollView's OnGeometryChanged() didn't update the low
            // and highValues.
            var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);
            m_ScrollView.verticalScroller.highValue = Mathf.Min(Mathf.Max(m_ScrollOffset, m_ScrollView.verticalScroller.highValue), scrollableHeight);
            m_ScrollView.verticalScroller.value = Mathf.Min(m_ScrollOffset, m_ScrollView.verticalScroller.highValue);

            int itemCount = Math.Min((int)(height / pixelAlignedItemHeight) + k_ExtraVisibleItems, itemsSource.Count);

            if (m_VisibleItemCount != itemCount)
            {
                if (m_VisibleItemCount > itemCount)
                {
                    // Shrink
                    int removeCount = m_VisibleItemCount - itemCount;
                    for (int i = 0; i < removeCount; i++)
                    {
                        int lastIndex = m_Pool.Count - 1;

                        var poolItem = m_Pool[lastIndex];
                        poolItem.element.RemoveFromHierarchy();
                        poolItem.DetachElement();

                        m_Pool.RemoveAt(lastIndex);
                    }
                }
                else
                {
                    // Grow
                    int addCount = itemCount - m_VisibleItemCount;
                    for (int i = 0; i < addCount; i++)
                    {
                        int index = i + m_FirstVisibleIndex + m_VisibleItemCount;
                        var item = makeItem();
                        var recycledItem = new RecycledItem(item);
                        m_Pool.Add(recycledItem);

                        item.AddToClassList("unity-listview-item");
                        item.style.marginTop = 0f;
                        item.style.marginBottom = 0f;
                        item.style.position = Position.Absolute;
                        item.style.left = 0f;
                        item.style.right = 0f;
                        item.style.height = pixelAlignedItemHeight;
                        if (index < itemsSource.Count)
                        {
                            Setup(recycledItem, index);
                        }
                        else
                        {
                            item.style.visibility = Visibility.Hidden;
                        }

                        Add(item);
                    }
                }

                m_VisibleItemCount = itemCount;
            }

            m_LastHeight = height;
            UpdateBackground();
        }

        private void Setup(RecycledItem recycledItem, int newIndex)
        {
            var newId = GetIdFromIndex(newIndex);
            recycledItem.element.style.visibility = Visibility.Visible;
            if (recycledItem.index == newIndex) return;

            m_LastItemIndex = newIndex;
            if (showAlternatingRowBackgrounds != AlternatingRowBackground.None && newIndex % 2 == 1)
                recycledItem.element.AddToClassList(itemAlternativeBackgroundUssClassName);
            else
                recycledItem.element.RemoveFromClassList(itemAlternativeBackgroundUssClassName);

            if (recycledItem.index != RecycledItem.kUndefinedIndex)
                unbindItem?.Invoke(recycledItem.element, recycledItem.index);

            var pixelAlignedItemHeight = resolvedItemHeight;
            recycledItem.index = newIndex;
            recycledItem.id = newId;
            recycledItem.element.style.top = recycledItem.index * pixelAlignedItemHeight;
            recycledItem.element.style.bottom = (itemsSource.Count - recycledItem.index - 1) * pixelAlignedItemHeight;
            bindItem(recycledItem.element, recycledItem.index);
            recycledItem.SetSelected(m_SelectedIds.Contains(newId));
        }

        private void UpdateBackground()
        {
            var backgroundFillHeight = m_ScrollView.contentViewport.layout.size.y - m_ScrollView.contentContainer.layout.size.y;
            if (showAlternatingRowBackgrounds != AlternatingRowBackground.All || backgroundFillHeight <= 0)
            {
                m_EmptyRows.RemoveFromHierarchy();
                return;
            }

            if (m_EmptyRows.parent == null)
                m_ScrollView.contentViewport.Add(m_EmptyRows);

            var pixelAlignedItemHeight = resolvedItemHeight;
            var itemsCount = Mathf.FloorToInt(backgroundFillHeight / pixelAlignedItemHeight) + 1;
            if (itemsCount > m_EmptyRows.childCount)
            {
                var itemsToAdd = itemsCount - m_EmptyRows.childCount;
                for (var i = 0; i < itemsToAdd; i++)
                {
                    var row  = new VisualElement();
                    //Inline style is used to prevent a user from changing an item flexShrink property.
                    row.style.flexShrink = 0;
                    m_EmptyRows.Add(row);
                }
            }

            var index = m_LastItemIndex;
            foreach (var child in m_EmptyRows.hierarchy.Children())
            {
                index++;
                child.style.height = pixelAlignedItemHeight;
                child.EnableInClassList(itemAlternativeBackgroundUssClassName, index % 2 == 1);
            }
        }

        private void OnSizeChanged(GeometryChangedEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (Mathf.Approximately(evt.newRect.height, evt.oldRect.height))
                return;

            ResizeHeight(evt.newRect.height);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            int height;
            if (!m_ItemHeightIsInline && e.customStyle.TryGetValue(s_ItemHeightProperty, out height))
            {
                if (m_ItemHeight != height)
                {
                    m_ItemHeight = height;
                    Refresh();
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Refresh();
        }
    }
}
