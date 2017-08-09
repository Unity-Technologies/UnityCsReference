// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public partial class MultiColumnHeader
    {
        MultiColumnHeaderState m_State;
        float m_Height = DefaultGUI.defaultHeight;
        float m_DividerWidth = 6;
        Rect m_PreviousRect;
        bool m_ResizeToFit = false;
        bool m_CanSort = true;
        GUIView m_GUIView;
        Rect[] m_ColumnRects;

        public delegate void HeaderCallback(MultiColumnHeader multiColumnHeader);
        public event HeaderCallback sortingChanged;
        public event HeaderCallback visibleColumnsChanged;

        public MultiColumnHeader(MultiColumnHeaderState state)
        {
            m_State = state;
        }

        public int sortedColumnIndex
        {
            get { return state.sortedColumnIndex; }
            set
            {
                if (value != state.sortedColumnIndex)
                {
                    state.sortedColumnIndex = value;
                    OnSortingChanged();
                }
            }
        }

        public void SetSortingColumns(int[] columnIndices, bool[] sortAscending)
        {
            if (columnIndices == null)
                throw new ArgumentNullException("columnIndices");

            if (sortAscending == null)
                throw new ArgumentNullException("sortAscending");

            if (columnIndices.Length != sortAscending.Length)
                throw new ArgumentException("Input arrays should have same length");

            if (columnIndices.Length > state.maximumNumberOfSortedColumns)
                throw new ArgumentException("The maximum number of sorted columns is " + state.maximumNumberOfSortedColumns + ". Trying to set " + columnIndices.Length + " columns.");

            if (columnIndices.Length != columnIndices.Distinct().Count())
                throw new ArgumentException("Duplicate column indices are not allowed", "columnIndices");

            bool changed = false;

            if (!columnIndices.SequenceEqual(state.sortedColumns))
            {
                state.sortedColumns = columnIndices;
                changed = true;
            }

            for (int i = 0; i < columnIndices.Length; ++i)
            {
                var column = GetColumn(columnIndices[i]);
                if (column.sortedAscending != sortAscending[i])
                {
                    column.sortedAscending = sortAscending[i];
                    changed = true;
                }
            }

            if (changed)
                OnSortingChanged();
        }

        public void SetSorting(int columnIndex, bool sortAscending)
        {
            bool changed = false;
            if (state.sortedColumnIndex != columnIndex)
            {
                state.sortedColumnIndex = columnIndex;
                changed = true;
            }

            var column = GetColumn(columnIndex);
            if (column.sortedAscending != sortAscending)
            {
                column.sortedAscending = sortAscending;
                changed = true;
            }

            if (changed)
                OnSortingChanged();
        }

        public void SetSortDirection(int columnIndex, bool sortAscending)
        {
            var column = GetColumn(columnIndex);
            if (column.sortedAscending != sortAscending)
            {
                column.sortedAscending = sortAscending;
                OnSortingChanged();
            }
        }

        public bool IsSortedAscending(int columnIndex)
        {
            return GetColumn(columnIndex).sortedAscending;
        }

        public MultiColumnHeaderState.Column GetColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= state.columns.Length)
                throw new ArgumentOutOfRangeException("columnIndex", string.Format("columnIndex {0} is not valid when the current column count is {1}", columnIndex, state.columns.Length));
            return state.columns[columnIndex];
        }

        public MultiColumnHeaderState state
        {
            get { return m_State; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("state", "MultiColumnHeader state is not allowed to be null");
                m_State = value;
            }
        }

        public float height
        {
            get { return m_Height; }
            set { m_Height = value; }
        }

        public bool canSort
        {
            get { return m_CanSort; }
            set
            {
                m_CanSort = value;
                height = m_Height;
            }
        }

        public bool IsColumnVisible(int columnIndex)
        {
            return state.visibleColumns.Any(t => t == columnIndex);
        }

        public int GetVisibleColumnIndex(int columnIndex)
        {
            for (int i = 0; i < state.visibleColumns.Length; i++)
            {
                if (state.visibleColumns[i] == columnIndex)
                    return i;
            }
            string visibleIndices = string.Join(", ", state.visibleColumns.Select(t => t.ToString()).ToArray());
            throw new ArgumentException(string.Format("Invalid columnIndex: {0}. The index is not part of the current visible columns: {1}", columnIndex, visibleIndices), "columnIndex");
        }

        public Rect GetCellRect(int visibleColumnIndex, Rect rowRect)
        {
            Rect result = GetColumnRect(visibleColumnIndex);
            result.y = rowRect.y;
            result.height = rowRect.height;
            return result;
        }

        public Rect GetColumnRect(int visibleColumnIndex)
        {
            if (visibleColumnIndex < 0 || visibleColumnIndex >= m_ColumnRects.Length)
                throw new ArgumentException(string.Format("The provided visibleColumnIndex is invalid. Ensure the index ({0}) is within the number of visible columns ({1})", visibleColumnIndex, m_ColumnRects.Length), "visibleColumnIndex");

            return m_ColumnRects[visibleColumnIndex];
        }

        public void ResizeToFit()
        {
            m_ResizeToFit = true;
            Repaint();
        }

        void UpdateColumnHeaderRects(Rect totalHeaderRect)
        {
            if (m_ColumnRects == null || m_ColumnRects.Length != state.visibleColumns.Length)
                m_ColumnRects = new Rect[state.visibleColumns.Length];

            Rect curRect = totalHeaderRect;
            for (int v = 0; v < state.visibleColumns.Length; v++)
            {
                int columnIndex = state.visibleColumns[v];
                MultiColumnHeaderState.Column column = state.columns[columnIndex];

                if (v > 0)
                    curRect.x += curRect.width;
                curRect.width = column.width;

                m_ColumnRects[v] = curRect;
            }
        }

        // Virtual so clients can override header behavior and rendering entirely
        public virtual void OnGUI(Rect rect, float xScroll)
        {
            Event evt = Event.current;

            if (m_GUIView == null)
                m_GUIView = GUIView.current;

            DetectSizeChanges(rect);

            if (m_ResizeToFit && evt.type == EventType.Repaint)
            {
                m_ResizeToFit = false;
                ResizeColumnsWidthsProportionally(rect.width - GUI.skin.verticalScrollbar.fixedWidth - state.widthOfAllVisibleColumns);
            }

            // We create a guiclip to let the header be able to scroll horizontally according to the tree view's horizontal scroll
            GUIClip.Push(rect, new Vector2(-xScroll, 0f), Vector2.zero, false);
            {
                Rect localRect = new Rect(0, 0, rect.width, rect.height);

                // Background ( We always add the width of the vertical scrollbar to accomodate if this is being shown below e.g by a tree view)
                float widthOfAllColumns = state.widthOfAllVisibleColumns;
                float backgroundWidth = (localRect.width > widthOfAllColumns ? localRect.width : widthOfAllColumns) + GUI.skin.verticalScrollbar.fixedWidth;
                Rect backgroundRect = new Rect(0, 0, backgroundWidth, localRect.height);
                GUI.Label(backgroundRect, GUIContent.none, DefaultStyles.background);

                // Context menu
                if (evt.type == EventType.ContextClick && backgroundRect.Contains(evt.mousePosition))
                {
                    evt.Use();
                    DoContextMenu();
                }

                // Update column rects (cached for clients to have fast access to column rects by using GetCellRect)
                UpdateColumnHeaderRects(localRect);

                // Columns
                for (int v = 0; v < state.visibleColumns.Length; v++)
                {
                    int columnIndex = state.visibleColumns[v];
                    MultiColumnHeaderState.Column column = state.columns[columnIndex];

                    Rect headerRect = m_ColumnRects[v];
                    const float limitHeightOfDivider = 4f;
                    Rect dividerRect = new Rect(headerRect.xMax - 1, headerRect.y + limitHeightOfDivider, 1f, headerRect.height - 2 * limitHeightOfDivider);

                    // Resize columns logic
                    Rect dragRect = new Rect(dividerRect.x - m_DividerWidth * 0.5f, localRect.y, m_DividerWidth, localRect.height);
                    bool hasControl;
                    column.width = EditorGUI.WidthResizer(dragRect, column.width, column.minWidth, column.maxWidth, out hasControl);
                    if (hasControl && evt.type == EventType.Repaint)
                    {
                        DrawColumnResizing(headerRect, column);
                    }

                    // Draw divider (can be overridden)
                    DrawDivider(dividerRect, column);

                    // Draw header (can be overridden)
                    ColumnHeaderGUI(column, headerRect, columnIndex);
                }
            }
            GUIClip.Pop();
        }

        internal virtual void DrawColumnResizing(Rect headerRect, MultiColumnHeaderState.Column column)
        {
            const float margin = 1;
            headerRect.y += margin;
            headerRect.width -= margin;
            headerRect.height -= 2 * margin;
            EditorGUI.DrawRect(headerRect, new Color(0.5f, 0.5f, 0.5f, 0.1f));
        }

        internal virtual void DrawDivider(Rect dividerRect, MultiColumnHeaderState.Column column)
        {
            EditorGUI.DrawRect(dividerRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        protected virtual void ColumnHeaderClicked(MultiColumnHeaderState.Column column, int columnIndex)
        {
            if (state.sortedColumnIndex == columnIndex)
                column.sortedAscending = !column.sortedAscending;
            else
                state.sortedColumnIndex = columnIndex;

            OnSortingChanged();
        }

        protected virtual void OnSortingChanged()
        {
            if (sortingChanged != null)
                sortingChanged(this);
        }

        protected virtual void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            if (canSort && column.canSort)
            {
                SortingButton(column, headerRect, columnIndex);
            }

            GUIStyle style = GetStyle(column.headerTextAlignment);

            float labelHeight = EditorGUIUtility.singleLineHeight;
            Rect labelRect = new Rect(headerRect.x, headerRect.yMax - labelHeight - DefaultGUI.labelSpaceFromBottom, headerRect.width, labelHeight);
            GUI.Label(labelRect, column.headerContent, style);
        }

        protected void SortingButton(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            // Button logic
            if (EditorGUI.Button(headerRect, GUIContent.none, GUIStyle.none))
            {
                ColumnHeaderClicked(column, columnIndex);
            }

            // Draw sorting arrow
            if (columnIndex == state.sortedColumnIndex && Event.current.type == EventType.Repaint)
            {
                var arrowRect = GetArrowRect(column, headerRect);

                Matrix4x4 normalMatrix = GUI.matrix;
                if (column.sortedAscending)
                    GUIUtility.RotateAroundPivot(180, arrowRect.center - new Vector2(0, 1));

                GUI.Label(arrowRect, "\u25BE", DefaultStyles.arrowStyle);

                if (column.sortedAscending)
                    GUI.matrix = normalMatrix;
            }
        }

        internal virtual Rect GetArrowRect(MultiColumnHeaderState.Column column, Rect headerRect)
        {
            float sortingArrowWidth = DefaultStyles.arrowStyle.fixedWidth;
            float arrowYPos = headerRect.y;
            float arrowXPos = 0f;

            switch (column.sortingArrowAlignment)
            {
                case TextAlignment.Left:
                    arrowXPos = headerRect.x + DefaultStyles.columnHeader.padding.left;
                    break;
                case TextAlignment.Center:
                    arrowXPos = headerRect.x + headerRect.width * 0.5f - sortingArrowWidth * 0.5f;
                    break;
                case TextAlignment.Right:
                    arrowXPos = headerRect.xMax - DefaultStyles.columnHeader.padding.right - sortingArrowWidth;
                    break;
                default:
                    Debug.LogError("Unhandled enum");
                    break;
            }

            Rect arrowRect = new Rect(Mathf.Round(arrowXPos), arrowYPos, sortingArrowWidth, 16f);
            return arrowRect;
        }

        GUIStyle GetStyle(TextAlignment alignment)
        {
            switch (alignment)
            {
                case TextAlignment.Left: return DefaultStyles.columnHeader;
                case TextAlignment.Center: return DefaultStyles.columnHeaderCenterAligned;
                case TextAlignment.Right: return DefaultStyles.columnHeaderRightAligned;
                default: return DefaultStyles.columnHeader;
            }
        }

        void DoContextMenu()
        {
            var menu = new GenericMenu();
            AddColumnHeaderContextMenuItems(menu);
            menu.ShowAsContext();
        }

        protected virtual void AddColumnHeaderContextMenuItems(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Resize to Fit"), false, ResizeToFit);

            menu.AddSeparator("");

            for (int i = 0; i < state.columns.Length; ++i)
            {
                var column = state.columns[i];
                var menuText = !string.IsNullOrEmpty(column.contextMenuText) ? column.contextMenuText : column.headerContent.text;
                if (column.allowToggleVisibility)
                    menu.AddItem(new GUIContent(menuText), state.visibleColumns.Contains(i), ToggleVisibility, i);
                else
                    menu.AddDisabledItem(new GUIContent(menuText));
            }
        }

        protected virtual void OnVisibleColumnsChanged()
        {
            if (visibleColumnsChanged != null)
                visibleColumnsChanged(this);
        }

        void ToggleVisibility(object userData)
        {
            ToggleVisibility((int)userData);
        }

        protected virtual void ToggleVisibility(int columnIndex)
        {
            var newVisibleColumns = new List<int>(state.visibleColumns);
            if (newVisibleColumns.Contains(columnIndex))
            {
                newVisibleColumns.Remove(columnIndex);
            }
            else
            {
                newVisibleColumns.Add(columnIndex);
                newVisibleColumns.Sort();
            }
            state.visibleColumns = newVisibleColumns.ToArray();
            Repaint();

            OnVisibleColumnsChanged();
        }

        public void Repaint()
        {
            if (m_GUIView != null)
                m_GUIView.Repaint();
        }

        void DetectSizeChanges(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (m_PreviousRect.width > 0f)
                {
                    float deltaWidth = Mathf.Round(rect.width - m_PreviousRect.width);
                    if (deltaWidth != 0f)
                    {
                        float tep = GUI.skin.verticalScrollbar.fixedWidth;
                        bool isColumnsVisible = rect.width - tep > state.widthOfAllVisibleColumns;
                        if (isColumnsVisible || deltaWidth < 0f)
                            ResizeColumnsWidthsProportionally(deltaWidth);
                    }
                }
                m_PreviousRect = rect;
            }
        }

        void ResizeColumnsWidthsProportionally(float deltaWidth)
        {
            // Find auto resizing columns
            List<MultiColumnHeaderState.Column> autoResizeColumns = null;
            foreach (int i in state.visibleColumns)
            {
                MultiColumnHeaderState.Column column = state.columns[i];
                if (column.autoResize)
                {
                    // Ignore the columns that cannot expand anymore
                    if (deltaWidth > 0f && column.width >= column.maxWidth)
                        continue;
                    // Ignore the columns that cannot shrink anymore
                    if (deltaWidth < 0f && column.width <= column.minWidth)
                        continue;

                    if (autoResizeColumns == null)
                        autoResizeColumns = new List<MultiColumnHeaderState.Column>();

                    autoResizeColumns.Add(column);
                }
            }

            // Any auto resizing columns?
            if (autoResizeColumns == null)
                return;

            // Sum
            float totalAutoResizeWidth = autoResizeColumns.Sum(x => x.width);

            // Distribute
            foreach (var column in autoResizeColumns)
            {
                column.width += deltaWidth * (column.width / totalAutoResizeWidth);
                column.width = Mathf.Clamp(column.width, column.minWidth, column.maxWidth);
            }
        }
    }
}
