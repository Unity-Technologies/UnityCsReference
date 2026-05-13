// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;

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
    [Serializable, UxmlObject]
    public partial class SortColumnDescription : INotifyBindablePropertyChanged
    {
        static readonly BindingId columnNameProperty = nameof(columnName);
        static readonly BindingId columnIndexProperty = nameof(columnIndex);
        static readonly BindingId directionProperty = nameof(direction);

        [SerializeField]
        int m_ColumnIndex = -1;

        [SerializeField]
        string m_ColumnName;

        [SerializeField]
        SortDirection m_SortDirection;

        /// <summary>
        /// Called when a property has changed.
        /// </summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        /// <summary>
        /// The name of the column.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public string columnName
        {
            get => m_ColumnName;
            set
            {
                if (m_ColumnName == value)
                    return;
                m_ColumnName = value;
                changed?.Invoke(this);
                NotifyPropertyChanged(columnNameProperty);
            }
        }

        /// <summary>
        /// The index of the column to be used to find the column only if the <see cref="SortColumnDescription.columnName">columnName</see> isn't set.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public int columnIndex
        {
            get => m_ColumnIndex;
            set
            {
                if (m_ColumnIndex == value)
                    return;
                m_ColumnIndex = value;
                changed?.Invoke(this);
                NotifyPropertyChanged(columnIndexProperty);
            }
        }

        /// <summary>
        /// The sorted column.
        /// </summary>
        public Column column { get; internal set; }

        /// <summary>
        /// The sort direction.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public SortDirection direction
        {
            get => m_SortDirection;
            set
            {
                if (m_SortDirection == value)
                    return;
                m_SortDirection = value;
                changed?.Invoke(this);
                NotifyPropertyChanged(directionProperty);
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

        void NotifyPropertyChanged(in BindingId property)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
