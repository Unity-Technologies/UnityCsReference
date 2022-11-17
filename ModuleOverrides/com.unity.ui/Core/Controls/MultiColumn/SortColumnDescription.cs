// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The sort direction.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// The ascending order.
        /// </summary>
        Ascending,
        /// <summary>
        /// The descending order.
        /// </summary>
        Descending
    }

    /// <summary>
    /// This represents a description on what column to sort and in which order.
    /// </summary>
    [Serializable]
    public class SortColumnDescription
    {
        /// <summary>
        /// Instantiates a <see cref="SortColumnDescription"/> using the data read from a UXML file.
        /// </summary>
        internal class UxmlObjectFactory<T> : UxmlObjectFactory<T, UxmlObjectTraits<T>> where T : SortColumnDescription, new() {}
        /// <summary>
        /// Instantiates a <see cref="SortColumnDescription"/> using the data read from a UXML file.
        /// </summary>
        internal class UxmlObjectFactory : UxmlObjectFactory<SortColumnDescription> {}

        /// <summary>
        /// Defines <see cref="UxmlObjectTraits{T}"/> for the <see cref="SortColumnDescription"/>.
        /// </summary>
        internal class UxmlObjectTraits<T> : UnityEngine.UIElements.UxmlObjectTraits<T> where T : SortColumnDescription
        {
            readonly UxmlStringAttributeDescription m_ColumnName = new UxmlStringAttributeDescription { name = "column-name" };
            readonly UxmlIntAttributeDescription m_ColumnIndex = new UxmlIntAttributeDescription { name = "column-index", defaultValue=-1};
            readonly UxmlEnumAttributeDescription<SortDirection> m_SortDescription = new UxmlEnumAttributeDescription<SortDirection> { name = "direction", defaultValue = SortDirection.Ascending };

            public override void Init(ref T obj, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ref obj, bag, cc);

                obj.columnName = m_ColumnName.GetValueFromBag(bag, cc);
                obj.columnIndex = m_ColumnIndex.GetValueFromBag(bag, cc);
                obj.direction = m_SortDescription.GetValueFromBag(bag, cc);
            }
        }

        [SerializeField]
        int m_ColumnIndex = -1;

        [SerializeField]
        string m_ColumnName;

        [SerializeField]
        SortDirection m_SortDirection;

        /// <summary>
        /// The name of the column.
        /// </summary>
        public string columnName
        {
            get => m_ColumnName;
            set
            {
                if (m_ColumnName == value)
                    return;
                m_ColumnName = value;
                changed?.Invoke(this);
            }
        }

        /// <summary>
        /// The index of the column.
        /// </summary>
        public int columnIndex
        {
            get => m_ColumnIndex;
            set
            {
                if (m_ColumnIndex == value)
                    return;
                m_ColumnIndex = value;
                changed?.Invoke(this);
            }
        }

        /// <summary>
        /// The sorted column.
        /// </summary>
        public Column column { get; internal set; }

        /// <summary>
        /// The sort direction.
        /// </summary>
        public SortDirection direction
        {
            get => m_SortDirection;
            set
            {
                if (m_SortDirection == value)
                    return;
                m_SortDirection = value;
                changed?.Invoke(this);
            }
        }

        /// <summary>
        /// Emitted when the description has changed.
        /// </summary>
        internal event Action<SortColumnDescription> changed;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SortColumnDescription()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="columnIndex">The column index for this sort description.</param>
        /// <param name="direction">The sort description.</param>
        public SortColumnDescription(int columnIndex, SortDirection direction)
        {
            this.columnIndex = columnIndex;
            this.direction = direction;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="columnName">The column name for this sort description.</param>
        /// <param name="direction">The sort description.</param>
        public SortColumnDescription(string columnName, SortDirection direction)
        {
            this.columnName = columnName;
            this.direction = direction;
        }
    }
}
