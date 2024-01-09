// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Search.GridView.KeyboardGridNavigationManipulator;

namespace UnityEditor.Search
{
    class SearchGridViewItem : SearchViewItem
    {
        public static readonly string ussClassName = "search-grid-view-item";
        public static readonly string labelClassName = ussClassName.WithUssElement("label");
        public static readonly string iconClassName = ussClassName.WithUssElement("icon");
        public static readonly string favoriteButtonClassName = ussClassName.WithUssElement("favorite-button");
        public static readonly string thumbnailClassName = ussClassName.WithUssElement("thumbnail");

        public SearchGridViewItem(ISearchView viewModel)
            : base(string.Empty, viewModel, ussClassName)
        {
            m_Label.AddToClassList(labelClassName);
            m_Thumbnail.AddToClassList(iconClassName);
            m_FavoriteButton.AddToClassList(favoriteButtonClassName);

            var thumbnailElement = new VisualElement();
            thumbnailElement.AddToClassList(thumbnailClassName);
            thumbnailElement.Add(m_Thumbnail);
            thumbnailElement.Add(m_FavoriteButton);

            Add(thumbnailElement);
            Add(m_Label);

            style.flexDirection = FlexDirection.Column;
        }

        public override void Bind(in SearchItem item)
        {
            base.Bind(item);

            m_Label.tooltip = item.GetDescription(context, stripHTML: true);
        }
    }

    class SearchGridView : SearchElement, IResultView
    {
        private bool m_Disposed;
        private Delayer m_Throttler;
        private readonly GridView m_GridView;
        private const float m_LabelHeight = 23f;

        Rect IResultView.rect => worldBound;
        float IResultView.itemSize => m_ViewModel.itemIconSize;
        bool IResultView.showNoResultMessage => true;

        public SearchGridView(ISearchView viewModel)
            : base("SearchGridView", viewModel)
        {
            m_GridView = new GridView((IList)viewModel.results, GetItemSize(), GetItemSize() + m_LabelHeight, MakeItem, BindItem)
            {
                unbindItem = UnbindItem,
                destroyItem = DestroyItem,
                selectionType = SelectionType.Multiple
            };

            Add(m_GridView);
        }


        int IResultView.ComputeVisibleItemCapacity(float width, float height)
        {
            // Approximation of how many we can fit.
            width /= m_ViewModel.itemIconSize;
            height /= m_ViewModel.itemIconSize;
            return (int)(width * height);
        }

        private float GetItemSize()
        {
            if (m_ViewModel.itemIconSize == 0)
                return (float)DisplayMode.Grid;

            return m_ViewModel.itemIconSize;
        }

        private VisualElement MakeItem()
        {
            return new SearchGridViewItem(m_ViewModel);
        }

        private void BindItem(VisualElement element, int index)
        {
            var e = (SearchGridViewItem)element;
            if (index >= 0 && index < m_ViewModel.results.Count)
                e.Bind(m_ViewModel.results[index]);
        }

        private void UnbindItem(VisualElement element, int index)
        {
            var e = (SearchGridViewItem)element;
            e.Unbind();
        }

        private void DestroyItem(VisualElement element)
        {
            var e = (SearchGridViewItem)element;
            e.Destroy();
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            m_Throttler = Delayer.Throttle(o =>
            {
                UpdateView();
            }, SearchView.resultViewUpdateThrottleDelay, true);

            m_GridView.itemsBuilt += UpdateSelection;
            m_GridView.itemsChosen += OnItemsChosen;
            m_GridView.selectedIndicesChanged += HandleItemsSelected;
            On(SearchEvent.SelectionHasChanged, OnSelectionChanged);
            On(SearchEvent.RefreshContent, OnRefreshContent);

            RegisterGlobalEventHandler<KeyDownEvent>(OnKeyNavigation, 20);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_Throttler?.Dispose();

            UnregisterCallback<PointerDownEvent>(OnPointerDown);
            UnregisterGlobalEventHandler<KeyDownEvent>(OnKeyNavigation);

            Off(SearchEvent.RefreshContent, OnRefreshContent);
            Off(SearchEvent.SelectionHasChanged, OnSelectionChanged);
            m_GridView.selectedIndicesChanged -= HandleItemsSelected;
            m_GridView.itemsChosen -= OnItemsChosen;
            m_GridView.itemsBuilt -= UpdateSelection;

            base.OnDetachFromPanel(evt);
        }

