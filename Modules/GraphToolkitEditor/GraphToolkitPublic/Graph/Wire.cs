// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Represents a runtime-friendly single logical connection between an output port and an input port in a graph.
    /// </summary>
    /// <remarks>
    /// Two instances are considered equal when they refer to the same output port and the same input port.
    /// </remarks>
    [Serializable]
    public sealed class Wire : IEquatable<Wire>
    {
        [SerializeField]
        Hash128 m_OutputPortGuid;

        [SerializeField]
        Hash128 m_InputPortGuid;

        /// <summary>
        /// The ID of the output port for this connection.
        /// </summary>
        public Hash128 OutputPortID => m_OutputPortGuid;

        /// <summary>
        /// The ID of the input port for this connection.
        /// </summary>
        public Hash128 InputPortID => m_InputPortGuid;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wire"/> class.
        /// </summary>
        /// <param name="outputPortID">The ID of the output port where the connection starts.</param>
        /// <param name="inputPortID">The ID of the input port where the connection ends.</param>
        internal Wire(Hash128 outputPortID, Hash128 inputPortID)
        {
            m_OutputPortGuid = outputPortID;
            m_InputPortGuid = inputPortID;
        }

        /// <inheritdoc />
        public bool Equals(Wire other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return OutputPortID == other.OutputPortID
                && InputPortID == other.InputPortID;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Wire other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(OutputPortID, InputPortID);
        }

        /// <summary>
        /// Determines whether two instances are equal.
        /// </summary>
        public static bool operator ==(Wire left, Wire right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two instances are not equal.
        /// </summary>
        public static bool operator !=(Wire left, Wire right) => !(left == right);
    }
}
