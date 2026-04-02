// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    abstract class SearchBaseCollectionView<T> : SearchElement, IResultView where T : BaseVerticalCollectionView
    {
        const float k_NormalItemHeight = 40f;

        private bool m_Disposed;
        private Delayer m_Throttler;
        private Action m_UpdateSelectionOffHandler;
        private SearchResultViewGlobalEventHandler m_GlobalKeyboardHandler;

        protected T m_ListView;

        public abstract string ViewId { get; }
        public virtual bool ShowNoResultMessage => true;
        public virtual bool UpdateNeeded => false;
        public event IResultView.SelectionChangedEventHandler SelectionChanged;

        // This event is not used in the SearchBaseCollectionView
        public event IResultView.PopulateItemsContextMenuHandler PopulateItemsContextMenu
        {
            add { }
            remove { }
        }

        public SearchBaseCollectionView(string name, ISearchView viewModel, string className)
            : base(name, viewModel, className)
        {
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            var targetHandler = m_ListView.Q<ScrollView>().contentContainer;
            m_GlobalKeyboardHandler ??= new SearchResultViewGlobalEventHandler(
                this,
                targetHandler,
                null,
                GetCurrentIndex,
                GetItemCount,
                SetSelectedIndex,
                SelectionContains,
                AddToSelection,
                RemoveFromSelection,
                Frame,
                GetVisibleItemCount,
                null);

            m_Throttler = Delayer.Throttle(o =>
            {
                UpdateView();
            }, TimeSpan.FromSeconds(SearchView.resultViewUpdateThrottleDelay), true);

            m_ListView.selectedIndicesChanged += HandleItemsSelected;
            m_ListView.itemsChosen += OnItemsChosen;
            On(SearchEvent.DisplayModeChanged, OnDisplayModeChanged);

            m_GlobalKeyboardHandler.RegisterGlobalEventHandlers();
            RegisterCallback<PointerDownEvent>(OnPointerDown);

            // Update selection just in case it was already modified
            SetSelectionWithoutNotify(m_ViewModel.selection);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_GlobalKeyboardHandler.UnregisterGlobalEventHandler();

            Off(SearchEvent.DisplayModeChanged, OnDisplayModeChanged);
            m_ListView.itemsChosen -= OnItemsChosen;
            m_ListView.selectedIndicesChanged -= HandleItemsSelected;

            // Make sure to remove callbacks when Detaching from panel
            m_UpdateSelectionOffHandler?.Invoke();
            m_UpdateSelectionOffHandler = null;

            m_Throttler?.Dispose();

            base.OnDetachFromPanel(evt);
        }

        private void OnItemsChosen(IEnumerable<object> chosenItems)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var convertedItems = chosenItems.Select(item => (SearchItem)item).ToArray();
#pragma warning restore UA2001
            m_ViewModel.ExecuteAction(null, convertedItems, true);
        }

        private void OnDisplayModeChanged(ISearchEvent evt)
        {
            UpdateItemSize();
            SetSelectionWithoutNotify(m_ViewModel.selection);
        }

        public virtual void AddSaveQueryMenuItems(SearchContext context, GenericMenu menu)
        {
            // Nothing to do
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    // Dispose can be called without attaching to a panel first, so some members could still be null.
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

        void IResultView.Focus() => m_ListView.Focus();
        public virtual void Refresh(RefreshFlags flags)
        {
            if (flags.HasAny(RefreshFlags.ItemsChanged))
            {
                Refresh();
            }
            if (flags.HasAny(RefreshFlags.DisplayModeChanged))
            {
                OnDisplayModeChanged(null);
            }
        }

        void IResultView.OnGroupChanged(string prevGroupId, string newGroupId) => OnGroupChanged(prevGroupId, newGroupId);

        void IResultView.OnItemSourceChanged(ISearchList itemSource)
        {
            if (m_ListView != null)
                m_ListView.itemsSource = (IList)itemSource;
        }

        protected virtual void OnGroupChanged(string prevGroupId, string newGroupId)
        {
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
            m_ListView.RefreshItems();
        }

        void IResultView.UpdateView()
        {
            UpdateView();
        }

        bool IResultView.UpdateViewIncremental()
        {
            return false;
        }

        bool IResultView.UpdateViewIncrementalTimed(TimeSpan timeLimit)
        {
            return false;
        }

        void IResultView.SetSearchItemComparer(IComparer<SearchItem> searchItemComparer)
        {
            UpdateView();
        }

        public virtual void SetSelectionWithoutNotify(SearchSelection selection)
        {
            if (selection == null)
                return;

            var selectedIndexes = selection.indexes;
            #pragma warning disable UA2014 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (m_ListView.selectedIndicesList.SequenceEqual(selectedIndexes))
                #pragma warning restore UA2014
                return;

            var firstSelection = selectedIndexes.Count > 0 ? selectedIndexes[0] : -1;
            m_ListView.SetSelectionWithoutNotify(selectedIndexes);
            if (firstSelection != -1)
            {
                m_UpdateSelectionOffHandler?.Invoke();
                m_UpdateSelectionOffHandler = Utils.CallDelayed(() => m_ListView.ScrollToItem(firstSelection), 0.01d);
            }
        }

        protected virtual void UpdateItemSize()
        {
            if (m_ListView.fixedItemHeight != GetItemHeight())
            {
                m_ListView.fixedItemHeight = GetItemHeight();
                m_ListView.Rebuild();
            }
        }

        protected virtual float GetItemHeight()
        {
            return k_NormalItemHeight;
        }

        private void HandleItemsSelected(IEnumerable<int> selection)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selArray = selection.ToArray();
#pragma warning restore UA2001
            SelectionChanged?.Invoke(selArray);
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

        #region Global Event Handler
        int GetCurrentIndex()
        {
            if (m_ListView.selectedIndex == -1)
                return -1;
            return m_ListView.selectedIndicesList[^1];
        }

        int GetItemCount()
        {
            return m_ListView.itemsSource.Count;
        }

        void SetSelectedIndex(int nextSelectedIndex)
        {
            m_ListView.selectedIndex = nextSelectedIndex;
        }

        int IResultView.ComputeVisibleItemCapacity(float size, float height)
        {
            return (int)(height / GetItemHeight()) + 10;
        }

        bool SelectionContains(int index)
        {
            return m_ListView.selectedIndicesList.Contains(index);
        }

        void AddToSelection(ReadOnlySpan<int> newIndices)
        {
            m_ListView.AddToSelection(newIndices);
        }

        void RemoveFromSelection(int index)
        {
            m_ListView.RemoveFromSelection(index);
        }

        void Frame(int index)
        {
            m_ListView.ScrollToItem(index);
        }

        int GetVisibleItemCount()
        {
            return m_ListView.activeItems.Count;
        }
        #endregion
    }
}
