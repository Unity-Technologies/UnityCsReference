// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.Serialization;

// Managed serialization V2: flat read executor (doc §11).
//
// Walks the same per-type template the write executor uses (readCompatible
// templates contain only direction-agnostic commands) and consumes the wire
// through the NativeReadBufferContext contract. kFixedBlock ensures a
// readable window, direct copies load `input + destOffset` and store narrow
// (1 or 2 bytes from the write side's 4-byte-widened slots), collections
// allocate with v1's reuse-or-allocate contract, and EnterObject materializes
// on null like the write side.
//
// Pinning policy matches the write executor: the host is unpinned and
// addressed through GC-tracked refs, and scoped array pins wrap only the bulk
// readBytesDirect crossings.
//
// The frame stack and layout facts live in SharedUtilities.cs, out-of-loop
// helpers in ReadUtilities.cs, and the framed-string read in Strings.cs.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Entry point for ExecuteV2Read (ManagedSerialization.cpp). Same EH split
    // as the write executor: the frame-clearing finally lives here so the
    // dispatch loop's locals stay enregistered.
    [RequiredByNativeCode]
    public static unsafe void SerializationBufferToObjectsV2(
        object host,
        IntPtr streamStart,
        IntPtr readContext)
    {
        // Every published stream leads with its header command; the dispatch
        // loop starts past it and runs until the End terminator.
        var streamHeader = (V2CmdStreamHeader*)streamStart;
        int maxFrameDepth = streamHeader->maxFrameDepth;
        V2ObjectFrame[] frames = TakeV2Frames(maxFrameDepth);
        frames[0].Instance = host;
        frames[0].Offset = 0;
        int openDictFrames = 0;
        try
        {
            // The wrapper owns the host kind: it seeds frame 0 and hands the
            // dispatch loop the host's field base. The unmanaged wrapper passes
            // an empty frame span and a ref into unmanaged memory instead.
            ExecuteV2ReadStream((byte*)(streamHeader + 1),
                (NativeReadBufferContext*)readContext, frames,
                ref V2RebaseTo(in frames[0]), ref openDictFrames);
        }
        finally
        {
            Array.Clear(frames, 0, maxFrameDepth);
            ReturnV2Frames(frames);
            // Rebalance the thread-local FUID dict-frame stack if a transfer
            // threw mid-dictionary (the write wrapper's shape).
            while (openDictFrames > 0)
            {
                PopDictionaryFUIDFrame();
                --openDictFrames;
            }
        }
    }

    // Unmanaged-base entry (isUnmanagedSafe templates only). See WriteExecutor's
    // unmanaged entry for the invariant.
    [RequiredByNativeCode]
    public static unsafe void SerializationBufferToObjectsV2Unmanaged(
        IntPtr unmanagedBase,
        IntPtr streamStart,
        IntPtr readContext)
    {
        var streamHeader = (V2CmdStreamHeader*)streamStart;
        int openDictFrames = 0;
        ExecuteV2ReadStream((byte*)(streamHeader + 1),
            (NativeReadBufferContext*)readContext,
            Span<V2ObjectFrame>.Empty,
            ref Unsafe.AsRef<byte>((void*)unmanagedBase),
            ref openDictFrames);
        UnityEngine.Assertions.Assert.IsTrue(openDictFrames == 0,
            "isUnmanagedSafe templates cannot push dictionary FUID frames");
    }

    // The flat dispatch loop over the wrapper-seeded frame stack (frame 0
    // holds the host) and the host's field base. No EH in here; see the
    // wrapper.
    private static unsafe void ExecuteV2ReadStream(
        byte* streamStart,
        NativeReadBufferContext* ctx,
        Span<V2ObjectFrame> frames,
        ref byte baseAddr,
        ref int openDictFrames)
    {
        byte* pathSegments = ctx->pathSegments;
        byte* pathNames = ctx->pathNames;
        // Open element-source loops whose FUID frame push succeeded, one bit
        // per frame depth of the staging frame; consulted at the loop's exit.
        ulong pushedDictFrames = 0;
        {
            // No range check so the unmanaged entry can pass an empty span;
            // frame-touching opcodes are rejected by the compose gate.
            ref V2ObjectFrame top = ref MemoryMarshal.GetReference(frames);

            // Base of the fixed block currently windowed at the reader; copy
            // entries index off it. The window stays valid until the next
            // ensureReadable, and every entry of the block executes before that.
            byte* input = null;

            byte* pos = streamStart;
            while (true)
            {
                // Header-owned advancement: every command carries its total
                // size (entry arrays and padding included) in 4-byte units.
                // Control-flow arms (repeat skip/backedge) override next; the
                // End terminator returns.
                byte* next = pos + (*(ushort*)(pos + 2) << 2);
                int ensureSpace;
                switch ((V2OpCode)pos[0])
                {
                    case V2OpCode.FixedBlock:
                    {
                        var cmd = (V2CmdFixedBlock*)pos;
                        // Unchecked capture (doc §12): the preceding replenish
                        // guaranteed this block's bytes.
                        input = ctx->readerPtr;
                        ctx->readerPtr += (int)cmd->wireBytes;
                        break;
                    }

                    case V2OpCode.FixedBlockGuarded:
                    {
                        // The self-guaranteeing block form (doc §12): ensures
                        // its own window plus the trailing static sum.
                        var cmd = (V2CmdFixedBlockGuarded*)pos;
                        if (ctx->readerEnd - ctx->readerPtr < (int)cmd->ensureSum)
                            InvokeEnsureReadable(ctx, (int)cmd->ensureSum);
                        input = ctx->readerPtr;
                        ctx->readerPtr += (int)cmd->wireBytes;
                        break;
                    }

                    case V2OpCode.EnsureWriteRoom:
                        // Write-side window re-arm (doc §12); reads are
                        // covered by the stamped ensure sums.
                        break;

                    case V2OpCode.DirectCopy4:
                    {
                        // Typed store: the lowering routes every field offset
                        // that is not 4-aligned to DirectCopy4Unaligned, so
                        // this arm's offset is aligned by contract.
                        var cmd = (V2CmdDirectCopy*)pos;
                        Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset)) =
                            Unsafe.ReadUnaligned<int>(input + cmd->destOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy4Unaligned:
                    {
                        // Pack/Explicit-layout field offsets: an unaligned
                        // store, because a typed one faults on
                        // strict-alignment ARM.
                        var cmd = (V2CmdDirectCopy*)pos;
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset),
                            Unsafe.ReadUnaligned<int>(input + cmd->destOffset));
                        break;
                    }

                    case V2OpCode.DirectCopyRun:
                    {
                        var cmd = (V2CmdDirectCopyRun*)pos;
                        Unsafe.CopyBlockUnaligned(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset),
                            ref Unsafe.AsRef<byte>(input + cmd->destOffset),
                            cmd->byteCount);
                        break;
                    }

                    case V2OpCode.DirectCopy4N_8:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 2))) =
                                Unsafe.ReadUnaligned<int>(input + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy4N_8x4:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;  // multiple of 4 by construction
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; i += 4)
                        {
                            Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 2))) =
                                Unsafe.ReadUnaligned<int>(input + (e[i].destOffset << 2));
                            Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i + 1].fieldOffset << 2))) =
                                Unsafe.ReadUnaligned<int>(input + (e[i + 1].destOffset << 2));
                            Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i + 2].fieldOffset << 2))) =
                                Unsafe.ReadUnaligned<int>(input + (e[i + 2].destOffset << 2));
                            Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i + 3].fieldOffset << 2))) =
                                Unsafe.ReadUnaligned<int>(input + (e[i + 3].destOffset << 2));
                        }
                        break;
                    }

                    case V2OpCode.DirectCopy4N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 2))) =
                                Unsafe.ReadUnaligned<int>(input + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy4N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset << 2)) =
                                Unsafe.ReadUnaligned<int>(input + ((nint)e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_8:
                    {
                        // The write side stored zero-extended 4-byte slots; the
                        // value is the slot's low ushort.
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 1))) =
                                Unsafe.ReadUnaligned<ushort>(input + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 1))) =
                                Unsafe.ReadUnaligned<ushort>(input + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset << 1)) =
                                Unsafe.ReadUnaligned<ushort>(input + ((nint)e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy1N_8:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset) = *(input + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy1N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset) = *(input + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy1N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset) = *(input + ((nint)e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy1:
                    {
                        var cmd = (V2CmdDirectCopy*)pos;
                        // The write side stored the byte zero-extended into a
                        // 4-byte slot; the value is the slot's low byte.
                        Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset) = *(input + cmd->destOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy2:
                    {
                        // Typed store; 2-aligned by contract (see DirectCopy4).
                        var cmd = (V2CmdDirectCopy*)pos;
                        Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset)) =
                            Unsafe.ReadUnaligned<ushort>(input + cmd->destOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy2Unaligned:
                    {
                        // Odd field offsets; see DirectCopy4Unaligned.
                        var cmd = (V2CmdDirectCopy*)pos;
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset),
                            Unsafe.ReadUnaligned<ushort>(input + cmd->destOffset));
                        break;
                    }

                    case V2OpCode.EnterObject:
                    {
                        var cmd = (V2CmdEnterObject*)pos;
                        ref object slot = ref Unsafe.As<byte, object>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        object child = slot;
                        if (child == null)
                        {
                            // The try wraps only the calli: the instance
                            // already exists on a throw and the transfer
                            // continues with it (v1's contract), and the
                            // narrow region pins no dispatch-loop state.
                            var ctor = (V2Constructor)SerializationCommandObjectTable.Get(cmd->constructorIndex);
                            child = RuntimeHelpers.GetUninitializedObject(ctor.Type);
                            try
                            {
                                // Unconditional; a null ctor is its own opcode,
                                // resolved at compose time (EnterObjectNoCtor).
                                ((delegate*<object, void>)ctor.CtorFunctionPtr)(child);
                            }
                            catch (Exception e)
                            {
                                // Exception policy lives in Instances.cs.
                                s_V2CommandExceptionHandler(e);
                            }
                            slot = child;
                        }

                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = child;
                        top.Offset = 0;
                        baseAddr = ref Unsafe.As<ObjectWrapper>(child).Data;
                        break;
                    }

                    case V2OpCode.EnterObjectNoCtor:
                    {
                        var cmd = (V2CmdEnterObject*)pos;
                        // Class without a parameterless ctor (v1's optional-
                        // ctor contract): allocation only, nothing can throw.
                        ref object slot = ref Unsafe.As<byte, object>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        object child = slot;
                        if (child == null)
                        {
                            var ctor = (V2Constructor)SerializationCommandObjectTable.Get(cmd->constructorIndex);
                            child = slot = RuntimeHelpers.GetUninitializedObject(ctor.Type);
                        }

                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = child;
                        top.Offset = 0;
                        baseAddr = ref Unsafe.As<ObjectWrapper>(child).Data;
                        break;
                    }

                    case V2OpCode.EnterObjectFallback:
                    {
                        var cmd = (V2CmdEnterObjectFallback*)pos;
                        ref object slot = ref Unsafe.As<byte, object>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        object child = slot;
                        if (child == null)
                            child = slot = V2CreateInstanceFallback((IntPtr)cmd->runtimeTypeHandle, (IntPtr)cmd->ctorFunctionPtr);

                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = child;
                        top.Offset = 0;
                        baseAddr = ref Unsafe.As<ObjectWrapper>(child).Data;
                        break;
                    }

                    case V2OpCode.EnterObjectFactory:
                    {
                        var cmd = (V2CmdEnterObjectFactory*)pos;
                        ref object slot = ref Unsafe.As<byte, object>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        object child = slot;
                        if (child == null)
                        {
                            // One calli owns allocate-and-construct. The catch
                            // materializes the uninitialized instance out of
                            // line, so the transfer continues with it (v1's
                            // throwing-ctor contract) and the narrow region
                            // pins no dispatch-loop state.
                            try
                            {
                                child = ((delegate*<object>)cmd->factoryFunctionPtr)();
                            }
                            catch (Exception e)
                            {
                                child = V2FactoryThrowFallback(cmd->constructorIndex, e);
                            }
                            slot = child;
                        }

                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = child;
                        top.Offset = 0;
                        baseAddr = ref Unsafe.As<ObjectWrapper>(child).Data;
                        break;
                    }

                    case V2OpCode.ExitObject:
                    {
                        top.Instance = null;
                        top = ref Unsafe.Subtract(ref top, 1);
                        baseAddr = ref V2RebaseTo(in top);
                        break;
                    }

                    case V2OpCode.String:
                    {
                        var cmd = (V2CmdString*)pos;
                        // The trailing guarantee folds into the read's body
                        // ensure (doc §12): one branch for in-window strings.
                        Unsafe.As<byte, string>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset)) =
                            V2ReadFramedString(ctx, cmd->ensureSum);
                        break;
                    }

                    case V2OpCode.LinearCollectionMemCpy:
                    {
                        var cmd = (V2CmdLinearCollectionMemCpy*)pos;

                        // Unchecked count (doc §12).
                        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
                        ctx->readerPtr += 4;

                        byte[] dataAsBytes = V2ReuseOrAllocateCollection(
                            ref baseAddr, cmd->fieldOffset, (byte)(cmd->kind & kV2Pad0KindMask), cmd->elementTypeHandle, count);

                        if (count > 0)
                        {
                            long totalBytesL = (long)count * cmd->elementStride;
                            int totalBytes = checked((int)totalBytesL);
                            int padBytes = (4 - (totalBytes & 3)) & 3;
                            if (totalBytes > 0)
                            {
                                // Bulk-stream straight into the backing store;
                                // scoped pin around the native crossing.
                                fixed (byte* dataPtr = dataAsBytes)
                                    InvokeReadBytesDirect(ctx, dataPtr, totalBytes);
                            }
                            if (padBytes > 0)
                            {
                                // The trailing ensure below covers the pad too.
                                if (ctx->readerEnd - ctx->readerPtr < padBytes + cmd->ensureSum)
                                    InvokeEnsureReadable(ctx, padBytes + cmd->ensureSum);
                                ctx->readerPtr += padBytes;
                                break;
                            }
                        }

                        ensureSpace = cmd->ensureSum;
                        goto EnsureAndAdvance;
                    }

                    case V2OpCode.EnterRepeat:
                    {
                        var cmd = (V2CmdEnterRepeat*)pos;

                        // Unchecked count (doc §12).
                        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
                        ctx->readerPtr += 4;

                        // Bound to the field before the body; element bodies
                        // only touch the backing array, and the write side
                        // never emits a tail pad for per-element bodies (v2
                        // slots are 4-aligned).
                        byte[] dataAsBytes = V2ReuseOrAllocateCollection(
                            ref baseAddr, cmd->fieldOffset, (byte)(cmd->kind & kV2Pad0KindMask), cmd->elementTypeHandle, count);

                        if (count == 0)
                        {
                            next += cmd->bodyBytes;
                            ensureSpace = cmd->exitEnsureSum;
                            goto EnsureAndAdvance;
                        }

                        // The frame is the whole loop state: Offset is the
                        // element cursor and EndOffset the exhaust bound; the
                        // trailing exec exit carries the loop facts. The shared
                        // replenish guarantees the body's leading statics for
                        // the first iteration.
                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = dataAsBytes;
                        top.Offset = V2LayoutFacts.ArrayDataOffset;
                        top.EndOffset = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        baseAddr = ref V2RebaseTo(in top);
                        ensureSpace = cmd->bodyEnsureSum;
                        goto EnsureAndAdvance;
                    }

                    // Bulk repeat read (doc §3.5): same bulk/remainder split
                    // as the write side. The collection is reused or allocated
                    // like the plain repeat.
                    case V2OpCode.EnterRepeatBulk:
                    {
                        var cmd = (V2CmdEnterRepeatBulk*)pos;

                        // Unchecked count (doc §12).
                        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
                        ctx->readerPtr += 4;

                        byte[] dataAsBytes = V2ReuseOrAllocateCollection(
                            ref baseAddr, cmd->fieldOffset, (byte)(cmd->kind & kV2Pad0KindMask), cmd->elementTypeHandle, count);

                        if (count == 0)
                        {
                            next += cmd->bulkBodyBytes + cmd->bodyBytes;
                            ensureSpace = cmd->exitEnsureSum;
                            goto EnsureAndAdvance;
                        }

                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = dataAsBytes;
                        top.Offset = V2LayoutFacts.ArrayDataOffset;
                        top.EndOffset = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        baseAddr = ref V2RebaseTo(in top);

                        if (count >= (int)cmd->bulkN)
                        {
                            // The merged bulk block's leading statics.
                            ensureSpace = cmd->bulkBodyEnsureSum;
                        }
                        else
                        {
                            ensureSpace = cmd->bodyEnsureSum;
                            next += cmd->bulkBodyBytes;  // element body only
                        }
                        goto EnsureAndAdvance;
                    }

                    case V2OpCode.ExitRepeatExec:
                    {
                        var cmd = (V2CmdExitRepeatExec*)pos;
                        top.Offset += (nint)cmd->stride;
                        if (top.Offset < top.EndOffset)
                        {
                            // Backedge. baseAddr is the element start at every
                            // exec exit (Enter/Exit pairs rebase back), so the
                            // cursor advances by the stride; a full rebase is
                            // only for frame changes.
                            baseAddr = ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->stride);
                            next -= cmd->bodyBytes;
                            ensureSpace = cmd->bodyEnsureSum;
                        }
                        else
                        {
                            top.Instance = null;
                            top = ref Unsafe.Subtract(ref top, 1);
                            baseAddr = ref V2RebaseTo(in top);
                            // The region after the loop.
                            ensureSpace = cmd->exitEnsureSum;
                        }
                        goto EnsureAndAdvance;
                    }

                    case V2OpCode.ExitRepeatBulkExec:
                    {
                        var cmd = (V2CmdExitRepeatBulkExec*)pos;
                        top.Offset += (nint)cmd->bulkStride;
                        if (top.Offset + (nint)cmd->bulkStride <= top.EndOffset)
                        {
                            // Another full chunk fits.
                            baseAddr = ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->bulkStride);
                            next -= cmd->bulkBodyBytes;
                            ensureSpace = cmd->bulkBodyEnsureSum;
                        }
                        else if (top.Offset < top.EndOffset)
                        {
                            // Chunks done with a remainder: fall through into
                            // the element body, which directly follows, on the
                            // same untouched frame.
                            baseAddr = ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->bulkStride);
                            ensureSpace = cmd->bodyEnsureSum;
                        }
                        else
                        {
                            top.Instance = null;
                            top = ref Unsafe.Subtract(ref top, 1);
                            baseAddr = ref V2RebaseTo(in top);
                            next += cmd->bodyBytes;  // skip the element body
                            ensureSpace = cmd->exitEnsureSum;
                        }
                        goto EnsureAndAdvance;
                    }

                    case V2OpCode.ExitElementSourceExec:
                    {
                        var cmd = (V2CmdExitElementSourceExec*)pos;
                        top.Offset += (nint)cmd->stride;
                        if (top.Offset < top.EndOffset)
                        {
                            baseAddr = ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->stride);
                            next -= cmd->bodyBytes;
                            ensureSpace = cmd->bodyEnsureSum;
                            goto EnsureAndAdvance;
                        }

                        // Exhaust: recover the Enter command directly before
                        // the body, pop the staging frame, hand the staged
                        // array to the sink handler (for Dictionary, v1's
                        // default-allocate and SetEntriesTyped tail) inside
                        // the consumer's FUID frame (v1's ordering), then pop
                        // that frame.
                        var enter = (V2CmdEnterElementSource*)(next - cmd->bodyBytes - sizeof(V2CmdEnterElementSource));
                        int stagingDepth = V2FrameIndexOf(frames, ref top);
                        object closedInstance = top.Instance;
                        top.Instance = null;
                        top = ref Unsafe.Subtract(ref top, 1);
                        baseAddr = ref V2RebaseTo(in top);
                        // Suppressed-callback transfers discard the sink for
                        // callback-keyed dictionaries; the staged entries were
                        // still read for the wire.
                        if ((enter->pad0 & kV2Pad0FlagDictKeyHasCallbacks) == 0
                            || ctx->discardCallbackDictSinks == 0)
                        {
                            var finishFuid = new V2FuidBuilder(pathSegments, pathNames, enter->segmentIndex, frames);
                            ((delegate*<ref byte, Array, ref V2FuidBuilder, NativeReadBufferContext*, ulong, void>)(void*)enter->sinkHandler)(
                                ref Unsafe.AddByteOffset(ref baseAddr, (nint)enter->fieldOffset),
                                Unsafe.As<Array>(closedInstance), ref finishFuid, ctx, enter->sinkUserData);
                        }
                        if ((pushedDictFrames & (1ul << stagingDepth)) != 0)
                        {
                            pushedDictFrames &= ~(1ul << stagingDepth);
                            PopDictionaryFUIDFrame();
                            --openDictFrames;
                        }
                        // The region after the loop.
                        ensureSpace = cmd->exitEnsureSum;
                        goto EnsureAndAdvance;
                    }

                    // Element-source read: read the count prefix, stage the
                    // composed element bodies into a fresh array (element frame
                    // plus its exec exit, like the write side), then at the
                    // loop's ExitElementSourceExec hand the staged array to the
                    // registered sink handler (for Dictionary, default-allocate
                    // and SetEntriesTyped, v1's contract; the bridge indexes
                    // ride the command's opaque sinkUserData).
                    case V2OpCode.EnterElementSource:
                    {
                        var cmd = (V2CmdEnterElementSource*)pos;

                        // Unchecked count (doc §12).
                        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
                        ctx->readerPtr += 4;

                        Type elementType = UnmarshalSystemType((IntPtr)cmd->elementTypeHandle);
                        Array elements = Array.CreateInstance(elementType, count);

                        bool pushedFuidFrame = false;
                        if ((cmd->pad0 & kV2Pad0FlagPushFuidFrame) != 0)
                        {
                            pushedFuidFrame = PushDictionaryFUIDFrame(ctx->fuidContext);
                            if (pushedFuidFrame)
                                ++openDictFrames;
                        }

                        // Suppressed-callback transfers discard the sink for
                        // callback-keyed dictionaries: insertion hashes the
                        // key, and the key's OnAfterDeserialize did not run.
                        // The wire is still consumed; the field stays as-is.
                        bool discardSink = (cmd->pad0 & kV2Pad0FlagDictKeyHasCallbacks) != 0
                            && ctx->discardCallbackDictSinks != 0;

                        if (count == 0)
                        {
                            // The sink still runs (v1's null/empty contract; for
                            // Dictionary, default-allocate and SetEntries(empty)).
                            // No tail pad: v2 element bodies are 4-byte multiples.
                            if (!discardSink)
                            {
                                var emptyFuid = new V2FuidBuilder(pathSegments, pathNames, cmd->segmentIndex, frames);
                                ((delegate*<ref byte, Array, ref V2FuidBuilder, NativeReadBufferContext*, ulong, void>)(void*)cmd->sinkHandler)(
                                    ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset),
                                    elements, ref emptyFuid, ctx, cmd->sinkUserData);
                            }
                            if (pushedFuidFrame)
                            {
                                PopDictionaryFUIDFrame();
                                --openDictFrames;
                            }
                            next += cmd->bodyBytes;
                            ensureSpace = cmd->exitEnsureSum;
                            goto EnsureAndAdvance;
                        }

                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = elements;
                        top.Offset = V2LayoutFacts.ArrayDataOffset;
                        top.EndOffset = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        baseAddr = ref V2RebaseTo(in top);

                        // Remember the successful push at the staging frame's
                        // depth; the exit arm pops against it. Depths past the
                        // bitmask are beyond the serialization depth cap. The
                        // shared replenish guarantees the body's leading
                        // statics for iteration one.
                        if (pushedFuidFrame)
                            pushedDictFrames |= 1ul << V2FrameIndexOf(frames, ref top);
                        ensureSpace = cmd->bodyEnsureSum;
                        goto EnsureAndAdvance;
                    }

                    // ISerializationCallbackReceiver bracket. Write-direction
                    // (OnBeforeSerialize) brackets are skipped. Read-direction
                    // brackets (OnAfterDeserialize, positioned after the
                    // receiver's data) invoke inline, the write bracket's
                    // mirror: registry-first data has every reference patched
                    // before any body's bracket fires, so there is nothing to
                    // defer for. Suppressed transfers (import metadata reads
                    // with kSuppressDeserializeCallbacks) never reach this
                    // arm: ExecuteV2Read serves them the stripped stream.
                    case V2OpCode.InvokeCallback:
                    {
                        var cmd = (V2CmdInvokeCallback*)pos;
                        if ((cmd->flavor & kV2Pad0FlagCallbackRead) == 0)
                            break;  // OnBeforeSerialize bracket, write side only
                        if ((cmd->flavor & kV2CallbackFlavorMask) == kV2CallbackFlavorStruct)
                        {
                            // In-place calli on the struct's data (a host field
                            // or a staged element), mutating before any
                            // following command reads it (v1's contract).
                            if (cmd->methodFnPtr != 0)
                            {
                                ((delegate*<ref byte, void>)(void*)cmd->methodFnPtr)(
                                    ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                            }
                        }
                        else
                        {
                            // Interface dispatch on the current frame's
                            // instance; the composer places the bracket just
                            // before the frame's ExitObject (root brackets
                            // ride frame 0's host), so the receiver is fully
                            // populated. The cast cannot fail: the bracket is
                            // only emitted for declared classes implementing
                            // the interface, and read enters always leave an
                            // assignment-compatible non-null instance.
                            ((ISerializationCallbackReceiver)top.Instance).OnAfterDeserialize();
                        }
                        break;
                    }

                    // Fixed-wire extension group; readCompatible admits it only
                    // with a readGroupHandler. The group's slots live inside the
                    // enclosing fixed block, so entries index off `input` like
                    // copy entries. Layout after the header: count x 8B write
                    // entries, then when flagged count x read entries (v1's
                    // UnityObjectReadEntry layout, 8 + sizeof(IntPtr)) and
                    // count x (field, fieldParent) pairs of 2 x sizeof(void*).
                    // Both strides are pointer-width: V2ExternalFixedReadEntry
                    // and V2ExternalFieldPair in Commands.h are emitted that way
                    // so v1's ReadUnityObjectsIntoFields and the C# handler's
                    // UnityObjectReadEntry cast both match on 32-bit too.
                    case V2OpCode.ExternalFixed:
                    {
                        var cmd = (V2CmdExternalFixedGroup*)pos;
                        byte* entries = pos + sizeof(V2CmdExternalFixedGroup);
                        int count = cmd->count;
                        byte* readEntries = null;
                        byte* fieldTable = null;
                        byte* tableCursor = entries + count * sizeof(V2ExternalFixedEntry);
                        if ((cmd->pad0 & kV2ExternalFixedFlagHasReadTable) != 0)
                        {
                            readEntries = tableCursor;
                            fieldTable = readEntries + count * sizeof(UnityObjectReadEntry);
                            tableCursor = fieldTable + count * 2 * sizeof(void*);
                        }
                        byte* segmentTable = null;
                        if ((cmd->pad0 & kV2ExternalFixedFlagHasSegments) != 0)
                            segmentTable = tableCursor;

                        // The frame instance rides along: handlers whose native
                        // crossing needs the frame's object (SR fixups) hand it
                        // over as a GCHandle, which is GC-safe on CoreCLR's
                        // moving GC where recovering it from baseAddr is not.
                        // top.Offset distinguishes the frame kind for the SR
                        // crossing: 0 = object frame, non-zero = struct
                        // collection-element frame (interior fixup addressing).
                        ((delegate*<object, nint, ref byte, byte*, byte*, byte*, int, byte*, NativeReadBufferContext*, ulong, void>)(void*)cmd->readGroupHandler)(
                            top.Instance, top.Offset, ref baseAddr, entries, readEntries, fieldTable, count, input, ctx, cmd->userData);

                        // SerializeReference missing-type pass. The ctx crossing
                        // is stamped only when the persistent registry holds
                        // missing types, so ordinary reads skip it.
                        if (segmentTable != null && ctx->registerSrReferenceField != IntPtr.Zero)
                            V2RegisterSrReadReferenceFields(entries, segmentTable, count, input, ctx,
                                pathSegments, pathNames, frames);
                        break;
                    }

                    // Dynamic extension field: the read handler owns its input.
                    // The native read dispatchers consume the CachedReader
                    // directly after a syncReader rewind (SimpleNativeType,
                    // NativeValueStruct) or parse framed payloads through the
                    // window contract (PropertyName, FixedBuffer).
                    case V2OpCode.ExternalDynamic:
                    {
                        var cmd = (V2CmdExternalDynamic*)pos;
                        ((delegate*<ref byte, byte*, NativeReadBufferContext*, void>)(void*)cmd->readHandler)(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset),
                            (byte*)cmd, ctx);
                        // The handler's native transfer owns its reads;
                        // re-establish the trailing window guarantee.
                        ensureSpace = cmd->ensureSum;
                        goto EnsureAndAdvance;
                    }

                    // Collection of fixed-wire extension elements: the handler
                    // owns the full framing (count read, reuse-or-allocate,
                    // payload) off the command's read-side tail.
                    case V2OpCode.ExternalArray:
                    {
                        var cmd = (V2CmdExternalArray*)pos;
                        // The path tables serve per-element missing-type
                        // registration in the SR collection handler.
                        ((delegate*<ref byte, byte*, NativeReadBufferContext*, byte*, byte*, void>)(void*)cmd->readArrayHandler)(
                            ref baseAddr, (byte*)cmd, ctx, pathSegments, pathNames);
                        // The handler checks its own framing; re-establish
                        // the trailing window guarantee.
                        ensureSpace = cmd->ensureSum;
                        goto EnsureAndAdvance;
                    }

                    case V2OpCode.End:
                        return;

                    default:
                        throw new InvalidOperationException(
                            $"SerializationBufferToObjectsV2: opcode {pos[0]} is not read-compatible "
                            + "(the composer must not admit it — readCompatible gate out of sync?)");
                }
                pos = next;
                continue;

                // Doc §12: the shared trailing replenish. Arms whose exit
                // carries a trailing window guarantee set ensureSpace and jump
                // here; leading guarantees (guarded blocks, the collection pad)
                // stay in their arms. Hot arms break past this entirely.
            EnsureAndAdvance:
                if (ctx->readerEnd - ctx->readerPtr < ensureSpace)
                    InvokeEnsureReadable(ctx, ensureSpace);
                pos = next;
            }
        }
    }

}
