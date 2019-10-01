// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public class ListView : BindableElement
    {
        public new class UxmlFactory : UxmlFactory<ListView, UxmlTraits> {}

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlIntAttributeDescription m_ItemHeight = new UxmlIntAttributeDescription { name = "item-height", obsoleteNames = new[] {"itemHeight"}, defaultValue = s_DefaultItemHeight };
            UxmlBoolAttributeDescription m_ShowBorder = new UxmlBoolAttributeDescription { name = "show-border", defaultValue = false };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                int itemHeight = 0;
                if (m_ItemHeight.TryGetValueFromBag(bag, cc, ref itemHeight))
                {
                    ((ListView)ve).itemHeight = itemHeight;
                }

                ((ListView)ve).showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
            }
        }

        private class RecycledItem
        {
            public VisualElement element { get; private set; }
            public int index;
            public int id;

            public RecycledItem(VisualElement element)
            {
                this.element = element;
                index = id = -1;
                element.AddToClassList(ListView.itemUssClassName);
            }

            public void DetachElement()
            {
                element.RemoveFromClassList(ListView.itemUssClassName);
                element = null;
            }

            public void SetSelected(bool selected)
            {
                if (element != null)
                {
                    if (selected)
                    {
                        element.AddToClassList(ListView.itemSelectedVariantUssClassName);
                        element.pseudoStates |= PseudoStates.Checked;
                    }
                    else
                    {
                        element.RemoveFromClassList(ListView.itemSelectedVariantUssClassName);
                        element.pseudoStates &= ~PseudoStates.Checked;
                    }
                }
            }
        }

        public event Action<object> onItemChosen;
        public event Action<List<object>> onSelectionChanged;

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
            get
            {
                return m_MakeItem;
            }
            set
            {
                if (m_MakeItem == value)
                    return;
                m_MakeItem = value;
                Refresh();
            }
        }

        private Action<VisualElement, int> m_BindItem;
        public Action<VisualElement, int> bindItem
        {
            get
            {
                return m_BindItem;
            }
            set
            {
                m_BindItem = value;
                Refresh();
            }
        }

        private Func<int, int> m_GetItemId;
        internal Func<int, int> getItemId
        {
            get
            {
                return m_GetItemId;
            }
            set
            {
                m_GetItemId = value;
                Refresh();
            }
        }

        private float m_PixelAlignedItemHeight;
        public float resolvedItemHeight => m_PixelAlignedItemHeight;

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
                m_ItemHeight = value;
                UpdatePixelAlignedHeight();
                Refresh();
            }
        }

        public bool showBorder
        {
            get { return ClassListContains(borderUssClassName); }
            set { EnableInClassList(borderUssClassName, value); }
        }

        // Persisted.
        [SerializeField]
        private float m_ScrollOffset;

        // Persisted. It's why this can't be a HashSet(). :(
        [SerializeField]
        private List<int> m_SelectedIds = new List<int>();

        internal List<int> currentSelectionIds
        {
            get { return m_SelectedIds; }
        }

        // Not persisted! Just used for fast lookups of selected indices and object references.
        // This is to avoid also having a mapping from index/objectref to index for the entire
        // items source.
        private List<int> m_SelectedIndices = new List<int>();
        private List<object> m_SelectedItems = new List<object>();

        private int m_RangeSelectionOrigin = -1;

        public int selectedIndex
        {
            get { return m_SelectedIndices.Count == 0 ? -1 : m_SelectedIndices.First(); }
            set { SetSelection(value); }
        }

        public object selectedItem { get { return m_SelectedItems.Count == 0 ? null : m_SelectedItems.First(); } }

        public override VisualElement contentContainer { get { return m_ScrollView.contentContainer; } }

        public SelectionType selectionType { get; set; }

        internal static readonly int s_DefaultItemHeight = 30;
        internal static CustomStyleProperty<int> s_ItemHeightProperty = new CustomStyleProperty<int>("--unity-item-height");

        private int m_FirstVisibleIndex;
        private float m_LastHeight;
        private List<RecycledItem> m_Pool = new List<RecycledItem>();
        private ScrollView m_ScrollView;

        // we keep this list in order to minimize temporary gc allocs
        List<RecycledItem> m_ScrollInsertionList = new List<RecycledItem>();

        private const int k_ExtraVisibleItems = 2;
        private int m_VisibleItemCount;

        public static readonly string ussClassName = "unity-list-view";
        public static readonly string borderUssClassName = ussClassName + "--with-border";
        public static readonly string itemUssClassName = ussClassName + "__item";
        public static readonly string itemSelectedVariantUssClassName = itemUssClassName + "--selected";

        public ListView()
        {
            AddToClassList(ussClassName);

            selectionType = SelectionType.Single;
            m_ScrollOffset = 0.0f;

            m_ScrollView = new ScrollView();
            m_ScrollView.viewDataKey = "list-view__scroll-view";
            m_ScrollView.StretchToParentSize();
            m_ScrollView.verticalScroller.valueChanged += OnScroll;
            hierarchy.Add(m_ScrollView);

            RegisterCallback<GeometryChangedEvent>(OnSizeChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            m_ScrollView.contentContainer.RegisterCallback<MouseDownEvent>(OnClick);
            m_ScrollView.contentContainer.RegisterCallback<KeyDownEvent>(OnKeyDown);
            m_ScrollView.contentContainer.focusable = true;
            m_ScrollView.contentContainer.usageHints &= ~UsageHints.GroupTransform; // Scroll views with virtualized content shouldn't have the "view transform" optimization

            focusable = true;
            isCompositeRoot = true;
            delegatesFocus = true;
        }

        public ListView(IList itemsSource, int itemHeight, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem) : this()
        {
            m_ItemsSource = itemsSource;
            m_ItemHeight = itemHeight;
            m_MakeItem = makeItem;
            m_BindItem = bindItem;
        }

        public void OnKeyDown(KeyDownEvent evt)
        {
            if (evt == null || !HasValidDataAndBindings())
                return;

            bool shouldStopPropagation = true;
            bool shouldScroll = true;

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
                    if (onItemChosen != null)
                        onItemChosen.Invoke(m_ItemsSource[selectedIndex]);
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

            if (index == -1)
            {
                // Scroll to last item
                int actualCount = (int)(m_LastHeight / resolvedItemHeight);
                if (itemsSource.Count < actualCount)
                {
                    m_ScrollView.scrollOffset = new Vector2(0, 0);
                }
                else
                {
                    m_ScrollView.scrollOffset = new Vector2(0, itemsSource.Count * resolvedItemHeight);
                }
            }
            else if (m_FirstVisibleIndex > index)
            {
                m_ScrollView.scrollOffset = Vector2.up * resolvedItemHeight * index;
            }
            else // index >= first
            {
                int actualCount = (int)(m_LastHeight / resolvedItemHeight);
                if (index < m_FirstVisibleIndex + actualCount)
                    return;

                bool someItemIsPartiallyVisible = (int)(m_LastHeight - actualCount * resolvedItemHeight) != 0;
                int d = index - actualCount;

                // we're scrolling down in that case
                // if the listview size is not an integer multiple of the item height
                // the selected item might be the last visible and truncated one
                // in that case, increment by one the index
                if (someItemIsPartiallyVisible)
                    d++;

                m_ScrollView.scrollOffset = Vector2.up * resolvedItemHeight * d;
            }
        }

        private void OnClick(MouseDownEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            var clickedIndex = (int)(evt.localMousePosition.y / resolvedItemHeight);

            if (clickedIndex > m_ItemsSource.Count - 1)
                return;

            var clickedItem = m_ItemsSource[clickedIndex];
            var clickedItemId = GetIdFromIndex(clickedIndex);
            switch (evt.clickCount)
            {
                case 1:
                    if (selectionType == SelectionType.None)
                        return;

                    if (selectionType == SelectionType.Multiple && evt.actionKey)
                    {
                        m_RangeSelectionOrigin = clickedIndex;

                        // Add/remove single clicked element
                        if (m_SelectedIds.Contains(clickedItemId))
                            RemoveFromSelection(clickedIndex);
                        else
                            AddToSelection(clickedIndex);
                    }
                    else if (selectionType == SelectionType.Multiple && evt.shiftKey)
                    {
                        if (m_RangeSelectionOrigin == -1)
                        {
                            m_RangeSelectionOrigin = clickedIndex;
                            SetSelection(clickedIndex);
                        }
                        else
                        {
                            foreach (var recycledItem in m_Pool)
                                recycledItem.SetSelected(false);
                            m_SelectedIds.Clear();
                            m_SelectedIndices.Clear();
                            m_SelectedItems.Clear();

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
                    else // single
                    {
                        m_RangeSelectionOrigin = clickedIndex;
                        SetSelection(clickedIndex);
                    }
                    break;
                case 2:
                    if (onItemChosen == null)
                        return;

                    onItemChosen.Invoke(clickedItem);
                    break;
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

        protected void AddToSelection(int index)
        {
            if (!HasValidDataAndBindings())
                return;

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

            NotifyOfSelectionChange();

            SaveViewData();
        }

        protected void RemoveFromSelection(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            var id = GetIdFromIndex(index);
            var item = m_ItemsSource[index];

            foreach (var recycledItem in m_Pool)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(false);

            if (m_SelectedIds.Contains(id))
            {
                m_SelectedIds.Remove(id);
                m_SelectedIndices.Remove(index);
                m_SelectedItems.Remove(item);
            }

            NotifyOfSelectionChange();

            SaveViewData();
        }

        protected void SetSelection(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            if (index < 0)
            {
                ClearSelection();
                return;
            }

            var id = GetIdFromIndex(index);
            var item = m_ItemsSource[index];

            foreach (var recycledItem in m_Pool)
                recycledItem.SetSelected(recycledItem.id == id);

            m_SelectedIds.Clear();
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();

            m_SelectedIds.Add(id);
            m_SelectedIndices.Add(index);
            m_SelectedItems.Add(item);

            NotifyOfSelectionChange();

            SaveViewData();
        }

        private void NotifyOfSelectionChange()
        {
            if (!HasValidDataAndBindings())
                return;

            if (onSelectionChanged == null)
                return;

            onSelectionChanged.Invoke(m_SelectedItems);
        }

        protected void ClearSelection()
        {
            if (!HasValidDataAndBindings())
                return;

            foreach (var recycledItem in m_Pool)
                recycledItem.SetSelected(false);
            m_SelectedIds.Clear();
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();

            NotifyOfSelectionChange();
        }

        public void ScrollTo(VisualElement visualElement)
        {
            m_ScrollView.ScrollTo(visualElement);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);
        }

        private void OnScroll(float offset)
        {
            if (!HasValidDataAndBindings())
                return;

            m_ScrollOffset = offset;
            int fistVisibleItem = (int)(offset / resolvedItemHeight);
            m_ScrollView.contentContainer.style.height = itemsSource.Count * resolvedItemHeight;

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
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();

            m_VisibleItemCount = 0;

            if (!HasValidDataAndBindings())
                return;

            m_LastHeight = m_ScrollView.layout.height;

            if (float.IsNaN(m_LastHeight))
                return;

            ResizeHeight(m_LastHeight);
        }

        private void UpdatePixelAlignedHeight()
        {
            var dpiScaling = scaledPixelsPerPoint;
            m_PixelAlignedItemHeight = Mathf.Round(itemHeight * dpiScaling) / dpiScaling;
        }

        private void ResizeHeight(float height)
        {
            UpdatePixelAlignedHeight();

            var contentHeight = itemsSource.Count * m_PixelAlignedItemHeight;
            m_ScrollView.contentContainer.style.height = contentHeight;

            // Restore scroll offset and pre-emptively update the highValue
            // in case this is the initial restore from persistent data and
            // the ScrollView's OnGeometryChanged() didn't update the low
            // and highValues.
            var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);
            m_ScrollView.verticalScroller.highValue = Mathf.Min(Mathf.Max(m_ScrollOffset, m_ScrollView.verticalScroller.highValue), scrollableHeight);
            m_ScrollView.verticalScroller.value = Mathf.Min(m_ScrollOffset, m_ScrollView.verticalScroller.highValue);

            int itemCount = Math.Min((int)(height / m_PixelAlignedItemHeight) + k_ExtraVisibleItems, itemsSource.Count);

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
                        item.style.height = m_PixelAlignedItemHeight;
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


            // O(n)
            if (m_SelectedIds.Count > 0)
            {
                // Add selected objects to working lists.
                for (int index = 0; index < m_ItemsSource.Count; ++index)
                {
                    if (m_SelectedIds.Contains(GetIdFromIndex(index)))
                    {
                        m_SelectedIndices.Add(index);
                        m_SelectedItems.Add(m_ItemsSource[index]);
                    }
                }
            }
        }

        private void Setup(RecycledItem recycledItem, int newIndex)
        {
            var newId = GetIdFromIndex(newIndex);

            recycledItem.element.style.visibility = Visibility.Visible;
            if (recycledItem.index != newIndex)
            {
                recycledItem.index = newIndex;
                recycledItem.id = newId;
                recycledItem.element.style.top = recycledItem.index * m_PixelAlignedItemHeight;
                recycledItem.element.style.bottom = (itemsSource.Count - recycledItem.index - 1) * m_PixelAlignedItemHeight;
                bindItem(recycledItem.element, recycledItem.index);
                recycledItem.SetSelected(m_SelectedIds.Contains(newId));
            }
        }

        private void OnSizeChanged(GeometryChangedEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (evt.newRect.height == evt.oldRect.height)
                return;

            ResizeHeight(evt.newRect.height);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            int height = 0;
            if (!m_ItemHeightIsInline && e.customStyle.TryGetValue(s_ItemHeightProperty, out height))
                itemHeight = height;
        }
    }
}
