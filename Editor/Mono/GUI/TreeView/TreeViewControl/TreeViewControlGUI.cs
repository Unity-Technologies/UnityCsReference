// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public partial class TreeView
    {
        class TreeViewControlGUI : TreeViewGUI
        {
            readonly TreeView m_Owner;
            List<Rect> m_RowRects;                           // If m_RowRects is null fixed row height is used
            Rect[] m_CellRects;                              // Allocated once and reused
            const float k_BackgroundWidth = 100000f;         // The TreeView can have a horizontal scrollbar so ensure to fill out the entire width of the background
            public float borderWidth = 1f;

            public TreeViewControlGUI(TreeViewController treeView, TreeView owner)
                : base(treeView)
            {
                m_Owner = owner;
                cellMargin = MultiColumnHeader.DefaultGUI.columnContentMargin;
            }

            public void RefreshRowRects(IList<TreeViewItem> rows)
            {
                if (m_RowRects == null)
                    m_RowRects = new List<Rect>(rows.Count);

                if (m_RowRects.Capacity < rows.Count)
                    m_RowRects.Capacity = rows.Count;

                m_RowRects.Clear();
                float curY = k_TopRowMargin;
                for (int row = 0; row < rows.Count; ++row)
                {
                    float height = m_Owner.GetCustomRowHeight(row, rows[row]);
                    m_RowRects.Add(new Rect(0f, curY, 1f, height)); // row width is updatd to the actual visibleRect of the TreeView's scrollview on the fly
                    curY += height;
                }
            }

            public float cellMargin { get; set; }

            public float foldoutWidth
            {
                get { return Styles.foldoutWidth; }
            }

            public int columnIndexForTreeFoldouts { get; set; } // column 0 is the default column for the tree foldouts

            public float totalHeight
            {
                get { return (useCustomRowRects ? customRowsTotalHeight : base.GetTotalSize().y) + (m_Owner.multiColumnHeader != null ? m_Owner.multiColumnHeader.height : 0f); }
            }

            public override Vector2 GetTotalSize()
            {
                Vector2 contentSize = useCustomRowRects ? new Vector2(1, customRowsTotalHeight) : base.GetTotalSize();

                // If we have multi column state we use the width of the columns so we show the horizontal scrollbar if needed
                if (m_Owner.multiColumnHeader != null)
                    contentSize.x = Mathf.Floor(m_Owner.multiColumnHeader.state.widthOfAllVisibleColumns);
                return contentSize;
            }

            bool useCustomRowRects
            {
                get { return m_RowRects != null; }
            }

            float customRowsTotalHeight
            {
                get
                {
                    return
                        (m_RowRects.Count > 0 ? m_RowRects[m_RowRects.Count - 1].yMax : 0f) + k_BottomRowMargin
                        - (m_TreeView.expansionAnimator.isAnimating ? m_TreeView.expansionAnimator.deltaHeight : 0f);
                }
            }

            // We only override DrawIconAndLabel (and not OnRowGUI) so the user only have to care about item content rendering; and not drag marker rendering, renaming and foldout button
            protected override void OnContentGUI(Rect rect, int row, TreeViewItem item, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
            {
                // We do not support pinging in the TreeView (to simplify api)
                if (isPinging)
                    return;

                // Make sure the GUI contents for each row is starting with its own controlID unique to its item. This prevents key-focus of a control (e.g a float field)
                // to jump from row to row when mouse scrolling (due to culling of rows and the assigning of controlIDs in call-order)
                GUIUtility.GetControlID(TreeViewController.GetItemControlID(item), FocusType.Passive);

                if (m_Owner.m_OverriddenMethods.hasRowGUI)
                {
                    var args = new RowGUIArgs
                    {
                        rowRect = rect,
                        row = row,
                        item = item,
                        label = label,
                        selected = selected,
                        focused = focused,
                        isRenaming = IsRenaming(item.id),
                    };

                    // For multi column header we call OnRowGUI for each cell with the cell rect
                    if (m_Owner.multiColumnHeader != null)
                    {
                        //args.columnInfo = m_ColumnInfo;
                        var visibleColumns = m_Owner.multiColumnHeader.state.visibleColumns;
                        if (m_CellRects == null || m_CellRects.Length != visibleColumns.Length)
                            m_CellRects = new Rect[visibleColumns.Length];

                        var columns = m_Owner.multiColumnHeader.state.columns;
                        var columnRect = args.rowRect;
                        for (int i = 0; i < visibleColumns.Length; ++i)
                        {
                            var columnData = columns[visibleColumns[i]];
                            columnRect.width = columnData.width;

                            m_CellRects[i] = columnRect;

                            // Use cell margins for all columns except the column with the tree view foldouts
                            // since that column assumes full cell rect for rename overlay placement
                            if (columnIndexForTreeFoldouts != visibleColumns[i])
                            {
                                m_CellRects[i].x += cellMargin;
                                m_CellRects[i].width -= 2 * cellMargin;
                            }

                            columnRect.x += columnData.width;
                        }

                        args.columnInfo = new RowGUIArgs.MultiColumnInfo(m_Owner.multiColumnHeader.state, m_CellRects);
                    }

                    m_Owner.RowGUI(args);
                }
                else
                {
                    // Default item gui
                    base.OnContentGUI(rect, row, item, label, selected, focused, useBoldFont, false);
                }
            }

            internal void DefaultRowGUI(RowGUIArgs args)
            {
                base.OnContentGUI(args.rowRect, args.row, args.item, args.label, args.selected, args.focused, false, false);
            }

            protected override Rect DoFoldout(Rect rowRect, TreeViewItem item, int row)
            {
                // For multicolumn setup we need to ensure foldouts are clipped so they are not in the next column
                if (m_Owner.multiColumnHeader != null)
                    return DoMultiColumnFoldout(rowRect, item, row);

                return base.DoFoldout(rowRect, item, row);
            }

            Rect DoMultiColumnFoldout(Rect rowRect, TreeViewItem item, int row)
            {
                if (!m_Owner.multiColumnHeader.IsColumnVisible(columnIndexForTreeFoldouts))
                    return new Rect();

                Rect cellRect = m_Owner.GetCellRectForTreeFoldouts(rowRect);

                // Clip foldout arrow if the cell is not big enough
                if (GetContentIndent(item) > cellRect.width)
                {
                    GUIClip.Push(cellRect, Vector2.zero, Vector2.zero, false);
                    cellRect.x = cellRect.y = 0f; // set guiclip coords
                    Rect foldoutRect = base.DoFoldout(cellRect, item, row);
                    GUIClip.Pop();
                    return foldoutRect;
                }

                return base.DoFoldout(cellRect, item, row);
            }

            public override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
            {
                if (m_Owner.m_OverriddenMethods.hasGetRenameRect)
                {
                    return m_Owner.GetRenameRect(rowRect, row, item);
                }

                return base.GetRenameRect(rowRect, row, item);
            }

            public Rect DefaultRenameRect(Rect rowRect, int row, TreeViewItem item)
            {
                return base.GetRenameRect(rowRect, row, item);
            }

            public override void BeginRowGUI()
            {
                base.BeginRowGUI();

                if (m_Owner.isDragging && m_Owner.multiColumnHeader != null && columnIndexForTreeFoldouts > 0)
                {
                    // Adjust insertion marker for multi column setups
                    int visibleColumnIndex = m_Owner.multiColumnHeader.GetVisibleColumnIndex(columnIndexForTreeFoldouts);
                    extraInsertionMarkerIndent = m_Owner.multiColumnHeader.GetColumnRect(visibleColumnIndex).x;
                }

                m_Owner.BeforeRowsGUI();
            }

            public override void EndRowGUI()
            {
                base.EndRowGUI();
                m_Owner.AfterRowsGUI();
            }

            protected override void RenameEnded()
            {
                var renameState = m_TreeView.state.renameOverlay;
                var renameEndedArgs = new RenameEndedArgs
                {
                    acceptedRename = renameState.userAcceptedRename,
                    itemID = renameState.userData,
                    originalName = renameState.originalName,
                    newName = renameState.name
                };
                m_Owner.RenameEnded(renameEndedArgs);
            }

            public override Rect GetRowRect(int row, float rowWidth)
            {
                if (!useCustomRowRects)
                {
                    return base.GetRowRect(row, rowWidth);
                }

                if (row < 0 || row >= m_RowRects.Count)
                {
                    throw new ArgumentOutOfRangeException("row", string.Format("Input row index: {0} is invalid. Number of rows rects: {1}. (Number of rows: {2})", row, m_RowRects.Count, m_Owner.m_DataSource.rowCount));
                }

                Rect rowRect = m_RowRects[row];
                rowRect.width = rowWidth;
                return rowRect;
            }

            public override Rect GetRectForFraming(int row)
            {
                return GetRowRect(row, 1);
            }

            // Should return the row number of the first and last row thats fits in the pixel rect defined by top and height
            public override void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
            {
                if (!useCustomRowRects)
                {
                    base.GetFirstAndLastRowVisible(out firstRowVisible, out lastRowVisible);
                    return;
                }

                if (m_TreeView.data.rowCount == 0)
                {
                    firstRowVisible = lastRowVisible = -1;
                    return;
                }

                var rowCount = m_TreeView.data.rowCount;
                if (rowCount != m_RowRects.Count)
                {
                    m_RowRects = null;
                    throw new InvalidOperationException(string.Format("Number of rows does not match number of row rects. Did you remember to update the row rects when BuildRootAndRows was called? Number of rows: {0}, number of custom row rects: {1}. Falling back to fixed row height.", rowCount, m_RowRects.Count));
                }

                float topPixel = m_TreeView.state.scrollPos.y;
                float heightInPixels = m_TreeView.GetTotalRect().height;

                int firstVisible = -1;
                int lastVisible = -1;
                for (int i = 0; i < m_RowRects.Count; ++i)
                {
                    bool visible =  ((m_RowRects[i].y > topPixel && (m_RowRects[i].y < topPixel + heightInPixels))) ||
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

            protected override void DrawItemBackground(Rect rect, int row, TreeViewItem item, bool selected, bool focused)
            {
                // To ensure backgrounds are also animated when animating expansion we render the rows backgrounds individually here. When not animating expansion the backgrounds
                // are rendered in one go in DrawAlternatingRowBackgrounds() which also handles background outside rows
                if (m_Owner.showAlternatingRowBackgrounds && m_TreeView.animatingExpansion)
                {
                    rect.width = k_BackgroundWidth;
                    var bgStyle = row % 2 == 0 ? DefaultStyles.backgroundEven : DefaultStyles.backgroundOdd;
                    bgStyle.Draw(rect, false, false, false, false);
                }
            }

            public void DrawAlternatingRowBackgrounds()
            {
                if (Event.current.rawType != EventType.Repaint)
                    return;

                // Render entire bg
                float contentYMax = m_Owner.treeViewRect.height + m_Owner.state.scrollPos.y;

                DefaultStyles.backgroundOdd.Draw(new Rect(0, 0, k_BackgroundWidth, contentYMax), false, false, false, false);

                int first = 0;
                var numRows = m_Owner.GetRows().Count;
                if (numRows > 0)
                {
                    int last;
                    GetFirstAndLastRowVisible(out first, out last);
                    if (first < 0 || first >= numRows)
                        return;
                }

                // Render every 2nd row for the entire background (we also show alternating colors below the last item)
                Rect rowRect = new Rect(0, 0, 0, m_Owner.rowHeight);
                for (int row = first; rowRect.yMax < contentYMax; row++)
                {
                    if (row % 2 == 1)
                        continue;

                    if (row < numRows)
                        rowRect = m_Owner.GetRowRect(row);
                    else if (row > 0)
                        rowRect.y += rowRect.height * 2;
                    rowRect.width = k_BackgroundWidth;

                    DefaultStyles.backgroundEven.Draw(rowRect, false, false, false, false);
                }
            }

            // Returns the rect available within the borders
            public Rect DoBorder(Rect rect)
            {
                EditorGUI.DrawOutline(rect, borderWidth, EditorGUI.kSplitLineSkinnedColor.color);
                return new Rect(rect.x + borderWidth, rect.y + borderWidth, rect.width - 2 * borderWidth, rect.height - 2 * borderWidth);
            }
        }
    }
}
