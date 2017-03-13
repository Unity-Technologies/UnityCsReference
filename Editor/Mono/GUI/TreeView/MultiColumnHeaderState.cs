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
            // Only serialize the fields that can be changed by the user (the others are reconstructed)
            [SerializeField] public float width = 50;
            [SerializeField] public bool sortedAscending;
            [NonSerialized] public GUIContent headerContent = new GUIContent();
            [NonSerialized] public string contextMenuText;
            [NonSerialized] public TextAlignment headerTextAlignment = TextAlignment.Left;
            [NonSerialized] public TextAlignment sortingArrowAlignment = TextAlignment.Center;
            [NonSerialized] public float minWidth = 20;
            [NonSerialized] public float maxWidth = 1000000f;
            [NonSerialized] public bool autoResize = true;
            [NonSerialized] public bool allowToggleVisibility = true;
            [NonSerialized] public bool canSort = true;
        }

        public static bool CanOverwriteSerializedFields(MultiColumnHeaderState source, MultiColumnHeaderState destination)
        {
            if (source == null || destination == null)
                return false;

            if (source.m_Columns == null || destination.m_Columns == null)
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
