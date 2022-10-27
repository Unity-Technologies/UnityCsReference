// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods for <see cref="PortModel"/>.
    /// </summary>
    static class PortModelExtensions
    {
        /// <summary>
        /// Checks whether this port has any connection.
        /// </summary>
        /// <param name="self">The port.</param>
        /// <returns>True if there is at least one wire connected on this port.</returns>
        public static bool IsConnected(this PortModel self)
        {
            return self.GetConnectedWires().Any();
        }

        /// <summary>
        /// Checks whether two ports are equivalent.
        /// </summary>
        /// <param name="a">The first port.</param>
        /// <param name="b">The second port.</param>
        /// <returns>True if the two ports are owned by the same node, have the same direction and have the same unique name.</returns>
        public static bool Equivalent(this PortModel a, PortModel b)
        {
            if (a == null || b == null)
                return a == b;

            return a.Direction == b.Direction && a.NodeModel.Guid == b.NodeModel.Guid && a.UniqueName == b.UniqueName;
        }
    }
}
