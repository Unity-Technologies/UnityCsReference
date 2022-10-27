// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Port directions.
    /// </summary>
    [Flags]
    enum PortDirection
    {
        /// <summary>
        /// The port does not have a specific direction. It can receive or send information.
        /// </summary>
        None = 0,

        /// <summary>
        /// The port is used to receive information.
        /// </summary>
        Input = 1,

        /// <summary>
        /// The port is used to send information.
        /// </summary>
        Output = 2
    }
}
