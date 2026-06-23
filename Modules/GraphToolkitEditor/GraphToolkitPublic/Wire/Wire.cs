// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Represents a wire connection between an output port and an input port in a graph.
    /// </summary>
    /// <remarks>
    /// A <see cref="Wire"/> corresponds to the connection between two ports, including connections that pass through
    /// wire portals. Use <see cref="Graph.GetWire"/> to obtain a <see cref="Wire"/> for a connected port pair.
    /// </remarks>
    /// <example>
    /// Retrieve a wire between two connected ports and customize its visual properties on the graph canvas.
    /// <code>
    /// Wire wire = graph.GetWire(outputPort, inputPort);
    /// wire.WidthOverride = 4f;
    /// wire.Opacity = 0.5f;
    /// wire.IsDashed = true;
    /// </code>
    /// </example>
    /// <seealso cref="Graph.GetWire"/>
    /// <seealso cref="IPort"/>
    public sealed partial class Wire : IEquatable<Wire>
    {
        /// <summary>
        /// The output port at the start of the connection.
        /// </summary>
        public IPort OutputPort { get; }

        /// <summary>
        /// The input port at the end of the connection.
        /// </summary>
        public IPort InputPort { get; }

        /// <summary>
        /// The <see cref="Graph"/> that contains the current wire connection.
        /// </summary>
        public Graph Graph => (OutputPort as PortModel)?.GraphModel is GraphModelImp graphModelImp ? graphModelImp.Graph : null;

        /// <summary>
        /// The width override applied to the wire.
        /// </summary>
        /// <remarks>
        /// Setting the value to 0 removes the width override on the wire, so the wire reverts to the default thickness defined by the graph canvas style.
        /// </remarks>
        public float WidthOverride
        {
            get
            {
                foreach (var wireModel in m_Implementation.Wires)
                {
                    if (wireModel != null)
                        return wireModel.WidthOverride;
                }

                return 0f;
            }
            set
            {
                foreach (var wireModel in m_Implementation.Wires)
                {
                    if (wireModel != null)
                        wireModel.WidthOverride = value;
                }
            }
        }

        /// <summary>
        /// The opacity multiplier applied to the wire.
        /// </summary>
        /// <remarks>
        /// The opacity multiplier is clamped to the [0, 1] range when set. A value of 0 makes the wire fully transparent, while a value of 1 leaves the wire fully opaque.
        /// </remarks>
        public float Opacity
        {
            get
            {
                foreach (var wireModel in m_Implementation.Wires)
                {
                    if (wireModel != null)
                        return wireModel.Opacity;
                }

                return 1f;
            }
            set
            {
                value = Mathf.Clamp01(value);
                foreach (var wireModel in m_Implementation.Wires)
                {
                    if (wireModel != null)
                        wireModel.Opacity = value;
                }
            }
        }

        /// <summary>
        /// Whether the wire is drawn with a dashed pattern.
        /// </summary>
        public bool IsDashed
        {
            get
            {
                foreach (var wireModel in m_Implementation.Wires)
                {
                    if (wireModel != null)
                        return wireModel.IsDashed;
                }

                return false;
            }
            set
            {
                foreach (var wireModel in m_Implementation.Wires)
                {
                    if (wireModel != null)
                        wireModel.IsDashed = value;
                }
            }
        }

        /// <summary>
        /// Determines whether the current wire represents the same connection as another wire.
        /// </summary>
        /// <param name="other">The wire to compare with the current instance.</param>
        /// <returns>true if both wires share the same output and input port; otherwise, false.</returns>
        public bool Equals(Wire other)
        {
            if (other is null)
                return false;

            return OutputPort.ID == other.OutputPort.ID && InputPort.ID == other.InputPort.ID;
        }

        /// <summary>
        /// Determines whether the current wire is equal to the specified object.
        /// </summary>
        /// <param name="obj">The object to compare with the current wire.</param>
        /// <returns>true if <paramref name="obj"/> is a <see cref="Wire"/> that represents the same connection as the current wire; otherwise, false.</returns>
        public override bool Equals(object obj) => obj is Wire other && Equals(other);

        /// <summary>
        /// Returns a hash code that represents the current wire.
        /// </summary>
        /// <returns>A hash code derived from the output and input port identifiers of the current wire.</returns>
        public override int GetHashCode() => HashCode.Combine(OutputPort.ID, InputPort.ID);

        /// <summary>
        /// Determine whether two wires represent the same connection.
        /// </summary>
        public static bool operator ==(Wire left, Wire right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        /// <summary>
        /// Determine whether two wires represent different connections.
        /// </summary>
        public static bool operator !=(Wire left, Wire right) => !(left == right);
    }
}
