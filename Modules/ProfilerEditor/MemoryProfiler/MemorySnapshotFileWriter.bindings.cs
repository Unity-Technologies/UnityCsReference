// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditorInternal.Profiling.Memory.Experimental
{
    [NativeHeader("Modules/Profiler/Public/MemorySnapshot/MemorySnapshotFileWriter.h")]
    public class MemorySnapshotFileWriter : IDisposable
    {
        private IntPtr m_Ptr;

        [NativeThrows]
        extern public void Open(string filename);
        extern public void Close();

        extern private static IntPtr Internal_Create();
        extern private static void Internal_Destroy(IntPtr ptr);

        [NativeThrows]
        extern private void Internal_WriteEntryString(string data, int entryType);
        [NativeThrows]
        extern private void Internal_WriteEntryData(IntPtr data, int dataSize, int entryType);
        [NativeThrows]
        extern private void Internal_WriteEntryDataArray(IntPtr data, int dataSize, int numElements, int entryType);

        public MemorySnapshotFileWriter(string filepath)
        {
            m_Ptr = Internal_Create();
            Open(filepath);
        }

        public MemorySnapshotFileWriter()
        {
            m_Ptr = Internal_Create();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        public void WriteEntry(FileFormat.EntryType entryType, string data)
        {
            Internal_WriteEntryString(data, (int)entryType);
        }

        public void WriteEntry<T>(FileFormat.EntryType entryType, T data) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                int dataSize = UnsafeUtility.SizeOf<T>();
                Internal_WriteEntryData(rawDataPtr, dataSize, (int)entryType);
            }
            finally
            {
                handle.Free();
            }
        }

        public void WriteEntryArray<T>(FileFormat.EntryType entryType, T[] data) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr rawDataPtr = handle.AddrOfPinnedObject();
                int dataSize = UnsafeUtility.SizeOf<T>();
                int numElements = data.Length;
                Internal_WriteEntryDataArray(rawDataPtr, dataSize, numElements, (int)entryType);
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
