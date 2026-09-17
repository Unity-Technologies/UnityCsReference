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
    // PPtr field: resolves the EntityId in managed code (the object never
    // crosses to native) and writes the 12-byte LSOI.
    private static unsafe void V2WriteUnityObjectField(ref byte field, byte* dest, NativeBufferContext* ctx, ulong userData)
    {
        object slot = Unsafe.As<byte, object>(ref field);
        ulong entityId = ResolveUnityObjectEntityIdForWrite(slot, ctx->flags);
        if ((ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0 || entityId == 0UL)
            PackEntityIdIntoLsoi(dest, entityId);
        else
            s_writeEntityIdToBuffer(entityId, ctx->resolverHandle, (IntPtr)dest, ctx->flags);
    }

    // PPtr array: ids resolve in managed code, one native crossing per
    // window-sized batch (src == output).
    private static unsafe void V2WriteUnityObjectArray(byte[] dataAsBytes, int count, uint stride, NativeBufferContext* ctx, ref BufferDataStager stager, ulong userData)
    {
        ConsumeLinearCollectionUnityObjectArray(ctx, dataAsBytes, count, stride, ref stager);
    }
    // PPtr group read (M7b). readEntries uses v1's UnityObjectReadEntry layout
    // and fieldTable its (field, fieldParent) pointer pairs, so the batched
    // native crossing takes both unchanged. baseAddr is unpinned: the
    // multi-entry crossing relies on the non-moving GC (Mono/IL2CPP). CoreCLR
    // and the native-test image read per field, since an Object-returning
    // calli cannot cross and CoreCLR can bind a packed EntityId to a resident
    // wrapper without crossing.
    private static unsafe void V2ReadUnityObjectGroup(object frameInstance, nint frameOffset, ref byte baseAddr, byte* entriesRaw, byte* readEntriesRaw, byte* fieldTableBase, int count, byte* input, NativeReadBufferContext* ctx, ulong userData)
    {
        var entry = (UnityObjectReadEntry*)readEntriesRaw;
        if (count > 1)
        {
            s_readUnityObjectsIntoFields(
                ctx->resolverHandle, ctx->flags,
                (IntPtr)Unsafe.AsPointer(ref baseAddr),
                (IntPtr)entry, (IntPtr)fieldTableBase, (IntPtr)input, count);
            return;
        }
        var end = entry + count;
        int i = 0;
        do
        {
            ref object fieldRef = ref Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset));
            byte* src = input + entry->destOffset;

            object managed = null;

            if (managed != null)
            {
                fieldRef = managed;
            }
            else
            {
                byte* slotBase = fieldTableBase + i * 2 * sizeof(IntPtr);
                IntPtr fieldPtr       = Unsafe.ReadUnaligned<IntPtr>(slotBase);
                IntPtr fieldParentPtr = Unsafe.ReadUnaligned<IntPtr>(slotBase + sizeof(IntPtr));

                fieldRef = ReadUnityObjectFromBuffer(
                    ctx->resolverHandle, (IntPtr)src, entry->klass, ctx->flags,
                    fieldPtr, fieldParentPtr);
            }
            entry++;
            i++;
        }
        while (entry < end);
    }

    // PPtr collection read (M7b). The handler owns the full framing: count,
    // reuse-or-allocate (v1's null/empty behavior: allocate and assign even at
    // count 0), and windowed batched resolves.
    private static unsafe void V2ReadUnityObjectArray(ref byte baseAddr, byte* cmdRaw, NativeReadBufferContext* ctx, byte* pathSegments, byte* pathNames)
    {
        var cmd = (V2CmdExternalArray*)cmdRaw;

        if (ctx->readerEnd - ctx->readerPtr < 4)
            InvokeEnsureReadable(ctx, 4);
        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;

        Type elementType = UnmarshalSystemType((IntPtr)cmd->elementTypeHandle);
        Array arr = AllocateOrReuseArrayBacking(
            ref baseAddr, cmd->kind, cmd->fieldOffset, elementType, count, out byte[] dataAsBytes);

        if (count > 0)
        {
            int wire = (int)cmd->elementWireBytes;
            fixed (byte* dataPtr = dataAsBytes)
            {
                long stride    = (long)cmd->elementStride;
                int  processed = 0;
                while (processed < count)
                {
                    if (ctx->readerEnd - ctx->readerPtr < wire)
                        InvokeEnsureReadable(ctx, wire);
                    int batch = (int)(ctx->readerEnd - ctx->readerPtr) / wire;
                    int remaining = count - processed;
                    if (batch > remaining)
                        batch = remaining;

                    s_readUnityObjectsArrayIntoElements(
                        ctx->resolverHandle, ctx->flags,
                        (IntPtr)(dataPtr + (long)processed * stride), batch, stride,
                        (IntPtr)cmd->readKlass, (IntPtr)cmd->readField, (IntPtr)cmd->readFieldParent,
                        (IntPtr)ctx->readerPtr);

                    ctx->readerPtr += batch * wire;
                    processed += batch;
                }
            }
        }

        AssignArrayBacking(ref baseAddr, cmd->kind, cmd->fieldOffset, arr, dataAsBytes, count, elementType);
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
                object slot = Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)e->fieldOffset));
                Unsafe.WriteUnaligned<ulong>(output + e->destOffset, ResolveUnityObjectEntityIdForWrite(slot, ctx->flags));
                e++;
            }
            while (e < end);
            s_writeUnityObjectEntityIdsToBuffer(ctx->resolverHandle, ctx->flags, (IntPtr)entry, (IntPtr)output, count);
            return;
        }
        do
        {
            object slot = Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset));
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
}