        private void OnItemsChosen(IEnumerable<object> chosenItems)
        {
            var convertedItems = chosenItems.Select(item => (SearchItem)item).ToArray();
            m_ViewModel.ExecuteAction(null, convertedItems, !SearchSettings.keepOpen);
        }

        private void OnRefreshContent(ISearchEvent evt)
        {
            var flags = evt.GetArgument(0, RefreshFlags.Default);
            if (flags.HasAny(RefreshFlags.ItemsChanged))
            {
                Refresh();
            }
        }

        private void OnSelectionChanged(ISearchEvent evt)
        {
            if (evt.sourceElement != this)
                UpdateSelection();
        }

        private void HandleItemsSelected(IEnumerable<int> selection)
        {
            m_ViewModel.SetSelection(selection.ToArray());
            Dispatcher.Emit(SearchEvent.SelectionHasChanged, new SearchEventPayload(this, selection));
        }

        private bool IsValidKey(IKeyboardEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.DownArrow:
                case KeyCode.UpArrow:
                case KeyCode.RightArrow:
                case KeyCode.LeftArrow:
                    return true;

                case KeyCode.PageUp:
                case KeyCode.PageDown:
                    return true;
            }

            return false;
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            var itemCount = m_GridView.itemsSource.Count;
            var currentIndex = m_GridView.selectedIndex == -1 ? -1 : m_GridView.selectedIndices.Last();
            var nextSelectedIndex = -1;
            if (evt.direction == NavigationMoveEvent.Direction.Right)
                WrapNextSelectedItem(currentIndex, itemCount, ref nextSelectedIndex);
            else if (evt.direction == NavigationMoveEvent.Direction.Left)
                WrapPreviousSelectedItem(currentIndex, itemCount, ref nextSelectedIndex);

            VerifySelectionChanged(currentIndex, nextSelectedIndex, evt);

            m_GridView.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            evt.StopImmediatePropagation();
        }

        private void WrapNextSelectedItem(int currentIndex, int itemCount, ref int nextSelectedIndex)
        {
            if (currentIndex == -1)
            {
                if (itemCount > 0)
                    nextSelectedIndex = 0;
            }
            else
                nextSelectedIndex = Utils.Wrap(currentIndex + 1, itemCount);
        }

        private void WrapPreviousSelectedItem(int currentIndex, int itemCount, ref int nextSelectedIndex)
        {
            if (currentIndex == -1)
            {
                if (itemCount > 0)
                    nextSelectedIndex = itemCount - 1;
            }
            else if (itemCount > 0)
                nextSelectedIndex = Utils.Wrap(currentIndex - 1, itemCount);
        }

        private bool VerifySelectionChanged(int currentIndex, int nextSelectedIndex, EventBase evt)
        {
            var selectionHasChanged = currentIndex != nextSelectedIndex;
            if (selectionHasChanged && nextSelectedIndex != -1)
            {
                var shiftKey = evt is KeyDownEvent kde && kde.shiftKey || evt is INavigationEvent ne && ne.shiftKey;
                if (!shiftKey)
                    m_GridView.selectedIndex = nextSelectedIndex;
                else
                {
                    if (!m_GridView.selectedIndices.Contains(nextSelectedIndex))
                    {
                        var newSelection = new List<int>();
                        if (nextSelectedIndex > currentIndex)
                        {
                            for (int i = currentIndex++; i <= nextSelectedIndex; ++i)
                                newSelection.Add(i);
                        }
                        else
                        {
                            for (int i = currentIndex--; i >= nextSelectedIndex; --i)
                                newSelection.Add(i);
                        }
                        m_GridView.AddToSelection(newSelection);
                    }
                    else
                    {
                        if (nextSelectedIndex > currentIndex)
                        {
                            for (int i = currentIndex; i < nextSelectedIndex; ++i)
                                m_GridView.RemoveFromSelection(i);
                        }
                        else
                        {
                            for (int i = currentIndex; i > nextSelectedIndex; --i)
                                m_GridView.RemoveFromSelection(i);
                        }

                        m_GridView.RemoveFromSelection(currentIndex);
                    }
                }

                m_GridView.ScrollToItem(nextSelectedIndex);
            }

            return selectionHasChanged;
        }

