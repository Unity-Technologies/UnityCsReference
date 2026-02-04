// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements;

/// <summary>
/// Indicates options for clearing a VisualElement's children.
/// For more information, refer to <see cref="VisualElement.Clear(VisualElementClearOptions)"/>.
/// </summary>
[Flags]
public enum VisualElementClearOptions
{
    /// <summary>
    /// Clears only the direct children of the VisualElement.
    /// </summary>
    None = 0,
    /// <summary>
    /// Clears all children of the VisualElement recursively.
    /// </summary>
    Recursive = 1 << 0,
    /// <summary>
    /// Clears all children of the VisualElement recursively and releases their resources.
    /// </summary>
    RecursiveReleaseResources = Recursive | 1 << 1,
}
