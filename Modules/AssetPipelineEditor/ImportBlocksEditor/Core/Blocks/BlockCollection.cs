// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// A collection of blocks that can be executed in sequence.
    /// Supports generic type constraints to restrict which blocks can be added.
    /// </summary>
    /// <typeparam name="T">The IBlock interface type that blocks must implement</typeparam>
    [UnityEngine.Internal.ExcludeFromDocs]
    [Serializable]
    public class BlockCollection<T> : IEnumerable<T>, IBlockCollection where T : IBlock
    {
        [SerializeReference] List<T> m_Blocks = new List<T>();

        public T this[int index]
        {
            get => m_Blocks[index];
            set => m_Blocks[index] = value;
        }

        /// <summary>
        /// Number of blocks in the immediate collection (not traversal descendants).
        /// </summary>
        public int Count => m_Blocks.Count;

        /// <summary>
        /// Adds a block to the collection.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="block"/> is null.</exception>
        public void Add(T block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            m_Blocks.Add(block);
        }

        public void AddRange(BlockCollection<T> blocks)
        {
            foreach (var block in blocks)
            {
                // Tolerate null holes in the source (e.g. a [SerializeReference] slot emptied by a deleted or
                // renamed managed type); Add itself rejects null as invalid caller input.
                if (block != null)
                    m_Blocks.Add(block);
            }
        }

        public void Remove(T block)
        {
            m_Blocks.Remove(block);
        }

        public void Clear()
        {
            m_Blocks.Clear();
        }

        /// <summary>
        /// Iterates the immediate blocks in this collection. Does not traverse child blocks.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return m_Blocks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Traverse the block tree depth-first, skipping disabled blocks and their subtrees.
        /// Each block is returned followed by its children (from GetChildBlocks()) recursively.
        /// Detects circular references within the block tree and throws InvalidOperationException if found.
        /// Note: Does not detect circular asset dependencies (e.g. model referencing its own variant).
        /// </summary>
        /// <returns>All blocks in the tree depth-first</returns>
        /// <exception cref="InvalidOperationException">Thrown when a circular reference is detected in the block hierarchy</exception>
        public IEnumerable<T> Traverse() => Traverse<T>();

        /// <summary>
        /// Performs a depth-first walk of the entire block tree, appending every enabled block
        /// (including non-<typeparamref name="T"/> children) to <paramref name="result"/>.
        /// Skips disabled blocks and their entire subtrees. Throws <see cref="InvalidOperationException"/>
        /// if a circular reference is detected — the same exception and message as <see cref="Traverse()"/>.
        /// </summary>
        /// <param name="result">List to append collected blocks to (in depth-first order).</param>
        /// <exception cref="InvalidOperationException">Thrown when a circular reference is detected in the block hierarchy.</exception>
        void CollectEnabledBlocksDepthFirst(List<IBlock> result)
        {
            var currentPath = new HashSet<IBlock>();
            var visited = new HashSet<IBlock>();
            // Cycles through collection references can't be caught by block identity: each expansion
            // returns fresh clones. Track the expanded providers on the current path instead.
            var pathProviders = new BlockCollectionReference.ProviderPathGuard();

            if (m_Blocks != null)
            {
                foreach (var block in m_Blocks)
                {
                    if (block != null)
                    {
                        CollectBlock(block, currentPath);
                    }
                }
            }

            void CollectBlock(IBlock block, HashSet<IBlock> pathBlocks)
            {
                if (block is Block baseBlock && !baseBlock.Enabled)
                    return;

                if (pathBlocks.Contains(block))
                {
                    var blockName = block is Block b ? b.Name : block.GetType().Name;
                    throw new InvalidOperationException(
                        $"Circular reference detected in block hierarchy. Block '{blockName}' appears in its own ancestor path. " +
                        $"Ensure GetChildBlocks() does not return the block itself or any ancestor block.");
                }

                // A block reachable via two distinct parents (a diamond/shared instance) must be emitted and
                // dispatched only once. The path set above still detects true cycles; this set dedups DAGs.
                if (!visited.Add(block))
                    return;

                pathBlocks.Add(block);

                var provider = (block as BlockCollectionReference)?.GetReferencedObject();
                if (provider != null && !pathProviders.TryEnter(provider))
                {
                    throw new InvalidOperationException(
                        $"Circular reference detected in block hierarchy. '{provider.name}' is referenced from " +
                        $"its own expansion. Remove the reference cycle between the referenced block collections.");
                }

                result.Add(block);
                var childBlocks = block.GetChildBlocks();
                if (childBlocks != null)
                {
                    foreach (var child in childBlocks)
                    {
                        // GetChildBlocks() may surface raw storage (e.g. a [SerializeReference]
                        // collection with a null hole from a deleted/renamed managed type).
                        if (child == null)
                            continue;

                        CollectBlock(child, pathBlocks);
                    }
                }

                if (provider != null)
                    pathProviders.Exit(provider);
                pathBlocks.Remove(block);
            }
        }

        /// <summary>
        /// Builds an immutable <see cref="BlockDispatch"/> over the full enabled, cycle-validated,
        /// depth-first block walk — every reachable block, <b>not</b> filtered to <typeparamref name="T"/>.
        /// Registers no dependencies and caches nothing; <see cref="PrepareForDispatch"/> layers those on.
        ///
        /// <para>
        /// The ctx-free build seam. Throws <see cref="InvalidOperationException"/> on a cyclic tree (the
        /// same exception and message as <see cref="Traverse()"/>), via the shared cycle-detecting walk.
        /// </para>
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a circular reference is detected.</exception>
        internal BlockDispatch BuildDispatch()
        {
            var fullWalk = new List<IBlock>();
            CollectEnabledBlocksDepthFirst(fullWalk);
            return new BlockDispatch(fullWalk);
        }

        /// <summary>
        /// Builds the full-walk dispatch token, registers <b>every</b> block's dependencies over that walk
        /// against <paramref name="ctx"/>, and returns it. Registering over the full walk — including
        /// non-<typeparamref name="T"/> children and hookless blocks that only override
        /// <see cref="Block.RegisterDependencies"/> — is required so the asset reimports when any
        /// contributing block's inputs change.
        ///
        /// <para>
        /// Stateless by design: nothing is cached on the collection (which outlives the import — the managed
        /// importer object is reused across imports). Each call rebuilds from the current block set and
        /// re-registers, so the result always reflects the collection as it is right now — including edits made
        /// through <see cref="SerializedProperty"/>, which bypass the mutators — and dependencies are always
        /// registered for the given import. Callers that dispatch more than once should hold the returned token
        /// for the duration of the import rather than calling again.
        /// </para>
        ///
        /// <para>
        /// Throws from <see cref="BuildDispatch"/> on a cyclic tree. Never returns null — a blockless
        /// collection yields an empty, non-null token.
        /// </para>
        /// </summary>
        /// <param name="ctx">The import context used for dependency registration.</param>
        /// <exception cref="InvalidOperationException">Thrown when a circular reference is detected.</exception>
        public BlockDispatch PrepareForDispatch(AssetImportContext ctx)
        {
            var dispatch = BuildDispatch();
            foreach (var block in dispatch.Blocks)
            {
                if (block is Block b)
                    b.RegisterBlockDependencies(ctx);
            }

            return dispatch;
        }

        /// <summary>
        /// Returns every block in the depth-first enabled walk assignable to <typeparamref name="TResult"/>.
        /// Built on the full walk (the same one <see cref="BuildDispatch"/> dispatches over), so
        /// non-<typeparamref name="T"/> descendants reached via composite children are visible here too, then
        /// filtered to <typeparamref name="TResult"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a circular reference is detected.</exception>
        public IEnumerable<TResult> Traverse<TResult>() where TResult : IBlock
        {
            var fullWalk = new List<IBlock>();
            CollectEnabledBlocksDepthFirst(fullWalk);
            foreach (var block in fullWalk)
            {
                if (block is TResult typedBlock)
                    yield return typedBlock;
            }
        }

    }
}
