// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ItemLibrary.Editor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// interface for ModelUI that will show the <see cref="ItemLibraryWindow"/>.
    /// </summary>
    interface IShowItemLibraryUI_Internal
    {
        /// <summary>
        /// Shows the <see cref="ItemLibraryWindow"/>.
        /// </summary>
        /// <param name="mousePosition">The mouse position in window coordinates.</param>
        /// <returns>True if a <see cref="ItemLibraryWindow"/> could be displayed.</returns>
        bool ShowItemLibrary(Vector2 mousePosition);
    }
}
