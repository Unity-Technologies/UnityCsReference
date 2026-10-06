// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Abstract base for every block that references another block collection. Points at an
    /// <see cref="IBlockCollectionProvider"/> and expands to independent deep-clones of its blocks at
    /// import time; the provider itself is never mutated.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    [Serializable]
    [ImportBlock(1, "Block Collection Reference", "Base class for blocks that reference another block collection.")]
    public abstract class BlockCollectionReference : Block
    {
        /// <summary>Serialized field name holding the reference Object (for PropertyField rendering).</summary>
        internal abstract string ReferenceFieldName { get; }

        /// <summary>
        /// Whether the editor renders the reference field. A picked reference (e.g. a Block Asset) shows
        /// it; an auto-managed reference (a fixed inheritance link) hides it.
        /// </summary>
        internal virtual bool ShowReferenceField => true;

        /// <summary>The currently-referenced provider Object, or null.</summary>
        internal abstract UnityEngine.Object GetReferencedObject();

        /// <summary>SerializedProperty path to the m_Blocks array inside the provider.</summary>
        internal abstract string ProviderBlocksPath { get; }

        /// <summary>
        /// Tracks the providers expanded on the current path: re-entering one is a cycle, while a
        /// sibling re-expansion of the same provider is not.
        /// </summary>
        internal sealed class ProviderPathGuard
        {
            readonly HashSet<EntityId> m_Path = new HashSet<EntityId>();

            public bool TryEnter(UnityEngine.Object provider) => m_Path.Add(provider.GetEntityId());
            public void Exit(UnityEngine.Object provider) => m_Path.Remove(provider.GetEntityId());
        }

        class CloneHost : ScriptableObject
        {
            [SerializeReference] public List<IBlock> blocks = new List<IBlock>();
        }

        /// <summary>
        /// Deep-clones blocks into detached instances through a single <c>[SerializeReference]</c> host
        /// round-trip, preserving concrete types, null entries, and children shared between blocks —
        /// a per-block round-trip would duplicate a shared child and the traversal would dispatch it twice.
        /// </summary>
        internal static List<IBlock> DeepCloneBlocks(IEnumerable<IBlock> source)
        {
            var result = new List<IBlock>();
            if (source == null)
                return result;

            var host = ScriptableObject.CreateInstance<CloneHost>();
            try
            {
                host.blocks.AddRange(source);
                var clone = ScriptableObject.CreateInstance<CloneHost>();
                try
                {
                    EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(host), clone);
                    result.AddRange(clone.blocks);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(clone);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            return result;
        }
    }
}
