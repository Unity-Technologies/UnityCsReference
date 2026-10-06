// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    // Per-closed-TResult cache: profiler markers are only meaningful for a real hook interface — one
    // declaring exactly one method, which yields a clean "ImportBlocks.{Block}.{HookMethod}" name.
    // Family/marker types (e.g. IBlock) resolve to no single method, so matching
    // blocks are iterated without markers rather than emitting a junk, interface-named sample.
    // Evaluated once per TResult per domain.
    static class BlockHookMarker<TResult> where TResult : IBlock
    {
        internal static readonly bool Emit = BlockProfiling.IsHookInterface(typeof(TResult));
    }

    /// <summary>
    /// A <c>foreach</c>-able value that yields all blocks in a block list that implement
    /// <typeparamref name="TResult"/>. When <typeparamref name="TResult"/> is a hook interface it also
    /// drives a per-block <see cref="ProfilerMarker"/> around each iteration body.
    ///
    /// <para>
    /// Obtain instances via <see cref="BlockDispatch.Implementing{TResult}"/>. The default value
    /// (null list) is a valid empty enumerable.
    /// </para>
    ///
    /// <para>
    /// <b>Use <c>foreach</c>.</b> The enumerator opens a profiler marker in <c>MoveNext</c> and closes
    /// it in <c>Dispose</c>; <c>foreach</c> guarantees <c>Dispose</c> via its compiler-generated
    /// <c>finally</c>, balancing markers on normal completion, <c>break</c>, and exceptions. Driving the
    /// enumerator by hand (calling <c>GetEnumerator</c>/<c>MoveNext</c> directly) without disposing it
    /// leaks the last open marker — do not do this.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">
    /// The hook interface or block family type to dispatch. Must derive from <see cref="IBlock"/>.
    /// </typeparam>
    [UnityEngine.Internal.ExcludeFromDocs]
    public readonly struct BlockDispatchEnumerable<TResult> where TResult : IBlock
    {
        readonly List<IBlock> m_Blocks;

        internal BlockDispatchEnumerable(List<IBlock> blocks)
        {
            m_Blocks = blocks;
        }

        /// <summary>
        /// Returns the struct enumerator. Called implicitly by <c>foreach</c>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BlockDispatchEnumerator<TResult> GetEnumerator() =>
            new BlockDispatchEnumerator<TResult>(m_Blocks);
    }

    /// <summary>
    /// Struct enumerator that iterates a block list, yielding only those blocks that implement
    /// <typeparamref name="TResult"/>, and driving a <see cref="ProfilerMarker"/> around each
    /// caller body.
    ///
    /// <para>
    /// Marker lifecycle: <see cref="MoveNext"/> begins the next block's marker <em>after</em>
    /// locating the block; the <em>previous</em> block's marker is ended at the top of the
    /// subsequent <see cref="MoveNext"/> call. The last open marker is closed by
    /// <see cref="Dispose"/>, which <c>foreach</c> calls via its implicit <c>finally</c> — so
    /// markers are balanced on <c>break</c>, <c>return</c>, and exceptions.
    /// </para>
    ///
    /// <para>
    /// Idle: when no block in the list implements <typeparamref name="TResult"/>, the scan finds
    /// nothing and the loop body never runs (O(≤N), zero allocations). A null or empty list yields
    /// zero iterations the same way. Markers are emitted only when <typeparamref name="TResult"/> is a
    /// hook interface; for a family/marker type the matching blocks are yielded without markers.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">
    /// The hook interface or block family type being dispatched.
    /// </typeparam>
    [UnityEngine.Internal.ExcludeFromDocs]
    public struct BlockDispatchEnumerator<TResult> : System.IDisposable where TResult : IBlock
    {
        // Null tolerant: a default(BlockDispatchEnumerable) or an empty list both yield zero
        // iterations. Concrete List<T> (not IReadOnlyList<T>) so the indexer / Count below are non-virtual.
        readonly List<IBlock> m_ScanList;

        // Current position in m_ScanList; starts at -1 (before the first element).
        int m_Index;

        // The block yielded by the last successful MoveNext.
        TResult m_Current;

        // The marker opened by the last successful MoveNext.
        ProfilerMarker m_Marker;

        // True while a marker has been Begin()ed but not yet End()ed.
        bool m_MarkerActive;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal BlockDispatchEnumerator(List<IBlock> blocks)
        {
            m_Index = -1;
            m_Current = default;
            m_Marker = default;
            m_MarkerActive = false;
            m_ScanList = blocks;
        }

        /// <summary>
        /// Gets the block yielded by the last successful <see cref="MoveNext"/> call.
        /// </summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Current;
        }

        /// <summary>
        /// Advances to the next block implementing <typeparamref name="TResult"/>.
        /// Ends the previous block's marker (if any) and begins the next one.
        /// Returns <c>false</c> when no more blocks remain.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            // End the previous block's marker — the caller's body for it has now finished.
            if (m_MarkerActive)
            {
                m_Marker.End();
                m_MarkerActive = false;
            }

            // Null scan list means nothing to iterate.
            if (m_ScanList == null)
                return false;

            // Advance until we find a block implementing TResult.
            while (++m_Index < m_ScanList.Count)
            {
                if (m_ScanList[m_Index] is TResult typed)
                {
                    m_Current = typed;
                    if (BlockHookMarker<TResult>.Emit)
                    {
                        m_Marker = BlockProfiling.GetMarker(typed.GetType(), typeof(TResult));
                        m_Marker.Begin();
                        m_MarkerActive = true;
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Closes the last open marker. Called by <c>foreach</c>'s implicit <c>finally</c> so that
        /// markers are always balanced even on <c>break</c>, <c>return</c>, or an exception in the
        /// loop body. Safe to call multiple times (idempotent).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (m_MarkerActive)
            {
                m_Marker.End();
                m_MarkerActive = false;
            }
        }
    }
}
