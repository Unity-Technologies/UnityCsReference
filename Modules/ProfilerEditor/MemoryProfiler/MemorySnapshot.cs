// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEditorInternal.Profiling.Memory.Experimental.FileFormat;
using ExperimentalMemoryProfiler = UnityEngine.Profiling.Memory.Experimental.MemoryProfiler;

namespace UnityEditor.Profiling.Memory.Experimental
{
    // !!!!! NOTE: Keep in sync with Runtime\Profiler\MemorySnapshots.cpp
    public class PackedMemorySnapshot : IDisposable
    {
        static readonly UInt32 kMinSupportedVersion = 7;
        static readonly UInt32 kCurrentVersion = 8;

        public static PackedMemorySnapshot Load(string path)
        {
            MemorySnapshotFileReader reader = new MemorySnapshotFileReader(path);

            UInt32 ver = reader.GetDataSingle(EntryType.Metadata_Version, ConversionFunctions.ToUInt32);

            if (ver < kMinSupportedVersion)
            {
                throw new Exception(string.Format("Memory snapshot at {0}, is using an older format version: {1}", new object[] { reader.GetFilePath(), ver.ToString() }));
            }

            return new PackedMemorySnapshot(reader);
        }

        public static bool Convert(UnityEditor.MemoryProfiler.PackedMemorySnapshot snapshot, string writePath)
        {
            MemorySnapshotFileWriter writer = null;

            try
            {
                writer = new MemorySnapshotFileWriter(writePath);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to create snapshot file: " + e.Message);
                return false;
            }

            //snapshot version will always be the current one for convertion operations
            writer.WriteEntry(EntryType.Metadata_Version, kCurrentVersion);

            //timestamp with conversion date
            writer.WriteEntry(EntryType.Metadata_RecordDate, (ulong)DateTime.Now.Ticks);

            //prepare metadata
            string platform = "Unknown";
            string content = "Converted Snapshot";
            int stringDataLength = (platform.Length + content.Length) * sizeof(char);
            byte[] metaDataBytes = new byte[stringDataLength + (sizeof(int) * 3)]; // add space for serializing the lengths of the strings

            int offset = 0;
            offset = ExperimentalMemoryProfiler.WriteIntToByteArray(metaDataBytes, offset, content.Length);
            offset = ExperimentalMemoryProfiler.WriteStringToByteArray(metaDataBytes, offset, content);

            offset = ExperimentalMemoryProfiler.WriteIntToByteArray(metaDataBytes, offset, platform.Length);
            offset = ExperimentalMemoryProfiler.WriteStringToByteArray(metaDataBytes, offset, platform);

            offset = ExperimentalMemoryProfiler.WriteIntToByteArray(metaDataBytes, offset, 0);

            // Write metadata
            writer.WriteEntryArray(EntryType.Metadata_UserMetadata, metaDataBytes);

            writer.WriteEntry(EntryType.Metadata_CaptureFlags, (UInt32)CaptureFlags.ManagedObjects); //capture just managed

            // Write managed heap sections
            for (int i = 0; i < snapshot.managedHeapSections.Length; ++i)
            {
                var heapSection = snapshot.managedHeapSections[i];
                writer.WriteEntry(EntryType.ManagedHeapSections_StartAddress, heapSection.startAddress);
                writer.WriteEntryArray(EntryType.ManagedHeapSections_Bytes, heapSection.m_Bytes);
            }

            // Write managed types
            int fieldDescriptionCount = 0;
            for (int i = 0; i < snapshot.typeDescriptions.Length; ++i)
            {
                var type = snapshot.typeDescriptions[i];
                writer.WriteEntry(EntryType.TypeDescriptions_Flags, (UInt32)type.m_Flags);
                writer.WriteEntry(EntryType.TypeDescriptions_BaseOrElementTypeIndex, type.baseOrElementTypeIndex);
                writer.WriteEntry(EntryType.TypeDescriptions_TypeIndex, type.typeIndex);

                if (!type.isArray)
                {
                    var typeFields = type.fields;
                    int[] fieldIndices = new int[typeFields.Length];

                    for (int j = 0; j < typeFields.Length; ++j)
                    {
                        var field = typeFields[j];
                        fieldIndices[j] = fieldDescriptionCount;
                        ++fieldDescriptionCount;

                        writer.WriteEntry(EntryType.FieldDescriptions_Offset, field.offset);
                        writer.WriteEntry(EntryType.FieldDescriptions_TypeIndex, field.typeIndex);
                        writer.WriteEntry(EntryType.FieldDescriptions_Name, field.name);
                        writer.WriteEntry(EntryType.FieldDescriptions_IsStatic, field.isStatic);
                    }

                    writer.WriteEntryArray(EntryType.TypeDescriptions_FieldIndices, fieldIndices);
                    writer.WriteEntryArray(EntryType.TypeDescriptions_StaticFieldBytes, type.staticFieldBytes);
                }
                else
                {
                    writer.WriteEntryArray(EntryType.TypeDescriptions_FieldIndices, new int[0]);
                    writer.WriteEntryArray(EntryType.TypeDescriptions_StaticFieldBytes, new byte[0]);
                }

                writer.WriteEntry(EntryType.TypeDescriptions_Name, type.name);
                writer.WriteEntry(EntryType.TypeDescriptions_Assembly, type.assembly);
                writer.WriteEntry(EntryType.TypeDescriptions_TypeInfoAddress, type.typeInfoAddress);
                writer.WriteEntry(EntryType.TypeDescriptions_Size, type.size);
            }

            //write managed gc handles
            for (int i = 0; i < snapshot.gcHandles.Length; ++i)
            {
                var handle = snapshot.gcHandles[i];
                writer.WriteEntry(EntryType.GCHandles_Target, handle.target);
            }

            //write managed connections
            for (int i = 0; i < snapshot.connections.Length; ++i)
            {
                var connection = snapshot.connections[i];
                writer.WriteEntry(EntryType.Connections_From, connection.from);
                writer.WriteEntry(EntryType.Connections_To, connection.to);
            }

            //write native types
            for (int i = 0; i < snapshot.nativeTypes.Length; ++i)
            {
                var nativeType = snapshot.nativeTypes[i];
                writer.WriteEntry(EntryType.NativeTypes_NativeBaseTypeArrayIndex, nativeType.nativeBaseTypeArrayIndex);
                writer.WriteEntry(EntryType.NativeTypes_Name, nativeType.name);
            }

            //write stub root reference for all native objects to point to, as the old format did not capture these
            writer.WriteEntry(EntryType.NativeRootReferences_AreaName, "Invalid Root");
            writer.WriteEntry(EntryType.NativeRootReferences_AccumulatedSize, (ulong)0);
            writer.WriteEntry(EntryType.NativeRootReferences_Id, (long)0);
            writer.WriteEntry(EntryType.NativeRootReferences_ObjectName, "Invalid Root Object");

            //write native objects
            for (int i = 0; i < snapshot.nativeObjects.Length; ++i)
            {
                var nativeObject = snapshot.nativeObjects[i];
                writer.WriteEntry(EntryType.NativeObjects_Name, nativeObject.name);
                writer.WriteEntry(EntryType.NativeObjects_InstanceId, nativeObject.instanceId);
                writer.WriteEntry(EntryType.NativeObjects_Size, (ulong)nativeObject.size);
                writer.WriteEntry(EntryType.NativeObjects_NativeTypeArrayIndex, nativeObject.nativeTypeArrayIndex);
                writer.WriteEntry(EntryType.NativeObjects_HideFlags, (UInt32)nativeObject.hideFlags);
                writer.WriteEntry(EntryType.NativeObjects_Flags, (UInt32)nativeObject.m_Flags);
                writer.WriteEntry(EntryType.NativeObjects_NativeObjectAddress, (ulong)nativeObject.nativeObjectAddress);
                writer.WriteEntry(EntryType.NativeObjects_RootReferenceId, (long)0);
            }

            //write virtual machine information
            writer.WriteEntry(EntryType.Metadata_VirtualMachineInformation, snapshot.virtualMachineInformation);
            writer.Close();

            return true;
        }

