// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.ItemLibrary.Editor
{
    /// <summary>
    /// ItemLibrary TreeView element to display <see cref="ItemLibraryItem"/> in a collapsible hierarchy.
    /// </summary>
    class LibraryTreeView_Internal : ListView
    {
        public event Action<IReadOnlyList<ITreeItemView_Internal>> OnModelViewSelectionChange;

        public ResultsViewMode ViewMode { get; set; }

        internal const string k_FavoriteCategoryStyleName_Internal = "favorite-category";
        const string k_FavoriteCategoryHelp = "Contains all the items marked as favorites for this search context.\n" +
                                    "You can add or remove favorites by clicking the star icon on each search item.";

        public const string itemClassName = "unity-item-library-item";
        public const string customItemClassName = "item-library-custom-item";
        public const string itemNameClassName = itemClassName + "__name-label";
        public const string itemPathClassName = itemClassName + "__path-label";
        public const string itemCategoryClassName = itemClassName + "--category";
        public const string CategoryIconSuffix = "__icon";
        public const string itemCategoryIconClassName = itemClassName + CategoryIconSuffix;
        public const string collapseButtonClassName = itemClassName + "__collapse-button";
        public const string collapseButtonCollapsedClassName = collapseButtonClassName + "--collapsed";
        public const string favoriteButtonClassName = itemClassName + "__favorite-button";
        public const string favoriteButtonFavoriteClassName = "favorite";

        const int k_IndentDepthFactor = 15;
        const string k_EntryName = "smartSearchItem";
        const string k_FavoriteButtonname = "favoriteButton";

        const string k_ItemTemplateFileName = "UXML/ItemLibrary/Item.uxml";

        ItemLibraryLibrary_Internal m_Library;
        Action<ItemLibraryItem> m_ItemChosenCallback;
        HashSet<ItemLibraryItem> m_MultiSelectSelection;
        Dictionary<ItemLibraryItem, Toggle> m_SearchItemToVisualToggle;
        CategoryView_Internal m_FavoriteCategoryView;
        IReadOnlyList<ItemLibraryItem> m_Results;
        readonly VisualTreeAsset m_ItemTemplate;

        ICategoryView_Internal m_ResultsHierarchy;
        List<ITreeItemView_Internal> m_VisibleItems;
        Stack<ITreeItemView_Internal> m_RootItems;

        ItemLibraryItem m_LastFavoriteClicked;
        ITreeItemView_Internal m_LastItemViewClicked;

        double m_LastFavoriteClickTime;

        TypeHandleInfos m_TypeHandleInfos;

        public LibraryTreeView_Internal(TypeHandleInfos typeHandleInfos)
        {
            m_TypeHandleInfos = typeHandleInfos;
            m_MultiSelectSelection = new HashSet<ItemLibraryItem>();
            m_SearchItemToVisualToggle = new Dictionary<ItemLibraryItem, Toggle>();
            m_FavoriteCategoryView = new CategoryView_Internal("Favorites")
            {
                Help = k_FavoriteCategoryHelp,
                StyleName = k_FavoriteCategoryStyleName_Internal
            };
            m_VisibleItems = new List<ITreeItemView_Internal>();
            m_RootItems = new Stack<ITreeItemView_Internal>();

            m_ItemTemplate = EditorGUIUtility.Load(k_ItemTemplateFileName) as VisualTreeAsset;

            bindItem = Bind;
            unbindItem = UnBind;
            makeItem = MakeItem;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            itemsChosen += obj => OnItemChosen((obj.FirstOrDefault() as IItemView_Internal)?.Item);
            selectionChanged += _ => OnSelectionChanged();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);
        }

        public void Setup(ItemLibraryLibrary_Internal library, Action<ItemLibraryItem> selectionCallback)
        {
            m_Library = library;
            m_ItemChosenCallback = selectionCallback;

            // Add a single dummy Item to warn users that data is not ready to display yet
            m_VisibleItems = new List<ITreeItemView_Internal> { new PlaceHolderItemView() };
            RefreshListView();
        }

        internal void AddPortItemsToVisibleResults_Internal(IEnumerable<GraphNodeModelLibraryItem> portItems)
        {
            if (selectedItem is IItemView_Internal selectedItemView)
            {
                foreach (var portItem in portItems)
                {
                    if (selectedItemView.GetPortToConnect() != null)
                        selectedItemView.Parent.AddItem(new ItemView_Internal(selectedItemView.Parent, portItem));
                    else
                        selectedItemView.AddItem(new ItemView_Internal(selectedItemView, portItem));
                }
            }
            RegenerateVisibleResults();
        }

        void RegenerateVisibleResults()
        {
            m_VisibleItems.Clear();

            m_RootItems.Clear();

            for (var i = m_ResultsHierarchy.Items.Count - 1; i >= 0; i--)
            {
                m_RootItems.Push(m_ResultsHierarchy.Items[i]);
            }
            for (var i = m_ResultsHierarchy.SubCategories.Count - 1; i >= 0; i--)
            {
                m_RootItems.Push(m_ResultsHierarchy.SubCategories[i]);
            }

            var selectedItemView = selectedItem as IItemView_Internal;

            if (ViewMode == ResultsViewMode.Hierarchy)
            {
                m_FavoriteCategoryView.ClearItems();
                foreach (var favoriteItem in m_Library.CurrentFavorites.Where(f => m_Results.Contains(f)))
                {
                    IItemView_Internal itemViewToAdd = null;
                    if (selectedItemView != null && selectedItemView.IsInCategory(m_FavoriteCategoryView))
                    {
                        if (selectedItemView.GetPortToConnect() != null)
                        {
                            if (selectedItemView.Parent is IItemView_Internal parentItemView && parentItemView.Item == favoriteItem)
                                itemViewToAdd = parentItemView;
                        }
                        else if (selectedItemView.Item == favoriteItem)
                        {
                            itemViewToAdd = selectedItemView;
                        }
                    }
                    m_FavoriteCategoryView.AddItem(itemViewToAdd?? new ItemView_Internal(m_FavoriteCategoryView, favoriteItem));
                }

                m_RootItems.Push(m_FavoriteCategoryView);
            }

            while (m_RootItems.Count > 0)
            {
                var item = m_RootItems.Pop();
                m_VisibleItems.Add(item);

                if (item is IItemView_Internal itemView)
                {
                    if (itemView == selectedItemView)
                        selectedIndex = m_VisibleItems.Count - 1;

                    for (var i = itemView.Items.Count - 1; i >= 0; i--)
                    {
                        m_RootItems.Push(itemView.Items[i]);
                    }
                    itemView.ClearItems();
                }

                if (item is ICategoryView_Internal category && !m_Library.IsCollapsed_Internal(category))
                {
                    for (var i = category.Items.Count - 1; i >= 0; i--)
                    {
                        m_RootItems.Push(category.Items[i]);
                    }
                    for (var i = category.SubCategories.Count - 1; i >= 0; i--)
                    {
                        m_RootItems.Push(category.SubCategories[i]);
                    }
                }
            }

            RefreshListView();
        }

        internal void SetResults_Internal(IReadOnlyList<ItemLibraryItem> results)
        {
            var firstItemWasSelected = selectedIndex == 0;

            m_Results = results;

            m_ResultsHierarchy = CategoryView_Internal.BuildViewModels(m_Results, ViewMode, m_Library.CategoryPathStyleNames);

            RegenerateVisibleResults();

            SelectItemInListView(0);

            // force selection callback if first viewmodel was already selected
            if (firstItemWasSelected)
                OnModelViewSelectionChange?.Invoke(m_VisibleItems.Take(1).ToList());
        }

        void OnKeyDownEvent(KeyDownEvent evt)
        {
            var itemView = selectedItem as IItemView_Internal;
            var categoryView = itemView is null ? selectedItem as ICategoryView_Internal : null;

            switch (evt.keyCode)
            {
                case KeyCode.LeftArrow:
                    if (categoryView != null)
                    {
                        Collapse(categoryView);
                        evt.StopPropagation();
                    }
                    break;
                case KeyCode.RightArrow:
                    if (categoryView != null)
                    {
                        Expand(categoryView);
                        evt.StopPropagation();
                    }
                    break;
            }
        }

        void SelectItemInListView(int index)
        {
            if (index >= 0 && index < itemsSource.Count)
            {
                selectedIndex = index;
                ScrollToItem(index);
            }
        }

        void RefreshListView()
        {
            itemsSource = m_VisibleItems;
            m_SearchItemToVisualToggle.Clear();
            Rebuild();
        }

        /// <summary>
        /// Prepares a <see cref="VisualElement"/> to be (re-)used as a list item.
        /// </summary>
        /// <param name="target">The <see cref="VisualElement"/> to bind.</param>
        /// <param name="index">Index of the item in the items list.</param>
        void Bind(VisualElement target, int index)
        {
            var treeItem = m_VisibleItems[index];
            target.AddToClassList(itemClassName);
            var itemView = treeItem as IItemView_Internal;
            var categoryView = itemView is null ? treeItem as ICategoryView_Internal : null;
            target.EnableInClassList(itemCategoryClassName, categoryView != null);
            if (!string.IsNullOrEmpty(treeItem.StyleName))
                target.AddToClassList(GetItemCustomClassName(treeItem));

            var indent = target.Q<VisualElement>("itemIndent");
            indent.style.width = treeItem.Depth * k_IndentDepthFactor;

            var expander = target.Q<VisualElement>("itemChildExpander");

            var icon = expander.Query("expanderIcon").First();

            if (categoryView != null)
            {
                icon.AddToClassList(collapseButtonClassName);
                icon.EnableInClassList(collapseButtonCollapsedClassName, m_Library.IsCollapsed_Internal(categoryView));
            }

            var iconElement = target.Q<VisualElement>("itemIconVisualElement");
            iconElement.AddToClassList(itemCategoryIconClassName);

            if (!TryAddCompatiblePortIcon(iconElement, itemView) && !string.IsNullOrEmpty(treeItem.StyleName))
                iconElement.AddToClassList(GetItemCustomClassName(treeItem) + CategoryIconSuffix);

            var nameLabelsContainer = target.Q<VisualElement>("labelsContainer");
            nameLabelsContainer.Clear();

            var nameLabel = new Label(treeItem.Name);
            nameLabel.AddToClassList(itemNameClassName);
            nameLabelsContainer.Add(nameLabel);
            // TODO VladN: support highlight for parts of the string?
            // Highlight was disabled because it was inconsistent with fuzzy search
            // and with searching allowing to match item path (e.g. 'Debug/Log message' will be matched by DbgLM)
            // We need to figure out if there's a good way to highlight results.

            if ((treeItem.Parent == m_FavoriteCategoryView || ViewMode == ResultsViewMode.Flat)
                && !string.IsNullOrEmpty(treeItem.Path))
            {
                var pathLabel = new Label(PrettyPrintItemPath_Internal(treeItem.Path));
                pathLabel.AddToClassList(itemPathClassName);
                nameLabelsContainer.Add(pathLabel);
            }

            target.userData = treeItem;
            target.name = k_EntryName;

            var favButton = target.Q(k_FavoriteButtonname);
            if (favButton != null && itemView != null && itemView.GetPortToConnect() == null)
            {
                favButton.AddToClassList(favoriteButtonClassName);

                favButton.EnableInClassList(favoriteButtonFavoriteClassName,
                    m_Library.IsFavorite(itemView.Item));

                favButton.RegisterCallback<PointerDownEvent>(ToggleFavorite);
            }

            var selectionToggle = target.Q<Toggle>("itemToggle");
            if (selectionToggle != null)
            {
                if (categoryView != null)
                    selectionToggle.RemoveFromHierarchy();
                else if (itemView != null)
                {
                    var item = itemView.Item;
                    selectionToggle.SetValueWithoutNotify(m_MultiSelectSelection.Contains(item));
                    m_SearchItemToVisualToggle[item] = selectionToggle;
                }
            }
            target.RegisterCallback<MouseDownEvent>(ExpandOrCollapse);
        }

        bool TryAddCompatiblePortIcon(VisualElement iconElement, IItemView_Internal itemView)
        {
            var portToConnect = itemView?.GetPortToConnect();

            if (portToConnect != null && m_Library.Adapter is GraphNodeLibraryAdapter nodeLibraryAdapter && nodeLibraryAdapter.PreviewGraphView != null)
            {
                if (portToConnect.DataTypeHandle == TypeHandle.ExecutionFlow)
                {
                    var portToConnectModelView = portToConnect.GetView_Internal(nodeLibraryAdapter.PreviewGraphView);
                    if (portToConnectModelView is Port portToConnectUI)
                    {
                        iconElement.AddStylesheet_Internal("PortConnectorPart.uss");
                        iconElement.AddStylesheet_Internal("Port.uss");
                        iconElement.AddToClassList(Port.ussClassName);
                        iconElement.AddToClassList(Port.ussClassName.WithUssElement(Port.connectorPartName));
                        iconElement.AddToClassList(Port.outputModifierUssClassName);
                        iconElement.AddToClassList(Port.notConnectedModifierUssClassName);
                        iconElement.AddToClassList(Port.ussClassName.WithUssModifier("data-type-execution-flow"));
                        iconElement.AddToClassList(Port.ussClassName.WithUssModifier("type-execution"));
                        if (portToConnectUI.PortModel.Orientation == PortOrientation.Vertical)
                            iconElement.AddToClassList(Port.verticalModifierUssClassName);

                        var connectorBox = new VisualElement { name = PortConnectorPart.connectorUssName };
                        connectorBox.AddToClassList(Port.ussClassName.WithUssElement(PortConnectorPart.connectorUssName));
                        connectorBox.AddToClassList(PortConnectorPart.ussClassName.WithUssElement(PortConnectorPart.connectorUssName));
                        iconElement.Add(connectorBox);

                        portToConnectUI.RegisterCallback<CustomStyleResolvedEvent>(evt =>
                        {
                            if (evt.customStyle.TryGetValue(new CustomStyleProperty<Color>("--port-color"), out var portColorValue))
                            {
                                connectorBox.style.borderBottomColor = portColorValue;
                                connectorBox.style.borderLeftColor = portColorValue;
                                connectorBox.style.borderRightColor = portColorValue;
                                connectorBox.style.borderTopColor = portColorValue;
                            }
                            if (evt.customStyle.TryGetValue(new CustomStyleProperty<Color>("--port-background"), out var portBackgroundValue))
                            {
                                connectorBox.style.backgroundColor = portBackgroundValue;
                            }
                        });
                    }
                }
                else
                {
                    var imageIcon = new Image();
                    imageIcon.AddStylesheet_Internal("TypeIcons.uss");
                    var portToConnectModelView = portToConnect.GetView_Internal(nodeLibraryAdapter.PreviewGraphView);
                    imageIcon.tintColor = Color.white;
                    if (portToConnectModelView is Port portToConnectUI)
                    {
                        imageIcon.tintColor = portToConnectUI.PortColor;

                        portToConnectUI.RegisterCallback<CustomStyleResolvedEvent>(evt =>
                        {
                            if (evt.customStyle.TryGetValue(new CustomStyleProperty<Color>("--port-color"), out var portColorValue))
                                imageIcon.tintColor = portColorValue;
                        });
                    }

                    m_TypeHandleInfos.AddUssClasses(Port.dataTypeClassPrefix, imageIcon, portToConnect.DataTypeHandle);

                    iconElement.Add(imageIcon);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Text to display for the path of an item.
        /// Exposed to internal for tests.
        /// </summary>
        /// <param name="itemPath">The item which path to display.</param>
        /// <returns>A pretty version of the path for the item.</returns>
        internal static string PrettyPrintItemPath_Internal(string itemPath)
        {
            return "(in " + itemPath + ")";
        }

        /// <summary>
        /// Clears things before a list item <see cref="VisualElement"/> is potentially reused for another item.
        /// </summary>
        /// <param name="target">The <see cref="VisualElement"/> to clean.</param>
        /// <param name="index">Index of the item in the items list.</param>
        void UnBind(VisualElement target, int index)
        {
            target.RemoveFromClassList(itemCategoryClassName);
            RemoveCustomClassIfFound(target);
            target.UnregisterCallback<MouseDownEvent>(ExpandOrCollapse);

            var expander = target.Q<VisualElement>("itemChildExpander");
            var icon = expander.Query("expanderIcon").First();
            var iconElement = target.Q<VisualElement>("itemIconVisualElement");

            icon.RemoveFromClassList(collapseButtonClassName);
            icon.RemoveFromClassList(collapseButtonCollapsedClassName);

            iconElement.RemoveFromClassList(itemCategoryIconClassName);

            if (index > 0 && index < m_VisibleItems.Count && m_VisibleItems[index] is IItemView_Internal itemView)
            {
                var portToConnect = itemView.GetPortToConnect();
                if (portToConnect != null)
                {

                    m_TypeHandleInfos.RemoveUssClasses(Port.dataTypeClassPrefix, iconElement, portToConnect.DataTypeHandle);

                    for (var i = 0; i < iconElement.childCount; i++)
                    {
                        if (iconElement[i] is Image)
                        {
                            iconElement.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            RemoveCustomClassIfFound(iconElement);

            var favButton = target.Q(k_FavoriteButtonname);
            if (favButton != null)
            {
                favButton.RemoveFromClassList(favoriteButtonClassName);
                favButton.RemoveFromClassList(favoriteButtonFavoriteClassName);
                favButton.UnregisterCallback<PointerDownEvent>(ToggleFavorite);
            }

            void RemoveCustomClassIfFound(VisualElement visualElement)
            {
                var customClass = visualElement.GetClasses()
                    .FirstOrDefault(c => c.StartsWith(customItemClassName));
                if (customClass != null)
                    visualElement.RemoveFromClassList(customClass);
            }
        }

        /// <summary>
        /// Get the class name to give to an item with a custom style.
        /// Exposed to internals for tests purposes.
        /// </summary>
        /// <param name="styleName">Name of the style to apply.</param>
        /// <returns>The name of the class for the style name.</returns>
        internal static string GetCustomClassNameForStyle_Internal(string styleName)
        {
            return string.IsNullOrEmpty(styleName) ? null : customItemClassName + "-" + styleName;
        }

        static string GetItemCustomClassName(ITreeItemView_Internal item)
        {
            return GetCustomClassNameForStyle_Internal(item.StyleName);
        }

        internal void ConfirmMultiselect_Internal()
        {
            if (m_MultiSelectSelection.Count == 0)
            {
                m_ItemChosenCallback(null);
                return;
            }
            foreach (var item in m_MultiSelectSelection)
            {
                m_ItemChosenCallback(item);
            }
        }

        /// <summary>
        /// Clicks on favorite actually can't intercept the click on the list view.
        /// So we keep track off every click on favorites to prevent triggering selection when clicking favorites.
        /// </summary>
        bool SelectionIsInvalidOrAFavoriteClick()
        {
            var selectedItemView = (selectedItem as IItemView_Internal)?.Item;
            if (EditorApplication.timeSinceStartup - m_LastFavoriteClickTime > .8)
                return false;

            return selectedItemView == null || m_LastFavoriteClicked == selectedItemView;
        }

        void OnSelectionChanged()
        {
            if (SelectionIsInvalidOrAFavoriteClick())
                return;

            var selectedItemView = selectedItem as IItemView_Internal;
            if (m_LastItemViewClicked == selectedItemView)
                return;

            m_LastItemViewClicked = selectedItemView;

            if (!selectedItems.Any())
                m_ItemChosenCallback(null);
            else
                OnModelViewSelectionChange?.Invoke(selectedItems
                    .OfType<ITreeItemView_Internal>()
                    .ToList());
        }

        void OnItemChosen(ItemLibraryItem item)
        {
            if (SelectionIsInvalidOrAFavoriteClick())
                return;

            if (item == null)
                m_ItemChosenCallback(null);
            else if (m_LastFavoriteClicked != item || EditorApplication.timeSinceStartup - m_LastFavoriteClickTime > 1.0)
            {
                if (!m_Library.Adapter.MultiSelectEnabled)
                {
                    m_ItemChosenCallback(item);
                }
                else
                {
                    ToggleItemForMultiSelect(item, !m_MultiSelectSelection.Contains(item));
                }
            }
        }

        void ToggleItemForMultiSelect(ItemLibraryItem item, bool selected)
        {
            if (selected)
            {
                m_MultiSelectSelection.Add(item);
            }
            else
            {
                m_MultiSelectSelection.Remove(item);
            }

            if (m_SearchItemToVisualToggle.TryGetValue(item, out var toggle))
            {
                toggle.SetValueWithoutNotify(selected);
            }
        }

        VisualElement MakeItem()
        {
            var itemElement = m_ItemTemplate.CloneTree();
            if (m_Library.Adapter.MultiSelectEnabled)
            {
                var selectionToggle = itemElement.Q<Toggle>("itemToggle");
                if (selectionToggle != null)
                {
                    selectionToggle.RegisterValueChangedCallback(changeEvent =>
                    {
                        var item = itemElement.userData as ItemLibraryItem;
                        ToggleItemForMultiSelect(item, changeEvent.newValue);
                    });
                }
            }
            return itemElement;
        }

        // ReSharper disable once UnusedMember.Local

        void RefreshListViewOn()
        {
            // TODO: Call ListView.Refresh() when it is fixed.
            // Need this workaround until then.
            // See: https://fogbugz.unity3d.com/f/cases/1027728/
            // And: https://gitlab.internal.unity3d.com/upm-packages/editor/com.unity.library/issues/9

            var scroller = scrollView?.Q<Scroller>("VerticalScroller");
            if (scroller == null)
                return;

            var oldValue = scroller.value;
            scroller.value = oldValue + 1.0f;
            scroller.value = oldValue - 1.0f;
            scroller.value = oldValue;
        }

        void Expand(ICategoryView_Internal itemView)
        {
            m_Library.SetCollapsed_Internal(itemView, false);
            RegenerateVisibleResults();
        }

        void Collapse(ICategoryView_Internal itemView)
        {
            m_Library.SetCollapsed_Internal(itemView);
            RegenerateVisibleResults();
        }

        void ToggleFavorite(PointerDownEvent evt)
        {
            // Check that we're clicking on a favorite
            if (!(evt.target is VisualElement target
                  && target.name == k_FavoriteButtonname
                  && target.parent?.parent?.userData is ItemView_Internal itemView))
            {
                return;
            }

            // Prevent ListView from selecting the item under the favorite icon
            evt.StopPropagation();

            var item = itemView.Item;
            var wasFavorite = m_Library.IsFavorite(item);
            m_Library.SetFavorite(item, !wasFavorite);
            m_LastFavoriteClicked = item;
            m_LastFavoriteClickTime = EditorApplication.timeSinceStartup;
            target.EnableInClassList(favoriteButtonFavoriteClassName, !wasFavorite);

            RegenerateVisibleResults();

            // Compensate list shrinking/growing when we add/remove favorites.
            // Avoids having the selection and item under the mouse cursor to jump around when adding/removing favorites.
            if (!m_Library.IsCollapsed_Internal(m_FavoriteCategoryView))
            {
                var scroller = scrollView?.Q<Scroller>();
                if (scroller != null)
                {
                    var scrolledBot = scroller.value >= scroller.highValue;
                    if (!(scrolledBot && wasFavorite))
                    {
                        var selectionDelta = wasFavorite ? -1 : 1;
                        selectedIndex += selectionDelta;
                        var scrollerDelta = selectionDelta * fixedItemHeight;
                        scroller.value += scrollerDelta;
                    }
                }
            }
        }

        void ExpandOrCollapse(MouseDownEvent evt)
        {
            if (!(evt.target is VisualElement target))
                return;

            VisualElement itemElement = target.GetFirstAncestorOfType<TemplateContainer>();
            var expandingItemName = "expanderIcon";
            if (target.name != expandingItemName)
                target = itemElement.Q(expandingItemName);

            if (target == null || !(itemElement?.userData is ICategoryView_Internal item) || itemElement.userData is IItemView_Internal)
                return;

            if (m_Library.IsCollapsed_Internal(item))
                Expand(item);
            else
                Collapse(item);

            evt.StopImmediatePropagation();
        }

        class PlaceHolderItemView : ITreeItemView_Internal
        {
            public ICategoryView_Internal Parent => null;
            public string StyleName => null;
            public int Depth => 0;
            public string Path => null;
            public string Name => "Indexing databases...";
            public string Help => "The Database is being indexed...";
        }
    }
}
