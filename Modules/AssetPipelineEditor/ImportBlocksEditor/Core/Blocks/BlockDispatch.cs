// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// The prepared set of blocks to run for a single import. Create one at the start of the import with
    /// <see cref="BlockCollection{T}.PrepareForDispatch"/>, then run each hook through
    /// <see cref="Implementing{TResult}"/>.
    ///
    /// <para>
    /// Building it walks the collection depth-first, skips disabled blocks (and their subtrees), validates
    /// that there are no cycles, and flattens every enabled block — including those reached through composite
    /// children — into one ordered list. The list is not filtered to any hook type; callers select what they
    /// need per hook.
    /// </para>
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    public sealed class BlockDispatch
    {
        // Concrete List<IBlock> (not IReadOnlyList<IBlock>) so the enumerator's indexer/Count stay
        // non-virtual on the hot iteration path.
        readonly List<IBlock> m_Blocks;

        /// <param name="blocks">
        ///   The full enabled, cycle-validated, depth-first block list. The token takes ownership of
        ///   this list — the caller must not mutate it after construction.
        /// </param>
        internal BlockDispatch(List<IBlock> blocks)
        {
            m_Blocks = blocks;
        }

        /// <summary>
        /// The full block list this token wraps, in depth-first order. Exposed for the registration
        /// loop in <see cref="BlockCollection{T}.PrepareForDispatch"/> and for tests. Treat as read-only.
        /// </summary>
        internal IReadOnlyList<IBlock> Blocks => m_Blocks;

        /// <summary>
        /// Returns a <c>foreach</c>-able struct enumerable that yields every block in the token
        /// implementing <typeparamref name="TResult"/>. Allocation-free; the per-block <c>is TResult</c>
        /// narrowing lives in the enumerator's scan. When <typeparamref name="TResult"/> is a hook
        /// interface, each iteration body is wrapped in a per-block
        /// <see cref="Unity.Profiling.ProfilerMarker"/>; for a family/marker type
        /// (e.g. <see cref="IBlock"/>) the blocks are yielded without markers.
        /// </summary>
        /// <typeparam name="TResult">The hook interface or block family type to dispatch.</typeparam>
        public BlockDispatchEnumerable<TResult> Implementing<TResult>() where TResult : IBlock
        {
            return new BlockDispatchEnumerable<TResult>(m_Blocks);
        }

        /// <summary>
        /// Returns <c>true</c> if any block in the token implements <typeparamref name="TResult"/>.
        /// A single allocation-free scan, correct for both method-bearing hook interfaces and
        /// zero-method family/marker types (e.g. <see cref="IBlock"/>).
        /// Used by native has-gates; scripted importers use <see cref="Implementing{TResult}"/>.
        /// </summary>
        /// <typeparam name="TResult">The hook interface or block family type to test for.</typeparam>
        internal bool Contains<TResult>() where TResult : IBlock
        {
            foreach (var block in m_Blocks)
            {
                if (block is TResult)
                    return true;
            }

            return false;
        }
    }
}
