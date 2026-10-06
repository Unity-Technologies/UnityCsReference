// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 fixed-buffer dynamic handler (doc §2.4) for C#
// unsafe fixed T buf[N]. The wire format is a 4-byte declared element count,
// N*elementSize inline bytes, and 0..3 zero pad. The composer
// (TypeFixedBuffer.cpp) stamps the per-field (count, elementSize) pair into
// the command's userDatas. The inline source gets a scoped pin around the
// copy.
internal static unsafe partial class SerializationBackendManagedCommands
{
    private static unsafe void V2WriteFixedBuffer(ref byte field, NativeBufferContext* ctx, ulong elementCount, ulong elementSize)
    {
        int count = (int)elementCount;
        int totalBytes = count * (int)elementSize;
        int padBytes = (4 - (totalBytes & 3)) & 3;

        // A dynamic command has no guaranteed room ahead of it.
        if (ctx->writerEnd - ctx->writerPtr < 4)
            ctx->ensureWritable(ctx, 4);
        Unsafe.WriteUnaligned(ctx->writerPtr, count);
        ctx->writerPtr += 4;

        int room = (int)(ctx->writerEnd - ctx->writerPtr);
        fixed (byte* dataPtr = &field)
        {
            if (room >= totalBytes + padBytes)
            {
                Buffer.MemoryCopy(dataPtr, ctx->writerPtr, totalBytes, totalBytes);
                if (padBytes > 0)
                    Unsafe.InitBlockUnaligned(ctx->writerPtr + totalBytes, 0, (uint)padBytes);
                ctx->writerPtr += totalBytes + padBytes;
                return;
            }

            // The head fills the window; one crossing commits it and streams
            // the rest and the pad. The executor's arm re-arms for its
            // trailing sum.
            int head = Math.Min(room, totalBytes);
            Buffer.MemoryCopy(dataPtr, ctx->writerPtr, head, head);
            ctx->writerPtr += head;
            ctx->writeBytesDirect(ctx, dataPtr + head, totalBytes - head, padBytes, 0);
        }
    }

    // Read (M7c): count prefix, then min(wire, capacity) elements into the
    // inline buffer under a scoped pin, discarding the wire overflow (assets
    // serialized when the buffer was larger) plus the pad. A record inside
    // the window is copied in managed; otherwise one crossing streams it and
    // skips the discard by a reader position jump, never fetching it. userData is the
    // declared element count, userData2 the element size.
    private static unsafe void V2ReadFixedBuffer(ref byte field, byte* cmdRaw, NativeReadBufferContext* ctx)
    {
        var cmd = (V2CmdExternalDynamic*)cmdRaw;

        if (ctx->readerEnd - ctx->readerPtr < 4)
            InvokeEnsureReadable(ctx, 4);
        int wireCount = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;

        // A negative length would sign-extend into an over-read below.
        if (wireCount < 0)
            throw new InvalidOperationException(
                $"Managed fixed-buffer deserialization read a negative length prefix ({wireCount}). The serialized data is corrupted.");

        int elementSize = (int)cmd->userData2;
        int capacity    = (int)cmd->userData;
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
                    Buffer.MemoryCopy(ctx->readerPtr, dstPtr, copyBytes, copyBytes);
                ctx->readerPtr += copyBytes + discardBytes;
                return;
            }

            fixed (byte* dstPtr = &field)
                InvokeReadBytesDirect(ctx, dstPtr, copyBytes, discardBytes, 0);
        }
    }
}
