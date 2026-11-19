// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Unity.Properties;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements.Collections;

/// <summary>
/// Represents a list that notifies UI Toolkit when its items are added, removed, or replaced, and when the content of
/// its items changes.
/// </summary>
/// <remarks>
/// Use an <see cref="ObservableList{T}"/> instead of a <see cref="List{T}"/> to keep the UI that displays a list up to date
/// without extra code, and to reduce the work that UI Toolkit does on every update.
///
/// When you use the list as a data source, the data binding system updates only the bindings that a change affects.
/// For example, when you replace the item at index 3, it updates the bindings that use the data source path `[3]`, but not
/// the bindings that use `[2]`.
///
/// The list reports two kinds of changes:
///
///- <see cref="collectionChanged"/>: items are added, inserted, removed, or replaced, or the list is cleared.
///- <see cref="propertyChanged"/>: the content of an item changes. The list reports this change when the item implements <see cref="INotifyBindablePropertyChanged"/> or <see cref="INotifyBindableCollectionChanged"/> and raises its own event. The property path of the change is empty, because the list doesn't track the index of its items. The data binding system still updates only the bindings of the item that changed.
///
/// The list only listens to the events of its items while <see cref="propertyChanged"/> has listeners. The data
/// binding system adds a listener while the list is the data source of a binding. If the same item appears several
/// times in the list, the list reports its change once for each time the item appears.
///
/// The list can't detect changes to items that don't raise events, such as structs, or classes that don't implement
/// <see cref="INotifyBindablePropertyChanged"/>. To report a change to such an item, assign the item to its index again,
/// for example `list[3] = list[3]`.
///
/// Each change raises an event, so modifying an <see cref="ObservableList{T}"/> that has listeners costs more than
/// modifying a <see cref="List{T}"/>. To add or remove many items at once, use <see cref="AddRange"/>,
/// <see cref="InsertRange"/>, or <see cref="RemoveRange"/>, which raise a single event for all the items. To reorder
/// the items, use the `Sort` and `Reverse` methods, which also raise a single event.
///
/// Unity doesn't serialize <see cref="ObservableList{T}"/>. To save the items in a <see cref="MonoBehaviour"/> or a
/// <see cref="ScriptableObject"/>, serialize them in a <see cref="List{T}"/> or an array, and fill the
/// <see cref="ObservableList{T}"/> from it at runtime.
///
/// Only modify the list from the main thread, because the data binding system handles its events immediately.
/// </remarks>
/// <typeparam name="T">Type of the items in the list.</typeparam>
/// <seealso cref="INotifyBindableCollectionChanged"/>
/// <seealso cref="INotifyBindablePropertyChanged"/>
internal sealed class ObservableList<T> :
    IList<T>,
    IList,
    INotifyBindablePropertyChanged,
    INotifyBindableCollectionChanged
{
    // A boxed value type is a copy, so it can never report changes made to the stored item.
    [NoAutoStaticsCleanup]
    static readonly bool k_CanContainObservableItems = CanContainObservableItems();

    readonly List<T> m_Items;

    // Changes made by a listener while the list notifies another change. They are notified afterwards, so that every
    // listener receives the changes in the order they were made.
    struct PendingNotification
    {
        public bool isCollectionChange;
        public BindableCollectionChangedEventArgs collectionArgs;
        public BindablePropertyChangedEventArgs propertyArgs;
    }

    Queue<PendingNotification> m_PendingNotifications;
    bool m_IsNotifying;

    // Lists that contain each other relay the change of an item back to the list that is notifying it, possibly through
    // several paths, which would never end. While the list notifies, it notifies the change of each item only once, since
    // the listeners only need to know that the item changed. A list is enough because few items change during a notification.
    object m_FirstChangedItem;
    List<object> m_PendingChangedItems;

    EventHandler<BindablePropertyChangedEventArgs> m_PropertyChanged;
    EventHandler<BindableCollectionChangedEventArgs> m_CollectionChanged;

    // Invocation lists of the events, so that each listener can be called separately without allocating on every change.
    Delegate[] m_PropertyChangedListeners;
    Delegate[] m_CollectionChangedListeners;

    EventHandler<BindablePropertyChangedEventArgs> m_ItemPropertyChangedHandler;
    EventHandler<BindableCollectionChangedEventArgs> m_ItemCollectionChangedHandler;

    // True while propertyChanged has listeners. Each occurrence of an item in the list subscribes to its events once.
    bool m_IsObservingItems;

    // The reflected property bag provider only creates indexed bags for the built-in collection types, and it can create
    // a bag for a list type before any list exists. The property bags are cleared on code reload, but a static constructor
    // doesn't run again for the types that outlive the reload, so new lists and new listeners check the bag.
    static void EnsurePropertyBag()
    {
        if (!PropertyBag.Exists<ObservableList<T>>() ||
            PropertyBag.GetPropertyBag<ObservableList<T>>() is not IndexedCollectionPropertyBag<ObservableList<T>, T>)
            PropertyBag.Register(new IndexedCollectionPropertyBag<ObservableList<T>, T>());
    }

    /// <summary>
    /// Initializes a new empty list.
    /// </summary>
    public ObservableList()
    {
        m_Items = new List<T>();
        #pragma warning disable UAL0015 // the bag is registered again by the next list or listener after the CodeLoaded scope exits
        EnsurePropertyBag();
        #pragma warning restore UAL0015
    }

    /// <summary>
    /// Initializes a new empty list that can hold the specified number of items before it needs to grow.
    /// </summary>
    /// <param name="capacity">Number of items that the list can hold before it needs to grow.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than 0.</exception>
    public ObservableList(int capacity)
    {
        m_Items = new List<T>(capacity);
        #pragma warning disable UAL0015 // the bag is registered again by the next list or listener after the CodeLoaded scope exits
        EnsurePropertyBag();
        #pragma warning restore UAL0015
    }

    /// <summary>
    /// Initializes a new list that contains the items of a collection.
    /// </summary>
    /// <param name="collection">Items to add to the list, in order.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
    public ObservableList(IEnumerable<T> collection)
    {
        m_Items = new List<T>(collection);
        #pragma warning disable UAL0015 // the bag is registered again by the next list or listener after the CodeLoaded scope exits
        EnsurePropertyBag();
        #pragma warning restore UAL0015
    }

    static bool CanContainObservableItems()
    {
        var type = typeof(T);
        if (type.IsValueType)
            return false;
        if (!type.IsSealed)
            return true;
        return typeof(INotifyBindablePropertyChanged).IsAssignableFrom(type) ||
            typeof(INotifyBindableCollectionChanged).IsAssignableFrom(type);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the items of the list.
    /// </summary>
    /// <remarks>
    /// The enumerator is a struct, so a `foreach` loop over the list doesn't allocate memory.
    /// If you modify the list during the enumeration, the enumerator throws an <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <returns>An enumerator for the items of the list.</returns>
    public List<T>.Enumerator GetEnumerator()
    {
        return m_Items.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return m_Items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return m_Items.GetEnumerator();
    }

    /// <summary>
    /// Adds an item at the end of the list.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Added"/> change.
    /// </remarks>
    /// <param name="item">Item to add.</param>
    public void Add(T item)
    {
        var index = m_Items.Count;
        m_Items.Add(item);
        ObserveItem(item);
        NotifyAdded(index);
    }

    /// <summary>
    /// Adds the items of a collection at the end of the list.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> once, with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Added"/>
    /// change for all the new items. To add many items, this method costs less than calling <see cref="Add"/> for each item.
    /// If <paramref name="collection"/> is empty, this method doesn't raise any event.
    /// If enumerating <paramref name="collection"/> throws an exception, the items added before the exception stay in the
    /// list, like with <see cref="List{T}.AddRange"/>, and the event reports them.
    /// If a listener of that event also throws an exception, the list logs it and throws the exception of the enumeration.
    /// </remarks>
    /// <param name="collection">Items to add, in order.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
    public void AddRange(IEnumerable<T> collection)
    {
        var index = m_Items.Count;
        try
        {
            m_Items.AddRange(collection);
        }
        catch
        {
            // Like List<T>, the items added before the enumeration throws stay in the list, so they are reported. A
            // listener that throws doesn't replace the exception of the enumeration.
            try
            {
                ReportAddedItems(index);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            throw;
        }

        ReportAddedItems(index);
    }

    void ReportAddedItems(int index)
    {
        var addedCount = m_Items.Count - index;
        if (addedCount == 0)
            return;

        ObserveItems(index, addedCount);
        NotifyAdded(index, addedCount);
    }

    int IList.Add(object value)
    {
        Add(CastItem(value));
        return m_Items.Count - 1;
    }

    /// <summary>
    /// Removes all the items from the list.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Cleared"/> change.
    /// If the list is already empty, this method doesn't raise any event.
    /// </remarks>
    public void Clear()
    {
        if (m_Items.Count == 0)
            return;

        UnobserveAllItems();
        m_Items.Clear();
        NotifyCleared();
    }

    bool IList.Contains(object value)
    {
        return ((IList)m_Items).Contains(value);
    }

    int IList.IndexOf(object value)
    {
        return ((IList)m_Items).IndexOf(value);
    }

    /// <summary>
    /// Inserts the items of a collection at the specified index.
    /// </summary>
    /// <remarks>
    /// The items at <paramref name="index"/> and after it move to higher indices.
    /// Raises <see cref="collectionChanged"/> once, with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Inserted"/>
    /// change for all the new items. If <paramref name="collection"/> is empty, this method doesn't raise any event.
    /// If enumerating <paramref name="collection"/> throws an exception, the items inserted before the exception stay in the
    /// list, like with <see cref="List{T}.InsertRange"/>, and the event reports them.
    /// If a listener of that event also throws an exception, the list logs it and throws the exception of the enumeration.
    /// </remarks>
    /// <param name="index">Index of the first new item.</param>
    /// <param name="collection">Items to insert, in order.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than 0 or greater than <see cref="Count"/>.</exception>
    public void InsertRange(int index, IEnumerable<T> collection)
    {
        var previousCount = Count;
        try
        {
            m_Items.InsertRange(index, collection);
        }
        catch
        {
            // Like List<T>, the items inserted before the enumeration throws stay in the list, so they are reported. A
            // listener that throws doesn't replace the exception of the enumeration.
            try
            {
                ReportInsertedItems(index, previousCount);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            throw;
        }

        ReportInsertedItems(index, previousCount);
    }

    void ReportInsertedItems(int index, int previousCount)
    {
        var insertedCount = m_Items.Count - previousCount;
        if (insertedCount == 0)
            return;

        ObserveItems(index, insertedCount);
        NotifyInserted(index, insertedCount);
    }

    void IList.Insert(int index, object value)
    {
        Insert(index, CastItem(value));
    }

    void IList.Remove(object value)
    {
        if (value is T || (value == null && default(T) == null))
            Remove((T)value);
    }

    // Matches the exceptions of the non-generic IList members of List<T>.
    static T CastItem(object value)
    {
        if (value is T item)
            return item;
        if (value == null && default(T) == null)
            return default;
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        throw new ArgumentException($"The value \"{value}\" is not of type \"{typeof(T)}\" and cannot be used in this generic collection.", nameof(value));
    }

    /// <summary>
    /// Determines whether the list contains an item.
    /// </summary>
    /// <param name="item">Item to find.</param>
    /// <returns>True if the list contains the item, false otherwise.</returns>
    public bool Contains(T item)
    {
        return m_Items.Contains(item);
    }

    /// <summary>
    /// Copies all the items of the list to an array.
    /// </summary>
    /// <param name="array">Array that receives the items.</param>
    /// <param name="arrayIndex">Index in <paramref name="array"/> where the copy starts.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayIndex"/> is less than 0.</exception>
    /// <exception cref="ArgumentException">Thrown when the items don't fit in <paramref name="array"/> from <paramref name="arrayIndex"/>.</exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        m_Items.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Removes the first occurrence of an item from the list.
    /// </summary>
    /// <remarks>
    /// The items after the removed item move to lower indices.
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Removed"/> change
    /// if the list contains the item.
    /// </remarks>
    /// <param name="item">Item to remove.</param>
    /// <returns>True if the item was removed, false if the list doesn't contain it.</returns>
    public bool Remove(T item)
    {
        var index = m_Items.IndexOf(item);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes consecutive items from the list.
    /// </summary>
    /// <remarks>
    /// The items after the removed range move to lower indices.
    /// Raises <see cref="collectionChanged"/> once, with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Removed"/>
    /// change for all the removed items. If <paramref name="count"/> is 0, this method doesn't raise any event.
    /// </remarks>
    /// <param name="index">Index of the first item to remove.</param>
    /// <param name="count">Number of items to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> or <paramref name="count"/> is less than 0, or when the range extends past the end of the list.</exception>
    public void RemoveRange(int index, int count)
    {
        if (index < 0 || index > m_Items.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (count < 0 || m_Items.Count - index < count)
            throw new ArgumentOutOfRangeException(nameof(count));
        if (count == 0)
            return;

        UnobserveItems(index, count);
        m_Items.RemoveRange(index, count);
        NotifyRemoved(index, count);
    }

    /// <summary>
    /// Removes all the items that match a condition.
    /// </summary>
    /// <remarks>
    /// The remaining items keep their order and move to lower indices.
    /// If the removed items were consecutive, raises <see cref="collectionChanged"/> once, with a
    /// <see cref="BindableCollectionChangedEventArgs.ChangeType.Removed"/> change. Otherwise, raises a
    /// <see cref="BindableCollectionChangedEventArgs.ChangeType.Removed"/> change for the indices past the new end of the
    /// list, then a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/> change for the indices whose item
    /// moved. If no item matches, this method doesn't raise any event.
    /// If <paramref name="match"/> throws an exception, the items removed before the exception stay removed, and the
    /// events report them.
    /// If a listener of these events also throws an exception, the list logs it and throws the exception of <paramref name="match"/>.
    /// If a listener throws an exception, the list still raises both events, then throws the exception.
    /// </remarks>
    /// <param name="match">Condition that the items to remove match.</param>
    /// <returns>The number of removed items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="match"/> is null.</exception>
    public int RemoveAll(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));

        var count = m_Items.Count;
        var firstRemovedIndex = 0;
        while (firstRemovedIndex < count && !match(m_Items[firstRemovedIndex]))
        {
            ++firstRemovedIndex;
        }

        if (firstRemovedIndex == count)
            return 0;

        UnobserveItem(m_Items[firstRemovedIndex]);
        var lastRemovedIndex = firstRemovedIndex;
        var keptCount = firstRemovedIndex;
        var readIndex = firstRemovedIndex + 1;
        try
        {
            for (; readIndex < count; ++readIndex)
            {
                var item = m_Items[readIndex];
                if (match(item))
                {
                    UnobserveItem(item);
                    lastRemovedIndex = readIndex;
                }
                else
                {
                    m_Items[keptCount++] = item;
                }
            }
        }
        catch
        {
            // The items removed before the condition throws stay removed, and a listener that throws doesn't replace
            // the exception of the condition.
            try
            {
                CompleteRemoveAll(count, readIndex, keptCount, firstRemovedIndex, lastRemovedIndex);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            throw;
        }

        return CompleteRemoveAll(count, readIndex, keptCount, firstRemovedIndex, lastRemovedIndex);
    }

    int CompleteRemoveAll(int count, int readIndex, int keptCount, int firstRemovedIndex, int lastRemovedIndex)
    {
        // Keeps the items that the condition didn't reach when it throws.
        for (; readIndex < count; ++readIndex)
        {
            m_Items[keptCount++] = m_Items[readIndex];
        }

        var removedCount = count - keptCount;
        m_Items.RemoveRange(keptCount, removedCount);

        if (lastRemovedIndex - firstRemovedIndex + 1 == removedCount)
            NotifyRemoved(firstRemovedIndex, removedCount);
        else
        {
            // Removing the end first keeps every event consistent with the number of items in the list. A listener that
            // throws doesn't keep the other listeners from receiving the moved items.
            ExceptionDispatchInfo exception = null;
            NotifyCollectionChanged(BindableCollectionChangedEventArgs.Removed(keptCount, removedCount), ref exception);
            NotifyCollectionChanged(BindableCollectionChangedEventArgs.Replaced(firstRemovedIndex, keptCount - firstRemovedIndex), ref exception);
            exception?.Throw();
        }

        return removedCount;
    }

    /// <summary>
    /// Reverses the order of the items in the list.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/>
    /// change for all the items. If the list has fewer than two items, this method doesn't raise any event.
    /// </remarks>
    public void Reverse()
    {
        Reverse(0, m_Items.Count);
    }

    /// <summary>
    /// Reverses the order of consecutive items in the list.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/>
    /// change for the reversed items. If <paramref name="count"/> is less than 2, this method doesn't raise any event.
    /// </remarks>
    /// <param name="index">Index of the first item to reverse.</param>
    /// <param name="count">Number of items to reverse.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
    /// <exception cref="ArgumentException">Thrown when the range extends past the end of the list.</exception>
    public void Reverse(int index, int count)
    {
        m_Items.Reverse(index, count);
        if (count > 1)
            NotifyReplaced(index, count);
    }

    /// <summary>
    /// Sorts the items of the list with the default comparer of <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/>
    /// change for all the items. If the list has fewer than two items, this method doesn't raise any event.
    /// If the comparer throws an exception, the items can stay partly sorted, like with <see cref="List{T}.Sort()"/>, and
    /// the event reports them. If a listener of that event also throws an exception, the list logs it and throws the
    /// exception of the comparer.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the default comparer can't compare the items.</exception>
    public void Sort()
    {
        Sort(0, m_Items.Count, null);
    }

    /// <summary>
    /// Sorts the items of the list with a comparer.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/>
    /// change for all the items. If the list has fewer than two items, this method doesn't raise any event.
    /// If the comparer throws an exception, the items can stay partly sorted, like with <see cref="List{T}.Sort()"/>, and
    /// the event reports them. If a listener of that event also throws an exception, the list logs it and throws the
    /// exception of the comparer.
    /// </remarks>
    /// <param name="comparer">Comparer of the items, or null to use the default comparer of <typeparamref name="T"/>.</param>
    /// <exception cref="InvalidOperationException">Thrown when the comparer can't compare the items.</exception>
    public void Sort(IComparer<T> comparer)
    {
        Sort(0, m_Items.Count, comparer);
    }

    /// <summary>
    /// Sorts consecutive items of the list with a comparer.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/>
    /// change for the sorted items. If <paramref name="count"/> is less than 2, this method doesn't raise any event.
    /// If the comparer throws an exception, the items can stay partly sorted, like with <see cref="List{T}.Sort()"/>, and
    /// the event reports them. If a listener of that event also throws an exception, the list logs it and throws the
    /// exception of the comparer.
    /// </remarks>
    /// <param name="index">Index of the first item to sort.</param>
    /// <param name="count">Number of items to sort.</param>
    /// <param name="comparer">Comparer of the items, or null to use the default comparer of <typeparamref name="T"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> or <paramref name="count"/> is less than 0.</exception>
    /// <exception cref="ArgumentException">Thrown when the range extends past the end of the list.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the comparer can't compare the items.</exception>
    public void Sort(int index, int count, IComparer<T> comparer)
    {
        try
        {
            m_Items.Sort(index, count, comparer);
        }
        catch (InvalidOperationException)
        {
            ReportPartiallySortedItems(index, count);
            throw;
        }

        if (count > 1)
            NotifyReplaced(index, count);
    }

    /// <summary>
    /// Sorts the items of the list with a comparison method.
    /// </summary>
    /// <remarks>
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/>
    /// change for all the items. If the list has fewer than two items, this method doesn't raise any event.
    /// If <paramref name="comparison"/> throws an exception, the items can stay partly sorted, like with
    /// <see cref="List{T}.Sort()"/>, and the event reports them. If a listener of that event also throws an exception, the
    /// list logs it and throws the exception of the comparison.
    /// </remarks>
    /// <param name="comparison">Method that compares two items.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparison"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="comparison"/> throws an exception.</exception>
    public void Sort(Comparison<T> comparison)
    {
        try
        {
            m_Items.Sort(comparison);
        }
        catch (InvalidOperationException)
        {
            ReportPartiallySortedItems(0, m_Items.Count);
            throw;
        }

        if (m_Items.Count > 1)
            NotifyReplaced(0, m_Items.Count);
    }

    void ReportPartiallySortedItems(int index, int count)
    {
        // Like List<T>, the items can stay partly sorted when the comparer throws, so they are reported. A listener
        // that throws doesn't replace the exception of the comparer.
        try
        {
            if (count > 1)
                NotifyReplaced(index, count);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ((ICollection)m_Items).CopyTo(array, index);
    }

    /// <summary>
    /// The number of items in the list.
    /// </summary>
    public int Count => m_Items.Count;

    bool ICollection.IsSynchronized => ((ICollection)m_Items).IsSynchronized;
    object ICollection.SyncRoot => ((ICollection)m_Items).SyncRoot;

    /// <summary>
    /// Indicates whether the list is read-only. This value is always false.
    /// </summary>
    public bool IsReadOnly => ((IList)m_Items).IsReadOnly;

    object IList.this[int index]
    {
        get => m_Items[index];
        set => this[index] = CastItem(value);
    }

    /// <summary>
    /// Returns the index of the first occurrence of an item in the list.
    /// </summary>
    /// <param name="item">Item to find.</param>
    /// <returns>The index of the item, or -1 if the list doesn't contain it.</returns>
    public int IndexOf(T item)
    {
        return m_Items.IndexOf(item);
    }

    /// <summary>
    /// Inserts an item at the specified index.
    /// </summary>
    /// <remarks>
    /// The items at <paramref name="index"/> and after it move to higher indices.
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Inserted"/> change,
    /// even when <paramref name="index"/> is equal to <see cref="Count"/>.
    /// </remarks>
    /// <param name="index">Index of the new item.</param>
    /// <param name="item">Item to insert.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than 0 or greater than <see cref="Count"/>.</exception>
    public void Insert(int index, T item)
    {
        m_Items.Insert(index, item);
        ObserveItem(item);
        NotifyInserted(index);
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <remarks>
    /// The items after the removed item move to lower indices.
    /// Raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Removed"/> change.
    /// </remarks>
    /// <param name="index">Index of the item to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than 0, or equal to or greater than <see cref="Count"/>.</exception>
    public void RemoveAt(int index)
    {
        UnobserveItem(m_Items[index]);
        m_Items.RemoveAt(index);
        NotifyRemoved(index, 1);
    }

    bool IList.IsFixedSize => ((IList)m_Items).IsFixedSize;

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// </summary>
    /// <remarks>
    /// Setting an item raises <see cref="collectionChanged"/> with a <see cref="BindableCollectionChangedEventArgs.ChangeType.Replaced"/>
    /// change. To report a change inside an item that doesn't raise events, assign the item to its index again.
    ///
    /// An item that implements <see cref="INotifyBindablePropertyChanged"/> or <see cref="INotifyBindableCollectionChanged"/>
    /// reports its own changes, so assigning that item to its own index doesn't raise any event. For example, a two-way
    /// data binding that sets a property of an item through the list writes the item back to its index, and the list
    /// only reports the change of the property.
    /// </remarks>
    /// <param name="index">Index of the item.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than 0, or equal to or greater than <see cref="Count"/>.</exception>
    public T this[int index]
    {
        get => m_Items[index];
        set
        {
            var previous = m_Items[index];
            if (IsReassigningObservableItem(previous, value))
                return;

            m_Items[index] = value;
            ReplaceObservedItem(previous, value);
            NotifyReplaced(index);
        }
    }

    /// <summary>
    /// Raised when the content of an item of the list changes.
    /// </summary>
    /// <remarks>
    /// The list raises this event when one of its items raises <see cref="INotifyBindablePropertyChanged.propertyChanged"/> or
    /// <see cref="INotifyBindableCollectionChanged.collectionChanged"/>. The property name of the event is an empty path, so the
    /// event doesn't tell which item changed. When the list is the data source of bindings, the data binding system updates
    /// the bindings whose data source path starts with the index of the changed item, such as `[3].name`, but not the bindings
    /// of the other items.
    ///
    /// While the list raises an event, it raises this event at most once for each item that its listeners change, even if they
    /// change the item several times. This stops lists that contain each other from reporting a change to each other forever.
    ///
    /// When the list raises this event from the event of an item, it logs the exceptions that its listeners throw instead
    /// of throwing them, because they would stop the other listeners of the item.
    ///
    /// The list only listens to its items while this event has at least one listener. Structs and other items that don't
    /// implement <see cref="INotifyBindablePropertyChanged"/> or <see cref="INotifyBindableCollectionChanged"/> never raise this event.
    /// Lists that contain each other listen to each other, so they keep listening to their items after their last other
    /// listener is removed. The items that they share then keep them alive.
    ///
    /// Changes to the list itself, such as added or replaced items, raise <see cref="collectionChanged"/> instead.
    /// </remarks>
    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged
    {
        add
        {
            EnsurePropertyBag();
            var wasObserved = m_PropertyChanged != null;
            m_PropertyChanged += value;
            m_PropertyChangedListeners = null;
            if (!wasObserved && m_PropertyChanged != null)
                StartObservingItems();
        }
        remove
        {
            m_PropertyChanged -= value;
            m_PropertyChangedListeners = null;
            if (m_PropertyChanged == null)
                StopObservingItems();
        }
    }

    /// <summary>
    /// Raised after items are added, inserted, removed, or replaced, or after the list is cleared.
    /// </summary>
    /// <remarks>
    /// The list raises this event at most once for each method call, after the list changes. For example, <see cref="AddRange"/>
    /// raises one event for all the new items. Only <see cref="RemoveAll"/> can raise two events. The sender of the event is the list.
    ///
    /// Changes inside the items raise <see cref="propertyChanged"/> instead.
    ///
    /// If a listener changes the list while it receives an event, the list raises the event for that change after all the
    /// listeners receive the current event. This applies to both <see cref="collectionChanged"/> and <see cref="propertyChanged"/>,
    /// so every listener receives the changes in the order they were made.
    ///
    /// If a listener throws an exception, the other listeners still receive the event and the pending events. The list then
    /// throws the first exception, and logs the others. This applies to both events, except when the change of an item
    /// starts the notification: the list then logs all the exceptions, so that they don't stop the other listeners of the item.
    /// </remarks>
    public event EventHandler<BindableCollectionChangedEventArgs> collectionChanged
    {
        add
        {
            m_CollectionChanged += value;
            m_CollectionChangedListeners = null;
        }
        remove
        {
            m_CollectionChanged -= value;
            m_CollectionChangedListeners = null;
        }
    }

    void StartObservingItems()
    {
        if (!k_CanContainObservableItems)
            return;

        m_ItemPropertyChangedHandler ??= OnItemPropertyChanged;
        m_ItemCollectionChangedHandler ??= OnItemCollectionChanged;
        m_IsObservingItems = true;
        ObserveItems(0, m_Items.Count);
    }

    void StopObservingItems()
    {
        if (!m_IsObservingItems)
            return;

        // Unsubscribing from an item that contains this list can stop observing this list again.
        m_IsObservingItems = false;
        UnsubscribeAll();
    }

    void ObserveItems(int index, int count)
    {
        if (!m_IsObservingItems)
            return;

        for (var i = index; i < index + count; ++i)
        {
            ObserveItem(m_Items[i]);
        }
    }

    // A list that contains itself would relay its own notifications forever.
    bool IsObservable(T item) => item is INotifyBindablePropertyChanged or INotifyBindableCollectionChanged && !ReferenceEquals(item, this);

    void ObserveItem(T item)
    {
        if (!m_IsObservingItems || !IsObservable(item))
            return;

        Subscribe(item);
    }

    // An item that raises its own events already reported the changes made to it, for example by a binding that writes
    // the item back after it sets one of its properties.
    bool IsReassigningObservableItem(T previous, T item) => k_CanContainObservableItems && ReferenceEquals(previous, item) && IsObservable(previous);

    void ReplaceObservedItem(T previous, T item)
    {
        if (!m_IsObservingItems || ReferenceEquals(previous, item))
            return;

        ObserveItem(item);
        UnobserveItem(previous);
    }

    void UnobserveItems(int index, int count)
    {
        if (!m_IsObservingItems)
            return;

        for (var i = index; i < index + count; ++i)
        {
            UnobserveItem(m_Items[i]);
        }
    }

    void UnobserveItem(T item)
    {
        if (!m_IsObservingItems || !IsObservable(item))
            return;

        Unsubscribe(item);
    }

    void UnobserveAllItems()
    {
        if (!m_IsObservingItems)
            return;

        UnsubscribeAll();
    }

    void UnsubscribeAll()
    {
        for (var i = 0; i < m_Items.Count; ++i)
        {
            object item = m_Items[i];
            if (item != null && !ReferenceEquals(item, this))
                Unsubscribe(item);
        }
    }

    void Subscribe(object item)
    {
        if (item is INotifyBindablePropertyChanged propertyNotifier)
            propertyNotifier.propertyChanged += m_ItemPropertyChangedHandler;
        if (item is INotifyBindableCollectionChanged collectionNotifier)
            collectionNotifier.collectionChanged += m_ItemCollectionChangedHandler;
    }

    void Unsubscribe(object item)
    {
        if (item is INotifyBindablePropertyChanged propertyNotifier)
            propertyNotifier.propertyChanged -= m_ItemPropertyChangedHandler;
        if (item is INotifyBindableCollectionChanged collectionNotifier)
            collectionNotifier.collectionChanged -= m_ItemCollectionChangedHandler;
    }

    void OnItemPropertyChanged(object sender, BindablePropertyChangedEventArgs args) => NotifyItemChanged(sender);

    void OnItemCollectionChanged(object sender, BindableCollectionChangedEventArgs args) => NotifyItemChanged(sender);

    // The change is reported on the list itself, with the item instead of its index, so the list never tracks item indices.
    void NotifyItemChanged(object item)
    {
        if (m_PropertyChanged == null || !m_IsObservingItems)
            return;

        // The change is raised from the event of the item, where an exception would stop the other listeners of the item.
        ExceptionDispatchInfo exception = null;
        Notify(new PendingNotification { propertyArgs = BindablePropertyChangedEventArgs.ForChangedItem(item) }, ref exception);
        if (exception != null)
            Debug.LogException(exception.SourceException);
    }

    void NotifyCollectionChanged(in BindableCollectionChangedEventArgs args)
    {
        ExceptionDispatchInfo exception = null;
        NotifyCollectionChanged(args, ref exception);
        exception?.Throw();
    }

    void NotifyCollectionChanged(in BindableCollectionChangedEventArgs args, ref ExceptionDispatchInfo exception)
    {
        if (m_CollectionChanged == null)
            return;

        Notify(new PendingNotification { isCollectionChange = true, collectionArgs = args }, ref exception);
    }

    void Notify(in PendingNotification notification, ref ExceptionDispatchInfo exception)
    {
        if (m_IsNotifying)
        {
            if (notification.isCollectionChange || TrackChangedItem(notification.propertyArgs.changedItem))
                EnqueueNotification(notification);
            return;
        }

        m_IsNotifying = true;
        if (!notification.isCollectionChange)
            m_FirstChangedItem = notification.propertyArgs.changedItem;
        try
        {
            InvokeListeners(notification, ref exception);
            while (m_PendingNotifications is { Count: > 0 })
            {
                InvokeListeners(m_PendingNotifications.Dequeue(), ref exception);
            }
        }
        finally
        {
            m_IsNotifying = false;
            m_FirstChangedItem = null;
            m_PendingChangedItems?.Clear();
        }
    }

    bool TrackChangedItem(object item)
    {
        if (ReferenceEquals(item, m_FirstChangedItem))
            return false;

        m_PendingChangedItems ??= new List<object>();
        for (var i = 0; i < m_PendingChangedItems.Count; ++i)
        {
            if (ReferenceEquals(item, m_PendingChangedItems[i]))
                return false;
        }

        m_PendingChangedItems.Add(item);
        return true;
    }

    void EnqueueNotification(in PendingNotification notification)
    {
        (m_PendingNotifications ??= new Queue<PendingNotification>()).Enqueue(notification);
    }

    void InvokeListeners(in PendingNotification notification, ref ExceptionDispatchInfo exception)
    {
        if (notification.isCollectionChange)
            InvokeListeners(m_CollectionChanged, ref m_CollectionChangedListeners, notification.collectionArgs, ref exception);
        else
            InvokeListeners(m_PropertyChanged, ref m_PropertyChangedListeners, notification.propertyArgs, ref exception);
    }

    // The data binding system is one of the listeners, so a listener that throws must not keep the others from receiving the change.
    void InvokeListeners<TArgs>(
        EventHandler<TArgs> handler,
        ref Delegate[] listeners,
        in TArgs args,
        ref ExceptionDispatchInfo exception)
    {
        if (handler == null)
            return;

        var invocationList = listeners ??= handler.GetInvocationList();
        for (var i = 0; i < invocationList.Length; ++i)
        {
            try
            {
                ((EventHandler<TArgs>)invocationList[i]).Invoke(this, args);
            }
            catch (Exception e)
            {
                if (exception == null)
                    exception = ExceptionDispatchInfo.Capture(e);
                else
                    Debug.LogException(e);
            }
        }
    }

    void NotifyAdded(int index, int count = 1)
        => NotifyCollectionChanged(BindableCollectionChangedEventArgs.Added(index, count));

    void NotifyCleared()
        => NotifyCollectionChanged(BindableCollectionChangedEventArgs.Cleared());

    void NotifyReplaced(int index, int count = 1)
        => NotifyCollectionChanged(BindableCollectionChangedEventArgs.Replaced(index, count));

    void NotifyRemoved(int index, int count)
        => NotifyCollectionChanged(BindableCollectionChangedEventArgs.Removed(index, count));

    void NotifyInserted(int index, int count = 1)
        => NotifyCollectionChanged(BindableCollectionChangedEventArgs.Inserted(index, count));
}
