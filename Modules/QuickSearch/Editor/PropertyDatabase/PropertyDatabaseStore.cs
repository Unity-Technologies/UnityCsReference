// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        bool Store(in PropertyDatabaseRecord record);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data);
        bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records);
        void Invalidate(ulong documentKey);
        void Invalidate(in PropertyDatabaseRecordKey recordKey);
        void Invalidate(uint documentKeyHiWord);
        void InvalidateMask(ulong documentKeyMask);
        void InvalidateMask(uint documentKeyHiWordMask);
        IEnumerable<IPropertyDatabaseRecord> EnumerateAll();
        void Clear();
        IPropertyDatabaseStoreView GetView();
    }

    interface IPropertyDatabaseVolatileStore : IPropertyDatabaseStore
    {
        bool Store(in PropertyDatabaseRecordKey recordKey, object value);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data);
    }

    interface IPropertyDatabaseStoreView : IDisposable, IPropertyLockable
    {
        long length { get; }
        long byteSize { get; }

        PropertyDatabaseRecord this[long index] { get; }

        bool Store(in PropertyDatabaseRecord record, bool sync);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data);
        bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records);
        void Invalidate(ulong documentKey, bool sync);
        void Invalidate(in PropertyDatabaseRecordKey recordKey, bool sync);
        void Invalidate(uint documentKeyHiWord, bool sync);
        void InvalidateMask(ulong documentKeyMask, bool sync);
        void InvalidateMask(uint documentKeyHiWordMask, bool sync);
        IEnumerable<IPropertyDatabaseRecord> EnumerateAll();
        void Clear();
        void Sync();
        bool Find(in PropertyDatabaseRecordKey recordKey, out long index);
    }

    interface IPropertyDatabaseVolatileStoreView : IPropertyDatabaseStoreView
    {
        bool Store(in PropertyDatabaseRecordKey recordKey, object value, bool sync);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data);
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

        public bool Store(in PropertyDatabaseRecord record)
        {
            using (var view = GetView())
                return view.Store(record, true);
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data)
        {
            using (var view = GetView())
                return view.TryLoad(recordKey, out data);
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data)
        {
            using (var view = GetView())
                return view.TryLoad(recordKey, out data);
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records)
        {
            using (var view = GetView())
                return view.TryLoad(documentKey, out records);
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

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll()
        {
            using (var view = GetView())
                return view.EnumerateAll();
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

    abstract class BasePropertyDatabaseMemoryStore<T> : BasePropertyDatabaseStore
        where T : struct
    {
        protected List<T> m_Store;

        public List<T> data => m_Store;

        public BasePropertyDatabaseMemoryStore()
        {
            m_Store = new List<T>();
        }

        public BasePropertyDatabaseMemoryStore(List<T> initialStore)
        {
            m_Store = initialStore;
        }
    }

    class PropertyDatabaseMemoryStore : BasePropertyDatabaseMemoryStore<PropertyDatabaseRecord>
    {
        public PropertyDatabaseMemoryStore()
        {}

        public PropertyDatabaseMemoryStore(List<PropertyDatabaseRecord> initialStore)
            : base(initialStore)
        {}

        public override IPropertyDatabaseStoreView GetView()
        {
            return new PropertyDatabaseMemoryStoreView(m_Store, this);
        }
    }

    struct PropertyDatabaseMemoryStoreView : IPropertyDatabaseStoreView, IBinarySearchRangeData<PropertyDatabaseRecordKey>
    {
        bool m_Disposed;
        List<PropertyDatabaseRecord> m_StoreData;
        PropertyDatabaseMemoryStore m_Store;

        public PropertyDatabaseMemoryStoreView(List<PropertyDatabaseRecord> storeData, PropertyDatabaseMemoryStore store)
        {
            m_StoreData = storeData;
            m_Store = store;
            m_Disposed = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Store = null;
            m_StoreData = null;
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
                        m_StoreData.Insert((int)index, record);
                    else
                        m_StoreData[(int)index] = record;
                    return true;
                }
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data)
        {
            data = PropertyDatabaseRecordValue.invalid;
            using (LockRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return false;

                var record = m_StoreData[(int)index];
                if (!record.IsValid())
                    return false;

                data = record.recordValue;
                return true;
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data)
        {
            var success = TryLoad(recordKey, out PropertyDatabaseRecordValue record);
            data = record;
            return success;
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records)
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
                    var record = m_StoreData[(int)i];
                    if (record.IsValid())
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
                    var record = m_StoreData[(int)index];
                    var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                    m_StoreData[(int)index] = newRecord;
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

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll()
        {
            using (LockRead())
            {
                return m_StoreData.Where(p => p.IsValid()).Cast<IPropertyDatabaseRecord>().ToList();
            }
        }

        public long length => m_StoreData.Count;
        public long byteSize => length * PropertyDatabaseRecord.size;

        public PropertyDatabaseRecordKey this[long index] => m_StoreData[(int)index].recordKey;

        PropertyDatabaseRecord IPropertyDatabaseStoreView.this[long index] => m_StoreData[(int)index];

        public void MergeWith(IPropertyDatabaseStore store)
        {
            using (var otherView = store.GetView())
                MergeWith(otherView);
        }

        public void MergeWith(IPropertyDatabaseStoreView view)
        {
            using (LockWrite())
            using (view.LockRead())
            {
                for (var i = 0; i < view.length; ++i)
                {
                    var record = view[i];
                    if (record.IsValid())
                        Store(record, true);
                }
            }
        }

        public void MergeWith(IEnumerable<PropertyDatabaseRecord> records)
        {
            using (LockWrite())
            {
                foreach (var record in records)
                {
                    if (record.IsValid())
                        Store(record, true);
                }
            }
        }

        public void Clear()
        {
            using (m_Store.LockWrite())
                m_StoreData.Clear();
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
                    foreach (var databaseRecord in m_StoreData)
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
                    var record = m_StoreData[(int)i];
                    var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                    m_StoreData[(int)i] = newRecord;
                }
            }
        }

        void InvalidateMaskRange(BinarySearchRange binarySearchRange, ulong documentKeyMask)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_StoreData[(int)i];
                    if ((record.key.documentKey & documentKeyMask) != 0)
                    {
                        var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                        m_StoreData[(int)i] = newRecord;
                    }
                }
            }
        }
    }

    class PropertyDatabaseFileStore : BasePropertyDatabaseStore
    {
        public string filePath { get; }

        event Action<string> m_FileStoreChanged;
        event Action m_FileStoreAboutToChange;

        public PropertyDatabaseFileStore(string filePath)
        {
            this.filePath = filePath;
        }

        public override IPropertyDatabaseStoreView GetView()
        {
            return new PropertyDatabaseFileStoreView(filePath, this);
        }

        public PropertyDatabaseMemoryStore ToMemoryStore()
        {
            using (var view = new PropertyDatabaseFileStoreView(filePath, this))
                return view.ToMemoryStore();
        }

        public void SwapFile(string filePathToSwap)
        {
            using (LockWrite())
            {
                try
                {
                    NotifyFileStoreAboutToChange();
                    File.Copy(filePathToSwap, filePath, true);
                    File.Delete(filePathToSwap);
                }
                finally
                {
                    // Make sure the views reopen if there is an exception
                    NotifyFileStoreChanged();
                }
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
    class PropertyDatabaseFileStoreView : IPropertyDatabaseStoreView, IBinarySearchRangeData<PropertyDatabaseRecordKey>
    {
        bool m_Disposed;
        FileStream m_Fs;
        BinaryReader m_Br;
        BinaryWriter m_Bw;
        PropertyDatabaseFileStore m_Store;

        public PropertyDatabaseFileStoreView(string filePath, PropertyDatabaseFileStore store)
        {
            length = 0;
            m_Disposed = false;
            m_Fs = null;
            m_Br = null;
            m_Bw = null;
            m_Store = store;
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
                m_Bw?.Dispose();
                m_Fs?.Dispose();
            }
        }

        public bool Store(in PropertyDatabaseRecord record, bool sync)
        {
            // Cannot add anything in this store
            throw new NotSupportedException();
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data)
        {
            data = PropertyDatabaseRecordValue.invalid;
            using (LockRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return false;

                var record = GetRecord(index);
                if (!record.IsValid())
                    return false;

                data = record.recordValue;
                return true;
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data)
        {
            var success = TryLoad(recordKey, out PropertyDatabaseRecordValue record);
            data = record;
            return success;
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records)
        {
            records = null;
            using (LockRead())
            {
                if (m_Fs == null)
                    return false;
                var binarySearchRange = PropertyDatabaseRecordFinder.FindRange(this, documentKey);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return false;

                var result = new List<IPropertyDatabaseRecord>();
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = GetRecord(i);
                    if (record.IsValid())
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
                if (m_Fs == null)
                    return;
                var binarySearchRange = PropertyDatabaseRecordFinder.FindRange(this, documentKey);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateRange(binarySearchRange, sync);
            }
        }

        public void Invalidate(in PropertyDatabaseRecordKey recordKey, bool sync)
        {
            using (LockUpgradeableRead())
            {
                if (m_Fs == null)
                    return;
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

        public void Invalidate(uint documentKeyHiWord, bool sync)
        {
            using (LockUpgradeableRead())
            {
                if (m_Fs == null)
                    return;
                var binarySearchRange = PropertyDatabaseRecordFinder.FindHiWordRange(this, documentKeyHiWord);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateRange(binarySearchRange, sync);
            }
        }

        public void InvalidateMask(ulong documentKeyMask, bool sync)
        {
            using (LockUpgradeableRead())
            {
                if (m_Fs == null)
                    return;
                var binarySearchRange = PropertyDatabaseRecordFinder.FindMaskRange(this, documentKeyMask);
                if (binarySearchRange == BinarySearchRange.invalid)
                    return;

                InvalidateMaskRange(binarySearchRange, documentKeyMask, sync);
            }
        }

        public void InvalidateMask(uint documentKeyHiWordMask, bool sync)
        {
            var documentKeyMask = PropertyDatabaseDocumentKeyHiWordRange.GetULongFromHiWord(documentKeyHiWordMask);
            InvalidateMask(documentKeyMask, sync);
        }

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll()
        {
            using (LockRead())
            {
                if (m_Fs == null)
                    return Enumerable.Empty<IPropertyDatabaseRecord>();

                var allRecords = new List<IPropertyDatabaseRecord>((int)length);
                for (var i = 0; i < length; ++i)
                {
                    var record = GetRecord(i);
                    if (record.IsValid())
                        allRecords.Add(record);
                }

                return allRecords;
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
                m_Store.NotifyFileStoreChanged();
            }
        }

        public void Sync()
        {
            using (LockWrite())
            {
                if (m_Fs == null)
                    return;
                m_Fs.Flush(true);
            }
        }

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

        public PropertyDatabaseMemoryStore ToMemoryStore()
        {
            using (LockRead())
            {
                if (length == 0)
                    return new PropertyDatabaseMemoryStore();

                var localStore = new List<PropertyDatabaseRecord>((int)length);

                for (var i = 0; i < length; ++i)
                {
                    m_Fs.Seek(GetFileOffset(i), SeekOrigin.Begin);
                    var record = PropertyDatabaseRecord.FromBinary(m_Br);
                    localStore.Add(record);
                }

                return new PropertyDatabaseMemoryStore(localStore);
            }
        }

        public long length { get; private set; }
        public long byteSize => length * PropertyDatabaseRecord.size + sizeof(int);

        public PropertyDatabaseRecordKey this[long index] => GetRecordKey(index);

        PropertyDatabaseRecord IPropertyDatabaseStoreView.this[long index] => GetRecord(index);

        void OpenFileStream(string path)
        {
            using (LockRead())
            {
                if (!File.Exists(path))
                    return;
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
            return PropertyDatabaseRecord.FromBinary(m_Br);
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

        void WriteRecord(in PropertyDatabaseRecord record, long index, bool flush = true)
        {
            if (m_Fs == null)
                return;
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var fileOffset = GetFileOffset((int)index);
            m_Bw.Seek(fileOffset, SeekOrigin.Begin);
            record.ToBinary(m_Bw);

            if (flush)
                m_Fs.Flush(true);
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
                    if ((record.key.documentKey & documentKeyMask) != 0)
                    {
                        var newRecord = new PropertyDatabaseRecord(record.recordKey, record.recordValue, false);
                        WriteRecord(newRecord, i, false);
                    }
                }
                if (sync)
                    Sync();
            }
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

    class PropertyDatabaseVolatileMemoryStore : BasePropertyDatabaseMemoryStore<PropertyDatabaseVolatileRecord>, IPropertyDatabaseVolatileStore
    {
        public override IPropertyDatabaseStoreView GetView()
        {
            return new PropertyDatabaseVolatileMemoryStoreView(this, m_Store);
        }

        public bool Store(in PropertyDatabaseRecordKey recordKey, object value)
        {
            using (var view = (PropertyDatabaseVolatileMemoryStoreView)GetView())
                return view.Store(recordKey, value, true);
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data)
        {
            using (var view = (PropertyDatabaseVolatileMemoryStoreView)GetView())
                return view.TryLoad(recordKey, out data);
        }
    }

    struct PropertyDatabaseVolatileMemoryStoreView : IPropertyDatabaseVolatileStoreView, IBinarySearchRangeData<PropertyDatabaseRecordKey>
    {
        bool m_Disposed;
        PropertyDatabaseVolatileMemoryStore m_VolatileStore;
        List<PropertyDatabaseVolatileRecord> m_StoreData;

        public PropertyDatabaseVolatileMemoryStoreView(PropertyDatabaseVolatileMemoryStore volatileStore, List<PropertyDatabaseVolatileRecord> storeData)
        {
            m_VolatileStore = volatileStore;
            m_StoreData = storeData;
            m_Disposed = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_VolatileStore = null;
            m_StoreData = null;
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

        public long length => m_StoreData.Count;

        public long byteSize => length * PropertyDatabaseVolatileRecord.size;

        PropertyDatabaseRecordKey IBinarySearchRangeData<PropertyDatabaseRecordKey>.this[long index] => m_StoreData[(int)index].key;

        public PropertyDatabaseRecord this[long index] => throw new NotSupportedException();

        public bool Store(in PropertyDatabaseRecord record, bool sync)
        {
            throw new NotSupportedException();
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data)
        {
            throw new NotSupportedException();
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data)
        {
            var success = TryLoad(recordKey, out PropertyDatabaseVolatileRecordValue record);
            data = record;
            return success;
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records)
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
                    var record = m_StoreData[(int)i];
                    if (record.valid)
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
                    var record = m_StoreData[(int)index];
                    var newRecord = new PropertyDatabaseVolatileRecord(record.key, record.recordValue.value, false);
                    m_StoreData[(int)index] = newRecord;
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

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll()
        {
            using (LockRead())
            {
                return m_StoreData.Where(p => p.valid).Cast<IPropertyDatabaseRecord>().ToList();
            }
        }

        public void Clear()
        {
            using (LockWrite())
                m_StoreData.Clear();
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
                        m_StoreData.Insert((int)index, newRecord);
                    else
                        m_StoreData[(int)index] = newRecord;
                    return true;
                }
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data)
        {
            var success = TryLoad(recordKey, out PropertyDatabaseVolatileRecordValue record);
            data = record.value;
            return success;
        }

        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseVolatileRecordValue data)
        {
            data = new PropertyDatabaseVolatileRecordValue();
            using (LockRead())
            {
                var foundRecord = Find(recordKey, out var index);
                if (!foundRecord)
                    return false;

                var record = m_StoreData[(int)index];
                if (!record.valid)
                    return false;

                data = record.recordValue;
                return true;
            }
        }

        void InvalidateRange(BinarySearchRange binarySearchRange)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_StoreData[(int)i];
                    var newRecord = new PropertyDatabaseVolatileRecord(record.key, record.recordValue.value, false);
                    m_StoreData[(int)i] = newRecord;
                }
            }
        }

        void InvalidateMaskRange(BinarySearchRange binarySearchRange, ulong documentKeyMask)
        {
            using (LockWrite())
            {
                for (var i = binarySearchRange.startOffset; i < binarySearchRange.endOffset; ++i)
                {
                    var record = m_StoreData[(int)i];
                    if ((record.key.documentKey & documentKeyMask) != 0)
                    {
                        var newRecord = new PropertyDatabaseVolatileRecord(record.key, record.recordValue.value, false);
                        m_StoreData[(int)i] = newRecord;
                    }
                }
            }
        }
    }
}
