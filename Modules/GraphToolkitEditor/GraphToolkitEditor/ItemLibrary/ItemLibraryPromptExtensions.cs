// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.ItemLibrary.Editor
{
    /// <summary>
    /// Extension methods to display a UI for a <see cref="ItemLibraryLibrary"/>.
    /// </summary>
    [UnityRestricted]
    internal static class ItemLibraryPromptExtensions
    {
        /// <summary>
        /// Shows a library in a popup <see cref="ItemLibraryWindow"/>.
        /// </summary>
        /// <param name="library">The <see cref="ItemLibraryLibrary"/> to browse with this window.</param>
        /// <param name="host">The window to host this window in.</param>
        /// <param name="displayPosition">The position where to display the window.</param>
        /// <param name="typeHandleInfos">The <see cref="TypeHandleInfos"/> to use for this view.</param>
        /// <returns>The <see cref="ItemLibraryWindow"/> instance.</returns>
        public static ItemLibraryWindow Show(this ItemLibraryLibrary library, EditorWindow host, Vector2 displayPosition, TypeHandleInfos typeHandleInfos)
        {
            return ItemLibraryWindow.Show(host, library, displayPosition, typeHandleInfos);
        }

        /// <summary>
        /// Shows a popup <see cref="ItemLibraryWindow"/> restricted to a host window.
        /// </summary>
        /// <param name="library">The <see cref="ItemLibraryLibrary"/> to browse with this window.</param>
        /// <param name="host">The window to host this window in.</param>
        /// <param name="rect">The position and size of the window to create.</param>
        /// <param name="typeHandleInfos">The <see cref="TypeHandleInfos"/> to use for this view.</param>
        public static ItemLibraryWindow Show(this ItemLibraryLibrary library, EditorWindow host, Rect rect, TypeHandleInfos typeHandleInfos)
        {
            return ItemLibraryWindow.Show(host, library, rect, typeHandleInfos);
        }
    }
}
