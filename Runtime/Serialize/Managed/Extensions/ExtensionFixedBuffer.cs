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
    private static unsafe void V2WriteFixedBuffer(ref byte field, NativeBufferContext* ctx, ref BufferDataStager stager, ulong elementCount, ulong elementSize)
    {
        int count = (int)elementCount;
        int totalBytes = count * (int)elementSize;
        int padBytes = (4 - (totalBytes & 3)) & 3;
        int record = 4 + totalBytes + padBytes;

        fixed (byte* dataPtr = &field)
        {
            // Stage the framed record when it fits a window, so a run of fixed
            // buffers (and adjacent fixed segments) commits in one flush.
            byte* dst = stager.TryReserve(record);
            if (dst != null)
            {
                Unsafe.WriteUnaligned(dst, count);
                Buffer.MemoryCopy(dataPtr, dst + 4, totalBytes, totalBytes);
                if (padBytes > 0)
                    Unsafe.InitBlockUnaligned(dst + 4 + totalBytes, 0, (uint)padBytes);
                return;
            }

            // Payload exceeds a whole window: TryReserve has committed the
            // staged bytes (staged == 0). Frame the count, hand the inline
            // source to FlushBuffer's spill arm, then the tail pad.
            Unsafe.WriteUnaligned(stager.Reserve(4), count);
            stager.FlushStaged(kManagedBlockMaxPayloadSize);
            stager.Bulk(dataPtr, totalBytes);
        }
        if (padBytes > 0)
            Unsafe.InitBlockUnaligned(stager.Reserve(padBytes), 0, (uint)padBytes);
    }

    // Read (M7c): count prefix, stream min(wire, capacity) elements into the
    // inline buffer under a scoped pin, discard wire overflow in window-sized
    // chunks (assets serialized when the buffer was larger), skip the pad.
    // userData is the declared element count, userData2 the element size.
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

        if (copyBytes > 0)
        {
            fixed (byte* dstPtr = &field)
                InvokeReadBytesDirect(ctx, dstPtr, copyBytes);
        }

        long discardBytes = wireBytesL - copyBytes;
        while (discardBytes > 0)
        {
            int chunk = discardBytes > ctx->stackBufferSize
                ? ctx->stackBufferSize
                : (int)discardBytes;
            if (ctx->readerEnd - ctx->readerPtr < chunk)
                InvokeEnsureReadable(ctx, chunk);
            ctx->readerPtr += chunk;
            discardBytes -= chunk;
        }

        if (alignBytes > 0)
        {
            if (ctx->readerEnd - ctx->readerPtr < alignBytes)
                InvokeEnsureReadable(ctx, alignBytes);
            ctx->readerPtr += alignBytes;
        }
    }
}
