// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ItemLibrary.Editor;
using UnityEngine.Assertions;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Attribute to create a <see cref="ItemLibraryItem"/> out of a <see cref="GraphElementModel"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    class LibraryItemAttribute : Attribute
    {
        /// <summary>
        /// Type of Stencil to use to create the element.
        /// </summary>
        public Type StencilType { get; }
        /// <summary>
        /// Path of the item in the <see cref="ItemLibraryLibrary_Internal"/>.
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// Search context where this item should figure.
        /// </summary>
        public SearchContext Context { get; }

        /// <summary>
        /// Style to apply to this item.
        /// </summary>
        /// <remarks>Allows UI to apply custom styles to elements made with this item.</remarks>
        public string StyleName { get; }

        /// <summary>
        /// When applicable, the mode of this item.
        /// </summary>
        /// <remarks>A node can have multiple modes. Each mode needs to be added as a different item in the Item Library.</remarks>
        public string Mode { get; }

        /// <summary>
        /// Initializes a new instance of the LibraryItemAttribute class.
        /// </summary>
        /// <param name="stencilType">Type of Stencil to use to create the element.</param>
        /// <param name="context">Search context where this item should figure.</param>
        /// <param name="path">Path of the item in the library.</param>
        /// <param name="styleName">Style name to give to this item.</param>
        /// <param name="mode">When applicable, the mode of this item.</param>
        public LibraryItemAttribute(Type stencilType, SearchContext context, string path, string styleName = null, string mode = null)
        {
            Assert.IsTrue(
                stencilType.IsSubclassOf(typeof(Stencil)),
                $"Parameter stencilType is type of {stencilType.FullName} which is not a subclass of {typeof(Stencil).FullName}");

            StencilType = stencilType;
            Path = path;
            Context = context;
            StyleName = styleName;
            Mode = mode;
        }
    }
}
