// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 EntityId extension handlers (doc §2.4), serving
// LazyLoadReference<T>. The 12-byte LSOI wire format is shared with
// UnityObject, but the id is read from the field slot as a ulong without
// resolving an object. No group handler is registered; the executor's
// per-entry fieldHandler fallback produces the same bytes as the batched
// remap crossing.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Scalar field. Clone transfers (and EntityId.None) encode the id in
    // managed code; serialized-file transfers map it through the native
    // resolver, which also records the dependency.
    private static unsafe void V2WriteEntityIdField(ref byte field, byte* dest, NativeBufferContext* ctx, ulong userData)
    {
        ulong entityId = Unsafe.ReadUnaligned<ulong>(ref field);
        if ((ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0 || entityId == 0UL)
            PackEntityIdIntoLsoi(dest, entityId);
        else
            s_writeEntityIdToBuffer(entityId, ctx->resolverHandle, (IntPtr)dest, ctx->flags);
    }

    // LazyLoadReference<T>[] / List<>: count prefix plus 12-byte LSOIs. Ids
    // are read from the pinned backing array, one native crossing per batch on
    // the remap arm.
    private static unsafe void V2WriteEntityIdArray(byte[] dataAsBytes, int count, uint stride, NativeBufferContext* ctx, ref BufferDataStager stager, ulong userData)
    {
        ConsumeLinearCollectionEntityIdArray(ctx, dataAsBytes, count, stride, ref stager);
    }

    // Group read (M7b). No read table: the 8-byte write entries are
    // layout-identical to v1's EntityIdReadEntry, and the id decodes without
    // resolving an object (a value store with no write barrier, safe on every
    // GC).
    private static unsafe void V2ReadEntityIdGroup(object frameInstance, nint frameOffset, ref byte baseAddr, byte* entriesRaw, byte* readEntriesRaw, byte* fieldTableBase, int count, byte* input, NativeReadBufferContext* ctx, ulong userData)
    {
        var entry = (V2ExternalFixedEntry*)entriesRaw;
        bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
        // Remap arm: two or more fields in one crossing on all runtimes.
        if (!packInLSOI && count > 1)
        {
            s_readEntityIdsIntoFields(
                ctx->resolverHandle, ctx->flags,
                (IntPtr)Unsafe.AsPointer(ref baseAddr),
                (IntPtr)entry, (IntPtr)input, count);
            return;
        }
        var end = entry + count;
        do
        {
            byte* src = input + entry->destOffset;
            ulong entityId = packInLSOI
                ? UnpackEntityIdFromLsoi(src)
                : s_readEntityIdFromBuffer(ctx->resolverHandle, (IntPtr)src, ctx->flags);
            ref byte fieldByteRef = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);
            Unsafe.WriteUnaligned<ulong>(ref fieldByteRef, entityId);
            entry++;
        }
        while (entry < end);
    }

    // Collection read (M7b). The handler owns the full framing.
    private static unsafe void V2ReadEntityIdArray(ref byte baseAddr, byte* cmdRaw, NativeReadBufferContext* ctx, byte* pathSegments, byte* pathNames)
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
            bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
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

                    if (packInLSOI)
                    {
                        // Clone arm: unpack each id inline (no crossing).
                        for (int i = 0; i < batch; ++i)
                        {
                            ulong id = UnpackEntityIdFromLsoi(ctx->readerPtr + i * wire);
                            Unsafe.WriteUnaligned<ulong>(dataPtr + (long)(processed + i) * stride, id);
                        }
                    }
                    else
                    {
                        // Remap arm: whole batch in one crossing into the pinned backing.
                        s_readEntityIdsArrayIntoElements(
                            ctx->resolverHandle, ctx->flags,
                            (IntPtr)(dataPtr + (long)processed * stride), batch, stride,
                            (IntPtr)ctx->readerPtr);
                    }

                    ctx->readerPtr += batch * wire;
                    processed += batch;
                }
            }
        }

        AssignArrayBacking(ref baseAddr, cmd->kind, cmd->fieldOffset, arr, dataAsBytes, count, elementType);
    }
}
