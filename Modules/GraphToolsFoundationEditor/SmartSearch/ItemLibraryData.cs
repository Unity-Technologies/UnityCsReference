// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface to define custom data for an <see cref="ItemLibraryItem"/>.
    /// </summary>
    interface IItemLibraryData
    {
    }

    /// <summary>
    /// Tag for specific <see cref="ItemLibraryItem"/>.
    /// </summary>
    enum CommonLibraryTags
    {
        StickyNote
    }

    /// <summary>
    /// Data for a <see cref="ItemLibraryItem"/> tagged by a <see cref="CommonLibraryTags"/>.
    /// </summary>
    readonly struct TagItemLibraryData : IItemLibraryData
    {
        public CommonLibraryTags Tag { get; }

        public TagItemLibraryData(CommonLibraryTags tag)
        {
            Tag = tag;
        }
    }

    /// <summary>
    /// Data for a <see cref="ItemLibraryItem"/> linked to a type.
    /// </summary>
    readonly struct TypeItemLibraryData : IItemLibraryData
    {
        public TypeHandle Type { get; }

        public TypeItemLibraryData(TypeHandle type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Data for a <see cref="ItemLibraryItem"/> linked to a node.
    /// </summary>
    readonly struct NodeItemLibraryData : IItemLibraryData
    {
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the NodeItemLibraryData class.
        /// </summary>
        /// <param name="type">Type of the node represented by the item.</param>
        public NodeItemLibraryData(Type type)
        {
            Type = type;
        }
    }
}
