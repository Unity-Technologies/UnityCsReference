// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define USE_PERFORMANCE_TRACKER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor.Search
{
    interface IPropertyDatabaseView : IDisposable
    {
        public bool Store(string documentId, string propertyPath, object value);
        public bool Store(ulong documentKey, Hash128 propertyHash, object value);
        public bool Store(Hash128 propertyHash, object value);
        public bool Store(in PropertyDatabaseRecordKey recordKey, object value);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data);
        bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data);
        bool TryLoad(ulong documentKey, out IEnumerable<object> data);
        bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records);
        void Invalidate(string documentId);
        void Invalidate(ulong documentKey);
        void Invalidate(in PropertyDatabaseRecordKey recordKey);
        void InvalidateMask(ulong documentKeyMask);
        IEnumerable<IPropertyDatabaseRecord> EnumerateAll();
        void Sync();
        void Clear();
        public PropertyDatabaseRecordKey CreateRecordKey(string documentId, string propertyPath);
        public PropertyDatabaseRecordKey CreateRecordKey(ulong documentKey, Hash128 propertyPathHash);
        public PropertyDatabaseRecordKey CreateRecordKey(string propertyPath);
        public PropertyDatabaseRecordKey CreateRecordKey(string documentId, Hash128 propertyHash);
        public PropertyDatabaseRecordKey CreateRecordKey(Hash128 propertyHash);
        public ulong CreateDocumentKey(string documentId);
        public Hash128 CreatePropertyHash(string propertyPath);
        TValue GetValueFromRecord<TValue>(IPropertyDatabaseRecordValue recordValue);
        public bool IsPersistableType(Type type);
    }

    class PropertyDatabaseLock : IDisposable
    {
        class SharedCounter
        {
            public int value;

            public SharedCounter(int value)
            {
                this.value = value;
            }
        }

        string m_Path;
        SharedCounter m_Counter;
        FileStream m_Fs;
        bool m_Disposed;

        const string k_TempFolderPath = "Temp/";

        PropertyDatabaseLock(string propertyDatabasePath, SharedCounter counter)
        {
            m_Path = $"{k_TempFolderPath}PropertyDatabase_{propertyDatabasePath.GetHashCode()}";
            m_Fs = File.Open(m_Path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            m_Counter = counter;
        }

        PropertyDatabaseLock(PropertyDatabaseLock other)
        {
            this.m_Path = other.m_Path;
            this.m_Counter = other.m_Counter;
            this.m_Disposed = other.m_Disposed;
            this.m_Fs = other.m_Fs;
        }

        public static bool TryOpen(string propertyDatabasePath, out PropertyDatabaseLock pdbl)
        {
            pdbl = null;
            try
            {
                pdbl = new PropertyDatabaseLock(propertyDatabasePath, new SharedCounter(1));
                return true;
            }
            catch (IOException)
            {
                pdbl = null;
                return false;
            }
        }

        public PropertyDatabaseLock Share()
        {
            lock (m_Counter)
            {
                if (m_Disposed)
                    throw new ObjectDisposedException(m_Path, "Trying to share a lock that has already been disposed.");
                ++m_Counter.value;
                return new PropertyDatabaseLock(this);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PropertyDatabaseLock()
        {
            Dispose(false);
        }

        void Dispose(bool disposed)
        {
            lock (m_Counter)
            {
                if (m_Disposed)
                    return;
                --m_Counter.value;
                if (m_Counter.value == 0)
                {
                    m_Fs.Dispose();
                    m_Fs = null;
                    m_Disposed = true;
                    if (File.Exists(m_Path))
                        File.Delete(m_Path);
                }
            }
        }
    }

    class PropertyDatabase : IDisposable
    {
        PropertyDatabaseVolatileMemoryStore m_LocalVolatileStore;
        PropertyDatabaseMemoryStore m_LocalStore;
        PropertyDatabaseFileStore m_FileStore;
        PropertyStringTable m_StringTable;
        PropertyDatabaseLock m_PropertyDatabaseLock;

        internal static readonly int version = ((0x50 << 24) | (0x44 << 16) | 0x0007) ^ PropertyStringTable.version ^ ((InternalEditorUtility.GetUnityVersion().Major & 0xffff) << 16 | InternalEditorUtility.GetUnityVersion().Minor);

        public string filePath { get; }
        internal string stringTableFilePath { get; }
        public bool autoFlush { get; }

        public bool valid => m_Valid && !disposed;

        const double k_DefaultBackgroundUpdateDebounceInSeconds = 5.0;
        Delayer m_Debounce;
        bool m_Valid;

        internal bool disposed { get; private set; }

        internal PropertyDatabaseErrorView disposedView { get; private set; }

        public PropertyDatabase(string filePath)
            : this(filePath, false)
        {}

        public PropertyDatabase(string filePath, bool autoFlush, double backgroundUpdateDebounceInSeconds = k_DefaultBackgroundUpdateDebounceInSeconds)
        {
            this.filePath = filePath;
            stringTableFilePath = GetStringTablePath(filePath);

            disposedView = new PropertyDatabaseErrorView($"The property database has already been disposed.");

            if (PropertyDatabaseLock.TryOpen(filePath, out m_PropertyDatabaseLock))
            {
                m_LocalVolatileStore = new PropertyDatabaseVolatileMemoryStore();
                m_LocalStore = new PropertyDatabaseMemoryStore();
                m_FileStore = new PropertyDatabaseFileStore(filePath);
                m_StringTable = new PropertyStringTable(stringTableFilePath, 30);

                // Do not allow automatic background updates while running tests. The writing of the file
                // causes an assembly leak during the test Unity.IntegrationTests.Scripting.AssemblyReloadTest.AssemblyReloadDoesntLeakAssemblies
                // on MacOs. I haven't found out why exactly does the writing of a file causes an assembly to be held, so instead I deactivate
                // the automatic update during tests.
                this.autoFlush = autoFlush && !Utils.IsRunningTests();

                m_Debounce = Delayer.Debounce(_ => TriggerBackgroundUpdate(), backgroundUpdateDebounceInSeconds);
                m_Valid = true;
            }
            else
            {
                this.autoFlush = false;
                m_Valid = false;
            }
        }

        public bool Store(string documentId, string propertyPath, object value)
        {
            using (var view = GetView())
                return view.Store(documentId, propertyPath, value);
        }

        public bool Store(ulong documentKey, Hash128 propertyHash, object value)
        {
            using (var view = GetView())
                return view.Store(documentKey, propertyHash, value);
        }

        public bool Store(Hash128 propertyHash, object value)
        {
            using (var view = GetView())
                return view.Store(propertyHash, value);
        }

        public bool Store(in PropertyDatabaseRecordKey recordKey, object value)
        {
            using (var view = GetView())
                return view.Store(recordKey, value);
        }

        internal bool Store(in PropertyDatabaseRecordKey recordKey, in PropertyDatabaseRecordValue value)
        {
            using (var view = (PropertyDatabaseView)GetView())
                return view.Store(recordKey, value);
        }

        internal bool Store(in PropertyDatabaseRecord record)
        {
            using (var view = (PropertyDatabaseView)GetView())
                return view.Store(record);
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object value)
        {
            using (var view = GetView())
                return view.TryLoad(recordKey, out value);
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue value)
        {
            using (var view = GetView())
                return view.TryLoad(recordKey, out value);
        }

        internal bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue value)
        {
            using (var view = (PropertyDatabaseView)GetView())
                return view.TryLoad(recordKey, out value);
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<object> data)
        {
            using (var view = GetView())
                return view.TryLoad(documentKey, out data);
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records)
        {
            using (var view = GetView())
                return view.TryLoad(documentKey, out records);
        }

        public void Invalidate(string documentId)
        {
            using (var view = GetView())
                view.Invalidate(documentId);
        }

        public void Invalidate(ulong documentKey)
        {
            using (var view = GetView())
                view.Invalidate(documentKey);
        }

        public void Invalidate(in PropertyDatabaseRecordKey recordKey)
        {
            using (var view = GetView())
                view.Invalidate(recordKey);
        }

        internal void Invalidate(uint documentKeyHiWord)
        {
            using (var view = GetView())
                view.Invalidate(documentKeyHiWord);
        }

        public void InvalidateMask(ulong documentKeyMask)
        {
            using (var view = GetView())
                view.InvalidateMask(documentKeyMask);
        }

        internal void InvalidateMask(uint documentKeyHiWordMask)
        {
            using (var view = GetView())
                view.InvalidateMask(documentKeyHiWordMask);
        }

        public void Clear()
        {
            using (var view = GetView())
                view.Clear();
        }

        internal PropertyDatabaseRecord CreateRecord(string documentId, string propertyPath, object value)
        {
            return CreateRecord(CreateDocumentKey(documentId), CreatePropertyHash(propertyPath), value);
        }

        internal PropertyDatabaseRecord CreateRecord(string documentId, string propertyPath, object value, PropertyStringTableView view)
        {
            return CreateRecord(CreateDocumentKey(documentId), CreatePropertyHash(propertyPath), value, view);
        }

        internal PropertyDatabaseRecord CreateRecord(ulong documentKey, Hash128 propertyPathHash, object value)
        {
            var key = CreateRecordKey(documentKey, propertyPathHash);
            return CreateRecord(key, value);
        }

        internal PropertyDatabaseRecord CreateRecord(ulong documentKey, Hash128 propertyPathHash, object value, PropertyStringTableView view)
        {
            var key = CreateRecordKey(documentKey, propertyPathHash);
            return CreateRecord(key, value, view);
        }

        internal PropertyDatabaseRecord CreateRecord(string propertyPath, object value)
        {
            return CreateRecord(CreatePropertyHash(propertyPath), value);
        }

        internal PropertyDatabaseRecord CreateRecord(string propertyPath, object value, PropertyStringTableView view)
        {
            return CreateRecord(CreatePropertyHash(propertyPath), value, view);
        }

        internal PropertyDatabaseRecord CreateRecord(Hash128 propertyPathHash, object value)
        {
            return CreateRecord(0, propertyPathHash, value);
        }

        internal PropertyDatabaseRecord CreateRecord(Hash128 propertyPathHash, object value, PropertyStringTableView view)
        {
            return CreateRecord(0, propertyPathHash, value, view);
        }

        internal PropertyDatabaseRecord CreateRecord(in PropertyDatabaseRecordKey recordKey, object value)
        {
            var recordValue = CreateRecordValue(value);
            return CreateRecord(recordKey, recordValue);
        }

        internal PropertyDatabaseRecord CreateRecord(in PropertyDatabaseRecordKey recordKey, object value, PropertyStringTableView view)
        {
            var recordValue = CreateRecordValue(value, view);
            return CreateRecord(recordKey, recordValue);
        }

        internal static PropertyDatabaseRecord CreateRecord(in PropertyDatabaseRecordKey recordKey, PropertyDatabaseRecordValue recordValue)
        {
            if (!IsSupportedPropertyType(recordValue.propertyType))
                throw new ArgumentException($"Property type of {nameof(recordValue)} is not supported.");
            return new PropertyDatabaseRecord(recordKey, recordValue);
        }

        public static PropertyDatabaseRecordKey CreateRecordKey(string documentId, string propertyPath)
        {
            return CreateRecordKey(documentId, CreatePropertyHash(propertyPath));
        }

        public static Hash128 CreatePropertyHash(string propertyPath)
        {
            return Hash128.Compute(propertyPath);
        }

        public static PropertyDatabaseRecordKey CreateRecordKey(ulong documentKey, Hash128 propertyPathHash)
        {
            return new PropertyDatabaseRecordKey(documentKey, propertyPathHash);
        }

        public static PropertyDatabaseRecordKey CreateRecordKey(string propertyPath)
        {
            return CreateRecordKey(CreatePropertyHash(propertyPath));
        }

        public static PropertyDatabaseRecordKey CreateRecordKey(string documentId, Hash128 propertyHash)
        {
            return CreateRecordKey(CreateDocumentKey(documentId), propertyHash);
        }

        public static PropertyDatabaseRecordKey CreateRecordKey(Hash128 propertyHash)
        {
            return CreateRecordKey(0, propertyHash);
        }

        public static ulong CreateDocumentKey(string documentId)
        {
            return string.IsNullOrEmpty(documentId) ? 0UL : documentId.GetHashCode64();
        }

        internal PropertyDatabaseRecordValue CreateRecordValue(object value)
        {
            if (!valid)
                return PropertyDatabaseRecordValue.invalid;
            using (var view = m_StringTable.GetView())
                return CreateRecordValue(value, view);
        }

        internal PropertyDatabaseRecordValue CreateRecordValue(object value, PropertyStringTableView stringTableView)
        {
            if (!IsPersistableType(value.GetType()))
                throw new ArgumentException($"Type \"{value.GetType()}\" is not supported.");

            return PropertyDatabaseSerializerManager.Serialize(value, stringTableView);
        }

        internal object GetObjectFromRecordValue(in PropertyDatabaseRecordValue recordValue)
        {
            if (!valid)
                return null;
            using (var view = m_StringTable.GetView())
                return GetObjectFromRecordValue(recordValue, view);
        }

        public TValue GetValueFromRecord<TValue>(IPropertyDatabaseRecordValue recordValue)
        {
            if (!valid)
                return default;
            using (var view = m_StringTable.GetView())
                return GetValueFromRecord<TValue>(recordValue, view);
        }

        internal object GetObjectFromRecordValue(in PropertyDatabaseRecordValue recordValue, PropertyStringTableView stringTableView)
        {
            if (!IsSupportedPropertyType(recordValue.propertyType))
                throw new ArgumentException($"Property type \"{recordValue.propertyType}\" of {nameof(recordValue)} is not supported.");

            return PropertyDatabaseSerializerManager.Deserialize(recordValue, stringTableView);
        }

        internal object GetObjectFromRecordValue(IPropertyDatabaseRecordValue recordValue)
        {
            if (!valid)
                return null;
            using (var view = m_StringTable.GetView())
                return GetObjectFromRecordValue(recordValue, view);
        }

        internal object GetObjectFromRecordValue(IPropertyDatabaseRecordValue recordValue, PropertyStringTableView stringTableView)
        {
            if (recordValue == null)
                return null;

            if (recordValue.type != PropertyDatabaseType.Volatile)
                return GetObjectFromRecordValue((PropertyDatabaseRecordValue)recordValue, stringTableView);

            var volatileRecordValue = (PropertyDatabaseVolatileRecordValue)recordValue;
            return volatileRecordValue.value;
        }

        internal TValue GetValueFromRecord<TValue>(IPropertyDatabaseRecordValue recordValue, PropertyStringTableView stringTableView)
        {
            if (recordValue == null)
                return default;

            if (recordValue.type != PropertyDatabaseType.Volatile)
                return (TValue)GetObjectFromRecordValue((PropertyDatabaseRecordValue)recordValue, stringTableView);

            var volatileRecordValue = (PropertyDatabaseVolatileRecordValue)recordValue;
            return (TValue)volatileRecordValue.value;
        }

        public static bool IsPersistableType(Type type)
        {
            return PropertyDatabaseSerializerManager.SerializerExists(type);
        }

        internal static bool IsSupportedPropertyType(byte propertyType)
        {
            return PropertyDatabaseSerializerManager.DeserializerExists((PropertyDatabaseType)propertyType);
        }

        public IPropertyDatabaseView GetView(bool delayedSync = false)
        {
            {
                if (valid)
                    return new PropertyDatabaseView(this, m_LocalVolatileStore, m_LocalStore, m_FileStore, m_StringTable, m_PropertyDatabaseLock.Share(), delayedSync);

                if (!disposed)
                    return new PropertyDatabaseErrorView($"The property database \"{filePath}\" is already opened.");
                return disposedView;
            }
        }

        public void Flush()
        {
            if (!valid)
                return;
            var task = TriggerBackgroundUpdate();
            task.Wait();
        }

        internal Task TriggerBackgroundUpdate()
        {
            var task = Task.Run(() =>
            {
                try
                {
                    MergeStoresToFile();
                }
                catch (ThreadAbortException)
                {
                    // Rethrow but do not log.
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    throw;
                }
            });
            return task;
        }

        internal void StoresChanged()
        {
            if (!autoFlush)
                return;

            lock (this)
            {
                m_Debounce?.Execute();
            }
        }

        public string GetInfo()
        {
            if (!valid)
                return string.Empty;
            var sb = new StringBuilder();
            sb.AppendLine($"Property Database: \"{filePath}\"");
            GetStoreInfo(m_LocalVolatileStore, "Volatile Store", sb);
            GetStoreInfo(m_LocalStore, "Non-Serialized Store", sb);
            GetStoreInfo(m_FileStore, "Serialized Store (on disk)", sb);
            GetStringTableInfo(m_StringTable, sb);
            return sb.ToString();
        }

        static void GetStoreInfo(IPropertyDatabaseStore store, string storeTitle, StringBuilder sb)
        {
            using (var view = store.GetView())
            {
                sb.AppendLine($"\t{storeTitle}:");
                sb.AppendLine($"\t\tCount: {view.length}");
                sb.AppendLine($"\t\tSize: {Utils.FormatBytes(view.byteSize)}");
            }
        }

        static void GetStringTableInfo(PropertyStringTable st, StringBuilder sb)
        {
            var longestStringsCount = 5;
            using (var view = st.GetView())
            {
                sb.AppendLine($"\tString Table: {st.filePath}");
                sb.AppendLine($"\t\tVersion: {view.version}");
                sb.AppendLine($"\t\tCount: {view.count}");
                sb.AppendLine($"\t\tSymbols slots: {view.symbolSlots}");
                sb.AppendLine($"\t\tAllocated bytes for strings: {Utils.FormatBytes(view.allocatedStringBytes)}");
                sb.AppendLine($"\t\tUsed bytes for strings: {Utils.FormatBytes(view.usedStringBytes)}");
                sb.AppendLine($"\t\tAverage byte size for strings: {Utils.FormatBytes(view.GetAverageBytesPerString())}");
                sb.AppendLine($"\t\tFile size: {Utils.FormatBytes(view.fileSize)}");
                sb.AppendLine($"\t\tTop {longestStringsCount} longest strings:");

                var strings = view.GetAllStrings();
                foreach (var str in strings.OrderByDescending(s => s.Length).Take(longestStringsCount))
                {
                    sb.AppendLine($"\t\t\t{str}");
                }
            }
        }

        void MergeStoresToFile()
        {
            if (!valid)
                return;

            // TODO: When trackers are removed, put back GetView()
            using (var view = new PropertyDatabaseView(this, m_LocalVolatileStore, m_LocalStore, m_FileStore, m_StringTable, m_PropertyDatabaseLock.Share(), false))
                view.MergeStores();
        }

        static string GetStringTablePath(string propertyDatabasePath)
        {
            return propertyDatabasePath + ".st";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool _)
        {
            lock (this)
            {
                if (disposed)
                    return;

                m_Debounce?.Dispose();
                m_Debounce = null;

                m_PropertyDatabaseLock?.Dispose();
                m_PropertyDatabaseLock = null;
                disposed = true;
            }
        }

        ~PropertyDatabase()
        {
            Dispose(false);
        }
    }

    struct PropertyDatabaseView : IPropertyDatabaseView
    {
        bool m_Disposed;
        bool m_DelayedSync;

        PropertyDatabase m_PropertyDatabase;
        PropertyDatabaseMemoryStore m_MemoryStore;
        PropertyDatabaseFileStore m_FileStore;
        PropertyDatabaseLock m_Lock;

        PropertyDatabaseVolatileMemoryStoreView m_VolatileMemoryStoreView;
        PropertyDatabaseMemoryStoreView m_MemoryStoreView;
        PropertyDatabaseFileStoreView m_FileStoreView;
        PropertyStringTableView m_StringTableView;

        // Internal for debugging
        internal PropertyDatabaseVolatileMemoryStoreView volatileMemoryStoreView => m_VolatileMemoryStoreView;
        internal PropertyDatabaseMemoryStoreView memoryStoreView => m_MemoryStoreView;
        internal PropertyDatabaseFileStoreView fileStoreView => m_FileStoreView;
        internal PropertyStringTableView stringTableView => m_StringTableView;
        internal PropertyDatabase database => m_PropertyDatabase;

        internal PropertyDatabaseView(PropertyDatabase propertyDatabase, PropertyDatabaseVolatileMemoryStore volatileMemoryStore, PropertyDatabaseMemoryStore memoryStore, PropertyDatabaseFileStore fileStore, PropertyStringTable stringTable, PropertyDatabaseLock propertyDatabaseLock, bool delayedSync)
        {
            m_PropertyDatabase = propertyDatabase;
            m_MemoryStore = memoryStore;
            m_FileStore = fileStore;
            m_VolatileMemoryStoreView = (PropertyDatabaseVolatileMemoryStoreView)volatileMemoryStore.GetView();
            m_MemoryStoreView = (PropertyDatabaseMemoryStoreView)memoryStore.GetView();
            m_FileStoreView = (PropertyDatabaseFileStoreView)fileStore.GetView();
            m_StringTableView = stringTable.GetView(delayedSync);
            m_Disposed = false;
            m_DelayedSync = delayedSync;
            m_Lock = propertyDatabaseLock;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            if (m_DelayedSync)
                Sync();

            m_PropertyDatabase = null;
            m_MemoryStore = null;
            m_FileStore = null;
            m_VolatileMemoryStoreView.Dispose();
            m_MemoryStoreView.Dispose();
            m_FileStoreView.Dispose();
            m_StringTableView.Dispose();
            m_Lock.Dispose();
            m_DelayedSync = false;
            m_Disposed = true;
        }

        public bool Store(string documentId, string propertyPath, object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.Store(documentId, propertyPath, value);
            var recordKey = CreateRecordKey(documentId, propertyPath);
            return Store(recordKey, value);
        }

        public bool Store(ulong documentKey, Hash128 propertyHash, object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.Store(documentKey, propertyHash, value);
            var recordKey = CreateRecordKey(documentKey, propertyHash);
            return Store(recordKey, value);
        }

        public bool Store(Hash128 propertyHash, object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.Store(propertyHash, value);
            var recordKey = CreateRecordKey(propertyHash);
            return Store(recordKey, value);
        }

        public bool Store(in PropertyDatabaseRecordKey recordKey, object value)
        {
            {
                if (m_PropertyDatabase.disposed)
                    return m_PropertyDatabase.disposedView.Store(recordKey, value);
                if (!IsPersistableType(value.GetType()))
                    return m_VolatileMemoryStoreView.Store(recordKey, value, !m_DelayedSync);
                var record = m_PropertyDatabase.CreateRecord(recordKey, value, m_StringTableView);
                return Store(record);
            }
        }

        public bool Store(in PropertyDatabaseRecordKey recordKey, in PropertyDatabaseRecordValue value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.Store(recordKey, value);
            var record = CreateRecord(recordKey, value);
            return Store(record);
        }

        public bool Store(in PropertyDatabaseRecord record)
        {
            {
                if (m_PropertyDatabase.disposed)
                    return m_PropertyDatabase.disposedView.Store(record);
                if (!record.recordValue.valid)
                    return false;
                var success = m_MemoryStoreView.Store(record, !m_DelayedSync);
                if (success)
                {
                    m_PropertyDatabase.StoresChanged();
                }
                return success;
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data)
        {
            {
                data = null;
                if (m_PropertyDatabase.disposed)
                    return m_PropertyDatabase.disposedView.TryLoad(recordKey, out data);
                if (!TryLoad(recordKey, out PropertyDatabaseRecordValue recordValue))
                    return m_VolatileMemoryStoreView.TryLoad(recordKey, out data);
                data = GetObjectFromRecordValue(recordValue);
                return true;
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data)
        {
            {
                if (m_PropertyDatabase.disposed)
                    return m_PropertyDatabase.disposedView.TryLoad(recordKey, out data);
                if (m_MemoryStoreView.TryLoad(recordKey, out data))
                    return true;
                if (!m_FileStoreView.TryLoad(recordKey, out data))
                    return false;

                // Cache loaded value into memory store.
                var record = CreateRecord(recordKey, data);
                m_MemoryStoreView.Store(record, !m_DelayedSync);
                return true;
            }
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data)
        {
            {
                if (m_PropertyDatabase.disposed)
                    return m_PropertyDatabase.disposedView.TryLoad(recordKey, out data);
                if (m_VolatileMemoryStoreView.TryLoad(recordKey, out data))
                    return true;

                var success = TryLoad(recordKey, out PropertyDatabaseRecordValue recordValue);
                data = recordValue;
                return success;
            }

        }

        public bool TryLoad(ulong documentKey, out IEnumerable<object> data)
        {
            {
                data = null;
                if (m_PropertyDatabase.disposed)
                    return m_PropertyDatabase.disposedView.TryLoad(documentKey, out data);
                if (!TryLoad(documentKey, out IEnumerable<IPropertyDatabaseRecord> records))
                    return false;

                var results = new List<object>();
                foreach (var propertyDatabaseRecord in records)
                {
                    results.Add(GetObjectFromRecordValue(propertyDatabaseRecord.value));
                }

                data = results;
                return true;
            }
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records)
        {
            {
                records = null;
                if (m_PropertyDatabase.disposed)
                    return m_PropertyDatabase.disposedView.TryLoad(documentKey, out records);
                SortedSet<IPropertyDatabaseRecord> allRecords = new SortedSet<IPropertyDatabaseRecord>(new IPropertyDatabaseRecordComparer());
                if (m_VolatileMemoryStoreView.TryLoad(documentKey, out var volatileRecords))
                    allRecords.UnionWith(volatileRecords);
                if (m_MemoryStoreView.TryLoad(documentKey, out var memoryRecords))
                    allRecords.UnionWith(memoryRecords);
                if (m_FileStoreView.TryLoad(documentKey, out var fileRecords))
                    allRecords.UnionWith(fileRecords);

                if (allRecords.Count == 0)
                    return false;
                records = allRecords;
                return true;
            }

        }

        public void Invalidate(string documentId)
        {
            if (m_PropertyDatabase.disposed)
            {
                m_PropertyDatabase.disposedView.Invalidate(documentId);
                return;
            }
            var documentKey = PropertyDatabase.CreateDocumentKey(documentId);
            Invalidate(documentKey);
        }

        public void Invalidate(ulong documentKey)
        {
            {
                if (m_PropertyDatabase.disposed)
                {
                    m_PropertyDatabase.disposedView.Invalidate(documentKey);
                    return;
                }
                m_VolatileMemoryStoreView.Invalidate(documentKey, !m_DelayedSync);
                m_MemoryStoreView.Invalidate(documentKey, !m_DelayedSync);
                m_FileStoreView.Invalidate(documentKey, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        public void Invalidate(in PropertyDatabaseRecordKey recordKey)
        {
            {
                if (m_PropertyDatabase.disposed)
                {
                    m_PropertyDatabase.disposedView.Invalidate(recordKey);
                    return;
                }
                m_VolatileMemoryStoreView.Invalidate(recordKey, !m_DelayedSync);
                m_MemoryStoreView.Invalidate(recordKey, !m_DelayedSync);
                m_FileStoreView.Invalidate(recordKey, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        internal void Invalidate(uint documentKeyHiWord)
        {
            {
                if (m_PropertyDatabase.disposed)
                {
                    m_PropertyDatabase.disposedView.Invalidate(documentKeyHiWord);
                    return;
                }
                m_VolatileMemoryStoreView.Invalidate(documentKeyHiWord, !m_DelayedSync);
                m_MemoryStoreView.Invalidate(documentKeyHiWord, !m_DelayedSync);
                m_FileStoreView.Invalidate(documentKeyHiWord, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        public void InvalidateMask(ulong documentKeyMask)
        {
            {
                if (m_PropertyDatabase.disposed)
                {
                    m_PropertyDatabase.disposedView.InvalidateMask(documentKeyMask);
                    return;
                }
                m_VolatileMemoryStoreView.InvalidateMask(documentKeyMask, !m_DelayedSync);
                m_MemoryStoreView.InvalidateMask(documentKeyMask, !m_DelayedSync);
                m_FileStoreView.InvalidateMask(documentKeyMask, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        internal void InvalidateMask(uint documentKeyHiWordMask)
        {
            {
                if (m_PropertyDatabase.disposed)
                {
                    m_PropertyDatabase.disposedView.InvalidateMask(documentKeyHiWordMask);
                    return;
                }
                m_VolatileMemoryStoreView.InvalidateMask(documentKeyHiWordMask, !m_DelayedSync);
                m_MemoryStoreView.InvalidateMask(documentKeyHiWordMask, !m_DelayedSync);
                m_FileStoreView.InvalidateMask(documentKeyHiWordMask, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll()
        {
            {
                if (m_PropertyDatabase.disposed)
                    return m_PropertyDatabase.disposedView.EnumerateAll();
                SortedSet<IPropertyDatabaseRecord> allRecords = new SortedSet<IPropertyDatabaseRecord>(new IPropertyDatabaseRecordComparer());
                allRecords.UnionWith(m_VolatileMemoryStoreView.EnumerateAll());
                allRecords.UnionWith(m_MemoryStoreView.EnumerateAll());
                allRecords.UnionWith(m_FileStoreView.EnumerateAll());
                return allRecords;
            }
        }

        public void Sync()
        {
            if (m_PropertyDatabase.disposed)
            {
                m_PropertyDatabase.disposedView.Sync();
                return;
            }
            m_VolatileMemoryStoreView.Sync();
            m_MemoryStoreView.Sync();
            m_FileStoreView.Sync();
            m_StringTableView.Sync();
        }

        public void Clear()
        {
            if (m_PropertyDatabase.disposed)
            {
                m_PropertyDatabase.disposedView.Clear();
                return;
            }
            m_VolatileMemoryStoreView.Clear();
            m_MemoryStoreView.Clear();
            m_FileStoreView.Clear();
            m_StringTableView.Clear();
        }

        public PropertyDatabaseRecord CreateRecord(string documentId, string propertyPath, object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecord(documentId, propertyPath, value);
            return m_PropertyDatabase.CreateRecord(documentId, propertyPath, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(ulong documentKey, Hash128 propertyPathHash, object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecord(documentKey, propertyPathHash, value);
            return m_PropertyDatabase.CreateRecord(documentKey, propertyPathHash, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(string propertyPath, object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecord(propertyPath, value);
            return m_PropertyDatabase.CreateRecord(propertyPath, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(Hash128 propertyPathHash, object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecord(propertyPathHash, value);
            return m_PropertyDatabase.CreateRecord(propertyPathHash, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(in PropertyDatabaseRecordKey recordKey, object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecord(recordKey, value);
            return m_PropertyDatabase.CreateRecord(recordKey, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(in PropertyDatabaseRecordKey recordKey, in PropertyDatabaseRecordValue recordValue)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecord(recordKey, recordValue);
            return PropertyDatabase.CreateRecord(recordKey, recordValue);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string documentId, string propertyPath)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecordKey(documentId, propertyPath);
            return PropertyDatabase.CreateRecordKey(documentId, propertyPath);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(ulong documentKey, Hash128 propertyPathHash)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecordKey(documentKey, propertyPathHash);
            return PropertyDatabase.CreateRecordKey(documentKey, propertyPathHash);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string propertyPath)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecordKey(propertyPath);
            return PropertyDatabase.CreateRecordKey(propertyPath);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string documentId, Hash128 propertyHash)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecordKey(documentId, propertyHash);
            return PropertyDatabase.CreateRecordKey(documentId, propertyHash);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(Hash128 propertyHash)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecordKey(propertyHash);
            return PropertyDatabase.CreateRecordKey(propertyHash);
        }

        public ulong CreateDocumentKey(string documentId)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateDocumentKey(documentId);
            return PropertyDatabase.CreateDocumentKey(documentId);
        }

        public Hash128 CreatePropertyHash(string propertyPath)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreatePropertyHash(propertyPath);
            return PropertyDatabase.CreatePropertyHash(propertyPath);
        }

        public PropertyDatabaseRecordValue CreateRecordValue(object value)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.CreateRecordValue(value);
            return m_PropertyDatabase.CreateRecordValue(value, m_StringTableView);
        }

        public object GetObjectFromRecordValue(in PropertyDatabaseRecordValue recordValue)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.GetObjectFromRecordValue(recordValue);
            return m_PropertyDatabase.GetObjectFromRecordValue(recordValue, m_StringTableView);
        }

        public object GetObjectFromRecordValue(IPropertyDatabaseRecordValue recordValue)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.GetObjectFromRecordValue(recordValue);
            return m_PropertyDatabase.GetObjectFromRecordValue(recordValue, m_StringTableView);
        }

        public TValue GetValueFromRecord<TValue>(IPropertyDatabaseRecordValue recordValue)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.GetValueFromRecord<TValue>(recordValue);
            return m_PropertyDatabase.GetValueFromRecord<TValue>(recordValue, m_StringTableView);
        }

        public bool IsPersistableType(Type type)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.IsPersistableType(type);
            return PropertyDatabase.IsPersistableType(type);
        }

        public bool IsSupportedPropertyType(byte propertyType)
        {
            if (m_PropertyDatabase.disposed)
                return m_PropertyDatabase.disposedView.IsSupportedPropertyType(propertyType);
            return PropertyDatabase.IsSupportedPropertyType(propertyType);
        }

        internal void MergeStores()
        {
            if (m_PropertyDatabase.disposed)
            {
                m_PropertyDatabase.disposedView.MergeStores();
                return;
            }
            var newMemoryStore = new PropertyDatabaseMemoryStore();
            using (m_MemoryStore.LockUpgradeableRead())
            using (m_FileStore.LockUpgradeableRead())
            using (var newMemoryStoreView = (PropertyDatabaseMemoryStoreView)newMemoryStore.GetView())
            using (new RaceConditionDetector(m_PropertyDatabase))
            {
                // Merge both stores
                newMemoryStoreView.MergeWith(m_FileStoreView);
                newMemoryStoreView.MergeWith(m_MemoryStoreView);

                // Write new memory store to file.
                var tempFilePath = GetTempFilePath(m_FileStore.filePath);
                newMemoryStoreView.SaveToFile(tempFilePath);

                // Swap file store with new one
                m_FileStore.SwapFile(tempFilePath);

                // Clear the memory store after file was swapped. If you do it before, you risk
                // entering a state where another thread could try to read between the moment the clear is done
                // and the new file is written and opened.
                m_MemoryStoreView.Clear();
            }
        }

        static string GetTempFilePath(string baseFilePath)
        {
            return $"{baseFilePath}_{Guid.NewGuid().ToString("N")}_temp";
        }
    }

    struct PropertyDatabaseErrorView : IPropertyDatabaseView
    {
        string m_Message;
        LogType m_LogType;

        public PropertyDatabaseErrorView(string message, LogType type = LogType.Error)
        {
            m_Message = message;
            m_LogType = type;
        }

        void LogMessage()
        {
            Debug.LogFormat(m_LogType, LogOption.None, null, m_Message);
        }

        public void Dispose()
        {}

        public bool Store(string documentId, string propertyPath, object value)
        {
            LogMessage();
            return false;
        }

        public bool Store(ulong documentKey, Hash128 propertyHash, object value)
        {
            LogMessage();
            return false;
        }

        public bool Store(Hash128 propertyHash, object value)
        {
            LogMessage();
            return false;
        }

        public bool Store(in PropertyDatabaseRecordKey recordKey, object value)
        {
            LogMessage();
            return false;
        }

        public bool Store(in PropertyDatabaseRecord record)
        {
            LogMessage();
            return false;
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out object data)
        {
            data = null;
            LogMessage();
            return false;
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data)
        {
            data = PropertyDatabaseRecordValue.invalid;
            LogMessage();
            return false;
        }

        public bool TryLoad(in PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data)
        {
            data = null;
            LogMessage();
            return false;
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<object> data)
        {
            data = null;
            LogMessage();
            return false;
        }

        public bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records)
        {
            records = null;
            LogMessage();
            return false;
        }

        public void Invalidate(string documentId)
        {
            LogMessage();
        }

        public void Invalidate(ulong documentKey)
        {
            LogMessage();
        }

        public void Invalidate(in PropertyDatabaseRecordKey recordKey)
        {
            LogMessage();
        }

        public void InvalidateMask(ulong documentKeyMask)
        {
            LogMessage();
        }

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll()
        {
            LogMessage();
            return null;
        }

        public void Sync()
        {
            LogMessage();
        }

        public void Clear()
        {
            LogMessage();
        }

        public PropertyDatabaseRecord CreateRecord(string documentId, string propertyPath, object value)
        {
            LogMessage();
            return PropertyDatabaseRecord.invalid;
        }

        public PropertyDatabaseRecord CreateRecord(ulong documentKey, Hash128 propertyPathHash, object value)
        {
            LogMessage();
            return PropertyDatabaseRecord.invalid;
        }

        public PropertyDatabaseRecord CreateRecord(string propertyPath, object value)
        {
            LogMessage();
            return PropertyDatabaseRecord.invalid;
        }

        public PropertyDatabaseRecord CreateRecord(Hash128 propertyPathHash, object value)
        {
            LogMessage();
            return PropertyDatabaseRecord.invalid;
        }

        public PropertyDatabaseRecord CreateRecord(in PropertyDatabaseRecordKey recordKey, object value)
        {
            LogMessage();
            return PropertyDatabaseRecord.invalid;
        }

        public PropertyDatabaseRecord CreateRecord(in PropertyDatabaseRecordKey recordKey, in PropertyDatabaseRecordValue recordValue)
        {
            return PropertyDatabase.CreateRecord(recordKey, recordValue);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string documentId, string propertyPath)
        {
            LogMessage();
            return PropertyDatabase.CreateRecordKey(documentId, propertyPath);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(ulong documentKey, Hash128 propertyPathHash)
        {
            LogMessage();
            return PropertyDatabase.CreateRecordKey(documentKey, propertyPathHash);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string propertyPath)
        {
            LogMessage();
            return PropertyDatabase.CreateRecordKey(propertyPath);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string documentId, Hash128 propertyHash)
        {
            LogMessage();
            return PropertyDatabase.CreateRecordKey(documentId, propertyHash);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(Hash128 propertyHash)
        {
            LogMessage();
            return PropertyDatabase.CreateRecordKey(propertyHash);
        }

        public ulong CreateDocumentKey(string documentId)
        {
            LogMessage();
            return PropertyDatabase.CreateDocumentKey(documentId);
        }

        public Hash128 CreatePropertyHash(string propertyPath)
        {
            LogMessage();
            return PropertyDatabase.CreatePropertyHash(propertyPath);
        }

        public PropertyDatabaseRecordValue CreateRecordValue(object value)
        {
            LogMessage();
            return PropertyDatabaseRecordValue.invalid;
        }

        public object GetObjectFromRecordValue(in PropertyDatabaseRecordValue recordValue)
        {
            LogMessage();
            return null;
        }

        public object GetObjectFromRecordValue(IPropertyDatabaseRecordValue recordValue)
        {
            LogMessage();
            return null;
        }

        public TValue GetValueFromRecord<TValue>(IPropertyDatabaseRecordValue recordValue)
        {
            LogMessage();
            return default;
        }

        public bool IsPersistableType(Type type)
        {
            LogMessage();
            return PropertyDatabase.IsPersistableType(type);
        }

        public bool IsSupportedPropertyType(byte propertyType)
        {
            LogMessage();
            return PropertyDatabase.IsSupportedPropertyType(propertyType);
        }

        internal void MergeStores()
        {
            LogMessage();
        }
    }
}
