// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 UnityObject (PPtr) extension handlers (doc §2.4).
// Each reference is a 12-byte LocalSerializedObjectIdentifier. The handlers
// call v1's codecs (ResolveUnityObjectEntityIdForWrite with its UUM-143556
// drop, PackEntityIdIntoLsoi, WriteEntityIdToBuffer,
// ConsumeLinearCollectionUnityObjectArray), so the wire bytes match v1's.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // PPtr array: ids resolve in managed code, one native crossing per
    // window-sized batch (src == output).
    private static unsafe void V2WriteUnityObjectArray(byte[] dataAsBytes, int count, uint stride, NativeBufferContext* ctx, ulong userData)
    {
        ConsumeLinearCollectionUnityObjectArray(ctx, dataAsBytes, count, stride);
    }
    // readEntries and fieldTable share the native system's layouts, so the batched crossing takes
    // both unchanged. baseAddr is unpinned, so that crossing needs a non-moving GC: IL2CPP only,
    // and only for count > 1.
    private static unsafe void V2ReadUnityObjectGroup(object frameInstance, nint frameOffset, ref byte baseAddr, byte* entriesRaw, byte* readEntriesRaw, byte* fieldTableBase, int count, byte* input, NativeReadBufferContext* ctx, ulong userData)
    {
        var entry = (UnityObjectReadEntry*)readEntriesRaw;
        var end = entry + count;
        bool packed = IsPackedUnityObjectReference(ctx->flags);
        bool resolveInManaged = CanResolveUnityObjectsInManaged(ctx->flags);
        LastAssignableKlass lastAssignable = default;
        uint lastDeclaredTypeIndex = uint.MaxValue;
        IntPtr lastKlass = IntPtr.Zero;
        int i = 0;
        do
        {
            ref object fieldRef = ref V2FieldRef<object>(ref baseAddr, (nint)entry->fieldOffset);
            byte* src = input + entry->destOffset;

            object managed = null;
            bool resolvedInManaged = false;
            // This entry's fake-null context, read only by the full Editor read and the native fallback.
            byte* entryFieldSlot = fieldTableBase + i * 2 * sizeof(IntPtr);

            // CoreCLR only: IL2CPP has no cheap per-element type check, and a System.Type
            // lookup per element costs more than the crossing it saves.
            if (resolveInManaged)
            {
                // A group's fields usually share a declared type.
                if (entry->declaredTypeIndex != lastDeclaredTypeIndex)
                {
                    lastDeclaredTypeIndex = entry->declaredTypeIndex;
                    lastKlass = DeclaredTypeKlass(lastDeclaredTypeIndex);
                }
                ulong packedId = 0;
                if (packed)
                {
                    packedId = UnpackEntityIdFromLsoi(src);
                    resolvedInManaged = TryReadResidentPackedUnityObject(packedId, lastKlass, ref lastAssignable, out managed);
                }
                if (!resolvedInManaged)
                {
                    resolvedInManaged = TryReadUnityObjectReferenceForEditor(
                        src, packedId, entry->declaredTypeIndex, ctx, packed, lastKlass,
                        Unsafe.ReadUnaligned<IntPtr>(entryFieldSlot), Unsafe.ReadUnaligned<IntPtr>(entryFieldSlot + sizeof(IntPtr)),
                        ref lastAssignable, out managed);
                }
            }

            if (resolvedInManaged)
            {
                fieldRef = managed;
            }
            else
            {
                fieldRef = ReadUnityObjectFromBuffer(
                    ctx->resolverHandle, (IntPtr)src, entry->declaredTypeIndex, ctx->flags,
                    Unsafe.ReadUnaligned<IntPtr>(entryFieldSlot), Unsafe.ReadUnaligned<IntPtr>(entryFieldSlot + sizeof(IntPtr)));
            }
            entry++;
            i++;
        }
        while (entry < end);
    }

    // Windowed batched resolves. The enter read the count, bound the collection to the field and
    // rebased `data` to element 0.
    // It skips the body at count 0, so count is always positive here.
    private static unsafe void V2ReadUnityObjectArray(
        byte[] backing, int count, long stride, byte* cmdRaw, NativeReadBufferContext* ctx,
        byte* pathSegments, byte* pathNames)
    {
        var cmd = (V2CmdExternalArray*)cmdRaw;
        int wire = (int)cmd->elementWireBytes;
        // An interior reference rather than a pinned pointer. The element loop allocates wrappers, so
        // a compacting GC can move the backing array, and the GC updates a byref.
        ref byte data = ref backing[0];
        int processed = 0;
        bool packed = IsPackedUnityObjectReference(ctx->flags);
        bool resolveInManaged = CanResolveUnityObjectsInManaged(ctx->flags);
        // The element class is constant, so this hits after the first element.
        LastAssignableKlass lastAssignable = default;
        IntPtr readKlass = DeclaredTypeKlass((uint)cmd->readDeclaredTypeIndex);
        while (processed < count)
        {
            if (ctx->readerEnd - ctx->readerPtr < wire)
                InvokeEnsureReadable(ctx, wire);
            int batch = (int)(ctx->readerEnd - ctx->readerPtr) / wire;
            int remaining = count - processed;
            if (batch > remaining)
                batch = remaining;

            // No batch codec here: an Object-returning calli cannot cross and the GC moves
            // objects. Resolve per element and store through a managed reference so the
            // write barrier runs.
            for (int i = 0; i < batch; ++i)
            {
                byte* src = ctx->readerPtr + i * wire;
                object wrapper = null;
                bool resolvedInManaged = false;
                // The fake-null context is uniform for the collection, so it comes off the
                // command rather than a per-entry table.
                if (resolveInManaged)
                {
                    ulong packedId = 0;
                    if (packed)
                    {
                        packedId = UnpackEntityIdFromLsoi(src);
                        resolvedInManaged = TryReadResidentPackedUnityObject(packedId, readKlass, ref lastAssignable, out wrapper);
                    }
                    if (!resolvedInManaged)
                        resolvedInManaged = TryReadUnityObjectReferenceForEditor(
                            src, packedId, (uint)cmd->readDeclaredTypeIndex, ctx, packed, readKlass,
                            (nint)cmd->readField, (nint)cmd->readFieldParent,
                            ref lastAssignable, out wrapper);
                }
                if (!resolvedInManaged)
                    wrapper = ReadUnityObjectFromBuffer(
                        ctx->resolverHandle, (IntPtr)src,
                        (uint)cmd->readDeclaredTypeIndex, ctx->flags, (nint)cmd->readField, (nint)cmd->readFieldParent);
                ref object elemSlot = ref Unsafe.As<byte, object>(
                    ref Unsafe.AddByteOffset(ref data, (nint)((long)(processed + i) * stride)));
                elemSlot = wrapper;
            }

            ctx->readerPtr += batch * wire;
            processed += batch;
        }
    }

    // PPtr scalar group, one call per group. Remap groups (two or more fields,
    // non-clone) pre-write ids into the output slots and resolve them in one
    // native crossing; the entry table is layout-compatible with
    // UnityObjectWriteEntry, so the batched codec takes it unchanged. Clone and
    // pack groups loop inline with no native call.
    private static unsafe void V2WriteUnityObjectGroup(ref byte baseAddr, byte* entriesRaw, int count, byte* output, NativeBufferContext* ctx, ulong userData)
    {
        var entry = (V2ExternalFixedEntry*)entriesRaw;
        var end = entry + count;
        bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
        if (!packInLSOI && count > 1)
        {
            var e = entry;
            do
            {
                object slot = V2FieldRef<object>(ref baseAddr, (nint)e->fieldOffset);
                Unsafe.WriteUnaligned<ulong>(output + e->destOffset, ResolveUnityObjectEntityIdForWrite(slot, ctx->flags));
                e++;
            }
            while (e < end);
            s_writeUnityObjectEntityIdsToBuffer(ctx->resolverHandle, ctx->flags, (IntPtr)entry, (IntPtr)output, count);
            return;
        }
        do
        {
            object slot = V2FieldRef<object>(ref baseAddr, (nint)entry->fieldOffset);
            byte* dst = output + entry->destOffset;
            ulong entityId = ResolveUnityObjectEntityIdForWrite(slot, ctx->flags);
            if (packInLSOI || entityId == 0UL)
                PackEntityIdIntoLsoi(dst, entityId);
            else
                s_writeEntityIdToBuffer(entityId, ctx->resolverHandle, (IntPtr)dst, ctx->flags);
            entry++;
        }
        while (entry < end);
    }

    // Collect arms (V2BuildCollectStream swaps them in for the two write arms
    // above): report each non-null reference's EntityId to the collect sink,
    // resolved as the write resolves it, with the target index as userData.
    private static unsafe void V2CollectUnityObjectGroup(ref byte baseAddr, byte* entriesRaw, int count, byte* output, NativeBufferContext* ctx, ulong userData)
    {
        var sink = (V2CollectSink*)ctx->transferState;
        var entries = (V2ExternalFixedEntry*)entriesRaw;
        for (int i = 0; i < count; ++i)
        {
            ulong entityId = ResolveUnityObjectEntityIdForWrite(V2FieldRef<object>(ref baseAddr, (nint)entries[i].fieldOffset), ctx->flags);
            if (entityId != 0UL)
                sink->reportEntityId(sink, (uint)userData, entityId);
        }
    }

    private static unsafe void V2CollectUnityObjectArray(byte[] dataAsBytes, int count, uint stride, NativeBufferContext* ctx, ulong userData)
    {
        var sink = (V2CollectSink*)ctx->transferState;
        object[] elements = Unsafe.As<byte[], object[]>(ref dataAsBytes);
        for (int i = 0; i < count; ++i)
        {
            ulong entityId = ResolveUnityObjectEntityIdForWrite(elements[i], ctx->flags);
            if (entityId != 0UL)
                sink->reportEntityId(sink, (uint)userData, entityId);
        }
    }
}
