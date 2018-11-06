// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using System.Linq;
using UnityEngine.UIElements.StyleSheets;

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
            UxmlIntAttributeDescription m_ItemHeight = new UxmlIntAttributeDescription { name = "item-height", defaultValue = ListView.s_DefaultItemHeight };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((TreeView)ve).itemHeight = m_ItemHeight.GetValueFromBag(bag, cc);
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
                ListViewRefresh();
            }
        }

        public event Action<ITreeViewItem> onItemChosen;

        private List<ITreeViewItem> m_CurrentSelection;
        public IEnumerable<ITreeViewItem> currentSelection
        {
            get
            {
                if (m_CurrentSelection != null)
                    return m_CurrentSelection;

                m_CurrentSelection = new List<ITreeViewItem>();
                foreach (var treeItem in items)
                {
                    foreach (var itemId in m_ListView.currentSelectionIds)
                    {
                        if (treeItem.id == itemId)
                            m_CurrentSelection.Add(treeItem);
                    }
                }

                return m_CurrentSelection;
            }
        }
        public event Action<List<ITreeViewItem>> onSelectionChanged;

        private Action<VisualElement, ITreeViewItem> m_BindItem;
        public Action<VisualElement, ITreeViewItem> bindItem
        {
            get
            {
                return m_BindItem;
            }
            set
            {
                m_BindItem = value;
                ListViewRefresh();
            }
        }

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

        public IEnumerable<ITreeViewItem> items
        {
            get { return GetAllItems(m_RootItems); }
        }

        public int itemHeight
        {
            get { return m_ListView.itemHeight; }
            set { m_ListView.itemHeight = value; }
        }

        public SelectionType selectionType
        {
            get { return m_ListView.selectionType; }
            set { m_ListView.selectionType = value; }
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

        private ListView m_ListView;
        private ScrollView m_ScrollView;

        public TreeView()
        {
            m_CurrentSelection = null;
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
            m_ListView.getItemId = GetItemId;
            m_ListView.onItemChosen += OnItemChosen;
            m_ListView.onSelectionChanged += OnSelectionChanged;

            m_ScrollView = m_ListView.Q<ScrollView>();
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

        static public IEnumerable<ITreeViewItem> GetAllItems(IEnumerable<ITreeViewItem> rootItems)
        {
            if (rootItems == null)
                yield break;

            var iteratorStack = new Stack<IEnumerator<ITreeViewItem>>();
            IEnumerator<ITreeViewItem> currentIterator = rootItems.GetEnumerator();

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
                    else
                    {
                        // We're at the end of the root items list.
                        break;
                    }
                }

                var currentItem = currentIterator.Current;
                yield return currentItem;

                if (currentItem.hasChildren)
                {
                    iteratorStack.Push(currentIterator);
                    currentIterator = currentItem.children.GetEnumerator();
                }
            }

            yield break;
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

        public void SelectItem(int id)
        {
            var item = FindItem(id);
            if (item == null)
                throw new InvalidOperationException("id");

            // Expand all parents.
            var parent = item.parent;
            while (parent != null)
            {
                if (!m_ExpandedItemIds.Contains(parent.id))
                    m_ExpandedItemIds.Add(parent.id);

                parent = parent.parent;
            }

            Refresh();

            int index = 0;
            for (; index < m_ItemWrappers.Count; ++index)
                if (m_ItemWrappers[index].id == id)
                    break;

            m_ListView.selectedIndex = index;
            m_ListView.ScrollToItem(m_ListView.selectedIndex);
        }

        public void ClearSelection()
        {
            m_ListView.selectedIndex = -1;
        }

        public bool IsExpanded(int id)
        {
            return m_ExpandedItemIds.Contains(id);
        }

        public void CollapseItem(int id)
        {
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
            // Try to find it in the currently visible list.
            for (int i = 0; i < m_ItemWrappers.Count; ++i)
                if (m_ItemWrappers[i].item.id == id)
                    if (!IsExpandedByIndex(i))
                    {
                        ExpandItemByIndex(i);
                        return;
                    }

            // Make sure the item is valid.
            if (FindItem(id) == null)
                throw new InvalidOperationException("TreeView: Item id not found.");

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

        private void OnItemChosen(object item)
        {
            if (onItemChosen != null)
            {
                var wrapper = (TreeViewItemWrapper)item;
                onItemChosen.Invoke(wrapper.item);
            }
        }

        private void OnSelectionChanged(List<object> items)
        {
            if (m_CurrentSelection == null)
                m_CurrentSelection = new List<ITreeViewItem>();

            m_CurrentSelection.Clear();
            foreach (var item in items)
                m_CurrentSelection.Add(((TreeViewItemWrapper)item).item);

            if (onSelectionChanged != null)
                onSelectionChanged.Invoke(m_CurrentSelection);
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
            toggle.AddToClassList(s_ItemToggleName);
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
            var userContentContainer = element.Q(s_ItemContentContainerName);
            m_BindItem(userContentContainer.ElementAt(0), item);
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

        private void CreateWrappers(IEnumerable<ITreeViewItem> items, int depth, ref List<TreeViewItemWrapper> wrappers)
        {
            int i = 0;
            foreach (var item in items)
            {
                var wrapper = new TreeViewItemWrapper()
                {
                    depth = depth,
                    item = item
                };

                wrappers.Add(wrapper);

                if (m_ExpandedItemIds.Contains(item.id) && item.hasChildren)
                    CreateWrappers(item.children, depth + 1, ref wrappers);

                i++;
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
            var oldHeight = m_ListView.itemHeight;
            int height = 0;
            if (!m_ListView.m_ItemHeightIsInline && e.customStyle.TryGetValue(ListView.s_ItemHeightProperty, out height))
                m_ListView.m_ItemHeight = height;

            if (m_ListView.m_ItemHeight != oldHeight)
                m_ListView.Refresh();
        }
    }
}
