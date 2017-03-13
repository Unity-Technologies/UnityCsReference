// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityEditor
{
    // Total size: 1) When changing: non changing rows + changing rows fraction, 2) When not changing sum of rows
    // Size of changing rows fraction used for finding new endRow after last changing row

    internal abstract class TreeViewGUIWithCustomItemsHeights : ITreeViewGUI
    {
        private List<Rect> m_RowRects = new List<Rect>();
        private float m_MaxWidthOfRows;
        protected readonly TreeViewController m_TreeView;

        public TreeViewGUIWithCustomItemsHeights(TreeViewController treeView)
        {
            m_TreeView = treeView;
        }

        public virtual void OnInitialize()
        {
        }

        public Rect GetRowRect(int row, float rowWidth)
        {
            if (m_RowRects.Count == 0)
            {
                Debug.LogError("Ensure precalc rects");
                return new Rect();
            }

            return m_RowRects[row];
        }

        public Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            return new Rect();
        }

        public Rect GetRectForFraming(int row)
        {
            return GetRowRect(row, 1);
        }

        public abstract void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused);

        protected virtual float AddSpaceBefore(TreeViewItem item)
        {
            return 0;
        }

        protected virtual Vector2 GetSizeOfRow(TreeViewItem item)
        {
            return new Vector2(m_TreeView.GetTotalRect().width, 16);
        }

        public void CalculateRowRects()
        {
            if (m_TreeView.isSearching)
                return;
            const float startY = 2f;
            var rows = m_TreeView.data.GetRows();
            m_RowRects = new List<Rect>(rows.Count);
            float curY = startY;
            m_MaxWidthOfRows = 1f;
            for (int i = 0; i < rows.Count; ++i)
            {
                TreeViewItem item = rows[i];
                float space = AddSpaceBefore(item);
                curY += space;
                Vector2 rowSize = GetSizeOfRow(item);
                m_RowRects.Add(new Rect(0, curY, rowSize.x, rowSize.y));
                curY += rowSize.y;
                if (rowSize.x > m_MaxWidthOfRows)
                    m_MaxWidthOfRows = rowSize.x;
            }
        }

        // Calc correct width if horizontal scrollbar is wanted return new Vector2(1, height)
        public Vector2 GetTotalSize()
        {
            if (m_RowRects.Count == 0)
                return new Vector2(0, 0);

            return new Vector2(m_MaxWidthOfRows, m_RowRects[m_RowRects.Count - 1].yMax);
        }

        public int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView)
        {
            Debug.LogError("GetNumRowsOnPageUpDown: Not impemented");
            return (int)Mathf.Floor(heightOfTreeView / 30); // return something
        }

        // Should return the row number of the first and last row thats fits in the pixel rect defined by top and height
        public void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
        {
            float topPixel = m_TreeView.state.scrollPos.y;
            float heightInPixels = m_TreeView.GetTotalRect().height;

            var rowCount = m_TreeView.data.rowCount;
            if (rowCount != m_RowRects.Count)
            {
                Debug.LogError("Mismatch in state: rows vs cached rects. Did you remember to hook up: dataSource.onVisibleRowsChanged += gui.CalculateRowRects ?");
                CalculateRowRects();
            }

            int firstVisible = -1;
            int lastVisible = -1;

            for (int i = 0; i < m_RowRects.Count; ++i)
            {
                bool visible = ((m_RowRects[i].y > topPixel && (m_RowRects[i].y < topPixel + heightInPixels))) ||
                    ((m_RowRects[i].yMax > topPixel && (m_RowRects[i].yMax < topPixel + heightInPixels)));

                if (visible)
                {
                    if (firstVisible == -1)
                        firstVisible = i;
                    lastVisible = i;
                }
            }

            if (firstVisible != -1 && lastVisible != -1)
            {
                firstRowVisible = firstVisible;
                lastRowVisible = lastVisible;
            }
            else
            {
                firstRowVisible = 0;
                lastRowVisible = rowCount - 1;
            }
        }

        public virtual void BeginRowGUI()
        {
        }

        public virtual void EndRowGUI()
        {
        }

        public virtual void BeginPingItem(TreeViewItem item, float topPixelOfRow, float availableWidth)
        {
            throw new NotImplementedException();
        }

        public virtual void EndPingItem()
        {
            throw new NotImplementedException();
        }

        public virtual bool BeginRename(TreeViewItem item, float delay)
        {
            throw new NotImplementedException();
        }

        public virtual void EndRename()
        {
            throw new NotImplementedException();
        }

        public virtual float halfDropBetweenHeight
        {
            get { return 8f; }
        }
        public virtual float topRowMargin { get; private set; }
        public virtual float bottomRowMargin { get; private set; }

        protected float m_BaseIndent = 2f;
        protected float m_IndentWidth = 14f;
        protected float m_FoldoutWidth = 12f;
        protected float indentWidth { get { return m_IndentWidth; } }

        virtual public float GetFoldoutIndent(TreeViewItem item)
        {
            // Ignore depth when showing search results
            if (m_TreeView.isSearching)
                return m_BaseIndent;

            return m_BaseIndent + item.depth * indentWidth;
        }

        virtual public float GetContentIndent(TreeViewItem item)
        {
            return GetFoldoutIndent(item) + m_FoldoutWidth;
        }
    }
}
