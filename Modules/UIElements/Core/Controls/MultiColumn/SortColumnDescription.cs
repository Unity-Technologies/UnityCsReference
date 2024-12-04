// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Internal;

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
    public class SortColumnDescription : INotifyBindablePropertyChanged
    {
        static readonly BindingId columnNameProperty = nameof(columnName);
        static readonly BindingId columnIndexProperty = nameof(columnIndex);
        static readonly BindingId directionProperty = nameof(direction);

        [ExcludeFromDocs, Serializable]
        public class UxmlSerializedData : UIElements.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(columnName), "column-name"),
                    new (nameof(columnIndex), "column-index"),
                    new (nameof(direction), "direction"),
                });
            }

            #pragma warning disable 649
            [SerializeField] string columnName;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags columnName_UxmlAttributeFlags;
            [SerializeField] int columnIndex;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags columnIndex_UxmlAttributeFlags;
            [SerializeField] SortDirection direction;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags direction_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new SortColumnDescription();

            public override void Deserialize(object obj)
            {
                var e = (SortColumnDescription)obj;
                if (ShouldWriteAttributeValue(columnName_UxmlAttributeFlags))
                    e.columnName = columnName;
                if (ShouldWriteAttributeValue(columnIndex_UxmlAttributeFlags))
                    e.columnIndex = columnIndex;
                if (ShouldWriteAttributeValue(direction_UxmlAttributeFlags))
                    e.direction = direction;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="SortColumnDescription"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        internal class UxmlObjectFactory<T> : UxmlObjectFactory<T, UxmlObjectTraits<T>> where T : SortColumnDescription, new() {}
        /// <summary>
        /// Instantiates a <see cref="SortColumnDescription"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlObjectFactory<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        internal class UxmlObjectFactory : UxmlObjectFactory<SortColumnDescription> {}

        /// <summary>
        /// Defines <see cref="UxmlObjectTraits{T}"/> for the <see cref="SortColumnDescription"/>.
        /// </summary>
        [Obsolete("UxmlObjectTraits<T> is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
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
        /// Called when a property has changed.
        /// </summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        /// <summary>
        /// The name of the column.
        /// </summary>
        [CreateProperty]
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
