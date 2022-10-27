// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// A serializable version of <see cref="UnityEditor.GUID"/>.
    /// </summary>
    /// <remarks>
    /// This implementation is using a Hash128 backing. The binary representation is the same as the
    /// <see cref="UnityEditor.GUID"/> one, but the string version differs.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation", "Unity.GraphTools.Foundation")]
    struct SerializableGUID : IEquatable<SerializableGUID>
    {
        /// <summary>
        /// Determines whether two SerializableGUIDs are equal.
        /// </summary>
        /// <param name="other">The SerializableGUID to compare with the current one.</param>
        /// <returns>True if the specified SerializableGUID is equal to the current one; otherwise, false.</returns>
        public bool Equals(SerializableGUID other)
        {
            return m_Value0 == other.m_Value0 && m_Value1 == other.m_Value1;
        }

        /// <summary>
        /// Determines whether a SerializableGUIDs is equal to a given object.
        /// </summary>
        /// <param name="obj">The object to compare with the current SerializableGUID.</param>
        /// <returns>True if the specified object is equal to the current SerializableGUID; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is SerializableGUID other && Equals(other);
        }

        /// <summary>
        /// Get a hash code for the SerializableGUID.
        /// </summary>
        /// <returns>A hash code for the GUID.</returns>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")] // Making the fields readonly breaks the serialization.
        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value0.GetHashCode() * 397) ^ m_Value1.GetHashCode();
            }
        }

        /// <summary>
        /// Determines whether two SerializableGUIDs are equal.
        /// </summary>
        /// <param name="left">The left SerializableGUID to compare.</param>
        /// <param name="right">The right SerializableGUID to compare.</param>
        /// <returns>True if both specified SerializableGUIDs are equal; otherwise, false.</returns>
        public static bool operator==(SerializableGUID left, SerializableGUID right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two SerializableGUIDs are not equal.
        /// </summary>
        /// <param name="left">The left SerializableGUID to compare.</param>
        /// <param name="right">The right SerializableGUID to compare.</param>
        /// <returns>True if the specified SerializableGUIDs are not equal; otherwise, false.</returns>
        public static bool operator!=(SerializableGUID left, SerializableGUID right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Get the pair of ulong representing the SerializableGUID.
        /// </summary>
        /// <returns>A tuple containing the pair of ulong representing the SerializableGUID.</returns>
        public (ulong, ulong) ToParts()
        {
            return (m_Value0, m_Value1);
        }

        [FieldOffset(0)]
        Hash128 m_Hash128;

        [SerializeField]
        [FieldOffset(0)]
        ulong m_Value0;
        [SerializeField]
        [FieldOffset(8)]
        ulong m_Value1;

        /// <summary>
        /// Whether or not the SerializableGUID is valid.
        /// </summary>
        public bool Valid => m_Hash128.isValid;

        /// <summary>
        /// Returns a string representation of the SerializableGUID.
        /// </summary>
        /// <returns>A string that represents the SerializableGUID.</returns>
        public override string ToString()
        {
            return m_Hash128.ToString();
        }

        /// <summary>
        /// Generates a new SerializableGUID.
        /// </summary>
        /// <remarks>
        /// When called from editor code, the method will use <see cref="GUID.Generate()"/> to generate
        /// the GUID. When called from the runtime, it will use <see cref="System.Guid.NewGuid()"/>.</remarks>
        /// <returns>A new SerializableGUID.</returns>
        public static SerializableGUID Generate()
        {
            return new SerializableGUID(Hash128.Compute(Guid.NewGuid().ToByteArray()));
        }

        SerializableGUID(Hash128 hash)
        {
            // Values will be overriden by new Hash128 but are required for the struct constructor.
            m_Value0 = 0;
            m_Value1 = 0;
            m_Hash128 = hash;
        }

        /// <summary>
        /// Initializes a new SerializableGUID from a string.
        /// </summary>
        /// <remarks>The string passed in must be parsable by <see cref="Hash128.Parse(string)"/>.</remarks>
        /// <param name="hashString">The string used to initialize the SerializableGUID.</param>
        public SerializableGUID(string hashString)
        {
            // Values will be overriden by Hash128.Parse(string) but are required for the struct constructor.
            m_Value0 = 0;
            m_Value1 = 0;
            m_Hash128 = Hash128.Parse(hashString);
        }

        /// <summary>
        /// Initializes a new SerializableGUID from a pair of ulongs.
        /// </summary>
        /// <param name="a">The first part of the GUID.</param>
        /// <param name="b">The second part of the GUID.</param>
        public SerializableGUID(ulong a, ulong b)
        {
            m_Hash128 = default;
            m_Value0 = a;
            m_Value1 = b;
        }

        /// <summary>
        /// Cast a SerializedGUID as a <see cref="Hash128"/>.
        /// </summary>
        /// <param name="sGuid">The SerializedGUID to cast.</param>
        /// <returns>The cast value.</returns>
        public static implicit operator Hash128(SerializableGUID sGuid) => sGuid.m_Hash128;

        /// <summary>
        /// Cast a <see cref="Hash128"/> as a SerializedGUID.
        /// </summary>
        /// <param name="hash">The <see cref="Hash128"/> to cast.</param>
        /// <returns>The cast value.</returns>
        public static implicit operator SerializableGUID(Hash128 hash) => new SerializableGUID(hash);
    }
}
