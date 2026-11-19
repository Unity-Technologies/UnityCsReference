// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 fixed-buffer dynamic handler (doc §2.4) for C#
// unsafe fixed T buf[N]. Classic wire format: 4-byte element count, then
// N*elementSize inline bytes, then 0..3 zero pad. Out-of-line templates write the
// handle, with the bytes and the count in their own allocation (padded to 4).
// The command is header-framed: the count or handle ends the staged region. The
// composer (TypeFixedBuffer.cpp) stamps the per-field (count, elementSize)
// into the command's userDatas.
internal static unsafe partial class SerializationBackendManagedCommands
{
    private static unsafe void V2WriteFixedBuffer(ref byte field, NativeBufferContext* ctx, ulong elementCount, ulong elementSize)
    {
        int count = (int)elementCount;
        int totalBytes = count * (int)elementSize;

        fixed (byte* dataPtr = &field)
        {
            if (V2IsOutOfLine(ctx))
            {
                uint handle = kV2OutOfLineInvalidHandle;
                if (count > 0)
                {
                    handle = V2AllocateOutOfLine(ctx, count, totalBytes, out byte* allocation);
                    Unsafe.CopyBlockUnaligned(allocation, dataPtr, (uint)totalBytes);
                }
                Unsafe.WriteUnaligned(ctx->writerPtr - 4, handle);
                return;
            }

            Unsafe.WriteUnaligned(ctx->writerPtr - 4, count);
            int padBytes = (4 - (totalBytes & 3)) & 3;
            int room = (int)(ctx->writerEnd - ctx->writerPtr);
            if (room >= totalBytes + padBytes)
            {
                Unsafe.CopyBlockUnaligned(ctx->writerPtr, dataPtr, (uint)totalBytes);
                if (padBytes > 0)
                    Unsafe.InitBlockUnaligned(ctx->writerPtr + totalBytes, 0, (uint)padBytes);
                ctx->writerPtr += totalBytes + padBytes;
                return;
            }

            // The head fills the window; one crossing commits it and streams
            // the rest and the pad. The executor's arm re-arms for its
            // trailing sum.
            int head = Math.Min(room, totalBytes);
            Unsafe.CopyBlockUnaligned(ctx->writerPtr, dataPtr, (uint)head);
            ctx->writerPtr += head;
            ctx->writeBytesDirect(ctx, dataPtr + head, totalBytes - head, padBytes, 0);
        }
    }

    // Read (M7c): the count, then min(wire, capacity) elements into the inline
    // buffer, discarding the wire overflow (assets serialized when the buffer
    // was larger) plus the pad. A record inside the window is copied in
    // managed; otherwise one crossing streams it and skips the discard by a
    // reader position jump, never fetching it. On out-of-line templates the count
    // and elements come from the handle's allocation, and an unreadable allocation
    // leaves the buffer's contents unchanged. userData is the declared element count,
    // userData2 the element size.
    private static unsafe void V2ReadFixedBuffer(ref byte field, byte* cmdRaw, NativeReadBufferContext* ctx)
    {
        var cmd = (V2CmdExternalDynamic*)cmdRaw;
        int elementSize = (int)cmd->userData2;
        int capacity    = (int)cmd->userData;

        if (V2IsOutOfLine(ctx))
        {
            int count = V2LookupOutOfLine(ctx, V2ReadOutOfLineHandle(ctx), elementSize, out byte* allocation);
            int copyElements = count < capacity ? count : capacity;
            if (copyElements > 0)
            {
                fixed (byte* dstPtr = &field)
                    Unsafe.CopyBlockUnaligned(dstPtr, allocation, (uint)(copyElements * elementSize));
            }
            return;
        }

        int wireCount = Unsafe.ReadUnaligned<int>(ctx->readerPtr - 4);

        // A negative length would sign-extend into an over-read below.
        if (wireCount < 0)
            throw new InvalidOperationException(
                $"Managed fixed-buffer deserialization read a negative length prefix ({wireCount}). The serialized data is corrupted.");

        int copyCount   = wireCount < capacity ? wireCount : capacity;
        int copyBytes   = copyCount * elementSize;

        long wireBytesL = (long)wireCount * (long)elementSize;
        int  alignBytes = (int)((4 - (wireBytesL & 3)) & 3);

        // checked: a corrupt count overflows here instead of skewing the skip.
        int discardBytes = checked((int)(wireBytesL - copyBytes + alignBytes));
        if (copyBytes > 0 || discardBytes > 0)
        {
            if (ctx->readerEnd - ctx->readerPtr >= (long)copyBytes + discardBytes)
            {
                fixed (byte* dstPtr = &field)
                    Unsafe.CopyBlockUnaligned(dstPtr, ctx->readerPtr, (uint)copyBytes);
                ctx->readerPtr += copyBytes + discardBytes;
                return;
            }

            fixed (byte* dstPtr = &field)
                InvokeReadBytesDirect(ctx, dstPtr, copyBytes, discardBytes, 0);
        }
    }
}
