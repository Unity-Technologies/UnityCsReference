// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.Serialization;

// Managed serialization V2: flat write executor.
//
// Executes a V2 command stream (composed and cached per type on the native
// side, Runtime/Serialize/Managed/) against a managed object, producing
// bytes identical to the v1 backend's.
// Design: core-pipeline-masterplan Serialization/Design/ManagedSerializationV2Write.md.
//
// Pinning policy (doc §6.1): the host is not pinned. It arrives as an object
// reference, every field access goes through a GC-tracked `ref byte`, and
// nested objects are rooted by the frame stack. Pins exist only where an
// interior pointer escapes into native code.
//
// Frame stack (doc §6.2): a managed pointer to the top frame (`ref top`)
// rather than an index. There are no bounds checks because capacity is
// template metadata sized up front, and rebasing is branch-free because every
// frame's Instance is non-null (the root frame holds the host).
//
// One flat loop, no recursion. EnterObject/ExitObject rebase in place, and
// the open FixedBlock spans them (they consume zero wire bytes and never
// touch the stager).
//
// Partial-class fragment of SerializationBackendManagedCommands, sharing
// NativeBufferContext, BufferDataStager, ObjectWrapper, and the framing
// utilities. The frame stack and layout facts live in SharedUtilities.cs;
// out-of-loop helpers live in WriteUtilities.cs.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Executes one V2 command stream for one object. Called by ExecuteV2Write
    // (ManagedSerialization.cpp).
    //
    // Thin EH wrapper: the frame-clearing finally lives here rather than in
    // the dispatch method, because locals that are live across an EH region
    // are de-enregistered for the whole method.
    [RequiredByNativeCode]
    public static unsafe void ObjectsToSerializationBufferV2(
        object host,
        IntPtr streamStart,
        IntPtr bufferContext)
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
            ExecuteV2WriteStream((byte*)(streamHeader + 1),
                (NativeBufferContext*)bufferContext, frames,
                ref V2RebaseTo(in frames[0]), ref openDictFrames);
        }
        finally
        {
            // Clears every slot, including any left populated by a thrown
            // transfer, so the thread-static never roots user objects.
            Array.Clear(frames, 0, maxFrameDepth);
            ReturnV2Frames(frames);
            // Rebalance the thread-local FUID dict-frame stack if a transfer
            // threw mid-dictionary, keeping the dispatch loop EH-free. The
            // FUID index stack needs no unwinding: v2 never pushes to it,
            // identifiers recover live indices from the frame stack
            // (FuidPaths.cs).
            while (openDictFrames > 0)
            {
                PopDictionaryFUIDFrame();
                --openDictFrames;
            }
        }
    }

    // Unmanaged-base entry for TransferComponent (isUnmanagedSafe templates
    // only). The compose gate rules out frame pushes and managed-reference
    // chase, so we pass an empty frame span and skip the pool round-trip.
    [RequiredByNativeCode]
    public static unsafe void ObjectsToSerializationBufferV2Unmanaged(
        IntPtr unmanagedBase,
        IntPtr streamStart,
        IntPtr bufferContext)
    {
        var streamHeader = (V2CmdStreamHeader*)streamStart;
        int openDictFrames = 0;
        ExecuteV2WriteStream((byte*)(streamHeader + 1),
            (NativeBufferContext*)bufferContext,
            Span<V2ObjectFrame>.Empty,
            ref Unsafe.AsRef<byte>((void*)unmanagedBase),
            ref openDictFrames);
        UnityEngine.Assertions.Assert.IsTrue(openDictFrames == 0,
            "isUnmanagedSafe templates cannot push dictionary FUID frames");
    }

    // The flat dispatch loop over the wrapper-seeded frame stack (frame 0
    // holds the host) and the host's field base. No EH in here; see the
    // wrapper.
    private static unsafe void ExecuteV2WriteStream(
        byte* streamStart,
        NativeBufferContext* ctx,
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

            // Base of the fixed block currently open at the stager's staging
            // tail; copy entries index off it. Native memory, so a raw pointer.
            byte* output = null;
            var stager = new BufferDataStager(ctx);

            // Whether the most recent EnterObject materialized its null slot.
            // Consumed by the class-flavor callback bracket the composer
            // places directly after the enter: a materialized instance fires
            // nothing and writes ctor defaults (v1's null-slot contract).
            // False at entry so the root bracket fires on the host.
            bool enterMaterialized = false;

            byte* pos = streamStart;
            while (true)
            {
                // Header-owned advancement: every command carries its total
                // size (entry arrays and padding included) in 4-byte units.
                // Control-flow arms (repeat skip/backedge) override next; the
                // End terminator returns.
                byte* next = pos + (*(ushort*)(pos + 2) << 2);
                switch ((V2OpCode)pos[0])
                {
                    case V2OpCode.FixedBlock:
                    {
                        var cmd = (V2CmdFixedBlock*)pos;
                        // Unchecked staging (doc §12): the publish-time drain
                        // walk (V2InsertWriteRoomGuards) proves every unguarded
                        // block fits the window armed by the preceding re-arm
                        // point, inserting EnsureWriteRoom commands where the
                        // count-only collection exits would drain it short.
                        output = stager.ReserveUnchecked((int)cmd->wireBytes);
                        break;
                    }

                    case V2OpCode.FixedBlockGuarded:
                    {
                        // Re-arm then claim, so the drain accounting restarts
                        // at this block's own bytes.
                        var cmd = (V2CmdFixedBlockGuarded*)pos;
                        stager.EnsureRoom(kManagedBlockMaxPayloadSize);
                        output = stager.ReserveUnchecked((int)cmd->wireBytes);
                        break;
                    }

                    case V2OpCode.EnsureWriteRoom:
                        // Publish-inserted re-arm (doc §12): covers regions
                        // whose worst-case drain (count-only collection exits)
                        // exceeds the window.
                        goto EnsureRoomAndAdvance;

                    case V2OpCode.DirectCopy4:
                    {
                        // Typed load: the lowering routes every field offset
                        // that is not 4-aligned to DirectCopy4Unaligned, so
                        // this arm's offset is aligned by contract.
                        var cmd = (V2CmdDirectCopy*)pos;
                        *(int*)(output + cmd->destOffset) =
                            Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        break;
                    }

                    case V2OpCode.DirectCopy4Unaligned:
                    {
                        // Pack/Explicit-layout field offsets: an unaligned
                        // load, because a typed one faults on
                        // strict-alignment ARM. Dest slots stay 4-aligned.
                        var cmd = (V2CmdDirectCopy*)pos;
                        *(int*)(output + cmd->destOffset) =
                            Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        break;
                    }

                    case V2OpCode.DirectCopyRun:
                    {
                        var cmd = (V2CmdDirectCopyRun*)pos;
                        Unsafe.CopyBlockUnaligned(
                            ref Unsafe.AsRef<byte>(output + cmd->destOffset),
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset),
                            cmd->byteCount);
                        break;
                    }

                    case V2OpCode.DirectCopy4N_8:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + (e[i].destOffset << 2)) =
                                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 2)));
                        break;
                    }

                    case V2OpCode.DirectCopy4N_8x4:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;  // multiple of 4 by construction
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; i += 4)
                        {
                            *(int*)(output + (e[i].destOffset << 2)) =
                                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 2)));
                            *(int*)(output + (e[i + 1].destOffset << 2)) =
                                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i + 1].fieldOffset << 2)));
                            *(int*)(output + (e[i + 2].destOffset << 2)) =
                                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i + 2].fieldOffset << 2)));
                            *(int*)(output + (e[i + 3].destOffset << 2)) =
                                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i + 3].fieldOffset << 2)));
                        }
                        break;
                    }

                    case V2OpCode.DirectCopy4N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + (e[i].destOffset << 2)) =
                                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 2)));
                        break;
                    }

                    case V2OpCode.DirectCopy4N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + ((nint)e[i].destOffset << 2)) =
                                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_8:
                    {
                        // Zero-extended int stores, matching DirectCopy2's slots.
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + (e[i].destOffset << 2)) =
                                Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 1)));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + (e[i].destOffset << 2)) =
                                Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)(e[i].fieldOffset << 1)));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + ((nint)e[i].destOffset << 2)) =
                                Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset << 1));
                        break;
                    }

                    case V2OpCode.DirectCopy1N_8:
                    {
                        // Zero-extended int stores, matching DirectCopy1's slots.
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + (e[i].destOffset << 2)) =
                                Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy1N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + (e[i].destOffset << 2)) =
                                Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy1N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        for (int i = 0; i < n; ++i)
                            *(int*)(output + ((nint)e[i].destOffset << 2)) =
                                Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy1:
                    {
                        var cmd = (V2CmdDirectCopy*)pos;
                        // Zero-extended int store: the value byte plus three zero
                        // pad bytes, matching native's transfer.Align() output.
                        *(int*)(output + cmd->destOffset) =
                            Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy2:
                    {
                        // Typed load; 2-aligned by contract (see DirectCopy4).
                        var cmd = (V2CmdDirectCopy*)pos;
                        *(int*)(output + cmd->destOffset) =
                            Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        break;
                    }

                    case V2OpCode.DirectCopy2Unaligned:
                    {
                        // Odd field offsets; see DirectCopy4Unaligned.
                        var cmd = (V2CmdDirectCopy*)pos;
                        *(int*)(output + cmd->destOffset) =
                            Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        break;
                    }

                    case V2OpCode.EnterObject:
                    {
                        var cmd = (V2CmdEnterObject*)pos;
                        // Null-slot materialization; write-side null population
                        // matches native.
                        ref object slot = ref Unsafe.As<byte, object>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        object child = slot;
                        enterMaterialized = child == null;
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

                        top = ref Unsafe.Add(ref top, 1);          // push (no bounds check; capacity is template metadata)
                        top.Instance = child;
                        top.Offset = 0;
                        baseAddr = ref Unsafe.As<ObjectWrapper>(child).Data;
                        // Zero wire bytes: the open FixedBlock stays open.
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
                        enterMaterialized = child == null;
                        if (child == null)
                        {
                            var ctor = (V2Constructor)SerializationCommandObjectTable.Get(cmd->constructorIndex);
                            child = slot = RuntimeHelpers.GetUninitializedObject(ctor.Type);
                        }

                        top = ref Unsafe.Add(ref top, 1);          // push (no bounds check; capacity is template metadata)
                        top.Instance = child;
                        top.Offset = 0;
                        baseAddr = ref Unsafe.As<ObjectWrapper>(child).Data;
                        // Zero wire bytes: the open FixedBlock stays open.
                        break;
                    }

                    case V2OpCode.EnterObjectFallback:
                    {
                        var cmd = (V2CmdEnterObjectFallback*)pos;
                        ref object slot = ref Unsafe.As<byte, object>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        object child = slot;
                        enterMaterialized = child == null;
                        if (child == null)
                            child = slot = V2CreateInstanceFallback((IntPtr)cmd->runtimeTypeHandle, (IntPtr)cmd->ctorFunctionPtr);

                        top = ref Unsafe.Add(ref top, 1);          // push (no bounds check; capacity is template metadata)
                        top.Instance = child;
                        top.Offset = 0;
                        baseAddr = ref Unsafe.As<ObjectWrapper>(child).Data;
                        // Zero wire bytes: the open FixedBlock stays open.
                        break;
                    }

                    case V2OpCode.EnterObjectFactory:
                    {
                        var cmd = (V2CmdEnterObjectFactory*)pos;
                        ref object slot = ref Unsafe.As<byte, object>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        object child = slot;
                        enterMaterialized = child == null;
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

                        top = ref Unsafe.Add(ref top, 1);          // push (no bounds check; capacity is template metadata)
                        top.Instance = child;
                        top.Offset = 0;
                        baseAddr = ref Unsafe.As<ObjectWrapper>(child).Data;
                        // Zero wire bytes: the open FixedBlock stays open.
                        break;
                    }

                    case V2OpCode.ExitObject:
                    {
                        top.Instance = null;                       // do not root the child past its scope
                        top = ref Unsafe.Subtract(ref top, 1);     // pop
                        baseAddr = ref V2RebaseTo(in top);
                        break;
                    }

                    case V2OpCode.String:
                    {
                        var cmd = (V2CmdString*)pos;
                        string value = Unsafe.As<byte, string>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset)) ?? string.Empty;
                        V2WriteFramedString(ctx, value.AsSpan(), ref stager);
                        // Re-arm the window for the region after.
                        goto EnsureRoomAndAdvance;
                    }

                    case V2OpCode.LinearCollectionMemCpy:
                    {
                        var cmd = (V2CmdLinearCollectionMemCpy*)pos;

                        if (!V2ReadCollection(ref baseAddr, cmd->fieldOffset, (byte)(cmd->kind & kV2Pad0KindMask), out byte[] dataAsBytes, out int count))
                        {
                            Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), 0);
                            break;
                        }

                        // Wire: int32 count, count * stride raw bytes, then
                        // 0..3 zero pad (v1's ConsumeLinearCollection contract).
                        // The pin is the allowed per-crossing kind: the array
                        // object, scoped to the copy (doc §6.1).
                        long totalBytesL = (long)count * cmd->elementStride;
                        int totalBytes = checked((int)totalBytesL);
                        int padBytes = (4 - (totalBytes & 3)) & 3;
                        int framedSize = 4 + totalBytes + padBytes;

                        byte* dst = stager.TryReserve(framedSize);
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
                            goto EnsureRoomAndAdvance;
                        }

                        fixed (byte* dataPtr = dataAsBytes)
                        {
                            Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), count);
                            stager.FlushStaged(kManagedBlockMaxPayloadSize);
                            stager.Bulk(dataPtr, totalBytes);
                        }
                        if (padBytes > 0)
                            Unsafe.InitBlockUnaligned(stager.Reserve(padBytes), 0, (uint)padBytes);
                        goto EnsureRoomAndAdvance;
                    }

                    case V2OpCode.EnterRepeat:
                    {
                        var cmd = (V2CmdEnterRepeat*)pos;

                        if (!V2ReadCollection(ref baseAddr, cmd->fieldOffset, (byte)(cmd->kind & kV2Pad0KindMask), out byte[] dataAsBytes, out int count))
                        {
                            Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), 0);
                            next += cmd->bodyBytes;
                            break;
                        }

                        Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), count);
                        if (count == 0)
                        {
                            next += cmd->bodyBytes;
                            break;
                        }

                        // Element frame: {collection backing array, cursor,
                        // exhaust bound}, a tracked triple with no pin. The GC
                        // can move the array between elements or mid-element
                        // at a flush transition, and the rebase stays correct.
                        // The frame is the whole loop state; the trailing exec
                        // exit carries the loop facts.
                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = dataAsBytes;
                        top.Offset = V2LayoutFacts.ArrayDataOffset;
                        top.EndOffset = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        baseAddr = ref V2RebaseTo(in top);
                        break;
                    }

                    // Bulk repeat (doc §3.5): count / bulkN chunks over the
                    // replicated bulk body, then the element body for the
                    // remainder. count < bulkN skips the bulk body.
                    case V2OpCode.EnterRepeatBulk:
                    {
                        var cmd = (V2CmdEnterRepeatBulk*)pos;

                        if (!V2ReadCollection(ref baseAddr, cmd->fieldOffset, (byte)(cmd->kind & kV2Pad0KindMask), out byte[] dataAsBytes, out int count))
                        {
                            Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), 0);
                            next += cmd->bulkBodyBytes + cmd->bodyBytes;
                            break;
                        }

                        Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), count);
                        if (count == 0)
                        {
                            next += cmd->bulkBodyBytes + cmd->bodyBytes;
                            break;
                        }

                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = dataAsBytes;
                        top.Offset = V2LayoutFacts.ArrayDataOffset;
                        top.EndOffset = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        baseAddr = ref V2RebaseTo(in top);

                        if (count < (int)cmd->bulkN)
                            next += cmd->bulkBodyBytes;  // element body only
                        break;
                    }

                    case V2OpCode.ExitRepeatExec:
                    {
                        // Every outcome re-arms the window (the next
                        // iteration's block or the region after the loop
                        // stages unchecked against it).
                        var cmd = (V2CmdExitRepeatExec*)pos;
                        top.Offset += (nint)cmd->stride;
                        if (top.Offset < top.EndOffset)
                        {
                            // Backedge. baseAddr is the element start at every
                            // exec exit (Enter/Exit pairs rebase back), so the
                            // cursor advances by the stride; a full rebase is
                            // only for frame changes.
                            baseAddr = ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->stride);
                            next -= cmd->bodyBytes;  // loop backedge
                        }
                        else
                        {
                            top.Instance = null;
                            top = ref Unsafe.Subtract(ref top, 1);
                            baseAddr = ref V2RebaseTo(in top);
                        }
                        goto EnsureRoomAndAdvance;
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
                        }
                        else if (top.Offset < top.EndOffset)
                        {
                            // Chunks done with a remainder: fall through into
                            // the element body, which directly follows, on the
                            // same untouched frame.
                            baseAddr = ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->bulkStride);
                        }
                        else
                        {
                            top.Instance = null;
                            top = ref Unsafe.Subtract(ref top, 1);
                            baseAddr = ref V2RebaseTo(in top);
                            next += cmd->bodyBytes;  // skip the element body
                        }
                        goto EnsureRoomAndAdvance;
                    }

                    case V2OpCode.ExitElementSourceExec:
                    {
                        var cmd = (V2CmdExitElementSourceExec*)pos;
                        top.Offset += (nint)cmd->stride;
                        if (top.Offset < top.EndOffset)
                        {
                            baseAddr = ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->stride);
                            next -= cmd->bodyBytes;
                            goto EnsureRoomAndAdvance;
                        }

                        int stagingDepth = V2FrameIndexOf(frames, ref top);
                        top.Instance = null;
                        top = ref Unsafe.Subtract(ref top, 1);
                        baseAddr = ref V2RebaseTo(in top);
                        if ((pushedDictFrames & (1ul << stagingDepth)) != 0)
                        {
                            pushedDictFrames &= ~(1ul << stagingDepth);
                            PopDictionaryFUIDFrame();
                            --openDictFrames;
                        }
                        goto EnsureRoomAndAdvance;
                    }

                    case V2OpCode.ExternalFixed:
                    {
                        var cmd = (V2CmdExternalFixedGroup*)pos;
                        var entries = (V2ExternalFixedEntry*)(pos + sizeof(V2CmdExternalFixedGroup));
                        int count = cmd->count;

                        if (cmd->groupHandler != 0)
                        {
                            // One managed call per group; the handler may batch
                            // its native crossing.
                            ((delegate*<ref byte, byte*, int, byte*, NativeBufferContext*, ulong, void>)(void*)cmd->groupHandler)(
                                ref baseAddr, (byte*)entries, count, output, ctx, cmd->userData);
                            break;
                        }

                        var fieldHandler = (delegate*<ref byte, byte*, NativeBufferContext*, ulong, void>)(void*)cmd->fieldHandler;
                        for (int i = 0; i < count; ++i)
                        {
                            fieldHandler(
                                ref Unsafe.AddByteOffset(ref baseAddr, (nint)entries[i].fieldOffset),
                                output + entries[i].destOffset, ctx, cmd->userData);
                        }
                        break;
                    }

                    case V2OpCode.ExternalDynamic:
                    {
                        var cmd = (V2CmdExternalDynamic*)pos;
                        ((delegate*<ref byte, NativeBufferContext*, ref BufferDataStager, ulong, ulong, void>)(void*)cmd->handler)(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset),
                            ctx, ref stager, cmd->userData, cmd->userData2);
                        // Re-arm the window for the region after.
                        goto EnsureRoomAndAdvance;
                    }

                    case V2OpCode.ExternalArray:
                    {
                        var cmd = (V2CmdExternalArray*)pos;

                        if (!V2ReadCollection(ref baseAddr, cmd->fieldOffset, (byte)(cmd->kind & kV2Pad0KindMask), out byte[] dataAsBytes, out int count))
                        {
                            Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), 0);
                            break;
                        }

                        if (cmd->arrayHandler != 0)
                        {
                            // Batched path: the handler owns the full framing
                            // (count, payload, pad) in one call, so it can batch
                            // its native crossings.
                            ((delegate*<byte[], int, uint, NativeBufferContext*, ref BufferDataStager, ulong, void>)(void*)cmd->arrayHandler)(
                                dataAsBytes, count, cmd->elementStride, ctx, ref stager, cmd->userData);
                            goto EnsureRoomAndAdvance;
                        }

                        // Per-element fallback: the executor frames, and the
                        // field handler fills window-sized chunks.
                        Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), count);
                        int wire = (int)cmd->elementWireBytes;
                        var fieldHandler = (delegate*<ref byte, byte*, NativeBufferContext*, ulong, void>)(void*)cmd->fieldHandler;
                        int processed = 0;
                        while (processed < count)
                        {
                            if (stager.StagingRoom < wire)
                                stager.FlushStaged(kManagedBlockMaxPayloadSize);
                            int batch = Math.Min(stager.StagingRoom / wire, count - processed);
                            byte* dst = stager.StagingPtr;
                            for (int i = 0; i < batch; ++i)
                            {
                                ref byte element = ref Unsafe.AddByteOffset(
                                    ref Unsafe.As<ObjectWrapper>((object)dataAsBytes).Data,
                                    V2LayoutFacts.ArrayDataOffset + (nint)((long)(processed + i) * cmd->elementStride));
                                fieldHandler(ref element, dst + i * wire, ctx, cmd->userData);
                            }
                            stager.Stage(batch * wire);
                            processed += batch;
                        }

                        int totalBytes = count * wire;
                        int padBytes = (4 - (totalBytes & 3)) & 3;
                        if (padBytes > 0)
                            Unsafe.InitBlockUnaligned(stager.Reserve(padBytes), 0, (uint)padBytes);
                        goto EnsureRoomAndAdvance;
                    }

                    case V2OpCode.EnterElementSource:
                    {
                        var cmd = (V2CmdEnterElementSource*)pos;

                        // A null collection writes count 0 with no FUID frame
                        // and no handler call; v1's ConsumeDictionary pushes
                        // after its null check and never bridges a null.
                        object collection = Unsafe.As<byte, object>(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                        if (collection == null)
                        {
                            Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), 0);
                            next += cmd->bodyBytes;
                            break;
                        }

                        // Install the consumer's FUID frame before the source
                        // handler runs; the native identifier formatter resolves
                        // against the active frame. The wrapper's finally
                        // rebalances on a thrown transfer.
                        bool pushedFuidFrame = false;
                        if ((cmd->pad0 & kV2Pad0FlagPushFuidFrame) != 0)
                        {
                            pushedFuidFrame = PushDictionaryFUIDFrame(ctx->fuidContext);
                            if (pushedFuidFrame)
                                ++openDictFrames;
                        }

                        // The registered source handler stages the element array
                        // (for Dictionary, v1's GetEntries bridge with its dedup,
                        // warnings, and FUID row keying). The FUID builder gives
                        // it on-demand access to the resolved identifier.
                        var fuid = new V2FuidBuilder(pathSegments, pathNames, cmd->segmentIndex, frames);
                        Array elements = ((delegate*<object, ref V2FuidBuilder, NativeBufferContext*, ulong, Array>)(void*)cmd->sourceHandler)(
                            collection, ref fuid, ctx, cmd->sourceUserData);

                        int count = elements != null ? elements.Length : 0;
                        Unsafe.WriteUnaligned(stager.ReserveUnchecked(4), count);
                        if (count == 0)
                        {
                            if (pushedFuidFrame)
                            {
                                PopDictionaryFUIDFrame();
                                --openDictFrames;
                            }
                            next += cmd->bodyBytes;
                            break;
                        }

                        // Element frame over the staged array (tracked, no
                        // pin); the body is the ordinary composed element
                        // template ending in its exec exit. No tail pad:
                        // composed element bodies are 4-byte multiples.
                        top = ref Unsafe.Add(ref top, 1);
                        top.Instance = elements;
                        top.Offset = V2LayoutFacts.ArrayDataOffset;
                        top.EndOffset = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        baseAddr = ref V2RebaseTo(in top);

                        // Remember the successful push at the staging frame's
                        // depth; the exit arm pops against it.
                        if (pushedFuidFrame)
                            pushedDictFrames |= 1ul << V2FrameIndexOf(frames, ref top);
                        break;
                    }

                    case V2OpCode.InvokeCallback:
                    {
                        var cmd = (V2CmdInvokeCallback*)pos;
                        if ((cmd->flavor & kV2Pad0FlagCallbackRead) != 0)
                            break;  // OnAfterDeserialize bracket, read side only
                        if ((cmd->flavor & kV2CallbackFlavorMask) == kV2CallbackFlavorStruct)
                        {
                            // In-place calli on the struct's data, so mutations
                            // land before the following copy commands read the
                            // fields (v1's contract).
                            if (cmd->methodFnPtr != 0)
                            {
                                ((delegate*<ref byte, void>)(void*)cmd->methodFnPtr)(
                                    ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                            }
                        }
                        else
                        {
                            // Interface dispatch on the current frame's
                            // instance; the composer places the bracket
                            // directly after the frame's EnterObject (root
                            // brackets ride frame 0's host). A materialized
                            // slot fires nothing and writes ctor defaults
                            // (v1's null-slot contract). The cast cannot fail:
                            // the bracket is only emitted for declared classes
                            // implementing the interface, and the frame holds
                            // an assignment-compatible non-null instance.
                            if (!enterMaterialized)
                                ((ISerializationCallbackReceiver)top.Instance).OnBeforeSerialize();
                        }
                        break;
                    }

                    case V2OpCode.End:
                        stager.FlushStaged(0);
                        return;

                    default:
                        stager.FlushStaged(0);
                        throw new InvalidOperationException(
                            $"ObjectsToSerializationBufferV2: unknown V2 opcode {pos[0]} (out-of-sync Commands.h mirror?)");
                }
                pos = next;
                continue;

                // Doc §12: the shared window re-arm. Arms whose every path
                // re-arms at exit jump here; leading re-arms (guarded blocks)
                // and the deliberately non-re-arming count-only collection
                // paths stay in their arms. Hot arms break past this entirely.
            EnsureRoomAndAdvance:
                stager.EnsureRoom(kManagedBlockMaxPayloadSize);
                pos = next;
            }
        }
    }
}
