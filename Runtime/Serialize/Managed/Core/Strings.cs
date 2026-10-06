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
// the write window, the body is encoded directly at writerPtr + 4 and the
// length is backpatched. Larger strings take the exact-size or split arms.
internal static unsafe partial class SerializationBackendManagedCommands
{
    // Chunked-arm scratch encoder; stateless between calls, holds no user
    // references, safe to persist.
    [NoAutoStaticsCleanup]
    private static Encoder s_V2Utf8Encoder;


    private static void V2WriteFramedString(NativeBufferContext* ctx, ReadOnlySpan<char> chars)
    {
        // Single-pass arm: the worst case (3 bytes per UTF-16 unit, computed
        // over the untruncated length) fits the window, so encode once with
        // the fused encoder and backpatch the length.
        long worstCase = 4 + 3L * chars.Length + 3;
        long room = ctx->writerEnd - ctx->writerPtr;
        if (worstCase <= room)
        {
            byte* dst = ctx->writerPtr;
            int byteCount = chars.IsEmpty
                ? 0
                : V2EncodeUtf8TruncateAtNul(chars, dst + 4, (int)room - 4);
            Unsafe.WriteUnaligned(dst, byteCount);
            int padBytes = (4 - (byteCount & 3)) & 3;
            for (int i = 0; i < padBytes; ++i)
                dst[4 + byteCount + i] = 0;
            ctx->writerPtr = dst + 4 + byteCount + padBytes;
            return;
        }
        V2WriteFramedStringSplit(ctx, chars);
    }

