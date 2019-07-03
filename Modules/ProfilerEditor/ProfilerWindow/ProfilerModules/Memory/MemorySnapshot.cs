// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.MemoryProfiler
{
    public static class MemorySnapshot
    {
        public static event Action<PackedMemorySnapshot> OnSnapshotReceived;

        private static void SnapshotFinished(string path, bool result)
        {
            if (result)
            {
                Profiling.Memory.Experimental.PackedMemorySnapshot snapshot = Profiling.Memory.Experimental.PackedMemorySnapshot.Load(path);

                var oldSnapshot = new PackedMemorySnapshot(snapshot);
                snapshot.Dispose();
                File.Delete(path);

                OnSnapshotReceived(oldSnapshot);
            }
            else
            {
                if (File.Exists(path))
                    File.Delete(path);

                OnSnapshotReceived(null);
            }
        }

        internal static string GetTemporarySnapshotPath()
        {
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            return Path.Combine(Application.temporaryCachePath, projectName + ".snap");
        }

        public static void RequestNewSnapshot()
        {
            UnityEngine.Profiling.Memory.Experimental.MemoryProfiler.TakeSnapshot(GetTemporarySnapshotPath(), SnapshotFinished, UnityEngine.Profiling.Memory.Experimental.CaptureFlags.NativeObjects | UnityEngine.Profiling.Memory.Experimental.CaptureFlags.ManagedObjects);
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
        internal PackedGCHandle[] m_GCHandles = null;

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

        public PackedMemorySnapshot(Profiling.Memory.Experimental.PackedMemorySnapshot snapshot)
        {
            int cacheCapacity = 128;
            string[] cacheString = new string[cacheCapacity];
            string[] cacheString2 = new string[cacheCapacity];
            int[]    cacheInt = new int[cacheCapacity];
            int[]    cacheInt2 = new int[cacheCapacity];
            int[]    cacheInt3 = new int[cacheCapacity];
            ulong[]  cacheULong = new ulong[cacheCapacity];
            ulong[]  cacheULong2 = new ulong[cacheCapacity];
            byte[][]  cacheBytes = new byte[cacheCapacity][];

            m_NativeTypes = new PackedNativeType[snapshot.nativeTypes.GetNumEntries()];
            {
                for (int offset = 0; offset < m_NativeTypes.Length; offset += cacheCapacity)
                {
                    uint size = (uint)Math.Min(m_NativeTypes.Length - offset, cacheCapacity);

                    snapshot.nativeTypes.typeName.GetEntries((uint)offset, size, ref cacheString);
                    snapshot.nativeTypes.nativeBaseTypeArrayIndex.GetEntries((uint)offset, size, ref cacheInt);

                    for (uint i = 0; i < size; i++)
                    {
                        m_NativeTypes[offset + i] = new PackedNativeType(cacheString[i], cacheInt[i]);
                    }
                }
            }

            m_NativeObjects = new PackedNativeUnityEngineObject[snapshot.nativeObjects.GetNumEntries()];
            {
                UnityEditor.Profiling.Memory.Experimental.ObjectFlags[] cacheObjectFlags = new UnityEditor.Profiling.Memory.Experimental.ObjectFlags[cacheCapacity];
                UnityEngine.HideFlags[] cacheHideFlags = new UnityEngine.HideFlags[cacheCapacity];

                for (int offset = 0; offset < m_NativeObjects.Length; offset += cacheCapacity)
                {
                    uint size = (uint)Math.Min(m_NativeObjects.Length - offset, cacheCapacity);

                    snapshot.nativeObjects.objectName.GetEntries((uint)offset, size, ref cacheString);
                    snapshot.nativeObjects.instanceId.GetEntries((uint)offset, size, ref cacheInt);
                    snapshot.nativeObjects.size.GetEntries((uint)offset, size, ref cacheULong);
                    snapshot.nativeObjects.nativeTypeArrayIndex.GetEntries((uint)offset, size, ref cacheInt2);
                    snapshot.nativeObjects.hideFlags.GetEntries((uint)offset, size, ref cacheHideFlags);
                    snapshot.nativeObjects.flags.GetEntries((uint)offset, size, ref cacheObjectFlags);
                    snapshot.nativeObjects.nativeObjectAddress.GetEntries((uint)offset, size, ref cacheULong2);

                    for (uint i = 0; i < size; i++)
                    {
                        m_NativeObjects[offset + i] = new PackedNativeUnityEngineObject(
                            cacheString[i],
                            cacheInt[i],
                            (int)cacheULong[i],
                            cacheInt2[i],
                            cacheHideFlags[i],
                            (PackedNativeUnityEngineObject.ObjectFlags)cacheObjectFlags[i],
                            (long)cacheULong2[i]);
                    }
                }
            }

            m_GCHandles = new PackedGCHandle[snapshot.gcHandles.GetNumEntries()];
            {
                for (int offset = 0; offset < m_GCHandles.Length; offset += cacheCapacity)
                {
                    uint size = (uint)Math.Min(m_GCHandles.Length - offset, cacheCapacity);

                    snapshot.gcHandles.target.GetEntries((uint)offset, size, ref cacheULong);

                    for (uint i = 0; i < size; i++)
                    {
                        m_GCHandles[offset + i] = new PackedGCHandle((UInt64)cacheULong[i]);
                    }
                }
            }

            m_Connections = new Connection[snapshot.connections.GetNumEntries()];
            {
                for (int offset = 0; offset < m_Connections.Length; offset += cacheCapacity)
                {
                    uint size = (uint)Math.Min(m_Connections.Length - offset, cacheCapacity);

                    snapshot.connections.from.GetEntries((uint)offset, (uint)size, ref cacheInt);
                    snapshot.connections.to.GetEntries((uint)offset, (uint)size, ref cacheInt2);

                    for (uint i = 0; i < size; i++)
                    {
                        m_Connections[offset + i] = new Connection(cacheInt[i], cacheInt2[i]);
                    }
                }
            }

            m_ManagedHeapSections = new MemorySection[snapshot.managedHeapSections.GetNumEntries()];
            {
                for (int offset = 0; offset < m_ManagedHeapSections.Length; offset += cacheCapacity)
                {
                    uint size = (uint)Math.Min(m_ManagedHeapSections.Length - offset, cacheCapacity);

                    snapshot.managedHeapSections.startAddress.GetEntries((uint)offset, (uint)size, ref cacheULong);
                    snapshot.managedHeapSections.bytes.GetEntries((uint)offset, (uint)size, ref cacheBytes);

                    for (uint i = 0; i < size; i++)
                    {
                        m_ManagedHeapSections[offset + i] = new MemorySection(cacheBytes[i], (UInt64)cacheULong[i]);
                    }
                }
            }

            m_TypeDescriptions = new TypeDescription[snapshot.typeDescriptions.GetNumEntries()];
            {
                UnityEditor.Profiling.Memory.Experimental.TypeFlags[] cacheFlags = new UnityEditor.Profiling.Memory.Experimental.TypeFlags[cacheCapacity];
                int[][] cacheRange = new int[cacheCapacity][];
                string[] cacheSmallString = new string[1];
                int[] cacheSmallInt = new int[1];
                int[] cacheSmallInt2 = new int[1];
                bool[] cacheSmallBool = new bool[1];

                for (int offset = 0; offset < m_TypeDescriptions.Length; offset += cacheCapacity)
                {
                    uint size = (uint)Math.Min(m_TypeDescriptions.Length - offset, cacheCapacity);
                    snapshot.typeDescriptions.typeDescriptionName.GetEntries((uint)offset, (uint)size, ref cacheString);
                    snapshot.typeDescriptions.assembly.GetEntries((uint)offset, (uint)size, ref cacheString2);
                    snapshot.typeDescriptions.fieldIndices.GetEntries((uint)offset, (uint)size, ref cacheRange);
                    snapshot.typeDescriptions.staticFieldBytes.GetEntries((uint)offset, (uint)size, ref cacheBytes);
                    snapshot.typeDescriptions.baseOrElementTypeIndex.GetEntries((uint)offset, (uint)size, ref cacheInt);
                    snapshot.typeDescriptions.size.GetEntries((uint)offset, (uint)size, ref cacheInt2);
                    snapshot.typeDescriptions.typeInfoAddress.GetEntries((uint)offset, (uint)size, ref cacheULong);
                    snapshot.typeDescriptions.typeIndex.GetEntries((uint)offset, (uint)size, ref cacheInt3);
                    snapshot.typeDescriptions.flags.GetEntries((uint)offset, (uint)size, ref cacheFlags);

                    for (int i = 0; i < size; ++i)
                    {
                        FieldDescription[] fieldDescription = new FieldDescription[cacheRange[i].Length];

                        for (uint j = 0; j < cacheRange[i].Length; j++)
                        {
                            snapshot.fieldDescriptions.fieldDescriptionName.GetEntries((uint)cacheRange[i][j], 1, ref cacheSmallString);
                            snapshot.fieldDescriptions.offset.GetEntries((uint)cacheRange[i][j], 1, ref cacheSmallInt);
                            snapshot.fieldDescriptions.typeIndex.GetEntries((uint)cacheRange[i][j], 1, ref cacheSmallInt2);
                            snapshot.fieldDescriptions.isStatic.GetEntries((uint)cacheRange[i][j], 1, ref cacheSmallBool);

                            fieldDescription[j] = new FieldDescription(cacheSmallString[0], cacheSmallInt[0], cacheSmallInt2[0], cacheSmallBool[0]);
                        }

                        m_TypeDescriptions[offset + i] = new TypeDescription(
                            cacheString[i],
                            cacheString2[i],
                            fieldDescription,
                            cacheBytes[i],
                            cacheInt[i],
                            cacheInt2[i],
                            (UInt64)cacheULong[i],
                            cacheInt3[i],
                            (TypeDescription.TypeFlags)cacheFlags[i]);
                    }
                }
            }

            m_VirtualMachineInformation = new VirtualMachineInformation(snapshot.virtualMachineInformation);
        }

        public PackedNativeType[] nativeTypes { get { return m_NativeTypes; } }
        public PackedNativeUnityEngineObject[] nativeObjects { get { return m_NativeObjects; } }
        public PackedGCHandle[] gcHandles { get { return m_GCHandles; } }
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

        public PackedNativeType(string name, int nativeBaseTypeArrayIndex)
        {
            m_Name = name;
            m_NativeBaseTypeArrayIndex = nativeBaseTypeArrayIndex;
        }

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

        public PackedNativeUnityEngineObject(string name, int instanceId, int size, int nativeTypeArrayIndex, UnityEngine.HideFlags hideFlags, ObjectFlags flags, long nativeObjectAddress)
        {
            m_Name = name;
            m_InstanceId = instanceId;
            m_Size = size;
            m_NativeTypeArrayIndex = nativeTypeArrayIndex;
            m_HideFlags = hideFlags;
            m_Flags = flags;
            m_NativeObjectAddress = nativeObjectAddress;
        }

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

        public enum ObjectFlags
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

        public PackedGCHandle(UInt64 target)
        {
            m_Target = target;
        }

        public UInt64 target { get { return m_Target; } }
    }

    [Serializable]
    public struct Connection
    {
        [SerializeField]
        private int m_From;

        [SerializeField]
        private int m_To;

        public Connection(int from, int to)
        {
            m_From = from;
            m_To = to;
        }

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

        public MemorySection(byte[] bytes, UInt64 startAddress)
        {
            m_Bytes = bytes;
            m_StartAddress = startAddress;
        }

        public byte[] bytes { get { return m_Bytes; } }
        public UInt64 startAddress { get { return m_StartAddress; } }
    }

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

        public TypeDescription(string name, string assembly, FieldDescription[] fields, byte[] staticFieldBytes, int baseOrElementTypeIndes, int size, UInt64 typeInfoAddress, int typeIndex, TypeFlags flags)
        {
            m_Name = name;
            m_Assembly = assembly;
            m_Fields = fields;
            m_StaticFieldBytes = staticFieldBytes;
            m_BaseOrElementTypeIndex = baseOrElementTypeIndes;
            m_Size = size;
            m_TypeInfoAddress = typeInfoAddress;
            m_TypeIndex = typeIndex;
            m_Flags = flags;
        }

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

        public enum TypeFlags
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

        public FieldDescription(string name, int offset, int typeIndex, bool isStatic)
        {
            m_Name = name;
            m_Offset = offset;
            m_TypeIndex = typeIndex;
            m_IsStatic = isStatic;
        }

        public string name { get { return m_Name; } }
        public int offset { get { return m_Offset; } }
        public int typeIndex { get { return m_TypeIndex; } }
        public bool isStatic { get { return m_IsStatic; } }
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

        internal VirtualMachineInformation(Profiling.Memory.Experimental.VirtualMachineInformation virtualMachineInformation)
        {
            m_PointerSize = virtualMachineInformation.pointerSize;
            m_ObjectHeaderSize = virtualMachineInformation.objectHeaderSize;
            m_ArrayHeaderSize = virtualMachineInformation.arrayHeaderSize;
            m_ArrayBoundsOffsetInHeader = virtualMachineInformation.arrayBoundsOffsetInHeader;
            m_ArraySizeOffsetInHeader = virtualMachineInformation.arraySizeOffsetInHeader;
            m_AllocationGranularity = virtualMachineInformation.allocationGranularity;
        }
    };
}
