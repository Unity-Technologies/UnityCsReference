// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    class GridView : ResultView
    {
        const float itemPadding = 4f;
        const float itemLabelHeight = 32f;
        const float itemLabelTopPadding = 4f;
        const float contentViewPadding = 6f;

        public GridView(ISearchView hostView)
            : base(hostView)
        {
        }

        public override void Draw(Rect screenRect, ICollection<int> selection)
        {
            GetGridSize(screenRect, out var contentRect, out var itemHeight, out var itemWidth, out var gridHeight, out var gridWidth, out var scrollBarWidth, out var columnCount, out _);

            var spaceBetweenTiles = Mathf.Floor((gridWidth - (columnCount * itemWidth)) / (columnCount - 1f));
            var viewRect = screenRect; viewRect.width = screenRect.width - scrollBarWidth; viewRect.height = gridHeight + contentViewPadding;
            var gridRect = new Rect(screenRect.x, screenRect.y + m_ScrollPosition.y, gridWidth, screenRect.height);
            var itemRect = new Rect(contentRect.x, contentRect.y, itemWidth, itemHeight);

            m_ScrollPosition = GUI.BeginScrollView(screenRect, m_ScrollPosition, viewRect);

            var evt = Event.current;
            var index = 0;
            var selectionIndex = selection.Count == 0 ? -1 : selection.Last();
            var eventType = evt.type;
            var mousePosition = evt.mousePosition;
            var isHoverGrid = !AutoComplete.IsHovered(evt.mousePosition) & gridRect.Contains(mousePosition);

            foreach (var item in items)
            {
                if (evt.type == EventType.Repaint && index == selectionIndex && focusSelectedItem)
                {
                    FocusGridItemRect(itemRect, screenRect);
                    focusSelectedItem = false;
                }

                if (itemRect.Overlaps(gridRect))
                {
                    var buttonWidth = Styles.actionButton.fixedWidth + Styles.actionButton.margin.right;
                    var favoriteButtonRect = new Rect(itemRect.xMax - buttonWidth, itemRect.yMin + 4f, Styles.actionButton.fixedWidth, Styles.actionButton.fixedHeight);

                    if (evt.isMouse && !isHoverGrid)
                    {
                        // Skip
                    }
                    else if (eventType == EventType.MouseDown)
                    {
                        if (itemRect.Contains(mousePosition))
                        {
                            if (favoriteButtonRect.Contains(evt.mousePosition))
                            {
                                if (SearchSettings.searchItemFavorites.Contains(item.id))
                                    SearchSettings.RemoveItemFavorite(item);
                                else
                                    SearchSettings.AddItemFavorite(item);
                            }
                            else
                            {
                                HandleMouseDown(index);
                            }
                        }
                    }
                    else if (evt.type == EventType.MouseUp || IsDragClicked(evt))
                    {
                        if (itemRect.Contains(mousePosition))
                        {
                            HandleMouseUp(index, items.Count);
                            if (index == selectionIndex)
                                focusSelectedItem = true;
                        }
                    }
                    else if (eventType == EventType.MouseDrag && m_PrepareDrag)
                    {
                        if (itemRect.Contains(mousePosition))
                            HandleMouseDrag(index, items.Count);
                    }
                    else if (eventType == EventType.Repaint)
                    {
                        DrawGridItem(index, item, itemRect, isHoverGrid, selection, evt);

                        if (isHoverGrid && itemRect.Contains(evt.mousePosition))
                        {
                            if (SearchSettings.searchItemFavorites.Contains(item.id))
                                GUI.Button(favoriteButtonRect, Styles.searchFavoriteOnButtonContent, Styles.actionButton);
                            else
                            {
                                using (new GUI.ColorScope(new Color(0.9f, 0.9f, 0.9f, 0.4f)))
                                    GUI.Button(favoriteButtonRect, Styles.searchFavoriteButtonContent, Styles.actionButton);
                            }
                            EditorGUIUtility.AddCursorRect(favoriteButtonRect, MouseCursor.Link);
                        }
                    }
                }
                else
                {
                    item.preview = null;
                }

                itemRect = new Rect(itemRect.x + itemWidth + spaceBetweenTiles, itemRect.y, itemWidth, itemHeight);
                if (itemRect.xMax > contentRect.x + gridWidth)
                    itemRect = new Rect(contentRect.x, itemRect.y + itemHeight, itemRect.width, itemRect.height);

                ++index;
            }

            GUI.EndScrollView();
        }

        public override int GetDisplayItemCount()
        {
            GetGridSize(m_DrawItemsRect, out _, out var columnCount, out var rowCount);
            int gridItemCount = rowCount * columnCount;

            return Math.Max(0, Math.Min(searchView.results.Count, gridItemCount));
        }

        private void DrawGridItem(int index, SearchItem item, Rect itemRect, bool canHover, ICollection<int> selection, Event evt)
        {
            var backgroundRect = new Rect(itemRect.x + 1, itemRect.y + 1, itemRect.width - 2, itemRect.height - 2);
            var isSelected = selection.Contains(index);
            var isHovered = canHover && itemRect.Contains(evt.mousePosition);

            Texture2D thumbnail = null;
            var shouldFetchPreview = SearchSettings.fetchPreview;
            if (shouldFetchPreview)
            {
                thumbnail = item.preview;
                shouldFetchPreview = !thumbnail && item.provider.fetchPreview != null;
                if (shouldFetchPreview)
                {
                    var previewSize = new Vector2(itemSize, itemSize);
                    thumbnail = item.provider.fetchPreview(item, context, previewSize, FetchPreviewOptions.Preview2D | FetchPreviewOptions.Normal);
                    if (thumbnail && !AssetPreview.IsLoadingAssetPreviews())
                        item.preview = thumbnail;
                }
            }

            if (!thumbnail)
            {
                thumbnail = item.thumbnail;
                if (!thumbnail && item.provider.fetchThumbnail != null)
                {
                    thumbnail = item.provider.fetchThumbnail(item, context);
                    if (thumbnail && !shouldFetchPreview && !AssetPreview.IsLoadingAssetPreviews())
                        item.thumbnail = thumbnail;
                }
            }

            var thumbnailRect = new Rect(itemRect.x + itemPadding, itemRect.y + itemPadding, itemSize, itemSize);
            Styles.gridItemBackground.Draw(thumbnailRect, thumbnail ?? Icons.quicksearch, isHovered, false, isSelected, false);

            var labelRect = new Rect(
                itemRect.x + itemPadding, itemRect.yMax - itemLabelHeight - itemPadding,
                itemRect.width - itemPadding * 2f, itemLabelHeight - itemPadding);
            var originalItemLabel = item.GetLabel(context, true);
            var itemLabel = originalItemLabel;
            var textRectWidth = (labelRect.width - Styles.gridItemLabel.padding.horizontal - 18) * 2f;
            var maxCharLength = Utils.GetNumCharactersThatFitWithinWidth(Styles.gridItemLabel, originalItemLabel, textRectWidth);
            if (originalItemLabel.Length > maxCharLength)
            {
                maxCharLength = Math.Max(0, maxCharLength - 3);
                itemLabel = originalItemLabel.Substring(0, maxCharLength / 2) + "\u2026" + originalItemLabel.Substring(originalItemLabel.Length - maxCharLength / 2);
            }
            else
            {
                var labelSize = Styles.gridItemLabel.CalcSize(new GUIContent(itemLabel));
                labelSize.x += 2;
                labelSize.y += 2;
                if (labelSize.x < labelRect.width)
                {
                    var c = labelRect.center;
                    labelRect = new Rect(c.x - labelSize.x / 2.0f, labelRect.y, labelSize.x, labelSize.y);
                }
            }
            Styles.gridItemLabel.Draw(labelRect, new GUIContent(itemLabel, originalItemLabel), false, false, isSelected || isHovered, isSelected);
        }

        private void FocusGridItemRect(Rect itemRect, Rect screenRect)
        {
            // Focus item
            var itemHalfHeight = itemRect.height / 2f;
            if (itemRect.center.y <= m_ScrollPosition.y + itemHalfHeight)
            {
                m_ScrollPosition.y = Mathf.Max(0, itemRect.yMin - itemHalfHeight);
                searchView.Repaint();
            }
            else if (itemRect.center.y > m_ScrollPosition.y + screenRect.yMax)
            {
                m_ScrollPosition.y = Mathf.Max(0f, itemRect.yMax - screenRect.yMax);
                searchView.Repaint();
            }
        }

        protected override int GetFirstVisibleItemIndex()
        {
            GetGridSize(m_DrawItemsRect, out var itemHeight, out var columnCount, out _);

            var halfItemHeight = itemHeight / 2f;
            // Here, we want to get the first row that won't get focused if selected. Therefore, we find the first
            // row that is > halfItemHeight visible.
            var firstVisibleRowOffset = Math.Max(0, Mathf.FloorToInt((m_ScrollPosition.y + halfItemHeight) / itemHeight));
            return Math.Min(items.Count - 1, firstVisibleRowOffset * columnCount);
        }

        protected override int GetLastVisibleItemIndex()
        {
            GetGridSize(m_DrawItemsRect, out var itemHeight, out var columnCount, out _);
            var halfItemHeight = itemHeight / 2f;
            // Here, we want to get the last row that won't get focused if selected. Therefore, we find the last
            // row that is >= halfItemHeight visible.
            var lastVisibleRowOffset = Math.Max(1, Mathf.CeilToInt((m_ScrollPosition.y + m_DrawItemsRect.height - halfItemHeight) / itemHeight));
            return Math.Min(items.Count - 1, lastVisibleRowOffset * columnCount - 1);
        }

        private void GetGridSize(Rect screenRect, out float itemHeight, out int columnCount, out int rowCount)
        {
            GetGridSize(screenRect, out _, out itemHeight, out _, out _, out _, out _, out columnCount, out rowCount);
        }

        private void GetGridSize(Rect screenRect, out Rect contentRect, out float itemHeight, out float itemWidth, out float gridHeight, out float gridWidth, out float scrollBarWidth, out int columnCount, out int rowCount)
        {
            itemWidth = itemSize + itemPadding * 2;
            itemHeight = itemSize + itemLabelHeight + itemLabelTopPadding + itemPadding * 2;

            contentRect = screenRect;
            contentRect.x += contentViewPadding;
            contentRect.y += contentViewPadding;
            contentRect.width -= (contentViewPadding * 2f);
            contentRect.height -= (contentViewPadding * 2f);

            gridWidth = contentRect.width;
            var itemCount = items.Count;
            columnCount = (int)(gridWidth / itemWidth);
            int lineCount = Mathf.CeilToInt(itemCount / (float)columnCount);
            gridHeight = lineCount * itemHeight - Styles.statusLabel.fixedHeight;

            scrollBarWidth = 0f;
            scrollbarVisible = gridHeight > screenRect.height;
            if (scrollbarVisible)
            {
                scrollBarWidth = Styles.scrollbar.fixedWidth;
                gridWidth -= scrollBarWidth;
                columnCount = (int)(gridWidth / itemWidth);
                lineCount = Mathf.CeilToInt(itemCount / (float)columnCount);
                gridHeight = lineCount * itemHeight;
            }

            rowCount = Mathf.Max(1, Mathf.RoundToInt(contentRect.height / itemHeight));
        }
    }
}
