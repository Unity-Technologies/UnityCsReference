// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods for wires.
    /// </summary>
    static class WireModelExtensions
    {
        /// <summary>
        /// Gets the opposite side of an <see cref="WireSide"/>.
        /// </summary>
        /// <param name="side">The side to get the opposite of.</param>
        /// <returns>The opposite side.</returns>
        public static WireSide GetOtherSide(this WireSide side) => side == WireSide.From ? WireSide.To : WireSide.From;

        /// <summary>
        /// Gets the port of a wire on a specific side.
        /// </summary>
        /// <param name="wireModel">The wire to get the port from.</param>
        /// <param name="side">The side of the wire to get the port from.</param>
        /// <returns>The port connected to the side of the wire.</returns>
        public static PortModel GetPort(this WireModel wireModel, WireSide side)
        {
            return side == WireSide.To ? wireModel.ToPort : wireModel.FromPort;
        }

        /// <summary>
        /// Gets the port of a wire on the other side.
        /// </summary>
        /// <param name="wireModel">The wire to get the port from.</param>
        /// <param name="otherSide">The other side of the wire to get the port from.</param>
        /// <returns>The port connected to the other side of the wire.</returns>
        public static PortModel GetOtherPort(this WireModel wireModel, WireSide otherSide) =>
            wireModel.GetPort(otherSide.GetOtherSide());
    }
}
