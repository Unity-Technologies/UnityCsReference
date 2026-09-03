// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;
// EntityId lives in namespace UnityEngine (UnityEngineObject.bindings.cs:141). The
// test-resources compile context (UNITY_NATIVE_TEST_RESOURCES) provides a stub in the
// same namespace via Runtime/Testing/ScriptWithManagedRefTestFixture.Resources_cs, so
// the bare `EntityId` field type below resolves identically in both compile contexts.
using UnityEngine;

namespace UnityEngine.Serialization;

internal static unsafe partial class SerializationBackendManagedCommands
{
    // Walks the entries in [entriesPtr, entriesPtr + entryBufferSize), executing each
    // opcode against the pinned source object and the ctx buffer chain. output (the open
    // segment base) and bufferDataStager are threaded by ref so the inline FBP / String /
    // VRT cases claim per-segment destinations and recursive calls (per-element collection
    // bodies, VRT bodies) accumulate writer-tail bytes across iterations.
    //
    // The stager is owned by the outermost caller (ObjectsToSerializationBuffer), which
    // issues the single trailing flush after the run loop; inner recursive callers must
    // not flush on return — doing so would emit one P/Invoke per nested element/instance,
    // which is exactly what threading the stager by ref avoids.
    private static unsafe void ExecuteWriteCommands(
        NativeBufferContext* ctx,
        IntPtr pinnedBase,
        IntPtr entriesPtr,
        int    entryBufferSize,
        IntPtr transfer,
        ref byte* output,
        ref BufferDataStager bufferDataStager,
        int  repeatCount,
        long repeatStride,
        IntPtr fuidCtxForElements = default)
    {
        byte* basePtr      = (byte*)pinnedBase;
        byte* entriesStart = (byte*)entriesPtr;
        byte* endPos       = entriesStart + entryBufferSize;

        // repeatCount==1, repeatStride==0 (single-instance callers) folds away under the JIT.
        for (int elem = 0; elem < repeatCount; ++elem)
        {
            if (fuidCtxForElements != IntPtr.Zero)
                SetFUIDCurrentArrayIndex(fuidCtxForElements, elem);
            ref byte baseAddr = ref Unsafe.AsRef<byte>(basePtr + (long)elem * repeatStride);
            byte* pos = entriesStart;

            while (pos < endPos)
            {
            var opCode = (RttiDataType)pos[0];

            // Aligned DC2/DC4/DC8 variants store offset / N; the execution side multiplies by N
            // and uses direct typed reads (Unsafe.As<byte, T>(ref ...)) -- single mov on Mono
            // and CoreCLR. _Unaligned variants store the raw offset and use Unsafe.ReadUnaligned /
            // WriteUnaligned because the typed reads would fault on strict-alignment (ARM) targets
            // when the field is not N-aligned (e.g. StructLayout.Pack=1, LayoutKind.Explicit).
            switch (opCode)
            {
                // ---- Compact aligned ----
                case RttiDataType.DirectCopy1:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        // Destination is always 4-byte aligned; widen the 1B source into an int store.
                        *(int*)(output + entry->destOffset) = Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 2;
                        nint destOffset = (nint)entry->destOffset * 2;
                        *(int*)(output + destOffset) = Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 4;
                        nint destOffset = (nint)entry->destOffset * 4;
                        *(int*)(output + destOffset) = Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 8;
                        nint destOffset = (nint)entry->destOffset * 8;
                        // Unaligned: SelectDirectCopyOpCode's destOffset%8 gate only proves
                        // segment-relative alignment, not absolute address alignment. The
                        // segment base (output) can be 4-byte aligned (e.g. inside per-element
                        // bodies after a linear collection's 4B count prefix), which would
                        // SIGBUS on armv7 with a typed 8B store. Build-side fix pending.
                        Unsafe.WriteUnaligned(output + destOffset, Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Compact unaligned ----
                case RttiDataType.DirectCopy2_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, (int)Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<long>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large aligned ----
                case RttiDataType.DirectCopy1_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        *(int*)(output + entry->destOffset) = Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 2;
                        uint destOffset = entry->destOffset * 2;
                        *(int*)(output + destOffset) = Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 4;
                        uint destOffset = entry->destOffset * 4;
                        *(int*)(output + destOffset) = Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 8;
                        uint destOffset = entry->destOffset * 8;
                        // Unaligned: see DirectCopy8 above for the segment-base alignment caveat.
                        Unsafe.WriteUnaligned(output + destOffset, Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large unaligned ----
                case RttiDataType.DirectCopy2_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, (int)Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<long>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.FixedBlockPrefix:
                {
                    var prefix = (ManagedCommandFixedBlockPrefix*)pos;
                    int segmentSize = prefix->payloadSize;
                    pos += sizeof(ManagedCommandFixedBlockPrefix);

                    // Open the next segment at the staging tail (Reserve flushes only if it
                    // won't fit). The DirectCopy entries that follow index off output.
                    output = bufferDataStager.Reserve(segmentSize);
                    break;
                }

                // dst is 4-byte aligned but not 8-byte, so the null-LSOI
                // pathID write below uses WriteUnaligned to stay UB-free.
                case RttiDataType.UnityObject:
                {
                    var entry = ConsumeDirectCopyGroup<UnityObjectWriteEntry>(ref pos, out var end);
                    // Resolve the EntityId in managed (ResolveUnityObjectEntityIdForWrite applies the
                    // UUM-143556 drop) and serialize the id — the object never crosses to native.
                    // Mirrors the EntityId case: pack/None inline, remapped id via WriteEntityIdToBuffer.
                    bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
                    // Remap batch (≥2 fields): pre-write each id into the low bytes of its output slot,
                    // then one native crossing resolves them in place — object-free batching (c0b1c42).
                    if (!packInLSOI && end - entry > 1)
                    {
                        var e = entry;
                        do
                        {
                            object slot = Unsafe.As<byte, object>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)e->fieldOffset));
                            Unsafe.WriteUnaligned<ulong>(output + e->destOffset, ResolveUnityObjectEntityIdForWrite(slot, ctx->flags));
                            e++;
                        }
                        while (e < end);
                        s_writeUnityObjectEntityIdsToBuffer(ctx->resolverHandle, ctx->flags, (IntPtr)entry, (IntPtr)output, (int)(end - entry));
                        break;
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
                    break;
                }

                case RttiDataType.UnityObjectArray:
                {
                    var hdr = (UnityObjectArrayHeader*)pos;
                    pos += sizeof(UnityObjectArrayHeader);

                    // The helper stages the count, which rides the first body batch's flush.
                    byte[] dataAsBytes;
                    int    count;
                    if (hdr->kind == LinearCollectionKind.Array)
                    {
                        Array arr = Unsafe.As<byte, Array>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)hdr->fieldOffset));
                        if (arr == null)
                        {
                            Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), 0);
                            break;
                        }
                        dataAsBytes = Unsafe.As<Array, byte[]>(ref arr);
                        count       = arr.Length;
                    }
                    else
                    {
                        ListLayout list = Unsafe.As<byte, ListLayout>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)hdr->fieldOffset));
                        if (list == null || list._items == null)
                        {
                            Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), 0);
                            break;
                        }
                        dataAsBytes = list._items;
                        count       = list._size;
                    }

                    ConsumeLinearCollectionUnityObjectArray(ctx, dataAsBytes, count, hdr->elementStride, ref bufferDataStager);
                    break;
                }

                case RttiDataType.EntityIdArray:
                {
                    var hdr = (EntityIdArrayHeader*)pos;
                    pos += sizeof(EntityIdArrayHeader);

                    // The helper stages the count, which rides the first body batch's flush.
                    byte[] dataAsBytes;
                    int    count;
                    if (hdr->kind == LinearCollectionKind.Array)
                    {
                        Array arr = Unsafe.As<byte, Array>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)hdr->fieldOffset));
                        if (arr == null)
                        {
                            Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), 0);
                            break;
                        }
                        dataAsBytes = Unsafe.As<Array, byte[]>(ref arr);
                        count       = arr.Length;
                    }
                    else
                    {
                        ListLayout list = Unsafe.As<byte, ListLayout>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)hdr->fieldOffset));
                        if (list == null || list._items == null)
                        {
                            Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), 0);
                            break;
                        }
                        dataAsBytes = list._items;
                        count       = list._size;
                    }

                    ConsumeLinearCollectionEntityIdArray(ctx, dataAsBytes, count, hdr->elementStride, ref bufferDataStager);
                    break;
                }

                case RttiDataType.ManagedReferenceArray:
                {
                    var hdr = (ManagedReferenceArrayHeader*)pos;
                    pos += sizeof(ManagedReferenceArrayHeader);

                    int count;
                    if (hdr->kind == LinearCollectionKind.Array)
                    {
                        Array arr = Unsafe.As<byte, Array>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)hdr->fieldOffset));
                        count = arr == null ? 0 : arr.Length;
                    }
                    else
                    {
                        ListLayout list = Unsafe.As<byte, ListLayout>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)hdr->fieldOffset));
                        count = (list == null || list._items == null) ? 0 : list._size;
                    }

                    ConsumeLinearCollectionManagedReferenceArray(ctx, count, ref bufferDataStager);
                    break;
                }

                // [SerializeReference] inline RefId. Reuses the UnityObject write
                // group shape (UnityObjectWriteEntry: {fieldOffset, destOffset}), but
                // the field is NOT read: the gather pass resolved every SR field's
                // inline RefId (incl. the missing-type upgrade) in field order and the
                // icall pops the next one from the per-object cursor on transferState.
                // The native SR-collection arm advances the same cursor, so scalar and
                // collection SR fields stay in lockstep. fieldOffset is unused here
                // (kept for the shared entry shape). transferState is non-null whenever
                // this opcode is emitted (build only emits it for SR fields).
                case RttiDataType.ManagedReference:
                {
                    var entry = ConsumeDirectCopyGroup<UnityObjectWriteEntry>(ref pos, out var end);
                    do
                    {
                        byte* dst = output + entry->destOffset;
                        WriteManagedReferenceToBuffer(ctx->transferState, (IntPtr)dst);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.EntityId:
                {
                    var entry = ConsumeDirectCopyGroup<EntityIdWriteEntry>(ref pos, out var end);
                    // Clone transfers (and EntityId.None) encode the id in managed code;
                    // serialized-file transfers map it through the native resolver, which
                    // also records the dependency (WriteEntityIdToBuffer).
                    bool packInLSOI = (ctx->flags & UnityObjectTransferFlags.PackEntityIdInLSOI) != 0;
                    // Remap arm: ≥2 fields in one crossing; count==1 takes the per-field codec. Bytes match.
                    if (!packInLSOI && end - entry > 1)
                    {
                        s_writeEntityIdsToBuffer(
                            ctx->resolverHandle, ctx->flags,
                            (IntPtr)Unsafe.AsPointer(ref baseAddr),
                            (IntPtr)entry, (IntPtr)output, (int)(end - entry));
                        break;
                    }
                    do
                    {
                        ref byte fieldByteRef = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);
                        ulong entityId = Unsafe.ReadUnaligned<ulong>(ref fieldByteRef);
                        byte* dst = output + entry->destOffset;

                        if (packInLSOI || entityId == 0UL)
                            PackEntityIdIntoLsoi(dst, entityId);
                        else
                            s_writeEntityIdToBuffer(entityId, ctx->resolverHandle, (IntPtr)dst, ctx->flags);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.DirectCopyBlock:
                    // DirectCopyBlock is a build-time-only opcode: the accumulator
                    // (AppendToManagedBlock in ManagedBlockAccumulator.h) decomposes it
                    // into DirectCopy8 / DirectCopy4 entries before flushing. Reaching it
                    // here means the builder failed to decompose, or a producer is writing
                    // raw DirectCopyBlock entries into the byte stream — both are bugs.
                    throw new InvalidOperationException(
                        "DirectCopyBlock should never appear in the executed byte stream; "
                        + "the accumulator decomposes it into DirectCopy{4,8} entries.");

                case RttiDataType.String:
                    // Stages the framed string at writerPtr + staged, so it joins the
                    // surrounding fixed segments in one trailing flush.
                    ConsumeString(ctx, ref baseAddr, ref pos, ref bufferDataStager);
                    break;

                case RttiDataType.ValueReferenceType:
                    // Recurse with the stager threaded through, so a child's segments
                    // coalesce with the surrounding flow instead of flushing per child.
                    ConsumeValueReference(ctx, ref baseAddr, transfer, ref output, ref bufferDataStager, ref pos);
                    break;

                case RttiDataType.NativeValueStruct:
                {
                    var entry = (ManagedCommandNativeValueStructEntry*)pos;
                    pos += sizeof(ManagedCommandNativeValueStructEntry);

                    // Native dispatcher writes straight to the writer: flush staged bytes first, resync after.
                    bufferDataStager.FlushStaged(kManagedBlockMaxPayloadSize);

                    // Inline value struct: hand the field's own address to the native
                    // Transfer dispatcher. baseAddr is pinned by the ExecuteWriteCommands
                    // caller, so this interior pointer stays valid for the synchronous call.
                    ref byte nvsField = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);
                    IntPtr nvsFieldPtr = (IntPtr)Unsafe.AsPointer(ref nvsField);

                    ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)entry->fnPtr)(nvsFieldPtr, transfer, IntPtr.Zero);

                    bufferDataStager.ResyncWithNativeBuffer();
                    break;
                }

                case RttiDataType.SimpleNativeType:
                {
                    var entry = (ManagedCommandSimpleNativeTypeEntry*)pos;
                    pos += sizeof(ManagedCommandSimpleNativeTypeEntry);

                    // Native dispatcher writes straight to the writer: flush staged bytes first, resync after.
                    bufferDataStager.FlushStaged(kManagedBlockMaxPayloadSize);

                    ref byte field = ref Unsafe.AddByteOffset(ref baseAddr, (nint)entry->fieldOffset);

                    // entry->userData holds the post-header byte offset of m_Ptr within the
                    // wrapper, computed by the C++ initialiser via scripting_field_get_offset so
                    // it is correct for the active scripting backend (Mono preserves declaration
                    // order; CoreCLR may reorder fields, e.g. placing m_SourceStyle before m_Ptr).
                    object wrapper = Unsafe.As<byte, object>(ref field);

                    IntPtr nativePtr = wrapper != null
                        ? Unsafe.ReadUnaligned<IntPtr>(ref Unsafe.AddByteOffset(
                            ref Unsafe.As<ObjectWrapper>(wrapper).Data,
                            entry->userData))
                        : IntPtr.Zero;

                    ((delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, void>)entry->fnPtr)(nativePtr, transfer, entry->userData);

                    bufferDataStager.ResyncWithNativeBuffer();
                    break;
                }

                // Callbacks emit no wire bytes, so no flush/buffer bookkeeping
                // is needed here.
                case RttiDataType.CallOnBeforeSerializeClass:
                {
                    var header = (CallbackHeader*)pos;
                    pos += sizeof(CallbackHeader);
                    object target = Unsafe.As<byte, object>(
                        ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset));
                    if (target is ISerializationCallbackReceiver receiver)
                        receiver.OnBeforeSerialize();
                    break;
                }

                case RttiDataType.CallOnBeforeSerializeStruct:
                {
                    var header = (CallbackHeader*)pos;
                    pos += sizeof(CallbackHeader);
                    if (header->methodFnPtr != IntPtr.Zero)
                    {
                        ref byte structData = ref Unsafe.AddByteOffset(ref baseAddr, header->fieldOffset);
                        ((delegate*<ref byte, void>)header->methodFnPtr)(ref structData);
                    }
                    break;
                }

                case RttiDataType.Array:
                case RttiDataType.List:
                    // ConsumeLinearCollection owns the bufferDataStager per-arm: the trivially-
                    // copyable bulk body coalesces (stages when it fits); the per-element /
                    // shuffle arms flush first since their count + body land at writerPtr
                    // (offset 0).
                    ConsumeLinearCollection(ctx, ref baseAddr, transfer, ref output, ref bufferDataStager, ref pos);
                    break;

                case RttiDataType.Dictionary:
                    // ConsumeDictionary stages the count; the per-entry bodies coalesce after
                    // it via the same FBP threading as the per-element collection path.
                    ConsumeDictionary(ctx, ref baseAddr, transfer, ref output, ref bufferDataStager, ref pos);
                    break;

                case RttiDataType.FixedBuffer:
                    // ConsumeFixedBuffer stages its framed record, coalescing with any
                    // open segment.
                    ConsumeFixedBuffer(ref baseAddr, ref pos, ref bufferDataStager);
                    break;

                case RttiDataType.PropertyNameId:
                    ConsumePropertyNameEditor(ctx, ref baseAddr, ref pos, ref bufferDataStager);
                    break;

                case RttiDataType.Reference:
                case RttiDataType.DynamicBuffer:
                case RttiDataType.Unknown:
                    throw new NotSupportedException(
                        $"OpCode {(RttiDataType)pos[0]} is not implemented for managed command blocks.");
                default:
                    throw new NotSupportedException($"OpCode {(RttiDataType)pos[0]} not supported");
            }

            // Re-align pos to a 4-byte offset relative to entriesStart before reading the
            // next header: the native writer (FlushManagedBlockToCommandQueue in
            // ManagedBlockAccumulator.h) keeps every DirectCopyGroupHeader 4-byte-aligned
            // relative to entriesPtr, so each DirectCopyLargeEntry array gets the alignment
            // its uint fields need. Only compact groups with an odd entry count need this
            // fixup (2-byte skip); large groups are already a multiple of 4. (Source-side
            // reads at baseAddr + fieldOffset may still be unaligned — managed class layout
            // is outside this writer's control — and are handled separately.)
            long entryOffset = pos - entriesStart;
            long aligned = (entryOffset + 3) & ~3L;
            pos = entriesStart + aligned;
        }
        }

        // No trailing flush: the stager's staged bytes are owned by the outermost caller
        // (ObjectsToSerializationBuffer). Inner callers and collection batches inherit
        // accumulated bytes so consecutive elements / nested instances coalesce.
    }

}
