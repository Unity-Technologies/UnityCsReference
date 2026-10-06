// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    /// <summary>
    /// Marks a Block class with a version number for dependency hash computation and upgrade dispatch.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ImportBlockAttribute : Attribute
    {
        /// <summary>
        /// The block's current version number, used for dependency hash computation and upgrade dispatch.
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// Display name for the block in the Add Block menu. Required.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description for the block shown in the Add Block menu tooltip. Required.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// When false, excludes the block from the "Add Block" menu while still participating in chain hash and upgrade dispatch.
        /// </summary>
        public bool Instantiatable { get; }

        /// <summary>
        /// Initializes the attribute. Every block must declare a version, a display name, and a description.
        /// </summary>
        /// <param name="version">The block's current version number.</param>
        /// <param name="name">Display name shown in the Add Block menu.</param>
        /// <param name="description">Description shown in the Add Block menu tooltip.</param>
        /// <param name="instantiatable">Whether the block appears in the Add Block menu. Defaults to true.</param>
        public ImportBlockAttribute(int version, string name, string description, bool instantiatable = true)
        {
            Version = version;
            Name = name;
            Description = description;
            Instantiatable = instantiatable;
        }

        /// <summary>
        /// Gets the custom dependency name for the given type.
        /// </summary>
        /// <param name="type">The block type.</param>
        /// <returns>The custom dependency key string for this type.</returns>
        public static string GetDependencyName(Type type)
        {
            return $"ImportBlocks.{type.FullName}";
        }
    }
}
