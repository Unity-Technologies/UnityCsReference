// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The number of connections a port can accept.
    /// </summary>
    public enum PortCapacity
    {
        /// <summary>
        /// The port cannot accept any connection.
        /// </summary>
        None,

        /// <summary>
        /// The port can only accept a single connection.
        /// </summary>
        Single,

        /// <summary>
        /// The port can accept multiple connections.
        /// </summary>
        Multi
    }
}
