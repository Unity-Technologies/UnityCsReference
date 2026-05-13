// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Scripting;

namespace UnityEngine.Serialization;

// NOTE: This enum must be kept in sync with RttiDataType in
// Runtime/Mono/SerializationBackend_DirectMemoryAccess/SerializationCommands.h.
// The numeric values drive the accumulator's opcode-selection logic, so the
// native side asserts each variant's value with static_assert; this side is
// covered by a runtime enum-sync test.
//
// Layout invariants:
//   - Executed DirectCopy variants occupy 0..13 contiguously.
//   - Compact (2B per entry) at 0..6; large (_L, 8B per entry) at 7..13.
//   - Within each half: DC1, DC2, DC4, DC8, DC2_Unaligned, DC4_Unaligned, DC8_Unaligned.
//   - DirectCopyBlock (build-time only, never in the execution byte stream) sits at 14,
//     immediately after the executed variants. IsDirectCopy / IsCompactDirectCopy /
//     IsLargeDirectCopy intentionally exclude it since their consumers all run on
//     entries pulled from the byte stream.
//   - Non-DirectCopy opcodes shift up by 7 to 15..24.
//
// Divided-offset encoding: aligned DC2/DC4/DC8 variants store offset / N at flush
// time (N = 2/4/8). The execution-side cases below multiply by N before indexing.
// _Unaligned variants and DC1 store raw offsets.
internal enum RttiDataType : byte
{
    // Compact DirectCopy (entry stream: 2B per entry, DirectCopyCompactEntry).
    DirectCopy1             = 0,
    DirectCopy2             = 1,
    DirectCopy4             = 2,
    DirectCopy8             = 3,
    DirectCopy2_Unaligned   = 4,
    DirectCopy4_Unaligned   = 5,
    DirectCopy8_Unaligned   = 6,

    // Large DirectCopy (entry stream: 8B per entry, DirectCopyLargeEntry).
    DirectCopy1_L           = 7,
    DirectCopy2_L           = 8,
    DirectCopy4_L           = 9,
    DirectCopy8_L           = 10,
    DirectCopy2_L_Unaligned = 11,
    DirectCopy4_L_Unaligned = 12,
    DirectCopy8_L_Unaligned = 13,

    // Build-time-only opcode: AppendToManagedBlock decomposes it into DirectCopy8/4
    // entries and it is never written into the execution byte stream.
    DirectCopyBlock         = 14,

    // Non-DirectCopy opcodes.
    String                  = 15,
    Array                   = 16,
    List                    = 17,
    Reference               = 18,
    UnityObject             = 19,
    EntityId                = 20,
    DynamicBuffer           = 21,
    PropertyNameId          = 22,
    SimpleNativeType        = 23,
    ValueReferenceType      = 24,

    // Write-path metadata header emitted at the start of each fixed segment.
    FixedBlockPrefix        = 25,

    Unknown                 = 0xFF,
}

// Mirrors native structs in SerializationCommands.h. Natural sequential layout matches
// the native side exactly. DirectCopyGroupHeader is 4 bytes wide so that the entry
// array immediately following it is 4-byte aligned (required by DirectCopyLargeEntry's
// uint fields); _pad exists to make up that width.

internal struct DirectCopyGroupHeader // 4 bytes
{
    public RttiDataType opCode;
    public byte count;
    public ushort _pad;
}

internal struct DirectCopyCompactEntry // 2 bytes
{
    public byte fieldOffset;
    public byte destOffset;
}

internal struct DirectCopyLargeEntry // 8 bytes
{
    public uint fieldOffset;
    public uint destOffset;
}

// Mirrors ManagedCommandStringEntry in SerializationCommands.h (8 bytes).
// Natural sequential layout matches the native side exactly: no padding is
// inserted (uint8 + 3xbyte + uint32 fits tightly with no holes).
internal unsafe struct ManagedCommandStringEntry  // 8 bytes
{
    public RttiDataType opCode;
    public fixed byte   reserved[3];
    public uint         fieldOffset;
}

// Mirrors ManagedCommandFixedBlockPrefix in SerializationCommands.h (4 bytes).
// Emitted at the start of every fixed (DirectCopy) segment; carries the segment's
// total payload size so the executor can size its output buffer up front and
// pre-bump bytesInBuffer for the segment. Natural layout matches the native side
// exactly (uint8 + uint8 + uint16 fits tightly with no holes).
internal struct ManagedCommandFixedBlockPrefix  // 4 bytes
{
    public RttiDataType opCode;
    public byte         reserved;
    public ushort       payloadSize;
}

