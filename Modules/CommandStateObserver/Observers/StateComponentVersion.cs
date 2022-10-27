// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// The version of a state component.
    /// </summary>
    struct StateComponentVersion : IEquatable<StateComponentVersion>
    {
        /// <summary>
        /// The hash code of the state component.
        /// </summary>
        public int HashCode;
        /// <summary>
        /// The version number of the state component.
        /// </summary>
        public uint Version;

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
