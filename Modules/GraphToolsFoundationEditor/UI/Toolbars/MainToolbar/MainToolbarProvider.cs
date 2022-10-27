// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Default implementation of <see cref="OverlayToolbarProvider"/> for the main toolbar.
    /// </summary>
    class MainToolbarProvider : OverlayToolbarProvider
    {
        /// <inheritdoc />
        public override IEnumerable<string> GetElementIds()
        {
            return new[]
            {
                NewGraphButton.id, SaveButton.id
            };
        }
    }
}