        public static void Save(PackedMemorySnapshot snapshot, string writePath)
        {
            string path = snapshot.GetReader().GetFilePath();

            if (!System.IO.File.Exists(path))
            {
                throw new UnityException("Failed to save snapshot. Snapshot file: " + snapshot.filePath + " is missing.");
            }

            FileUtil.CopyFileIfExists(path, writePath, true);
        }

        MemorySnapshotFileReader m_Reader = null;

        public ConnectionEntries connections { get; internal set; }
        public FieldDescriptionEntries fieldDescriptions { get; internal set; }
        public GCHandleEntries gcHandles { get; internal set; }
        public ManagedMemorySectionEntries managedHeapSections { get; internal set; }
        public ManagedMemorySectionEntries managedStacks { get; internal set; }
        public NativeAllocationEntries nativeAllocations { get; internal set; }
        public NativeAllocationSiteEntries nativeAllocationSites { get; internal set; }
        public NativeCallstackSymbolEntries nativeCallstackSymbols { get; internal set; }
        public NativeMemoryLabelEntries nativeMemoryLabels { get; internal set; }
        public NativeMemoryRegionEntries nativeMemoryRegions { get; internal set; }
        public NativeObjectEntries nativeObjects { get; internal set; }
        public NativeRootReferenceEntries nativeRootReferences { get; internal set; }
        public NativeTypeEntries nativeTypes { get; internal set; }
        public TypeDescriptionEntries typeDescriptions { get; internal set; }

