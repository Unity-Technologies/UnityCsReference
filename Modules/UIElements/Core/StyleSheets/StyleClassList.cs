// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets;

/// <summary>
/// A mutable collection of VisualElement style class names.
/// </summary>
internal struct StyleClassList : IEnumerable<UniqueStyleString>
{
    private static Dictionary<int, List<Record>> s_HashToInstances = new();

    static StyleClassList()
    {
        UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.StyleClassList, ClearInstances);
    }

    // For tests, operations inside this scope will not modify the global dictionary.
    internal readonly struct TestScope : IDisposable
    {
        private readonly Dictionary<int, List<Record>> m_HashToInstances;
        public TestScope()
        {
            m_HashToInstances = s_HashToInstances;
            s_HashToInstances = new();
        }
        public void Dispose()
        {
            s_HashToInstances = m_HashToInstances;
        }
    }

    /// <summary>
    /// An empty class list. Use this value instead of `new StyleClassList()`.
    /// </summary>
    public static readonly StyleClassList Empty = new() { m_Record = Record.k_Empty };

    private Record m_Record;

    internal readonly StringEnumerable ToStringEnumerable() => m_Record.ToStringEnumerable();

    /// <summary>
    /// Returns a ReadOnlySpan view of the underlying UniqueStyleString IDs for efficient iteration.
    /// This avoids the overhead of constructing UniqueStyleString objects for each access.
    /// </summary>
    internal readonly ReadOnlySpan<int> GetClassIds() => m_Record.GetClassIds();

    /// <summary>
    /// Returns an unsafe pointer to the underlying class IDs array for C++ interop.
    /// The pointer is stable (won't move with GC) because it points to a NativeArray.
    /// </summary>
    internal readonly unsafe int* GetClassIdsPtr() => m_Record.GetClassIdsPtr();

    /// <summary>
    /// The number of enabled classes in this class list.
    /// </summary>
    public readonly int Count => m_Record.Count;

    /// <summary>
    /// Returns the enabled class in this class list corresponding to the index in sequential order.
    /// </summary>
    public readonly UniqueStyleString this[int index] => m_Record[index];

    /// <summary>
    /// Returns an array containing all the enabled classes in this class list.
    /// </summary>
    public readonly UniqueStyleString[] ToArray() => m_Record.ToArray();

    /// <summary>
    /// Initializes a class list where all the provided classes are enabled.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="classNames">The class names to include in this class list</param>
    public StyleClassList(UniqueStyleString[] classNames)
    {
        if (classNames == null)
        {
            m_Record = Record.k_Empty;
            return;
        }

        int count = classNames.Length;
        Span<UniqueStyleString> sanitizedClassNames = count <= 256 ? stackalloc UniqueStyleString[count] : new UniqueStyleString[count];
        for (var i = 0; i < count; i++)
            sanitizedClassNames[i] = classNames[i];

        // Sort the class names in increasing id
        SpanSort.Sort(sanitizedClassNames, (ref UniqueStyleString a, ref UniqueStyleString b) => a.id - b.id);

        // Remove any null, empty. After the Sort, they will always be at the start.
        var iNotNullOrEmpty = 0;
        while (iNotNullOrEmpty < count && sanitizedClassNames[iNotNullOrEmpty].IsNullOrEmpty())
            iNotNullOrEmpty++;

        if (iNotNullOrEmpty >= count)
        {
            m_Record = Record.k_Empty;
            return;
        }

        // Remove duplicates. After the Sort, they will always be grouped together.
        sanitizedClassNames[0] = sanitizedClassNames[iNotNullOrEmpty];
        var iKeep = 1;
        for (var i = iNotNullOrEmpty + 1; i < count; i++)
        {
            if (sanitizedClassNames[i] != sanitizedClassNames[i - 1])
                sanitizedClassNames[iKeep++] = sanitizedClassNames[i];
        }
        sanitizedClassNames = sanitizedClassNames.Slice(0, iKeep);

        int combinedHash = 0;
        foreach (var className in sanitizedClassNames)
            combinedHash ^= className.GetHashCode();
        if (s_HashToInstances.TryGetValue(combinedHash, out var instances))
        {
            foreach (var instance in instances)
            {
                if (instance.MatchesAll(sanitizedClassNames))
                {
                    m_Record = instance;
                    return;
                }
            }
        }
        else
        {
            s_HashToInstances.Add(combinedHash, instances = new());
        }

        instances.Add(m_Record = Record.MakeWithNames(combinedHash, sanitizedClassNames));
    }

    /// <summary>
    /// Removes all enabled classes from this class list.
    /// </summary>
    public void Clear()
    {
        m_Record = Record.k_Empty;
    }

    /// <summary>
    /// Adds the provided class to the enabled classes in this class list.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="className">The class to add</param>
    public void Add(UniqueStyleString className)
    {
        if (className.IsNullOrEmpty())
            return;

        int insertIndex = m_Record.Find(className);
        if (insertIndex < 0) // Is class not already in there?
            _Add(className, ~insertIndex);
    }

    /// <summary>
    /// Adds the provided class to the enabled classes in this class list.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="className">The class to add</param>
    /// <param name="added">Returns true if the class list changed as a result of this operation</param>
    public void Add(UniqueStyleString className, out bool added)
    {
        if (className.IsNullOrEmpty())
        {
            added = false;
            return;
        }

        int insertIndex = m_Record.Find(className);
        if (insertIndex < 0) // Is class not already in there?
        {
            _Add(className, ~insertIndex);
            added = true;
            return;
        }

        added = false;
    }

    private void _Add(UniqueStyleString className, int insertIndex)
    {
        int combinedHash = m_Record.combinedHash ^ className.GetHashCode();
        if (s_HashToInstances.TryGetValue(combinedHash, out var instances))
        {
            foreach (var instance in instances)
            {
                if (instance.MatchesWithAdded(m_Record, className, insertIndex))
                {
                    m_Record = instance;
                    return;
                }
            }
        }
        else
        {
            s_HashToInstances.Add(combinedHash, instances = new());
        }

        instances.Add(m_Record = Record.MakeWithAdded(combinedHash, m_Record, className, insertIndex));
    }

    /// <summary>
    /// Adds the provided classes to the enabled classes in this class list.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="className">The class to add</param>
    /// <param name="className2">A second class to add</param>
    public void Add(UniqueStyleString className, UniqueStyleString className2)
    {
        Add(className, className2, out _);
    }

    /// <summary>
    /// Adds the provided classes to the enabled classes in this class list.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="className">The class to add</param>
    /// <param name="className2">A second class to add</param>
    /// <param name="added">Returns true if the class list changed as a result of this operation</param>
    public void Add(UniqueStyleString className, UniqueStyleString className2, out bool added)
    {
        if (className.IsNullOrEmpty())
        {
            Add(className2, out added);
            return;
        }

        if (className2.IsNullOrEmpty())
        {
            Add(className, out added);
            return;
        }

        if (className.id == className2.id)
        {
            Add(className, out added);
            return;
        }

        int insertIndex = m_Record.Find(className);
        if (insertIndex >= 0) // Is first class already in there? Add only the second one.
        {
            Add(className2, out added);
            return;
        }
        insertIndex = ~insertIndex;

        int insertIndex2 = m_Record.Find(className2);
        if (insertIndex2 >= 0) // Is second class already in there? Add only the first one.
        {
            _Add(className, insertIndex);
            added = true;
            return;
        }
        insertIndex2 = ~insertIndex2;

        if (className.id > className2.id) // Keep classes sorted
        {
            (className, className2) = (className2, className);
            (insertIndex, insertIndex2) = (insertIndex2, insertIndex);
        }

        _Add(className, insertIndex, className2, insertIndex2);
        added = true;
    }

    private void _Add(
        UniqueStyleString className, int insertIndex,
        UniqueStyleString className2, int insertIndex2)
    {
        int combinedHash = m_Record.combinedHash ^ className.GetHashCode() ^ className2.GetHashCode();
        if (s_HashToInstances.TryGetValue(combinedHash, out var instances))
        {
            foreach (var instance in instances)
            {
                if (instance.MatchesWithAdded(m_Record,
                        className, insertIndex,
                        className2, insertIndex2))
                {
                    m_Record = instance;
                    return;
                }
            }
        }
        else
        {
            s_HashToInstances.Add(combinedHash, instances = new());
        }

        instances.Add(m_Record = Record.MakeWithAdded(combinedHash, m_Record,
            className, insertIndex,
            className2, insertIndex2));
    }

    private void _Add(ReadOnlySpan<UniqueStyleString> classNames, ReadOnlySpan<int> insertIndices)
    {
        int combinedHash = m_Record.combinedHash;
        foreach (var className in classNames)
            combinedHash ^= className.GetHashCode();

        if (s_HashToInstances.TryGetValue(combinedHash, out var instances))
        {
            foreach (var instance in instances)
            {
                if (instance.MatchesWithAdded(m_Record, classNames, insertIndices))
                {
                    m_Record = instance;
                    return;
                }
            }
        }
        else
        {
            s_HashToInstances.Add(combinedHash, instances = new());
        }

        instances.Add(m_Record = Record.MakeWithAdded(combinedHash, m_Record, classNames, insertIndices));
    }

    /// <summary>
    /// Removes the provided class from the enabled classes in this class list.
    /// </summary>
    /// <param name="className">The class to remove</param>
    public void Remove(UniqueStyleString className)
    {
        // No need to check for null or empty, as they have no way of being added in the first place.

        int removeIndex = m_Record.Find(className);
        if (removeIndex >= 0) // Is class effectively in there?
            _Remove(className, removeIndex);
    }

    /// <summary>
    /// Removes the provided class from the enabled classes in this class list.
    /// </summary>
    /// <param name="className">The class to remove</param>
    /// <param name="removed">Returns true if the class list changed as a result of this operation</param>
    public void Remove(UniqueStyleString className, out bool removed)
    {
        // No need to check for null or empty, as they have no way of being added in the first place.

        int removeIndex = m_Record.Find(className);
        if (removeIndex >= 0) // Is class effectively in there?
        {
            _Remove(className, removeIndex);
            removed = true;
            return;
        }

        removed = false;
    }

    private void _Remove(UniqueStyleString className, int removeIndex)
    {
        int combinedHash = m_Record.combinedHash ^ className.GetHashCode(); // XOR-ing again to remove the hash
        if (s_HashToInstances.TryGetValue(combinedHash, out var instances))
        {
            foreach (var instance in instances)
            {
                if (instance.MatchesWithRemoved(m_Record, removeIndex))
                {
                    m_Record = instance;
                    return;
                }
            }
        }
        else
        {
            s_HashToInstances.Add(combinedHash, instances = new());
        }

        instances.Add(m_Record = Record.MakeWithRemoved(combinedHash, m_Record, removeIndex));
    }

    /// <summary>
    /// Adds the provided classes to the enabled classes in this class list.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="classNames">The classes to add</param>
    public void AddRange(ReadOnlySpan<UniqueStyleString> classNames)
    {
        _AddRange(classNames, out _);
    }

    /// <summary>
    /// Adds the provided classes to the enabled classes in this class list.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="classNames">The classes to add</param>
    /// <param name="added">Returns true if the class list changed as a result of this operation</param>
    public void AddRange(ReadOnlySpan<UniqueStyleString> classNames, out bool added)
    {
        _AddRange(classNames, out added);
    }

    private unsafe void _AddRange(ReadOnlySpan<UniqueStyleString> classNames, out bool added)
    {
        var sortedNewClasses = stackalloc UniqueStyleString[classNames.Length];
        var sortedInsertIndices = stackalloc int[classNames.Length];
        var newClassCount = 0;

        // Remove null or empty classes and classes that are already present.
        foreach (var className in classNames)
        {
            if (className.IsNullOrEmpty())
                continue;

            int insertIndex = m_Record.Find(className);
            if (insertIndex >= 0)
                continue;

            sortedNewClasses[newClassCount] = className;
            sortedInsertIndices[newClassCount] = ~insertIndex;

            // Keep sorted using bubble-up (assuming we're working with small lists)
            for (int j = newClassCount; j > 0 && sortedNewClasses[j].id < sortedNewClasses[j - 1].id; j--)
            {
                (sortedNewClasses[j], sortedNewClasses[j - 1]) = (sortedNewClasses[j - 1], sortedNewClasses[j]);
                (sortedInsertIndices[j], sortedInsertIndices[j - 1]) = (sortedInsertIndices[j - 1], sortedInsertIndices[j]);
            }

            newClassCount++;
        }

        added = newClassCount > 0;
        if (!added)
            return;

        // Remove duplicates
        var distinctClassCount = newClassCount;
        for (var i = 1; i < newClassCount; i++)
        {
            if (sortedNewClasses[i].id == sortedNewClasses[i - 1].id)
            {
                distinctClassCount = i;

                for (i++; i < newClassCount; i++)
                {
                    if (sortedNewClasses[i].id == sortedNewClasses[distinctClassCount - 1].id)
                        continue;

                    sortedNewClasses[distinctClassCount] = sortedNewClasses[i];
                    sortedInsertIndices[distinctClassCount] = sortedInsertIndices[i];
                    distinctClassCount++;
                }
                break;
            }
        }

        // Add all classes at once
        _Add(new ReadOnlySpan<UniqueStyleString>(sortedNewClasses, distinctClassCount),
             new ReadOnlySpan<int>(sortedInsertIndices, distinctClassCount));
    }

    /// <summary>
    /// Adds the provided class in the enabled classes in this class list if not present. Otherwise, removes it.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="className">The class to add or remove</param>
    public void Toggle(UniqueStyleString className)
    {
        if (className.IsNullOrEmpty())
            return;

        int index = m_Record.Find(className);
        if (index < 0)
            _Add(className, ~index);
        else
            _Remove(className, index);
    }

    /// <summary>
    /// If enable is true, adds the provided class to the enabled classes in this class list. Otherwise, removes it.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="className">The class to add or remove</param>
    /// <param name="enable">True if the class should be added</param>
    /// <param name="changed">Returns true if the class list changed as a result of this operation</param>
    public void Enable(UniqueStyleString className, bool enable, out bool changed)
    {
        if (className.IsNullOrEmpty())
        {
            changed = false;
            return;
        }

        if (enable)
            Add(className, out changed);
        else
            Remove(className, out changed);
    }

    /// <summary>
    /// Returns true if the provided class is in the enabled classes in this class list.
    /// </summary>
    /// <param name="className">The provided class to find in this class list</param>
    /// <returns>True if the provided class is enabled classes in this class list</returns>
    public readonly bool Contains(UniqueStyleString className) => m_Record.Contains(className);

    /// <summary>
    /// Returns a sequential access index of the provided class in the enabled classes of this class list.
    /// If the provided class is not contained in the enabled classes of this class list, returns a negative number.
    /// </summary>
    /// <param name="className">The provided class to find in this class list</param>
    /// <returns>The index of the provided class if present, or a negative number otherwise</returns>
    public readonly int IndexOf(UniqueStyleString className) => m_Record.Find(className);

    internal readonly bool ReferenceEquals(StyleClassList other) => m_Record == other.m_Record;

    /// <summary>
    /// Returns a sequential enumeration of the enabled classes in this class list.
    /// </summary>
    /// <returns>An enumerator going through each enabled class in this class list</returns>
    public readonly StyleClassListEnumerator GetEnumerator() => m_Record.GetEnumerator();

    IEnumerator<UniqueStyleString> IEnumerable<UniqueStyleString>.GetEnumerator() => new StyleClassListBoxedEnumerator(m_Record.m_SortedClassIds);
    IEnumerator IEnumerable.GetEnumerator() => new StyleClassListBoxedEnumerator(m_Record.m_SortedClassIds);

    /// <summary>
    /// Cleanup method called during domain unload to dispose all NativeArray instances in cached
    /// Records. Records are never released during the session, so a single sweep here is enough.
    /// </summary>
    internal static void ClearInstances()
    {
        Record.k_Empty.Dispose();
        foreach (var kvp in s_HashToInstances)
        {
            foreach (var record in kvp.Value)
            {
                record.Dispose();
            }
        }
        s_HashToInstances.Clear();
    }

    private class Record : IDisposable
    {
        public static readonly Record k_Empty = new(0, new NativeArray<int>(0, Allocator.Persistent));
        internal NativeArray<int> m_SortedClassIds;

        public int Count => m_SortedClassIds.Length;
        public UniqueStyleString this[int index] => new UniqueStyleString(m_SortedClassIds[index]);

        public UniqueStyleString[] ToArray()
        {
            var result = new UniqueStyleString[m_SortedClassIds.Length];
            for (var i = 0; i < m_SortedClassIds.Length; i++)
                result[i] = new(m_SortedClassIds[i]);
            return result;
        }

        public StringEnumerable ToStringEnumerable() => new(m_SortedClassIds);

        public ReadOnlySpan<int> GetClassIds() => m_SortedClassIds.AsReadOnlySpan();

        internal unsafe int* GetClassIdsPtr() => (int*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(m_SortedClassIds);

        public void Dispose()
        {
            if (m_SortedClassIds.IsCreated)
                m_SortedClassIds.Dispose();
        }

        public int combinedHash { get; }

        private Record(int combinedHash, NativeArray<int> sortedClassIds)
        {
            this.combinedHash = combinedHash;
            m_SortedClassIds = sortedClassIds;
        }

        public static Record MakeWithNames(int combinedHash, ReadOnlySpan<UniqueStyleString> classNames)
        {
            var sortedClassIds = new NativeArray<int>(classNames.Length, Allocator.Persistent);
            for (int i = 0; i < classNames.Length; i++)
                sortedClassIds[i] = classNames[i].id;
            return new(combinedHash, sortedClassIds);
        }

        public static Record MakeWithAdded(int combinedHash, Record record,
            UniqueStyleString addedClassName, int insertIndex)
        {
            var sortedClassIds = new NativeArray<int>(record.m_SortedClassIds.Length + 1, Allocator.Persistent);

            for (int i = 0; i < insertIndex; i++)
                sortedClassIds[i] = record.m_SortedClassIds[i];

            sortedClassIds[insertIndex] = addedClassName.id;

            for (int i = insertIndex; i < record.m_SortedClassIds.Length; i++)
                sortedClassIds[i + 1] = record.m_SortedClassIds[i];

            return new(combinedHash, sortedClassIds);
        }

        // An unrolled version of MakeWithAdded with exactly 2 items, which is a common case during element creation.
        // Insert indices are positions within the record argument, not the final positions of the new items.
        // insertIndex2 can be equal to insertIndex, but the second element should be inserted after the first.
        public static Record MakeWithAdded(int combinedHash, Record record,
            UniqueStyleString addedClassName, int insertIndex,
            UniqueStyleString addedClassName2, int insertIndex2)
        {
            var sortedClassIds = new NativeArray<int>(record.m_SortedClassIds.Length + 2, Allocator.Persistent);

            for (int i = 0; i < insertIndex; i++)
                sortedClassIds[i] = record.m_SortedClassIds[i];

            sortedClassIds[insertIndex] = addedClassName.id;

            for (int i = insertIndex; i < insertIndex2; i++)
                sortedClassIds[i + 1] = record.m_SortedClassIds[i];

            sortedClassIds[insertIndex2 + 1] = addedClassName2.id;

            for (int i = insertIndex2; i < record.m_SortedClassIds.Length; i++)
                sortedClassIds[i + 2] = record.m_SortedClassIds[i];

            return new(combinedHash, sortedClassIds);
        }

        // Insert indices are positions within the record argument, not the final positions of the new items.
        // insertIndices[1] can be equal to insertIndices[0], but the second element should be inserted after the first.
        public static Record MakeWithAdded(int combinedHash, Record record,
            ReadOnlySpan<UniqueStyleString> addedClassNames, ReadOnlySpan<int> insertIndices)
        {
            var sortedClassIds = new NativeArray<int>(record.m_SortedClassIds.Length + addedClassNames.Length, Allocator.Persistent);
            var self = new Record(combinedHash, sortedClassIds);

            int recordStart = 0;
            for (int i = 0; i < addedClassNames.Length; i++)
            {
                self.CopySpan(recordStart + i, record, recordStart, insertIndices[i] - recordStart);
                sortedClassIds[insertIndices[i] + i] = addedClassNames[i].id;
                recordStart = insertIndices[i];
            }

            self.CopySpan(recordStart + addedClassNames.Length, record, recordStart, record.Count - recordStart);
            return self;
        }

        public static Record MakeWithRemoved(int combinedHash, Record record, int removeIndex)
        {
            var sortedClassIds = new NativeArray<int>(record.m_SortedClassIds.Length - 1, Allocator.Persistent);

            for (int i = 0; i < removeIndex; i++)
                sortedClassIds[i] = record.m_SortedClassIds[i];

            for (int i = removeIndex + 1; i < record.m_SortedClassIds.Length; i++)
                sortedClassIds[i - 1] = record.m_SortedClassIds[i];

            return new(combinedHash, sortedClassIds);
        }

        public bool Contains(UniqueStyleString className)
        {
            return BinarySearch(className.id) >= 0;
        }

        public int Find(UniqueStyleString className)
        {
            return BinarySearch(className.id);
        }

        private int BinarySearch(int classId)
        {
            int left = 0;
            int right = m_SortedClassIds.Length - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                int midValue = m_SortedClassIds[mid];

                if (midValue == classId)
                    return mid;
                if (midValue < classId)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            return ~left; // Return bitwise complement of insertion point (same as Array.BinarySearch)
        }

        public bool MatchesAll(ReadOnlySpan<UniqueStyleString> classNames)
        {
            if (m_SortedClassIds.Length != classNames.Length)
                return false;

            for (int i = 0; i < classNames.Length; i++)
                if (m_SortedClassIds[i] != classNames[i].id)
                    return false;

            return true;
        }

        public bool MatchesWithAdded(Record record, UniqueStyleString addedClassName, int insertIndex)
        {
            if (Count != record.Count + 1)
                return false;

            if (m_SortedClassIds[insertIndex] != addedClassName.id)
                return false;

            // Because of hash collisions, we need to also compare the rest of the record.
            // Technically we could skip one element, but for safety we'll check everything.
            return CompareSpan(0, record, 0, insertIndex) &&
                   CompareSpan(insertIndex + 1, record, insertIndex, Count - insertIndex - 1);
        }

        // An unrolled version of MatchesWithAdded with exactly 2 items, which is a common case during element creation.
        // Insert indices are positions within the record argument, not the final positions of the new items.
        // insertIndex2 can be equal to insertIndex, but the second element should be inserted after the first.
        public bool MatchesWithAdded(Record record,
            UniqueStyleString addedClassName, int insertIndex,
            UniqueStyleString addedClassName2, int insertIndex2)
        {
            if (Count != record.Count + 2)
                return false;

            if (m_SortedClassIds[insertIndex] != addedClassName.id ||
                m_SortedClassIds[insertIndex2 + 1] != addedClassName2.id)
                return false;

            return CompareSpan(0, record, 0, insertIndex) &&
                   CompareSpan(insertIndex + 1, record, insertIndex, insertIndex2 - insertIndex) &&
                   CompareSpan(insertIndex2 + 2, record, insertIndex2, record.Count - insertIndex2);
        }

        // Insert indices are positions within the record argument, not the final positions of the new items.
        // insertIndices[1] can be equal to insertIndices[0], but the second element should be inserted after the first.
        public bool MatchesWithAdded(Record record,
            ReadOnlySpan<UniqueStyleString> addedClassNames, ReadOnlySpan<int> insertIndices)
        {
            if (Count != record.Count + addedClassNames.Length)
                return false;

            int recordStart = 0;
            for (var i = 0; i < addedClassNames.Length; i++)
            {
                if (!CompareSpan(recordStart + i, record, recordStart, insertIndices[i] - recordStart))
                    return false;
                if (m_SortedClassIds[insertIndices[i] + i] != addedClassNames[i].id)
                    return false;
                recordStart = insertIndices[i];
            }

            return CompareSpan(recordStart + addedClassNames.Length, record, recordStart, record.Count - recordStart);
        }

        public bool MatchesWithRemoved(Record record, int removeIndex)
        {
            if (Count != record.Count - 1)
                return false;

            return CompareSpan(0, record, 0, removeIndex) &&
                   CompareSpan(removeIndex, record, removeIndex + 1, Count - removeIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CompareSpan(int selfStart, Record record, int recordStart, int count)
        {
            // Assuming short spans, avoid ReadOnlySpan.SequenceEqual and such
            for (int i = 0; i < count; i++)
                if (m_SortedClassIds[selfStart++] != record.m_SortedClassIds[recordStart++])
                    return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopySpan(int selfStart, Record record, int recordStart, int count)
        {
            // Assuming short spans, avoid Array.Copy and such
            for (int i = 0; i < count; i++)
                m_SortedClassIds[selfStart++] = record.m_SortedClassIds[recordStart++];
        }

        public StyleClassListEnumerator GetEnumerator() => new(m_SortedClassIds.AsReadOnlySpan());
    }

    // For backward compatibility with tests that use GetClasses() and don't store the underlying type
    internal struct StringEnumerable : IEnumerable<string>
    {
        readonly NativeArray<int> ids;
        internal StringEnumerable(NativeArray<int> nativeIds) { this.ids = nativeIds; }
        public StringEnumerator GetEnumerator() => new(ids);
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal struct StringEnumerator : IEnumerator<string>
    {
        readonly NativeArray<int> ids;
        int i;
        internal StringEnumerator(NativeArray<int> ids) { this.ids = ids; i = -1; }
        public string Current => new UniqueStyleString(ids[i]).value;
        string IEnumerator<string>.Current => Current;
        object IEnumerator.Current => Current;
        public bool MoveNext() => ++i < ids.Length;
        public void Reset() { }
        public void Dispose() { }
    }
}

/// <summary>
/// A sequential enumeration of VisualElement style class names from an element's class list.
/// </summary>
internal ref struct StyleClassListEnumerator
{
    private readonly ReadOnlySpan<int> ids;
    private int i;
    internal StyleClassListEnumerator(ReadOnlySpan<int> ids) { this.ids = ids; i = -1; }

    /// <summary>
    /// The enabled class name that's currently enumerated.
    /// </summary>
    public UniqueStyleString Current => new(ids[i]);

    /// <summary>
    /// Moves to the next enabled class name.
    /// </summary>
    /// <returns>True if another class is available in the enumeration.</returns>
    public bool MoveNext() => ++i < ids.Length;
}

/// <summary>
/// A boxed enumerator for IEnumerable interface implementation.
/// Uses NativeArray directly since it cannot be a ref struct.
/// </summary>
internal class StyleClassListBoxedEnumerator : IEnumerator<UniqueStyleString>
{
    private readonly NativeArray<int> ids;
    private int i;

    internal StyleClassListBoxedEnumerator(NativeArray<int> ids)
    {
        this.ids = ids;
        i = -1;
    }

    public UniqueStyleString Current => new(ids[i]);
    object IEnumerator.Current => Current;
    public bool MoveNext() => ++i < ids.Length;
    public void Reset() => i = -1;
    public void Dispose() { }
}

/// <summary>
/// A readonly collection of VisualElement style class names.
/// </summary>
internal readonly ref struct StyleClassListRef
{
    private readonly StyleClassList m_ClassList;

    internal StyleClassListRef(StyleClassList classList)
    {
        m_ClassList = classList;
    }

    /// <summary>
    /// The number of enabled classes in this class list.
    /// </summary>
    public int Count => m_ClassList.Count;
    /// <summary>
    /// Returns the enabled class in this class list corresponding to the index in sequential order.
    /// </summary>
    public UniqueStyleString this[int index] => m_ClassList[index];
    /// <summary>
    /// Returns an array containing all the enabled classes in this class list.
    /// </summary>
    public UniqueStyleString[] ToArray() => m_ClassList.ToArray();

    /// <summary>
    /// Returns a ReadOnlySpan view of the underlying UniqueStyleString IDs for efficient iteration.
    /// This avoids the overhead of constructing UniqueStyleString objects for each access.
    /// </summary>
    internal ReadOnlySpan<int> GetClassIds() => m_ClassList.GetClassIds();

    /// <summary>
    /// Returns true if the provided class is enabled classes in this class list.
    /// </summary>
    /// <param name="className">The provided class to find in this class list</param>
    /// <returns>True if the provided class is enabled classes in this class list</returns>
    public bool Contains(UniqueStyleString className) => m_ClassList.Contains(className);
    /// <summary>
    /// Returns a sequential access index of the provided class in the enabled classes of this class list.
    /// If the provided class is not contained in the enabled classes of this class list, returns a negative number.
    /// </summary>
    /// <param name="className">The provided class to find in this class list</param>
    /// <returns>The index of the provided class if present, or a negative number otherwise</returns>
    public int IndexOf(UniqueStyleString className) => m_ClassList.IndexOf(className);
    /// <summary>
    /// Returns a sequential enumeration of the enabled classes in this class list.
    /// </summary>
    /// <returns>An enumerator going through each enabled class in this class list</returns>
    public StyleClassListEnumerator GetEnumerator() => m_ClassList.GetEnumerator();
}
