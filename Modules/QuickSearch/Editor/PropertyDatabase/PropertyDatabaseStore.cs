// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace UnityEditor.Search
{
    interface IPropertyLockable
    {
        IDisposable LockRead();
        IDisposable LockUpgradeableRead();
        IDisposable LockWrite();
    }

    interface IPropertyDatabaseStore : IPropertyLockable
    {
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecord data, bool loadInvalid);
        bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records, bool loadInvalid);
        void Invalidate(ulong documentKey);
        void Invalidate(in PropertyDatabaseRecordKey recordKey);
        void Invalidate(uint documentKeyHiWord);
        void InvalidateMask(ulong documentKeyMask);
        void InvalidateMask(uint documentKeyHiWordMask);
        IEnumerable<IPropertyDatabaseRecord> EnumerateAll(bool enumerateInvalid);
        void Clear();
        IPropertyDatabaseStoreView GetView();
    }

    interface IPropertyDatabaseSerializableStore : IPropertyDatabaseStore
    {
        bool Store(in PropertyDatabaseRecord record);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecord data, bool loadInvalid);
        IEnumerable<PropertyDatabaseRecord> EnumerateAllSerializableRecords(bool enumerateInvalid);
        IPropertyDatabaseSerializableStoreView GetSerializableStoreView();
    }

    interface IPropertyDatabaseVolatileStore : IPropertyDatabaseStore
    {
        bool Store(in PropertyDatabaseRecordKey recordKey, object value);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data, bool loadInvalid);
    }

    interface IPropertyDatabaseStoreView : IDisposable, IPropertyLockable
    {
        long length { get; }
        long byteSize { get; }

        IPropertyDatabaseRecord this[long index] { get; }

        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecord data, bool loadInvalid);
        bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records, bool loadInvalid);
        void Invalidate(ulong documentKey, bool sync);
        void Invalidate(in PropertyDatabaseRecordKey recordKey, bool sync);
        void Invalidate(uint documentKeyHiWord, bool sync);
        void InvalidateMask(ulong documentKeyMask, bool sync);
        void InvalidateMask(uint documentKeyHiWordMask, bool sync);
        IEnumerable<IPropertyDatabaseRecord> EnumerateAll(bool enumerateInvalid);
        void Clear();
        void Sync();
        bool Find(in PropertyDatabaseRecordKey recordKey, out long index);
    }

    interface IPropertyDatabaseSerializableStoreView : IPropertyDatabaseStoreView
    {
        new PropertyDatabaseRecord this[long index] { get; }

        bool Store(in PropertyDatabaseRecord record, bool sync);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecord data, bool loadInvalid);
        IEnumerable<PropertyDatabaseRecord> EnumerateAllSerializableRecords(bool enumerateInvalid);
    }

    interface IPropertyDatabaseVolatileStoreView : IPropertyDatabaseStoreView
    {
        bool Store(in PropertyDatabaseRecordKey recordKey, object value, bool sync);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data, bool loadInvalid);
    }

    readonly struct PropertyDatabaseDocumentKeyRange : IBinarySearchRange<PropertyDatabaseRecordKey>
    {
        readonly ulong m_DocumentKey;

        public PropertyDatabaseDocumentKeyRange(ulong documentKey)
        {
            m_DocumentKey = documentKey;
        }

        public bool StartIsInRange(PropertyDatabaseRecordKey start)
        {
            return start.documentKey >= m_DocumentKey;
        }

        public bool EndIsInRange(PropertyDatabaseRecordKey end)
        {
            return end.documentKey <= m_DocumentKey;
        }
    }

    readonly struct PropertyDatabaseDocumentKeyHiWordRange : IBinarySearchRange<PropertyDatabaseRecordKey>
    {
        readonly ulong m_DocumentKeyHiWord;
        const ulong k_HighMask = 0xffffffff00000000;

        public PropertyDatabaseDocumentKeyHiWordRange(uint documentKeyHiWord)
        {
            m_DocumentKeyHiWord = GetULongFromHiWord(documentKeyHiWord);
        }

        public bool StartIsInRange(PropertyDatabaseRecordKey start)
        {
            return (start.documentKey & k_HighMask) >= m_DocumentKeyHiWord;
        }

        public bool EndIsInRange(PropertyDatabaseRecordKey end)
        {
            return (end.documentKey & k_HighMask) <= m_DocumentKeyHiWord;
        }

        public static ulong GetULongFromHiWord(uint hiWord)
        {
            return (ulong)hiWord << 32;
        }

        public static uint ToHiWord(ulong documentKey)
        {
            return (uint)((documentKey & k_HighMask) >> 32);
        }
    }

    readonly struct PropertyDatabaseDocumentKeyMaskRange : IBinarySearchRange<PropertyDatabaseRecordKey>
    {
        readonly ulong m_LowestValue;

        public PropertyDatabaseDocumentKeyMaskRange(ulong documentKeyMask)
        {
            m_LowestValue = 0;
            unchecked
            {
                var convertedVal = (long)documentKeyMask;
                m_LowestValue = (ulong)(convertedVal & -convertedVal);
            }
        }

        public bool StartIsInRange(PropertyDatabaseRecordKey start)
        {
            return start.documentKey >= m_LowestValue;
        }

        public bool EndIsInRange(PropertyDatabaseRecordKey end)
        {
            // Always return true, since we want to go to the end of records
            return true;
        }

        public static bool KeyMatchesMask(PropertyDatabaseRecordKey key, ulong mask)
        {
            return DocumentKeyMatchesMask(key.documentKey, mask);
        }

        public static bool DocumentKeyMatchesMask(ulong documentKey, ulong mask)
        {
            return (documentKey & mask) != 0;
        }
    }

    static class PropertyDatabaseRecordFinder
    {
        public static bool Find(IBinarySearchRangeData<PropertyDatabaseRecordKey> store, in PropertyDatabaseRecordKey recordKey, out long index)
        {
            if (store.length == 0)
            {
                index = 0;
                return false;
            }

            var searchRange = new BinarySearchRange() {startOffset = 0, endOffset = store.length, halfOffset = store.length / 2};

            while (true)
            {
                index = searchRange.halfOffset;
                var currentRecordKey = store[index];

                if (recordKey == currentRecordKey)
                {
                    index = searchRange.halfOffset;
                    return true;
                }

                if (recordKey < currentRecordKey)
                {
                    searchRange.endOffset = searchRange.halfOffset;
                    searchRange.halfOffset = searchRange.startOffset + (searchRange.endOffset - searchRange.startOffset) / 2;

                    if (searchRange.halfOffset == searchRange.endOffset)
                        break;
                }
                else
                {
                    searchRange.startOffset = searchRange.halfOffset;
                    searchRange.halfOffset = searchRange.startOffset + (searchRange.endOffset - searchRange.startOffset) / 2;

                    if (searchRange.halfOffset == searchRange.startOffset)
                    {
                        ++index;
                        break;
                    }
                }
            }

            return false;
        }

        public static BinarySearchRange FindRange(IBinarySearchRangeData<PropertyDatabaseRecordKey> store, ulong documentKey)
        {
            var range = new PropertyDatabaseDocumentKeyRange(documentKey);
            return BinarySearchFinder.FindRange(range, store);
        }

        public static BinarySearchRange FindHiWordRange(IBinarySearchRangeData<PropertyDatabaseRecordKey> store, uint documentKeyHiWord)
        {
            var range = new PropertyDatabaseDocumentKeyHiWordRange(documentKeyHiWord);
            return BinarySearchFinder.FindRange(range, store);
        }

        public static BinarySearchRange FindMaskRange(IBinarySearchRangeData<PropertyDatabaseRecordKey> store, ulong documentKeyMask)
        {
            var range = new PropertyDatabaseDocumentKeyMaskRange(documentKeyMask);
            return BinarySearchFinder.FindRange(range, store);
        }
    }

    struct ReadLockGuard : IDisposable
    {
        ReaderWriterLockSlim m_Lock;
        bool m_Disposed;

        public ReadLockGuard(ReaderWriterLockSlim readLock)
        {
            m_Disposed = false;
            m_Lock = readLock;
            m_Lock.EnterReadLock();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Lock.ExitReadLock();
            m_Lock = null;
            m_Disposed = true;
        }
    }

    struct UpgradeableReadLockGuard : IDisposable
    {
        ReaderWriterLockSlim m_Lock;
        bool m_Disposed;

        public UpgradeableReadLockGuard(ReaderWriterLockSlim readLock)
        {
            m_Disposed = false;
            m_Lock = readLock;
            m_Lock.EnterUpgradeableReadLock();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Lock.ExitUpgradeableReadLock();
            m_Lock = null;
            m_Disposed = true;
        }
    }

    struct WriteLockGuard : IDisposable
    {
        ReaderWriterLockSlim m_Lock;
        bool m_Disposed;

        public WriteLockGuard(ReaderWriterLockSlim writeLock)
        {
            m_Disposed = false;
            m_Lock = writeLock;
            m_Lock.EnterWriteLock();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Lock.ExitWriteLock();
            m_Lock = null;
            m_Disposed = true;
        }
    }

    abstract class BasePropertyDatabaseStore : IPropertyDatabaseStore
    {
        ReaderWriterLockSlim m_Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecord data, bool loadInvalid = false)
        {
            using (var view = GetView())
                return view.TryLoad(recordKey, out data, loadInvalid);
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records, bool loadInvalid = false)
        {
            using (var view = GetView())
                return view.TryLoad(documentKey, out records, loadInvalid);
        }

        public void Invalidate(ulong documentKey)
        {
            using (var view = GetView())
                view.Invalidate(documentKey, true);
        }

        public void Invalidate(in PropertyDatabaseRecordKey recordKey)
        {
            using (var view = GetView())
                view.Invalidate(recordKey, true);
        }

        public void Invalidate(uint documentKeyHiWord)
        {
            using (var view = GetView())
                view.Invalidate(documentKeyHiWord, true);
        }

        public void InvalidateMask(ulong documentKeyMask)
        {
            using (var view = GetView())
                view.InvalidateMask(documentKeyMask, true);
        }

        public void InvalidateMask(uint documentKeyHiWordMask)
        {
            using (var view = GetView())
                view.InvalidateMask(documentKeyHiWordMask, true);
        }

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll(bool enumerateInvalid = false)
        {
            using (var view = GetView())
                return view.EnumerateAll(enumerateInvalid);
        }

        public void Clear()
        {
            using (var view = GetView())
                view.Clear();
        }

        public abstract IPropertyDatabaseStoreView GetView();

        public IDisposable LockRead()
        {
            return new ReadLockGuard(m_Lock);
        }

        public IDisposable LockUpgradeableRead()
        {
            return new UpgradeableReadLockGuard(m_Lock);
        }

        public IDisposable LockWrite()
        {
            return new WriteLockGuard(m_Lock);
        }
    }

    abstract class BasePropertyDatabaseSerializableStore : BasePropertyDatabaseStore, IPropertyDatabaseSerializableStore
    {
        public bool Store(in PropertyDatabaseRecord record)
        {
            using (var view = GetSerializableStoreView())
                return view.Store(record, true);
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecord data, bool loadInvalid = false)
        {
            using (var view = GetSerializableStoreView())
                return view.TryLoad(recordKey, out data, loadInvalid);
        }

        public IEnumerable<PropertyDatabaseRecord> EnumerateAllSerializableRecords(bool enumerateInvalid = false)
        {
            using (var view = GetSerializableStoreView())
                return view.EnumerateAllSerializableRecords(enumerateInvalid);
        }

        public abstract IPropertyDatabaseSerializableStoreView GetSerializableStoreView();
    }

    class MemoryDataStore<T> : IList<T>
    {
        List<T> m_Data;

        public int Count => m_Data.Count;
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => m_Data[index];
            set => m_Data[index] = value;
        }

        public MemoryDataStore()
        {
            m_Data = new List<T>();
        }

        public MemoryDataStore(IEnumerable<T> initialData)
        {
            m_Data = initialData.ToList();
        }

        public void Add(T item)
        {
            m_Data.Add(item);
        }

        public void Clear()
        {
            m_Data.Clear();
        }

        public void Resize(int capacity)
        {
            m_Data.Capacity = capacity;
        }

        public bool Contains(T item)
        {
            return m_Data.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_Data.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return m_Data.Remove(item);
        }

        public void Assign(List<T> newData)
        {
            m_Data = newData;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return m_Data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return m_Data.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            m_Data.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            m_Data.RemoveAt(index);
        }
    }

    class PropertyDatabaseMemoryStore : BasePropertyDatabaseSerializableStore, IPropertyDatabaseSerializableStore
    {
        protected MemoryDataStore<PropertyDatabaseRecord> m_Store;

        public PropertyDatabaseMemoryStore()
        {
            m_Store = new MemoryDataStore<PropertyDatabaseRecord>();
        }

        public PropertyDatabaseMemoryStore(IEnumerable<PropertyDatabaseRecord> initialStore)
        {
            m_Store = new MemoryDataStore<PropertyDatabaseRecord>(initialStore);
        }

        public override IPropertyDatabaseStoreView GetView() => GetSerializableStoreView();

        public override IPropertyDatabaseSerializableStoreView GetSerializableStoreView()
        {
            return new PropertyDatabaseMemoryStoreView(m_Store, this);
        }
    }

    struct PropertyDatabaseMemoryStoreView : IPropertyDatabaseSerializableStoreView, IBinarySearchRangeData<PropertyDatabaseRecordKey>
    {
        bool m_Disposed;
        MemoryDataStore<PropertyDatabaseRecord> m_MemoryDataStore;
        PropertyDatabaseMemoryStore m_Store;

        public PropertyDatabaseMemoryStoreView(MemoryDataStore<PropertyDatabaseRecord> memoryDataStore, PropertyDatabaseMemoryStore store)
        {
            m_MemoryDataStore = memoryDataStore;
            m_Store = store;
            m_Disposed = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Store = null;
            m_MemoryDataStore = null;
            m_Disposed = true;
        }

        public bool Store(in PropertyDatabaseRecord record, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var foundRecord = Find(record.recordKey, out var index);
                bool insert = !foundRecord;

                using (LockWrite())
                {
                    if (insert)
                        m_MemoryDataStore.Insert((int)index, record);
                    else
                        m_MemoryDataStore[(int)index] = record;
                    return true;
                }
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecord data, bool loadInvalid = false)
        {
            data = PropertyDatabaseRecord.invalid;
            using (LockRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return false;

                data = m_MemoryDataStore[(int)index];
                return data.IsValid() || loadInvalid;
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecord data, bool loadInvalid = false)
        {
            var success = TryLoad(recordKey, out PropertyDatabaseRecord record, loadInvalid);
            data = record;
            return success;
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records, bool loadInvalid = false)
        {
            records = null;
            using (LockRead())
            {
                var binarySearchRange = PropertyDatabaseRecordFinder.FindRange(this, documentKey);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return false;

                var result = new List<IPropertyDatabaseRecord>();
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_MemoryDataStore[(int)i];
                    if (record.IsValid() || loadInvalid)
                        result.Add(record);
                }

                records = result;
                return true;
            }
        }

        public void Sync()
        {}

        public bool Find(in PropertyDatabaseRecordKey recordKey, out long index)
        {
            using (LockRead())
                return PropertyDatabaseRecordFinder.Find(this, recordKey, out index);
        }

        public IDisposable LockRead()
        {
            return m_Store.LockRead();
        }

        public IDisposable LockUpgradeableRead()
        {
            return m_Store.LockUpgradeableRead();
        }

        public IDisposable LockWrite()
        {
            return m_Store.LockWrite();
        }

        public void Invalidate(ulong documentKey, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = PropertyDatabaseRecordFinder.FindRange(this, documentKey);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateRange(binarySearchRange);
            }
        }

        public void Invalidate(in PropertyDatabaseRecordKey recordKey, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return;

                using (LockWrite())
                {
                    var record = m_MemoryDataStore[(int)index];
                    var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                    m_MemoryDataStore[(int)index] = newRecord;
                }
            }
        }

        public void Invalidate(uint documentKeyHiWord, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = PropertyDatabaseRecordFinder.FindHiWordRange(this, documentKeyHiWord);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateRange(binarySearchRange);
            }
        }

        public void InvalidateMask(ulong documentKeyMask, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = PropertyDatabaseRecordFinder.FindMaskRange(this, documentKeyMask);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateMaskRange(binarySearchRange, documentKeyMask);
            }
        }

        public void InvalidateMask(uint documentKeyHiWordMask, bool sync)
        {
            var documentKeyMask = PropertyDatabaseDocumentKeyHiWordRange.GetULongFromHiWord(documentKeyHiWordMask);
            InvalidateMask(documentKeyMask, sync);
        }

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll(bool enumerateInvalid = false)
        {
            return EnumerateAllSerializableRecords(enumerateInvalid).Cast<IPropertyDatabaseRecord>();
        }

        public IEnumerable<PropertyDatabaseRecord> EnumerateAllSerializableRecords(bool enumerateInvalid = false)
        {
            using (LockRead())
            {
                return m_MemoryDataStore.Where(p => p.IsValid() || enumerateInvalid).ToList();
            }
        }

        public long length => m_MemoryDataStore.Count;
        public long byteSize => length * PropertyDatabaseRecord.size;

        public PropertyDatabaseRecordKey this[long index] => m_MemoryDataStore[(int)index].recordKey;

        IPropertyDatabaseRecord IPropertyDatabaseStoreView.this[long index] => m_MemoryDataStore[(int)index];
        PropertyDatabaseRecord IPropertyDatabaseSerializableStoreView.this[long index] => m_MemoryDataStore[(int)index];

        public void MergeWith(IPropertyDatabaseSerializableStore store, bool overrideWithInvalid)
        {
            using (var otherView = store.GetSerializableStoreView())
                MergeWith(otherView, overrideWithInvalid);
        }

        public void MergeWith(IPropertyDatabaseSerializableStoreView view, bool overrideWithInvalid)
        {
            using (LockWrite())
            using (view.LockRead())
            {
                MergeWith(view.EnumerateAllSerializableRecords(overrideWithInvalid), overrideWithInvalid);
            }
        }

        public void MergeWith(IEnumerable<PropertyDatabaseRecord> records, bool overrideWithInvalid)
        {
            var newRecordsList = records.ToList();
            var newRecordsCount = newRecordsList.Count;

            using (LockWrite())
            {
                if (m_MemoryDataStore.Count == 0)
                {
                    m_MemoryDataStore.Resize(newRecordsCount);
                    foreach (var record in newRecordsList)
                    {
                        if (record.IsValid() || overrideWithInvalid)
                            m_MemoryDataStore.Add(record); // Assume already in order
                    }
                }
                else
                {
                    var existingRecordsCount = m_MemoryDataStore.Count;

                    var newData = new List<PropertyDatabaseRecord>(existingRecordsCount + newRecordsCount);

                    var existingRecordsIndex = 0;
                    var newRecordsIndex = 0;
                    while (existingRecordsIndex < existingRecordsCount && newRecordsIndex < newRecordsCount)
                    {
                        var existingRecord = m_MemoryDataStore[existingRecordsIndex];
                        var newRecord = newRecordsList[newRecordsIndex];

                        var compare = existingRecord.CompareTo(newRecord);
                        if (compare < 0)
                        {
                            newData.Add(existingRecord);
                            ++existingRecordsIndex;
                        }
                        else if (compare > 0)
                        {
                            if (newRecord.IsValid() || overrideWithInvalid)
                                newData.Add(newRecord);
                            ++newRecordsIndex;
                        }
                        else
                        {
                            if (newRecord.IsValid() || overrideWithInvalid)
                                newData.Add(newRecord);
                            else
                                newData.Add(existingRecord);
                            ++existingRecordsIndex;
                            ++newRecordsIndex;
                        }
                    }

                    // Either the existing records or the new records have not finished merging. Try them both
                    while (existingRecordsIndex < existingRecordsCount)
                    {
                        newData.Add(m_MemoryDataStore[existingRecordsIndex]);
                        ++existingRecordsIndex;
                    }

                    while (newRecordsIndex < newRecordsCount)
                    {
                        var newRecord = newRecordsList[newRecordsIndex];
                        if (newRecord.IsValid() || overrideWithInvalid)
                            newData.Add(newRecord);
                        ++newRecordsIndex;
                    }

                    m_MemoryDataStore.Assign(newData);
                }
            }
        }

        public void Clear()
        {
            using (LockWrite())
                m_MemoryDataStore.Clear();
        }

        public void SaveToFile(string filePath)
        {
            if (File.Exists(filePath))
                throw new Exception($"File '{filePath}' already exists.");

            using (LockRead())
            {
                using (var fs = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
                {
                    var bw = new BinaryWriter(fs);
                    bw.Write(PropertyDatabase.version);
                    foreach (var databaseRecord in m_MemoryDataStore)
                    {
                        databaseRecord.ToBinary(bw);
                    }
                    fs.Flush(true);
                }
            }
        }

        void InvalidateRange(BinarySearchRange binarySearchRange)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_MemoryDataStore[(int)i];
                    var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                    m_MemoryDataStore[(int)i] = newRecord;
                }
            }
        }

        void InvalidateMaskRange(BinarySearchRange binarySearchRange, ulong documentKeyMask)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_MemoryDataStore[(int)i];
                    if (PropertyDatabaseDocumentKeyMaskRange.KeyMatchesMask(record.key, documentKeyMask))
                    {
                        var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                        m_MemoryDataStore[(int)i] = newRecord;
                    }
                }
            }
        }
    }

    class PropertyDatabaseFileStore : BasePropertyDatabaseSerializableStore
    {
        public string filePath { get; }

        event Action<string> m_FileStoreChanged;
        event Action m_FileStoreAboutToChange;

        HashSet<ulong> m_InvalidatedDocumentKeys;
        HashSet<uint> m_InvalidatedDocumentKeyHiWords;
        HashSet<ulong> m_InvalidatedDocumentKeyMasks;

        public PropertyDatabaseFileStore(string filePath)
        {
            this.filePath = filePath;
            m_InvalidatedDocumentKeys = new HashSet<ulong>();
            m_InvalidatedDocumentKeyHiWords = new HashSet<uint>();
            m_InvalidatedDocumentKeyMasks = new HashSet<ulong>();
        }

        public override IPropertyDatabaseStoreView GetView()
        {
            return GetSerializableStoreView();
        }

        public override IPropertyDatabaseSerializableStoreView GetSerializableStoreView()
        {
            return new PropertyDatabaseFileStoreView(filePath, this, m_InvalidatedDocumentKeys, m_InvalidatedDocumentKeyHiWords, m_InvalidatedDocumentKeyMasks);
        }

        public void SwapFile(string filePathToSwap)
        {
            using (LockWrite())
            {
                try
                {
                    NotifyFileStoreAboutToChange();

                    RetriableOperation<IOException>.Execute(() => File.Copy(filePathToSwap, filePath, true));
                    RetriableOperation<IOException>.Execute(() => File.Delete(filePathToSwap));
                }
                finally
                {
                    // Make sure the views reopen if there is an exception
                    NotifyFileStoreChanged();
                }
            }
        }

        public void ClearInvalidatedDocuments()
        {
            using (LockWrite())
            {
                m_InvalidatedDocumentKeys.Clear();
                m_InvalidatedDocumentKeyHiWords.Clear();
                m_InvalidatedDocumentKeyMasks.Clear();
            }
        }

        public void RegisterFileStoreChangedHandler(Action<string> handler)
        {
            using (LockWrite())
                m_FileStoreChanged += handler;
        }

        public void RegisterFileStoreAboutToChangeHandler(Action handler)
        {
            using (LockWrite())
                m_FileStoreAboutToChange += handler;
        }

        public void UnregisterFileStoreChangedHandler(Action<string> handler)
        {
            using (LockWrite())
                m_FileStoreChanged -= handler;
        }

        public void UnregisterFileStoreAboutToChangeHandler(Action handler)
        {
            using (LockWrite())
                m_FileStoreAboutToChange -= handler;
        }

        public void NotifyFileStoreChanged()
        {
            using (LockUpgradeableRead())
                m_FileStoreChanged?.Invoke(filePath);
        }

        public void NotifyFileStoreAboutToChange()
        {
            using (LockUpgradeableRead())
                m_FileStoreAboutToChange?.Invoke();
        }
    }

    // This needs to be a class, since it registers a itself for changes and the event modifies the filestream.
    // With a struct only the copy would get modified.
    class PropertyDatabaseFileStoreView : IPropertyDatabaseSerializableStoreView, IBinarySearchRangeData<PropertyDatabaseRecordKey>
    {
        bool m_Disposed;
        FileStream m_Fs;
        BinaryReader m_Br;
        BinaryWriter m_Bw;
        PropertyDatabaseFileStore m_Store;
        bool m_NeedsSync;

        HashSet<ulong> m_InvalidatedDocumentKeys;
        HashSet<uint> m_InvalidatedDocumentKeyHiWords;
        HashSet<ulong> m_InvalidatedDocumentKeyMasks;

        public PropertyDatabaseFileStoreView(string filePath, PropertyDatabaseFileStore store, HashSet<ulong> invalidatedDocumentKeys, HashSet<uint> invalidatedDocumentKeyHiWords, HashSet<ulong> invalidatedDocumentKeyMasks)
        {
            length = 0;
            m_Disposed = false;
            m_Fs = null;
            m_Br = null;
            m_Bw = null;
            m_Store = store;
            m_InvalidatedDocumentKeys = invalidatedDocumentKeys;
            m_InvalidatedDocumentKeyHiWords = invalidatedDocumentKeyHiWords;
            m_InvalidatedDocumentKeyMasks = invalidatedDocumentKeyMasks;
            // All fields must be assigned before calling any functions.
            store.RegisterFileStoreChangedHandler(HandleFileStoreChanged);
            store.RegisterFileStoreAboutToChangeHandler(HandleFileStoreAboutToChange);
            OpenFileStream(filePath);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Store.UnregisterFileStoreChangedHandler(HandleFileStoreChanged);
            m_Store.UnregisterFileStoreAboutToChangeHandler(HandleFileStoreAboutToChange);
            m_Br?.Dispose();
            m_Br = null;
            m_Bw?.Dispose();
            m_Bw = null;
            m_Fs?.Dispose();
            m_Fs = null;
            m_Store = null;
            m_Disposed = true;
        }

        void HandleFileStoreChanged(string newFilePath)
        {
            using (LockWrite())
            {
                OpenFileStream(newFilePath);
            }
        }

        void HandleFileStoreAboutToChange()
        {
            using (LockWrite())
            {
                m_Br?.Dispose();
                m_Br = null;
                m_Bw?.Dispose();
                m_Bw = null;
                m_Fs?.Dispose();
                m_Fs = null;
            }
        }

        public bool Store(in PropertyDatabaseRecord record, bool sync)
        {
            // Cannot add anything in this store
            throw new NotSupportedException();
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecord data, bool loadInvalid = false)
        {
            data = PropertyDatabaseRecord.invalid;
            using (LockRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return false;

                data = GetRecord(index);
                return data.IsValid() || loadInvalid;
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecord data, bool loadInvalid = false)
        {
            var success = TryLoad(recordKey, out PropertyDatabaseRecord record, loadInvalid);
            data = record;
            return success;
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records, bool loadInvalid = false)
        {
            records = null;
            using (LockRead())
            {
                var binarySearchRange = FindRange(documentKey);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return false;

                var result = new List<IPropertyDatabaseRecord>();
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = GetRecord(i);
                    if (record.IsValid() || loadInvalid)
                        result.Add(record);
                }

                records = result;
                return true;
            }
        }

        public void Invalidate(ulong documentKey, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = FindRange(documentKey);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateRange(binarySearchRange, sync);
            }
        }

        public void InvalidateInMemory(ulong documentKey, bool sync)
        {
            using (LockWrite())
            {
                m_InvalidatedDocumentKeys.Add(documentKey);
            }
        }

        public void Invalidate(in PropertyDatabaseRecordKey recordKey, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return;

                using (LockWrite())
                {
                    var record = GetRecord(index);
                    var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                    WriteRecord(newRecord, index, sync);
                }
            }
        }

        public void InvalidateInMemory(PropertyDatabaseMemoryStoreView memoryView, in PropertyDatabaseRecordKey recordKey, bool sync)
        {
            using (LockRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return;

                var record = GetRecord(index);
                var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                memoryView.Store(newRecord, sync);
            }
        }

        public void Invalidate(uint documentKeyHiWord, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = FindRange(documentKeyHiWord);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateRange(binarySearchRange, sync);
            }
        }

        public void InvalidateInMemory(uint documentKeyHiWord, bool sync)
        {
            using (LockWrite())
            {
                m_InvalidatedDocumentKeyHiWords.Add(documentKeyHiWord);
            }
        }

        public void InvalidateMask(ulong documentKeyMask, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = FindMaskRange(documentKeyMask);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateMaskRange(binarySearchRange, documentKeyMask, sync);
            }
        }

        public void InvalidateMaskInMemory(ulong documentKeyMask, bool sync)
        {
            using (LockRead())
            {
                m_InvalidatedDocumentKeyMasks.Add(documentKeyMask);
            }
        }

        public void InvalidateMask(uint documentKeyHiWordMask, bool sync)
        {
            var documentKeyMask = PropertyDatabaseDocumentKeyHiWordRange.GetULongFromHiWord(documentKeyHiWordMask);
            InvalidateMask(documentKeyMask, sync);
        }

        public void InvalidateMaskInMemory(uint documentKeyHiWordMask, bool sync)
        {
            var documentKeyMask = PropertyDatabaseDocumentKeyHiWordRange.GetULongFromHiWord(documentKeyHiWordMask);
            InvalidateMaskInMemory(documentKeyMask, sync);
        }

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll(bool enumerateInvalid = false)
        {
            return EnumerateAllSerializableRecords(enumerateInvalid).Cast<IPropertyDatabaseRecord>();
        }

        public IEnumerable<PropertyDatabaseRecord> EnumerateAllSerializableRecords(bool enumerateInvalid = false)
        {
            using (LockRead())
            {
                if (m_Fs == null)
                    yield break;

                for (var i = 0; i < length; ++i)
                {
                    var record = GetRecord(i);
                    if (record.IsValid() || enumerateInvalid)
                        yield return record;
                }
            }
        }

        public void Clear()
        {
            using (LockWrite())
            {
                if (m_Fs == null)
                    return;
                m_Fs.SetLength(sizeof(int));
                m_Fs.Seek(0, SeekOrigin.Begin);
                m_Bw.Write(PropertyDatabase.version);
                m_Fs.Flush(true);
                m_Store.ClearInvalidatedDocuments();
                m_Store.NotifyFileStoreChanged();
            }
        }

        public void Sync()
        {
            if (!m_NeedsSync)
                return;
            m_NeedsSync = false;
            using (LockWrite())
            {
                if (m_Fs == null)
                    return;
                m_Fs.Flush(true);
            }
        }

        public bool Find(in PropertyDatabaseRecordKey recordKey, out long index)
        {
            index = -1;
            if (m_Fs == null)
                return false;
            using (LockRead())
                return PropertyDatabaseRecordFinder.Find(this, recordKey, out index);
        }

        public IDisposable LockRead()
        {
            return m_Store.LockRead();
        }

        public IDisposable LockUpgradeableRead()
        {
            return m_Store.LockUpgradeableRead();
        }

        public IDisposable LockWrite()
        {
            return m_Store.LockWrite();
        }

        public long length { get; private set; }
        public long byteSize => length * PropertyDatabaseRecord.size + sizeof(int);

        public PropertyDatabaseRecordKey this[long index] => GetRecordKey(index);

        IPropertyDatabaseRecord IPropertyDatabaseStoreView.this[long index] => GetRecord(index);
        PropertyDatabaseRecord IPropertyDatabaseSerializableStoreView.this[long index] => GetRecord(index);

        void OpenFileStream(string path)
        {
            using (LockRead())
            {
                if (!File.Exists(path))
                    return;
                RetriableOperation<IOException>.Execute(() => OpenFileStreamInner(path), 10, TimeSpan.FromMilliseconds(100));
            }
        }

        void OpenFileStreamInner(string path)
        {
            using (LockRead())
            {
                m_Fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
                m_Br = new BinaryReader(m_Fs, Encoding.UTF8, true);
                m_Bw = new BinaryWriter(m_Fs, Encoding.UTF8, true);
                if (m_Fs.Length < sizeof(int))
                    return;
                var version = m_Br.ReadInt32();
                if (version != PropertyDatabase.version)
                {
                    length = 0;
                    return;
                }

                UpdateStoreSize(m_Fs);
            }
        }

        void UpdateStoreSize(FileStream fs)
        {
            length = fs == null ? 0 : (fs.Length - sizeof(int)) / PropertyDatabaseRecord.size;
        }

        PropertyDatabaseRecord GetRecord(long index)
        {
            if (m_Fs == null)
                return new PropertyDatabaseRecord();
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var fileOffset = GetFileOffset((int)index);
            m_Fs.Seek(fileOffset, SeekOrigin.Begin);
            var record = PropertyDatabaseRecord.FromBinary(m_Br);
            return IsRecordKeyValid(record.recordKey) ? record : new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
        }

        PropertyDatabaseRecordKey GetRecordKey(long index)
        {
            if (m_Fs == null)
                return new PropertyDatabaseRecordKey();
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var fileOffset = GetFileOffset((int)index);
            m_Fs.Seek(fileOffset, SeekOrigin.Begin);
            return PropertyDatabaseRecordKey.FromBinary(m_Br);
        }

        bool IsRecordKeyValid(PropertyDatabaseRecordKey recordKey)
        {
            var documentKey = recordKey.documentKey;
            if (m_InvalidatedDocumentKeys.Contains(documentKey))
                return false;
            var documentKeyHiWord = PropertyDatabaseDocumentKeyHiWordRange.ToHiWord(documentKey);
            if (m_InvalidatedDocumentKeyHiWords.Contains(documentKeyHiWord))
                return false;
            if (m_InvalidatedDocumentKeyMasks.Any(mask => PropertyDatabaseDocumentKeyMaskRange.DocumentKeyMatchesMask(documentKey, mask)))
                return false;
            return true;
        }

        void WriteRecord(in PropertyDatabaseRecord record, long index, bool sync = true)
        {
            if (m_Fs == null)
                return;
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var fileOffset = GetFileOffset((int)index);
            m_Bw.Seek(fileOffset, SeekOrigin.Begin);
            record.ToBinary(m_Bw);
            m_NeedsSync = true;

            if (sync)
                Sync();
        }

        int GetFileOffset(int recordIndex)
        {
            return sizeof(int) + recordIndex * PropertyDatabaseRecord.size;
        }

        void InvalidateRange(BinarySearchRange binarySearchRange, bool sync)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = GetRecord(i);
                    var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                    WriteRecord(newRecord, i, false);
                }
                if (sync)
                    Sync();
            }
        }

        void InvalidateMaskRange(BinarySearchRange binarySearchRange, ulong documentKeyMask, bool sync)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = GetRecord(i);
                    if (PropertyDatabaseDocumentKeyMaskRange.KeyMatchesMask(record.key, documentKeyMask))
                    {
                        var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                        WriteRecord(newRecord, i, false);
                    }
                }
                if (sync)
                    Sync();
            }
        }

        public IEnumerable<PropertyDatabaseRecord> EnumerateRange(BinarySearchRange range)
        {
            using (LockRead())
            {
                for (var i = range.startOffset; i < range.endOffset; ++i)
                    yield return GetRecord(i);
            }
        }

        public IEnumerable<PropertyDatabaseRecord> EnumerateMaskRange(BinarySearchRange range, ulong documentKeyMask)
        {
            using (LockRead())
            {
                for (var i = range.startOffset; i < range.endOffset; ++i)
                {
                    var record = GetRecord(i);
                    if (PropertyDatabaseDocumentKeyMaskRange.KeyMatchesMask(record.key, documentKeyMask))
                        yield return record;
                }
            }
        }

        public BinarySearchRange FindRange(ulong documentKey)
        {
            return m_Fs == null ? BinarySearchRange.invalid : PropertyDatabaseRecordFinder.FindRange(this, documentKey);
        }

        public BinarySearchRange FindRange(uint documentKeyHiWord)
        {
            return m_Fs == null ? BinarySearchRange.invalid : PropertyDatabaseRecordFinder.FindHiWordRange(this, documentKeyHiWord);
        }

        public BinarySearchRange FindMaskRange(ulong documentKeyMask)
        {
            return m_Fs == null ? BinarySearchRange.invalid : PropertyDatabaseRecordFinder.FindMaskRange(this, documentKeyMask);
        }

        public BinarySearchRange FindMaskRange(uint documentKeyHiWordMask)
        {
            var documentKeyMask = PropertyDatabaseDocumentKeyHiWordRange.GetULongFromHiWord(documentKeyHiWordMask);
            return FindMaskRange(documentKeyMask);
        }
    }

    struct PropertyDatabaseVolatileRecordValue : IPropertyDatabaseRecordValue
    {
        public object value;
        public PropertyDatabaseType type => PropertyDatabaseType.Volatile;

        public static int size => sizeof(PropertyDatabaseType) + 8; // 8 for object ref

        public PropertyDatabaseVolatileRecordValue(object value)
        {
            this.value = value;
        }
    }

    readonly struct PropertyDatabaseVolatileRecord : IPropertyDatabaseRecord
    {
        public readonly PropertyDatabaseRecordKey recordKey;
        public readonly bool valid;
        public readonly PropertyDatabaseVolatileRecordValue recordValue;

        public PropertyDatabaseRecordKey key => recordKey;
        public IPropertyDatabaseRecordValue value => recordValue;
        public bool validRecord => valid;

        public static int size => PropertyDatabaseRecordKey.size + sizeof(bool) + PropertyDatabaseVolatileRecordValue.size;

        public static PropertyDatabaseVolatileRecord invalid => new();

        public PropertyDatabaseVolatileRecord(in PropertyDatabaseRecordKey key, object value)
        {
            this.recordKey = key;
            this.recordValue = new PropertyDatabaseVolatileRecordValue(value);
            valid = true;
        }

        public PropertyDatabaseVolatileRecord(in PropertyDatabaseRecordKey key, object value, bool valid)
        {
            this.recordKey = key;
            this.recordValue = new PropertyDatabaseVolatileRecordValue(value);
            this.valid = valid;
        }
    }

    class PropertyDatabaseVolatileMemoryStore : BasePropertyDatabaseStore, IPropertyDatabaseVolatileStore
    {
        protected MemoryDataStore<PropertyDatabaseVolatileRecord> m_Store;

        public PropertyDatabaseVolatileMemoryStore()
        {
            m_Store = new MemoryDataStore<PropertyDatabaseVolatileRecord>();
        }

        public PropertyDatabaseVolatileMemoryStore(IEnumerable<PropertyDatabaseVolatileRecord> initialStore)
        {
            m_Store = new MemoryDataStore<PropertyDatabaseVolatileRecord>(initialStore);
        }

        public override IPropertyDatabaseStoreView GetView()
        {
            return new PropertyDatabaseVolatileMemoryStoreView(this, m_Store);
        }

        public bool Store(in PropertyDatabaseRecordKey recordKey, object value)
        {
            using (var view = (PropertyDatabaseVolatileMemoryStoreView)GetView())
                return view.Store(recordKey, value, true);
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data, bool loadInvalid = false)
        {
            using (var view = (PropertyDatabaseVolatileMemoryStoreView)GetView())
                return view.TryLoad(recordKey, out data, loadInvalid);
        }
    }

    struct PropertyDatabaseVolatileMemoryStoreView : IPropertyDatabaseVolatileStoreView, IBinarySearchRangeData<PropertyDatabaseRecordKey>
    {
        bool m_Disposed;
        PropertyDatabaseVolatileMemoryStore m_VolatileStore;
        MemoryDataStore<PropertyDatabaseVolatileRecord> m_MemoryDataStore;

        public PropertyDatabaseVolatileMemoryStoreView(PropertyDatabaseVolatileMemoryStore volatileStore, MemoryDataStore<PropertyDatabaseVolatileRecord> memoryDataStore)
        {
            m_VolatileStore = volatileStore;
            m_MemoryDataStore = memoryDataStore;
            m_Disposed = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_VolatileStore = null;
            m_MemoryDataStore = null;
            m_Disposed = true;
        }

        public IDisposable LockRead()
        {
            return m_VolatileStore.LockRead();
        }

        public IDisposable LockUpgradeableRead()
        {
            return m_VolatileStore.LockUpgradeableRead();
        }

        public IDisposable LockWrite()
        {
            return m_VolatileStore.LockWrite();
        }

        public long length => m_MemoryDataStore.Count;

        public long byteSize => length * PropertyDatabaseVolatileRecord.size;

        PropertyDatabaseRecordKey IBinarySearchRangeData<PropertyDatabaseRecordKey>.this[long index] => m_MemoryDataStore[(int)index].key;

        public IPropertyDatabaseRecord this[long index] => throw new NotSupportedException();

        public bool Store(in PropertyDatabaseRecord record, bool sync)
        {
            throw new NotSupportedException();
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecord data, bool loadInvalid = false)
        {
            throw new NotSupportedException();
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecord data, bool loadInvalid = false)
        {
            var success = TryLoad(recordKey, out PropertyDatabaseVolatileRecord record, loadInvalid);
            data = record;
            return success;
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records, bool loadInvalid = false)
        {
            records = null;
            using (LockRead())
            {
                var binarySearchRange = PropertyDatabaseRecordFinder.FindRange(this, documentKey);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return false;

                var result = new List<IPropertyDatabaseRecord>();
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_MemoryDataStore[(int)i];
                    if (record.validRecord || loadInvalid)
                        result.Add(record);
                }

                records = result;
                return true;
            }
        }

        public void Invalidate(ulong documentKey, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = PropertyDatabaseRecordFinder.FindRange(this, documentKey);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateRange(binarySearchRange);
            }
        }

        public void Invalidate(in PropertyDatabaseRecordKey recordKey, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return;

                using (LockWrite())
                {
                    var record = m_MemoryDataStore[(int)index];
                    var newRecord = new PropertyDatabaseVolatileRecord(record.key, record.recordValue.value, false);
                    m_MemoryDataStore[(int)index] = newRecord;
                }
            }
        }

        public void Invalidate(uint documentKeyHiWord, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = PropertyDatabaseRecordFinder.FindHiWordRange(this, documentKeyHiWord);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateRange(binarySearchRange);
            }
        }

        public void InvalidateMask(ulong documentKeyMask, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var binarySearchRange = PropertyDatabaseRecordFinder.FindMaskRange(this, documentKeyMask);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateMaskRange(binarySearchRange, documentKeyMask);
            }
        }

        public void InvalidateMask(uint documentKeyHiWordMask, bool sync)
        {
            var documentKeyMask = PropertyDatabaseDocumentKeyHiWordRange.GetULongFromHiWord(documentKeyHiWordMask);
            InvalidateMask(documentKeyMask, sync);
        }

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll(bool enumerateInvalid = false)
        {
            using (LockRead())
            {
                return m_MemoryDataStore.Where(p => p.valid || enumerateInvalid).Cast<IPropertyDatabaseRecord>().ToList();
            }
        }

        public void Clear()
        {
            using (LockWrite())
                m_MemoryDataStore.Clear();
        }

        public void Sync()
        {}

        public bool Find(in PropertyDatabaseRecordKey recordKey, out long index)
        {
            using (LockRead())
                return PropertyDatabaseRecordFinder.Find(this, recordKey, out index);
        }

        public bool Store(in PropertyDatabaseRecordKey recordKey, object value, bool sync)
        {
            using (LockUpgradeableRead())
            {
                var foundRecord = Find(recordKey, out var index);
                bool insert = !foundRecord;

                var newRecord = new PropertyDatabaseVolatileRecord(recordKey, value);
                using (LockWrite())
                {
                    if (insert)
                        m_MemoryDataStore.Insert((int)index, newRecord);
                    else
                        m_MemoryDataStore[(int)index] = newRecord;
                    return true;
                }
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data, bool loadInvalid = false)
        {
            var success = TryLoad(recordKey, out PropertyDatabaseVolatileRecord record, loadInvalid);
            data = record.recordValue.value;
            return success;
        }

        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseVolatileRecord data, bool loadInvalid = false)
        {
            data = PropertyDatabaseVolatileRecord.invalid;
            using (LockRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return false;

                data = m_MemoryDataStore[(int)index];
                return data.valid || loadInvalid;
            }
        }

        void InvalidateRange(BinarySearchRange binarySearchRange)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_MemoryDataStore[(int)i];
                    var newRecord = new PropertyDatabaseVolatileRecord(record.key, record.recordValue.value, false);
                    m_MemoryDataStore[(int)i] = newRecord;
                }
            }
        }

        void InvalidateMaskRange(BinarySearchRange binarySearchRange, ulong documentKeyMask)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_MemoryDataStore[(int)i];
                    if (PropertyDatabaseDocumentKeyMaskRange.KeyMatchesMask(record.key, documentKeyMask))
                    {
                        var newRecord = new PropertyDatabaseVolatileRecord(record.key, record.recordValue.value, false);
                        m_MemoryDataStore[(int)i] = newRecord;
                    }
                }
            }
        }
    }
}
