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
        private readonly SearchResultViewGlobalEventHandler m_GlobalKeyboardHandler;

        internal static string resultViewId = "grid";
        public string ViewId => resultViewId;
        bool IResultView.ShowNoResultMessage => true;
        bool IResultView.UpdateNeeded => false;

        public event IResultView.SelectionChangedEventHandler SelectionChanged;

        // This event is not used in the SearchGridView
        public event IResultView.PopulateItemsContextMenuHandler PopulateItemsContextMenu
        {
            add { }
            remove { }
        }

        public static SearchGridView Create(ISearchView viewModel)
        {
            return new SearchGridView(viewModel);
        }

        public static Texture2D FetchIcon()
        {
            return EditorGUIUtility.LoadIconRequired("GridView");
        }

        public static SearchResultViewDescriptor GetDescriptor()
        {
            return new SearchResultViewDescriptor(resultViewId, Create, FetchIcon,
                (float)DisplayMode.List + 1, (float)DisplayMode.Limit, (float)DisplayMode.Grid,
                description: "Grid View",
                buttonClassName: "search-statusbar__grid-mode-button");
        }

        public SearchGridView(ISearchView viewModel)
            : base("SearchGridView", viewModel)
        {
            m_GridView = new GridView((IList)viewModel.results, GetItemSize(), GetItemSize() + m_LabelHeight, MakeItem, BindItem)
            {
                unbindItem = UnbindItem,
                destroyItem = DestroyItem,
                selectionType = viewModel.multiselect ? SelectionType.Multiple : SelectionType.Single
            };

            Add(m_GridView);

            var targetHandler = m_GridView.Q<ScrollView>().contentContainer;
            m_GlobalKeyboardHandler = new SearchResultViewGlobalEventHandler(
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
                GenerateLocalKeyDownEvent);
        }

        KeyDownEvent GenerateLocalKeyDownEvent(KeyDownEvent globalEvent)
        {
            switch (globalEvent.keyCode)
            {
                case KeyCode.UpArrow:
                    return KeyDownEvent.GetPooled(globalEvent.character, KeyCode.LeftArrow, globalEvent.modifiers);
                case KeyCode.DownArrow:
                    return KeyDownEvent.GetPooled(globalEvent.character, KeyCode.RightArrow, globalEvent.modifiers);
                default:
                    return KeyDownEvent.GetPooled(globalEvent.character, globalEvent.keyCode, globalEvent.modifiers);
            }
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
            }, TimeSpan.FromSeconds(SearchView.resultViewUpdateThrottleDelay), true);

            m_GridView.itemsBuilt += OnItemsBuilt;
            m_GridView.itemsChosen += OnItemsChosen;
            m_GridView.selectedIndicesChanged += HandleItemsSelected;

            m_GlobalKeyboardHandler.RegisterGlobalEventHandlers();
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_Throttler?.Dispose();

            UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_GlobalKeyboardHandler.UnregisterGlobalEventHandler();

            m_GridView.selectedIndicesChanged -= HandleItemsSelected;
            m_GridView.itemsChosen -= OnItemsChosen;
            m_GridView.itemsBuilt -= OnItemsBuilt;

            base.OnDetachFromPanel(evt);
        }

        private void OnItemsBuilt()
        {
            SetSelectionWithoutNotify(m_ViewModel?.selection);
        }

        private void OnItemsChosen(IEnumerable<object> chosenItems)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var convertedItems = chosenItems.Select(item => (SearchItem)item).ToArray();
#pragma warning restore UA2001
            m_ViewModel.ExecuteAction(null, convertedItems, true);
        }

        private void HandleItemsSelected(IEnumerable<int> selection)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selArray = selection.ToArray();
            SelectionChanged?.Invoke(selArray);
#pragma warning restore UA2001
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
            if (flags.HasAny(RefreshFlags.ItemsChanged))
            {
                Refresh();
            }
            if (flags.HasAny(RefreshFlags.DisplayModeChanged))
            {
                m_GridView.fixedItemHeight = m_ViewModel.itemIconSize + m_LabelHeight;
                m_GridView.fixedItemWidth = m_ViewModel.itemIconSize;
                m_GridView.Rebuild();
            }
        }

        void IResultView.OnGroupChanged(string prevGroupId, string newGroupId)
        {
            Refresh();
        }

        void IResultView.OnItemSourceChanged(ISearchList itemSource)
        {
            if (m_GridView != null)
                m_GridView.itemsSource = (IList)itemSource;
        }

        private void UpdateView()
        {
            m_GridView.fixedItemHeight = m_ViewModel.itemIconSize + m_LabelHeight;
            m_GridView.fixedItemWidth = m_ViewModel.itemIconSize;
            m_GridView.ComputeGridSize();
            m_GridView.RefreshItems();
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

        public void SetSelectionWithoutNotify(SearchSelection selection)
        {
            if (selection == null)
                return;

            var selectedIndexes = selection.indexes;
            var span = NoAllocHelpers.CreateReadOnlySpan(selectedIndexes);
            if (m_GridView.MatchesExistingSelection(span))
                return;
            var firstSelection = selectedIndexes.Count > 0 ? selectedIndexes[0] : -1;
            m_GridView.SetSelectionWithoutNotify(span);
            if (firstSelection != -1)
                m_GridView.ScrollToItem(firstSelection);
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

        #region Global Event Handler
        bool SelectionContains(int index)
        {
            return m_GridView.IndexIsSelected(index);
        }

        int GetCurrentIndex()
        {
            return m_GridView.selectedIndex == -1 ? -1 : m_GridView.selectedIndices[^1];
        }

        int GetItemCount()
        {
            return m_GridView.itemsSource.Count;
        }

        void SetSelectedIndex(int nextSelectedIndex)
        {
            m_GridView.selectedIndex = nextSelectedIndex;
        }

        void AddToSelection(ReadOnlySpan<int> newSelection)
        {
            m_GridView.AddToSelection(newSelection);
        }

        void RemoveFromSelection(int index)
        {
            m_GridView.RemoveFromSelection(index);
        }

        void Frame(int index)
        {
            m_GridView.ScrollToItem(index);
        }

        int GetVisibleItemCount()
        {
            return m_GridView.visibleItemCount;
        }
        #endregion
    }
}
