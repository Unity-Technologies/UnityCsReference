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
            // The parent's field base across a frameless object body
            // (kV2Pad0FlagEnterNoFrame); such bodies hold no enter, so one
            // slot is enough.
            ref byte parentBase = ref baseAddr;

            // Register copy of top's cursor pair. Collection commands read it
            // instead of the frame, and write Offset back whenever a body is
            // about to run, so anything that reads frames[] from inside a body
            // (FUID index recovery, the SR group crossing) sees a live cursor.
            // Every pop reloads it, which is what carries an enclosing loop's
            // cursor back across nested frames. cursorEnd == 0 marks a
            // collection that pushed no frame (see CollectionEnterOrSkip).
            nint cursorOffset = 0;
            nint cursorEnd = 0;

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
                // Shared by each collection family's plain/factory case pair;
                // the pair's shared tail consumes it.
                int collectionCount;
                // The object the next pushed frame holds: an entered class
                // instance or a collection's backing array. Shared by the
                // enter arms and the tails that push their frame.
                object instance;
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

                    case V2OpCode.DirectCopy4:
                    {
                        // Typed store: the lowering routes every field offset
                        // that is not 4-aligned to DirectCopy4Unaligned, so
                        // this arm's offset is aligned by contract.
                        var cmd = (V2CmdDirectCopy*)pos;
                        V2FieldRef<int>(ref baseAddr, (nint)cmd->fieldOffset) =
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
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            V2FieldRef<int>(ref baseAddr, (nint)(e[i].fieldOffset << 2)) =
                                Unsafe.ReadUnaligned<int>(src + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy4N_8x4:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;  // multiple of 4 by construction
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; i += 4)
                        {
                            V2FieldRef<int>(ref baseAddr, (nint)(e[i].fieldOffset << 2)) =
                                Unsafe.ReadUnaligned<int>(src + (e[i].destOffset << 2));
                            V2FieldRef<int>(ref baseAddr, (nint)(e[i + 1].fieldOffset << 2)) =
                                Unsafe.ReadUnaligned<int>(src + (e[i + 1].destOffset << 2));
                            V2FieldRef<int>(ref baseAddr, (nint)(e[i + 2].fieldOffset << 2)) =
                                Unsafe.ReadUnaligned<int>(src + (e[i + 2].destOffset << 2));
                            V2FieldRef<int>(ref baseAddr, (nint)(e[i + 3].fieldOffset << 2)) =
                                Unsafe.ReadUnaligned<int>(src + (e[i + 3].destOffset << 2));
                        }
                        break;
                    }

                    case V2OpCode.DirectCopy4N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            V2FieldRef<int>(ref baseAddr, (nint)(e[i].fieldOffset << 2)) =
                                Unsafe.ReadUnaligned<int>(src + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy4N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            V2FieldRef<int>(ref baseAddr, (nint)e[i].fieldOffset << 2) =
                                Unsafe.ReadUnaligned<int>(src + ((nint)e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_8:
                    {
                        // The write side stored zero-extended 4-byte slots; the
                        // value is the slot's low ushort.
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            V2FieldRef<ushort>(ref baseAddr, (nint)(e[i].fieldOffset << 1)) =
                                Unsafe.ReadUnaligned<ushort>(src + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            V2FieldRef<ushort>(ref baseAddr, (nint)(e[i].fieldOffset << 1)) =
                                Unsafe.ReadUnaligned<ushort>(src + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy2N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            V2FieldRef<ushort>(ref baseAddr, (nint)e[i].fieldOffset << 1) =
                                Unsafe.ReadUnaligned<ushort>(src + ((nint)e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy1N_8:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry8*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset) = *(src + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy1N_16:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry16*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset) = *(src + (e[i].destOffset << 2));
                        break;
                    }

                    case V2OpCode.DirectCopy1N_32:
                    {
                        var cmd = (V2CmdDirectCopyN*)pos;
                        int n = cmd->count;
                        var e = (V2CopyEntry32*)(pos + sizeof(V2CmdDirectCopyN));
                        byte* src = input;
                        for (int i = 0; i < n; ++i)
                            Unsafe.AddByteOffset(ref baseAddr, (nint)e[i].fieldOffset) = *(src + ((nint)e[i].destOffset << 2));
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
                        V2FieldRef<ushort>(ref baseAddr, (nint)cmd->fieldOffset) =
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
                        ref object slot = ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset);
                        instance = slot;
                        if (instance == null)
                        {
                            instance = V2ConstructRegistered(cmd->constructorIndex);
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
                        if (instance == null)
                        {
                            var ctor = (V2Constructor)SerializationCommandObjectTable.Get(cmd->constructorIndex);
                            instance = slot = RuntimeHelpers.GetUninitializedObject(ctor.Type);
                        }

                        goto EnterObjectTail;
                    }

                    case V2OpCode.EnterObjectFallback:
                    {
                        var cmd = (V2CmdEnterObjectFallback*)pos;
                        ref object slot = ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset);
                        instance = slot;
                        if (instance == null)
                            instance = slot = V2CreateInstanceFallback((nint)cmd->runtimeTypeHandle, (nint)cmd->ctorFunctionPtr);

                        goto EnterObjectTail;
                    }

                    case V2OpCode.EnterObjectFactory:
                    {
                        var cmd = (V2CmdEnterObjectFactory*)pos;
                        ref object slot = ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset);
                        instance = slot;
                        if (instance == null)
                        {
                            instance = V2ConstructWithFactory(cmd->factoryFunctionPtr, cmd->constructorIndex);
                            slot = instance;
                        }

                        goto EnterObjectTail;
                    }

                    case V2OpCode.ExitObject:
                    {
                        top.Instance = null;
                        top = ref Unsafe.Subtract(ref top, 1);
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
                        // The trailing guarantee folds into the read's body
                        // ensure (doc §12): one branch for in-window strings.
                        V2FieldRef<string>(ref baseAddr, (nint)cmd->fieldOffset) =
                            V2ReadFramedString(ctx, cmd->ensureSum);
                        break;
                    }

                    // Each collection family is a plain/factory case pair
                    // sharing a tail: the plain case materializes by
                    // reflection and jumps; the factory case callis its
                    // stamped sized factory ((ref object slot, int n) ->
                    // backing; owns reuse decision, allocation, and slot
                    // store) and falls through. The tail reads only
                    // shared-prefix fields, valid across both layouts.
                    case V2OpCode.LinearCollectionMemCpy:
                    {
                        var cmd = (V2CmdLinearCollectionMemCpy*)pos;

                        // Unchecked count (doc §12).
                        collectionCount = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
                        ctx->readerPtr += 4;

                        instance = V2ReuseOrAllocateCollection(
                            ref baseAddr, cmd->fieldOffset, cmd->kind, cmd->elementTypeHandle, collectionCount);
                        goto MemCpyTail;
                    }

                    case V2OpCode.LinearCollectionMemCpyFactory:
                    {
                        var cmd = (V2CmdLinearCollectionMemCpyFactory*)pos;

                        // Unchecked count (doc §12).
                        collectionCount = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
                        ctx->readerPtr += 4;

                        instance = ((delegate*<ref object, int, object>)cmd->sizedFactoryPtr)(
                            ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset),
                            collectionCount);
                    }
                    MemCpyTail:
                    {
                        var cmd = (V2CmdLinearCollectionMemCpy*)pos;
                        if (collectionCount > 0)
                        {
                            int totalBytes = checked((int)((long)collectionCount * cmd->elementStride));
                            int padBytes = (4 - (totalBytes & 3)) & 3;
                            if (ctx->readerEnd - ctx->readerPtr >= (long)totalBytes + padBytes)
                            {
                                fixed (byte* dataPtr = Unsafe.As<byte[]>(instance))
                                    Buffer.MemoryCopy(ctx->readerPtr, dataPtr, totalBytes, totalBytes);
                                ctx->readerPtr += totalBytes + padBytes;
                                ensureSpace = cmd->ensureSum;
                                goto EnsureAndAdvance;
                            }

                            // The window does not hold it: one crossing copies
                            // what the window has, reads the rest through the
                            // reader, skips the pad, and takes the trailing
                            // guarantee at the 4-aligned post-pad position.
                            fixed (byte* dataPtr = Unsafe.As<byte[]>(instance))
                                InvokeReadBytesDirect(ctx, dataPtr, totalBytes, padBytes, cmd->ensureSum);
                            break;
                        }

                        ensureSpace = cmd->ensureSum;
                        goto EnsureAndAdvance;
                    }

                    // Collection loop: one test, Offset + stride <= EndOffset,
                    // in three positions (see the opcode comment). Entering a
                    // body rebases without advancing, so the cursor points at
                    // the element the body runs on; the latches advance first.
                    case V2OpCode.CollectionEnterOrSkip:
                    {
                        var cmd = (V2CmdCollectionEnterOrSkip*)pos;

                        // Unchecked count (doc §12).
                        collectionCount = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
                        ctx->readerPtr += 4;

                        instance = V2ReuseOrAllocateCollection(
                            ref baseAddr, cmd->fieldOffset, cmd->kind, cmd->elementTypeHandle, collectionCount);
                        goto CollectionEnterTail;
                    }

                    case V2OpCode.CollectionEnterOrSkipFactory:
                    {
                        var cmd = (V2CmdCollectionEnterOrSkipFactory*)pos;

                        // Unchecked count (doc §12).
                        collectionCount = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
                        ctx->readerPtr += 4;

                        instance = ((delegate*<ref object, int, object>)cmd->sizedFactoryPtr)(
                            ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset),
                            collectionCount);
                    }
                    CollectionEnterTail:
                    {
                        var cmd = (V2CmdCollectionEnterOrSkip*)pos;
                        if (collectionCount == 0)
                        {
                            // Nothing to iterate: push no frame at all. Every
                            // test on the way out fails against cursorEnd 0,
                            // and RepeatOrExit reads that as "no frame of
                            // ours" and restores instead of popping.
                            cursorEnd = 0;
                            next += cmd->delta;
                            ensureSpace = cmd->exitEnsureSum;
                            goto EnsureAndAdvance;
                        }

                        // The frame is pushed on the skip arm too: the bulk tail
                        // it lands on iterates the same frame.
                        cursorEnd = V2LayoutFacts.ArrayDataOffset + (nint)((long)collectionCount * cmd->elementStride);
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
                        break;
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
                        cursorOffset = top.Offset;
                        cursorEnd = top.EndOffset;
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

                        Type elementType = UnmarshalSystemType((nint)cmd->elementTypeHandle);
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

                        // Remember the successful push at the staging frame's
                        // depth (one above top); the exit arm pops against it.
                        // Depths past the bitmask are beyond the serialization
                        // depth cap. The shared replenish guarantees the body's
                        // leading statics for iteration one.
                        if (pushedFuidFrame)
                            pushedDictFrames |= 1ul << (V2FrameIndexOf(frames, ref top) + 1);
                        instance = elements;
                        cursorEnd = V2LayoutFacts.ArrayDataOffset + (nint)((long)count * cmd->elementStride);
                        ensureSpace = cmd->bodyEnsureSum;
                        goto PushCollectionFrame;
                    }

                    // ISerializationCallbackReceiver bracket. Write-direction
                    // (OnBeforeSerialize) brackets are skipped. Read-direction
                    // brackets (OnAfterDeserialize, positioned after the
                    // receiver's data) invoke inline, the write bracket's
                    // mirror: registry-first data has every reference patched
                    // before any body's bracket fires, so there is nothing to
                    // defer for. Suppressed transfers (import metadata reads
                    // with kLoadForNativeFormatImporter) never reach this
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
                                V2InvokeCallbackStruct((delegate*<ref byte, void>)(void*)cmd->methodFnPtr,
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
                            V2InvokeOnAfterDeserialize(top.Instance);
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
                top = ref Unsafe.Add(ref top, 1);
                top.Instance = instance;
                top.Offset = 0;
                baseAddr = ref Unsafe.As<ObjectWrapper>(instance).Data;
                pos = next;
                continue;

                // Collection and element-source frames: the cursor starts at
                // the first element, and the register copy mirrors the frame.
                // Falls through to the replenish for the arm's chosen path.
            PushCollectionFrame:
                cursorOffset = V2LayoutFacts.ArrayDataOffset;
                top = ref Unsafe.Add(ref top, 1);
                top.Instance = instance;
                top.Offset = cursorOffset;
                top.EndOffset = cursorEnd;
                baseAddr = ref V2RebaseTo(in top);

                // Doc §12: the shared trailing replenish. Arms whose exit
                // carries a trailing window guarantee set ensureSpace and jump
                // here; leading guarantees (guarded blocks) stay in their
                // arms, and the bulk-read crossings own their trailing pads
                // and guarantees. Hot arms break past this entirely.
            EnsureAndAdvance:
                if (ctx->readerEnd - ctx->readerPtr < ensureSpace)
                    InvokeEnsureReadable(ctx, ensureSpace);
                pos = next;
            }
        }
    }

}
