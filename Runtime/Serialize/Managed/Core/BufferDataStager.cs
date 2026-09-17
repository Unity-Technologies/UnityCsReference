// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEngine.Serialization;

internal static unsafe partial class SerializationBackendManagedCommands
{
    // Deferred-write cursor over the writer's current block: stages bytes at
    // writerPtr + m_Staged and commits them with a single flushBuffer when the next
    // write won't fit. Threaded by ref through ExecuteWriteCommands and its recursion
    // (VRT bodies, per-element / per-entry loops) so consecutive writes coalesce into
    // one flush regardless of nesting depth. Holds the call's NativeBufferContext,
    // which is invariant for the stager's lifetime (set once at construction).
    internal unsafe struct BufferDataStager
    {
        // The call's buffer context (writerPtr / writerEnd / flushBuffer).
        // Invariant for the stager's lifetime; readonly so the JIT can treat it as
        // loop-invariant across the executor's per-field walk.
        private readonly NativeBufferContext* m_Ctx;

        // Bytes written at m_Ctx->writerPtr that have not yet been flushed.
        private int m_Staged;

        public BufferDataStager(NativeBufferContext* ctx)
        {
            m_Ctx    = ctx;
            m_Staged = 0;
        }

        // Bump-allocate n bytes at the staging tail, flushing staged bytes first if they
        // won't fit. The flush passes n as minNextWrite, so the post-flush window holds n
        // (n <= kManagedBlockMaxPayloadSize, satisfied by the writer tail or the spill
        // buffer) without a re-check here; larger payloads use TryReserve.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* Reserve(int n)
        {
            UnityEngine.Assertions.Assert.IsTrue(n <= kManagedBlockMaxPayloadSize,
                "BufferDataStager.Reserve: n exceeds kManagedBlockMaxPayloadSize; use TryReserve for spill-capable payloads");
            if (m_Ctx->writerPtr + m_Staged + n > m_Ctx->writerEnd)
                FlushStaged(n);
            UnityEngine.Assertions.Assert.IsTrue(m_Ctx->writerPtr + m_Staged + n <= m_Ctx->writerEnd,
                "BufferDataStager.Reserve: post-flush window smaller than n (flush minNextWrite not threaded?)");
            byte* dst = m_Ctx->writerPtr + m_Staged;
            m_Staged += n;
            return dst;
        }

        // Like Reserve, but returns null when n won't fit one window even after a flush
        // (the caller then spills via Bulk / the chunked arm). For payloads — strings,
        // arrays, fixed buffers — whose framed size can exceed a window. Passes n as
        // minNextWrite so a short tail still serves it when it fits.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* TryReserve(int n)
        {
            if (m_Ctx->writerPtr + m_Staged + n > m_Ctx->writerEnd)
            {
                FlushStaged(n);
                if (m_Ctx->writerPtr + n > m_Ctx->writerEnd)
                    return null;
            }
            byte* dst = m_Ctx->writerPtr + m_Staged;
            m_Staged += n;
            return dst;
        }

        // Staging tail and remaining window room, for a producer that writes straight into
        // the window and reports its size afterward (the UTF-8 encoder in the chunked-string
        // arm) rather than reserving a known size up front. Pair a write of up to StagingRoom
        // bytes at StagingPtr with a matching Stage(n) call.
        public byte* StagingPtr  => m_Ctx->writerPtr + m_Staged;
        public int   StagingRoom => (int)(m_Ctx->writerEnd - m_Ctx->writerPtr) - m_Staged;

        // Absorb `n` bytes just written at StagingPtr into the staged run so they commit with
        // the surrounding flow's next flush instead of on their own.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stage(int n)
        {
            UnityEngine.Assertions.Assert.IsTrue(n >= 0 && m_Ctx->writerPtr + m_Staged + n <= m_Ctx->writerEnd,
                "BufferDataStager.Stage: n outside the current window (write past StagingRoom?)");
            m_Staged += n;
        }

        // Doc §12 write invariant: bump-allocate n bytes with no room check. Valid only
        // where the invariant guarantees the room: every dynamic write arm re-arms the
        // window to at least kManagedBlockMaxPayloadSize at exit, and a fixed block plus
        // the next count header never exceeds it (block cap 1016 + 4).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* ReserveUnchecked(int n)
        {
            UnityEngine.Assertions.Assert.IsTrue(m_Ctx->writerPtr + m_Staged + n <= m_Ctx->writerEnd,
                "BufferDataStager.ReserveUnchecked: window smaller than n (write invariant broken?)");
            byte* dst = m_Ctx->writerPtr + m_Staged;
            m_Staged += n;
            return dst;
        }

        // Doc §12 write invariant: re-arm the window to hold at least n. Unlike
        // FlushStaged this also refreshes a drained window when nothing is staged
        // (a zero-byte flush is a pure window refresh).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureRoom(int n)
        {
            if (m_Ctx->writerPtr + m_Staged + n <= m_Ctx->writerEnd)
                return;
            m_Ctx->flushBuffer(m_Ctx, m_Ctx->writerPtr, m_Staged, n);
            m_Staged = 0;
        }

        // Commit staged bytes (no-op when none). minNextWrite is the size of the write
        // that follows, so the writer hands back a window sized for it (the tail when it
        // fits, else the spill buffer).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FlushStaged(int minNextWrite)
        {
            if (m_Staged > 0)
            {
                m_Ctx->flushBuffer(m_Ctx, m_Ctx->writerPtr, m_Staged, minNextWrite);
                m_Staged = 0;
            }
        }

        // Stream a pinned source straight through the writer's spill arm, bypassing the
        // staging window. Requests a full window for whatever follows the spilled body.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bulk(byte* src, int n)
        {
            UnityEngine.Assertions.Assert.IsTrue(m_Staged == 0,
                "BufferDataStager.Bulk requires staged == 0 (commit staged bytes before streaming a raw region)");
            m_Ctx->flushBuffer(m_Ctx, src, n, kManagedBlockMaxPayloadSize);
        }

        // Reload writerPtr / writerEnd from the writer after a native dispatcher
        // (NativeValueStruct / SimpleNativeType) advanced it directly, bypassing the stager.
        // No bytes are committed (writtenBytes == 0); the flush runs purely for its
        // window-refresh side effect, sizing the next window for a full segment.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResyncWithNativeBuffer()
        {
            UnityEngine.Assertions.Assert.IsTrue(m_Staged == 0,
                "BufferDataStager.ResyncWithNativeBuffer requires staged == 0 (commit before a native dispatcher writes through writerPtr)");
            m_Ctx->flushBuffer(m_Ctx, m_Ctx->writerPtr, 0, kManagedBlockMaxPayloadSize);
        }
    }
}
