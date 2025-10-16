// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Specifies the visual style of the connector used to represent a port in the UI.
    /// </summary>
    /// <remarks>
    /// Use this enum to define how the connector of a port appears visually.
    /// The connector indicates the type or role of the port and can help users understand connection semantics.
    /// </remarks>
    public enum PortConnectorUI
    {
        /// <summary>
        /// A circular connector shape.
        /// </summary>
        Circle,

        /// <summary>
        /// An arrowhead connector shape.
        /// </summary>
        Arrowhead
    }
}
