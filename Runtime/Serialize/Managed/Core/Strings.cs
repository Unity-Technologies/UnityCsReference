// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Serialization;

// Managed serialization V2: string framing.
//
// Wire shape (identical to v1's): a 4-byte SInt32 length, the UTF-8 body
// truncated at the first '\0', and 0..3 zero bytes of padding to 4-byte
// alignment.
//
// The write is single-pass. The worst-case encoding is 3 bytes per UTF-16
// unit (a surrogate pair encodes 4 bytes from 2 units). When that bound fits
// the staging window, the body is encoded directly at StagingPtr + 4 and the
// length is backpatched. Larger strings take the exact-size or chunked arms.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Chunked-arm scratch encoder; stateless between calls, holds no user
    // references, safe to persist.
    [NoAutoStaticsCleanup]
    private static Encoder s_V2Utf8Encoder;


    private static void V2WriteFramedString(NativeBufferContext* ctx, ReadOnlySpan<char> chars,
        ref BufferDataStager stager)
    {
        // Single-pass arm: reserve worst-case room (3 bytes per UTF-16 unit,
        // computed over the untruncated length), encode once with the fused
        // encoder, then backpatch the length. A short window flushes first;
        // strings whose worst case still exceeds the window fall through to
        // the arms below.
        long worstCase = 4 + 3L * chars.Length + 3;
        if (worstCase > stager.StagingRoom)
            stager.FlushStaged((int)Math.Min(worstCase, kManagedBlockMaxPayloadSize));
        if (worstCase <= stager.StagingRoom)
        {
            byte* dst = stager.StagingPtr;
            int byteCount = chars.IsEmpty
                ? 0
                : V2EncodeUtf8TruncateAtNul(chars, dst + 4, stager.StagingRoom - 4);
            Unsafe.WriteUnaligned(dst, byteCount);
            int padBytes = (4 - (byteCount & 3)) & 3;
            for (int i = 0; i < padBytes; ++i)
                dst[4 + byteCount + i] = 0;
            stager.Stage(4 + byteCount + padBytes);
            return;
        }

        // Oversized strings only from here. Truncate at the first '\0' up front.
        int nullIdx = chars.IndexOf('\0');
        if (nullIdx >= 0)
            chars = chars.Slice(0, nullIdx);

        // Exact-size arm: the worst case exceeded the window, but the real
        // encoding may still fit one region.
        int totalByteCount = Encoding.UTF8.GetByteCount(chars);
        int totalPad = (4 - (totalByteCount & 3)) & 3;
        byte* exact = stager.TryReserve(4 + totalByteCount + totalPad);
        if (exact != null)
        {
            Unsafe.WriteUnaligned(exact, totalByteCount);
            if (totalByteCount > 0)
                Encoding.UTF8.GetBytes(chars, new Span<byte>(exact + 4, totalByteCount));
            if (totalPad > 0)
                Unsafe.InitBlockUnaligned(exact + 4 + totalByteCount, 0, (uint)totalPad);
            return;
        }

        // Chunked arm: stage the length header, then stream the body region
        // by region. flush: false while input remains lets the encoder hold a
        // high surrogate across chunks.
        Unsafe.WriteUnaligned(stager.Reserve(4), totalByteCount);
        if (totalByteCount > 0)
        {
            Encoder encoder = s_V2Utf8Encoder ??= Encoding.UTF8.GetEncoder();
            encoder.Reset();
            ReadOnlySpan<char> remaining = chars;
            while (!remaining.IsEmpty)
            {
                encoder.Convert(remaining, new Span<byte>(stager.StagingPtr, stager.StagingRoom),
                                flush: false, out int charsUsed, out int bytesUsed, out _);
                stager.Stage(bytesUsed);
                remaining = remaining.Slice(charsUsed);
                if (!remaining.IsEmpty)
                    stager.FlushStaged(kManagedBlockMaxPayloadSize);
            }
            // End-of-stream drain: the encoder may hold one high surrogate,
            // emitted now as a replacement of at most 3 bytes.
            bool completed;
            do
            {
                encoder.Convert(ReadOnlySpan<char>.Empty, new Span<byte>(stager.StagingPtr, stager.StagingRoom),
                                flush: true, out _, out int tailBytes, out completed);
                stager.Stage(tailBytes);
                if (!completed)
                    stager.FlushStaged(kManagedBlockMaxPayloadSize);
            } while (!completed);
        }
        if (totalPad > 0)
            Unsafe.InitBlockUnaligned(stager.Reserve(totalPad), 0, (uint)totalPad);
    }

    // Fused UTF-16 to UTF-8 transcode: ASCII narrowing, the non-ASCII test,
    // and '\0' truncation share one pass over the input. SWAR blocks of 4
    // chars use portable ulong ops with little-endian lane packing. The first
    // non-ASCII unit hands the entire remainder to Encoding.UTF8 in one call,
    // so surrogate pairs are never split. The caller guarantees dstCapacity
    // >= 3 * chars.Length.
    private static int V2EncodeUtf8TruncateAtNul(ReadOnlySpan<char> chars, byte* dst, int dstCapacity)
    {
        ref char src = ref MemoryMarshal.GetReference(chars);
        int length = chars.Length;
        int i = 0;
        int written = 0;

        // Bail on any lane >= 0x80 or any zero lane (the 16-bit HASZERO test).
        while (i + 4 <= length)
        {
            ulong quad = Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref src, i)));
            if ((quad & 0xFF80FF80FF80FF80UL) != 0
                || (((quad - 0x0001000100010001UL) & ~quad) & 0x8000800080008000UL) != 0)
                break;
            uint lo = (uint)quad;
            uint hi = (uint)(quad >> 32);
            dst[written] = (byte)lo;
            dst[written + 1] = (byte)(lo >> 16);
            dst[written + 2] = (byte)hi;
            dst[written + 3] = (byte)(hi >> 16);
            i += 4;
            written += 4;
        }

        // Scalar loop: the tail, and resolution of a SWAR bail.
        for (; i < length; ++i)
        {
            char c = Unsafe.Add(ref src, i);
            if (c < 0x80)
            {
                if (c == '\0')
                    return written;  // the wire truncates at the first '\0'
                dst[written++] = (byte)c;
                continue;
            }

            // Non-ASCII from here. Truncate the remainder at its first '\0',
            // then one Encoding call finishes the string.
            ReadOnlySpan<char> rest = chars.Slice(i);
            int nul = rest.IndexOf('\0');
            if (nul >= 0)
                rest = rest.Slice(0, nul);
            return written + Encoding.UTF8.GetBytes(rest, new Span<byte>(dst + written, dstCapacity - written));
        }
        return written;
    }

    // Framed-string read for invariant-covered call sites (the kString arm):
    // the length reads unchecked against the doc §12 window guarantee, and
    // trailingEnsure (the command's stamped sum) folds into the body ensure,
    // so the common in-window string costs ONE ensure branch total and the
    // window guarantee holds on return.
    private static string V2ReadFramedString(NativeReadBufferContext* ctx, int trailingEnsure)
    {
        int length = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;

        if (length < 0)
            throw new InvalidOperationException(
                $"Managed string deserialization read a negative length prefix ({length}). The serialized data is corrupted.");

        int padBytes = (4 - (length & 3)) & 3;
        int body = length + padBytes;

        if (length == 0)
        {
            if (ctx->readerEnd - ctx->readerPtr < trailingEnsure)
                InvokeEnsureReadable(ctx, trailingEnsure);
            return string.Empty;
        }

        if (body <= ctx->stackBufferSize - trailingEnsure)
        {
            // One ensure covers the body, its pad, and the trailing guarantee.
            if (ctx->readerEnd - ctx->readerPtr < body + trailingEnsure)
                InvokeEnsureReadable(ctx, body + trailingEnsure);
            string result = V2DecodeStringBody(ctx->readerPtr, length);
            ctx->readerPtr += body;
            return result;
        }

        return V2ReadFramedStringLarge(ctx, length, padBytes, trailingEnsure);
    }

    // The rare tail of V2ReadFramedString: bodies too large to fold with the
    // trailing guarantee. Not inlined into the hot arm.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string V2ReadFramedStringLarge(NativeReadBufferContext* ctx, int length, int padBytes, int trailingEnsure)
    {
        string result;
        if (length + padBytes <= ctx->stackBufferSize)
        {
            if (ctx->readerEnd - ctx->readerPtr < length + padBytes)
                InvokeEnsureReadable(ctx, length + padBytes);
            result = V2DecodeStringBody(ctx->readerPtr, length);
            ctx->readerPtr += length + padBytes;
        }
        else
        {
            byte[] buf = new byte[length];
            fixed (byte* bufPtr = buf)
            {
                InvokeReadBytesDirect(ctx, bufPtr, length);
                result = V2DecodeStringBody(bufPtr, length);
            }
            if (padBytes > 0)
            {
                if (ctx->readerEnd - ctx->readerPtr < padBytes)
                    InvokeEnsureReadable(ctx, padBytes);
                ctx->readerPtr += padBytes;
            }
        }
        if (ctx->readerEnd - ctx->readerPtr < trailingEnsure)
            InvokeEnsureReadable(ctx, trailingEnsure);
        return result;
    }

    // Guarded form for opt-out call sites (PropertyName's dynamic handler,
    // whose commands report a zero minimum): checks its own length header;
    // the trailing guarantee is the caller's arm's responsibility.
    private static string V2ReadFramedStringGuarded(NativeReadBufferContext* ctx)
    {
        if (ctx->readerEnd - ctx->readerPtr < 4)
            InvokeEnsureReadable(ctx, 4);
        return V2ReadFramedString(ctx, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string V2DecodeStringBody(byte* bytes, int length)
    {
        int firstZero = new ReadOnlySpan<byte>(bytes, length).IndexOf((byte)0);
        int effective = firstZero < 0 ? length : firstZero;

        if (effective == 0)
            return string.Empty;

        // Replacement fallback: malformed subsequences become U+FFFD.
        return Encoding.UTF8.GetString(bytes, effective);
    }

    // Writes an Int32 as decimal ASCII in the string wire shape, matching
    // native IntToString (PropertyName's id form). The framed payload is at
    // most 16 bytes and always fits after a flush. Not inlined: IL2CPP would
    // accumulate the stackalloc into the caller's frame.
    private static void V2WriteFramedDecimalInt32(NativeBufferContext* ctx, int value,
        ref BufferDataStager stager)
    {
        bool negative = value < 0;
        long magnitude = negative ? -(long)value : value;
        byte* rev = stackalloc byte[10];
        int digits = 0;
        do
        {
            rev[digits++] = (byte)('0' + (int)(magnitude % 10));
            magnitude /= 10;
        }
        while (magnitude > 0);

        int length = digits + (negative ? 1 : 0);
        int padBytes = (4 - (length & 3)) & 3;
        byte* dst = stager.Reserve(4 + length + padBytes);
        Unsafe.WriteUnaligned(dst, length);
        int cursor = 4;
        if (negative)
            dst[cursor++] = (byte)'-';
        for (int i = digits - 1; i >= 0; i--)
            dst[cursor++] = rev[i];
        if (padBytes > 0)
            Unsafe.InitBlockUnaligned(dst + 4 + length, 0, (uint)padBytes);
    }

    // Parses the decimal directly off the wire. The long accumulator lets
    // Int32.MinValue round-trip.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int V2ReadFramedDecimalInt32(NativeReadBufferContext* ctx)
    {
        if (ctx->readerEnd - ctx->readerPtr < 4)
            InvokeEnsureReadable(ctx, 4);
        int length = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;

        // An Int32 decimal is at most 11 bytes ("-2147483648"). A longer
        // prefix is corrupt, and the bound keeps the digit loop inside the
        // spill buffer.
        if (length < 0 || length > 11)
            throw new InvalidOperationException(
                $"Managed PropertyName deserialization read an invalid decimal length prefix ({length}). The serialized data is corrupted.");

        long magnitude = 0;
        bool negative = false;
        int padBytes = (4 - (length & 3)) & 3;
        if (length > 0)
        {
            // One ensure covers the digits and their pad.
            if (ctx->readerEnd - ctx->readerPtr < length + padBytes)
                InvokeEnsureReadable(ctx, length + padBytes);
            byte* p = ctx->readerPtr;
            int i = 0;
            if (p[0] == (byte)'-')
            {
                negative = true;
                i = 1;
            }
            for (; i < length; i++)
                magnitude = magnitude * 10 + (p[i] - (byte)'0');
            ctx->readerPtr += length + padBytes;
        }
        return negative ? (int)(-magnitude) : (int)magnitude;
    }
}
