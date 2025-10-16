// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Toolbars;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for toolbar definitions.
    /// </summary>
    [UnityRestricted]
    internal abstract class ToolbarDefinition
    {
        /// <summary>
        /// The ids of the element to display in the toolbar. The ids are the one specified using the <see cref="EditorToolbarElementAttribute"/>.
        /// </summary>
        public abstract IEnumerable<string> ElementIds { get; }
    }
}
