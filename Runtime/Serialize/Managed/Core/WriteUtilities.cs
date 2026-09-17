// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Serialization;

// Managed serialization V2: write-side helpers used by WriteExecutor.cs.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Resolves a T[] or List<T> field to its backing array (aliased as
    // byte[], valid because the SZArray data offset is element-type
    // independent) and element count. Returns false for a null collection or
    // null backing, which serializes as count 0.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool V2ReadCollection(ref byte baseAddr, uint fieldOffset, byte kind, out byte[] dataAsBytes, out int count)
    {
        if (kind == 0)  // array
        {
            Array array = Unsafe.As<byte, Array>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
            if (array == null)
            {
                dataAsBytes = null;
                count = 0;
                return false;
            }
            dataAsBytes = Unsafe.As<Array, byte[]>(ref array);
            count = array.Length;
            return true;
        }

        ListLayout list = Unsafe.As<byte, ListLayout>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
        if (list == null || list._items == null)
        {
            dataAsBytes = null;
            count = 0;
            return false;
        }
        dataAsBytes = list._items;
        count = list._size;
        return true;
    }
}
