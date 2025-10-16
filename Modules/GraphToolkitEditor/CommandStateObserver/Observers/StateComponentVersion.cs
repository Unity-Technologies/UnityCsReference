// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// The version of a state component.
    /// </summary>
    [UnityRestricted]
    internal readonly struct StateComponentVersion : IEquatable<StateComponentVersion>
    {
        /// <summary>
        /// The hash code of the state component.
        /// </summary>
        public readonly int HashCode;

        /// <summary>
        /// The version number of the state component.
        /// </summary>
        public readonly uint Version;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateComponentVersion"/> class.
        /// </summary>
        public StateComponentVersion(int hashCode, uint version)
        {
            HashCode = hashCode;
            Version = version;
        }

        /// <inheritdoc />
        public override string ToString() => $"{HashCode}.{Version}";

        /// <inheritdoc />
        public bool Equals(StateComponentVersion other)
        {
            return HashCode == other.HashCode && Version == other.Version;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is StateComponentVersion other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return System.HashCode.Combine(HashCode, Version);
        }
    }
}
