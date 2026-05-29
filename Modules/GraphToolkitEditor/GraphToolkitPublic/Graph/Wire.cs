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
        /// The output port GUID for this connection.
        /// </summary>
        public Hash128 OutputPortGuid => m_OutputPortGuid;

        /// <summary>
        /// The input port GUID for this connection.
        /// </summary>
        public Hash128 InputPortGuid => m_InputPortGuid;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wire"/> class.
        /// </summary>
        /// <param name="outputPortGuid">The GUID of the output port where the connection starts.</param>
        /// <param name="inputPortGuid">The GUID of the input port where the connection ends.</param>
        internal Wire(Hash128 outputPortGuid, Hash128 inputPortGuid)
        {
            m_OutputPortGuid = outputPortGuid;
            m_InputPortGuid = inputPortGuid;
        }

        /// <inheritdoc />
        public bool Equals(Wire other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return OutputPortGuid == other.OutputPortGuid
                && InputPortGuid == other.InputPortGuid;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Wire other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(OutputPortGuid, InputPortGuid);
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
