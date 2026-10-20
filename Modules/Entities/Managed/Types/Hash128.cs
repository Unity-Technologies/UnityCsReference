// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Mathematics;

namespace Unity.Entities
{
    /// <summary>
    /// A 128 bit hash, for cases where 32 or 64 bits are insufficient. Built on top of Unity.Mathematics types
    /// that will Burst-compile to SIMD instructions, for efficiency comparable to a 32-bit hash.
    /// </summary>
    [Serializable]
    public struct Hash128 : IEquatable<Hash128>, IComparable<Hash128>
    {
        /// <summary>
        /// The 128-bit hash value, as four consecutive 32-bit unsigned integers.
        /// </summary>
        public uint4 Value;

        static readonly char[] k_HexToLiteral = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};

        /// <summary>
        /// Construct a hash from a 128-bit input value.
        /// </summary>
        /// <param name="value">Value to hash.</param>
        public Hash128(uint4 value) => Value = value;

        /// <summary>
        /// Construct a hash from four 32-bit input values.
        /// </summary>
        /// <param name="x">Value to hash.</param>
        /// <param name="y">Value to hash.</param>
        /// <param name="z">Value to hash.</param>
        /// <param name="w">Value to hash.</param>
        public Hash128(uint x, uint y, uint z, uint w) => Value = new uint4(x, y, z, w);

        /// <summary>
        /// Construct a hash from a 32 character hex string
        /// </summary>
        /// <remarks>
        /// If the given string has the incorrect length or contains non-hex characters the Value will be all 0
        /// </remarks>
        /// <param name="value">32 character hex string.</param>
        public unsafe Hash128(string value)
        {
            fixed(char* ptr = value)
            {
                Value = StringToHash(ptr, value.Length);
            }
        }

        /// <summary>
        /// Construct a hash from a 32 character hex string
        /// If the string has the incorrect length or non-hex characters the Value will be all 0
        /// </summary>
        /// <param name="value">32 character hex string.</param>
        /// <param name="guidFormatted">True, if the string value is formatted as a UnityEngine.GUID</param>
        public unsafe Hash128(string value, bool guidFormatted)
        {
            fixed (char* ptr = value)
            {
                Value = StringToHash(ptr, value.Length, guidFormatted);
            }
        }

        /// <summary>
        /// Convert a Hash128 to a 32-character UTF-16 string of hexadecimal symbols.
        /// </summary>
        /// <returns>Returns the 32-character UTF-16 string of hexadecimal symbols.</returns>
        public override unsafe string ToString()
        {
            var chars = stackalloc char[32];

            for (int i = 0; i < 4; i++)
            {
                for (int j = 7; j >= 0; j--)
                {
                    uint cur = Value[i];
                    cur >>= (j * 4);
                    cur &= 0xF;
                    chars[i * 8 + j] = k_HexToLiteral[cur];
                }
            }

            return new string(chars, 0, 32);
        }

        static readonly sbyte[] k_LiteralToHex =
        {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
            -1, -1, -1, -1, -1, -1, -1,
            10, 11, 12, 13, 14, 15,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            10, 11, 12, 13, 14, 15,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };

        const int k_GUIDStringLength = 32;

        static unsafe uint4 StringToHash(char* guidString, int length, bool guidFormatted = true)
        {
            if (length != k_GUIDStringLength)
                return default;

            // Convert every hex char into an int [0...16]
            var hex = stackalloc int[k_GUIDStringLength];
            for (int i = 0; i < k_GUIDStringLength; i++)
            {
                int intValue = guidString[i];
                if (intValue < 0 || intValue > 255)
                    return default;

                hex[i] = k_LiteralToHex[intValue];
            }

            uint4 value = default;
            if (guidFormatted)
            {
                for (int i = 0; i < 4; i++)
                {
                    uint cur = 0;
                    for (int j = 7; j >= 0; j--)
                    {
                        int curHex = hex[i * 8 + j];
                        if (curHex == -1)
                            return default;

                        cur |= (uint)(curHex << (j * 4));
                    }
                    value[i] = cur;
                }
            }
            else
            {
                int currentHex = 0;
                for (int i = 0; i < 4; ++i)
                {
                    uint currentInt = 0;
                    for (int j = 0; j < 4; ++j)
                    {
                        currentInt |= (uint)(hex[currentHex++] << j * 8 + 4);
                        currentInt |= (uint)(hex[currentHex++] << j * 8);
                    }
                    value[i] = currentInt;
                }
            }
            return value;
        }

        /// <summary>
        /// Compares two hashes for equality.
        /// </summary>
        /// <param name="obj1">The first hash to compare.</param>
        /// <param name="obj2">The second hash to compare.</param>
        /// <returns>Whether the two hashes are equal.</returns>
        public static bool operator==(Hash128 obj1, Hash128 obj2)
        {
            return obj1.Value.Equals(obj2.Value);
        }

