// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents specific data of a header.
    /// </summary>
    internal enum ColumnsDataType
    {
        /// <summary>
        /// Represents the primary column name of the header.
        /// </summary>
        PrimaryColumn,
        /// <summary>
        /// Represents the stretch mode of the header.
        /// </summary>
        StretchMode,
        /// <summary>
        /// Represents the ability for user to interactively reorder columns.
        /// </summary>
        Reorderable,
        /// <summary>
        /// Represents the ability for user to interactively resize columns.
        /// </summary>
        Resizable,
        /// <summary>
        /// Represents the value that indicates whether columns are resized as the user drags resize handles or only upon mouse release.
        /// </summary>
        ResizePreview,
    }

    /// <summary>
    /// Represents a collection of columns.
    /// </summary>
    [UxmlObject]
    public class Columns : ICollection<Column>
    {
        /// <summary>
        /// Indicates how the size of a stretchable column in this collection should get automatically adjusted as other columns or its containing view get resized.
        /// The default value is <see cref="StretchMode.Grow"/>.
        /// </summary>
        public enum StretchMode
        {
            /// <summary>
            /// The size of stretchable columns is automatically and proportionally adjusted only as its container gets resized.
            /// Unlike <see cref="StretchMode.GrowAndFill"/>, the size is not adjusted to fill any available space within its container when other columns are resized.
            /// </summary>
            Grow,
            /// <summary>
            /// The size of stretchable columns is automatically adjusted to fill the available space within its container when this container or other columns get resized
            /// </summary>
            GrowAndFill
        }

        [ExcludeFromDocs, Serializable]
        public class UxmlSerializedData : UIElements.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] string primaryColumnName;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags primaryColumnName_UxmlAttributeFlags;
            [SerializeField] StretchMode stretchMode;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags stretchMode_UxmlAttributeFlags;
            [SerializeField] bool reorderable;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags reorderable_UxmlAttributeFlags;
            [SerializeField] bool resizable;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags resizable_UxmlAttributeFlags;
            [SerializeField] bool resizePreview;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags resizePreview_UxmlAttributeFlags;
            [SerializeReference, UxmlObjectReference] List<Column.UxmlSerializedData> columns;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags columns_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new Columns();

            public override void Deserialize(object obj)
            {
                var e = (Columns)obj;
                if (ShouldWriteAttributeValue(primaryColumnName_UxmlAttributeFlags))
                    e.primaryColumnName = primaryColumnName;
                if (ShouldWriteAttributeValue(stretchMode_UxmlAttributeFlags))
                    e.stretchMode = stretchMode;
                if (ShouldWriteAttributeValue(reorderable_UxmlAttributeFlags))
                    e.reorderable = reorderable;
                if (ShouldWriteAttributeValue(resizable_UxmlAttributeFlags))
                    e.resizable = resizable;
                if (ShouldWriteAttributeValue(resizePreview_UxmlAttributeFlags))
                    e.resizePreview = resizePreview;

                if (ShouldWriteAttributeValue(columns_UxmlAttributeFlags) && columns != null)
                {
                    foreach (var columnData in columns)
                    {
                        var column = (Column)columnData.CreateInstance();
                        columnData.Deserialize(column);
                        e.Add(column);
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="Columns"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        internal class UxmlObjectFactory<T> : UxmlObjectFactory<T, UxmlObjectTraits<T>> where T : Columns, new() {}

        /// <summary>
        /// Instantiates a <see cref="Columns"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        internal class UxmlObjectFactory : UxmlObjectFactory<Columns> {}

        /// <summary>
        /// Defines <see cref="UxmlObjectTraits{T}"/> for the <see cref="Columns"/>.
        /// </summary>
        [Obsolete("UxmlObjectTraits<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        internal class UxmlObjectTraits<T> : UnityEngine.UIElements.UxmlObjectTraits<T> where T : Columns
        {
            readonly UxmlStringAttributeDescription m_PrimaryColumnName = new UxmlStringAttributeDescription { name = "primary-column-name" };
            readonly UxmlEnumAttributeDescription<StretchMode> m_StretchMode = new UxmlEnumAttributeDescription<StretchMode> { name = "stretch-mode", defaultValue = StretchMode.GrowAndFill };
            readonly UxmlBoolAttributeDescription m_Reorderable = new UxmlBoolAttributeDescription { name = "reorderable", defaultValue = true };
            readonly UxmlBoolAttributeDescription m_Resizable = new UxmlBoolAttributeDescription { name = "resizable", defaultValue = true };
            readonly UxmlBoolAttributeDescription m_ResizePreview = new UxmlBoolAttributeDescription { name = "resize-preview" };
            readonly UxmlObjectListAttributeDescription<Column> m_Columns = new UxmlObjectListAttributeDescription<Column>();

            public override void Init(ref T obj, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ref obj, bag, cc);

                obj.primaryColumnName = m_PrimaryColumnName.GetValueFromBag(bag, cc);
                obj.stretchMode = m_StretchMode.GetValueFromBag(bag, cc);
                obj.reorderable = m_Reorderable.GetValueFromBag(bag, cc);
                obj.resizable = m_Resizable.GetValueFromBag(bag, cc);
                obj.resizePreview = m_ResizePreview.GetValueFromBag(bag, cc);

                var columnList = m_Columns.GetValueFromBag(bag, cc);
                if (columnList != null)
                {
                    foreach (var column in columnList)
                    {
                        obj.Add(column);
                    }
                }
            }
        }

        IList<Column> m_Columns = new List<Column>();
        List<Column> m_DisplayColumns;
        List<Column> m_VisibleColumns;
        bool m_VisibleColumnsDirty = true;

        StretchMode m_StretchMode = StretchMode.GrowAndFill;
        bool m_Reorderable = true;
        bool m_Resizable = true;
        bool m_ResizePreview;
        string m_PrimaryColumnName;

        internal IList<Column> columns => m_Columns;

        /// <summary>
        /// Indicates the column that needs to be considered as the primary column, by ID.
        /// </summary>
        /// <remarks>
        /// Needs to match a <see cref="Column"/>'s id, otherwise will be ignored.
        /// The primary column cannot be hidden and will contain the expand arrow for tree views.
        /// </remarks>
        public string primaryColumnName
        {
            get => m_PrimaryColumnName;
            set
            {
                if (m_PrimaryColumnName == value)
                    return;

                m_PrimaryColumnName = value;
                NotifyChange(ColumnsDataType.PrimaryColumn);
            }
        }

        /// <summary>
        /// Indicates whether the columns can be reordered interactively by user.
        /// </summary>
        /// <remarks>
        /// Reordering columns can be cancelled by pressing ESC key.
        /// </remarks>
        public bool reorderable
        {
            get => m_Reorderable;
            set
            {
                if (m_Reorderable == value)
                    return;
                m_Reorderable = value;
                NotifyChange(ColumnsDataType.Reorderable);
            }
        }

        /// <summary>
        /// Indicates whether the columns can be resized interactively by user.
        /// </summary>
        /// <remarks>
        /// The resize behaviour of a specific column in the column collection can be specified by setting <see cref="Column.resizable"/>.
        /// A column is effectively resizable if both <see cref="Column.resizable"/> and <see cref="Columns.resizable"/> are both true.
        /// </remarks>
        public bool resizable
        {
            get => m_Resizable;
            set
            {
                if (m_Resizable == value)
                    return;
                m_Resizable = value;
                NotifyChange(ColumnsDataType.Resizable);
            }
        }

        /// <summary>
        /// Indicates whether columns are resized as the user drags resize handles or only upon mouse release.
        /// </summary>
        /// <remarks>
        /// When enabled, resizing can be cancelled by pressing ESC key.
        /// </remarks>
        public bool resizePreview
        {
            get => m_ResizePreview;
            set
            {
                if (m_ResizePreview == value)
                    return;
                m_ResizePreview = value;
                NotifyChange(ColumnsDataType.ResizePreview);
            }
        }

        /// <summary>
        /// Returns the list of (visible and hidden) columns ordered by their display indexes.
        /// </summary>
        internal IEnumerable<Column> displayList
        {
            get
            {
                InitOrderColumns();
                return m_DisplayColumns;
            }
        }

        /// <summary>
        /// Returns the list of visible columns.
        /// </summary>
        internal IEnumerable<Column> visibleList
        {
            get
            {
                UpdateVisibleColumns();
                return m_VisibleColumns;
            }
        }

        /// <summary>
        /// Event sent whenever properties of the column collection change.
        /// </summary>
        internal event Action<ColumnsDataType> changed;

        /// <summary>
        /// Indicates how the size of columns in this collection is automatically adjusted as other columns or the containing view get resized.
        /// The default value is <see cref="StretchMode.GrowAndFill"/>
        /// </summary>
        public StretchMode stretchMode
        {
            get => m_StretchMode;
            set
            {
                if (m_StretchMode == value)
                    return;
                m_StretchMode = value;
                NotifyChange(ColumnsDataType.StretchMode);
            }
        }

        /// <summary>
        /// Event sent whenever a column is added to the collection at the specified index.
        /// </summary>
        internal event Action<Column, int> columnAdded;

        /// <summary>
        /// Event sent whenever a column is removed from the collection.
        /// </summary>
        internal event Action<Column> columnRemoved;

        /// <summary>
        /// Event sent whenever a column is changed, with the related data role that changed.
        /// </summary>
        internal event Action<Column, ColumnDataType> columnChanged;

        /// <summary>
        /// Event sent whenever a column is resized.
        /// </summary>
        internal event Action<Column> columnResized;

        /// <summary>
        /// Event sent whenever a column is moved from a display index to another.
        /// </summary>
        internal event Action<Column, int, int> columnReordered;

        /// <summary>
        /// Checks if the specified column is the primary one.
        /// </summary>
        /// <param name="column">The column to check.</param>
        /// <returns>Whether or not the specified column is the primary one.</returns>
        public bool IsPrimary(Column column)
        {
            return primaryColumnName == column.name || (string.IsNullOrEmpty(primaryColumnName) && column.visibleIndex == 0);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<Column> GetEnumerator()
        {
            return m_Columns.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a column at the end of the collection.
        /// </summary>
        /// <param name="item">The column to add.</param>
        public void Add(Column item)
        {
            Insert(m_Columns.Count, item);
        }

        /// <summary>
        /// Removes all columns from the collection.
        /// </summary>
        public void Clear()
        {
            while (m_Columns.Count > 0)
            {
                Remove(m_Columns[m_Columns.Count - 1]);
            }
        }

        /// <inheritdoc />
        public bool Contains(Column item)
        {
            return m_Columns.Contains(item);
        }

        /// <summary>
        /// Whether the columns contain the specified name.
        /// </summary>
        /// <param name="name">The name of the column to look for.</param>
        /// <returns>Whether a column with the given name exists or not.</returns>
        public bool Contains(string name)
        {
            foreach (var column in m_Columns)
            {
                if (column.name == name)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Copies the elements of the current collection to a Array, starting at the specified index.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The starting index.</param>
        public void CopyTo(Column[] array, int arrayIndex)
        {
            m_Columns.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the first occurence of a column from the collection.
        /// </summary>
        /// <param name="column">The column to remove.</param>
        /// <returns>Whether it was removed or not.</returns>
        public bool Remove(Column column)
        {
            if (column == null)
                throw new ArgumentException("Cannot remove null column");

            if (m_Columns.Remove(column))
            {
                m_DisplayColumns?.Remove(column);
                m_VisibleColumns?.Remove(column);

                column.collection = null;
                column.changed -= OnColumnChanged;
                column.resized -= OnColumnResized;
                columnRemoved?.Invoke(column);
                return true;
            }
            return false;
        }

        void OnColumnChanged(Column column, ColumnDataType type)
        {
            if (type == ColumnDataType.Visibility)
                DirtyVisibleColumns();
            columnChanged?.Invoke(column, type);
        }

        void OnColumnResized(Column column)
        {
            columnResized?.Invoke(column);
        }

        /// <summary>
        /// Gets the number of columns in the collection.
        /// </summary>
        public int Count => m_Columns.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is readonly.
        /// </summary>
        public bool IsReadOnly => m_Columns.IsReadOnly;

        /// <summary>
        /// Returns the index of the specified column if it is contained in the collection; returns -1 otherwise.
        /// </summary>
        /// <param name="column">The column to locate in the <see cref="Columns"/>.</param>
        /// <returns>The index of the column if found in the collection; otherwise, -1.</returns>
        public int IndexOf(Column column)
        {
            return m_Columns.IndexOf(column);
        }

        /// <summary>
        /// Inserts a column into the current instance at the specified index.
        /// </summary>
        /// <param name="index">Index to insert to.</param>
        /// <param name="column">The column to insert.</param>
        public void Insert(int index, Column column)
        {
            if (column == null)
                throw new ArgumentException("Cannot insert null column");

            if (column.collection == this)
                throw new ArgumentException("Already contains this column");

            // Removes from the previous collection
            if (column.collection != null)
                column.collection.Remove(column);

            m_Columns.Insert(index, column);
            if (m_DisplayColumns != null)
            {
                m_DisplayColumns.Insert(index, column);
                DirtyVisibleColumns();
            }
            column.collection = this;
            column.changed += OnColumnChanged;
            column.resized += OnColumnResized;
            columnAdded?.Invoke(column, index);
        }

        /// <summary>
        /// Removes the column at the specified index.
        /// </summary>
        /// <param name="index">The index of the column to remove.</param>
        public void RemoveAt(int index)
        {
            Remove(m_Columns[index]);
        }

        /// <summary>
        /// Returns the column at the specified index.
        /// </summary>
        /// <param name="index">The index of the colum to locate.</param>
        /// <returns>The column at the specified index.</returns>
        public Column this[int index]
        {
            get { return m_Columns[index]; }
        }

        /// <summary>
        /// Returns the column with the specified name.
        /// </summary>
        /// <param name="name">The name of the column to locate.</param>
        /// <returns>The column with the specified name.</returns>
        /// <remarks>
        /// Name must be unique in a column collection. Only the first with matching name will be returned.
        /// </remarks>
        public Column this[string name]
        {
            get
            {
                foreach (var column in m_Columns)
                {
                    if (column.name == name)
                        return column;
                }
                return null;
            }
        }

        /// <summary>
        /// Reorders the display of a column at the specified source index, to the destination index.
        /// </summary>
        /// <remarks>
        /// This does not change the order in the original columns data, only in columns being displayed.</remarks>
        /// <param name="from">The display index of the column to move.</param>
        /// <param name="to">The display index where the column will be moved to.</param>
        public void ReorderDisplay(int from, int to)
        {
            InitOrderColumns();

            var col = m_DisplayColumns[from];

            m_DisplayColumns.RemoveAt(from);
            m_DisplayColumns.Insert(to, col);
            DirtyVisibleColumns();
            columnReordered?.Invoke(col, from, to);
        }

        void InitOrderColumns()
        {
            if (m_DisplayColumns == null)
            {
                m_DisplayColumns = new List<Column>(this);
            }
        }

        void DirtyVisibleColumns()
        {
            m_VisibleColumnsDirty = true;

            if (m_VisibleColumns != null)
                m_VisibleColumns.Clear();
        }

        void UpdateVisibleColumns()
        {
            if (m_VisibleColumnsDirty == false)
                return;

            InitOrderColumns();

            if (m_VisibleColumns == null)
            {
                m_VisibleColumns = new List<Column>(m_Columns.Count);
            }
            m_VisibleColumns.AddRange(m_DisplayColumns.FindAll((c) => c.visible));
            m_VisibleColumnsDirty = false;
        }

        void NotifyChange(ColumnsDataType type)
        {
            changed?.Invoke(type);
        }
    }
}
