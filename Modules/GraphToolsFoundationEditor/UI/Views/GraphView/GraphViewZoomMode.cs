// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// THe graphview zoom mode enum that hints the level of details.
    /// </summary>
    enum GraphViewZoomMode
    {
        /// <summary>
        /// The zoom is normal at 100% and above, and still until the small zoom level is reached. Everything is considered visible.
        /// </summary>
        Normal,

        /// <summary>
        /// At the small zoom level ports are no longer visible and are therefore hidden
        /// </summary>
        Small,

        /// <summary>
        /// At very small zoom the nodes are too little to even have their name displayed.
        /// </summary>
        VerySmall,

        /// <summary>
        /// The unknown zoom level is used at element creation since we don't known how the element is configured by default.
        /// </summary>
        Unknown
    }
}
