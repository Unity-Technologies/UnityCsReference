// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Profiling;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    class ProgressWindow : EditorWindow
    {
        internal const string ussBasePath = "StyleSheets/ProgressWindow";
        internal static readonly string ussPath = $"{ussBasePath}/ProgressWindow.uss";
        internal static readonly string ussPathDark = $"{ussBasePath}/ProgressWindowDark.uss";
        internal static readonly string ussPathLight = $"{ussBasePath}/ProgressWindowLight.uss";
        internal const string k_UxmlProgressItemPath = "UXML/ProgressWindow/ProgressElement.uxml";
        public const string preferenceKey = "ProgressWindow.";

        const float k_WindowMinWidth = 100;
        const float k_WindowMinHeight = 70;
        const float k_WindowWidth = 400;
        const float k_WindowHeight = 300;

        static ProgressWindow s_Window;
        static readonly string k_CheckWindowKeyName = $"{typeof(ProgressWindow).FullName}h";
        internal static bool canHideDetails => s_Window && !s_Window.docked;
        static ProgressOrderComparer s_ProgressComparer = new ProgressOrderComparer(true);
        static VisualTreeAsset s_VisualProgressItemTask = null;

        Button m_DismissAllBtn;
        TreeView m_TreeView;

        Dictionary<int, List<int>> m_MissingParents;
        HashSet<int> m_ContainedItems;
        HashSet<int> m_ItemsNeedingExpansion;

        // For testing only
        internal TreeView treeView => m_TreeView;
        internal Button dismissAllButton => m_DismissAllBtn;

        [MenuItem("Window/General/Progress", priority = 50)]
        public static void ShowDetails()
        {
            ShowDetails(false);
        }

        internal static void ShowDetails(bool shouldReposition)
        {
            if (s_Window && s_Window.docked)
                shouldReposition = false;

            if (s_Window == null)
            {
                var wins = Resources.FindObjectsOfTypeAll<ProgressWindow>();
                if (wins.Length > 0)
                    s_Window = wins[0];
            }

            bool newWindowCreated = false;
            if (!s_Window)
            {
                s_Window = CreateInstance<ProgressWindow>();
                newWindowCreated = true;

                // If it is the first time this window is opened, reposition.
                if (!EditorPrefs.HasKey(k_CheckWindowKeyName))
                    shouldReposition = true;
            }

            s_Window.Show();
            s_Window.Focus();

            if (newWindowCreated && shouldReposition)
            {
                var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
                var size = new Vector2(k_WindowWidth, k_WindowHeight);
                s_Window.position = new Rect(mainWindowRect.xMax - k_WindowWidth - 6, mainWindowRect.yMax - k_WindowHeight - 50, size.x, size.y);
                s_Window.minSize = new Vector2(k_WindowMinWidth, k_WindowMinHeight);
            }
        }

        internal static void HideDetails()
        {
            if (canHideDetails)
            {
                s_Window.Close();
                s_Window = null;
            }
        }

        void OnEnable()
        {
            s_Window = this;
            titleContent = EditorGUIUtility.TrTextContent("Background Tasks");

            rootVisualElement.AddStyleSheetPath(ussPath);
            if (EditorGUIUtility.isProSkin)
                rootVisualElement.AddStyleSheetPath(ussPathDark);
            else
                rootVisualElement.AddStyleSheetPath(ussPathLight);

            var toolbar = new UIElements.Toolbar();
            m_DismissAllBtn = new ToolbarButton(ClearInactive)
            {
                name = "DismissAllBtn",
                text = L10n.Tr("Clear inactive"),
            };
            toolbar.Add(m_DismissAllBtn);

            // This is our friend the spacer
            toolbar.Add(new VisualElement()
            {
                style =
                {
                    flexGrow = 1
                }
            });

            rootVisualElement.Add(toolbar);
            s_VisualProgressItemTask = EditorGUIUtility.Load(k_UxmlProgressItemPath) as VisualTreeAsset;

            m_TreeView = new TreeView();
            m_TreeView.makeItem = MakeTreeViewItem;
            m_TreeView.bindItem = BindTreeViewItem;
            m_TreeView.unbindItem = UnbindTreeViewItem;
            m_TreeView.destroyItem = DestroyTreeViewItem;
            m_TreeView.fixedItemHeight = 50;
            m_TreeView.SetRootItems(new TreeViewItemData<Progress.Item>[] {});

            var scrollView = m_TreeView.Q<ScrollView>();
            if (scrollView != null)
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            rootVisualElement.Add(m_TreeView);
            m_TreeView.Rebuild();

            // Update the treeview with the existing items
            m_MissingParents = new Dictionary<int, List<int>>();
            m_ContainedItems = new HashSet<int>();
            m_ItemsNeedingExpansion = new HashSet<int>();
            OperationsAdded(Progress.EnumerateItems().ToArray());

            Progress.added += OperationsAdded;
            Progress.removed += OperationsRemoved;
            Progress.updated += OperationsUpdated;
            UpdateDismissAllButton();
        }

        void OnDisable()
        {
            Progress.added -= OperationsAdded;
            Progress.removed -= OperationsRemoved;
            Progress.updated -= OperationsUpdated;
        }

        static VisualElement MakeTreeViewItem()
        {
            return new VisualProgressItem(s_VisualProgressItemTask);
        }

        void BindTreeViewItem(VisualElement element, int index)
        {
            var visualProgressItem = element as VisualProgressItem;
            if (visualProgressItem == null)
                return;

            var progressItem = m_TreeView.GetItemDataForIndex<Progress.Item>(index);
            visualProgressItem.BindItem(progressItem);

            var indentLevel = GetIndentationLevel(index);
            var rootVE = m_TreeView.GetRootElementForIndex(index);
            var isEven = indentLevel % 2 == 0;
            rootVE.EnableInClassList("unity-tree-view__item-indent-even", isEven);
            rootVE.EnableInClassList("unity-tree-view__item-indent-odd", !isEven);
        }

        void UnbindTreeViewItem(VisualElement element, int index)
        {
            var visualProgressItem = element as VisualProgressItem;
            visualProgressItem?.UnbindItem();

            var rootVE = m_TreeView.GetRootElementForIndex(index);
            rootVE?.EnableInClassList("unity-tree-view__item-indent-even", false);
            rootVE?.EnableInClassList("unity-tree-view__item-indent-odd", false);
        }

        static void DestroyTreeViewItem(VisualElement element)
        {
            var visualProgressItem = element as VisualProgressItem;
            visualProgressItem?.DestroyItem();
        }

        int GetIndentationLevel(int index)
        {
            var level = 0;
            var parentId = m_TreeView.GetParentIdForIndex(index);
            while (parentId != -1)
            {
                ++level;
                parentId = m_TreeView.viewController.GetParentId(parentId);
            }

            return level;
        }

        internal static void ClearInactive()
        {
            var finishedItems = Progress.EnumerateItems().Where(item => item.finished);
            foreach (var item in finishedItems)
            {
                item.Remove();
            }
        }

        void UpdateDismissAllButton()
        {
            m_DismissAllBtn.SetEnabled(Progress.EnumerateItems().Any(item => item.finished));
        }

        void OperationsAdded(Progress.Item[] items)
        {
            //using (new EditorPerformanceTracker("ProgressWindow.OperationsAdded"))
            {
                foreach (var item in items)
                {
                    var treeViewItemData = new TreeViewItemData<Progress.Item>(item.id, item);
                    AddTreeViewItemToTree(treeViewItemData);

                    // When setting autoExpand to true, there is a possible race condition
                    // that can happen if the item is added and removed quickly.
                    // AutoExpand triggers a callback to be executed at a later point when makeItem is called.
                    // By the time the callback is called, the item might have been removed.
                    // Therefore, we expand all new items here manually.
                    m_TreeView.viewController.ExpandItem(item.id, true);

                    // Also, if the item has no child, then the expanded state is not set.
                    // Therefore, we need to keep track of this item to expand it when we add a child to it.
                    if (!m_TreeView.viewController.HasChildren(item.id))
                        m_ItemsNeedingExpansion.Add(item.id);
                }

                m_TreeView.RefreshItems();
            }
        }

        void OperationsRemoved(Progress.Item[] items)
        {
            using (new EditorPerformanceTracker("ProgressWindow.OperationsRemoved"))
            {
                foreach (var item in items)
                {
                    RemoveTreeViewItem(item.id);
                }

                m_TreeView.Rebuild();
                UpdateDismissAllButton();
            }
        }

        void OperationsUpdated(Progress.Item[] items)
        {
            //using (new EditorPerformanceTracker("ProgressWindow.OperationsUpdated"))
            {
                var itemsToBeReinserted = items
                    .Where(item => item.lastUpdates.HasAny(Progress.Updates.StatusChanged | Progress.Updates.PriorityChanged));

                // The items must me reinserted in a specific order, otherwise we end up
                // with the wrong insertion order or even with duplicates if not careful. To prevent any
                // issues, we must reinsert all siblings together, avoiding reinserting with their parents.
                var needsRebuild = false;
                var siblingGroups = itemsToBeReinserted.GroupBy(item => item.parentId, item => item.id);
                foreach (var siblings in siblingGroups)
                {
                    ReinsertAllItems(siblings);
                    needsRebuild = true;
                }

                if (needsRebuild)
                {
                    m_TreeView.Rebuild();
                    UpdateDismissAllButton();
                }
                else
                    m_TreeView.RefreshItems();
            }
        }

        List<Progress.Item> GetSiblingItems(Progress.Item item, out int newParentId)
        {
            newParentId = item.parentId;
            if (item.parentId == -1)
            {
                var rootIds = m_TreeView.GetRootIds();
                return rootIds?.Select(id => m_TreeView.GetItemDataForId<Progress.Item>(id)).ToList();
            }

            if (!m_ContainedItems.Contains(newParentId))
            {
                // If the parent is missing, the item should be put at the root level for now.
                List<int> itemIds;
                if (!m_MissingParents.TryGetValue(item.parentId, out itemIds))
                {
                    itemIds = new List<int>();
                    m_MissingParents.Add(item.parentId, itemIds);
                }

                itemIds.Add(item.id);
                newParentId = -1;
                return m_TreeView.GetRootIds()?.Select(id => m_TreeView.GetItemDataForId<Progress.Item>(id)).ToList();
            }

            var childrenIds = m_TreeView.viewController.GetChildrenIds(newParentId);
            return childrenIds?.Select(id => m_TreeView.GetItemDataForId<Progress.Item>(id)).ToList();
        }

        static int GetInsertionIndex(List<Progress.Item> items, Progress.Item itemToInsert)
        {
            if (items == null)
                return -1;
            var insertionIndex = items.BinarySearch(itemToInsert, s_ProgressComparer);
            if (insertionIndex < 0)
                return ~insertionIndex;
            return insertionIndex;
        }

        void ReinsertItem(int itemId)
        {
            var treeViewItemWithChildren = GetExistingTreeViewItemFromId(itemId);
            RemoveTreeViewItem(itemId);
            AddTreeViewItemToTree(treeViewItemWithChildren);
        }

        void ReinsertAllItems(IEnumerable<int> itemIds)
        {
            var treeViewItemsWithChildren = itemIds.Select(id => GetExistingTreeViewItemFromId(id)).ToList();

            // Remove all items first
            foreach (var treeViewItem in treeViewItemsWithChildren)
            {
                RemoveTreeViewItem(treeViewItem.id);
            }

            // Then reinsert them
            foreach (var treeViewItem in treeViewItemsWithChildren)
            {
                AddTreeViewItemToTree(treeViewItem);
            }
        }

        TreeViewItemData<Progress.Item> GetExistingTreeViewItemFromId(int itemId)
        {
            var progressItem = m_TreeView.GetItemDataForId<Progress.Item>(itemId);
            var treeViewItem = new TreeViewItemData<Progress.Item>(itemId, progressItem);

            var childrenIds = m_TreeView.viewController.GetChildrenIds(itemId);
            if (childrenIds != null)
            {
                var childrenItems = childrenIds.Select(id => GetExistingTreeViewItemFromId(id));
                treeViewItem.AddChildren(childrenItems.ToList());
            }

            return treeViewItem;
        }

        void AddTreeViewItemToTree(TreeViewItemData<Progress.Item> treeViewItem)
        {
            var siblings = GetSiblingItems(treeViewItem.data, out var newParentId);
            var insertionIndex = GetInsertionIndex(siblings, treeViewItem.data);

            var defaultController = m_TreeView.viewController as DefaultTreeViewController<Progress.Item>;
            defaultController.AddItem(treeViewItem, newParentId, insertionIndex);

            m_ContainedItems.Add(treeViewItem.id);
            if (m_MissingParents.TryGetValue(treeViewItem.id, out var orphans))
            {
                foreach (var orphanId in orphans)
                {
                    ReinsertItem(orphanId);
                }

                m_MissingParents.Remove(treeViewItem.id);
            }

            if (m_ItemsNeedingExpansion.Contains(treeViewItem.data.parentId))
            {
                m_TreeView.viewController.ExpandItem(treeViewItem.data.parentId, true);
                m_ItemsNeedingExpansion.Remove(treeViewItem.data.parentId);
            }
        }

        void RemoveTreeViewItem(int progressId)
        {
            m_TreeView.viewController.TryRemoveItem(progressId);
            m_ContainedItems.Remove(progressId);
        }

        // Internal functions, for testing only
        internal int GetIndexForProgressId(int progressId)
        {
            return m_TreeView.viewController.GetIndexForId(progressId);
        }

        internal VisualProgressItem GetVisualProgressItemAtIndex(int index)
        {
            var vi = m_TreeView.GetRootElementForIndex(index);
            return vi.Q<VisualProgressItem>(VisualProgressItem.visualElementName);
        }

        internal VisualProgressItem GetVisualProgressItem(int progressId)
        {
            var vi = m_TreeView.GetRootElementForId(progressId);
            return vi.Q<VisualProgressItem>(VisualProgressItem.visualElementName);
        }

        internal void ExpandAllItems()
        {
            m_TreeView.ExpandAll();
        }

        internal bool IsProgressIdInTree(int progressId)
        {
            // Calling ToList is needed here, as GetAllItems uses an internal stacked enumerator that is a member
            // of the viewController. If the iteration does not complete all the way, it messes with all other calls
            // to GetAllItems!
            var allIds = m_TreeView.viewController.GetAllItemIds().ToList();
            return allIds.Contains(progressId);
        }

        internal bool IsProgressExpanded(int progressId)
        {
            return !m_TreeView.viewController.HasChildren(progressId) || m_TreeView.IsExpanded(progressId);
        }
    }
}
