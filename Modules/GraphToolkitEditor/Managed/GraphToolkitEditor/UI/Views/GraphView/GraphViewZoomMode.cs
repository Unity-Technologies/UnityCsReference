// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The graphview zoom mode enum that hints the level of details.
    /// </summary>
    [UnityRestricted]
    internal enum GraphViewZoomMode
    {
        /// <summary>
        /// The zoom is normal at 100% and above.
        /// </summary>
        Normal,

        /// <summary>
        /// Medium zoom level from 75%.
        /// </summary>
        Medium,

        /// <summary>
        /// Small zoom level, under 20%.
        /// </summary>
        Small,

        /// <summary>
        /// Very small zoom level, under 11%.
        /// </summary>
        VerySmall,

        /// <summary>
        /// The unknown zoom level is used at element creation.
        /// </summary>
        Unknown
    }
}