        internal PackedMemorySnapshot(MemorySnapshotFileReader reader)
        {
            m_Reader = reader;
            BuildEntries();
        }

        internal void BuildEntries()
        {
            connections = new ConnectionEntries(m_Reader);
            fieldDescriptions = new FieldDescriptionEntries(m_Reader);
            gcHandles = new GCHandleEntries(m_Reader);
            managedHeapSections = new ManagedMemorySectionEntries(m_Reader, EntryType.ManagedHeapSections_StartAddress);
            managedStacks = new ManagedMemorySectionEntries(m_Reader, EntryType.ManagedStacks_StartAddress);
            nativeAllocations = new NativeAllocationEntries(m_Reader);
            nativeAllocationSites = new NativeAllocationSiteEntries(m_Reader);
            nativeCallstackSymbols = new NativeCallstackSymbolEntries(m_Reader);
            nativeMemoryLabels = new NativeMemoryLabelEntries(m_Reader);
            nativeMemoryRegions = new NativeMemoryRegionEntries(m_Reader);
            nativeObjects = new NativeObjectEntries(m_Reader);
            nativeRootReferences = new NativeRootReferenceEntries(m_Reader);
            nativeTypes = new NativeTypeEntries(m_Reader);
            typeDescriptions = new TypeDescriptionEntries(m_Reader);
        }

        internal MemorySnapshotFileReader GetReader()
        {
            return m_Reader;
        }

        public UInt32 version
        {
            get
            {
                return m_Reader.GetDataSingle(EntryType.Metadata_Version, ConversionFunctions.ToUInt32);
            }
        }

        public MetaData metadata
        {
            get
            {
                byte[] array = m_Reader.GetDataSingle(EntryType.Metadata_UserMetadata, ConversionFunctions.ToByteArray);
                // decoded as
                //   content_data_length
                //   content_data
                //   platform_data_length
                //   platform_data
                //   screenshot_data_length
                //     [opt: screenshot_data ]
                //     [opt: screenshot_width ]
                //     [opt: screenshot_height ]
                //     [opt: screenshot_format ]
                var data = new UnityEngine.Profiling.Memory.Experimental.MetaData();

                if (array.Length == 0)
                {
                    data.content = "";
                    data.platform = "";
                    return data;
                }

                int offset = 0;
                int dataLength = 0;
                offset = ReadIntFromByteArray(array, offset, out dataLength);
                offset = ReadStringFromByteArray(array, offset, dataLength, out data.content);
                offset = ReadIntFromByteArray(array, offset, out dataLength);
                offset = ReadStringFromByteArray(array, offset, dataLength, out data.platform);
                offset = ReadIntFromByteArray(array, offset, out dataLength);

                if (dataLength > 0)
                {
                    int width;
                    int height;
                    int format;
                    byte[] screenshot = new byte[dataLength];

                    Array.Copy(array, offset, screenshot, 0, dataLength);
                    offset += dataLength;

                    offset = ReadIntFromByteArray(array, offset, out width);
                    offset = ReadIntFromByteArray(array, offset, out height);
                    offset = ReadIntFromByteArray(array, offset, out format);

                    data.screenshot = new Texture2D(width, height, (TextureFormat)format, false);
                    data.screenshot.LoadRawTextureData(screenshot);
                    data.screenshot.Apply();
                }

                UnityEngine.Assertions.Assert.AreEqual(array.Length, offset);

                return data;
            }
        }

