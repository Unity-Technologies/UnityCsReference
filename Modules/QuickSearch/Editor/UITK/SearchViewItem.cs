// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    abstract class SearchViewItem : SearchElement
    {
        protected SearchItem m_BindedItem;

        protected readonly Label m_Label;
        protected readonly Image m_Thumbnail;
        protected readonly SearchViewItemButtonWithContext m_FavoriteButton;

        readonly SearchResultViewDragHandler m_DragHandler;
        private Action m_FetchPreviewOff = null;

        private SearchPreviewKey m_PreviewKey;
        private IVisualElementScheduledItem m_PreviewRefreshCallback;
        private const int k_PreviewFetchCounter = 100;

        public static readonly string searchFavoriteButtonTooltip = L10n.Tr("Mark as Favorite");
        public static readonly string searchFavoriteOnButtonTooltip = L10n.Tr("Remove as Favorite");
        public static readonly string moreActionsTooltip = L10n.Tr("Open actions menu");

        public static readonly string searchFavoriteButtonClassName = "search-view-item".WithUssElement("favorite-button");
        public static readonly string searchFavoriteButtonName = "SearchFavoriteButton";

        public static readonly string moreActionButtonName = "SearchItemActionsDropdown";

        public SearchViewItem(string name, ISearchView viewModel, params string[] classNames)
            : base(name, viewModel, classNames)
        {
            m_Label = new Label() { name = "SearchViewItemLabel" };
            m_Thumbnail = new Image() { name = "SearchViewItemThumbnail" };
            m_FavoriteButton = new SearchViewItemButtonWithContext(searchFavoriteButtonName,
                string.Empty,
                searchFavoriteButtonTooltip,
                OnFavoriteButtonClicked,
                baseIconButtonClassName,
                searchFavoriteButtonClassName);

            RegisterCallback<ContextClickEvent>(OnItemContextualClicked);
            m_DragHandler = new SearchResultViewDragHandler(viewModel, this)
            {
                CanStartDrag = CanStartDrag,
                StartDrag = StartDrag,
                GetDraggedItem = evt => m_BindedItem
            };
            m_DragHandler.RegisterDragCallbacks();
        }

        protected override void OnAttachToPanel(AttachToPanelEvent evt)
        {
            base.OnAttachToPanel(evt);

            OnAll(SearchEvent.ItemFavoriteStateChanged, OnFavoriteStateChanged);
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Off(SearchEvent.ItemFavoriteStateChanged, OnFavoriteStateChanged);

            base.OnDetachFromPanel(evt);
        }

        private void OnFavoriteStateChanged(ISearchEvent evt)
        {
            if (m_BindedItem == null)
                return;

            var id = string.Empty;
            if (evt.argumentCount == 1)
                id = (string)evt.GetArgument(0);
            else
                return;

            if (id.Equals(m_BindedItem.id, StringComparison.Ordinal))
                UpdateFavoriteImage();
        }

        public virtual void Destroy()
        {
            CancelFetchPreview();

            UnregisterCallback<ContextClickEvent>(OnItemContextualClicked);
            m_DragHandler.UnregisterDragCallbacks();
        }

        private void CancelFetchPreview()
        {
            m_ViewModel.previewManager.CancelFetch(m_PreviewKey);
            m_PreviewKey = default;
            if (m_FetchPreviewOff != null)
            {
                m_FetchPreviewOff?.Invoke();
                m_FetchPreviewOff = null;
            }
        }

        public virtual FetchPreviewOptions GetPreviewOptions()
        {
            return FetchPreviewOptions.Normal | FetchPreviewOptions.Preview2D;
        }

        public virtual bool ShouldFetchPreview()
        {
            return SearchSettings.fetchPreview && CanFetchPreview() && !m_ViewModel.previewManager.HasPreview(m_PreviewKey);
        }

        public virtual void Bind(in SearchItem item)
        {
            userData = item.id;
            name = item.id;
            m_BindedItem = item;
            m_Label.text = item.GetLabel(context);
            m_FavoriteButton.BoundItem = m_BindedItem;

            UpdatePreview();
            if (CanFetchPreview())
            {
                // Last tries at getting the preview. (For the AssetStoreProvider).
                // TODO FetchItemProperties(DOTSE - 1994): All the fetching of async properties should be consolidated in the ResultView/SearchView.
                var counter = 0;
                m_PreviewRefreshCallback = m_Thumbnail.schedule.Execute(() =>
                {
                    if (m_FetchPreviewOff == null)
                        UpdatePreview();
                }).StartingIn(500).Every(500).Until(() =>
                {
                    counter++;
                    return m_ViewModel.previewManager.HasPreview(m_PreviewKey) || counter >= k_PreviewFetchCounter;
                });
            }

            UpdateFavoriteImage();
        }

        public virtual void Unbind()
        {
            CancelFetchPreview();

            m_Label.text = null;
            m_Thumbnail.image = null;
            m_FavoriteButton.BoundItem = null;
            m_FavoriteButton.SetActivePseudoState(false);
            m_BindedItem = null;

            if (m_PreviewRefreshCallback?.isActive == true)
                m_PreviewRefreshCallback.Pause();
        }

        private bool IsSizeValid(out Vector2 size)
        {
            size = default;
            if (m_Thumbnail == null)
                return false;

            size.x = m_Thumbnail.resolvedStyle.width;
            if (float.IsNaN(size.x) || size.x <= 0)
                return false;

            size.y = m_Thumbnail.resolvedStyle.height;
            if (float.IsNaN(size.y) || size.y <= 0)
                return false;

            return true;
        }

        private bool GetExistingPreview()
        {
            if (m_ViewModel.previewManager.HasPreview(m_PreviewKey))
            {
                var preview = m_ViewModel.previewManager.FetchPreview(m_PreviewKey);
                if (preview.valid)
                {
                    m_Thumbnail.image = preview.texture;
                    m_BindedItem.preview = preview.texture;
                    return true;
                }
            }

            return false;
        }

        private void UpdatePreview()
        {
            if (GetExistingPreview())
                return;

            if (ShouldFetchPreview())
            {
                m_FetchPreviewOff?.Invoke();
                AsyncFetchPreview();
            }

            var tex = m_BindedItem.GetThumbnail(context, cacheThumbnail: false);
            m_Thumbnail.image = tex;
            m_BindedItem.thumbnail = tex;
        }

        private void AsyncFetchPreview()
        {
            if (m_BindedItem == null)
                return;

            if (IsSizeValid(out var previewSize))
            {
                m_PreviewKey = new SearchPreviewKey(m_BindedItem, GetPreviewOptions(), previewSize);
                if (GetExistingPreview())
                    return;

                m_FetchPreviewOff = m_ViewModel.previewManager.FetchPreview(m_BindedItem, context, m_PreviewKey, FetchPreview, OnPreviewReady);
            }
            else
            {
                m_FetchPreviewOff = Utils.CallDelayed(AsyncFetchPreview, 0.01d); // To make sure the style is resolved.
            }
        }

        private bool CanFetchPreview()
        {
            return m_BindedItem.provider.fetchPreview != null;
        }

        private void FetchPreview(SearchItem item, SearchContext context, FetchPreviewOptions options, Vector2 size, OnPreviewReady onPreviewReady)
        {
            SearchPreview searchPreview;
            if (item == null || m_BindedItem == null)
            {
                searchPreview = SearchPreview.invalid;
            }
            else
            {
                var fetchedPreview = m_BindedItem.GetPreview(context, size, options, cacheThumbnail: false);
                searchPreview = new SearchPreview(m_PreviewKey, fetchedPreview);
            }

            onPreviewReady?.Invoke(item, context, searchPreview);
        }

        private void OnPreviewReady(SearchItem item, SearchContext context, SearchPreview preview)
        {
            if (preview.valid && m_BindedItem != null && m_Thumbnail != null)
            {
                var fetchedPreview = preview.texture;
                if (fetchedPreview != null && fetchedPreview.width > 0 && fetchedPreview.height > 0)
                {
                    m_Thumbnail.image = fetchedPreview;
                    m_BindedItem.preview = fetchedPreview;
                }
            }

            if (m_Thumbnail != null && m_BindedItem != null && m_Thumbnail.image == null)
            {
                var tex = m_BindedItem.GetThumbnail(context, cacheThumbnail: false);
                m_Thumbnail.image = tex;
                m_BindedItem.thumbnail = tex;
            }

            m_FetchPreviewOff = null;
        }

        private void OnItemContextualClicked(ContextClickEvent evt)
        {
            m_ViewModel.ShowItemContextualMenu(m_BindedItem, default);
        }

        bool CanStartDrag(PointerDownEvent evt)
        {
            return m_BindedItem != null && m_BindedItem.provider.startDrag != null;
        }

        void StartDrag(SearchItem _)
        {
            DragAndDrop.PrepareStartDrag();
            m_BindedItem.provider.startDrag(m_BindedItem, context);
        }

        private void OnFavoriteButtonClicked(SearchViewItemButtonWithContext _, SearchItem _2)
        {
            if (SearchSettings.searchItemFavorites.Contains(m_BindedItem.id))
                SearchSettings.RemoveItemFavorite(m_BindedItem);
            else
                SearchSettings.AddItemFavorite(m_BindedItem);
            UpdateFavoriteImage();
        }

        protected void UpdateFavoriteImage()
        {
            if (SearchSettings.searchItemFavorites.Contains(m_BindedItem.id))
            {
                m_FavoriteButton.tooltip = searchFavoriteOnButtonTooltip;
                m_FavoriteButton.SetActivePseudoState(true);
            }
            else
            {
                m_FavoriteButton.tooltip = searchFavoriteButtonTooltip;
                m_FavoriteButton.SetActivePseudoState(false);
            }
        }

        protected void OnActionDropdownClicked(SearchViewItemButtonWithContext _, SearchItem _2)
        {
            m_ViewModel.ShowItemContextualMenu(m_BindedItem, default);
        }
    }
}