// Mirrors NativeBufferContext in SerializationCommands.h. Used by every
// variable-sized managed-execution command (fixed-size DirectCopy segments,
// strings, and any future variable-size payloads).
//
// Field layout must match the native struct exactly — `writer` is opaque to C#
// (only the leading slot is reserved). `writerPtr` / `writerAvailable` are kept
// in sync by flushBuffer: read them to decide where to write next, then commit
// via flushBuffer (which updates them in place).
//
// flushBuffer is stored as IntPtr rather than a typed function-pointer field.
// UWP / .NET Native reference rewriting and netstandard2.0 player builds cannot
// represent `delegate* unmanaged[Cdecl]` in metadata (neither as a struct field
// nor as a call-site cast — the latter emits a `method System.Void *(...)` type
// reference into TestAssembly.dll that the rewriter rejects). We dispatch
// through an [UnmanagedFunctionPointer] delegate + Marshal.GetDelegateForFunctionPointer
// instead (no IL calli). The lookup is cached per function pointer per thread
// (see s_FlushBufferFnPtr below) because native passes the same static address
// on every call (ExecuteManagedCommands.cpp).
internal unsafe struct NativeBufferContext
{
    public void*  writer;            // native CachedWriter* — opaque to C#
    public byte*  stackBuffer;       // stable for the lifetime of the call
    public byte*  writerPtr;         // current writer write-position; updated by flushBuffer
    public int    writerAvailable;   // bytes remaining in the writer's current block; updated by flushBuffer
    public IntPtr flushBuffer;       // unmanaged[Cdecl] void(NativeBufferContext*, byte*, int)
}

internal static unsafe class SerializationBackendManagedCommands
{
    // Cached UTF-8 encoder used by ConsumeString's chunked-flush path. Allocated
    // lazily per thread on first use and reused across calls via Reset(); this
    // keeps the hot path on managed serialization allocation-free for strings
    // that need more than one buffer flush. Strings that fit the current buffer
    // tail in one shot bypass the encoder entirely (see ConsumeString).
    [ThreadStatic]
    private static Encoder s_Utf8Encoder;

    [ThreadStatic]
    private static IntPtr s_FlushBufferFnPtr;
    [ThreadStatic]
    private static FlushBufferDelegate s_FlushBufferDel;

    // First arg typed `void*` (not `NativeBufferContext*`) to match the same
    // choice upstream made for the original GetBuffer/FlushBuffer delegates.
    // Marshal.GetDelegateForFunctionPointer<T> builds a thunk from the delegate
    // signature at first call; on netstandard2.0 / x86 / x64 player runtimes
    // the validator applies struct-marshalling rules to typed pointers to
    // managed structs and either throws or builds a wrong thunk. `void*` is
    // treated as opaque pass-through and works on every runtime. The bug only
    // shows up at test runtime, not at C# compile time.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void FlushBufferDelegate(void* ctx,
        byte* bufferUsed, int writtenBytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FlushBufferDelegate GetOrCreateFlushBufferDelegate(IntPtr fnPtr)
    {
        if (s_FlushBufferFnPtr != fnPtr || s_FlushBufferDel == null)
        {
            s_FlushBufferFnPtr = fnPtr;
            s_FlushBufferDel = Marshal.GetDelegateForFunctionPointer<FlushBufferDelegate>(fnPtr);
        }
        return s_FlushBufferDel;
    }