        /// <summary>
        /// Compares two hashes for inequality.
        /// </summary>
        /// <param name="obj1">The first hash to compare.</param>
        /// <param name="obj2">The second hash to compare.</param>
        /// <returns>Whether the two hashes are unequal.</returns>
        public static bool operator!=(Hash128 obj1, Hash128 obj2)
        {
            return !obj1.Value.Equals(obj2.Value);
        }

        /// <summary>
        /// Determines whether a hash is equal to this hash.
        /// </summary>
        /// <param name="obj">The hash to compare with this hash.</param>
        /// <returns>Whether the two hashes are equal.</returns>
        public bool Equals(Hash128 obj)
        {
            return Value.Equals(obj.Value);
        }

        /// <summary>
        /// Determines whether some object is equal to this hash.
        /// </summary>
        /// <param name="obj">The object to compare with this one.</param>
        /// <returns>Whether the two hashes are equal.</returns>
        public override bool Equals(object obj)
        {
            return obj is Hash128 other && Equals(other);
        }

        /// <summary>
        /// Determines whether one hash's value is less than another hash's value
        /// </summary>
        /// <param name="a">The first hash to compare.</param>
        /// <param name="b">The second hash to compare.</param>
        /// <returns>Whether the first hash is less than the second hash, or not.</returns>
        public static bool operator<(Hash128 a, Hash128 b)
        {
            if (a.Value.w != b.Value.w)
                return a.Value.w < b.Value.w;
            if (a.Value.z != b.Value.z)
                return a.Value.z < b.Value.z;
            if (a.Value.y != b.Value.y)
                return a.Value.y < b.Value.y;
            return a.Value.x < b.Value.x;
        }

        /// <summary>
        /// Determines whether one hash's value is greater than another hash's value
        /// </summary>
        /// <param name="a">The first hash to compare.</param>
        /// <param name="b">The second hash to compare.</param>
        /// <returns>Whether the first hash is greater than the second hash, or not.</returns>
        public static bool operator>(Hash128 a, Hash128 b)
        {
            if (a.Value.w != b.Value.w)
                return a.Value.w > b.Value.w;
            if (a.Value.z != b.Value.z)
                return a.Value.z > b.Value.z;
            if (a.Value.y != b.Value.y)
                return a.Value.y > b.Value.y;
            return a.Value.x > b.Value.x;
        }

        /// <summary>
        /// Compares this hash's value to another hash's, and returns an integer that is negative
        /// if this hash's value is less, 0 if the same, or positive if more than the other hash.
        /// </summary>
        /// <param name="other">The hash to compare to this hash.</param>
        /// <returns>
        /// a negative number, if this hash's value is less than the other hash's.
        /// zero, if the hash's values are the same.
        /// a positive number, if this hash's value is more than the other hash's.
        /// </returns>
        public int CompareTo(Hash128 other)
        {
            if (Value.w != other.Value.w)
                return Value.w < other.Value.w ? -1 : 1;
            if (Value.z != other.Value.z)
                return Value.z < other.Value.z ? -1 : 1;
            if (Value.y != other.Value.y)
                return Value.y < other.Value.y ? -1 : 1;
            if (Value.x != other.Value.x)
                return Value.x < other.Value.x ? -1 : 1;
            return 0;
        }

        /// <summary>
        /// Computes a hashcode to support hash-based collections.
        /// </summary>
        /// <returns>The computed hash.</returns>
        // ReSharper disable once NonReadonlyMemberInGetHashCode (readonly fields will not get serialized by unity)
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// A Hash128 is valid, only if at least one of its 128 bits has value 1.
        /// </summary>
        /// <returns>
        /// True if the Hash128 is valid, and False if the Hash128 is invalid (is all 0 bits).
        /// </returns>
        public bool IsValid => !Value.Equals(uint4.zero);

        /// <summary>
        /// Implicitly convert a UnityEngine.Hash128 to a Hash128.
        /// </summary>
        /// <param name="guid">The UnityEngine.Hash128 to convert.</param>
        /// <returns>The corresponding Hash128.</returns>
        public static unsafe implicit operator Hash128(UnityEngine.Hash128 guid) => *(Hash128*)&guid;

        /// <summary>
        /// Implicitly convert a Hash128 to a UnityEngine.Hash128.
        /// </summary>
        /// <param name="guid">The Hash128 to convert.</param>
        /// <returns>The corresponding UnityEngine.Hash128.</returns>
        public static unsafe implicit operator UnityEngine.Hash128(Hash128 guid) => *(UnityEngine.Hash128*) & guid;
    }
}
