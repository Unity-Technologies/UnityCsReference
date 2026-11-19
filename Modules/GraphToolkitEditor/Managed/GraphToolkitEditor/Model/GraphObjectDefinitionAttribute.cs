// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Links a <see cref="GraphObject"/> to a file extension. This can be added on classes derived from <see cref="GraphObject"/> only.
    /// </summary>
    /// <remarks>A given <see cref="GraphObject"/> can be linked to multiple extensions. A given extension can only be linked to a single <see cref="GraphObject"/> type.</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [UnityRestricted]
    internal class GraphObjectDefinitionAttribute : Attribute
    {
        /// <summary>
        /// The name of the static event method in a class derived <see cref="GraphObject"/> that allow customizing the loading of the <see cref="GraphObject"/>.
        /// </summary>
        public static readonly string LoadGraphObjectFromFileOnDiskMethodName = "LoadGraphObjectFromFileOnDisk";

        /// <summary>
        /// The delegate type of the static event method in a class derived <see cref="GraphObject"/> that allow customizing the loading of the <see cref="GraphObject"/>.
        /// A custom method to load a derived class from <see cref="GraphObject"/> from a file on disk must have this signature and be named "LoadGraphObjectFromFileOnDisk".
        /// </summary>
        public delegate GraphObject LoadGraphObjectLoader(string filePath);

        /// <summary>
        /// The file extension. Does not include the '.' character at the start.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Create a instance of the <see cref="GraphObjectDefinitionAttribute"/> class.
        /// </summary>
        /// <param name="extension">The file extension. Must not include the '.' character at the start of the extension.</param>
        public GraphObjectDefinitionAttribute(string extension)
        {
            Extension = extension;
        }
    }
}
