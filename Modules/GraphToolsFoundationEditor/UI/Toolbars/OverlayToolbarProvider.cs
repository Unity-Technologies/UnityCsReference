// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Toolbars;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for overlay toolbar providers.
    /// </summary>
    abstract class OverlayToolbarProvider
    {
        /// <summary>
        /// Returns the ids of the element to display in the toolbar. The ids are the one specified using the <see cref="EditorToolbarElementAttribute"/>.
        /// </summary>
        /// <returns>The ids of the element to display in the toolbar.</returns>
        public abstract IEnumerable<string> GetElementIds();
    }
}
