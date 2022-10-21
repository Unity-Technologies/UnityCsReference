// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    abstract class SearchBaseCollectionView<T> : SearchElement, IResultView where T : BaseVerticalCollectionView
    {
        const float k_NormalItemHeight = 40f;

        private bool m_Disposed;
        private Delayer m_Throttler;

        protected T m_ListView;

        Rect IResultView.rect => worldBound;
        float IResultView.itemSize => m_ViewModel.itemIconSize;
        public virtual bool showNoResultMessage => true;

        public SearchBaseCollectionView(string name, ISearchView viewModel, string className)
            : base(name, viewModel, className)
        {
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            m_Throttler = Delayer.Throttle(o =>
            {
                UpdateView();
            }, SearchView.resultViewUpdateThrottleDelay, true);

            m_ListView.selectedIndicesChanged += HandleItemsSelected;
            m_ListView.itemsChosen += OnItemsChosen;
            On(SearchEvent.SelectionHasChanged, OnSelectionChanged);
            On(SearchEvent.DisplayModeChanged, OnDisplayModeChanged);
            On(SearchEvent.RefreshContent, OnRefreshContent);

            RegisterGlobalEventHandler<KeyDownEvent>(OnKeyNavigation, 20);
            RegisterCallback<PointerDownEvent>(OnPointerDown);

            // Update selection just in case it was already modified
            UpdateSelection();
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<PointerDownEvent>(OnPointerDown);
            UnregisterGlobalEventHandler<KeyDownEvent>(OnKeyNavigation);

            Off(SearchEvent.RefreshContent, OnRefreshContent);
            Off(SearchEvent.DisplayModeChanged, OnDisplayModeChanged);
            Off(SearchEvent.SelectionHasChanged, OnSelectionChanged);
            m_ListView.itemsChosen -= OnItemsChosen;
            m_ListView.selectedIndicesChanged -= HandleItemsSelected;

            m_Throttler?.Dispose();

            base.OnDetachFromPanel(evt);
        }

        private void OnItemsChosen(IEnumerable<object> chosenItems)
        {
            var convertedItems = chosenItems.Select(item => (SearchItem)item).ToArray();
            m_ViewModel.ExecuteAction(null, convertedItems, !SearchSettings.keepOpen);
        }

        private void OnDisplayModeChanged(ISearchEvent evt)
        {
            UpdateItemSize();
            UpdateSelection();
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
                    m_Throttler.Dispose();
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

        void IResultView.Focus() => m_ListView.Focus();
        public virtual void Refresh(RefreshFlags flags) { }
        void IResultView.OnGroupChanged(string prevGroupId, string newGroupId) => OnGroupChanged(prevGroupId, newGroupId);

        protected virtual void OnGroupChanged(string prevGroupId, string newGroupId)
        {
            Refresh();
        }

        private void OnRefreshContent(ISearchEvent evt)
        {
            var flags = evt.GetArgument<RefreshFlags>(0);
            if (flags.HasAny(RefreshFlags.ItemsChanged | RefreshFlags.QueryCompleted))
                Refresh();
        }

        protected void Refresh()
        {
            // We are throttling the update of the view so that we don't call
            // RefreshItems on the list view too often. We chose a throttle
            // delay of 50ms (20fps), which we believe still gives a good enough visual
            // feedback. If the throttler is not currently throttling, the execution will go through
            // which means there is no delay.
            m_Throttler?.Execute();
        }

        protected virtual void UpdateView()
        {
            if (ShouldRefreshView())
                m_ListView.RefreshItems();
        }

        private bool ShouldRefreshView()
        {
            if (((IList)m_ListView.activeItems).Count < m_ViewModel.results.Count)
                return true;
            foreach (var e in m_ListView.activeItems)
            {
                if (e.index < 0 || e.index >= m_ViewModel.results.Count)
                    return true;

                if (m_ViewModel.results[e.index].id.GetHashCode() != e.rootElement.name.GetHashCode())
                    return true;
            }

            return false;
        }

        protected virtual void UpdateItemSize()
        {
            m_ListView.fixedItemHeight = GetItemHeight();
            m_ListView.Rebuild();
        }

        protected virtual float GetItemHeight()
        {
            return k_NormalItemHeight;
        }

        protected virtual void UpdateSelection()
        {
            var selectedIndexes = m_ViewModel.selection.indexes;
            var firstSelection = selectedIndexes.Count > 0 ? selectedIndexes[0] : -1;
            m_ListView.SetSelectionWithoutNotify(selectedIndexes);
            if (firstSelection != -1)
                Utils.CallDelayed(() => m_ListView.ScrollToItem(firstSelection), 0.01d);
        }

        private void HandleItemsSelected(IEnumerable<int> selection)
        {
            var selArray = selection.ToArray();
            m_ViewModel.SetSelection(selArray);
            Dispatcher.Emit(SearchEvent.SelectionHasChanged, new SearchEventPayload(this, selection));
        }

        private void OnSelectionChanged(ISearchEvent evt)
        {
            UpdateSelection();
        }

        private static bool IsValidKey(IKeyboardEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    return !evt.ctrlKey;
                case KeyCode.DownArrow:
                case KeyCode.UpArrow:
                    return true;

                case KeyCode.PageUp:
                case KeyCode.PageDown:
                    return true;
            }

            return false;
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            var itemCount = m_ListView.itemsSource.Count;
            var currentIndex = m_ListView.selectedIndex == -1 ? -1 : m_ListView.selectedIndices.Last();
            var nextSelectedIndex = -1;
            if (evt.direction == NavigationMoveEvent.Direction.Down)
                WrapNextSelectedItem(currentIndex, itemCount, ref nextSelectedIndex);
            else if (evt.direction == NavigationMoveEvent.Direction.Up)
                WrapPreviousSelectedItem(currentIndex, itemCount, ref nextSelectedIndex);

            VerifySelectionChanged(currentIndex, nextSelectedIndex, evt);

            m_ListView.UnregisterCallback<NavigationMoveEvent>(OnNavigationMove);
            evt.StopImmediatePropagation();
            evt.PreventDefault();
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
                    m_ListView.selectedIndex = nextSelectedIndex;
                else
                {
                    if (!m_ListView.selectedIndices.Contains(nextSelectedIndex))
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
                        m_ListView.AddToSelection(newSelection);
                    }
                    else
                    {
                        if (nextSelectedIndex > currentIndex)
                        {
                            for (int i = currentIndex; i < nextSelectedIndex; ++i)
                                m_ListView.RemoveFromSelection(i);
                        }
                        else
                        {
                            for (int i = currentIndex; i > nextSelectedIndex; --i)
                                m_ListView.RemoveFromSelection(i);
                        }

                        m_ListView.RemoveFromSelection(currentIndex);
                    }
                }

                m_ListView.ScrollToItem(nextSelectedIndex);
            }

            return selectionHasChanged;
        }

        private bool OnKeyNavigation(KeyDownEvent evt)
        {
            if (evt.target is not VisualElement ve || !IsValidKey(evt))
                return false;

            var currentIndex = m_ListView.selectedIndex == -1 ? -1 : m_ListView.selectedIndices.Last();
            var itemCount = m_ListView.itemsSource.Count;
            if (m_ListView == ve || m_ListView.Contains(ve))
            {
                if ((currentIndex == itemCount - 1 && evt.keyCode == KeyCode.DownArrow) || (currentIndex == 0 && evt.keyCode == KeyCode.UpArrow))
                    m_ListView.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);

                return false;
            }

            if ((evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && m_ListView.selectedIndex != -1)
            {
                m_ViewModel.ExecuteAction(null, m_ListView.selectedItems.Cast<SearchItem>().ToArray(), !SearchSettings.keepOpen);
                return true;
            }

            var nextSelectedIndex = -1;
            if (evt.keyCode == KeyCode.DownArrow)
            {
                WrapNextSelectedItem(currentIndex, itemCount, ref nextSelectedIndex);
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                WrapPreviousSelectedItem(currentIndex, itemCount, ref nextSelectedIndex);
            }
            else if (evt.keyCode == KeyCode.PageDown)
            {
                if (currentIndex == -1)
                {
                    if (itemCount > 0)
                        nextSelectedIndex = m_ListView.activeItems.LastOrDefault()?.index ?? -1;
                }
                else
                    nextSelectedIndex = Math.Min(currentIndex + m_ListView.activeItems.Count(), itemCount - 1);
            }
            else if (evt.keyCode == KeyCode.PageUp)
            {
                if (currentIndex == -1)
                {
                    if (itemCount > 0)
                        nextSelectedIndex = currentIndex = itemCount - 1;
                }
                else if (itemCount > 0)
                    nextSelectedIndex = Math.Max(currentIndex - m_ListView.activeItems.Count(), 0);
            }

            return VerifySelectionChanged(currentIndex, nextSelectedIndex, evt);
        }

        protected virtual void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.clickCount != 1 && evt.button != 0)
                return;

            if (evt.target is not VisualElement ve)
                return;

            if (ve is not SearchViewItem && ve.GetFirstAncestorOfType<SearchViewItem>() == null)
                m_ListView.ClearSelection();
        }
    }
}
