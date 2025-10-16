// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A IVewContext used to create a clone of the condition <see cref="ModelView"/>s for drag and drop.
    /// </summary>
    [UnityRestricted]
    internal class DragCloneCreationContext : IViewContext
    {
        public static readonly DragCloneCreationContext Default = new DragCloneCreationContext();

        public bool Equals(IViewContext other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
