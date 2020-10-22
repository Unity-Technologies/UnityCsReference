// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Search
{
    class GridView : ResultView
    {
        const float itemPadding = 4f;
        const float itemLabelHeight = 32f;
        const float itemLabelTopPadding = 4f;

        public GridView(ISearchView hostView)
            : base(hostView)
        {
        }

        public override void Draw(Rect screenRect, ICollection<int> selection)
        {
            float itemWidth = itemSize + itemPadding * 2;
            float itemHeight = itemSize + itemLabelHeight + itemLabelTopPadding + itemPadding * 2;

            var gridWidth = screenRect.width;
            var itemCount = items.Count;
            int columnCount = (int)(gridWidth / itemWidth);
            int lineCount = Mathf.CeilToInt(itemCount / (float)columnCount);
            var gridHeight = lineCount * itemHeight - Styles.statusLabel.fixedHeight;
            var availableHeight = screenRect.height;

            scrollbarVisible = gridHeight > availableHeight;
            if (scrollbarVisible)
            {
                gridWidth -= Styles.scrollbar.fixedWidth;
                columnCount = (int)(gridWidth / itemWidth);
                lineCount = Mathf.CeilToInt(itemCount / (float)columnCount);
                gridHeight = lineCount * itemHeight;
            }

            var spaceBetweenTiles = (gridWidth - (columnCount * itemWidth)) / (columnCount + 1f);

            var viewRect = screenRect; viewRect.width = gridWidth; viewRect.height = gridHeight;
            m_ScrollPosition = GUI.BeginScrollView(screenRect, m_ScrollPosition, viewRect);

            Rect gridRect = new Rect(screenRect.x, screenRect.y + m_ScrollPosition.y, gridWidth, availableHeight);
            Rect itemRect = new Rect(screenRect.x + spaceBetweenTiles, screenRect.y + spaceBetweenTiles, itemWidth, itemHeight);

            var evt = Event.current;
            int index = 0;
            int selectionIndex = selection.Count == 0 ? -1 : selection.Last();
            var eventType = evt.type;
            var mouseButton = evt.button;
            var mousePosition = evt.mousePosition;
            var isHoverGrid = !AutoComplete.IsHovered(evt.mousePosition);
            isHoverGrid &= gridRect.Contains(mousePosition);

            foreach (var item in items)
            {
                if (evt.type == EventType.Repaint && index == selectionIndex && focusSelectedItem)
                {
                    FocusGridItemRect(itemRect, screenRect);
                    focusSelectedItem = false;
                }

                if (itemRect.Overlaps(gridRect))
                {
                    if (evt.isMouse && !isHoverGrid)
                    {
                        // Skip
                    }
                    else if (eventType == EventType.MouseDown && mouseButton == 0)
                    {
                        if (itemRect.Contains(mousePosition))
                            HandleMouseDown(index);
                    }
                    else if (evt.type == EventType.MouseUp || IsDragClicked(evt))
                    {
                        if (itemRect.Contains(mousePosition))
                        {
                            HandleMouseUp(index, itemCount);
                            if (index == selectionIndex)
                                focusSelectedItem = true;
                        }
                    }
                    else if (eventType == EventType.MouseDrag && m_PrepareDrag)
                    {
                        if (itemRect.Contains(mousePosition))
                            HandleMouseDrag(index, itemCount);
                    }
                    else if (eventType == EventType.Repaint)
                    {
                        DrawGridItem(index, item, itemRect, isHoverGrid, selection, evt);
                    }
                    else
                    {
                        item.preview = null;
                    }
                }

                itemRect = new Rect(itemRect.x + itemWidth + spaceBetweenTiles, itemRect.y, itemWidth, itemHeight);
                if (itemRect.xMax > screenRect.x + gridWidth)
                    itemRect = new Rect(screenRect.x + spaceBetweenTiles, itemRect.y + itemHeight, itemRect.width, itemRect.height);

                ++index;
            }

            GUI.EndScrollView();
        }

        public override int GetDisplayItemCount()
        {
            float itemWidth = itemSize + itemPadding * 2;
            float itemHeight = itemSize + itemLabelHeight + itemLabelTopPadding + itemPadding * 2;

            var gridWidth = m_DrawItemsRect.width;
            var itemCount = searchView.results.Count;
            int columnCount = (int)(gridWidth / itemWidth);
            int lineCount = Mathf.CeilToInt(itemCount / (float)columnCount);
            var gridHeight = lineCount * itemHeight - Styles.statusLabel.fixedHeight;
            var availableHeight = m_DrawItemsRect.height;

            if (gridHeight > availableHeight)
            {
                gridWidth -= Styles.scrollbar.fixedWidth;
                columnCount = (int)(gridWidth / itemWidth);
            }

            int rowCount = Mathf.Max(1, Mathf.RoundToInt(m_DrawItemsRect.height / itemHeight));
            int gridItemCount = rowCount * columnCount + 1;

            return Math.Max(0, Math.Min(itemCount, gridItemCount));
        }

        private void DrawGridItem(int index, SearchItem item, Rect itemRect, bool canHover, ICollection<int> selection, Event evt)
        {
            var backgroundRect = new Rect(itemRect.x + 1, itemRect.y + 1, itemRect.width - 2, itemRect.height - 2);
            var itemContent = canHover ? new GUIContent("", item.GetDescription(context, true)) : GUIContent.none;
            if (selection.Contains(index))
                GUI.Label(backgroundRect, itemContent, Styles.selectedGridItemBackground);
            else if (canHover)
                GUI.Label(backgroundRect, itemContent, itemRect.Contains(evt.mousePosition) ? Styles.itemGridBackground2 : Styles.itemGridBackground1);

            Texture2D thumbnail = null;
            var shouldFetchPreview = SearchSettings.fetchPreview && itemSize > 64;
            if (SearchSettings.fetchPreview && itemSize > 64)
            {
                thumbnail = item.preview;
                shouldFetchPreview = !thumbnail && item.provider.fetchPreview != null;
                if (shouldFetchPreview)
                {
                    var previewSize = new Vector2(itemSize, itemSize);
                    thumbnail = item.provider.fetchPreview(item, context, previewSize, FetchPreviewOptions.Preview2D | FetchPreviewOptions.Normal);
                    if (thumbnail)
                    {
                        item.preview = thumbnail;
                    }
                }
            }

            if (!thumbnail)
            {
                thumbnail = item.thumbnail;
                if (!thumbnail && item.provider.fetchThumbnail != null)
                {
                    thumbnail = item.provider.fetchThumbnail(item, context);
                    if (thumbnail && !shouldFetchPreview)
                        item.thumbnail = thumbnail;
                }
            }

            if (thumbnail)
            {
                var thumbnailRect = new Rect(itemRect.x + itemPadding, itemRect.y + itemPadding, itemSize, itemSize);
                var dw = thumbnailRect.width - thumbnail.width;
                var dh = thumbnailRect.height - thumbnail.height;
                if (dw > 0 || dh > 0)
                {
                    var scaledWidth = Mathf.Min(thumbnailRect.width, thumbnail.width);
                    var scaledHeight = Mathf.Min(thumbnailRect.height, thumbnail.height);
                    thumbnailRect = new Rect(
                        thumbnailRect.center.x - scaledWidth / 2f,
                        thumbnailRect.center.y - scaledHeight / 2f,
                        scaledWidth, scaledHeight);
                }
                GUI.DrawTexture(thumbnailRect, thumbnail, ScaleMode.ScaleToFit, true, 0f, Color.white, 0f, 4f);
            }

            var labelRect = new Rect(
                itemRect.x + itemPadding, itemRect.yMax - itemLabelHeight - itemPadding,
                itemRect.width - itemPadding * 2f, itemLabelHeight - itemPadding);
            var maxCharLength = Styles.itemLabelGrid.GetNumCharactersThatFitWithinWidth(item.GetLabel(context, true), itemRect.width * 2f);
            var itemLabel = item.GetLabel(context);
            if (itemLabel.Length > maxCharLength)
            {
                maxCharLength = Math.Max(0, maxCharLength - 3);
                itemLabel = Utils.StripHTML(itemLabel);
                itemLabel = itemLabel.Substring(0, maxCharLength / 2) + "\u2026" + itemLabel.Substring(itemLabel.Length - maxCharLength / 2);
            }
            GUI.Label(labelRect, itemLabel, Styles.itemLabelGrid);
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
    }
}
