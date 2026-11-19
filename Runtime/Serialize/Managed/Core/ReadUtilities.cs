// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Serialization;

// Managed serialization V2: read-side helpers used by ReadExecutor.cs.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // The handle ends the staged region; the count and the element bytes come from its allocation,
    // and an unreadable allocation reads as empty.
    private static unsafe int V2LookupOutOfLineCollection(NativeReadBufferContext* ctx, uint elementStride, out byte* allocation)
    {
        int count = V2LookupOutOfLine(ctx, V2ReadOutOfLineHandle(ctx), (int)elementStride, out allocation);
        return count < 0 ? 0 : count;
    }

    private static unsafe void V2CopyOutOfLineElements(byte[] backing, byte* allocation, int count, uint elementStride)
    {
        if (count <= 0)
            return;
        int byteCount = checked((int)((long)count * elementStride));
        fixed (byte* dataPtr = backing)
        {
            Unsafe.CopyBlockUnaligned(dataPtr, allocation, (uint)byteCount);
        }
    }

    // Keeps the existing array when the length matches, and a list's backing
    // when it is large enough (v1's reuse-or-allocate contract), and binds the
    // result to the field: arrays store the reference, lists refill in place
    // to preserve instance identity, allocating an uninitialized List only
    // when the field is null (native LinearCollectionField::SetArray). The
    // caller fills the returned backing afterwards; nothing runs between the
    // bind and the fill that could observe the collection. Serves the plain
    // collection opcodes; the factory variants calli their stamped sized
    // factory instead (same materialization contract, no reflection).
    private static unsafe byte[] V2ReuseOrAllocateCollection(
        ref byte baseAddr, uint fieldOffset, byte kind, ulong elementTypeHandle, int count)
    {
        Type elementType = UnmarshalSystemType((nint)elementTypeHandle);
        ref byte fieldSlot = ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset);
        if ((kind & kV2Pad0KindMask) == 0)  // array
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

    // kMappedSkipSubtree: steps over a stored subtree the way SafeBinaryRead's Walk does. The command starts on a 4-byte
    // boundary of the data, so the bytes stepped over since its start decide each alignment.
    [MethodImpl(MethodImplOptions.NoInlining)]
    // The program after a MappedSkipSubtree or MappedEnterStoredArea command: inline, or a V2ExternalProgram when
    // programWords is 0.
    private static unsafe uint* V2ProgramAfterCommand(byte* afterCommand, ushort programWords, out int words)
    {
        if (programWords != 0)
        {
            words = programWords;
            return (uint*)afterCommand;
        }
        var external = (V2ExternalProgram*)afterCommand;
        words = (int)external->count;
        return (uint*)external->words;
    }

    private static unsafe void V2SkipStoredSubtree(NativeReadBufferContext* ctx, uint* program, int programWords)
    {
        long offset = 0;
        // The closing kEnd is not part of the range.
        V2RunSkipRange(ctx, program, program + programWords - 1, ref offset);
    }

    private static unsafe void V2RunSkipRange(NativeReadBufferContext* ctx, uint* op, uint* end, ref long offset)
    {
        while (op < end)
        {
            switch ((V2SkipOp)op[0])
            {
                case V2SkipOp.Fixed:
                    V2SkipBytes(ctx, op[1]);
                    offset += op[1];
                    op += 2;
                    break;

                case V2SkipOp.Align:
                {
                    int pad = (int)((4 - (offset & 3)) & 3);
                    V2SkipBytes(ctx, pad);
                    offset += pad;
                    op += 1;
                    break;
                }

                case V2SkipOp.ArrayFixed:
                {
                    long bytes = (long)V2ReadSkippedCount(ctx) * op[1];
                    V2SkipBytes(ctx, bytes);
                    offset += 4 + bytes;
                    op += 2;
                    break;
                }

                case V2SkipOp.Array:
                {
                    int count = V2ReadSkippedCount(ctx);
                    offset += 4;
                    uint* body = op + 2;
                    uint* bodyEnd = body + op[1];
                    for (int i = 0; i < count; ++i)
                        V2RunSkipRange(ctx, body, bodyEnd, ref offset);
                    op = bodyEnd;
                    break;
                }

                case V2SkipOp.Record:
                    if (ctx->storedPositionCount >= ctx->storedPositionCapacity)
                        throw new InvalidOperationException("kRecord: the stored position stack is full.");
                    ctx->storedPositions[ctx->storedPositionCount++] = V2TellReader(ctx);
                    op += 1;
                    break;

                case V2SkipOp.RegistryFrame:
                {
                    V2SkipBytes(ctx, 4);
                    if (ctx->readerEnd - ctx->readerPtr < 4)
                        InvokeEnsureReadable(ctx, 4);
                    long length = Unsafe.ReadUnaligned<uint>(ctx->readerPtr);
                    ctx->readerPtr += 4;
                    V2SkipBytes(ctx, length);
                    offset += 8 + length;
                    op += 1;
                    break;
                }

                default:
                    throw new InvalidOperationException($"kMappedSkipSubtree: unknown skip op {op[0]}.");
            }
        }
    }

    // An area's slots sit above one slot holding the enclosing area's base, which its exit restores.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void V2EnterStoredArea(NativeReadBufferContext* ctx, V2CmdMappedEnterStoredArea* cmd)
    {
        if (ctx->storedPositionCount + 1 + cmd->slots > ctx->storedPositionCapacity)
            throw new InvalidOperationException("kMappedEnterStoredArea: the stored position stack is full.");
        ctx->storedPositions[ctx->storedPositionCount++] = (ulong)ctx->storedAreaBase;
        ctx->storedAreaBase = ctx->storedPositionCount;
        uint* program = V2ProgramAfterCommand((byte*)(cmd + 1), cmd->programWords, out int programWords);
        V2SkipStoredSubtree(ctx, program, programWords);
        if (ctx->storedPositionCount - ctx->storedAreaBase != cmd->slots)
            throw new InvalidOperationException("kMappedEnterStoredArea: the program recorded a different number of slots.");
    }

    // The last slot is the area's end.
    private static unsafe void V2ExitStoredArea(NativeReadBufferContext* ctx, V2CmdMappedStoredArea* cmd)
    {
        int areaBase = ctx->storedAreaBase;
        V2SeekReader(ctx, ctx->storedPositions[areaBase + cmd->slot - 1], cmd->ensureSum);
        ctx->storedPositionCount = areaBase - 1;
        ctx->storedAreaBase = (int)ctx->storedPositions[areaBase - 1];
    }

    // The wire position of readerPtr.
    private static unsafe ulong V2TellReader(NativeReadBufferContext* ctx)
    {
        if (ctx->windowStart != null)
            return ctx->windowEndPosition - (ulong)(ctx->readerEnd - ctx->readerPtr);
        return ctx->tellReader(ctx);
    }

    // kMappedSeekArea and kMappedExitStoredArea: inside the window with ensureBytes left after the target, a pointer move.
    private static unsafe void V2SeekReader(NativeReadBufferContext* ctx, ulong position, int ensureBytes)
    {
        if (ctx->windowStart != null && position <= ctx->windowEndPosition)
        {
            long fromEnd = (long)(ctx->windowEndPosition - position);
            if (fromEnd <= ctx->readerEnd - ctx->windowStart && fromEnd >= ensureBytes)
            {
                ctx->readerPtr = ctx->readerEnd - fromEnd;
                return;
            }
        }
        ctx->seekReader(ctx, position, ensureBytes);
    }

    // kMappedAssignEmpty: what the field's own read binds at count 0, through the same reuse-or-allocate path.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void V2AssignEmptyValue(ref byte baseAddr, V2CmdMappedAssignEmpty* cmd)
    {
        if ((cmd->flags & kV2AssignEmptyFlagIfNull) != 0 && V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset) != null)
            return;
        switch (cmd->mode)
        {
            case V2AssignEmptyMode.String:
                V2FieldRef<string>(ref baseAddr, (nint)cmd->fieldOffset) = string.Empty;
                break;
            case V2AssignEmptyMode.Collection:
                V2ReuseOrAllocateCollection(ref baseAddr, cmd->fieldOffset, cmd->pad0, cmd->elementTypeOrFactory, 0);
                break;
            case V2AssignEmptyMode.CollectionFactory:
                ((delegate*<ref object, int, object>)cmd->elementTypeOrFactory)(
                    ref V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset), 0);
                break;
            case V2AssignEmptyMode.Dictionary:
                V2FieldRef<object>(ref baseAddr, (nint)cmd->fieldOffset) =
                    ((Func<object>)SerializationCommandObjectTable.Get(unchecked((int)cmd->elementTypeOrFactory)))();
                break;
            default:
                throw new InvalidOperationException($"kMappedAssignEmpty: unknown mode {cmd->mode}.");
        }
    }

    // kMappedConvertPrimitive: the conversions SafeBinaryRead registers are C++ assignments from the stored type to the
    // field's, so integers wrap to the target width, a bool target is nonzero, and floating point rounds to nearest.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void V2ReadConvertedPrimitive(NativeReadBufferContext* ctx, ref byte field, V2CmdMappedConvertPrimitive* cmd)
    {
        int size = V2PrimitiveSize(cmd->storedKind);
        if (ctx->readerEnd - ctx->readerPtr < size)
            InvokeEnsureReadable(ctx, size);
        byte* input = ctx->readerPtr;

        ulong bits = 0;
        double real = 0;
        switch (cmd->storedKind)
        {
            case V2PrimitiveKind.Bool:
            case V2PrimitiveKind.UInt8: bits = *input; break;
            case V2PrimitiveKind.SInt8: bits = (ulong)(long)*(sbyte*)input; break;
            case V2PrimitiveKind.SInt16: bits = (ulong)(long)Unsafe.ReadUnaligned<short>(input); break;
            case V2PrimitiveKind.UInt16: bits = Unsafe.ReadUnaligned<ushort>(input); break;
            case V2PrimitiveKind.SInt32: bits = (ulong)(long)Unsafe.ReadUnaligned<int>(input); break;
            case V2PrimitiveKind.UInt32: bits = Unsafe.ReadUnaligned<uint>(input); break;
            case V2PrimitiveKind.SInt64:
            case V2PrimitiveKind.UInt64: bits = Unsafe.ReadUnaligned<ulong>(input); break;
            case V2PrimitiveKind.Float: real = Unsafe.ReadUnaligned<float>(input); break;
            case V2PrimitiveKind.Double: real = Unsafe.ReadUnaligned<double>(input); break;
            default: throw new InvalidOperationException($"kMappedConvertPrimitive: unknown stored kind {cmd->storedKind}.");
        }
        ctx->readerPtr += size;

        if ((cmd->flags & kV2ConvertFlagDiscard) != 0)
        {
            if ((cmd->flags & kV2ConvertFlagElement) != 0)
                Unsafe.InitBlockUnaligned(ref field, 0, (uint)V2PrimitiveSize(cmd->targetKind));
            V2SkipBytes(ctx, cmd->storedPad);
            return;
        }

        switch (cmd->targetKind)
        {
            case V2PrimitiveKind.Bool: field = bits != 0 ? (byte)1 : (byte)0; break;
            case V2PrimitiveKind.SInt8:
            case V2PrimitiveKind.UInt8: field = unchecked((byte)bits); break;
            case V2PrimitiveKind.SInt16:
            case V2PrimitiveKind.UInt16: Unsafe.As<byte, ushort>(ref field) = unchecked((ushort)bits); break;
            case V2PrimitiveKind.SInt32:
            case V2PrimitiveKind.UInt32: Unsafe.As<byte, uint>(ref field) = unchecked((uint)bits); break;
            case V2PrimitiveKind.SInt64:
            case V2PrimitiveKind.UInt64: Unsafe.As<byte, ulong>(ref field) = bits; break;
            case V2PrimitiveKind.Float:
                Unsafe.As<byte, float>(ref field) = cmd->storedKind == V2PrimitiveKind.SInt32 ? (int)bits : (float)real;
                break;
            case V2PrimitiveKind.Double: Unsafe.As<byte, double>(ref field) = real; break;
            default: throw new InvalidOperationException($"kMappedConvertPrimitive: unknown target kind {cmd->targetKind}.");
        }

        V2SkipBytes(ctx, cmd->storedPad);
    }

    // The pad after a collection's stored elements, stepped over by its last element: SafeBinaryRead's Walk aligns
    // past the count and the elements, which start on a 4-byte boundary.
    private static unsafe void V2SkipConvertedCollectionPad(NativeReadBufferContext* ctx, V2CmdMappedConvertPrimitive* cmd, nint cursorEnd)
    {
        long count = (cursorEnd - V2LayoutFacts.ArrayDataOffset) / cmd->elementStride;
        long elementBytes = count * V2PrimitiveSize(cmd->storedKind);
        V2SkipBytes(ctx, (4 - (elementBytes & 3)) & 3);
    }

    private static int V2PrimitiveSize(V2PrimitiveKind kind)
    {
        switch (kind)
        {
            case V2PrimitiveKind.Bool:
            case V2PrimitiveKind.SInt8:
            case V2PrimitiveKind.UInt8: return 1;
            case V2PrimitiveKind.SInt16:
            case V2PrimitiveKind.UInt16: return 2;
            case V2PrimitiveKind.SInt32:
            case V2PrimitiveKind.UInt32:
            case V2PrimitiveKind.Float: return 4;
            default: return 8;
        }
    }

    private static unsafe int V2ReadSkippedCount(NativeReadBufferContext* ctx)
    {
        if (ctx->readerEnd - ctx->readerPtr < 4)
            InvokeEnsureReadable(ctx, 4);
        int count = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;
        if (count < 0)
            throw new InvalidOperationException(
                $"Managed deserialization read a negative array size ({count}) while skipping a field. The serialized data is corrupted.");
        return count;
    }

    // Consumes bytes through the window, refilling it at most a spill buffer at a time. A skip that reaches past the
    // next refill moves the reader instead, as v1 does, so a large removed field is not streamed through the window.
    private static unsafe void V2SkipBytes(NativeReadBufferContext* ctx, long bytes)
    {
        if (bytes > ctx->readerEnd - ctx->readerPtr + kManagedBlockSpillBufferSize)
        {
            ulong target = V2TellReader(ctx) + (ulong)bytes;
            if (target <= ctx->basePosition + (ulong)ctx->readableBytes)
            {
                V2SeekReader(ctx, target, 0);
                return;
            }
        }
        while (true)
        {
            long available = ctx->readerEnd - ctx->readerPtr;
            if (bytes <= available)
            {
                ctx->readerPtr += bytes;
                return;
            }
            ctx->readerPtr += available;
            bytes -= available;
            InvokeEnsureReadable(ctx, (int)Math.Min(bytes, kManagedBlockSpillBufferSize));
        }
    }

    // The span host sizes its destination for count elements, or returns null
    // to leave them unread. It must not read: the executor holds the window.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe byte* V2ResizeSpan(V2SpanHost* span, int count, NativeReadBufferContext* ctx)
        => ((delegate* unmanaged[Cdecl]<V2SpanHost*, int, NativeReadBufferContext*, byte*>)(void*)span->resize)(span, count, ctx);

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

    // The read mirror of the write side's mode test.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool V2IsOutOfLine(NativeReadBufferContext* ctx)
    {
        return ctx->outOfLineState != null;
    }

    // The element count of the allocation behind an out-of-line handle, with data at its bytes: 0 for
    // the invalid handle (empty), -1 when the allocation is unreadable or holds fewer than
    // count * stride bytes. Native resolves the invalid handle to count 0 as well; the test here only
    // saves the crossing for empty collections.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int V2LookupOutOfLine(NativeReadBufferContext* ctx, uint handle, int stride, out byte* data)
    {
        byte* allocation = null;
        int count = handle == kV2OutOfLineInvalidHandle ? 0 : ctx->lookupOutOfLine(ctx, handle, stride, &allocation);
        data = allocation;
        return count;
    }

    // Reads the inline out-of-line handle, the tail of the staged region.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint V2ReadOutOfLineHandle(NativeReadBufferContext* ctx)
    {
        return Unsafe.ReadUnaligned<uint>(ctx->readerPtr - 4);
    }

    // Reads a collection header, the tail of the staged region: the count, or on out-of-line transfers
    // the handle, which also opens the out-of-line window over the body. Returns the count; an
    // unreadable out-of-line allocation reads as an empty collection and opens no window.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int V2ReadCollectionHeader(NativeReadBufferContext* ctx, uint wireStride)
    {
        if (!V2IsOutOfLine(ctx))
            return Unsafe.ReadUnaligned<int>(ctx->readerPtr - 4);
        // As in V2LookupOutOfLine, the test only saves the crossing for empty collections.
        uint handle = V2ReadOutOfLineHandle(ctx);
        return handle == kV2OutOfLineInvalidHandle ? 0 : ctx->openOutOfLineWindow(ctx, handle, (int)wireStride);
    }

    private static unsafe void V2DispatchNativeRead(NativeReadBufferContext* ctx, IntPtr dispatchFn, IntPtr nativePtr)
        => ctx->dispatchNative(ctx, dispatchFn, nativePtr);
}
