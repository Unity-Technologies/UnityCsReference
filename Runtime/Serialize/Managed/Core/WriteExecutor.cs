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
// touch the write window).
//
// Write window (doc §12): commands stage at ctx->writerPtr, bounded by
// ctx->writerEnd. Each ensure point asks for exactly the bytes the stamped sum
// says the path ahead stages unchecked, the same sums the read executor
// replenishes by, so a window re-arm happens only where a read would refill.
//
// Partial-class fragment of SerializationBackendManagedCommands, sharing
// NativeBufferContext, ObjectWrapper, and the framing
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
            // The parent's field base across a frameless object body
            // (kV2Pad0FlagEnterNoFrame); such bodies hold no enter, so one
            // slot is enough.
            ref byte parentBase = ref baseAddr;

            // Register copy of top's cursor pair; see the same locals in
            // ReadExecutor.cs. Collection commands read it instead of the
            // frame and write Offset back only when a body is about to run;
            // every pop reloads it. cursorEnd == 0 marks a collection that
            // pushed no frame (see CollectionEnterOrSkip).
            nint cursorOffset = 0;
            nint cursorEnd = 0;

            // Base of the fixed block currently open at the write cursor; copy
            // entries index off it. Native memory, so a raw pointer.
            byte* output = null;

            // Whether the most recent EnterObject materialized its null slot.
            // Consumed by the class-flavor callback bracket the composer
            // places directly after the enter: a materialized instance fires
            // nothing and writes ctor defaults (v1's null-slot contract).
            // False at entry so the root bracket fires on the host.
            bool enterMaterialized = false;

            // A write that only feeds a text rendering (JsonUtility.ToJson via the
            // binary walker) materialises a null field to transfer it but must not
            // store the instance back -- native's
            // ShouldPopulateNullManagedFieldsWhenWriting == false, case 942547.
            bool populateNullSlots =
                (ctx->flags & UnityObjectTransferFlags.DontPopulateNullManagedFields) == 0;

            byte* pos = streamStart;
            while (true)
            {
                // Header-owned advancement: every command carries its total
                // size (entry arrays and padding included) in 4-byte units.
                // Control-flow arms (repeat skip/backedge) override next; the
                // End terminator returns.
                byte* next = pos + (*(ushort*)(pos + 2) << 2);
                int ensureSpace;
                // The object the next pushed frame holds: an entered class
                // instance or a collection's backing array. Shared by the
                // enter arms and the tails that push their frame.
                object instance;
                switch ((V2OpCode)pos[0])
                {
                    case V2OpCode.FixedBlock:
                    {
                        // Unchecked staging (doc §12): the preceding ensure
                        // point covered this block's bytes.
                        var cmd = (V2CmdFixedBlock*)pos;
                        output = ctx->writerPtr;
                        ctx->writerPtr += (int)cmd->wireBytes;
                        break;
                    }

                    case V2OpCode.FixedBlockGuarded:
                    {
                        // Ensures its own bytes plus the trailing static sum.
                        var cmd = (V2CmdFixedBlockGuarded*)pos;
                        if (ctx->writerEnd - ctx->writerPtr < (int)cmd->ensureSum)
                            ctx->ensureWritable(ctx, (int)cmd->ensureSum);
                        output = ctx->writerPtr;
                        ctx->writerPtr += (int)cmd->wireBytes;
                        break;
                    }

                    case V2OpCode.DirectCopy4:
                    {
                        // Typed load: the lowering routes every field offset
                        // that is not 4-aligned to DirectCopy4Unaligned, so
                        // this arm's offset is aligned by contract.
                        var cmd = (V2CmdDirectCopy*)pos;
                        *(int*)(output + cmd->destOffset) =
                            V2FieldRef<int>(ref baseAddr, (nint)cmd->fieldOffset);
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
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + (e[i].destOffset << 2)) =
                                V2FieldRef<int>(ref baseAddr, (nint)(e[i].fieldOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy4N_8x4:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;  // multiple of 4 by construction
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; i += 4)
                        {
                            *(int*)(dst + (e[i].destOffset << 2)) =
                                V2FieldRef<int>(ref baseAddr, (nint)(e[i].fieldOffset << 2));
                            *(int*)(dst + (e[i + 1].destOffset << 2)) =
                                V2FieldRef<int>(ref baseAddr, (nint)(e[i + 1].fieldOffset << 2));
                            *(int*)(dst + (e[i + 2].destOffset << 2)) =
                                V2FieldRef<int>(ref baseAddr, (nint)(e[i + 2].fieldOffset << 2));
                            *(int*)(dst + (e[i + 3].destOffset << 2)) =
                                V2FieldRef<int>(ref baseAddr, (nint)(e[i + 3].fieldOffset << 2));
                        }
                        break;
                    }

                    case V2OpCode.DirectCopy4N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + (e[i].destOffset << 2)) =
                                V2FieldRef<int>(ref baseAddr, (nint)(e[i].fieldOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy4N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + ((nint)e[i].destOffset << 2)) =
                                V2FieldRef<int>(ref baseAddr, (nint)e[i].fieldOffset << 2);
                        break;
                    }

                    case V2OpCode.DirectCopy2N_8:
                    {
                        // Zero-extended int stores, matching DirectCopy2's slots.
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + (e[i].destOffset << 2)) =
                                V2FieldRef<ushort>(ref baseAddr, (nint)(e[i].fieldOffset << 1));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + (e[i].destOffset << 2)) =
                                V2FieldRef<ushort>(ref baseAddr, (nint)(e[i].fieldOffset << 1));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + ((nint)e[i].destOffset << 2)) =
                                V2FieldRef<ushort>(ref baseAddr, (nint)e[i].fieldOffset << 1);
                        break;
                    }

                    case V2OpCode.DirectCopy1N_8:
                    {
                        // Zero-extended int stores, matching DirectCopy1's slots.
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + (e[i].destOffset << 2)) =
                                Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy1N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + (e[i].destOffset << 2)) =
                                Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset);
                        break;
                    }

                    case V2OpCode.DirectCopy1N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* dst = output;
                        for (int i = 0; i < n; ++i)
                            *(int*)(dst + ((nint)e[i].destOffset << 2)) =
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
                            V2FieldRef<ushort>(ref baseAddr, (nint)cmd->fieldOffset);
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
                        ref object slot = ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset);
                        instance = slot;
                        enterMaterialized = instance == null;
                        if (instance == null)
                        {
                            instance = V2ConstructRegistered(cmd->constructorIndex);
                            if (populateNullSlots)
                                slot = instance;
                        }

                        goto EnterObjectTail;
                    }

                    case V2OpCode.EnterObjectNoCtor:
                    {
                        var cmd = (V2CmdEnterObject*)pos;
                        // Class without a parameterless ctor (v1's optional-
                        // ctor contract): allocation only, nothing can throw.
                        ref object slot = ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset);
                        instance = slot;
                        enterMaterialized = instance == null;
                        if (instance == null)
                        {
                            var ctor = (V2Constructor)SerializationCommandObjectTable.Get(cmd->constructorIndex);
                            instance = RuntimeHelpers.GetUninitializedObject(ctor.Type);
                            if (populateNullSlots)
                                slot = instance;
                        }

                        goto EnterObjectTail;
                    }

                    case V2OpCode.EnterObjectFallback:
                    {
                        var cmd = (V2CmdEnterObjectFallback*)pos;
                        ref object slot = ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset);
                        instance = slot;
                        enterMaterialized = instance == null;
                        if (instance == null)
                        {
                            instance = V2CreateInstanceFallback((nint)cmd->runtimeTypeHandle, (nint)cmd->ctorFunctionPtr);
                            if (populateNullSlots)
                                slot = instance;
                        }

                        goto EnterObjectTail;
                    }

                    case V2OpCode.EnterObjectFactory:
                    {
                        var cmd = (V2CmdEnterObjectFactory*)pos;
                        ref object slot = ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset);
                        instance = slot;
                        enterMaterialized = instance == null;
                        if (instance == null)
                        {
                            instance = V2ConstructWithFactory(cmd->factoryFunctionPtr, cmd->constructorIndex);
                            if (populateNullSlots)
                                slot = instance;
                        }

                        goto EnterObjectTail;
                    }

                    case V2OpCode.ExitObject:
                    {
                        top.Instance = null;                       // do not root the child past its scope
                        top = ref Unsafe.Subtract(ref top, 1);     // pop
                        cursorOffset = top.Offset;
                        cursorEnd = top.EndOffset;
                        baseAddr = ref V2RebaseTo(in top);
                        break;
                    }

                    // Closes a frameless enter: the frame stack and the cursor
                    // pair never moved.
                    case V2OpCode.ExitObjectSimple:
                        baseAddr = ref parentBase;
                        break;

                    case V2OpCode.String:
                    {
                        var cmd = (V2CmdString*)pos;
                        string value = V2FieldRef<string>(ref baseAddr, (nint)cmd->fieldOffset) ?? string.Empty;
                        V2WriteFramedString(ctx, value.AsSpan());
                        ensureSpace = cmd->ensureSum;
                        goto EnsureAndAdvance;
                    }

                    // The factory variants share the whole prefix with the
                    // plain layouts; only the read side consumes the trailing
                    // payload, so the write arms serve both opcodes.
                    case V2OpCode.LinearCollectionMemCpy:
                    case V2OpCode.LinearCollectionMemCpyFactory:
                    {
                        var cmd = (V2CmdLinearCollectionMemCpy*)pos;

                        // The count is covered by the preceding ensure point.
                        if (!V2GetCollectionBacking(ref baseAddr, cmd->fieldOffset, cmd->kind, out byte[] dataAsBytes, out int count))
                        {
                            Unsafe.WriteUnaligned(ctx->writerPtr, 0);
                            ctx->writerPtr += 4;
                            ensureSpace = cmd->ensureSum;
                            goto EnsureAndAdvance;
                        }
                        Unsafe.WriteUnaligned(ctx->writerPtr, count);
                        ctx->writerPtr += 4;

                        // Wire: int32 count, count * stride raw bytes, then
                        // 0..3 zero pad (v1's ConsumeLinearCollection contract).
                        // The pin is the allowed per-crossing kind: the array
                        // object, scoped to the copy (doc §6.1).
                        int totalBytes = checked((int)((long)count * cmd->elementStride));
                        int padBytes = (4 - (totalBytes & 3)) & 3;
                        int room = (int)(ctx->writerEnd - ctx->writerPtr);
                        if (room >= totalBytes + padBytes)
                        {
                            if (totalBytes > 0)
                            {
                                fixed (byte* dataPtr = dataAsBytes)
                                    Buffer.MemoryCopy(dataPtr, ctx->writerPtr, totalBytes, totalBytes);
                            }
                            if (padBytes > 0)
                                Unsafe.InitBlockUnaligned(ctx->writerPtr + totalBytes, 0, (uint)padBytes);
                            ctx->writerPtr += totalBytes + padBytes;
                            ensureSpace = cmd->ensureSum;
                            goto EnsureAndAdvance;
                        }

                        // The head fills the window; one crossing commits it,
                        // streams the rest and the pad, and re-arms for the
                        // trailing sum at the 4-aligned post-pad position.
                        int head = Math.Min(room, totalBytes);
                        fixed (byte* dataPtr = dataAsBytes)
                        {
                            Buffer.MemoryCopy(dataPtr, ctx->writerPtr, head, head);
                            ctx->writerPtr += head;
                            ctx->writeBytesDirect(ctx, dataPtr + head, totalBytes - head, padBytes, cmd->ensureSum);
                        }
                        break;
                    }

                    // Collection loop: one test, Offset + stride <= EndOffset,
                    // in three positions (see the opcode comment). Entering a
                    // body rebases without advancing, so the cursor points at
                    // the element the body runs on; the latches advance first.
                    case V2OpCode.CollectionEnterOrSkip:
                    case V2OpCode.CollectionEnterOrSkipFactory:
                    {
                        var cmd = (V2CmdCollectionEnterOrSkip*)pos;

                        // The count is covered by the preceding ensure point.
                        if (!V2GetCollectionBacking(ref baseAddr, cmd->fieldOffset, cmd->kind, out byte[] dataAsBytes, out int count))
                            count = 0;  // dataAsBytes stays null: an empty frame is never rebased into
                        Unsafe.WriteUnaligned(ctx->writerPtr, count);
                        ctx->writerPtr += 4;

                        if (count == 0)
                        {
                            // Nothing to iterate: push no frame at all (a null
                            // collection has none to frame anyway). Every test
                            // on the way out fails against cursorEnd 0, and
                            // RepeatOrExit reads that as "no frame of ours".
                            cursorEnd = 0;
                            next += cmd->delta;
                            ensureSpace = cmd->exitEnsureSum;
                            goto EnsureAndAdvance;
                        }

                        // The frame is pushed on the skip arm too: the bulk tail
                        // it lands on iterates the same frame.
                        instance = dataAsBytes;
                        cursorEnd = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        if (cursorEnd - V2LayoutFacts.ArrayDataOffset >= (nint)cmd->stride)
                        {
                            ensureSpace = cmd->bodyEnsureSum;
                        }
                        else
                        {
                            next += cmd->delta;  // skip the body
                            ensureSpace = cmd->exitEnsureSum;
                        }
                        goto PushCollectionFrame;
                    }

                    case V2OpCode.CollectionBulkTail:
                    {
                        var cmd = (V2CmdCollectionBulkTail*)pos;
                        if (cursorEnd - cursorOffset >= (nint)cmd->stride)
                        {
                            // The chunk latch fell through without writing, so
                            // publish the cursor before the body runs on it.
                            top.Offset = cursorOffset;
                            baseAddr = ref V2RebaseTo(in top);
                            ensureSpace = cmd->bodyEnsureSum;
                        }
                        else
                        {
                            next += cmd->delta;
                            ensureSpace = cmd->exitEnsureSum;
                        }
                        goto EnsureAndAdvance;
                    }

                    case V2OpCode.CollectionRepeatOrFallthrough:
                    {
                        // Advance the register cursor, test, and only publish
                        // it when another iteration will run — the failing arm
                        // leaves the frame alone, so a collection that pushed
                        // no frame cannot write into its parent's.
                        var cmd = (V2CmdCollectionRepeat*)pos;
                        cursorOffset += (nint)cmd->stride;
                        if (cursorEnd - cursorOffset >= (nint)cmd->stride)
                        {
                            top.Offset = cursorOffset;
                            baseAddr = ref V2RebaseTo(in top);
                            next -= cmd->delta;
                            ensureSpace = cmd->bodyEnsureSum;
                            goto EnsureAndAdvance;
                        }
                        break;  // the bulk tail takes over
                    }

                    case V2OpCode.CollectionRepeatOrExit:
                    {
                        var cmd = (V2CmdCollectionRepeat*)pos;
                        cursorOffset += (nint)cmd->stride;
                        if (cursorEnd - cursorOffset >= (nint)cmd->stride)
                        {
                            top.Offset = cursorOffset;
                            baseAddr = ref V2RebaseTo(in top);
                            next -= cmd->delta;
                            ensureSpace = cmd->bodyEnsureSum;
                            goto EnsureAndAdvance;
                        }
                        if (cursorEnd != 0)
                        {
                            // Our frame: pop it. baseAddr and the cursor come
                            // back from the frame beneath.
                            top.Instance = null;
                            top = ref Unsafe.Subtract(ref top, 1);
                            baseAddr = ref V2RebaseTo(in top);
                        }
                        // Else the collection was empty and pushed nothing:
                        // baseAddr never moved, and the advance above only
                        // touched the register copy.
                        cursorOffset = top.Offset;
                        cursorEnd = top.EndOffset;
                        ensureSpace = cmd->exitEnsureSum;
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

                        int stagingDepth = V2FrameIndexOf(frames, ref top);
                        top.Instance = null;
                        top = ref Unsafe.Subtract(ref top, 1);
                        cursorOffset = top.Offset;
                        cursorEnd = top.EndOffset;
                        baseAddr = ref V2RebaseTo(in top);
                        if ((pushedDictFrames & (1ul << stagingDepth)) != 0)
                        {
                            pushedDictFrames &= ~(1ul << stagingDepth);
                            PopDictionaryFUIDFrame();
                            --openDictFrames;
                        }
                        ensureSpace = cmd->exitEnsureSum;
                        goto EnsureAndAdvance;
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

                    // The gather stream's form of the group: its handler also
                    // gets the FUID context it formats its sites' property
                    // paths from. Stamped only over groups carrying a handler.
                    case V2OpCode.ExternalFixedWithFUID:
                    {
                        var cmd = (V2CmdExternalFixedGroup*)pos;
                        var entries = (V2ExternalFixedEntry*)(pos + sizeof(V2CmdExternalFixedGroup));
                        int count = cmd->count;
                        uint* segments = (cmd->pad0 & kV2ExternalFixedFlagHasSegments) != 0
                            ? (uint*)(next - count * sizeof(uint))
                            : null;
                        ((delegate*<ref byte, byte*, int, byte*, uint*, Span<V2ObjectFrame>, NativeBufferContext*, ulong, void>)(void*)cmd->groupHandler)(
                            ref baseAddr, (byte*)entries, count, output, segments, frames, ctx, cmd->userData);
                        break;
                    }

                    case V2OpCode.ExternalDynamic:
                    {
                        var cmd = (V2CmdExternalDynamic*)pos;
                        ((delegate*<ref byte, NativeBufferContext*, ulong, ulong, void>)(void*)cmd->handler)(
                            ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset),
                            ctx, cmd->userData, cmd->userData2);
                        // The handler checks its own room; re-establish the
                        // trailing guarantee.
                        ensureSpace = cmd->ensureSum;
                        goto EnsureAndAdvance;
                    }

                    case V2OpCode.ExternalArray:
                    {
                        var cmd = (V2CmdExternalArray*)pos;

                        ensureSpace = cmd->ensureSum;

                        // The enter read the collection, wrote the count and pushed the element
                        // frame, so the backing comes from the frame and the count from the cursor.
                        byte[] dataAsBytes = Unsafe.As<byte[]>(top.Instance);
                        int count = (int)((cursorEnd - cursorOffset) / (nint)cmd->elementStride);

                        if (cmd->arrayHandler != 0)
                        {
                            // Batched path: the handler owns the payload and its pad in one call, so
                            // it can batch its native crossings.
                            ((delegate*<byte[], int, uint, NativeBufferContext*, ulong, void>)(void*)cmd->arrayHandler)(
                                dataAsBytes, count, cmd->elementStride, ctx, cmd->userData);
                            // Every element is consumed, so the trailing latch exits and pops the frame.
                            cursorOffset = cursorEnd;
                            top.Offset = cursorEnd;
                            goto EnsureAndAdvance;
                        }

                        // Per-element fallback: the executor frames, and the
                        // field handler fills whatever records the window holds.
                        int wire = (int)cmd->elementWireBytes;
                        var fieldHandler = (delegate*<ref byte, byte*, NativeBufferContext*, ulong, void>)(void*)cmd->fieldHandler;
                        int processed = 0;
                        while (processed < count)
                        {
                            if (ctx->writerEnd - ctx->writerPtr < wire)
                                ctx->ensureWritable(ctx, wire);
                            int batch = Math.Min((int)(ctx->writerEnd - ctx->writerPtr) / wire, count - processed);
                            byte* dst = ctx->writerPtr;
                            for (int i = 0; i < batch; ++i)
                            {
                                ref byte element = ref Unsafe.AddByteOffset(
                                    ref Unsafe.As<ObjectWrapper>((object)dataAsBytes).Data,
                                    V2LayoutFacts.ArrayDataOffset + (nint)((long)(processed + i) * cmd->elementStride));
                                fieldHandler(ref element, dst + i * wire, ctx, cmd->userData);
                            }
                            ctx->writerPtr += batch * wire;
                            processed += batch;
                        }

                        int padBytes = (4 - ((count * wire) & 3)) & 3;
                        if (padBytes > 0)
                        {
                            if (ctx->writerEnd - ctx->writerPtr < padBytes)
                                ctx->ensureWritable(ctx, padBytes);
                            Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
                            ctx->writerPtr += padBytes;
                        }
                        // Every element is consumed, so the trailing latch exits and pops the frame.
                        cursorOffset = cursorEnd;
                        top.Offset = cursorEnd;
                        goto EnsureAndAdvance;
                    }

                    // The gather stream's form of the collection, per the
                    // group above; no per-element arm, since it is stamped only
                    // over collections carrying a batched handler.
                    case V2OpCode.ExternalArrayWithFUID:
                    {
                        var cmd = (V2CmdExternalArray*)pos;
                        ensureSpace = cmd->ensureSum;

                        // The filter copies the command verbatim, so the enter owns the collection
                        // here exactly as it does for ExternalArray: the backing comes from the
                        // element frame, the count from the cursor, and a null collection never
                        // reaches this arm.
                        byte[] dataAsBytes = Unsafe.As<byte[]>(top.Instance);
                        int count = (int)((cursorEnd - cursorOffset) / (nint)cmd->elementStride);

                        ((delegate*<byte[], int, uint, uint, Span<V2ObjectFrame>, NativeBufferContext*, ulong, void>)(void*)cmd->arrayHandler)(
                            dataAsBytes, count, cmd->elementStride, cmd->segment, frames, ctx, cmd->userData);
                        // Every element is consumed, so the trailing latch exits and pops the frame.
                        cursorOffset = cursorEnd;
                        top.Offset = cursorEnd;
                        goto EnsureAndAdvance;
                    }

                    case V2OpCode.EnterElementSource:
                    {
                        var cmd = (V2CmdEnterElementSource*)pos;

                        // A null collection writes count 0 with no FUID frame
                        // and no handler call; v1's ConsumeDictionary pushes
                        // after its null check and never bridges a null.
                        object collection = V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset);
                        if (collection == null)
                        {
                            Unsafe.WriteUnaligned(ctx->writerPtr, 0);
                            ctx->writerPtr += 4;
                            next += cmd->bodyBytes;
                            ensureSpace = cmd->exitEnsureSum;
                            goto EnsureAndAdvance;
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

                        // The source handler stages nothing, so the preceding
                        // ensure point still covers the count.
                        int count = elements != null ? elements.Length : 0;
                        Unsafe.WriteUnaligned(ctx->writerPtr, count);
                        ctx->writerPtr += 4;
                        if (count == 0)
                        {
                            if (pushedFuidFrame)
                            {
                                PopDictionaryFUIDFrame();
                                --openDictFrames;
                            }
                            next += cmd->bodyBytes;
                            ensureSpace = cmd->exitEnsureSum;
                            goto EnsureAndAdvance;
                        }

                        // Remember the successful push at the staging frame's
                        // depth (one above top); the exit arm pops against it.
                        if (pushedFuidFrame)
                            pushedDictFrames |= 1ul << (V2FrameIndexOf(frames, ref top) + 1);

                        // Element frame over the staged array (tracked, no
                        // pin); the body is the ordinary composed element
                        // template ending in its exec exit. No tail pad:
                        // composed element bodies are 4-byte multiples.
                        instance = elements;
                        cursorEnd = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        ensureSpace = cmd->bodyEnsureSum;
                        goto PushCollectionFrame;
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
                                V2InvokeCallbackStruct((delegate*<ref byte, void>)(void*)cmd->methodFnPtr,
                                    ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                            }
                        }
                        else
                        {
                            // Interface dispatch on the current frame's
                            // instance; the composer places the bracket
                            // directly after the frame's EnterObject (root
                            // brackets ride frame 0's host). A materialized
                            // slot fires nothing and writes ctor defaults, the
                            // null-slot contract. The cast cannot fail: the
                            // bracket is only emitted for declared classes
                            // implementing the interface, and the frame holds
                            // an assignment-compatible non-null instance.
                            if (!enterMaterialized)
                                V2InvokeOnBeforeSerialize(top.Instance);
                        }
                        break;
                    }

                    // The gather stream's brackets. The gather owns their only
                    // fire (publish strips them from the exec stream), so a
                    // slot the executor just materialized still gets one.
                    // Write-direction by construction: no direction test.
                    case V2OpCode.InvokeCallbackIgnoresNullSlot:
                    {
                        var cmd = (V2CmdInvokeCallback*)pos;
                        if ((cmd->flavor & kV2CallbackFlavorMask) == kV2CallbackFlavorStruct)
                        {
                            if (cmd->methodFnPtr != 0)
                            {
                                V2InvokeCallbackStruct((delegate*<ref byte, void>)(void*)cmd->methodFnPtr,
                                    ref Unsafe.AddByteOffset(ref baseAddr, (nint)cmd->fieldOffset));
                            }
                        }
                        else
                        {
                            V2InvokeOnBeforeSerialize(top.Instance);
                        }
                        break;
                    }

                    // The last window stays staged; the native caller commits it.
                    case V2OpCode.End:
                        return;

                    default:
                        throw new InvalidOperationException(
                            $"ObjectsToSerializationBufferV2: unknown V2 opcode {pos[0]} (out-of-sync Commands.h mirror?)");
                }
                pos = next;
                continue;

            EnterObjectTail:
                if ((((V2CmdEnterObject*)pos)->pad0 & kV2Pad0FlagEnterNoFrame) != 0)
                {
                    // Frameless body: only the field base changes, and the
                    // exit restores it from parentBase.
                    parentBase = ref baseAddr;
                    baseAddr = ref Unsafe.As<ObjectWrapper>(instance).Data;
                    pos = next;
                    continue;
                }
                top = ref Unsafe.Add(ref top, 1);          // push (no bounds check; capacity is template metadata)
                top.Instance = instance;
                top.Offset = 0;
                baseAddr = ref Unsafe.As<ObjectWrapper>(instance).Data;
                // Zero wire bytes: the open FixedBlock stays open.
                pos = next;
                continue;

                // Collection and element-source frames: the cursor starts at
                // the first element, and the register copy mirrors the frame.
                // Falls through to the ensure for the arm's chosen path.
            PushCollectionFrame:
                cursorOffset = V2LayoutFacts.ArrayDataOffset;
                top = ref Unsafe.Add(ref top, 1);
                top.Instance = instance;
                top.Offset = cursorOffset;
                top.EndOffset = cursorEnd;
                baseAddr = ref V2RebaseTo(in top);

                // Doc §12: the shared trailing ensure. Arms whose exit carries a
                // trailing window guarantee set ensureSpace and jump here;
                // leading guarantees (guarded blocks) stay in their arms, and
                // the bulk-copy crossing owns its own. Hot arms break past this.
            EnsureAndAdvance:
                if (ctx->writerEnd - ctx->writerPtr < ensureSpace)
                    ctx->ensureWritable(ctx, ensureSpace);
                pos = next;
            }
        }
    }
}
