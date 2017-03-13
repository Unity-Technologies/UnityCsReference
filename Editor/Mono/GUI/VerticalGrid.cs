// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class VerticalGrid
    {
        int m_Columns = 1;
        int m_Rows;
        float m_Height;
        float m_HorizontalSpacing;

        public int columns { get  {return m_Columns; } }
        public int rows { get {return m_Rows; } }
        public float height { get {return m_Height; }  }
        public float horizontalSpacing { get {return m_HorizontalSpacing; } }

        // Adjust grid values
        public float fixedWidth { get; set; }
        public Vector2 itemSize { get; set; }
        public float verticalSpacing { get; set; }      // spacing between each row of items
        public float minHorizontalSpacing { get; set; } // minimum spacing between each column in grid
        public float topMargin { get; set; }
        public float bottomMargin { get; set; }
        public float rightMargin { get; set; }
        public float leftMargin { get; set; }
        public float fixedHorizontalSpacing {get; set; }
        public bool useFixedHorizontalSpacing {get; set; }

        // Call after setting parameters above and before using CalcRect
        public void InitNumRowsAndColumns(int itemCount, int maxNumRows)
        {
            if (useFixedHorizontalSpacing)
            {
                // Set columns (with an excess of 1 fixedHorizontalSpacing)
                m_Columns = CalcColumns();

                // Set horizontal spacing
                m_HorizontalSpacing = fixedHorizontalSpacing;

                // Set rows
                m_Rows = Mathf.Min(maxNumRows, CalcRows(itemCount));

                // Set height
                m_Height = m_Rows * (itemSize.y + verticalSpacing) - verticalSpacing + topMargin + bottomMargin;
            }
            else // center columns
            {
                // Set columns (with an excess of 1 minHorizontalSpacing)
                m_Columns = CalcColumns();

                // Set horizontal spacing
                m_HorizontalSpacing = Mathf.Max(0f, (fixedWidth - (m_Columns * itemSize.x + leftMargin + rightMargin)) / (m_Columns));

                // Set rows
                m_Rows = Mathf.Min(maxNumRows, CalcRows(itemCount));

                if (m_Rows == 1)
                    m_HorizontalSpacing = minHorizontalSpacing;

                // Set height
                m_Height = m_Rows * (itemSize.y + verticalSpacing) - verticalSpacing + topMargin + bottomMargin;
            }
        }

        public int CalcColumns()
        {
            float horizontalSpacing = useFixedHorizontalSpacing ? fixedHorizontalSpacing : minHorizontalSpacing;
            int cols = (int)Mathf.Floor((fixedWidth - leftMargin - rightMargin) / (itemSize.x + horizontalSpacing));
            cols = Mathf.Max(cols, 1);
            return cols;
        }

        public int CalcRows(int itemCount)
        {
            int t = (int)Mathf.Ceil(itemCount / (float)CalcColumns());
            if (t < 0)
                return int.MaxValue;
            return t;
        }

        public Rect CalcRect(int itemIdx, float yOffset)
        {
            float row = Mathf.Floor(itemIdx / columns);
            float column = itemIdx - row * columns;

            if (useFixedHorizontalSpacing)
            {
                return new Rect(leftMargin + column * (itemSize.x + fixedHorizontalSpacing),
                    row * (itemSize.y + verticalSpacing) + topMargin + yOffset,
                    itemSize.x,
                    itemSize.y);
            }
            else
            {
                return new Rect(leftMargin + horizontalSpacing * 0.5f + column * (itemSize.x + horizontalSpacing),
                    row * (itemSize.y + verticalSpacing) + topMargin + yOffset,
                    itemSize.x,
                    itemSize.y);
            }
        }

        public int GetMaxVisibleItems(float height)
        {
            int visibleRows = (int)Mathf.Ceil((height - topMargin - bottomMargin) / (itemSize.y + verticalSpacing));
            return visibleRows * columns;
        }

        public bool IsVisibleInScrollView(float scrollViewHeight, float scrollPos, float gridStartY, int maxIndex, out int startIndex, out int endIndex)
        {
            startIndex = endIndex = 0;

            // In grid coordinates

            float scrollViewStart = scrollPos;
            float scrollViewEnd = scrollPos + scrollViewHeight;

            float offsetY = gridStartY + topMargin;

            // Entirely below view
            if (offsetY > scrollViewEnd)
                return false;

            // Entirely above view
            if (offsetY + height < scrollViewStart)
                return false;

            float itemHeightAndSpacing = itemSize.y + verticalSpacing;

            // startRow can be negative if grid is starting in the middle of the view
            int startRow = Mathf.FloorToInt((scrollViewStart - offsetY) / itemHeightAndSpacing);
            startIndex = startRow * columns;
            startIndex = Mathf.Clamp(startIndex, 0, maxIndex);

            // endRow can be negative if grid is starting in the middle of the view
            int endRow = Mathf.FloorToInt((scrollViewEnd - offsetY) / itemHeightAndSpacing);
            endIndex = (endRow + 1) * columns - 1;
            endIndex = Mathf.Clamp(endIndex, 0, maxIndex);

            return true;
        }

        public override string ToString()
        {
            return string.Format("VerticalGrid: rows {0}, columns {1}, fixedWidth {2}, itemSize {3}", rows, columns, fixedWidth, itemSize);
        }
    }


    internal class VerticalGridWithSplitter
    {
        int m_Columns = 1;
        int m_Rows;
        float m_Height;
        float m_HorizontalSpacing;

        public int columns { get  {return m_Columns; } }
        public int rows { get {return m_Rows; } }
        public float height { get {return m_Height; }  }
        public float horizontalSpacing { get {return m_HorizontalSpacing; } }

        // Adjust grid values
        public float fixedWidth { get; set; }
        public Vector2 itemSize { get; set; }
        public float verticalSpacing { get; set; }      // spacing between each row of items
        public float minHorizontalSpacing { get; set; } // minimum spacing between each column in grid
        public float topMargin { get; set; }
        public float bottomMargin { get; set; }
        public float rightMargin { get; set; }
        public float leftMargin { get; set; }

        // Call after setting parameters above and before using CalcRect
        public void InitNumRowsAndColumns(int itemCount, int maxNumRows)
        {
            // Set columns (with an excess of 1 minHorizontalSpacing)
            m_Columns = (int)Mathf.Floor((fixedWidth - leftMargin - rightMargin) / (itemSize.x + minHorizontalSpacing));
            m_Columns = Mathf.Max(m_Columns, 1);

            // Set horizontal spacing
            m_HorizontalSpacing = 0f;
            if (m_Columns > 1)
                m_HorizontalSpacing = (fixedWidth - (m_Columns * itemSize.x + leftMargin + rightMargin)) / (m_Columns - 1);

            // Set rows
            m_Rows = Mathf.Min(maxNumRows, (int)Mathf.Ceil(itemCount / (float)m_Columns));

            // Set height
            m_Height = m_Rows * (itemSize.y + verticalSpacing) - verticalSpacing + topMargin + bottomMargin;
        }

        public Rect CalcRect(int itemIdx, float yOffset)
        {
            float row = Mathf.Floor(itemIdx / columns);
            float column = itemIdx - row * columns;

            return new Rect(column * (itemSize.x + horizontalSpacing) + leftMargin,
                row * (itemSize.y + verticalSpacing) + topMargin + yOffset,
                itemSize.x,
                itemSize.y);
        }

        public int GetMaxVisibleItems(float height)
        {
            int visibleRows = (int)Mathf.Ceil((height - topMargin - bottomMargin) / (itemSize.y + verticalSpacing));
            return visibleRows * columns;
        }

        int m_SplitAfterRow;
        float m_CurrentSplitHeight;
        float m_LastSplitUpdate;
        float m_TargetSplitHeight;

        public void ResetSplit()
        {
            m_SplitAfterRow = -1;
            m_CurrentSplitHeight = 0f;
            m_LastSplitUpdate = -1f;
            m_TargetSplitHeight = 0f;
        }

        public void OpenSplit(int splitAfterRowIndex, int numItems)
        {
            int numRows = (int)Mathf.Ceil(numItems / (float)m_Columns);
            float splitHeight = numRows * (itemSize.y + verticalSpacing) - verticalSpacing + topMargin + bottomMargin;
            m_SplitAfterRow = splitAfterRowIndex;
            m_TargetSplitHeight = splitHeight;
            m_LastSplitUpdate = Time.realtimeSinceStartup;
        }

        // Returns Rect of split content starting from index 0
        public Rect CalcSplitRect(int splitIndex, float yOffset)
        {
            Rect rect = new Rect(0, 0, 0, 0);

            return rect;
        }

        public void CloseSplit()
        {
            m_TargetSplitHeight = 0f;
        }

        // Returns true if animating (client should ensure to repaint in this case)
        public bool UpdateSplitAnimationOnGUI()
        {
            if (m_SplitAfterRow != -1)
            {
                float delta =  Time.realtimeSinceStartup - m_LastSplitUpdate;
                m_CurrentSplitHeight = delta * m_TargetSplitHeight;

                m_LastSplitUpdate = Time.realtimeSinceStartup;

                // Animate
                if (m_CurrentSplitHeight != m_TargetSplitHeight && Event.current.type == EventType.Repaint)
                {
                    m_CurrentSplitHeight = Mathf.MoveTowards(m_CurrentSplitHeight, m_TargetSplitHeight, 0.03f);
                    if (m_CurrentSplitHeight == 0 && m_TargetSplitHeight == 0)
                    {
                        ResetSplit();
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
