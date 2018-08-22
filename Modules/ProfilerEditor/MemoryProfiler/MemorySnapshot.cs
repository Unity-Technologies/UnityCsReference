// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEditorInternal.Profiling.Memory.Experimental.FileFormat;

namespace UnityEditor.Profiling.Memory.Experimental
{
    // Note: this snapshot is completely serializable by unity's serializer.
    // !!!!! NOTE: Keep in sync with Runtime\Profiler\MemorySnapshots.cpp
    public class PackedMemorySnapshot : ISerializationCallbackReceiver
    {
        private static readonly UInt64 kMinSupportedVersion = 7;

        public static PackedMemorySnapshot Load(string path)
        {
            MemorySnapshotFileReader reader = new MemorySnapshotFileReader(path);

            UInt64 ver = reader.GetDataSingle(EntryType.Metadata_Version, ConversionFunctions.ToUInt32);

            if (ver < kMinSupportedVersion)
            {
                throw new Exception(string.Format("Memory snapshot at {0}, is using an older format version: {1}", new object[] { reader.GetFilePath(), ver.ToString() }));
            }

            return new PackedMemorySnapshot(reader);
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

        [SerializeField]
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

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            BuildEntries();
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

        public UnityEngine.Profiling.Memory.Experimental.MetaData metadata
        {
            get
            {
                byte[] array = m_Reader.GetDataSingle<byte[]>(EntryType.Metadata_UserMetadata, ConversionFunctions.ToByteArray);
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

                var metaData = new UnityEngine.Profiling.Memory.Experimental.MetaData();

                int offset = 0;
                int dataLength = 0;
                offset = ReadIntFromByteArray(array, offset, out dataLength);
                offset = ReadStringFromByteArray(array, offset, dataLength, out metaData.content);
                offset = ReadIntFromByteArray(array, offset, out dataLength);
                offset = ReadStringFromByteArray(array, offset, dataLength, out metaData.platform);
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

                    metaData.screenshot = new Texture2D(width, height, (TextureFormat)format, false);
                    metaData.screenshot.LoadRawTextureData(screenshot);
                    metaData.screenshot.Apply();
                }

                UnityEngine.Assertions.Assert.AreEqual(array.Length, offset);

                return metaData;
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
                double seconds = (double)m_Reader.GetDataSingle(EntryType.Metadata_RecordDate, ConversionFunctions.ToUInt64);
                return new DateTime(1970, 1, 1).AddSeconds(seconds);
            }
        }

        public UnityEngine.Profiling.Memory.Experimental.CaptureFlags captureFlags
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
