// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    [UnityEngine.Internal.ExcludeFromDocs]
    [Serializable]
    [ImportBlock(2, "Block", "Base class for all Import Blocks.")]
    public abstract partial class Block : IBlock, ISerializationCallbackReceiver
    {
        [SerializeField]
        bool m_Enabled = true;

        [SerializeField]
        [HideInInspector]
        string m_BlockInstanceID;

        /// <summary>
        /// Initializes the block and auto-generates a BlockInstanceID.
        /// </summary>
        public Block()
        {
            GenerateBlockInstanceID();
        }

        /// <summary>
        /// Controls whether this block participates in traversal and import. Disabled blocks are skipped.
        /// </summary>
        public bool Enabled
        {
            get => m_Enabled;
            set => m_Enabled = value;
        }

        /// <summary>
        /// Stable per-instance GUID identifying this block. The property-override system uses it to target a
        /// specific block through a chain of block-collection references (each hop keyed by BlockInstanceID).
        /// </summary>
        public string BlockInstanceID
        {
            get => m_BlockInstanceID;
        }

        internal void GenerateBlockInstanceID()
        {
            m_BlockInstanceID = Guid.NewGuid().ToString();
        }

        // Used by "paste values" to keep the target block's identity while copying only its field data.
        internal void SetBlockInstanceID(string instanceId)
        {
            m_BlockInstanceID = instanceId;
        }

        // Assigns a brand-new BlockInstanceID to every block in the given root(s) and to every child block they
        // OWN (held in their own serialized [SerializeReference] fields/collections). Copying a block (paste or
        // duplicate) clones its serialized BlockInstanceID verbatim, so the copy — and its owned children — would
        // otherwise share identity with the source. A copy is a new block: the override system targets blocks by
        // BlockInstanceID, so each owned block must take on fresh identity to stay individually addressable.
        //
        // Deliberately does NOT recurse through GetChildBlocks(): that is the dispatch/traversal API and its
        // contract allows returning blocks resolved from an EXTERNAL asset (e.g. a BlockAssetReference). Rewriting
        // those would mutate another asset's data in place and repoint every importer that references it. We walk
        // only owned serialized fields instead. Cycle- and shared-reference-safe via the visited set.
        internal static void AssignFreshBlockInstanceIds(IEnumerable roots)
        {
            var visited = new HashSet<Block>();
            Walk(roots);

            void Walk(IEnumerable blocks)
            {
                if (blocks == null) return;

                foreach (var candidate in blocks)
                {
                    if (candidate is not Block block || !visited.Add(block))
                        continue;

                    block.GenerateBlockInstanceID();
                    Walk(EnumerateOwnedChildBlocks(block));
                }
            }
        }

        // Yields the blocks a block OWNS via its own serialized fields: [SerializeReference] fields typed as an
        // IBlock, or collections/arrays of them. A BlockAssetReference stores its target as a LazyLoadReference,
        // not an IBlock field, so nothing is yielded for it and its external children are left untouched.
        static IEnumerable<IBlock> EnumerateOwnedChildBlocks(Block block)
        {
            foreach (var field in block.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // Only owned, serialized data counts toward the block's persisted identity graph.
                if (field.IsNotSerialized)
                    continue;

                var value = field.GetValue(block);
                if (value is IBlock ownedBlock)
                {
                    yield return ownedBlock;
                }
                else if (value is IEnumerable sequence && value is not string)
                {
                    foreach (var item in sequence)
                        if (item is IBlock ownedChild)
                            yield return ownedChild;
                }
            }
        }

        static readonly Regex s_PascalCaseRegex = new Regex("(\\B[A-Z])", RegexOptions.Compiled);

        /// <summary>
        /// The display name of this block. Reads from [ImportBlock] attribute by default.
        /// Override for dynamic names that can't be expressed as a constant.
        /// </summary>
        public virtual string Name
        {
            get
            {
                var type = GetType();
                var attr = GetBlockAttribute(type);
                return !string.IsNullOrEmpty(attr?.Name) ? attr.Name : FormatTypeName(type.Name);
            }
        }

        /// <summary>
        /// Optional description of what this block does. Reads from [ImportBlock] attribute by default.
        /// Override for dynamic descriptions that can't be expressed as a constant.
        /// </summary>
        public virtual string Description
        {
            get
            {
                var attr = GetBlockAttribute(GetType());
                return !string.IsNullOrEmpty(attr?.Description) ? attr.Description : string.Empty;
            }
        }

        /// <summary>
        /// Converts a PascalCase type name into a human-readable display name by inserting spaces
        /// before capital letters and stripping trailing "Block" suffix and underscore-suffixed variants.
        /// </summary>
        internal static string FormatTypeName(string typeName)
        {
            int underscoreIndex = typeName.IndexOf('_');
            if (underscoreIndex >= 0)
                typeName = typeName.Substring(0, underscoreIndex);
            if (typeName.EndsWith("Block"))
                typeName = typeName.Substring(0, typeName.Length - "Block".Length);
            return s_PascalCaseRegex.Replace(typeName, " $1");
        }

        internal void RegisterBlockDependencies(AssetImportContext ctx)
        {
            RegisterCustomDependency(ctx);
            RegisterDependencies(ctx);
        }

        void RegisterCustomDependency(AssetImportContext ctx)
        {
            // Only depend on the custom-dependency key if this block carries [ImportBlock]; that attribute is
            // what InitializeAllBlocks registers the key/version-hash under, so a block without it has no key to
            // depend on. GetDependencyName is a pure function of the type, so we derive it directly.
            if (BlockDependencyManager.GetDependencyAttribute(GetType()) == null)
            {
                // TODO: Ideally change this to be a compile time check
                Debug.LogWarning(
                    $"Block {GetType().Name} does not have an [ImportBlock(version)] attribute. " +
                    $"Every block requires this attribute for proper dependency tracking and versioning. " +
                    $"Add [ImportBlock(1, \"Name\", \"Description\")] above your class declaration.");
                return;
            }

            ctx.DependsOnCustomDependency(ImportBlockAttribute.GetDependencyName(GetType()));
        }

        /// <summary>
        /// Override to register artifact dependencies that cannot be registered inline in a processing
        /// callback. This is the escape hatch for blocks that do not implement any processing
        /// interface and therefore have no natural
        /// point during import traversal to call RegisterArtifactDependency. Most blocks do
        /// not need to override this.
        /// </summary>
        public virtual void RegisterDependencies(AssetImportContext ctx) { }

        /// <summary>
        /// Returns child blocks defined by this block. Default is empty. Override in composite blocks that aggregate other blocks.
        /// </summary>
        public virtual IEnumerable<IBlock> GetChildBlocks()
        {
            return Array.Empty<IBlock>();
        }

        /// <summary>
        /// Registers an artifact dependency.
        /// </summary>
        public static void RegisterArtifactDependency<T>(T artifact, AssetImportContext ctx)
            where T : UnityEngine.Object
        {
            ctx.DependsOnArtifact(artifact);
        }

        /// <inheritdoc cref="RegisterArtifactDependency{T}(T, AssetImportContext)"/>
        public static void RegisterArtifactDependency<T>(List<T> artifacts, AssetImportContext ctx)
            where T : UnityEngine.Object
        {
            if (artifacts == null)
                return;

            foreach (var artifact in artifacts)
                ctx.DependsOnArtifact(artifact);
        }

        /// <inheritdoc cref="RegisterArtifactDependency{T}(T, AssetImportContext)"/>
        public static void RegisterArtifactDependency<T>(LazyLoadReference<T> artifact, AssetImportContext ctx)
            where T : UnityEngine.Object
        {
            ctx.DependsOnArtifact(artifact);
        }

        /// <inheritdoc cref="RegisterArtifactDependency{T}(T, AssetImportContext)"/>
        public static void RegisterArtifactDependency<T>(List<LazyLoadReference<T>> artifacts, AssetImportContext ctx)
            where T : UnityEngine.Object
        {
            if (artifacts == null)
                return;

            foreach (var artifact in artifacts)
                ctx.DependsOnArtifact(artifact);
        }

        static ImportBlockAttribute GetBlockAttribute(Type type)
        {
            return type.GetCustomAttribute<ImportBlockAttribute>(inherit: false);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_BlockInstanceID))
            {
                GenerateBlockInstanceID();
            }
        }
    }
}
