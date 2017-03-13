// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.MemoryProfiler
{
    public static class MemorySnapshot
    {
        public static event Action<PackedMemorySnapshot> OnSnapshotReceived;

        public static void RequestNewSnapshot()
        {
            ProfilerDriver.RequestMemorySnapshot();
        }

        static void DispatchSnapshot(PackedMemorySnapshot snapshot)
        {
            var onSnapshotReceived = OnSnapshotReceived;

            if (onSnapshotReceived != null)
                onSnapshotReceived(snapshot);
        }
    }

    // Note: this snapshot is completely serializable by unity's serializer.
    // !!!!! NOTE: Keep in sync with Runtime\Profiler\MemorySnapshots.cpp
    [Serializable]
    public class PackedMemorySnapshot
    {
        [SerializeField]
        internal PackedNativeType[] m_NativeTypes = null;

        [SerializeField]
        internal PackedNativeUnityEngineObject[] m_NativeObjects = null;

        [SerializeField]
        internal PackedGCHandle[] m_GcHandles = null;

        [SerializeField]
        internal Connection[] m_Connections = null;

        [SerializeField]
        internal MemorySection[] m_ManagedHeapSections = null;

        [SerializeField]
        internal MemorySection[] m_ManagedStacks = null;

        [SerializeField]
        internal TypeDescription[] m_TypeDescriptions = null;

        [SerializeField]
        internal VirtualMachineInformation m_VirtualMachineInformation = default(VirtualMachineInformation);

        internal PackedMemorySnapshot() {}
        public PackedNativeType[] nativeTypes { get { return m_NativeTypes; } }
        public PackedNativeUnityEngineObject[] nativeObjects { get { return m_NativeObjects; } }
        public PackedGCHandle[] gcHandles { get { return m_GcHandles; } }
        public Connection[] connections { get { return m_Connections; } }
        public MemorySection[] managedHeapSections { get { return m_ManagedHeapSections; } }
        public TypeDescription[] typeDescriptions { get { return m_TypeDescriptions; } }
        public VirtualMachineInformation virtualMachineInformation { get { return m_VirtualMachineInformation; } }
    }

    [Serializable]
    public struct PackedNativeType
    {
        [SerializeField]
        internal string m_Name;

        [SerializeField]
        internal int m_NativeBaseTypeArrayIndex;

        public string name { get { return m_Name; } }

        [Obsolete("PackedNativeType.baseClassId is obsolete. Use PackedNativeType.nativeBaseTypeArrayIndex instead (UnityUpgradable) -> nativeBaseTypeArrayIndex")]
        public int baseClassId { get { return m_NativeBaseTypeArrayIndex; } }
        public int nativeBaseTypeArrayIndex { get { return m_NativeBaseTypeArrayIndex; } }
    }

    [Serializable]
    public struct PackedNativeUnityEngineObject
    {
        [SerializeField]
        internal string m_Name;

        [SerializeField]
        internal int m_InstanceId;

        [SerializeField]
        internal int m_Size;

        [SerializeField]
        internal int m_NativeTypeArrayIndex;

        [SerializeField]
        internal UnityEngine.HideFlags m_HideFlags;

        [SerializeField]
        internal ObjectFlags m_Flags;

        [SerializeField]
        internal long m_NativeObjectAddress;

        public bool isPersistent { get { return (m_Flags & ObjectFlags.IsPersistent) != 0; } }
        public bool isDontDestroyOnLoad { get { return (m_Flags & ObjectFlags.IsDontDestroyOnLoad) != 0; } }
        public bool isManager { get { return (m_Flags & ObjectFlags.IsManager) != 0; } }
        public string name { get { return m_Name; } }
        public int instanceId { get { return m_InstanceId; } }
        public int size { get { return m_Size; } }

        [Obsolete("PackedNativeUnityEngineObject.classId is obsolete. Use PackedNativeUnityEngineObject.nativeTypeArrayIndex instead (UnityUpgradable) -> nativeTypeArrayIndex")]
        public int classId { get { return m_NativeTypeArrayIndex; } }
        public int nativeTypeArrayIndex { get { return m_NativeTypeArrayIndex; } }
        public UnityEngine.HideFlags hideFlags { get { return m_HideFlags; } }
        public long nativeObjectAddress { get { return m_NativeObjectAddress; } }

        internal enum ObjectFlags
        {
            IsDontDestroyOnLoad = 0x1,
            IsPersistent = 0x2,
            IsManager = 0x4,
        }
    }

    [Serializable]
    public struct PackedGCHandle
    {
        [SerializeField]
        internal UInt64 m_Target;

        public UInt64 target { get { return m_Target; } }
    }

    [Serializable]
    public struct Connection
    {
        // These indices index into an imaginary array that is the concatenation of
        // snapshot.gcHandles + snapshot.nativeObject snapshot.
        [SerializeField]
        internal int m_From;

        [SerializeField]
        internal int m_To;

        public int from { get { return m_From; } set { m_From = value; } }
        public int to { get { return m_To; } set { m_To = value; } }
    }

    [Serializable]
    public struct MemorySection
    {
        [SerializeField]
        internal byte[] m_Bytes;
        [SerializeField]
        internal UInt64 m_StartAddress;

        public byte[] bytes { get { return m_Bytes; } }
        public UInt64 startAddress { get { return m_StartAddress; } }
    }

    [Serializable]
    public struct VirtualMachineInformation
    {
        [SerializeField]
        internal int m_PointerSize;

        [SerializeField]
        internal int m_ObjectHeaderSize;

        [SerializeField]
        internal int m_ArrayHeaderSize;

        [SerializeField]
        internal int m_ArrayBoundsOffsetInHeader;

        [SerializeField]
        internal int m_ArraySizeOffsetInHeader;

        [SerializeField]
        internal int m_AllocationGranularity;

        public int pointerSize { get { return m_PointerSize; } }
        public int objectHeaderSize { get { return m_ObjectHeaderSize; } }
        public int arrayHeaderSize { get { return m_ArrayHeaderSize; } }
        public int arrayBoundsOffsetInHeader { get { return m_ArrayBoundsOffsetInHeader; } }
        public int arraySizeOffsetInHeader { get { return m_ArraySizeOffsetInHeader; } }
        public int allocationGranularity { get { return m_AllocationGranularity; } }
        public int heapFormatVersion { get { return 0; } }
    };

    [Serializable]
    public struct TypeDescription
    {
        [SerializeField]
        internal string m_Name;

        [SerializeField]
        internal string m_Assembly;

        [SerializeField]
        internal FieldDescription[] m_Fields;

        [SerializeField]
        internal byte[] m_StaticFieldBytes;

        [SerializeField]
        internal int m_BaseOrElementTypeIndex;

        [SerializeField]
        internal int m_Size;

        [SerializeField]
        internal UInt64 m_TypeInfoAddress;

        [SerializeField]
        internal int m_TypeIndex;

        [SerializeField]
        internal TypeFlags m_Flags;

        public bool isValueType
        {
            get { return (m_Flags & TypeFlags.kValueType) != 0; }
        }

        public bool isArray
        {
            get { return (m_Flags & TypeFlags.kArray) != 0; }
        }

        public int arrayRank
        {
            get { return (int)(m_Flags & TypeFlags.kArrayRankMask) >> 16; }
        }

        public string name { get { return m_Name; } }
        public string assembly { get { return m_Assembly; } }
        public FieldDescription[] fields { get { return m_Fields; } }
        public byte[] staticFieldBytes { get { return m_StaticFieldBytes; } }
        public int baseOrElementTypeIndex { get { return m_BaseOrElementTypeIndex; } }
        public int size { get { return m_Size; } }
        public UInt64 typeInfoAddress { get { return m_TypeInfoAddress; } }
        public int typeIndex { get { return m_TypeIndex; } }


        internal enum TypeFlags
        {
            kNone = 0,
            kValueType = 1 << 0,
            kArray = 1 << 1,
            kArrayRankMask = unchecked((int)0xFFFF0000)
        };
    }

    [Serializable]
    public struct FieldDescription
    {
        [SerializeField]
        internal string m_Name;

        [SerializeField]
        internal int m_Offset;

        [SerializeField]
        internal int m_TypeIndex;

        [SerializeField]
        internal bool m_IsStatic;

        public string name { get { return m_Name; } }
        public int offset { get { return m_Offset; } }
        public int typeIndex { get { return m_TypeIndex; } }
        public bool isStatic { get { return m_IsStatic; } }
    }
}
