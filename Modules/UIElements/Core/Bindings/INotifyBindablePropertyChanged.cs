// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides information about the property that has changed.
    /// </summary>
    public readonly struct BindablePropertyChangedEventArgs
    {
        readonly BindingId m_PropertyName;
        readonly object m_ChangedItem;

        /// <summary>
        /// Instantiates a new <see cref="BindablePropertyChangedEventArgs"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        public BindablePropertyChangedEventArgs(in BindingId propertyName)
        {
            m_PropertyName = propertyName;
            m_ChangedItem = null;
        }

        BindablePropertyChangedEventArgs(in BindingId propertyName, object changedItem)
        {
            m_PropertyName = propertyName;
            m_ChangedItem = changedItem;
        }

        internal static BindablePropertyChangedEventArgs ForChangedItem(object changedItem)
            => new BindablePropertyChangedEventArgs(default(Unity.Properties.PropertyPath), changedItem);

        /// <summary>
        /// Returns the name of the property that has changed.
        /// </summary>
        public BindingId propertyName => m_PropertyName;

        // The item of a collection whose content changed, when the collection reports the change of an item without its index.
        internal object changedItem => m_ChangedItem;
    }

    /// <summary>
    /// Defines a component that notifies when a property has changed.
    /// </summary>
    public interface INotifyBindablePropertyChanged
    {
        /// <summary>
        /// Called when a property has changed.
        /// </summary>
        /// <remarks>
        /// The property name can be a property path. For example, a collection can report a change inside one of its items with a
        /// path that starts with the index of the item, such as `[3].name`. The binding system then updates only the bindings
        /// whose data source path starts with that path.
        /// </remarks>
        event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
    }

    /// <summary>
    /// Information about a change to a collection that implements <see cref="INotifyBindableCollectionChanged"/>.
    /// </summary>
    /// <remarks>
    /// A collection passes this information to the listeners of <see cref="INotifyBindableCollectionChanged.collectionChanged"/>
    /// after each change. <see cref="changeType"/> describes the type of change, and <see cref="index"/> and <see cref="count"/>
    /// describe the range of items that the change affects.
    ///
    /// The meaning of <see cref="index"/> and <see cref="count"/> depends on the type of change:
    ///
    ///- <see cref="ChangeType.Added"/> and <see cref="ChangeType.Inserted"/>: the index of the first new item, and the number of new items.
    ///- <see cref="ChangeType.Removed"/>: the index that the first removed item had before the removal, and the number of removed items.
    ///- <see cref="ChangeType.Replaced"/>: the index of the first replaced item, and the number of replaced items.
    ///- <see cref="ChangeType.Cleared"/>: both values are -1.
    ///
    /// To create the information for a change with values that follow these rules, use the static methods, such as
    /// <see cref="Added(int)"/> or <see cref="Removed(int, int)"/>.
    /// </remarks>
    /// <seealso cref="INotifyBindableCollectionChanged"/>
    /// <seealso cref="UnityEngine.UIElements.Collections.ObservableList{T}"/>
    internal readonly struct BindableCollectionChangedEventArgs
    {
        /// <summary>
        /// Creates the information for one item added at the end of a collection.
        /// </summary>
        /// <param name="index">Index of the new item.</param>
        /// <returns>The information about the change, with a <see cref="count"/> of 1.</returns>
        public static BindableCollectionChangedEventArgs Added(int index)
            => new(ChangeType.Added, index);

        /// <summary>
        /// Creates the information for consecutive items added at the end of a collection.
        /// </summary>
        /// <param name="index">Index of the first new item.</param>
        /// <param name="count">Number of new items.</param>
        /// <returns>The information about the change.</returns>
        public static BindableCollectionChangedEventArgs Added(int index, int count)
            => new(ChangeType.Added, index, count);

        /// <summary>
        /// Creates the information for one item inserted in a collection.
        /// </summary>
        /// <param name="index">Index of the new item. The items that were at this index or after it move to the next index.</param>
        /// <returns>The information about the change, with a <see cref="count"/> of 1.</returns>
        public static BindableCollectionChangedEventArgs Inserted(int index)
            => new(ChangeType.Inserted, index);

        /// <summary>
        /// Creates the information for consecutive items inserted in a collection.
        /// </summary>
        /// <param name="index">Index of the first new item. The items that were at this index or after it move by <paramref name="count"/>.</param>
        /// <param name="count">Number of new items.</param>
        /// <returns>The information about the change.</returns>
        public static BindableCollectionChangedEventArgs Inserted(int index, int count)
            => new(ChangeType.Inserted, index, count);

        /// <summary>
        /// Creates the information for one item of a collection replaced by another item.
        /// </summary>
        /// <param name="index">Index of the replaced item.</param>
        /// <returns>The information about the change, with a <see cref="count"/> of 1.</returns>
        public static BindableCollectionChangedEventArgs Replaced(int index)
            => new(ChangeType.Replaced, index);

        /// <summary>
        /// Creates the information for consecutive items of a collection replaced by other items.
        /// </summary>
        /// <param name="index">Index of the first replaced item.</param>
        /// <param name="count">Number of replaced items.</param>
        /// <returns>The information about the change.</returns>
        public static BindableCollectionChangedEventArgs Replaced(int index, int count)
            => new(ChangeType.Replaced, index, count);

        /// <summary>
        /// Creates the information for one item removed from a collection.
        /// </summary>
        /// <param name="index">Index of the item before the removal. The items after it move to the previous index.</param>
        /// <returns>The information about the change, with a <see cref="count"/> of 1.</returns>
        public static BindableCollectionChangedEventArgs Removed(int index)
            => new(ChangeType.Removed, index);

        /// <summary>
        /// Creates the information for consecutive items removed from a collection.
        /// </summary>
        /// <param name="index">Index of the first removed item before the removal. The items after the removed range move by <paramref name="count"/>.</param>
        /// <param name="count">Number of removed items.</param>
        /// <returns>The information about the change.</returns>
        public static BindableCollectionChangedEventArgs Removed(int index, int count)
            => new(ChangeType.Removed, index, count);

        /// <summary>
        /// Creates the information for the removal of all the items of a collection.
        /// </summary>
        /// <returns>The information about the change, with an <see cref="index"/> and a <see cref="count"/> of -1.</returns>
        public static BindableCollectionChangedEventArgs Cleared()
            => new(ChangeType.Cleared, -1, -1);

        /// <summary>
        /// Types of change that a collection reports through <see cref="INotifyBindableCollectionChanged.collectionChanged"/>.
        /// </summary>
        /// <remarks>
        /// The data binding system handles <see cref="Added"/>, <see cref="Inserted"/>, and <see cref="Removed"/> as
        /// changes to the items at the index of the change and after it, because these items move, and to the other
        /// properties of the collection, such as `Count`. It handles <see cref="Cleared"/> as a change to all the items
        /// and properties of the collection. It handles <see cref="Replaced"/> as a change to the replaced items only,
        /// which costs the least to process.
        ///
        /// For the index and count that each type of change reports, refer to <see cref="BindableCollectionChangedEventArgs"/>.
        /// </remarks>
        public enum ChangeType
        {
            /// <summary>
            /// Items are added at the end of the collection.
            /// </summary>
            Added,
            /// <summary>
            /// Items are removed from the collection. The items after them move to lower indices.
            /// </summary>
            Removed,
            /// <summary>
            /// Items are inserted in the collection. The items at the insertion index and after it move to higher indices.
            /// </summary>
            Inserted,
            /// <summary>
            /// Items are replaced by other items. The number of items and the indices of the other items stay the same.
            /// </summary>
            Replaced,
            /// <summary>
            /// All the items are removed from the collection.
            /// </summary>
            Cleared,
        }

        private readonly ChangeType m_ChangeType;
        private readonly int m_Index;
        private readonly int m_Count;

        /// <summary>
        /// The type of change made to the collection.
        /// </summary>
        public ChangeType changeType => m_ChangeType;

        /// <summary>
        /// The index of the first item that the change affects.
        /// </summary>
        /// <remarks>
        /// For <see cref="ChangeType.Removed"/>, this is the index that the first removed item had before the removal.
        /// For <see cref="ChangeType.Cleared"/>, this value is -1.
        /// </remarks>
        public int index => m_Index;

        /// <summary>
        /// The number of consecutive items that the change affects.
        /// </summary>
        /// <remarks>
        /// For <see cref="ChangeType.Cleared"/>, this value is -1.
        /// </remarks>
        public int count => m_Count;

        /// <summary>
        /// Initializes the information for a change to one item of a collection.
        /// </summary>
        /// <remarks>
        /// To report that a collection is cleared, use <see cref="Cleared"/>, which sets <see cref="index"/> and <see cref="count"/> to -1.
        /// </remarks>
        /// <param name="changeType">Type of change made to the collection.</param>
        /// <param name="index">Index of the item that the change affects.</param>
        public BindableCollectionChangedEventArgs(ChangeType changeType, int index)
            : this(changeType, index, 1)
        {
        }

        /// <summary>
        /// Initializes the information for a change to consecutive items of a collection.
        /// </summary>
        /// <remarks>
        /// To report that a collection is cleared, use <see cref="Cleared"/>, which sets <see cref="index"/> and <see cref="count"/> to -1.
        /// </remarks>
        /// <param name="changeType">Type of change made to the collection.</param>
        /// <param name="index">Index of the first item that the change affects.</param>
        /// <param name="count">Number of items that the change affects.</param>
        public BindableCollectionChangedEventArgs(ChangeType changeType, int index, int count)
        {
            m_ChangeType = changeType;
            m_Index = index;
            m_Count = count;
        }
    }

    /// <summary>
    /// Defines a collection that notifies its listeners when items are added, inserted, removed, or replaced, or when the
    /// collection is cleared.
    /// </summary>
    /// <remarks>
    /// Implement this interface on a collection so that UI Toolkit reacts to its changes, instead of checking it for changes
    /// on every update. <see cref="UnityEngine.UIElements.Collections.ObservableList{T}"/> implements this interface.
    ///
    /// This interface only reports changes to the collection itself. To also report changes inside its items, implement
    /// <see cref="INotifyBindablePropertyChanged"/> and raise <see cref="INotifyBindablePropertyChanged.propertyChanged"/> with a
    /// property path that starts with the index of the item, such as `[3].name`.
    ///
    /// When you use the collection as a data source, the data binding system updates only the bindings that a change affects.
    /// For example, after a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/> change at index 3, it updates
    /// the bindings that use the data source path `[3]`, but not the bindings that use `[2]`. If the collection only implements
    /// <see cref="INotifyBindableCollectionChanged"/>, this applies to the bindings to the collection itself and to the paths that
    /// stop at an item, such as `[3]`. The bindings with paths inside an item, such as `[3].name`, or with other properties of
    /// the collection, still update on every frame, because the collection doesn't report changes to them. Implement both
    /// interfaces to apply it to all paths.
    ///
    /// To bind to the items of a custom collection by index, the collection type also needs a property bag that
    /// supports indices. To register one, call <see cref="Unity.Properties.PropertyBag.RegisterIList{TList, TElement}"/>.
    /// </remarks>
    /// <seealso cref="BindableCollectionChangedEventArgs"/>
    /// <seealso cref="INotifyBindablePropertyChanged"/>
    /// <seealso cref="UnityEngine.UIElements.Collections.ObservableList{T}"/>
    internal interface INotifyBindableCollectionChanged
    {
        /// <summary>
        /// Raised after the collection changes.
        /// </summary>
        /// <remarks>
        /// Raise this event once for each change, after you apply the change, so that the listeners can read the new content
        /// of the collection. Pass the collection itself as the sender.
        ///
        /// To report a change to many consecutive items, raise a single event with the index of the first item and the number
        /// of items, instead of one event for each item.
        /// </remarks>
        event EventHandler<BindableCollectionChangedEventArgs> collectionChanged;
    }
}