    // The worst case does not fit the window. Count the exact encoding, write
    // it in place when that fits, and otherwise fill the window and continue
    // in the next one. Not inlined into the hot arm.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void V2WriteFramedStringSplit(NativeBufferContext* ctx, ReadOnlySpan<char> chars)
    {
        int nullIdx = chars.IndexOf('\0');
        if (nullIdx >= 0)
            chars = chars.Slice(0, nullIdx);

        int byteCount = Encoding.UTF8.GetByteCount(chars);
        int padBytes = (4 - (byteCount & 3)) & 3;
        if (ctx->writerEnd - ctx->writerPtr >= 4 + byteCount + padBytes)
        {
            byte* dst = ctx->writerPtr;
            Unsafe.WriteUnaligned(dst, byteCount);
            if (byteCount > 0)
                Encoding.UTF8.GetBytes(chars, new Span<byte>(dst + 4, byteCount));
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(dst + 4 + byteCount, 0, (uint)padBytes);
            ctx->writerPtr = dst + 4 + byteCount + padBytes;
            return;
        }

        if (ctx->writerEnd - ctx->writerPtr < 4)
            ctx->ensureWritable(ctx, 4);
        Unsafe.WriteUnaligned(ctx->writerPtr, byteCount);
        ctx->writerPtr += 4;
        if (byteCount > 0)
        {
            // A window of 4 bytes holds any one scalar, so every Convert makes
            // progress. flush: false while input remains lets the encoder hold
            // a high surrogate across windows.
            Encoder encoder = s_V2Utf8Encoder ??= Encoding.UTF8.GetEncoder();
            encoder.Reset();
            ReadOnlySpan<char> remaining = chars;
            while (!remaining.IsEmpty)
            {
                if (ctx->writerEnd - ctx->writerPtr < 4)
                    ctx->ensureWritable(ctx, 4);
                encoder.Convert(remaining, new Span<byte>(ctx->writerPtr, (int)(ctx->writerEnd - ctx->writerPtr)),
                                flush: false, out int charsUsed, out int bytesUsed, out _);
                ctx->writerPtr += bytesUsed;
                remaining = remaining.Slice(charsUsed);
            }
            // End-of-stream drain: the encoder may hold one high surrogate,
            // emitted now as a replacement of at most 3 bytes.
            bool completed;
            do
            {
                if (ctx->writerEnd - ctx->writerPtr < 4)
                    ctx->ensureWritable(ctx, 4);
                encoder.Convert(ReadOnlySpan<char>.Empty, new Span<byte>(ctx->writerPtr, (int)(ctx->writerEnd - ctx->writerPtr)),
                                flush: true, out _, out int tailBytes, out completed);
                ctx->writerPtr += tailBytes;
            } while (!completed);
        }
        if (padBytes > 0)
        {
            if (ctx->writerEnd - ctx->writerPtr < padBytes)
                ctx->ensureWritable(ctx, padBytes);
            Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
            ctx->writerPtr += padBytes;
        }
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
    // the length reads unchecked against the doc §12 window guarantee. A string
    // already in the window together with trailingEnsure (the command's stamped
    // sum) costs one branch and no crossing, whatever its size; the refill paths
    // are out of line.
    private static string V2ReadFramedString(NativeReadBufferContext* ctx, int trailingEnsure)
    {
        int length = Unsafe.ReadUnaligned<int>(ctx->readerPtr);
        ctx->readerPtr += 4;

        if (length < 0)
            throw new InvalidOperationException(
                $"Managed string deserialization read a negative length prefix ({length}). The serialized data is corrupted.");

        // Reject a prefix the object cannot supply before it sizes an arm or an allocation.
        // The callback reports the overrun and throws.
        if (length > ctx->readableBytes)
            InvokeEnsureReadable(ctx, length);

        int padBytes = (4 - (length & 3)) & 3;
        long body = (long)length + padBytes;

        if (length == 0)
        {
            if (ctx->readerEnd - ctx->readerPtr < trailingEnsure)
                InvokeEnsureReadable(ctx, trailingEnsure);
            return string.Empty;
        }

        if (ctx->readerEnd - ctx->readerPtr >= body + trailingEnsure)
        {
            string result = V2DecodeStringBody(ctx->readerPtr, length);
            ctx->readerPtr += body;
            return result;
        }

        return V2ReadFramedStringRefill(ctx, length, padBytes, trailingEnsure);
    }

    // The window doesn't hold the string and its trailing guarantee. The spill
    // buffer size only matters here, where a refill has to pick between an ensure
    // (capped by the spill buffer) and a direct read. Not inlined into the hot arm.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string V2ReadFramedStringRefill(NativeReadBufferContext* ctx, int length, int padBytes, int trailingEnsure)
    {
        long body = (long)length + padBytes;
        string result;
        if (ctx->readerEnd - ctx->readerPtr >= body)
        {
            // Only the trailing guarantee is missing.
            result = V2DecodeStringBody(ctx->readerPtr, length);
            ctx->readerPtr += body;
        }
        else if (body + trailingEnsure <= kManagedBlockSpillBufferSize)
        {
            // One ensure covers the body, its pad and the trailing guarantee.
            InvokeEnsureReadable(ctx, (int)(body + trailingEnsure));
            result = V2DecodeStringBody(ctx->readerPtr, length);
            ctx->readerPtr += body;
            return result;
        }
        else if (body <= kManagedBlockSpillBufferSize)
        {
            InvokeEnsureReadable(ctx, (int)body);
            result = V2DecodeStringBody(ctx->readerPtr, length);
            ctx->readerPtr += body;
        }
        else
        {
            byte[] buf = new byte[length];
            fixed (byte* bufPtr = buf)
            {
                // The crossing owns the pad and the trailing guarantee,
                // taken at the 4-aligned post-pad position.
                InvokeReadBytesDirect(ctx, bufPtr, length, padBytes, trailingEnsure);
                result = V2DecodeStringBody(bufPtr, length);
            }
            return result;
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
    // most 16 bytes and always fits one ensured window. Not inlined: IL2CPP would
    // accumulate the stackalloc into the caller's frame.
    private static void V2WriteFramedDecimalInt32(NativeBufferContext* ctx, int value)
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
        int framed = 4 + length + padBytes;
        if (ctx->writerEnd - ctx->writerPtr < framed)
            ctx->ensureWritable(ctx, framed);
        byte* dst = ctx->writerPtr;
        ctx->writerPtr = dst + framed;
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