        private bool OnKeyNavigation(KeyDownEvent evt)
        {
            if (evt.target is not VisualElement ve || !IsValidKey(evt))
                return false;

            // In focus.
            var currentIndex = m_GridView.selectedIndex == -1 ? -1 : m_GridView.selectedIndices.Last();
            var itemCount = m_GridView.itemsSource.Count;
            if (m_GridView == ve || m_GridView.Contains(ve))
            {
                if ((currentIndex == itemCount - 1 && evt.keyCode == KeyCode.RightArrow) || (currentIndex == 0 && evt.keyCode == KeyCode.LeftArrow))
                    m_GridView.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);

                return false;
            }

            // Key handling when GridView is not in focus.
            if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && m_GridView.selectedIndex != -1)
            {
                var items = m_GridView.selectedItems.Cast<SearchItem>().ToArray();
                var action = evt.altKey ? SearchView.GetSecondaryAction(m_ViewModel.selection, items) : SearchView.GetDefaultAction(m_ViewModel.selection, items);
                m_ViewModel.ExecuteAction(action, items, !SearchSettings.keepOpen);
                return true;
            }

            var nextSelectedIndex = -1;
            var selectionHasChanged = false;
            if (evt.keyCode == KeyCode.DownArrow)
            {
                WrapNextSelectedItem(currentIndex, itemCount, ref nextSelectedIndex);
                selectionHasChanged = VerifySelectionChanged(currentIndex, nextSelectedIndex, evt);
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                WrapPreviousSelectedItem(currentIndex, itemCount, ref nextSelectedIndex);
                selectionHasChanged = VerifySelectionChanged(currentIndex, nextSelectedIndex, evt);
            }
            if (evt.keyCode == KeyCode.PageDown)
            {
                m_GridView.Apply(KeyboardGridNavigationOperation.PageDown, evt);
                selectionHasChanged = m_GridView.selectedIndex != (m_GridView.selectedIndices.Count() == 0 ? -1 : m_GridView.selectedIndices.Last());
            }
            else if (evt.keyCode == KeyCode.PageUp)
            {
                m_GridView.Apply(KeyboardGridNavigationOperation.PageUp, evt);
                selectionHasChanged = m_GridView.selectedIndex != (m_GridView.selectedIndices.Count() == 0 ? -1 : m_GridView.selectedIndices.Last());
            }

            return selectionHasChanged;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.clickCount != 1 && evt.button != 0)
                return;

            if (evt.target is not VisualElement ve)
                return;

            if (ve is not SearchViewItem && ve.GetFirstAncestorOfType<SearchViewItem>() == null)
                m_GridView.ClearSelection();
        }

        void IResultView.Refresh(RefreshFlags flags)
        {
            if (flags.HasAny(RefreshFlags.ItemsChanged | RefreshFlags.GroupChanged | RefreshFlags.QueryCompleted))
            {
                Refresh();
            }
            else if (flags.HasAny(RefreshFlags.DisplayModeChanged))
            {
                m_GridView.fixedItemHeight = m_ViewModel.itemIconSize + m_LabelHeight;
                m_GridView.fixedItemWidth = m_ViewModel.itemIconSize;
                m_GridView.Rebuild();
            }
        }

        private void UpdateSelection()
        {
            if (m_ViewModel == null || m_ViewModel.selection == null)
                return;
            
            var selectedIndexes = m_ViewModel.selection.indexes;
            if (m_GridView.selectedIndices.SequenceEqual(selectedIndexes))
                return;
            var firstSelection = selectedIndexes.Count > 0 ? selectedIndexes[0] : -1;
            m_GridView.SetSelectionWithoutNotify(selectedIndexes);
            if (firstSelection != -1)
                m_GridView.ScrollToItem(firstSelection);
        }

        void IResultView.OnGroupChanged(string prevGroupId, string newGroupId)
        {
            // Do nothing
        }

        private void UpdateView()
        {
            m_GridView.fixedItemHeight = m_ViewModel.itemIconSize + m_LabelHeight;
            m_GridView.fixedItemWidth = m_ViewModel.itemIconSize;
            m_GridView.RefreshItems();
        }

        void IResultView.AddSaveQueryMenuItems(SearchContext context, GenericMenu menu)
        {
            // Nothing to do
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    m_Throttler?.Dispose();
                    m_Throttler = null;
                }

                m_Disposed = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        void Refresh()
        {
            // We are throttling the update of the view so that we don't call
            // RefreshItems on the list view too often. We chose a throttle
            // delay of 50ms (20fps), which we believe still gives a good enough visual
            // feedback. If the throttler is not currently throttling, the execution will go through
            // which means there is no delay.
            m_Throttler?.Execute();
        }
    }
}
