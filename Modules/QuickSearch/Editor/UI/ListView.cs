// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    class ListView : ResultView
    {
        private float m_ItemRowHeight = Styles.itemRowHeight;

        public ListView(ISearchView hostView)
            : base(hostView)
        {
        }

        public override void Draw(Rect screenRect, ICollection<int> selection)
        {
            if (compactView)
                m_ItemRowHeight = Styles.itemRowHeight / 2.0f;
            else
                m_ItemRowHeight = Styles.itemRowHeight;

            var evt = Event.current;
            var itemCount = items.Count;
            var availableHeight = screenRect.height;
            var itemRowHeight = m_ItemRowHeight;
            var itemSkipCount = Math.Max(0, (int)(m_ScrollPosition.y / itemRowHeight));
            var itemDisplayCount = Math.Max(0, Math.Min(itemCount, Mathf.CeilToInt(availableHeight / itemRowHeight) + 1));
            var topSpaceSkipped = itemSkipCount * itemRowHeight;
            var limitCount = Math.Max(0, Math.Min(itemDisplayCount, itemCount - itemSkipCount));
            var totalSpace = itemCount * itemRowHeight;
            var scrollbarSpace = availableHeight <= totalSpace ? Styles.scrollbarWidth : 0f;

            var viewRect = screenRect;
            viewRect.width -= scrollbarSpace;
            viewRect.height = totalSpace;
            scrollbarVisible = scrollbarSpace > 0;

            m_ScrollPosition = GUI.BeginScrollView(screenRect, m_ScrollPosition, viewRect);

            var itemIndex = 0;
            var itemRect = new Rect(screenRect.x, topSpaceSkipped + screenRect.y, viewRect.width, itemRowHeight);
            foreach (var item in items)
            {
                if (itemIndex >= itemSkipCount && itemIndex <= itemSkipCount + limitCount)
                {
                    DrawItem(evt, item, screenRect, itemRect, itemIndex, selection);
                    itemRect.y += itemRect.height;
                }
                else
                {
                    item.preview = null;
                }

                itemIndex++;
            }

            // Fix selected index display if out of virtual scrolling area
            int selectionIndex = selection.Count == 0 ? -1 : selection.Last();
            if (evt.type == EventType.Repaint && focusSelectedItem && selectionIndex >= 0)
            {
                ScrollListToItem(itemSkipCount + 1, itemSkipCount + itemDisplayCount - 2, selectionIndex, screenRect);
                focusSelectedItem = false;
            }
            else
                HandleListItemEvents(itemCount, screenRect);

            GUI.EndScrollView();
        }

        public override int GetDisplayItemCount()
        {
            var itemCount = searchView.results.Count;
            return Math.Max(0, Math.Min(itemCount, Mathf.RoundToInt(m_DrawItemsRect.height / m_ItemRowHeight)));
        }

        private void DrawItem(Event evt, SearchItem item, Rect screenRect, Rect itemRect, int itemIndex, ICollection<int> selection)
        {
            bool isItemSelected = selection.Contains(itemIndex);
            if (evt.type == EventType.Repaint)
            {
                // Draw item background
                var bgStyle = itemIndex % 2 == 0 ? Styles.itemBackground1 : Styles.itemBackground2;
                if (isItemSelected)
                    bgStyle = Styles.selectedItemBackground;
                bgStyle.Draw(itemRect, itemRect.Contains(evt.mousePosition), false, false, false);
            }

            // Draw action dropdown
            var buttonRect = new Rect(itemRect.xMax - Styles.actionButton.fixedWidth - Styles.actionButton.margin.right, itemRect.y, Styles.actionButton.fixedWidth, Styles.actionButton.fixedHeight);
            buttonRect.y += (itemRect.height - Styles.actionButton.fixedHeight) / 2f;
            if (evt.type == EventType.Repaint || (itemRect.Contains(evt.mousePosition) && screenRect.Contains(evt.mousePosition)))
            {
                bool hasActionDropdown = searchView.selectCallback == null && searchView.selection.Count <= 1 && item.provider.actions.Count > 1;
                if (hasActionDropdown)
                {
                    bool actionHover = buttonRect.Contains(evt.mousePosition);
                    GUI.Label(buttonRect, Styles.moreActionsContent, actionHover ? Styles.actionButtonHovered : Styles.actionButton);
                    EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
                    if (evt.type == EventType.MouseDown && actionHover)
                    {
                        var contextRect = new Rect(evt.mousePosition, new Vector2(1, 1));
                        searchView.ShowItemContextualMenu(item, contextRect);
                        GUIUtility.ExitGUI();
                    }

                    buttonRect.x -= Styles.actionButton.fixedWidth + Styles.actionButton.margin.left;
                }

                if (SearchSettings.searchItemFavorites.Contains(item.id))
                {
                    if (GUI.Button(buttonRect, Styles.searchFavoriteOnButtonContent, Styles.actionButton))
                        SearchSettings.RemoveItemFavorite(item);
                    EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
                }
                else
                {
                    using (new GUI.ColorScope(new Color(0.9f, 0.9f, 0.9f, 0.4f)))
                        if (GUI.Button(buttonRect, Styles.searchFavoriteButtonContent, Styles.actionButton))
                            SearchSettings.AddItemFavorite(item);
                    EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
                }
            }

            if (evt.type == EventType.Repaint)
            {
                // Draw thumbnail
                var thumbnailRect = DrawListThumbnail(item, itemRect);

                // Draw label
                var maxWidth = buttonRect.xMin - itemRect.xMin
                    - (compactView ? Styles.itemPreviewSize / 2.0f : Styles.itemPreviewSize)
                    - Styles.descriptionPadding;
                var labelStyle = isItemSelected ?
                    (compactView ? Styles.selectedItemLabelCompact : Styles.selectedItemLabel) :
                    (compactView ? Styles.itemLabelCompact : Styles.itemLabel);
                var labelRect = new Rect(
                    thumbnailRect.xMax + labelStyle.margin.left, itemRect.y + labelStyle.margin.top,
                    maxWidth - labelStyle.margin.right, labelStyle.lineHeight);

                if (!compactView)
                {
                    var label = item.provider.fetchLabel(item, context);
                    GUI.Label(labelRect, label, labelStyle);
                    labelRect.y = labelRect.yMax + labelStyle.margin.bottom;

                    // Draw description
                    var labelContent = SearchContent.FormatDescription(item, context, maxWidth);
                    labelStyle = isItemSelected ? Styles.selectedItemDescription : Styles.itemDescription;
                    labelRect.y += labelStyle.margin.top;
                    GUI.Label(labelRect, labelContent, labelStyle);
                }
                else
                {
                    item.options |= SearchItemOptions.Compacted;
                    var labelContent = SearchContent.FormatDescription(item, context, maxWidth);
                    GUI.Label(labelRect, labelContent, labelStyle);
                    item.options &= ~SearchItemOptions.Compacted;
                }
            }
        }

        private Rect DrawListThumbnail(SearchItem item, Rect itemRect)
        {
            Texture2D thumbnail = null;
            if (!compactView && SearchSettings.fetchPreview)
            {
                thumbnail = item.preview;
                var shouldFetchPreview = !thumbnail && item.provider.fetchPreview != null;
                if (shouldFetchPreview)
                {
                    var previewSize = new Vector2(Styles.itemPreviewSize, Styles.itemPreviewSize);
                    thumbnail = item.provider.fetchPreview(item, context, previewSize, FetchPreviewOptions.Preview2D | FetchPreviewOptions.Normal);
                    if (thumbnail)
                        item.preview = thumbnail;
                }
            }

            if (!thumbnail)
            {
                thumbnail = item.thumbnail;
                if (!thumbnail && item.provider.fetchThumbnail != null)
                {
                    thumbnail = item.provider.fetchThumbnail(item, context);
                    if (thumbnail)
                        item.thumbnail = thumbnail;
                }
            }

            var thumbnailRect = new Rect(itemRect.x, itemRect.y, Styles.itemPreviewSize, Styles.itemPreviewSize);
            if (compactView)
                thumbnailRect.size /= 2.0f;
            thumbnailRect.x += Styles.preview.margin.left;
            thumbnailRect.y += (itemRect.height - thumbnailRect.height) / 2f;
            GUI.Label(thumbnailRect, thumbnail ?? Icons.quicksearch, Styles.preview);

            return thumbnailRect;
        }

        private void HandleListItemEvents(int itemTotalCount, Rect screenRect)
        {
            var mousePosition = Event.current.mousePosition - new Vector2(0, screenRect.y);
            if (AutoComplete.IsHovered(mousePosition))
                return;

            if (Event.current.type == EventType.MouseDown)
            {
                var clickedItemIndex = (int)(mousePosition.y / m_ItemRowHeight);
                if (clickedItemIndex >= 0 && clickedItemIndex < itemTotalCount)
                    HandleMouseDown(clickedItemIndex);
            }
            else if (Event.current.type == EventType.MouseUp || IsDragClicked(Event.current))
            {
                var clickedItemIndex = (int)(mousePosition.y / m_ItemRowHeight);
                HandleMouseUp(clickedItemIndex, itemTotalCount);
            }
            else if (Event.current.type == EventType.MouseDrag && m_PrepareDrag)
            {
                var dragIndex = (int)(mousePosition.y / m_ItemRowHeight);
                HandleMouseDrag(dragIndex, itemTotalCount);
            }
        }

        private void ScrollListToItem(int start, int end, int selection, Rect screenRect)
        {
            if (start <= selection && selection < end)
                return;

            Rect projectedSelectedItemRect = new Rect(0, selection * m_ItemRowHeight, screenRect.width, m_ItemRowHeight);
            if (selection <= start)
            {
                m_ScrollPosition.y = Mathf.Max(0, projectedSelectedItemRect.y - 2);
                searchView.Repaint();
            }
            else if (selection >= end)
            {
                Rect visibleRect = new Rect(m_ScrollPosition, screenRect.size);
                m_ScrollPosition.y += projectedSelectedItemRect.yMax - visibleRect.yMax + 2;
                searchView.Repaint();
            }
        }
    }
}
