// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace Unity.Entities.Serialization
{
    /// <summary>
    /// Runtime version of the editor type <seealso cref="GlobalObjectId"/>.  These types need to be binary compatible.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RuntimeGlobalObjectId : IEquatable<RuntimeGlobalObjectId>, IComparable<RuntimeGlobalObjectId>
    {
        /// <summary>
        /// Unique identifier within a scene
        /// </summary>
        public long SceneObjectIdentifier0;
        /// <summary>
        /// Unused.
        /// </summary>
        public long SceneObjectIdentifier1;
        /// <summary>
        /// Asset GUID.
        /// </summary>
        public Hash128 AssetGUID;
        /// <summary>
        /// Identifier type.
        /// </summary>
        public int IdentifierType;
        /// <summary>
        /// True if the id is valid.
        /// </summary>
        public bool IsValid => AssetGUID.IsValid;

        /// <inheritdoc/>
        public int CompareTo(RuntimeGlobalObjectId other)
        {
            var ac = AssetGUID.CompareTo(other.AssetGUID);
            if (ac != 0)
                return ac;
            return SceneObjectIdentifier0.CompareTo(other.SceneObjectIdentifier0);
        }

        /// <inheritdoc/>
        public bool Equals(RuntimeGlobalObjectId other)
        {
            return SceneObjectIdentifier0 == other.SceneObjectIdentifier0 && AssetGUID.Equals(other.AssetGUID);
        }

        /// <summary>
        /// Returns the hash code of this id.
        /// </summary>
        /// <returns>The hash code of this id.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (SceneObjectIdentifier0.GetHashCode() * 397) ^ AssetGUID.GetHashCode();
            }
        }

        /// <summary>
        /// Converts the id to a string representation.".
        /// </summary>
        /// <returns>The string representation of the id in the form $"{AssetGUID}:{SceneObjectIdentifier0}.</returns>
        public override string ToString()
        {
            return $"{AssetGUID}:{SceneObjectIdentifier0}";
        }
    }
}
