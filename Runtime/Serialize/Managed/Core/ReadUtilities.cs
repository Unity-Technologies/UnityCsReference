// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Serialization;

// Managed serialization V2: read-side helpers used by ReadExecutor.cs.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Keeps the existing array when the length matches, and a list's backing
    // when it is large enough (v1's reuse-or-allocate contract), and binds the
    // result to the field: arrays store the reference, lists refill in place
    // to preserve instance identity, allocating an uninitialized List only
    // when the field is null (native LinearCollectionField::SetArray). The
    // caller fills the returned backing afterwards; nothing runs between the
    // bind and the fill that could observe the collection.
    private static unsafe byte[] V2ReuseOrAllocateCollection(
        ref byte baseAddr, uint fieldOffset, byte kind, ulong elementTypeHandle, int count)
    {
        Type elementType = UnmarshalSystemType((IntPtr)elementTypeHandle);
        ref byte fieldSlot = ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset);
        if (kind == 0)  // array
        {
            Array existing = Unsafe.As<byte, Array>(ref fieldSlot);
            Array arr = (existing != null && existing.Length == count)
                ? existing
                : Array.CreateInstance(elementType, count);
            Unsafe.As<byte, Array>(ref fieldSlot) = arr;
            return Unsafe.As<Array, byte[]>(ref arr);
        }

        ListLayout layout = Unsafe.As<byte, ListLayout>(ref fieldSlot);
        Array backing = (layout != null
            && layout._size == count
            && layout._items != null
            && layout._items.Length >= count)
            ? Unsafe.As<byte[], Array>(ref layout._items)
            : Array.CreateInstance(elementType, count);
        if (layout == null)
        {
            layout = Unsafe.As<ListLayout>(RuntimeHelpers.GetUninitializedObject(GetCachedListType(elementType)));
            Unsafe.As<byte, ListLayout>(ref fieldSlot) = layout;
        }
        byte[] dataAsBytes = Unsafe.As<Array, byte[]>(ref backing);
        layout._items = dataAsBytes;
        layout._size = count;
        return dataAsBytes;
    }

    // SerializeReference missing-type registration (v1's
    // RegisterReferenceField read arm). For every segment-bearing entry of an
    // SR fixed group, resolves the site's identifier from the segment table
    // and the live frame stack, then hands (path, wire RefId) to the native
    // crossing, which prepends the registry-body "<hostRefId>." and appends
    // to the transfer state's reference-field map for
    // ProcessMissingTypeReferences. Cold path: the caller runs it only when
    // the persistent registry holds missing types.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void V2RegisterSrReadReferenceFields(
        byte* entriesRaw,
        byte* segmentTable,
        int count,
        byte* input,
        NativeReadBufferContext* ctx,
        byte* pathSegments,
        byte* pathNames,
        Span<V2ObjectFrame> frames)
    {
        if (pathSegments == null)
            return;
        var register = (delegate* unmanaged[Cdecl]<IntPtr, long, IntPtr, void>)(void*)ctx->registerSrReferenceField;
        var entries = (V2ExternalFixedEntry*)entriesRaw;
        for (int i = 0; i < count; ++i)
        {
            uint segment = Unsafe.ReadUnaligned<uint>(segmentTable + i * 4);
            if (segment == kV2NoPathSegment)
                continue;
            byte[] identifier = BuildV2FuidIdentifier(pathSegments, pathNames, segment, frames, -1);
            if (identifier == null)
                continue;
            long refId = Unsafe.ReadUnaligned<long>(input + entries[i].destOffset);
            fixed (byte* identifierPtr = identifier)
                register(ctx->transferState, refId, (IntPtr)identifierPtr);
        }
    }
}
