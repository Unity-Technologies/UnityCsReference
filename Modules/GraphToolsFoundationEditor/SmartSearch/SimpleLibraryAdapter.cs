// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Simple <see cref="ItemLibraryAdapter"/> without preview.
    /// </summary>
    class SimpleLibraryAdapter : ItemLibraryAdapter
    {
        // TODO: Disable details panel for now
        /// <inheritdoc />
        public override bool HasDetailsPanel => false;

        public SimpleLibraryAdapter(string title, string toolName = null)
            : base(title, toolName)
        {
        }
    }
}
