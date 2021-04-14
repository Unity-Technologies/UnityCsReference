// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    [Serializable]
    public class MultiColumnHeaderState
    {
        [SerializeField] Column[] m_Columns;
        [SerializeField] int[] m_VisibleColumns;
        [SerializeField] List<int> m_SortedColumns;               // First item is most recent
        [NonSerialized] int m_MaxNumberOfSortedColumns = 3;

        [Serializable]
        public class Column
        {
            [SerializeField] public float width = 50;
            [SerializeField] public bool sortedAscending;
            [SerializeField] public GUIContent headerContent = new GUIContent();
            [SerializeField] public string contextMenuText;
            [SerializeField] public TextAlignment headerTextAlignment = TextAlignment.Left;
            [SerializeField] public TextAlignment sortingArrowAlignment = TextAlignment.Center;
            [SerializeField] public float minWidth = 20;
            [SerializeField] public float maxWidth = 1000000f;
            [SerializeField] public bool autoResize = true;
            [SerializeField] public bool allowToggleVisibility = true;
            [SerializeField] public bool canSort = true;
            [SerializeField] public int userData;
            [NonSerialized] internal object userDataObj;
        }

        public static bool CanOverwriteSerializedFields(MultiColumnHeaderState source, MultiColumnHeaderState destination)
        {
            if (source == null || destination == null)
                return false;

            if (source.m_Columns == null || destination.m_Columns == null)
                return false;

            if (source.m_Columns.GetType() != destination.m_Columns.GetType())
                return false;

            return source.m_Columns.Length == destination.m_Columns.Length;
        }

        public static void OverwriteSerializedFields(MultiColumnHeaderState source, MultiColumnHeaderState destination)
        {
            if (!CanOverwriteSerializedFields(source, destination))
            {
                Debug.LogError("MultiColumnHeaderState: Not able to overwrite serialized fields");
                return;
            }

            destination.m_VisibleColumns = source.m_VisibleColumns.ToArray();
            destination.m_SortedColumns = new List<int>(source.m_SortedColumns);

            for (int i = 0; i < destination.m_Columns.Length; ++i)
            {
                destination.m_Columns[i].width = source.m_Columns[i].width;
                destination.m_Columns[i].sortedAscending = source.m_Columns[i].sortedAscending;
            }
        }

        public MultiColumnHeaderState(Column[] columns)
        {
            if (columns == null)
                throw new ArgumentException("columns are no allowed to be null", "columns");
            if (columns.Length == 0)
                throw new ArgumentException("columns array should at least have one column: it is empty", "columns");

            m_Columns = columns;
            m_SortedColumns = new List<int>();

            // Default to all visible
            m_VisibleColumns = new int[m_Columns.Length];
            for (int i = 0; i < m_Columns.Length; ++i)
                m_VisibleColumns[i] = i;
        }

        public int sortedColumnIndex
        {
            get
            {
                return m_SortedColumns.Count > 0 ? m_SortedColumns[0] : -1;
            }
            set
            {
                int current = m_SortedColumns.Count > 0 ? m_SortedColumns[0] : -1;
                if (value != current)
                {
                    if (value >= 0)
                    {
                        m_SortedColumns.Remove(value);
                        m_SortedColumns.Insert(0, value);
                    }
                    else
                    {
                        m_SortedColumns.Clear();
                    }
                }
            }
        }

        internal void SwapColumns(int columnIndex, int targetColumnIndex)
        {
            if (Mathf.Abs(columnIndex - targetColumnIndex) != 1)
                throw new ArgumentException("SwapColumn is only supported for columns next to each other otherwise visible columns and sorted columns will be incorrect. Column index " + columnIndex + ", targetColumnIndex " + targetColumnIndex);

            // Swap columns
            var temp = columns[columnIndex];
            columns[columnIndex] = columns[targetColumnIndex];
            columns[targetColumnIndex] = temp;

            // Swap visible columns state so it matches swapped columns above
            for (int i = 0; i < m_VisibleColumns.Length; i++)
            {
                if (m_VisibleColumns[i] == columnIndex)
                    m_VisibleColumns[i] = targetColumnIndex;
                else if (m_VisibleColumns[i] == targetColumnIndex)
                    m_VisibleColumns[i] = columnIndex;
            }
            var list = new List<int>(m_VisibleColumns);
            list.Sort();
            m_VisibleColumns = list.ToArray();

            // Swap sorted column state so it matches swapped columns above
            for (int i = 0; i < m_SortedColumns.Count; i++)
            {
                if (m_SortedColumns[i] == columnIndex)
                    m_SortedColumns[i] = targetColumnIndex;
                else if (m_SortedColumns[i] == targetColumnIndex)
                    m_SortedColumns[i] = columnIndex;
            }
        }

        void RemoveInvalidSortingColumnsIndices()
        {
            m_SortedColumns.RemoveAll(x => x >= m_Columns.Length);
            if (m_SortedColumns.Count > m_MaxNumberOfSortedColumns)
                m_SortedColumns.RemoveRange(m_MaxNumberOfSortedColumns, m_SortedColumns.Count - m_MaxNumberOfSortedColumns);
        }

        public int maximumNumberOfSortedColumns
        {
            get { return m_MaxNumberOfSortedColumns; }
            set
            {
                m_MaxNumberOfSortedColumns = value;
                RemoveInvalidSortingColumnsIndices();
            }
        }

        public int[] sortedColumns
        {
            get { return m_SortedColumns.ToArray(); }
            set
            {
                m_SortedColumns = value == null ? new List<int>() : new List<int>(value);
                RemoveInvalidSortingColumnsIndices();
            }
        }

        public Column[] columns
        {
            get { return m_Columns; }
        }

        public int[] visibleColumns
        {
            get { return m_VisibleColumns; }
            set
            {
                if (value == null)
                    throw new ArgumentException("visibleColumns should not be set to null");
                if (value.Length == 0)
                    throw new ArgumentException("visibleColumns should should not be set to an empty array. At least one visible column is required.");
                m_VisibleColumns = value;
            }
        }

        public float widthOfAllVisibleColumns
        {
            get { return visibleColumns.Sum(t => columns[t].width); }
        }
    }
}
