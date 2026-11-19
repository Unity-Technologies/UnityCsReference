// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The state of a graph element model.
    /// </summary>
    [UnityRestricted]
    internal enum ModelState
    {
        /// <summary>
        /// The model is enabled. This is the default value.
        /// </summary>
        Enabled = 0, // default value

        /// <summary>
        /// The model is disabled.
        /// </summary>
        Disabled,
    }
}
