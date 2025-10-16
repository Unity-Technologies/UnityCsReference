// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Overlays;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An <see cref="Overlay"/> that contains a <see cref="RootView"/>.
    /// </summary>
    /// <remarks>
    /// It serves as a base for creating various overlays that contains a <see cref="RootView"/>, such as overlays for the Blackboard,
    /// minimap, or inspector.
    /// </remarks>
    [UnityRestricted]
    internal abstract class OverlayWithView : Overlay
    {
        /// <summary>
        /// The <see cref="RootView"/> of the overlay.
        /// </summary>
        public abstract RootView RootView { get; }
    }
}