    // Dispatches through the cached [UnmanagedFunctionPointer] delegate so the
    // struct itself stays free of `delegate*` metadata the UWP / .NET Native
    // ReferenceRewriter cannot transform — see comment on NativeBufferContext.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void InvokeFlushBuffer(NativeBufferContext* ctx,
        byte* bufferUsed, int writtenBytes)
    {
        // Cast to void* to match the delegate signature; see comment on
        // FlushBufferDelegate for why the delegate uses void* instead of
        // NativeBufferContext*.
        GetOrCreateFlushBufferDelegate(ctx->flushBuffer)((void*)ctx, bufferUsed, writtenBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T* ConsumeDirectCopyGroup<T>(ref byte* pos, out T* end) where T : unmanaged
    {
        int count = ((DirectCopyGroupHeader*)pos)->count;
        T* entry = (T*)(pos + sizeof(DirectCopyGroupHeader));
        pos = (byte*)(entry + count);
        end = entry + count;
        return entry;
    }

    // The native writer (FlushManagedBlockToCommandQueue in ManagedBlockAccumulator.h)
    // keeps every DirectCopyGroupHeader at a 4-byte-aligned offset relative to entriesPtr,
    // so each DirectCopyLargeEntry array has the alignment its uint fields need. The loop
    // below re-aligns pos to a 4-byte boundary after consuming each group to match.
    //
    // Source-side reads (baseAddr + entry.fieldOffset) can still be unaligned since managed
    // class layout is outside this writer's control; they are handled separately.
    [RequiredByNativeCode]
    public static unsafe int ObjectToSerializationBuffer(
        IntPtr pinnedBase,
        IntPtr entriesPtr,
        int    entryBufferSize,
        IntPtr bufferContext)
    {
        // Managed object memory: accessed via ref so the GC can track it (the caller
        // currently pins, but the contract is "managed memory"). Unmanaged buffers
        // (output, command stream) stay as raw byte*.
        ref byte baseAddr = ref Unsafe.AsRef<byte>((void*)pinnedBase);
        byte* pos = (byte*)entriesPtr;
        byte* endPos = pos + entryBufferSize;

        var ctx = (NativeBufferContext*)bufferContext;

        // Per-segment destination chosen at each FixedBlockPrefix: the writer's
        // current write pointer (zero-copy when ctx->writerAvailable >= segmentSize)
        // or the stack buffer fallback. The DC entries that follow write to
        // dst[entry->destOffset]; the pending segment is committed via
        // InvokeFlushBuffer at the next non-DC opcode (String, the next prefix, or
        // end-of-stream). dstSize == 0 means no segment is currently pending.
        byte* output     = null;
        int   dstSize = 0;

        while (pos < endPos)
        {
            var opCode = (RttiDataType)pos[0];

            // Aligned DC2/DC4/DC8 variants store offset / N; the execution side multiplies by N
            // and uses direct typed reads (Unsafe.As<byte, T>(ref ...)) -- single mov on Mono
            // and CoreCLR. _Unaligned variants store the raw offset and use Unsafe.ReadUnaligned /
            // WriteUnaligned because the typed reads would fault on strict-alignment (ARM) targets
            // when the field is not N-aligned (e.g. StructLayout.Pack=1, LayoutKind.Explicit).
            switch (opCode)
            {
                // ---- Compact aligned ----
                case RttiDataType.DirectCopy1:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        // Destination is always 4-byte aligned; widen the 1B source into an int store.
                        *(int*)(output + entry->destOffset) = Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 2;
                        nint destOffset = (nint)entry->destOffset * 2;
                        *(int*)(output + destOffset) = Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 4;
                        nint destOffset = (nint)entry->destOffset * 4;
                        *(int*)(output + destOffset) = Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 8;
                        nint destOffset = (nint)entry->destOffset * 8;
                        *(long*)(output + destOffset) = Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Compact unaligned ----
                case RttiDataType.DirectCopy2_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, (int)Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<long>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large aligned ----
                case RttiDataType.DirectCopy1_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        *(int*)(output + entry->destOffset) = Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 2;
                        uint destOffset = entry->destOffset * 2;
                        *(int*)(output + destOffset) = Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 4;
                        uint destOffset = entry->destOffset * 4;
                        *(int*)(output + destOffset) = Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 8;
                        uint destOffset = entry->destOffset * 8;
                        *(long*)(output + destOffset) = Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large unaligned ----
                case RttiDataType.DirectCopy2_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, (int)Unsafe.ReadUnaligned<ushort>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<int>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(output + entry->destOffset, Unsafe.ReadUnaligned<long>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.FixedBlockPrefix:
                {
                    var prefix = (ManagedCommandFixedBlockPrefix*)pos;
                    int segmentSize = prefix->payloadSize;
                    pos += sizeof(ManagedCommandFixedBlockPrefix);

                    // Commit any pending segment from the previous prefix before we
                    // pick a destination for this one. Defensive — under normal builder
                    // output a String entry would have flushed first, so dstSize is 0.
                    if (dstSize > 0)
                    {
                        InvokeFlushBuffer(ctx, output, dstSize);
                        dstSize = 0;
                    }

                    // Pick the destination once: zero-copy into the writer's tail when
                    // it has room for the entire segment, otherwise the stack buffer
                    // (FlushBuffer will memcpy into the writer when the segment ends).
                    output     = (ctx->writerAvailable >= segmentSize) ? ctx->writerPtr : ctx->stackBuffer;
                    dstSize = segmentSize;
                    break;
                }

                case RttiDataType.DirectCopyBlock:
                    // DirectCopyBlock is a build-time-only opcode: the accumulator
                    // (AppendToManagedBlock in ManagedBlockAccumulator.h) decomposes it
                    // into DirectCopy8 / DirectCopy4 entries before flushing. Reaching it
                    // here means the builder failed to decompose, or a producer is writing
                    // raw DirectCopyBlock entries into the byte stream — both are bugs.
                    throw new InvalidOperationException(
                        "DirectCopyBlock should never appear in the executed byte stream; "
                        + "the accumulator decomposes it into DirectCopy{4,8} entries.");

                case RttiDataType.String:
                    // Commit the pending fixed segment before the string takes over.
                    if (dstSize > 0)
                    {
                        InvokeFlushBuffer(ctx, output, dstSize);
                        dstSize = 0;
                    }
                    ConsumeString(ctx, ref baseAddr, ref pos);
                    break;

                case RttiDataType.Array:
                case RttiDataType.List:
                case RttiDataType.Reference:
                case RttiDataType.UnityObject:
                case RttiDataType.EntityId:
                case RttiDataType.DynamicBuffer:
                case RttiDataType.PropertyNameId:
                case RttiDataType.SimpleNativeType:
                case RttiDataType.ValueReferenceType:
                case RttiDataType.Unknown:
                    throw new NotSupportedException(
                        $"OpCode {(RttiDataType)pos[0]} is not implemented for managed command blocks.");
                default:
                    throw new NotSupportedException($"OpCode {(RttiDataType)pos[0]} not supported");
            }

            // Re-align pos to a 4-byte offset relative to entriesPtr before reading the
            // next header. Only compact groups with an odd entry count actually need this
            // (2 bytes of skip); large groups are already a multiple of 4.
            long entryOffset = pos - (byte*)entriesPtr;
            long aligned = (entryOffset + 3) & ~3L;
            pos = (byte*)entriesPtr + aligned;
        }

        // Final commit of any pending fixed segment. Skip when no DC entries are
        // pending — e.g., the last entry was a String and ConsumeString already
        // issued its own trailing flush — to avoid an unnecessary P/Invoke.
        if (dstSize > 0)
            InvokeFlushBuffer(ctx, output, dstSize);

        return 0;
    }

    // Writes a string (length prefix + UTF-8 body + 4-byte alignment pad) into the
    // writer in one of two ways:
    //   - Fast path: the entire framed payload fits in the writer's current tail.
    //     One zero-copy direct write, one flush. No Encoder needed.
    //   - Chunked path: stream into the writer's tail in chunks, falling back to the
    //     stack buffer whenever the tail can't hold the next codepoint (worst case
    //     a 4-byte UTF-8 sequence). Each chunk commits via flushBuffer, which either
    //     advances the writer in place or memcpys the stack chunk into the writer
    //     (and may span block boundaries internally) — that memcpy is also how we
    //     implicitly get fresh writer space when the previous flush left
    //     writerAvailable at 0.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ConsumeString(
        NativeBufferContext* ctx, ref byte baseAddr, ref byte* pos)
    {
        var entry = (ManagedCommandStringEntry*)pos;
        pos += sizeof(ManagedCommandStringEntry);
        // Field offset points at a managed string reference inside the pinned object;
        // Unsafe.As<byte, string> reinterprets that ref as a string ref.
        string str = Unsafe.As<byte, string>(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset)) ?? string.Empty;

        // Computed up front because it goes into the 4-byte SInt32 length prefix and
        // also drives the fast-path / chunked-path decision.
        int totalByteCount = Encoding.UTF8.GetByteCount(str);
        int padBytes = (4 - (totalByteCount & 3)) & 3;
        int totalFramedSize = 4 + totalByteCount + padBytes;

        // Fast path: whole framed payload fits in the writer's tail. One direct write,
        // one flush, no Encoder state machine.
        if (ctx->writerAvailable >= totalFramedSize)
        {
            byte* dst = ctx->writerPtr;
            Unsafe.WriteUnaligned(dst, totalByteCount);
            if (totalByteCount > 0)
                Encoding.UTF8.GetBytes(str.AsSpan(), new Span<byte>(dst + 4, totalByteCount));
            if (padBytes > 0)
                Unsafe.InitBlockUnaligned(dst + 4 + totalByteCount, 0, (uint)padBytes);
            InvokeFlushBuffer(ctx, dst, totalFramedSize);
            return;
        }

        // Chunked path. Length header first — uses the writer's tail when ≥ 4 bytes
        // are available, otherwise spills 4 bytes through the stack buffer.
        WriteFramedInt32(ctx, totalByteCount);

        // Body: stream chunk by chunk. At each step pick writer (zero-copy) when the
        // tail can hold a worst-case 4-byte codepoint, else stack. Encoder is reused
        // from the [ThreadStatic] cache.
        if (totalByteCount > 0)
        {
            // Pass flush: false while input remains so the encoder can hold a
            // high surrogate across chunks until the matching low surrogate
            // arrives in the next call. Setting flush: true mid-stream would
            // emit a U+FFFD replacement for the held high surrogate and then
            // another U+FFFD for the orphan low surrogate in the next chunk —
            // corrupting any surrogate pair that happens to straddle a chunk.
            Encoder encoder = s_Utf8Encoder ??= Encoding.UTF8.GetEncoder();
            encoder.Reset();
            ReadOnlySpan<char> remaining = str.AsSpan();

            while (!remaining.IsEmpty)
            {
                bool useWriter = ctx->writerAvailable >= 4;
                byte* dst = useWriter ? ctx->writerPtr : ctx->stackBuffer;
                int   cap = useWriter ? ctx->writerAvailable : kManagedBlockMaxPayloadSize;

                encoder.Convert(remaining, new Span<byte>(dst, cap),
                                flush: false,
                                out int charsUsed, out int bytesUsed, out _);
                if (bytesUsed > 0)
                    InvokeFlushBuffer(ctx, dst, bytesUsed);
                remaining = remaining.Slice(charsUsed);
            }

            // Drain encoder state with a final flush:true call. For valid input this
            // is a no-op; for an unpaired high surrogate it emits a 3-byte
            // replacement character (U+FFFD).
            {
                bool useWriter = ctx->writerAvailable >= 4;
                byte* dst = useWriter ? ctx->writerPtr : ctx->stackBuffer;
                int   cap = useWriter ? ctx->writerAvailable : kManagedBlockMaxPayloadSize;
                encoder.Convert(ReadOnlySpan<char>.Empty, new Span<byte>(dst, cap),
                                flush: true,
                                out _, out int bytesUsed, out _);
                if (bytesUsed > 0)
                    InvokeFlushBuffer(ctx, dst, bytesUsed);
            }
        }

        // Alignment padding (0–3 zero bytes). When the writer's tail has room we
        // zero those bytes directly into the writer; otherwise we pass a stack-local
        // (already zero-initialized) to flushBuffer so it can memcpy them straight
        // out — saves a write into the stack buffer in the rare spill case.
        if (padBytes > 0)
        {
            if (ctx->writerAvailable >= padBytes)
            {
                Unsafe.InitBlockUnaligned(ctx->writerPtr, 0, (uint)padBytes);
                InvokeFlushBuffer(ctx, ctx->writerPtr, padBytes);
            }
            else
            {
                int zeroes = 0;  // up to 3 bytes of guaranteed-zero source
                InvokeFlushBuffer(ctx, (byte*)&zeroes, padBytes);
            }
        }
    }

    // Writes a 4-byte int32 into the writer's tail when it fits, otherwise spills
    // by passing the address of the local parameter directly to flushBuffer.
    // Native FlushBuffer always memcpys from non-writer-pointer buffers via
    // writer.Write(...), so &value works as a transient source and we avoid the
    // extra write into the stack buffer.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteFramedInt32(NativeBufferContext* ctx, int value)
    {
        if (ctx->writerAvailable >= 4)
        {
            Unsafe.WriteUnaligned(ctx->writerPtr, value);
            InvokeFlushBuffer(ctx, ctx->writerPtr, 4);
        }
        else
        {
            InvokeFlushBuffer(ctx, (byte*)&value, 4);
        }
    }

    // kManagedBlockMaxPayloadSize must match the C++ constant in SerializationCommands.h.
    private const int kManagedBlockMaxPayloadSize = 256;

    [RequiredByNativeCode]
    public static unsafe int SerializationBufferToObject(
        IntPtr pinnedBase,
        IntPtr entriesPtr,
        int entryBufferSize,
        int inputBufferSize,
        IntPtr inputBuffer)
    {
        // Managed object memory: accessed via ref so the GC can track it (the caller
        // currently pins, but the contract is "managed memory"). Unmanaged buffers
        // (input, command stream) stay as raw byte*.
        ref byte baseAddr = ref Unsafe.AsRef<byte>((void*)pinnedBase);
        byte* input = (byte*)inputBuffer;
        byte* pos = (byte*)entriesPtr;
        byte* endPos = pos + entryBufferSize;
        _ = inputBufferSize;

        while (pos < endPos)
        {
            var opCode = (RttiDataType)pos[0];

            switch (opCode)
            {
                // ---- Compact aligned ----
                case RttiDataType.DirectCopy1:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset) = *(input + entry->destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 2;
                        nint destOffset = (nint)entry->destOffset * 2;
                        Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset)) = *(ushort*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 4;
                        nint destOffset = (nint)entry->destOffset * 4;
                        Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset)) = *(int*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        nint fieldOffset = (nint)entry->fieldOffset * 8;
                        nint destOffset = (nint)entry->destOffset * 8;
                        Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, fieldOffset)) = *(long*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Compact unaligned ----
                case RttiDataType.DirectCopy2_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<ushort>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<int>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyCompactEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<long>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large aligned ----
                case RttiDataType.DirectCopy1_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset) = *(input + entry->destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy2_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 2;
                        uint destOffset = entry->destOffset * 2;
                        Unsafe.As<byte, ushort>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)) = *(ushort*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 4;
                        uint destOffset = entry->destOffset * 4;
                        Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)) = *(int*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        uint fieldOffset = entry->fieldOffset * 8;
                        uint destOffset = entry->destOffset * 8;
                        Unsafe.As<byte, long>(ref Unsafe.AddByteOffset(ref baseAddr, (nint)fieldOffset)) = *(long*)(input + destOffset);
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                // ---- Large unaligned ----
                case RttiDataType.DirectCopy2_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<ushort>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy4_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<int>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }
                case RttiDataType.DirectCopy8_L_Unaligned:
                {
                    var entry = ConsumeDirectCopyGroup<DirectCopyLargeEntry>(ref pos, out var end);
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref baseAddr, entry->fieldOffset), Unsafe.ReadUnaligned<long>(input + entry->destOffset));
                        entry++;
                    }
                    while (entry < end);
                    break;
                }

                case RttiDataType.FixedBlockPrefix:
                    // Write-path metadata only; nothing to do on read. Just skip the prefix.
                    pos += sizeof(ManagedCommandFixedBlockPrefix);
                    break;

                case RttiDataType.DirectCopyBlock:
                case RttiDataType.String:
                case RttiDataType.Array:
                case RttiDataType.List:
                case RttiDataType.Reference:
                case RttiDataType.UnityObject:
                case RttiDataType.EntityId:
                case RttiDataType.DynamicBuffer:
                case RttiDataType.PropertyNameId:
                case RttiDataType.SimpleNativeType:
                case RttiDataType.ValueReferenceType:
                case RttiDataType.Unknown:
                    throw new NotSupportedException(
                        $"OpCode {opCode} is not implemented for managed command blocks.");
                default:
                    throw new NotSupportedException($"OpCode {opCode} not supported");
            }

            // Match the writer's 4-byte header alignment (see ObjectToSerializationBuffer
            // for details). Compact groups with an odd entry count leave pos 2 bytes short.
            long entryOffset = pos - (byte*)entriesPtr;
            long aligned = (entryOffset + 3) & ~3L;
            pos = (byte*)entriesPtr + aligned;
        }

        return 0;
    }
}
