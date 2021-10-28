// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define USE_PERFORMANCE_TRACKER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor.Search
{
    interface IPropertyDatabaseView : IDisposable
    {
        bool Store(PropertyDatabaseRecord record);
        bool TryLoad(PropertyDatabaseRecordKey recordKey, out object data);
        bool TryLoad(PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data);
        bool TryLoad(PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data);
        bool TryLoad(ulong documentKey, out IEnumerable<object> data);
        bool TryLoad(ulong documentKey, out IEnumerable<IPropertyDatabaseRecord> records);
        void Invalidate(string documentId);
        void Invalidate(ulong documentKey);
        void Invalidate(PropertyDatabaseRecordKey recordKey);
        void Invalidate(uint documentKeyHiWord);
        void InvalidateMask(ulong documentKeyMask);
        void InvalidateMask(uint documentKeyHiWordMask);
        IEnumerable<IPropertyDatabaseRecord> EnumerateAll();
        void Sync();
        void Clear();
    }

    class PropertyDatabase
    {
        PropertyDatabaseVolatileMemoryStore m_LocalVolatileStore;
        PropertyDatabaseMemoryStore m_LocalStore;
        PropertyDatabaseFileStore m_FileStore;
        PropertyStringTable m_StringTable;

        public static readonly int version = ((0x50 << 24) | (0x44 << 16) | 0x0006) ^ PropertyStringTable.Version ^ ((InternalEditorUtility.GetUnityVersion().Major & 0xffff) << 16 | InternalEditorUtility.GetUnityVersion().Minor);

        public string filePath { get; }
        public string stringTableFilePath { get; }
        public bool autoBackgroundUpdate { get; }

        const double k_DefaultBackgroundUpdateDebounceInSeconds = 5.0;
        Delayer m_Debounce;

        public PropertyDatabase(string filePath)
            : this(filePath, false)
        {}

        public PropertyDatabase(string filePath, bool autoBackgroundUpdate, double backgroundUpdateDebounceInSeconds = k_DefaultBackgroundUpdateDebounceInSeconds)
        {
            this.filePath = filePath;
            stringTableFilePath = GetStringTablePath(filePath);
            m_LocalVolatileStore = new PropertyDatabaseVolatileMemoryStore();
            m_LocalStore = new PropertyDatabaseMemoryStore();
            m_FileStore = new PropertyDatabaseFileStore(filePath);
            m_StringTable = new PropertyStringTable(stringTableFilePath, 30);

            // Do not allow automatic background updates while running tests. The writing of the file
            // causes an assembly leak during the test Unity.IntegrationTests.Scripting.AssemblyReloadTest.AssemblyReloadDoesntLeakAssemblies
            // on MacOs. I haven't found out why exactly does the writing of a file causes an assembly to be held, so instead I deactivate
            // the automatic update during tests.
            this.autoBackgroundUpdate = autoBackgroundUpdate && !Utils.IsRunningTests();

            m_Debounce = Delayer.Debounce(_ => TriggerPropertyDatabaseBackgroundUpdate(), backgroundUpdateDebounceInSeconds);
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

        public bool Store(PropertyDatabaseRecordKey recordKey, object value)
        {
            using (var view = GetView())
                return view.Store(recordKey, value);
        }

        public bool Store(PropertyDatabaseRecordKey recordKey, PropertyDatabaseRecordValue value)
        {
            using (var view = GetView())
                return view.Store(recordKey, value);
        }

        public bool Store(PropertyDatabaseRecord record)
        {
            using (var view = GetView())
                return view.Store(record);
        }

        public bool TryLoad(PropertyDatabaseRecordKey recordKey, out object value)
        {
            using (var view = GetView())
                return view.TryLoad(recordKey, out value);
        }

        public bool TryLoad(PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue value)
        {
            using (var view = GetView())
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

        public void Invalidate(PropertyDatabaseRecordKey recordKey)
        {
            using (var view = GetView())
                view.Invalidate(recordKey);
        }

        public void Invalidate(uint documentKeyHiWord)
        {
            using (var view = GetView())
                view.Invalidate(documentKeyHiWord);
        }

        public void InvalidateMask(ulong documentKeyMask)
        {
            using (var view = GetView())
                view.InvalidateMask(documentKeyMask);
        }

        public void InvalidateMask(uint documentKeyHiWordMask)
        {
            using (var view = GetView())
                view.InvalidateMask(documentKeyHiWordMask);
        }

        public void Clear()
        {
            using (var view = GetView())
                view.Clear();
        }

        public PropertyDatabaseRecord CreateRecord(string documentId, string propertyPath, object value)
        {
            return CreateRecord(CreateDocumentKey(documentId), CreatePropertyHash(propertyPath), value);
        }

        public PropertyDatabaseRecord CreateRecord(string documentId, string propertyPath, object value, PropertyStringTableView view)
        {
            return CreateRecord(CreateDocumentKey(documentId), CreatePropertyHash(propertyPath), value, view);
        }

        public PropertyDatabaseRecord CreateRecord(ulong documentKey, Hash128 propertyPathHash, object value)
        {
            var key = CreateRecordKey(documentKey, propertyPathHash);
            return CreateRecord(key, value);
        }

        public PropertyDatabaseRecord CreateRecord(ulong documentKey, Hash128 propertyPathHash, object value, PropertyStringTableView view)
        {
            var key = CreateRecordKey(documentKey, propertyPathHash);
            return CreateRecord(key, value, view);
        }

        public PropertyDatabaseRecord CreateRecord(string propertyPath, object value)
        {
            return CreateRecord(CreatePropertyHash(propertyPath), value);
        }

        public PropertyDatabaseRecord CreateRecord(string propertyPath, object value, PropertyStringTableView view)
        {
            return CreateRecord(CreatePropertyHash(propertyPath), value, view);
        }

        public PropertyDatabaseRecord CreateRecord(Hash128 propertyPathHash, object value)
        {
            return CreateRecord(0, propertyPathHash, value);
        }

        public PropertyDatabaseRecord CreateRecord(Hash128 propertyPathHash, object value, PropertyStringTableView view)
        {
            return CreateRecord(0, propertyPathHash, value, view);
        }

        public PropertyDatabaseRecord CreateRecord(PropertyDatabaseRecordKey recordKey, object value)
        {
            var recordValue = CreateRecordValue(value);
            return CreateRecord(recordKey, recordValue);
        }

        public PropertyDatabaseRecord CreateRecord(PropertyDatabaseRecordKey recordKey, object value, PropertyStringTableView view)
        {
            var recordValue = CreateRecordValue(value, view);
            return CreateRecord(recordKey, recordValue);
        }

        public static PropertyDatabaseRecord CreateRecord(PropertyDatabaseRecordKey recordKey, PropertyDatabaseRecordValue recordValue)
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
            return string.IsNullOrEmpty(documentId) ? 0UL : (ulong)documentId.GetHashCode();
        }

        public PropertyDatabaseRecordValue CreateRecordValue(object value)
        {
            using (var view = m_StringTable.GetView())
                return CreateRecordValue(value, view);
        }

        public PropertyDatabaseRecordValue CreateRecordValue(object value, PropertyStringTableView stringTableView)
        {
            if (!IsSupportedValue(value))
                throw new ArgumentException($"Type \"{value.GetType()}\" is not supported.");

            return PropertyDatabaseSerializerManager.Serialize(value, stringTableView);
        }

        public object GetObjectFromRecordValue(PropertyDatabaseRecordValue recordValue)
        {
            using (var view = m_StringTable.GetView())
                return GetObjectFromRecordValue(recordValue, view);
        }

        public object GetObjectFromRecordValue(PropertyDatabaseRecordValue recordValue, PropertyStringTableView stringTableView)
        {
            if (!IsSupportedPropertyType(recordValue.propertyType))
                throw new ArgumentException($"Property type \"{recordValue.propertyType}\" of {nameof(recordValue)} is not supported.");

            return PropertyDatabaseSerializerManager.Deserialize(recordValue, stringTableView);
        }

        public object GetObjectFromRecordValue(IPropertyDatabaseRecordValue recordValue)
        {
            using (var view = m_StringTable.GetView())
                return GetObjectFromRecordValue(recordValue, view);
        }

        public object GetObjectFromRecordValue(IPropertyDatabaseRecordValue recordValue, PropertyStringTableView stringTableView)
        {
            if (recordValue.type != PropertyDatabaseType.Volatile)
                return GetObjectFromRecordValue((PropertyDatabaseRecordValue)recordValue, stringTableView);

            var volatileRecordValue = (PropertyDatabaseVolatileRecordValue)recordValue;
            return volatileRecordValue.value;
        }

        public static bool IsSupportedValue(object value)
        {
            var type = value.GetType();
            return PropertyDatabaseSerializerManager.SerializerExists(type);
        }

        public static bool IsSupportedPropertyType(byte propertyType)
        {
            return PropertyDatabaseSerializerManager.DeserializerExists((PropertyDatabaseType)propertyType);
        }

        public PropertyDatabaseView GetView(bool delayedSync = false)
        {
            {
                return new PropertyDatabaseView(this, m_LocalVolatileStore, m_LocalStore, m_FileStore, m_StringTable, delayedSync);
            }
        }

        public Task TriggerPropertyDatabaseBackgroundUpdate()
        {
            var task = Task.Run(() =>
            {
                try
                {
                    MergeStoresToFile();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    throw ex;
                }
            });
            return task;
        }

        public void StoresChanged()
        {
            if (!autoBackgroundUpdate)
                return;

            lock (this)
            {
                m_Debounce.Execute();
            }
        }

        public string GetInfo()
        {
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
            // TODO: When trackers are removed, put back GetView()
            using (var view = new PropertyDatabaseView(this, m_LocalVolatileStore, m_LocalStore, m_FileStore, m_StringTable, false))
                view.MergeStores();
        }

        static string GetStringTablePath(string propertyDatabasePath)
        {
            return propertyDatabasePath + ".st";
        }
    }

    struct PropertyDatabaseView : IPropertyDatabaseView
    {
        bool m_Disposed;
        bool m_DelayedSync;

        PropertyDatabase m_PropertyDatabase;
        PropertyDatabaseMemoryStore m_MemoryStore;
        PropertyDatabaseFileStore m_FileStore;

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

        public PropertyDatabaseView(PropertyDatabase propertyDatabase, PropertyDatabaseVolatileMemoryStore volatileMemoryStore, PropertyDatabaseMemoryStore memoryStore, PropertyDatabaseFileStore fileStore, PropertyStringTable stringTable, bool delayedSync)
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
            m_DelayedSync = false;
            m_Disposed = true;
        }

        public bool Store(string documentId, string propertyPath, object value)
        {
            var recordKey = CreateRecordKey(documentId, propertyPath);
            return Store(recordKey, value);
        }

        public bool Store(ulong documentKey, Hash128 propertyHash, object value)
        {
            var recordKey = CreateRecordKey(documentKey, propertyHash);
            return Store(recordKey, value);
        }

        public bool Store(Hash128 propertyHash, object value)
        {
            var recordKey = CreateRecordKey(propertyHash);
            return Store(recordKey, value);
        }

        public bool Store(PropertyDatabaseRecordKey recordKey, object value)
        {
            {
                if (!IsSupportedValue(value))
                    return m_VolatileMemoryStoreView.Store(recordKey, value, !m_DelayedSync);
                var record = m_PropertyDatabase.CreateRecord(recordKey, value, m_StringTableView);
                return Store(record);
            }
        }

        public bool Store(PropertyDatabaseRecordKey recordKey, PropertyDatabaseRecordValue value)
        {
            var record = CreateRecord(recordKey, value);
            return Store(record);
        }

        public bool Store(PropertyDatabaseRecord record)
        {
            {
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

        public bool TryLoad(PropertyDatabaseRecordKey recordKey, out object data)
        {
            {
                data = null;
                if (!TryLoad(recordKey, out PropertyDatabaseRecordValue recordValue))
                    return m_VolatileMemoryStoreView.TryLoad(recordKey, out data);
                data = GetObjectFromRecordValue(recordValue);
                return true;
            }
        }

        public bool TryLoad(PropertyDatabaseRecordKey recordKey, out PropertyDatabaseRecordValue data)
        {
            {
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

        public bool TryLoad(PropertyDatabaseRecordKey recordKey, out IPropertyDatabaseRecordValue data)
        {
            {
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
            var documentKey = PropertyDatabase.CreateDocumentKey(documentId);
            Invalidate(documentKey);
        }

        public void Invalidate(ulong documentKey)
        {
            {
                m_VolatileMemoryStoreView.Invalidate(documentKey, !m_DelayedSync);
                m_MemoryStoreView.Invalidate(documentKey, !m_DelayedSync);
                m_FileStoreView.Invalidate(documentKey, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        public void Invalidate(PropertyDatabaseRecordKey recordKey)
        {
            {
                m_VolatileMemoryStoreView.Invalidate(recordKey, !m_DelayedSync);
                m_MemoryStoreView.Invalidate(recordKey, !m_DelayedSync);
                m_FileStoreView.Invalidate(recordKey, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        public void Invalidate(uint documentKeyHiWord)
        {
            {
                m_VolatileMemoryStoreView.Invalidate(documentKeyHiWord, !m_DelayedSync);
                m_MemoryStoreView.Invalidate(documentKeyHiWord, !m_DelayedSync);
                m_FileStoreView.Invalidate(documentKeyHiWord, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        public void InvalidateMask(ulong documentKeyMask)
        {
            {
                m_VolatileMemoryStoreView.InvalidateMask(documentKeyMask, !m_DelayedSync);
                m_MemoryStoreView.InvalidateMask(documentKeyMask, !m_DelayedSync);
                m_FileStoreView.InvalidateMask(documentKeyMask, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        public void InvalidateMask(uint documentKeyHiWordMask)
        {
            {
                m_VolatileMemoryStoreView.InvalidateMask(documentKeyHiWordMask, !m_DelayedSync);
                m_MemoryStoreView.InvalidateMask(documentKeyHiWordMask, !m_DelayedSync);
                m_FileStoreView.InvalidateMask(documentKeyHiWordMask, !m_DelayedSync);
                m_PropertyDatabase.StoresChanged();
            }
        }

        public IEnumerable<IPropertyDatabaseRecord> EnumerateAll()
        {
            {
                SortedSet<IPropertyDatabaseRecord> allRecords = new SortedSet<IPropertyDatabaseRecord>(new IPropertyDatabaseRecordComparer());
                allRecords.UnionWith(m_VolatileMemoryStoreView.EnumerateAll());
                allRecords.UnionWith(m_MemoryStoreView.EnumerateAll());
                allRecords.UnionWith(m_FileStoreView.EnumerateAll());
                return allRecords;
            }
        }

        public void Sync()
        {
            m_VolatileMemoryStoreView.Sync();
            m_MemoryStoreView.Sync();
            m_FileStoreView.Sync();
            m_StringTableView.Sync();
        }

        public void Clear()
        {
            m_VolatileMemoryStoreView.Clear();
            m_MemoryStoreView.Clear();
            m_FileStoreView.Clear();
            m_StringTableView.Clear();
        }

        public PropertyDatabaseRecord CreateRecord(string documentId, string propertyPath, object value)
        {
            return m_PropertyDatabase.CreateRecord(documentId, propertyPath, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(ulong documentKey, Hash128 propertyPathHash, object value)
        {
            return m_PropertyDatabase.CreateRecord(documentKey, propertyPathHash, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(string propertyPath, object value)
        {
            return m_PropertyDatabase.CreateRecord(propertyPath, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(Hash128 propertyPathHash, object value)
        {
            return m_PropertyDatabase.CreateRecord(propertyPathHash, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(PropertyDatabaseRecordKey recordKey, object value)
        {
            return m_PropertyDatabase.CreateRecord(recordKey, value, m_StringTableView);
        }

        public PropertyDatabaseRecord CreateRecord(PropertyDatabaseRecordKey recordKey, PropertyDatabaseRecordValue recordValue)
        {
            return PropertyDatabase.CreateRecord(recordKey, recordValue);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string documentId, string propertyPath)
        {
            return PropertyDatabase.CreateRecordKey(documentId, propertyPath);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(ulong documentKey, Hash128 propertyPathHash)
        {
            return PropertyDatabase.CreateRecordKey(documentKey, propertyPathHash);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string propertyPath)
        {
            return PropertyDatabase.CreateRecordKey(propertyPath);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(string documentId, Hash128 propertyHash)
        {
            return PropertyDatabase.CreateRecordKey(documentId, propertyHash);
        }

        public PropertyDatabaseRecordKey CreateRecordKey(Hash128 propertyHash)
        {
            return PropertyDatabase.CreateRecordKey(propertyHash);
        }

        public ulong CreateDocumentKey(string documentId)
        {
            return PropertyDatabase.CreateDocumentKey(documentId);
        }

        public Hash128 CreatePropertyHash(string propertyPath)
        {
            return PropertyDatabase.CreatePropertyHash(propertyPath);
        }

        public PropertyDatabaseRecordValue CreateRecordValue(object value)
        {
            return m_PropertyDatabase.CreateRecordValue(value, m_StringTableView);
        }

        public object GetObjectFromRecordValue(PropertyDatabaseRecordValue recordValue)
        {
            return m_PropertyDatabase.GetObjectFromRecordValue(recordValue, m_StringTableView);
        }

        public object GetObjectFromRecordValue(IPropertyDatabaseRecordValue recordValue)
        {
            return m_PropertyDatabase.GetObjectFromRecordValue(recordValue, m_StringTableView);
        }

        public bool IsSupportedValue(object value)
        {
            return PropertyDatabase.IsSupportedValue(value);
        }

        public bool IsSupportedPropertyType(byte propertyType)
        {
            return PropertyDatabase.IsSupportedPropertyType(propertyType);
        }

        public void MergeStores()
        {
            var newMemoryStore = new PropertyDatabaseMemoryStore();
            using (m_MemoryStore.LockUpgradeableRead())
            using (m_FileStore.LockUpgradeableRead())
            using (var newMemoryStoreView = (PropertyDatabaseMemoryStoreView)newMemoryStore.GetView())
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
}