        private static int ReadIntFromByteArray(byte[] array, int offset, out int value)
        {
            unsafe
            {
                int lValue = 0;
                byte* pi = (byte*)&lValue;
                pi[0] = array[offset++];
                pi[1] = array[offset++];
                pi[2] = array[offset++];
                pi[3] = array[offset++];

                value = lValue;
            }

            return offset;
        }

        private static int ReadStringFromByteArray(byte[] array, int offset, int stringLength, out string value)
        {
            if (stringLength == 0)
            {
                value = "";
                return offset;
            }

            unsafe
            {
                value = new string('\0', stringLength);
                fixed(char* p = value)
                {
                    char* begin = p;
                    char* end = p + value.Length;

                    while (begin != end)
                    {
                        for (int i = 0; i < sizeof(char); ++i)
                        {
                            ((byte*)begin)[i] = array[offset++];
                        }

                        begin++;
                    }
                }
            }

            return offset;
        }

        public string filePath
        {
            get
            {
                return m_Reader.GetFilePath();
            }
        }

        public DateTime recordDate
        {
            get
            {
                long ticks = m_Reader.GetDataSingle(EntryType.Metadata_RecordDate, ConversionFunctions.ToInt64);
                return new DateTime(ticks);
            }
        }

        public CaptureFlags captureFlags
        {
            get
            {
                return (CaptureFlags)m_Reader.GetDataSingle(EntryType.Metadata_CaptureFlags, ConversionFunctions.ToUInt32);
            }
        }

        public VirtualMachineInformation virtualMachineInformation
        {
            get
            {
                return m_Reader.GetDataSingle(EntryType.Metadata_VirtualMachineInformation, ConversionFunctions.ToVirtualMachineInformation);
            }
        }

        ~PackedMemorySnapshot()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_Reader == null)
                {
                    return;
                }

                m_Reader.Dispose();
                m_Reader = null;
            }
        }
    }

    [Flags]
    public enum ObjectFlags
    {
        IsDontDestroyOnLoad = 0x1,
        IsPersistent = 0x2,
        IsManager = 0x4,
    }

    public static class ObjectFlagsExtensions
    {
        public static bool IsDontDestroyOnLoad(this ObjectFlags flags)
        {
            return (flags & ObjectFlags.IsDontDestroyOnLoad) != 0;
        }

        public static bool IsPersistent(this ObjectFlags flags)
        {
            return (flags & ObjectFlags.IsPersistent) != 0;
        }

        public static bool IsManager(this ObjectFlags flags)
        {
            return (flags & ObjectFlags.IsManager) != 0;
        }
    }

    [Flags]
    public enum TypeFlags
    {
        kNone = 0,
        kValueType = 1 << 0,
        kArray = 1 << 1,
        kArrayRankMask = unchecked((int)0xFFFF0000)
    };

    public static class TypeFlagsExtensions
    {
        public static bool IsValueType(this TypeFlags flags)
        {
            return (flags & TypeFlags.kValueType) != 0;
        }

        public static bool IsArray(this TypeFlags flags)
        {
            return (flags & TypeFlags.kArray) != 0;
        }

        public static int ArrayRank(this TypeFlags flags)
        {
            return (int)(flags & TypeFlags.kArrayRankMask) >> 16;
        }
    }

    public struct VirtualMachineInformation
    {
        public int pointerSize { get; internal set; }
        public int objectHeaderSize { get; internal set; }
        public int arrayHeaderSize { get; internal set; }
        public int arrayBoundsOffsetInHeader { get; internal set; }
        public int arraySizeOffsetInHeader { get; internal set; }
        public int allocationGranularity { get; internal set; }
    }
}
