// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    abstract class SearchViewItem : SearchElement
    {
        protected SearchItem m_BindedItem;

        protected readonly Label m_Label;
        protected readonly Image m_Thumbnail;
        protected readonly Button m_FavoriteButton;

        private bool m_InitiateDrag = false;
        private Vector3 m_InitiateDragPosition;
        private Action m_FetchPreviewOff = null;

        private SearchPreviewKey m_PreviewKey;
        private IVisualElementScheduledItem m_PreviewRefreshCallback;
        private const int k_PreviewFetchCounter = 5;

        static readonly string searchFavoriteButtonTooltip = L10n.Tr("Mark as Favorite");
        static readonly string searchFavoriteOnButtonTooltip = L10n.Tr("Remove as Favorite");

        public static readonly string searchFavoriteButtonClassName = "search-view-item".WithUssElement("favorite-button");

        public SearchViewItem(string name, ISearchView viewModel, params string[] classNames)
            : base(name, viewModel, classNames)
        {
            m_Label = new Label() { name = "SearchViewItemLabel" };
            m_Thumbnail = new Image() { name = "SearchViewItemThumbnail" };
            m_FavoriteButton = CreateButton("SearchFavoriteButton", searchFavoriteButtonTooltip, OnFavoriteButtonClicked, baseIconButtonClassName, searchFavoriteButtonClassName);

            RegisterCallback<ContextClickEvent>(OnItemContextualClicked);
            RegisterCallback<PointerDownEvent>(OnItemPointerDown);
            RegisterCallback<PointerUpEvent>(OnItemPointerUp);
            RegisterCallback<DragExitedEvent>(OnDragExited);
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
            UnregisterCallback<PointerDownEvent>(OnItemPointerDown);
            UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            UnregisterCallback<PointerUpEvent>(OnItemPointerUp);
            UnregisterCallback<DragExitedEvent>(OnDragExited);
            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
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
            return SearchSettings.fetchPreview && !m_ViewModel.previewManager.HasPreview(m_PreviewKey);
        }

        public virtual void Bind(in SearchItem item)
        {
            name = item.id;
            m_BindedItem = item;
            m_Label.text = item.GetLabel(context);

            UpdatePreview();

            // Last tries at getting the preview. (For the AssetStoreProvider).
            // TODO FetchItemProperties(DOTSE - 1994): All the fetching of async properties should be consolidated in the ResultView/SearchView.
            var counter = 0;
            m_PreviewRefreshCallback = m_Thumbnail.schedule.Execute(() =>
            {
                if (m_FetchPreviewOff == null)
                    UpdatePreview();
            });
            m_PreviewRefreshCallback.StartingIn(500).Every(500).Until(() =>
            {
                counter++;
                return m_ViewModel.previewManager.HasPreview(m_PreviewKey) || counter >= k_PreviewFetchCounter;
            });
            UpdateFavoriteImage();
        }

        public virtual void Unbind()
        {
            CancelFetchPreview();

            m_Label.text = null;
            m_Thumbnail.image = null;
            m_FavoriteButton.pseudoStates &= ~PseudoStates.Active;
            if (m_BindedItem != null)
            {
                m_BindedItem = null;
            }

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

        private void OnItemPointerDown(PointerDownEvent evt)
        {
            // dragging is initiated only by left mouse clicks
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            m_InitiateDrag = !m_ViewModel.IsPicker() && m_BindedItem.provider.startDrag != null;
            m_InitiateDragPosition = evt.localPosition;

            UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            RegisterCallback<PointerMoveEvent>(OnItemPointerMove);

            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
            RegisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
        }

        void OnItemPointerLeave(PointerLeaveEvent evt)
        {
            // If we enter here, it means the mouse left the element before any mouse
            // move, so the item jumped around to be repositioned in the window.
            // This will cause an issue with drag and drop
            m_InitiateDrag = false;

            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
        }

        private void OnItemPointerMove(PointerMoveEvent evt)
        {
            if (!m_InitiateDrag)
                return;

            if ((evt.localPosition - m_InitiateDragPosition).sqrMagnitude < 5f)
                return;

            UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);

            DragAndDrop.PrepareStartDrag();
            m_BindedItem.provider.startDrag(m_BindedItem, context);
            m_InitiateDrag = false;
        }

        private void OnDragExited(DragExitedEvent evt) => ResetDrag();
        private void OnItemPointerUp(PointerUpEvent evt) => ResetDrag();

        private void OnFavoriteButtonClicked()
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
                m_FavoriteButton.pseudoStates |= PseudoStates.Active;
            }
            else
            {
                m_FavoriteButton.tooltip = searchFavoriteButtonTooltip;
                m_FavoriteButton.pseudoStates &= ~PseudoStates.Active;
            }
        }

        protected void OnActionDropdownClicked()
        {
            m_ViewModel.ShowItemContextualMenu(m_BindedItem, default);
        }

        private void ResetDrag()
        {
            m_InitiateDrag = false;
            UnregisterCallback<PointerMoveEvent>(OnItemPointerMove);
            UnregisterCallback<PointerLeaveEvent>(OnItemPointerLeave);
        }
    }
}
