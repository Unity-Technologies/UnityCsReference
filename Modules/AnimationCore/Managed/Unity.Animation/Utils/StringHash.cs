// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_STRINGHASH

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Unity.Collections;
using UnityEngine.Animations;



namespace Unity.Animations
{

    /// <summary>
    /// A 32-bit hash of a string that identifies bones, channels, and clip bindings in a rig.
    /// </summary>
    /// <remarks>
    /// The hash of a string is the CRC32 of the string. <see cref="RigBuilder"/> and the channel types compute this
    /// hash for transform paths and property names. Clips store it in their bindings. For example,
    /// `new StringHash("Hips/Spine")` is the identifier of the bone with the path `Hips/Spine` in a rig built from
    /// transforms or an Avatar. The null string and the empty string both hash to 0.
    /// </remarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [GenerateTestsForBurstCompatibility]
    [Serializable]
    public struct StringHash : IEquatable<StringHash>
    {
        /// <summary>
        /// Hash value.
        /// </summary>
        public uint Id;

        /// <summary>
        /// Creates a hash for a string.
        /// </summary>
        /// <remarks>
        /// The hash is the CRC32 of the string, the same as the identifiers that rigs and clips use for transform paths
        /// and property names. A null or empty string gives a hash of 0, which <see cref="IsNullOrEmpty"/> reports as
        /// null or empty.
        /// </remarks>
        /// <param name="str">The string to hash, such as a transform path or a property name.</param>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringHash(string str) =>
            Id = Hash(str);

        /// <summary>
        /// Converts a string into its hash, so that you can pass a string where a StringHash is expected.
        /// </summary>
        /// <remarks>
        /// The conversion calls <see cref="StringHash(string)"/>, so a null or empty string gives a hash of 0. Use it
        /// to pass a string where a `StringHash` is expected, for example `FindBoneIndex("Hips/Spine")`.
        /// </remarks>
        /// <param name="str">The string to hash, such as a transform path or a property name.</param>
        /// <returns>The hash of the string. The hash is 0 if the string is null or empty.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StringHash(string str) =>
            new StringHash(str);

        /// <summary>
        /// Implicit conversion from uint to StringHash.
        /// </summary>
        /// <param name="id">uint value</param>
        /// <returns><see cref="StringHash"/> value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator StringHash(uint id) =>
            new StringHash {Id = id};

        /// <summary>
        /// Tests whether the <see cref="StringHash"/> is null or empty.
        /// </summary>
        /// <param name="strHash"><see cref="StringHash"/> value</param>
        /// <returns>True if the value is null or empty; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrEmpty(StringHash strHash) =>
            strHash.Id == 0;

        /// <summary>
        /// Compare two <see cref="StringHash"/> values for equality.
        /// </summary>
        /// <returns>True if the two values are equal; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator==(StringHash lhs, StringHash rhs) =>
            lhs.Id == rhs.Id;

        /// <summary>
        /// Compare two <see cref="StringHash"/> values for inequality.
        /// </summary>
        /// <returns>True if the two values aren't equal; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator!=(StringHash lhs, StringHash rhs) =>
            lhs.Id != rhs.Id;

        /// <summary>
        /// Determines whether the specified <see cref="StringHash"/> is equal to the current <see cref="StringHash"/>.
        /// </summary>
        /// <returns>True if the two values are equal; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(StringHash other) =>
            Id == other.Id;

        /// <summary>
        /// Calculates hash code for the <see cref="StringHash"/>.
        /// </summary>
        /// <returns>hash code</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() =>
            (int)Id;

        /// <summary>
        /// Determines whether the specified <see cref="StringHash"/> is equal to the current <see cref="StringHash"/>.
        /// </summary>
        /// <returns>True if the two values are equal; false otherwise.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed object")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (other == null || !(other is StringHash))
                return false;

            return Id == ((StringHash)other).Id;
        }

        [ExcludeFromBurstCompatTesting("Takes managed string")]
        internal static uint Hash(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            var hash = UnityEngine.Crc32Utility.Compute(str);


            return hash;
        }

        /// <summary>
        /// Combines two string hashes into one hash.
        /// </summary>
        /// <remarks>
        /// Use this method to build the hash of an animated property from the hash of its transform path and the hash
        /// of its property name, as <see cref="FloatChannel"/> does. The result depends on the order of the arguments:
        /// `CombineHash(a, b)` and `CombineHash(b, a)` usually differ.
        /// </remarks>
        /// <param name="a">The first hash to combine. The order of the arguments matters.</param>
        /// <param name="b">The second hash to combine. The order of the arguments matters.</param>
        /// <returns>A new hash that depends on both input hashes and on their order.</returns>
        public static StringHash CombineHash(StringHash a, StringHash b)
        {
            ulong x = ((ulong)a.Id << 32) | b.Id;
            unchecked {
                x ^= x >> 33; x *= 0xff51afd7ed558ccdUL;
                x ^= x >> 33; x *= 0xc4ceb9fe1a85ec53UL;
                x ^= x >> 33;
            }
            return (uint)x;
        }

        [ExcludeFromBurstCompatTesting("Returns managed string")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal string DebuggerDisplay
        {
            get
            {
                string debugDisplay = default;
                debugDisplay = $"Id = {Id}";
                return debugDisplay;
            }
        }

        /// <summary>Implicitly converts a GenericBinding to a StringHash.</summary>
        /// <remarks>
        /// The hash is the same in every session, so you can serialize it. For a MonoBehaviour property, the hash
        /// includes the assembly name and full class name of the script, so renaming the script class changes it.
        /// </remarks>
        /// <param name="binding">The GenericBinding to convert.</param>
        /// <returns>Returns a unique StringHash value for this GenericBinding.</returns>
        [ExcludeFromBurstCompatTesting("Resolves the script on the main thread")]
        public static implicit operator StringHash(GenericBinding binding)
        {
            var scriptTypeHash = binding.scriptEntityId.IsValid()
                ? GenericBindingUtility.GetScriptTypeHash(binding.scriptEntityId)
                : 0u;
            var componentID = CombineHash(
                CombineHash((uint)binding.typeID, scriptTypeHash),
                (uint)binding.customTypeID);
            var propertyID = CombineHash(binding.transformPathHash, binding.propertyNameHash);

            return CombineHash(propertyID, componentID);
        }

    }
}
