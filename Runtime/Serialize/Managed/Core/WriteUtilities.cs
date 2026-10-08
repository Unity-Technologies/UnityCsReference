// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Serialization;

// Mirror of ExtensionTable.h's V2CollectSink: the head of a collect pass's
// state, which NativeBufferContext.transferState points at while a collect
// stream runs. The collect arms report each EntityId through it.
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
internal unsafe struct V2CollectSink
{
    public delegate* unmanaged[Cdecl]<V2CollectSink*, uint, ulong, void> reportEntityId;
}

// Managed serialization V2: write-side helpers used by WriteExecutor.cs.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Resolves a T[] or List<T> field to its backing array (aliased as
    // byte[], valid because the SZArray data offset is element-type
    // independent) and element count. Returns false for a null collection or
    // null backing, which serializes as count 0. `kind` is the command's raw
    // kind byte; only its low nibble is the collection kind.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool V2GetCollectionBacking(ref byte baseAddr, uint fieldOffset, byte kind, out byte[] dataAsBytes, out int count)
    {
        if ((kind & kV2Pad0KindMask) == 0)  // array
        {
            Array array = V2FieldRef<Array>(ref baseAddr, (nint)fieldOffset);
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

        ListLayout list = V2FieldRef<ListLayout>(ref baseAddr, (nint)fieldOffset);
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
