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
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements.StyleSheets;

/// <summary>
/// A mutable collection of VisualElement style class names.
/// </summary>
internal unsafe struct StyleClassList : IEnumerable<UniqueStyleString>
{
    private static readonly MemoryLabel k_MemoryLabel = new(nameof(UIElements), nameof(Record));

    // Per record data
    private static ComponentDataStore s_Records;
    private static Record* s_EmptyRecordPtr;
    private static int s_RecordCount;

    // Per classId combined data
    private static UnmanagedBlock<int> s_RecordSortedIdPool;
    private static int s_RecordSortedIdCount = 0;

    // Per distinct hash data
    private static Dictionary<int, StyleClassList> s_HashToFirstRecord;

    static StyleClassList()
    {
        UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.StyleClassList, ClearInstances);
        ResetStaticState();
    }

    private static void ResetStaticState()
    {
        var empty = Record.k_Empty;
        s_Records = new(UnsafeUtility.SizeOf<Record>(), UnsafeUtility.AlignOf<Record>(), k_MemoryLabel, (byte*)&empty);
        s_Records.ResizeCapacity(1);
        s_EmptyRecordPtr = (Record*)s_Records.GetComponentDataPtr(0);
        s_RecordCount = 1;

        s_RecordSortedIdPool = new(128);
        UpdateNativeClassIdPointer();
        s_RecordSortedIdCount = 0;
        s_HashToFirstRecord = new() { { 0, Empty } };
    }

    private static void UpdateNativeClassIdPointer()
    {
        StyleClassListManager.SetClassIdBasePtr(s_RecordSortedIdPool.GetUnsafePtr());
    }

    /// <summary>
    /// For tests, operations inside this scope will not modify the global dictionary.
    /// </summary>
    /// <remarks>
    /// Class lists created within this scope must not be used after this scope has been disposed.
    ///
    /// It is highly recommended to avoid creating elements within this TestScope or to make sure any reference to such
    /// elements is properly discarded at the end of the scope.
    /// </remarks>
    internal readonly struct TestScope : IDisposable
    {
        private readonly ComponentDataStore m_Records;
        private readonly Record* m_EmptyRecordPtr;
        private readonly int m_RecordCount;
        private readonly int m_RecordSortedIdCount;
        private readonly UnmanagedBlock<int> m_RecordSortedIdPool;
        private readonly Dictionary<int, StyleClassList> m_HashToFirstRecordId;

        public TestScope()
        {
            m_Records = s_Records;
            m_EmptyRecordPtr = s_EmptyRecordPtr;
            m_RecordCount = s_RecordCount;
            m_RecordSortedIdPool = s_RecordSortedIdPool;
            m_RecordSortedIdCount = s_RecordSortedIdCount;
            m_HashToFirstRecordId = s_HashToFirstRecord;

            ResetStaticState();
        }

        public void Dispose()
        {
            s_Records.Dispose();
            s_RecordSortedIdPool.Dispose();

            s_Records = m_Records;
            s_EmptyRecordPtr = m_EmptyRecordPtr;
            s_RecordCount = m_RecordCount;
            s_HashToFirstRecord = m_HashToFirstRecordId;
            s_RecordSortedIdPool = m_RecordSortedIdPool;
            s_RecordSortedIdCount = m_RecordSortedIdCount;

            UpdateNativeClassIdPointer();
        }
    }

    /// <summary>
    /// An empty class list. Use this value instead of `new StyleClassList()`.
    /// </summary>
    public static StyleClassList Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(s_EmptyRecordPtr);
    }

    private Record* m_Record;

    private readonly ref Record record
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref *m_Record;
    }

    internal readonly StringEnumerable ToStringEnumerable() => record.ToStringEnumerable();

    /// <summary>
    /// Returns a ReadOnlySpan view of the underlying UniqueStyleString IDs for efficient iteration.
    /// This avoids the overhead of constructing UniqueStyleString objects for each access.
    /// </summary>
    internal readonly ReadOnlySpan<int> GetClassIds() => record.GetClassIds();

    /// <summary>
    /// Returns an offset to the underlying class IDs array for C++ interop.
    /// The array pointer is updated explicitly (won't move with GC) because it points to a NativeArray.
    /// </summary>
    internal readonly int GetClassIdStartOffset() => record.GetClassIdStartOffset();

    /// <summary>
    /// The number of enabled classes in this class list.
    /// </summary>
    public readonly int Count => record.Count;

    /// <summary>
    /// Returns the enabled class in this class list corresponding to the index in sequential order.
    /// </summary>
    public readonly UniqueStyleString this[int index] => record.GetClassName(index);

    /// <summary>
    /// Returns an array containing all the enabled classes in this class list.
    /// </summary>
    public readonly UniqueStyleString[] ToArray()
    {
        var ids = GetClassIds();
        var result = new UniqueStyleString[ids.Length];
        for (var i = 0; i < ids.Length; i++)
            result[i] = new(ids[i]);
        return result;
    }

    private StyleClassList(Record* record)
    {
        m_Record = record;
    }

    /// <summary>
    /// Initializes a class list where all the provided classes are enabled.
    /// </summary>
    /// <remarks>Null or empty classes are ignored and not added to this list.</remarks>
    /// <param name="classNames">The class names to include in this class list</param>
    public StyleClassList(UniqueStyleString[] classNames)
    {
        if (classNames == null)
        {
            m_Record = s_EmptyRecordPtr;
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
            m_Record = s_EmptyRecordPtr;
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

        Record* candidate;
        if (s_HashToFirstRecord.TryGetValue(combinedHash, out var candidateClassList))
        {
            candidate = candidateClassList.m_Record;
            Record* prev;
            int attempts = 0;
            do
            {
                if (candidate->MatchesAll(sanitizedClassNames))
                {
                    m_Record = candidate;
                    return;
                }
                prev = candidate;
                candidate = candidate->m_NextRecordWithSameHash;
            } while (candidate != null && ++attempts <= s_RecordCount);

            candidate = AllocateRecord();
            prev->m_NextRecordWithSameHash = candidate;
        }
        else
        {
            candidate = AllocateRecord();
            s_HashToFirstRecord.Add(combinedHash, new(candidate));
        }

        *candidate = Record.MakeWithNames(sanitizedClassNames, combinedHash);
        m_Record = candidate;
    }

    private static Record* AllocateRecord()
    {
        var recordId = s_RecordCount++;
        if (s_RecordCount > s_Records.Capacity)
        {
            s_Records.ResizeCapacity(Mathf.NextPowerOfTwo(s_RecordCount));
        }

        return (Record*)s_Records.GetComponentDataPtr(recordId);
    }

    /// <summary>
    /// Removes all enabled classes from this class list.
    /// </summary>
    public void Clear()
    {
        m_Record = s_EmptyRecordPtr;
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

        if (!record.Find(className, out var insertIndex))
            _Add(className, insertIndex);
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

        if (!record.Find(className, out var insertIndex))
        {
            _Add(className, insertIndex);
            added = true;
            return;
        }

        added = false;
    }

    private void _Add(UniqueStyleString className, int insertIndex)
    {
        int combinedHash = record.m_Hash ^ className.GetHashCode();

        Record* candidate;
        if (s_HashToFirstRecord.TryGetValue(combinedHash, out var candidateClassList))
        {
            candidate = candidateClassList.m_Record;
            Record* prev;
            int attempts = 0;
            do
            {
                if (candidate->MatchesWithAdded(record, className, insertIndex))
                {
                    m_Record = candidate;
                    return;
                }
                prev = candidate;
                candidate = candidate->m_NextRecordWithSameHash;
            } while (candidate != null && ++attempts <= s_RecordCount);

            candidate = AllocateRecord();
            prev->m_NextRecordWithSameHash = candidate;
        }
        else
        {
            candidate = AllocateRecord();
            s_HashToFirstRecord.Add(combinedHash, new(candidate));
        }

        *candidate = Record.MakeWithAdded(record, className, insertIndex, combinedHash);
        m_Record = candidate;
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

        // Is first class already in there? Add only the second one.
        if (record.Find(className, out var insertIndex))
        {
            Add(className2, out added);
            return;
        }

        // Is second class already in there? Add only the first one.
        if (record.Find(className2, out var insertIndex2))
        {
            _Add(className, insertIndex);
            added = true;
            return;
        }

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
        int combinedHash = record.m_Hash ^ className.GetHashCode() ^ className2.GetHashCode();
        Record* candidate;
        if (s_HashToFirstRecord.TryGetValue(combinedHash, out var candidateClassList))
        {
            candidate = candidateClassList.m_Record;
            Record* prev;
            int attempts = 0;
            do
            {
                if (candidate->MatchesWithAdded(record,
                        className, insertIndex,
                        className2, insertIndex2))
                {
                    m_Record = candidate;
                    return;
                }
                prev = candidate;
                candidate = candidate->m_NextRecordWithSameHash;
            } while (candidate != null && ++attempts <= s_RecordCount);

            candidate = AllocateRecord();
            prev->m_NextRecordWithSameHash = candidate;
        }
        else
        {
            candidate = AllocateRecord();
            s_HashToFirstRecord.Add(combinedHash, new(candidate));
        }

        *candidate = Record.MakeWithAdded(record, className, insertIndex, className2, insertIndex2, combinedHash);
        m_Record = candidate;
    }

    private void _Add(ReadOnlySpan<UniqueStyleString> classNames, ReadOnlySpan<int> insertIndices)
    {
        int combinedHash = record.m_Hash;
        foreach (var className in classNames)
            combinedHash ^= className.GetHashCode();

        Record* candidate;
        if (s_HashToFirstRecord.TryGetValue(combinedHash, out var candidateClassList))
        {
            candidate = candidateClassList.m_Record;
            Record* prev;
            int attempts = 0;
            do
            {
                if (candidate->MatchesWithAdded(record, classNames, insertIndices))
                {
                    m_Record = candidate;
                    return;
                }
                prev = candidate;
                candidate = candidate->m_NextRecordWithSameHash;
            } while (candidate != null && ++attempts <= s_RecordCount);

            candidate = AllocateRecord();
            prev->m_NextRecordWithSameHash = candidate;
        }
        else
        {
            candidate = AllocateRecord();
            s_HashToFirstRecord.Add(combinedHash, new(candidate));
        }

        *candidate = Record.MakeWithAdded(record, classNames, insertIndices, combinedHash);
        m_Record = candidate;
    }

    /// <summary>
    /// Removes the provided class from the enabled classes in this class list.
    /// </summary>
    /// <param name="className">The class to remove</param>
    public void Remove(UniqueStyleString className)
    {
        // No need to check for null or empty, as they have no way of being added in the first place.

        if (record.Find(className, out var removeIndex))
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

        if (record.Find(className, out var removeIndex))
        {
            _Remove(className, removeIndex);
            removed = true;
            return;
        }

        removed = false;
    }

    private void _Remove(UniqueStyleString className, int removeIndex)
    {
        int combinedHash = record.m_Hash ^ className.GetHashCode(); // XOR-ing again to remove the hash

        Record* candidate;
        if (s_HashToFirstRecord.TryGetValue(combinedHash, out var candidateClassList))
        {
            candidate = candidateClassList.m_Record;
            Record* prev;
            int attempts = 0;
            do
            {
                if (candidate->MatchesWithRemoved(record, removeIndex))
                {
                    m_Record = candidate;
                    return;
                }
                prev = candidate;
                candidate = candidate->m_NextRecordWithSameHash;
            } while (candidate != null && ++attempts <= s_RecordCount);

            candidate = AllocateRecord();
            prev->m_NextRecordWithSameHash = candidate;
        }
        else
        {
            candidate = AllocateRecord();
            s_HashToFirstRecord.Add(combinedHash, new(candidate));
        }

        *candidate = Record.MakeWithRemoved(record, removeIndex, combinedHash);
        m_Record = candidate;
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

            if (record.Find(className, out var insertIndex))
                continue;

            sortedNewClasses[newClassCount] = className;
            sortedInsertIndices[newClassCount] = insertIndex;

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

        if (record.Find(className, out var index))
            _Remove(className, index);
        else
            _Add(className, index);
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
    public readonly bool Contains(UniqueStyleString className) => record.Contains(className);

    /// <summary>
    /// Returns a sequential access index of the provided class in the enabled classes of this class list.
    /// If the provided class is not contained in the enabled classes of this class list, returns a negative number.
    /// </summary>
    /// <param name="className">The provided class to find in this class list</param>
    /// <returns>The index of the provided class if present, or a negative number otherwise</returns>
    public readonly int IndexOf(UniqueStyleString className) => record.IndexOf(className);

    internal readonly bool ReferenceEquals(StyleClassList other) => m_Record == other.m_Record;

    /// <summary>
    /// Returns a sequential enumeration of the enabled classes in this class list.
    /// </summary>
    /// <returns>An enumerator going through each enabled class in this class list</returns>
    public readonly Enumerator GetEnumerator() => record.GetEnumerator();

    IEnumerator<UniqueStyleString> IEnumerable<UniqueStyleString>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Cleanup method called during domain unload to dispose all NativeArray instances in cached
    /// Records. Records are never released during the session, so a single sweep here is enough.
    /// </summary>
    internal static void ClearInstances()
    {
        s_Records.Dispose();
        s_RecordSortedIdPool.Dispose();
        UpdateNativeClassIdPointer();
    }

    private struct Record
    {
        public static readonly Record k_Empty = new(0, 0, 0) { m_NextRecordWithSameHash = null };

        private readonly int m_IdPoolStart;
        private readonly int m_Count;
        internal Record* m_NextRecordWithSameHash;
        internal readonly int m_Hash;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Count;
        }

        private int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_RecordSortedIdPool[m_IdPoolStart + index];
        }

        public UniqueStyleString GetClassName(int index)
        {
            if (index < 0 || index >= m_Count)
                throw new IndexOutOfRangeException($"{nameof(index)} in [0, {m_Count}) expected, was {index}.");
            return new UniqueStyleString(s_RecordSortedIdPool[m_IdPoolStart + index]);
        }

        public StringEnumerable ToStringEnumerable() => new(m_IdPoolStart, m_Count);

        public ReadOnlySpan<int> GetClassIds() => s_RecordSortedIdPool.ReadOnlySpan(m_IdPoolStart, m_Count);

        internal int GetClassIdStartOffset() => m_IdPoolStart;

        private Record(int idPoolStart, int count, int hash)
        {
            m_IdPoolStart = idPoolStart;
            m_Count = count;
            m_Hash = hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<int> Alloc(int count, out int idPoolStart)
        {
            idPoolStart = s_RecordSortedIdCount;
            s_RecordSortedIdCount += count;
            if (s_RecordSortedIdCount > s_RecordSortedIdPool.Capacity)
            {
                s_RecordSortedIdPool.Capacity = Mathf.NextPowerOfTwo(s_RecordSortedIdCount);
                UpdateNativeClassIdPointer();
            }

            return s_RecordSortedIdPool.Span(idPoolStart, count);
        }

        public static Record MakeWithNames(ReadOnlySpan<UniqueStyleString> classNames, int hash)
        {
            var sortedClassIds = Alloc(classNames.Length, out var start);
            for (int i = 0; i < classNames.Length; i++)
                sortedClassIds[i] = classNames[i].id;
            return new(start, classNames.Length, hash);
        }

        public static Record MakeWithAdded(Record record,
            UniqueStyleString addedClassName, int insertIndex, int hash)
        {
            var newCount = record.Count + 1;
            var sortedClassIds = Alloc(newCount, out var start);

            for (int i = 0; i < insertIndex; i++)
                sortedClassIds[i] = record[i];

            sortedClassIds[insertIndex] = addedClassName.id;

            for (int i = insertIndex; i < record.Count; i++)
                sortedClassIds[i + 1] = record[i];

            return new(start, newCount, hash);
        }

        // An unrolled version of MakeWithAdded with exactly 2 items, which is a common case during element creation.
        // Insert indices are positions within the record argument, not the final positions of the new items.
        // insertIndex2 can be equal to insertIndex, but the second element should be inserted after the first.
        public static Record MakeWithAdded(Record record,
            UniqueStyleString addedClassName, int insertIndex,
            UniqueStyleString addedClassName2, int insertIndex2, int hash)
        {
            var newCount = record.Count + 2;
            var sortedClassIds = Alloc(newCount, out var start);

            for (int i = 0; i < insertIndex; i++)
                sortedClassIds[i] = record[i];

            sortedClassIds[insertIndex] = addedClassName.id;

            for (int i = insertIndex; i < insertIndex2; i++)
                sortedClassIds[i + 1] = record[i];

            sortedClassIds[insertIndex2 + 1] = addedClassName2.id;

            for (int i = insertIndex2; i < record.Count; i++)
                sortedClassIds[i + 2] = record[i];

            return new(start, newCount, hash);
        }

        // Insert indices are positions within the record argument, not the final positions of the new items.
        // insertIndices[1] can be equal to insertIndices[0], but the second element should be inserted after the first.
        public static Record MakeWithAdded(Record record,
            ReadOnlySpan<UniqueStyleString> addedClassNames, ReadOnlySpan<int> insertIndices, int hash)
        {
            var newCount = record.Count + addedClassNames.Length;
            var sortedClassIds = Alloc(newCount, out var start);

            int recordStart = 0;
            for (int i = 0; i < addedClassNames.Length; i++)
            {
                CopySpan(sortedClassIds, recordStart + i, record, recordStart, insertIndices[i] - recordStart);
                sortedClassIds[insertIndices[i] + i] = addedClassNames[i].id;
                recordStart = insertIndices[i];
            }

            CopySpan(sortedClassIds, recordStart + addedClassNames.Length, record, recordStart, record.Count - recordStart);
            return new Record(start, newCount, hash);
        }

        public static Record MakeWithRemoved(Record record, int removeIndex, int hash)
        {
            var newCount = record.Count - 1;
            var sortedClassIds = Alloc(newCount, out var start);

            for (int i = 0; i < removeIndex; i++)
                sortedClassIds[i] = record[i];

            for (int i = removeIndex + 1; i < record.Count; i++)
                sortedClassIds[i - 1] = record[i];

            return new(start, newCount, hash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(UniqueStyleString className)
        {
            return BinarySearch(className.id) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Find(UniqueStyleString className, out int index)
        {
            index = BinarySearch(className.id);
            if (index >= 0)
            {
                return true;
            }

            index = ~index;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(UniqueStyleString className)
        {
            return BinarySearch(className.id);
        }

        private int BinarySearch(int classId)
        {
            int left = m_IdPoolStart;
            int right = m_IdPoolStart + m_Count - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                int midValue = s_RecordSortedIdPool[mid];

                if (midValue == classId)
                    return mid - m_IdPoolStart;
                if (midValue < classId)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            return ~(left - m_IdPoolStart); // Return bitwise complement of insertion point (same as Array.BinarySearch)
        }

        public bool MatchesAll(ReadOnlySpan<UniqueStyleString> classNames)
        {
            if (m_Count != classNames.Length)
                return false;

            for (int i = 0; i < classNames.Length; i++)
                if (this[i] != classNames[i].id)
                    return false;

            return true;
        }

        public bool MatchesWithAdded(Record record, UniqueStyleString addedClassName, int insertIndex)
        {
            if (Count != record.Count + 1)
                return false;

            if (this[insertIndex] != addedClassName.id)
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

            if (this[insertIndex] != addedClassName.id ||
                this[insertIndex2 + 1] != addedClassName2.id)
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
                if (this[insertIndices[i] + i] != addedClassNames[i].id)
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
                if (this[selfStart++] != record[recordStart++])
                    return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopySpan(Span<int> self, int selfStart, Record record, int recordStart, int count)
        {
            // Assuming short spans, avoid Array.Copy and such
            for (int i = 0; i < count; i++)
                self[selfStart++] = record[recordStart++];
        }

        public Enumerator GetEnumerator() => new(m_IdPoolStart, m_Count);
    }

    // For backward compatibility with tests that use GetClasses() and don't store the underlying type
    internal struct StringEnumerable : IEnumerable<string>
    {
        readonly int m_Start, m_Count;
        internal StringEnumerable(int start, int count) { m_Start = start; m_Count = count; }
        public StringEnumerator GetEnumerator() => new(m_Start, m_Count);
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal struct StringEnumerator : IEnumerator<string>
    {
        readonly int m_Start, m_Count;
        int i;
        internal StringEnumerator(int start, int count) { m_Start = start; m_Count = count; i = -1; }
        public string Current => new UniqueStyleString(s_RecordSortedIdPool[m_Start + i]).value;
        string IEnumerator<string>.Current => Current;
        object IEnumerator.Current => Current;
        public bool MoveNext() => ++i < m_Count;
        public void Reset() { i = -1; }
        public void Dispose() { }
    }

    /// <summary>
    /// A sequential enumeration of VisualElement style class names from an element's class list.
    /// </summary>
    internal struct Enumerator : IEnumerator<UniqueStyleString>
    {
        readonly int m_Start, m_Count;
        int i;
        internal Enumerator(int start, int count) { m_Start = start; m_Count = count; i = -1; }

        /// <summary>
        /// The enabled class name that's currently enumerated.
        /// </summary>
        public UniqueStyleString Current => new(s_RecordSortedIdPool[m_Start + i]);

        /// <summary>
        /// Moves to the next enabled class name.
        /// </summary>
        /// <returns>True if another class is available in the enumeration.</returns>
        public bool MoveNext() => ++i < m_Count;

        UniqueStyleString IEnumerator<UniqueStyleString>.Current => Current;
        object IEnumerator.Current => Current;
        void IEnumerator.Reset() { i = -1; }
        void IDisposable.Dispose() { }
    }
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
    public StyleClassList.Enumerator GetEnumerator() => m_ClassList.GetEnumerator();
}

[NativeHeader("Modules/UIElements/Core/Native/StyleSheets/StyleClassListManager.h")]
internal static class StyleClassListManager
{
    public static extern unsafe void SetClassIdBasePtr(int* ptr);
}
