// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.GraphToolkit.Editor.ContextualMenuItems
{
    /// <summary>
    /// Provides the list <see cref="ContextualMenuItem"/>s for an element.
    /// </summary>
    [UnityRestricted]
    interface IHasContextualMenuItems
    {
        /// <summary>
        /// The contextual menu items.
        /// </summary>
        IReadOnlyList<ContextualMenuItem> ContextualMenuItems { get; }
    }
}
