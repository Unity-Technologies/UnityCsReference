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
    // Consumes one ManagedCommandLinearCollection entry. Three paths share
    // the same header + length prefix, then diverge:
    //   - Trivially-copyable: count*elementStride raw bytes streamed in
    //     one or more chunks, plus a 0..3 byte tail pad.
    //   - Shuffle path: per-element body is purely DC + FBP and fits in
    //     a single segment. Reserves count*elementWireSize in batches
    //     sized to the writable region, runs the DC entries once per
    //     element against a fixed per-element destination — no per-element
    //     ExecuteWriteCommands frame, no per-element FBP segment claim.
    //     Wire output is byte-identical to the per-element recursion path.
    //   - Per-element recursion (general fallback): nestedByteCount bytes
    //     of FBP-bracketed body executed once per element via
    //     ExecuteWriteCommands with an element-pinned base.
    //
    // Null array / null List → write a 0 length prefix and return (a null
    // collection serialises as a zero-length empty one).
    private static unsafe void ConsumeLinearCollection(
        NativeBufferContext* ctx, ref byte baseAddr, IntPtr transfer,
        ref byte* output, ref BufferDataStager bufferDataStager, ref byte* pos)
    {
        var header = (LinearCollectionHeader*)pos;
        pos += sizeof(LinearCollectionHeader);
        byte* nestedStart = pos;
        int   nestedBytes = (int)header->nestedByteCount;

        // Resolve the underlying byte[] for pinning (any T[] is pinnable as
        // byte[] — the SZArray pinning helper computes the same data offset
        // regardless of element type) and the element count.
        byte[] dataAsBytes;
        int    count;
        if (header->kind == LinearCollectionKind.Array)
        {
            // Field at (baseAddr + fieldOffset) holds a T[] reference.
            Array arr = Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
            if (arr == null)
            {
                Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), 0);
                pos = nestedStart + nestedBytes;
                return;
            }
            dataAsBytes = Unsafe.As<Array, byte[]>(ref arr);
            count       = arr.Length;
        }
        else
        {
            // Field at (baseAddr + fieldOffset) holds a List<T> reference.
            // Reinterpret as ListLayout to read _items + _size in one shot.
            ListLayout list = Unsafe.As<byte, ListLayout>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
            if (list == null || list._items == null)
            {
                Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), 0);
                pos = nestedStart + nestedBytes;
                return;
            }
            dataAsBytes = list._items;
            count       = list._size;
        }

        if ((header->flags & LinearCollectionFlags.TriviallyCopyable) != 0)
        {
            long  totalBytesL = (long)count * (long)header->elementStride;
            // Wire format: SInt32 length then count*elementStride raw bytes, padded to 4;
            // element counts above int.MaxValue are not representable.
            int   totalBytes  = checked((int)totalBytesL);
            int   padBytes    = (4 - (totalBytes & 3)) & 3;
            int   framedSize  = 4 + totalBytes + padBytes;

            // Stage the whole framed array (count + body + pad) when it fits a window, so a
            // small or empty blittable array coalesces with the surrounding segments.
            byte* dst = bufferDataStager.TryReserve(framedSize);
            if (dst != null)
            {
                Unsafe.WriteUnaligned(dst, count);
                if (totalBytes > 0)
                {
                    fixed (byte* dataPtr = dataAsBytes)
                        Buffer.MemoryCopy(dataPtr, dst + 4, totalBytes, totalBytes);
                }
                if (padBytes > 0)
                    Unsafe.InitBlockUnaligned(dst + 4 + totalBytes, 0, (uint)padBytes);
                pos = nestedStart + nestedBytes;
                return;
            }

            // Body exceeds a whole window: TryReserve has committed the staged bytes
            // (staged == 0). Frame the count, hand the pinned source to FlushBuffer's spill
            // arm (streams `totalBytes` through any number of cache-writer blocks in one
            // call), then the tail pad.
            fixed (byte* dataPtr = dataAsBytes)
            {
                Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), count);
                bufferDataStager.FlushStaged(kManagedBlockMaxPayloadSize);
                bufferDataStager.Bulk(dataPtr, totalBytes);
            }
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(bufferDataStager.Reserve(padBytes), 0, (uint)padBytes);

            pos = nestedStart + nestedBytes;
            return;
        }

        // The trivially-copyable arm returned above; reaching here means a per-element
        // element type. Both the shuffle path and the per-element path below stage everything
        // through the cursor — the count, the element bodies, and the tail pad all coalesce
        // and ride the surrounding flow's flushes, so neither needs a flush of its own.
        if ((header->flags & LinearCollectionFlags.ShufflePath) != 0)
        {
            ConsumeLinearCollectionShufflePath(
                ctx, dataAsBytes, count,
                (long)header->elementStride,
                (int)header->elementWireSize,
                nestedStart, nestedBytes,
                ref bufferDataStager);
            pos = nestedStart + nestedBytes;
            return;
        }

        // Per-element recursion: stage the SInt32 length, then walk each element's command
        // stream (the element class's FBP-bracketed DC + String entries) through the same
        // cursor with the element pinned.
        Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), count);

        if (count > 0)
        {
            fixed (byte* dataPtr = dataAsBytes)
            {
                long stride = (long)header->elementStride;
                bool pushedArrayIdx = ctx->fuidContext != IntPtr.Zero;
                if (pushedArrayIdx)
                    PushFUIDArrayIndex(ctx->fuidContext, 0);
                try
                {
                    // Threading the stager by ref + repeatCount lets ExecuteWriteCommands walk
                    // each element's body in one call, coalescing an N-element array of small
                    // structs into ceil(N*body/cap) flushes instead of N.
                    ExecuteWriteCommands(ctx, (IntPtr)dataPtr,
                        (IntPtr)nestedStart, nestedBytes, transfer,
                        ref output, ref bufferDataStager,
                        repeatCount: count, repeatStride: stride,
                        fuidCtxForElements: ctx->fuidContext);
                }
                finally
                {
                    if (pushedArrayIdx)
                        PopFUIDArrayIndex(ctx->fuidContext);
                }
            }
        }

        // Pad total wire output to 4-byte alignment, staged after the last element.
        // elementWireSize is 0 for variable-length elements (strings), already aligned.
        int elementWireSize = (int)header->elementWireSize;
        if (elementWireSize > 0)
        {
            int totalWritten = count * elementWireSize;
            int padBytes     = (4 - (totalWritten & 3)) & 3;
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(bufferDataStager.Reserve(padBytes), 0, (uint)padBytes);
        }

        pos = nestedStart + nestedBytes;
    }

    private static unsafe void ConsumeLinearCollectionManagedReferenceArray(
        NativeBufferContext* ctx, int count, ref BufferDataStager bufferDataStager)
    {
        Unsafe.WriteUnaligned(bufferDataStager.Reserve(4), count);

        if (count == 0)
        {
            // Still cross: the icall marks the registry active, as the native command's Activate<>() does.
            WriteManagedReferencesToBuffer(ctx->transferState, IntPtr.Zero, 0);
            return;
        }

        const int kRefIdSize = 8;

        int left = count;
        while (left > 0)
        {
            if (bufferDataStager.StagingRoom < kRefIdSize)
                bufferDataStager.FlushStaged(kManagedBlockMaxPayloadSize);

            int batch = bufferDataStager.StagingRoom / kRefIdSize;
            if (batch > left)
                batch = left;

            WriteManagedReferencesToBuffer(ctx->transferState, (IntPtr)bufferDataStager.StagingPtr, batch);

            bufferDataStager.Stage(batch * kRefIdSize);
            left -= batch;
        }
    }

    // UUM-143556 marker the read path stamps on a type-mismatched fake-null reference. This is the
    // managed write path's own source of truth (the serialization backend is moving to managed; the
    // native path — and its own kTypeMismatchReferenceError in TransferPPtrToMonoObject.cpp — is legacy
    // that will go away). While both exist they must stay byte-identical: the read path stamps with the
    // native copy and the drop below compares against this one, so the round-trip drop test fails if
    // they ever diverge.
    private const string kTypeMismatchReferenceError =
        "The serialized reference's type does not match the field's type; it was removed when building the player (UUM-143556).";


    // Read-path mirror of ConsumeLinearCollection. Reads the count prefix, then
    // routes on the same flag set the writer used:
    //   - Trivially-copyable: bulk memcpy count*elementStride from input into
    //     the freshly allocated array's backing store, then skip the 4-byte
    //     tail pad.
    //   - Shuffle path: input contains count * elementWireSize bytes laid out
    //     as N concatenated per-element bodies; ExecuteReadShuffleBody walks
    //     the FBP-bracketed body once per element with src=input slice and
    //     dst=array element slot, dispatching DC opcodes inline.
    //   - Per-element recursion (general fallback): the FBP-bracketed body is
    //     consumed via ExecuteReadCommands once per element with a fresh
    //     element-pinned baseAddr.
    //
    // The element Type is rebuilt via Type.GetTypeFromHandle from the
    // RuntimeTypeHandle.Value the build side stamped into elementTypeHandle.
    // For List<T> we additionally allocate an uninitialized List<T> and stamp
    // its _items / _size via the same ListLayout reinterpret the write side
    // uses to read them (the _version slot stays at zero — a valid initial
    // state for an uninitialized List<T>). Wire format always emits the
    // header regardless of null source, so a 0-length count leaves the
    // parent's field untouched (default-null).
    private static unsafe void ConsumeLinearCollectionRead(
        NativeReadBufferContext* ctx,
        ref byte baseAddr,
        IntPtr transfer,
        ref byte* pos)
    {
        var header = (LinearCollectionHeader*)pos;
        pos += sizeof(LinearCollectionHeader);
        byte* nestedStart = pos;
        int   nestedBytes = (int)header->nestedByteCount;

        // Count prefix: 4 bytes, sits between segments (no FBP bracketing) and
        // is always present even for null/empty source collections.
        if (ctx->readerAvailable < 4)
            InvokeEnsureReadable(ctx, 4);
        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr      += 4;
        ctx->readerAvailable -= 4;

        // Always allocate and assign the collection, even when count == 0.
        // The wire format collapses null and empty source collections to the
        // same `count == 0` framing (see ConsumeLinearCollection on the write
        // side: both `arr == null` and `arr.Length == 0` stage a count of 0
        // with no body). A non-null zero-length collection
        // is the contract user OnAfterDeserialize callbacks (e.g. UpmCache
        // iterating `m_SerializedProductSearchPackageInfoProductIds.Length`)
        // rely on. Skipping the assignment here would leave the field at its
        // CLR default (null) and silently break that contract — observed as
        // a NullReferenceException in UpmCache.OnAfterDeserialize during
        // code-reload backup restore.
        //
        // The build side stamped the native MethodTable* (via
        // scripting_class_get_type(elementClass).GetBackendPtr()) into
        // elementTypeHandle. UnmarshalSystemType (defined above) routes
        // through RuntimeTypeHandle.FromIntPtr on CoreCLR and through an
        // Unsafe.As reinterpret on Mono — see the helper's docs for why
        // a direct reinterpret can't be used on CoreCLR (RuntimeTypeHandle
        // is a managed-reference struct there, not a raw IntPtr).
        Type elementType = UnmarshalSystemType(header->elementTypeHandle);
        // Reuse the existing backing store when it already holds exactly `count`
        // elements, so [NonSerialized] bytes in struct elements survive the read
        // (e.g. undo restoring a same-length array). Mirrors the short-circuit in
        // ResizeSTLStyleArray / the legacy ArrayOfManagedObjectsTransferer path,
        // which checks element count (not byte capacity) for both arrays and lists.
        Array arr;
        if (header->kind == LinearCollectionKind.Array)
        {
            Array existingArr = Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
            arr = (existingArr != null && existingArr.Length == count)
                ? existingArr
                : Array.CreateInstance(elementType, count);
        }
        else
        {
            ListLayout existingList = Unsafe.As<byte, ListLayout>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset));
            arr = (existingList != null
                && existingList._size == count
                && existingList._items != null
                && existingList._items.Length >= count)
                ? Unsafe.As<byte[], Array>(ref existingList._items)
                : Array.CreateInstance(elementType, count);
        }
        byte[] dataAsBytes = Unsafe.As<Array, byte[]>(ref arr);

        if (count > 0)
        {
            if ((header->flags & LinearCollectionFlags.TriviallyCopyable) != 0)
            {
                long totalBytesL = (long)count * (long)header->elementStride;
                int  totalBytes  = checked((int)totalBytesL);
                int  padBytes    = (4 - (totalBytes & 3)) & 3;
                if (totalBytes > 0)
                {
                    // Bulk-stream straight into the freshly-allocated array's
                    // backing store, bypassing the spill buffer entirely. Drains
                    // any prefix already in readerPtr/readerAvailable, then reads
                    // the remainder directly from the CachedReader.
                    fixed (byte* dataPtr = dataAsBytes)
                    {
                        InvokeReadBytesDirect(ctx, dataPtr, totalBytes);
                    }
                }
                if (padBytes > 0)
                {
                    if (ctx->readerAvailable < padBytes)
                        InvokeEnsureReadable(ctx, padBytes);
                    ctx->readerPtr      += padBytes;
                    ctx->readerAvailable -= padBytes;
                }
            }
            else if ((header->flags & LinearCollectionFlags.ShufflePath) != 0)
            {
                int  elementWireSize = (int)header->elementWireSize;
                long stride          = (long)header->elementStride;

                // Process the wire payload in L1-sized batches: each batch
                // reads its own wire bytes directly from the CachedReader into
                // a stack scratch buffer, then runs the transposed walker
                // against the matching slice of the managed array. Two wins
                // over staging the whole payload in a pooled buffer:
                //   1. Peak memory is just `scratch + managed array`, not
                //      `wireBytes + managed array` — important for large
                //      arrays where wire bytes can be many MB.
                //   2. Both source (scratch, ~kReadShuffleScratchBytes) and
                //      destination (batch slice of the managed array) stay
                //      hot in L1 across all K DC entries of one batch. The
                //      one-shot version strided the dest through the entire
                //      managed array per DC entry, blowing the cache.
                //
                // P/Invoke cost: count / batchElements ReadBytesDirect calls
                // per array. With kReadShuffleScratchBytes = 32 KB and a
                // typical elementWireSize of 60–80 B, batchElements is in the
                // 400–500 range — so a 100k-element array takes ~200–250
                // callbacks. Versus ~8000 if we were chunking through the
                // 1 KB EnsureReadable spill buffer; versus 1 for the prior
                // pooled-buffer version. The middle ground retains the cache
                // win without paying the per-element-EnsureReadable cost.
                //
                // Build-side gate (elementWireSize > 0 and
                // elementWireSize <= kManagedBlockMaxPayloadSize = 256B)
                // makes batchElements >= kReadShuffleScratchBytes / 256 = 128
                // unconditionally on entry.
                const int kReadShuffleScratchBytes = 1024;
                byte* scratch = stackalloc byte[kReadShuffleScratchBytes];
                int batchElements = kReadShuffleScratchBytes / elementWireSize;

                fixed (byte* dataPtr = dataAsBytes)
                {
                    int batchStart   = 0;
                    int elementsLeft = count;
                    while (elementsLeft > 0)
                    {
                        int batch      = (elementsLeft < batchElements) ? elementsLeft : batchElements;
                        int batchBytes = batch * elementWireSize;

                        InvokeReadBytesDirect(ctx, scratch, batchBytes);

                        ExecuteReadShuffleBatch(
                            scratch, dataPtr + (long)batchStart * stride,
                            batch,
                            elementWireSize, stride,
                            nestedStart, nestedBytes);

                        batchStart   += batch;
                        elementsLeft -= batch;
                    }
                }
            }
            else
            {
                // Per-element recursion: each element's body is walked by
                // ExecuteReadCommands with the element pinned. Each call's
                // end-of-stream commit (CommitReadSegment) advances
                // ctx->readerPtr past the element's final segment, stepping
                // naturally to the next element.
                fixed (byte* dataPtr = dataAsBytes)
                {
                    long stride  = (long)header->elementStride;
                    int  segSize = 0;
                    bool pushedArrayIdx = ctx->fuidContext != IntPtr.Zero;
                    if (pushedArrayIdx)
                        PushFUIDArrayIndex(ctx->fuidContext, 0);
                    try
                    {
                        ExecuteReadCommands(
                            ctx,
                            ref Unsafe.AsRef<byte>(dataPtr),
                            nestedStart, nestedBytes,
                            transfer,
                            ref segSize,
                            repeatCount: count, repeatStride: stride,
                            fuidCtxForElements: ctx->fuidContext);
                    }
                    finally
                    {
                        if (pushedArrayIdx)
                            PopFUIDArrayIndex(ctx->fuidContext);
                    }
                }

                // Skip the 0..3-byte tail pad the write side emitted after
                // the per-element loop. Mirrors the trivially-copyable path.
                // elementWireSize is 0 for variable-length elements (strings)
                // — already 4-byte aligned, no pad written.
                int elementWireSize = (int)header->elementWireSize;
                if (elementWireSize > 0)
                {
                    int totalBytes = count * elementWireSize;
                    int padBytes   = (4 - (totalBytes & 3)) & 3;
                    if (padBytes > 0)
                    {
                        if (ctx->readerAvailable < padBytes)
                            InvokeEnsureReadable(ctx, padBytes);
                        ctx->readerPtr      += padBytes;
                        ctx->readerAvailable -= padBytes;
                    }
                }
            }
        }

        if (header->kind == LinearCollectionKind.Array)
        {
            Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset)) = arr;
        }
        else
        {
            // Refill the field's existing List in place (allocate only when null) to
            // preserve List instance identity across a read, matching native
            // LinearCollectionField::SetArray.
            ref byte fieldSlot = ref Unsafe.AddByteOffset(ref baseAddr, (nint)header->fieldOffset);
            ListLayout layout = Unsafe.As<byte, ListLayout>(ref fieldSlot);
            if (layout == null)
            {
                layout = Unsafe.As<ListLayout>(RuntimeHelpers.GetUninitializedObject(GetCachedListType(elementType)));
                Unsafe.As<byte, ListLayout>(ref fieldSlot) = layout;
            }
            layout._items = dataAsBytes;
            layout._size  = count;
        }

        pos = nestedStart + nestedBytes;
    }

    // Cache elementType -> List<elementType> so the expensive MakeGenericType runs once
    // per element type, not on every null-List allocation during read. Keyed/valued by
    // Type, which can pin a user-script ALC after reload; clear it there. Players never
    // code-reload, and the native-test-resources stub doesn't reference Unity.Scripting.
    [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Type>
        s_ListTypeCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, Type>();

    private static Type GetCachedListType(Type elementType) =>
        s_ListTypeCache.GetOrAdd(elementType, t => typeof(List<>).MakeGenericType(t));

    // Allocate or reuse same-length backing; returns Array + reinterpreted byte[] for pinning.
    private static unsafe Array AllocateOrReuseArrayBacking(
        ref byte baseAddr, byte kind, uint fieldOffset, Type elementType, int count, out byte[] dataAsBytes)
    {
        Array arr;
        if (kind == LinearCollectionKind.Array)
        {
            Array existingArr = Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
            arr = (existingArr != null && existingArr.Length == count)
                ? existingArr
                : Array.CreateInstance(elementType, count);
        }
        else
        {
            ListLayout existingList = Unsafe.As<byte, ListLayout>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
            arr = (existingList != null
                && existingList._size == count
                && existingList._items != null
                && existingList._items.Length >= count)
                ? Unsafe.As<byte[], Array>(ref existingList._items)
                : Array.CreateInstance(elementType, count);
        }
        dataAsBytes = Unsafe.As<Array, byte[]>(ref arr);
        return arr;
    }

    private static unsafe void AssignArrayBacking(
        ref byte baseAddr, byte kind, uint fieldOffset, Array arr, byte[] dataAsBytes, int count, Type elementType)
    {
        if (kind == LinearCollectionKind.Array)
        {
            Unsafe.As<byte, Array>(
                ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)) = arr;
        }
        else
        {
            // Refill in place, allocating only when the field is null: a read must not
            // replace the List instance the field holds.
            ref byte fieldSlot = ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset);
            ListLayout layout = Unsafe.As<byte, ListLayout>(ref fieldSlot);
            if (layout == null)
            {
                layout = Unsafe.As<ListLayout>(
                    RuntimeHelpers.GetUninitializedObject(GetCachedListType(elementType)));
                Unsafe.As<byte, ListLayout>(ref fieldSlot) = layout;
            }
            layout._items = dataAsBytes;
            layout._size  = count;
        }
    }

}
