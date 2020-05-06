using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using System.Linq;

namespace UnityEngine.UIElements
{
    internal class TreeView : VisualElement
    {
        private static readonly string s_ListViewName = "unity-tree-view__list-view";
        private static readonly string s_ItemName = "unity-tree-view__item";
        private static readonly string s_ItemToggleName = "unity-tree-view__item-toggle";
        private static readonly string s_ItemIndentsContainerName = "unity-tree-view__item-indents";
        private static readonly string s_ItemIndentName = "unity-tree-view__item-indent";
        private static readonly string s_ItemContentContainerName = "unity-tree-view__item-content";

        public new class UxmlFactory : UxmlFactory<TreeView, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlIntAttributeDescription m_ItemHeight = new UxmlIntAttributeDescription { name = "item-height", defaultValue = ListView.s_DefaultItemHeight };
            private readonly UxmlBoolAttributeDescription m_ShowBorder = new UxmlBoolAttributeDescription { name = "show-border", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType = new UxmlEnumAttributeDescription<SelectionType> { name = "selection-type", defaultValue = SelectionType.Single };
            private readonly UxmlEnumAttributeDescription<AlternatingRowBackground> m_ShowAlternatingRowBackgrounds = new UxmlEnumAttributeDescription<AlternatingRowBackground> { name = "show-alternating-row-backgrounds", defaultValue = AlternatingRowBackground.None };

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
                    ((TreeView)ve).itemHeight = itemHeight;
                }
                ((TreeView)ve).showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
                ((TreeView)ve).selectionType = m_SelectionType.GetValueFromBag(bag, cc);
                ((TreeView)ve).showAlternatingRowBackgrounds = m_ShowAlternatingRowBackgrounds.GetValueFromBag(bag, cc);
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
                ListViewRefresh();
            }
        }

        public event Action<IEnumerable<ITreeViewItem>> onItemsChosen;
        public event Action<IEnumerable<ITreeViewItem>> onSelectionChange;

        private List<ITreeViewItem> m_SelectedItems;
        public ITreeViewItem selectedItem => m_SelectedItems.Count == 0 ? null : m_SelectedItems.First();

        public IEnumerable<ITreeViewItem> selectedItems
        {
            get
            {
                if (m_SelectedItems != null)
                    return m_SelectedItems;

                m_SelectedItems = new List<ITreeViewItem>();
                foreach (var treeItem in items)
                {
                    foreach (var itemId in m_ListView.currentSelectionIds)
                    {
                        if (treeItem.id == itemId)
                            m_SelectedItems.Add(treeItem);
                    }
                }

                return m_SelectedItems;
            }
        }

        private Action<VisualElement, ITreeViewItem> m_BindItem;
        public Action<VisualElement, ITreeViewItem> bindItem
        {
            get { return m_BindItem; }
            set
            {
                m_BindItem = value;
                ListViewRefresh();
            }
        }

        public Action<VisualElement, ITreeViewItem> unbindItem { get; set; }

        IList<ITreeViewItem> m_RootItems;
        public IList<ITreeViewItem> rootItems
        {
            get { return m_RootItems; }
            set
            {
                m_RootItems = value;
                Refresh();
            }
        }

        public IEnumerable<ITreeViewItem> items => GetAllItems(m_RootItems);

        public float resolvedItemHeight => m_ListView.resolvedItemHeight;

        public int itemHeight
        {
            get { return m_ListView.itemHeight; }
            set { m_ListView.itemHeight = value; }
        }

        public bool horizontalScrollingEnabled
        {
            get { return m_ListView.horizontalScrollingEnabled; }
            set { m_ListView.horizontalScrollingEnabled = value; }
        }

        public bool showBorder
        {
            get { return m_ListView.showBorder;}
            set { m_ListView.showBorder = value; }
        }

        public SelectionType selectionType
        {
            get { return m_ListView.selectionType; }
            set { m_ListView.selectionType = value; }
        }

        public AlternatingRowBackground showAlternatingRowBackgrounds
        {
            get { return m_ListView.showAlternatingRowBackgrounds; }
            set { m_ListView.showAlternatingRowBackgrounds = value; }
        }

        private struct TreeViewItemWrapper
        {
            public int id => item.id;
            public int depth;

            public bool hasChildren => item.hasChildren;

            public ITreeViewItem item;
        }

        [SerializeField]
        private List<int> m_ExpandedItemIds;

        private List<TreeViewItemWrapper> m_ItemWrappers;

        private readonly ListView m_ListView;
        private readonly ScrollView m_ScrollView;

        public TreeView()
        {
            m_SelectedItems = null;
            m_ExpandedItemIds = new List<int>();
            m_ItemWrappers = new List<TreeViewItemWrapper>();

            m_ListView = new ListView();
            m_ListView.name = s_ListViewName;
            m_ListView.itemsSource = m_ItemWrappers;
            m_ListView.viewDataKey = s_ListViewName;
            m_ListView.AddToClassList(s_ListViewName);
            hierarchy.Add(m_ListView);

            m_ListView.makeItem = MakeTreeItem;
            m_ListView.bindItem = BindTreeItem;
            m_ListView.unbindItem = UnbindTreeItem;
            m_ListView.getItemId = GetItemId;
            m_ListView.onItemsChosen += OnItemsChosen;
            m_ListView.onSelectionChange += OnSelectionChange;

            m_ScrollView = m_ListView.m_ScrollView;
            m_ScrollView.contentContainer.RegisterCallback<KeyDownEvent>(OnKeyDown);

            RegisterCallback<MouseUpEvent>(OnTreeViewMouseUp, TrickleDown.TrickleDown);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        public TreeView(
            IList<ITreeViewItem> items,
            int itemHeight,
            Func<VisualElement> makeItem,
            Action<VisualElement, ITreeViewItem> bindItem) : this()
        {
            m_ListView.itemHeight = itemHeight;
            m_MakeItem = makeItem;
            m_BindItem = bindItem;
            m_RootItems = items;

            Refresh();
        }

        public void Refresh()
        {
            RegenerateWrappers();
            ListViewRefresh();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);

            Refresh();
        }

        public static IEnumerable<ITreeViewItem> GetAllItems(IEnumerable<ITreeViewItem> rootItems)
        {
            if (rootItems == null)
                yield break;

            var iteratorStack = new Stack<IEnumerator<ITreeViewItem>>();
            var currentIterator = rootItems.GetEnumerator();

            while (true)
            {
                bool hasNext = currentIterator.MoveNext();
                if (!hasNext)
                {
                    if (iteratorStack.Count > 0)
                    {
                        currentIterator = iteratorStack.Pop();
                        continue;
                    }

                    // We're at the end of the root items list.
                    break;
                }

                var currentItem = currentIterator.Current;
                yield return currentItem;

                if (currentItem.hasChildren)
                {
                    iteratorStack.Push(currentIterator);
                    currentIterator = currentItem.children.GetEnumerator();
                }
            }
        }

        public void OnKeyDown(KeyDownEvent evt)
        {
            var index = m_ListView.selectedIndex;

            bool shouldStopPropagation = true;

            switch (evt.keyCode)
            {
                case KeyCode.RightArrow:
                    if (!IsExpandedByIndex(index))
                        ExpandItemByIndex(index);
                    break;
                case KeyCode.LeftArrow:
                    if (IsExpandedByIndex(index))
                        CollapseItemByIndex(index);
                    break;
                default:
                    shouldStopPropagation = false;
                    break;
            }

            if (shouldStopPropagation)
                evt.StopPropagation();
        }

        public void SetSelection(int id)
        {
            SetSelection(new[] {id});
        }

        public void SetSelection(IEnumerable<int> ids)
        {
            SetSelectionInternal(ids, true);
        }

        public void SetSelectionWithoutNotify(IEnumerable<int> ids)
        {
            SetSelectionInternal(ids, false);
        }

        internal void SetSelectionInternal(IEnumerable<int> ids, bool sendNotification)
        {
            if (ids == null)
                return;

            var selectedIndexes = ids.Select(id => GetItemIndex(id, true)).ToList();
            ListViewRefresh();
            m_ListView.SetSelectionInternal(selectedIndexes, sendNotification);
        }

        public void AddToSelection(int id)
        {
            var index = GetItemIndex(id, true);
            ListViewRefresh();
            m_ListView.AddToSelection(index);
        }

        public void RemoveFromSelection(int id)
        {
            var index = GetItemIndex(id);
            m_ListView.RemoveFromSelection(index);
        }

        private int GetItemIndex(int id, bool expand = false)
        {
            var item = FindItem(id);
            if (item == null)
                throw new ArgumentOutOfRangeException(nameof(id), id, $"{nameof(TreeView)}: Item id not found.");

            if (expand)
            {
                bool regenerateWrappers = false;
                var itemParent = item.parent;
                while (itemParent != null)
                {
                    if (!m_ExpandedItemIds.Contains(itemParent.id))
                    {
                        m_ExpandedItemIds.Add(itemParent.id);
                        regenerateWrappers = true;
                    }
                    itemParent = itemParent.parent;
                }

                if (regenerateWrappers)
                    RegenerateWrappers();
            }


            var index = 0;
            for (; index < m_ItemWrappers.Count; ++index)
                if (m_ItemWrappers[index].id == id)
                    break;

            return index;
        }

        public void ClearSelection()
        {
            m_ListView.ClearSelection();
        }

        public void ScrollTo(VisualElement visualElement)
        {
            m_ListView.ScrollTo(visualElement);
        }

        public void ScrollToItem(int id)
        {
            var index = GetItemIndex(id, true);
            Refresh();
            m_ListView.ScrollToItem(index);
        }

        public bool IsExpanded(int id)
        {
            return m_ExpandedItemIds.Contains(id);
        }

        public void CollapseItem(int id)
        {
            // Make sure the item is valid.
            if (FindItem(id) == null)
                throw new ArgumentOutOfRangeException(nameof(id), id, $"{nameof(TreeView)}: Item id not found.");

            // Try to find it in the currently visible list.
            for (int i = 0; i < m_ItemWrappers.Count; ++i)
                if (m_ItemWrappers[i].item.id == id)
                    if (IsExpandedByIndex(i))
                    {
                        CollapseItemByIndex(i);
                        return;
                    }

            if (!m_ExpandedItemIds.Contains(id))
                return;

            m_ExpandedItemIds.Remove(id);
            Refresh();
        }

        public void ExpandItem(int id)
        {
            // Make sure the item is valid.
            if (FindItem(id) == null)
                throw new ArgumentOutOfRangeException(nameof(id), id, $"{nameof(TreeView)}: Item id not found.");

            // Try to find it in the currently visible list.
            for (int i = 0; i < m_ItemWrappers.Count; ++i)
                if (m_ItemWrappers[i].item.id == id)
                    if (!IsExpandedByIndex(i))
                    {
                        ExpandItemByIndex(i);
                        return;
                    }

            if (m_ExpandedItemIds.Contains(id))
                return;

            m_ExpandedItemIds.Add(id);
            Refresh();
        }

        public ITreeViewItem FindItem(int id)
        {
            foreach (var item in items)
                if (item.id == id)
                    return item;

            return null;
        }

        private void ListViewRefresh()
        {
            m_ListView.Refresh();
        }

        private void OnItemsChosen(IEnumerable<object> chosenItems)
        {
            if (onItemsChosen == null)
                return;

            var itemsList = new List<ITreeViewItem>();
            foreach (var item in chosenItems)
            {
                var wrapper = (TreeViewItemWrapper)item;
                itemsList.Add(wrapper.item);
            }

            onItemsChosen.Invoke(itemsList);
        }

        private void OnSelectionChange(IEnumerable<object> selectedListItems)
        {
            if (m_SelectedItems == null)
                m_SelectedItems = new List<ITreeViewItem>();

            m_SelectedItems.Clear();
            foreach (var item in selectedListItems)
                m_SelectedItems.Add(((TreeViewItemWrapper)item).item);

            onSelectionChange?.Invoke(m_SelectedItems);
        }

        private void OnTreeViewMouseUp(MouseUpEvent evt)
        {
            m_ScrollView.contentContainer.Focus();
        }

        private void OnItemMouseUp(MouseUpEvent evt)
        {
            if ((evt.modifiers & EventModifiers.Alt) == 0)
                return;

            var target = evt.currentTarget as VisualElement;
            var toggle = target.Q<Toggle>(s_ItemToggleName);
            var index = (int)toggle.userData;
            var item = m_ItemWrappers[index].item;
            var wasExpanded = IsExpandedByIndex(index);

            if (!item.hasChildren)
                return;

            var hashSet = new HashSet<int>(m_ExpandedItemIds);

            if (wasExpanded)
                hashSet.Remove(item.id);
            else
                hashSet.Add(item.id);

            foreach (var child in GetAllItems(item.children))
            {
                if (child.hasChildren)
                {
                    if (wasExpanded)
                        hashSet.Remove(child.id);
                    else
                        hashSet.Add(child.id);
                }
            }

            m_ExpandedItemIds = hashSet.ToList();

            Refresh();

            evt.StopPropagation();
        }

        private VisualElement MakeTreeItem()
        {
            var itemContainer = new VisualElement()
            {
                name = s_ItemName,
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            itemContainer.AddToClassList(s_ItemName);
            itemContainer.RegisterCallback<MouseUpEvent>(OnItemMouseUp);

            var indents = new VisualElement()
            {
                name = s_ItemIndentsContainerName,
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            indents.AddToClassList(s_ItemIndentsContainerName);
            itemContainer.hierarchy.Add(indents);

            var toggle = new Toggle() { name = s_ItemToggleName };
            toggle.AddToClassList(Foldout.toggleUssClassName);
            toggle.RegisterValueChangedCallback(ToggleExpandedState);
            itemContainer.hierarchy.Add(toggle);

            var userContentContainer = new VisualElement()
            {
                name = s_ItemContentContainerName,
                style =
                {
                    flexGrow = 1
                }
            };
            userContentContainer.AddToClassList(s_ItemContentContainerName);
            itemContainer.Add(userContentContainer);

            if (m_MakeItem != null)
                userContentContainer.Add(m_MakeItem());

            return itemContainer;
        }

        private void UnbindTreeItem(VisualElement element, int index)
        {
            if (unbindItem == null)
                return;

            var item = m_ItemWrappers[index].item;
            var userContentContainer = element.Q(s_ItemContentContainerName).ElementAt(0);
            unbindItem(userContentContainer, item);
        }

        private void BindTreeItem(VisualElement element, int index)
        {
            var item = m_ItemWrappers[index].item;

            // Add indentation.
            var indents = element.Q(s_ItemIndentsContainerName);
            indents.Clear();
            for (int i = 0; i < m_ItemWrappers[index].depth; ++i)
            {
                var indentElement = new VisualElement();
                indentElement.AddToClassList(s_ItemIndentName);
                indents.Add(indentElement);
            }

            // Set toggle data.
            var toggle = element.Q<Toggle>(s_ItemToggleName);
            toggle.SetValueWithoutNotify(IsExpandedByIndex(index));
            toggle.userData = index;
            if (item.hasChildren)
                toggle.visible = true;
            else
                toggle.visible = false;

            if (m_BindItem == null)
                return;

            // Bind user content container.
            var userContentContainer = element.Q(s_ItemContentContainerName).ElementAt(0);
            m_BindItem(userContentContainer, item);
        }

        private int GetItemId(int index)
        {
            return m_ItemWrappers[index].id;
        }

        private bool IsExpandedByIndex(int index)
        {
            return m_ExpandedItemIds.Contains(m_ItemWrappers[index].id);
        }

        private void CollapseItemByIndex(int index)
        {
            if (!m_ItemWrappers[index].item.hasChildren)
                return;

            m_ExpandedItemIds.Remove(m_ItemWrappers[index].item.id);

            int recursiveChildCount = 0;
            int currentIndex = index + 1;
            int currentDepth = m_ItemWrappers[index].depth;
            while (currentIndex < m_ItemWrappers.Count && m_ItemWrappers[currentIndex].depth > currentDepth)
            {
                recursiveChildCount++;
                currentIndex++;
            }

            m_ItemWrappers.RemoveRange(index + 1, recursiveChildCount);

            ListViewRefresh();

            SaveViewData();
        }

        private void ExpandItemByIndex(int index)
        {
            if (!m_ItemWrappers[index].item.hasChildren)
                return;

            var childWrappers = new List<TreeViewItemWrapper>();
            CreateWrappers(m_ItemWrappers[index].item.children, m_ItemWrappers[index].depth + 1, ref childWrappers);

            m_ItemWrappers.InsertRange(index + 1, childWrappers);

            m_ExpandedItemIds.Add(m_ItemWrappers[index].item.id);

            ListViewRefresh();

            SaveViewData();
        }

        private void ToggleExpandedState(ChangeEvent<bool> evt)
        {
            var toggle = evt.target as Toggle;
            var index = (int)toggle.userData;
            var isExpanded = IsExpandedByIndex(index);

            Assert.AreNotEqual(isExpanded, evt.newValue);

            if (isExpanded)
                CollapseItemByIndex(index);
            else
                ExpandItemByIndex(index);

            // To make sure our TreeView gets focus, we need to force this. :(
            m_ScrollView.contentContainer.Focus();
        }

        private void CreateWrappers(IEnumerable<ITreeViewItem> treeViewItems, int depth, ref List<TreeViewItemWrapper> wrappers)
        {
            foreach (var item in treeViewItems)
            {
                var wrapper = new TreeViewItemWrapper()
                {
                    depth = depth,
                    item = item
                };

                wrappers.Add(wrapper);

                if (m_ExpandedItemIds.Contains(item.id) && item.hasChildren)
                    CreateWrappers(item.children, depth + 1, ref wrappers);
            }
        }

        private void RegenerateWrappers()
        {
            m_ItemWrappers.Clear();

            if (m_RootItems == null)
                return;

            CreateWrappers(m_RootItems, 0, ref m_ItemWrappers);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            int height;
            var oldHeight = m_ListView.itemHeight;
            if (!m_ListView.m_ItemHeightIsInline && e.customStyle.TryGetValue(ListView.s_ItemHeightProperty, out height))
                m_ListView.m_ItemHeight = height;

            if (m_ListView.m_ItemHeight != oldHeight)
                m_ListView.Refresh();
        }
    }
}
