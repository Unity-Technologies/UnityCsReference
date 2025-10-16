// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEngine.Assertions;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute to create a <see cref="ItemLibraryItem"/> out of a <see cref="GraphElementModel"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [UnityRestricted]
    internal class LibraryItemAttribute : Attribute
    {
        /// <summary>
        /// Type of <see cref="GraphModel"/> to use to create the element.
        /// </summary>
        public Type GraphModelType { get; }
        /// <summary>
        /// Path of the item in the <see cref="ItemLibraryLibrary"/>.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Subtitle of the item.
        /// </summary>
        public string Subtitle { get; }

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
        /// <param name="graphModelType">Type of <see cref="GraphModel"/> to use to create the element.</param>
        /// <param name="path">Path of the item in the library.</param>
        /// <param name="styleName">Style name to give to this item.</param>
        /// <param name="subtitle">The subtitle of this item.</param>
        /// <param name="mode">When applicable, the mode of this item.</param>
        public LibraryItemAttribute(Type graphModelType, string path, string styleName = null, string subtitle = null, string mode = null)
        {
            Assert.IsTrue(
                graphModelType.IsSubclassOf(typeof(GraphModel)),
                $"Parameter {nameof(graphModelType)} is type of {graphModelType.FullName} which is not a subclass of {typeof(GraphModel).FullName}");


            if (path != null && subtitle == null)
            {
                var slash = path.IndexOf('/');
                if (slash != -1)
                {
                    subtitle = path.Substring(0, slash);
                }
            }

            GraphModelType = graphModelType;
            Path = path;
            Subtitle = subtitle;
            StyleName = styleName;
            Mode = mode;
        }
    }
}
