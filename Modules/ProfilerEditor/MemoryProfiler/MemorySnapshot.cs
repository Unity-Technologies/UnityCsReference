// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEditorInternal;
using UnityEditorInternal.MemoryProfiling;
using UnityEditorInternal.MemoryProfiling.FileFormat;

namespace UnityEditor.MemoryProfiling
{
    public static class MemorySnapshot
    {
        [Flags]
        public enum CaptureFlags : int
        {
            ManagedObjects = 0x001,
            NativeObjects = 0x002,
            NativeAllocations = 0x004,
            NativeAllocationSites = 0x008,
            NativeStackTraces = 0x010,
        }
        private static readonly UInt64 kMinSupportedVersion = 7;
        public static event Action<PackedMemorySnapshot> OnSnapshotReceived;

        public static void RequestNewSnapshot(CaptureFlags captureflags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects)
        {
            ProfilerDriver.RequestMemorySnapshot((uint)captureflags);
        }

        public static PackedMemorySnapshot LoadSnapshot(string path)
        {
            MemorySnapshotFileReader reader = new MemorySnapshotFileReader(path);
            UInt64 ver = reader.GetDataSingle(EntryType.Metadata_Version, ConversionFunctions.ToUInt32);
            if (ver < kMinSupportedVersion)
            {
                throw new Exception(string.Format("Memory snapshot at {0}, is using an older format version: {1}", new object[] { reader.GetFilePath(), ver.ToString() }));
            }

            return new PackedMemorySnapshot(reader);
        }

        public static void SaveSnapshot(PackedMemorySnapshot snapshot, string writePath)
        {
            string path = snapshot.GetReader().GetFilePath();
            FileUtil.CopyFileIfExists(path, writePath, true);
        }

        static void DispatchSnapshot(PackedMemorySnapshot snapshot)
        {
            var onSnapshotReceived = OnSnapshotReceived;

            if (onSnapshotReceived != null)
                onSnapshotReceived(snapshot);
        }

        static void OpenSnapshotFile(string path)
        {
            PackedMemorySnapshot snapshot = null;
            try
            {
                MemorySnapshotFileReader reader = new MemorySnapshotFileReader(path);
                snapshot = new PackedMemorySnapshot(reader);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return;
            }
            if (snapshot != null)
            {
                DispatchSnapshot(snapshot);
            }
        }
    }

    // Note: this snapshot is completely serializable by unity's serializer.
    // !!!!! NOTE: Keep in sync with Runtime\Profiler\MemorySnapshots.cpp
    public class PackedMemorySnapshot : ISerializationCallbackReceiver
    {
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

        public DateTime recordDate
        {
            get
            {
                double seconds = (double)m_Reader.GetDataSingle(EntryType.Metadata_RecordDate, ConversionFunctions.ToUInt64);
                return new DateTime(1970, 1, 1).AddSeconds(seconds);
            }
        }

        public MemorySnapshot.CaptureFlags captureFlags
        {
            get
            {
                return (MemorySnapshot.CaptureFlags)m_Reader.GetDataSingle(
                    EntryType.Metadata_CaptureFlags, ConversionFunctions.ToUInt32);
            }
        }

        public VirtualMachineInformation virtualMachineInformation
        {
            get
            {
                return (VirtualMachineInformation)m_Reader.GetDataSingle(
                    EntryType.Metadata_VirtualMachineInformation, ConversionFunctions.ToVirtualMachineInformation);
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

    [Serializable, MovedFrom("UnityEditor.MemoryProfiler")]
    public struct VirtualMachineInformation
    {
        [SerializeField]
        public int pointerSize { get; internal set; }

        [SerializeField]
        public int objectHeaderSize { get; internal set; }

        [SerializeField]
        public int arrayHeaderSize { get; internal set; }

        [SerializeField]
        public int arrayBoundsOffsetInHeader { get; internal set; }

        [SerializeField]
        public int arraySizeOffsetInHeader { get; internal set; }

        [SerializeField]
        public int allocationGranularity { get; internal set; }
    }
}
