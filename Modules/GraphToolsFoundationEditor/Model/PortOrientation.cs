// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The orientation of a port.
    /// </summary>
    enum PortOrientation
    {
        /// <summary>
        /// The port is placed on the left or right side of the node. Wires connected to this port emerge from the left and the right side of the port.
        /// </summary>
        Horizontal,

        /// <summary>
        /// The port is placed on the top or the bottom of the node. Wires connected to this port emerge from the top and bottom of the port.
        /// </summary>
        Vertical
    }
}
