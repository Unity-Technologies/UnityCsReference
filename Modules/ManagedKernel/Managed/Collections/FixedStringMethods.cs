// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Collections
{
    /// <summary>
    /// Provides extension methods for FixedString*N*Bytes, NativeText, UnsafeText, and string,
    /// including appending, formatting, and parsing numbers.
    /// </summary>
    // This is the only part of this partial class that carries a summary. The doc tooling raises an
    // AmbiguousTriviaException when the parts disagree, so don't add one to the other parts.
    [GenerateTestsForBurstCompatibility]
    public unsafe static partial class FixedStringMethods
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckSubstringInRange(int strLength, int startIndex, int length)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException($"startIndex {startIndex} must be positive.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException($"length {length} cannot be negative.");
            }

            if (startIndex > strLength)
            {
                throw new ArgumentOutOfRangeException($"startIndex {startIndex} cannot be larger than string length {strLength}.");
            }
        }

        /// <summary>
        /// Retrieves a substring of this string. The substring starts from a specific character index, and has a specified length.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="str">A string to get the substring from.</param>
        /// <param name="startIndex">Start index of substring.</param>
        /// <param name="length">Length of substring.</param>
        /// <returns>A new string with length equivalent to `length` that begins at `startIndex`.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if startIndex or length parameter is negative, or if startIndex is larger than the string length.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T Substring<T>(ref this T str, int startIndex, int length)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            CheckSubstringInRange(str.Length, startIndex, length);
            length = math.min(length, str.Length - startIndex);

            var substr = new T();
            substr.Append(str.GetUnsafePtr() + startIndex, length);
            return substr;
        }

        /// <summary>
        /// Retrieves a substring of this string. The substring starts from a specific character index and continues to the end of the string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="str">A string to get the substring from.</param>
        /// <param name="startIndex">Start index of substring.</param>
        /// <returns>A new string that begins at `startIndex`.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T Substring<T>(ref this T str, int startIndex)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            return str.Substring(startIndex, str.Length - startIndex);
        }

        /// <summary>
        /// Retrieves a substring from this string. The substring starts from a specific character index, and has a specified length. Allocates memory to the new substring with the allocator specified.
        /// </summary>
        /// <param name="str">A <see cref="NativeText"/> string to get the substring from.</param>
        /// <param name="startIndex">Start index of substring.</param>
        /// <param name="length">Length of substring.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>A `NativeText` string with a length equivalent to `length` that starts at `startIndex` and an allocator type of `allocator`.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if startIndex or length parameter is negative, or if startIndex is larger than string length.</exception>
        public static NativeText Substring(ref this NativeText str, int startIndex, int length, AllocatorManager.AllocatorHandle allocator)
        {
            CheckSubstringInRange(str.Length, startIndex, length);
            length = math.min(length, str.Length - startIndex);

            var substr = new NativeText(length, allocator);
            substr.Append(str.GetUnsafePtr() + startIndex, length);
            return substr;
        }

        /// <summary>
        /// Retrieves a substring of this string. The substring starts from a specific character index and continues to the end of the string. Allocates memory to the new substring with the allocator specified.
        /// </summary>
        /// <param name="str">A <see cref="NativeText"/> string to get the substring from.</param>
        /// <param name="startIndex">Start index of substring.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>A NativeText string that begins at `startIndex` and has an allocator of type `allocator`.</returns>
        public static NativeText Substring(ref this NativeText str, int startIndex, AllocatorManager.AllocatorHandle allocator)
        {
            return str.Substring(startIndex, str.Length - startIndex);
        }

        /// <summary>
        /// Retrieves a substring of this string. The substring starts from a specific character index, and has a specified length. The new substring has the same allocator as the string.
        /// </summary>
        /// <param name="str">A <see cref="NativeText"/> string to get the substring from.</param>
        /// <param name="startIndex">Start index of substring.</param>
        /// <param name="length">Length of substring.</param>
        /// <returns>A NativeText string that has length equivalent to `length` and begins at `startIndex`.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if startIndex or length parameter is negative, or if startIndex is larger than string length.</exception>
        public static NativeText Substring(ref this NativeText str, int startIndex, int length)
        {
            return str.Substring(startIndex, length, str.m_Data->m_UntypedListData.Allocator);
        }

        /// <summary>
        /// Retrieves a substring of this string. The substring starts from a specific character index and continues to the end of the string. The new substring has the same allocator as the string.
        /// </summary>
        /// <param name="str">A <see cref="NativeText"/> to get the substring from.</param>
        /// <param name="startIndex">Start index of substring.</param>
        /// <returns>A NativeText string that begins at `startIndex`.</returns>
        public static NativeText Substring(ref this NativeText str, int startIndex)
        {
            return str.Substring(startIndex, str.Length - startIndex);
        }

        /// <summary>
        /// Returns the index of the first occurrence of a single Unicode rune in this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="rune">A single UTF-8 Unicode Rune to search for within this string.</param>
        /// <returns>The index of the first occurrence of the byte sequence in this string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int IndexOf<T>(ref this T fs, Unicode.Rune rune)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var dstLen = fs.Length;
            int index = 0;
            while(index < dstLen)
            {
                int tempIndex = index;
                var runeAtIndex = Read(ref fs, ref tempIndex);
                if (runeAtIndex.value == rune.value)
                {
                    return index;
                }
                index = tempIndex;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of a byte sequence in this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="bytes">A byte sequence to search for within this string.</param>
        /// <param name="bytesLen">The number of bytes in the byte sequence.</param>
        /// <returns>The index of the first occurrence of the byte sequence in this string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int IndexOf<T>(ref this T fs, byte* bytes, int bytesLen)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var dst = fs.GetUnsafePtr();
            var dstLen = fs.Length;
            for (var i = 0; i <= dstLen - bytesLen; ++i)
            {
                for (var j = 0; j < bytesLen; ++j)
                    if (dst[i + j] != bytes[j])
                        goto end_of_loop;
                return i;
                end_of_loop : {}
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of a byte sequence within a subrange of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="bytes">A byte sequence to search for within this string.</param>
        /// <param name="bytesLen">The number of bytes in the byte sequence.</param>
        /// <param name="startIndex">The first index in this string to consider as the first byte of the byte sequence.</param>
        /// <param name="distance">The last index in this string to consider as the first byte of the byte sequence.</param>
        /// <returns>The index of the first occurrence of the byte sequence in this string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int IndexOf<T>(ref this T fs, byte* bytes, int bytesLen, int startIndex, int distance = Int32.MaxValue)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var dst = fs.GetUnsafePtr();
            var dstLen = fs.Length;
            var searchrange = Math.Min(distance - 1, dstLen - bytesLen);
            for (var i = startIndex; i <= searchrange; ++i)
            {
                for (var j = 0; j < bytesLen; ++j)
                    if (dst[i + j] != bytes[j])
                        goto end_of_loop;
                return i;
                end_of_loop : {}
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of a substring within this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="T2">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="other">A substring to search for within this string.</param>
        /// <returns>The index of the first occurrence of the second string within this string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static int IndexOf<T,T2>(ref this T fs, in T2 other)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.IndexOf(oref.GetUnsafePtr(), oref.Length);
        }

        /// <summary>
        /// Returns the index of the first occurrence of a substring within a subrange of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="T2">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="other">A substring to search for within this string.</param>
        /// <param name="startIndex">The first index in this string to consider as an occurrence of the second string.</param>
        /// <param name="distance">The last index in this string to consider as an occurrence of the second string.</param>
        /// <returns>The index of the first occurrence of the substring within this string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static int IndexOf<T,T2>(ref this T fs, in T2 other, int startIndex, int distance = Int32.MaxValue)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.IndexOf(oref.GetUnsafePtr(), oref.Length, startIndex, distance);
        }

        /// <summary>
        /// Returns true if a given substring occurs within this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="T2">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="other">A substring to search for within this string.</param>
        /// <returns>True if the substring occurs within this string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static bool Contains<T,T2>(ref this T fs, in T2 other)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            return fs.IndexOf(in other) != -1;
        }

        /// <summary>
        /// Returns the index of the last occurrence of a single Unicode rune within this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="rune">A single Unicode.Rune to search for within this string.</param>
        /// <returns>The index of the last occurrence of the byte sequence within this string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int LastIndexOf<T>(ref this T fs, Unicode.Rune rune)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            if (Unicode.IsValidCodePoint(rune.value))
            {
                var dstLen = fs.Length;
                for (var i = dstLen - 1; i >= 0; --i)
                {
                    var runeAtIndex = Peek(ref fs, i);
                    if (Unicode.IsValidCodePoint(runeAtIndex.value) && runeAtIndex.value == rune.value)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the last occurrence of a byte sequence within this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="bytes">A byte sequence to search for within this string.</param>
        /// <param name="bytesLen">The number of bytes in the byte sequence.</param>
        /// <returns>The index of the last occurrence of the byte sequence within this string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int LastIndexOf<T>(ref this T fs, byte* bytes, int bytesLen)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var dst = fs.GetUnsafePtr();
            var dstLen = fs.Length;
            for (var i = dstLen - bytesLen; i >= 0; --i)
            {
                for (var j = 0; j < bytesLen; ++j)
                    if (dst[i + j] != bytes[j])
                        goto end_of_loop;
                return i;
                end_of_loop : {}
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the last occurrence of a byte sequence within a subrange of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="bytes">A byte sequence to search for within this string.</param>
        /// <param name="bytesLen">The number of bytes in the byte sequence.</param>
        /// <param name="startIndex">The smallest index in this string to consider as the first byte of the byte sequence.</param>
        /// <param name="distance">The greatest index in this string to consider as the first byte of the byte sequence.</param>
        /// <returns>The index of the last occurrence of the byte sequence within this string. Returns -1 if no occurrences found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int LastIndexOf<T>(ref this T fs, byte* bytes, int bytesLen, int startIndex, int distance = int.MaxValue)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var dst = fs.GetUnsafePtr();
            var dstLen = fs.Length;
            startIndex = Math.Min(dstLen - bytesLen, startIndex);
            var searchrange = Math.Max(0, startIndex - distance);
            for (var i = startIndex; i >= searchrange; --i)
            {
                for (var j = 0; j < bytesLen; ++j)
                    if (dst[i + j] != bytes[j])
                        goto end_of_loop;
                return i;
                end_of_loop : {}
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the last occurrence of a substring within this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="T2">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="other">A substring to search for in the this string.</param>
        /// <returns>The index of the last occurrence of the substring within this string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static int LastIndexOf<T,T2>(ref this T fs, in T2 other)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.LastIndexOf(oref.GetUnsafePtr(), oref.Length);
        }

        /// <summary>
        /// Returns the index of the last occurrence of a substring within a subrange of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="T2">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="other">A substring to search for within this string.</param>
        /// <param name="startIndex">The greatest index in this string to consider as an occurrence of the substring.</param>
        /// <param name="distance">The smallest index in this string to consider as an occurrence of the substring.</param>
        /// <returns>the index of the last occurrence of the substring within the first string. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static int LastIndexOf<T,T2>(ref this T fs, in T2 other, int startIndex, int distance = Int32.MaxValue)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.LastIndexOf(oref.GetUnsafePtr(), oref.Length, startIndex, distance);
        }

        /// <summary>
        /// Returns the sort position of this string relative to a byte sequence.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to compare.</param>
        /// <param name="bytes">A byte sequence to compare.</param>
        /// <param name="bytesLen">The number of bytes in the byte sequence.</param>
        /// <returns>A number denoting the sort position of this string relative to the byte sequence:
        ///
        /// 0 denotes that this string and byte sequence have the same sort position.<br/>
        /// -1 denotes that this string should be sorted to precede the byte sequence.<br/>
        /// +1 denotes that this string should be sorted to follow the byte sequence.<br/>
        /// </returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int CompareTo<T>(ref this T fs, byte* bytes, int bytesLen)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var a = fs.GetUnsafePtr();
            var aa = fs.Length;
            int chars = aa < bytesLen ? aa : bytesLen;
            for (var i = 0; i < chars; ++i)
            {
                if (a[i] < bytes[i])
                    return -1;
                if (a[i] > bytes[i])
                    return 1;
            }
            if (aa < bytesLen)
                return -1;
            if (aa > bytesLen)
                return 1;
            return 0;
        }

        /// <summary>
        /// Returns the sort position of this string relative to another.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="T2">A string type.</typeparam>
        /// <param name="fs">A string to compare.</param>
        /// <param name="other">Another string to compare.</param>
        /// <returns>A number denoting the relative sort position of the strings:
        ///
        /// 0 denotes that the strings have the same sort position.<br/>
        /// -1 denotes that this string should be sorted to precede the other.<br/>
        /// +1 denotes that this first string should be sorted to follow the other.<br/>
        /// </returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static int CompareTo<T,T2>(ref this T fs, in T2 other)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.CompareTo(oref.GetUnsafePtr(), oref.Length);
        }

        /// <summary>
        /// Returns true if this string and a byte sequence are equal (meaning they have the same length and content).
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to compare for equality.</param>
        /// <param name="bytes">A sequence of bytes to compare for equality.</param>
        /// <param name="bytesLen">The number of bytes in the byte sequence.</param>
        /// <returns>True if this string and the byte sequence have the same length and if this string's character bytes match the byte sequence.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static bool Equals<T>(ref this T fs, byte* bytes, int bytesLen)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var a = fs.GetUnsafePtr();
            var aa = fs.Length;
            if (aa != bytesLen)
                return false;
            if (a == bytes)
                return true;
            return fs.CompareTo(bytes, bytesLen) == 0;
        }

        /// <summary>
        /// Returns true if this string is equal to another.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="T2">A string type.</typeparam>
        /// <param name="fs">A string to compare for equality.</param>
        /// <param name="other">Another string to compare for equality.</param>
        /// <returns>true if the two strings have the same length and matching content.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static bool Equals<T,T2>(ref this T fs, in T2 other)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref var oref = ref UnsafeUtilityExtensions.AsRef(in other);
            return fs.Equals(oref.GetUnsafePtr(), oref.Length);
        }

        /// <summary>
        /// Returns the Unicode.Rune at an index of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to read.</param>
        /// <param name="index">A reference to an index in bytes (not characters).</param>
        /// <returns>The Unicode.Rune (character) which starts at the byte index. Returns Unicode.BadRune
        /// if the byte(s) at the index do not form a valid UTF-8 encoded character.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static Unicode.Rune Peek<T>(ref this T fs, int index)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            if (index >= fs.Length)
                return Unicode.BadRune;
            Unicode.Utf8ToUcs(out var rune, fs.GetUnsafePtr(), ref index, fs.Capacity);
            return rune;
        }

        /// <summary>
        /// Returns the Unicode.Rune at an index of this string. Increments the index to the position of the next character.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to read.</param>
        /// <param name="index">A reference to an index in bytes (not characters). Incremented by 1 to 4 depending upon the UTF-8 encoded size of the character read.</param>
        /// <returns>The character (as a `Unicode.Rune`) which starts at the byte index. Returns `Unicode.BadRune`
        /// if the byte(s) at the index do not form a valid UTF-8 encoded character.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static Unicode.Rune Read<T>(ref this T fs, ref int index)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            if (index >= fs.Length)
                return Unicode.BadRune;
            Unicode.Utf8ToUcs(out var rune, fs.GetUnsafePtr(), ref index, fs.Capacity);
            return rune;
        }

        /// <summary>
        /// Writes a Unicode.Rune at an index of this string. Increments the index to the position of the next character.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to modify.</param>
        /// <param name="index">A reference to an index in bytes (not characters). Incremented by 1 to 4 depending upon the UTF-8 encoded size of the character written.</param>
        /// <param name="rune">A rune to write to the string, encoded as UTF-8.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the index is invalid or if there is not enough space to store the encoded rune.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Write<T>(ref this T fs, ref int index, Unicode.Rune rune)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var err = Unicode.UcsToUtf8(fs.GetUnsafePtr(), ref index, fs.Capacity, rune);
            if (err != ConversionError.None)
                return FormatError.Overflow;
            return FormatError.None;
        }

        /// <summary>
        /// Returns a copy of this string as a managed string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to copy.</param>
        /// <returns>A copy of this string as a managed string.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed string")]
        public static String ConvertToString<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var c = stackalloc char[fs.Length * 2];
            int length = 0;
            Unicode.Utf8ToUtf16(fs.GetUnsafePtr(), fs.Length, c, out length, fs.Length * 2);
            return new String(c, 0, length);
        }

        /// <summary>
        /// Returns a hash code of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to get a hash code of.</param>
        /// <returns>A hash code of this string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int ComputeHashCode<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            return (int)CollectionHelper.Hash(fs.GetUnsafePtr(), fs.Length);
        }

        /// <summary>
        /// Returns the effective size in bytes of this string.
        /// </summary>
        /// <remarks>
        /// "Effective size" is `Length + 3`, the number of bytes you need to copy when serializing the string.
        /// (The plus 3 accounts for the null-terminator byte and the 2 bytes that store the Length).
        ///
        /// Useful for checking whether this string will fit in the space of a smaller string.
        /// </remarks>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to get the effective size of.</param>
        /// <returns>The effective size in bytes of this string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static int EffectiveSizeOf<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            return sizeof(ushort) + fs.Length + 1;
        }

        /// <summary>
        /// Returns true if a given character occurs at the beginning of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="rune">A character to search for within this string.</param>
        /// <returns>True if the character occurs at the beginning of this string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static bool StartsWith<T>(ref this T fs, Unicode.Rune rune)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var len = rune.LengthInUtf8Bytes();
            return fs.Length >= len
                && 0 == UTF8ArrayUnsafeUtility.StrCmp(fs.GetUnsafePtr(), len, &rune, 1)
                ;
        }

        /// <summary>
        /// Returns true if a given substring occurs at the beginning of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="U">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="other">A substring to search for within this string.</param>
        /// <returns>True if the substring occurs at the beginning of this string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static bool StartsWith<T, U>(ref this T fs, in U other)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where U : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var len = other.Length;
            return fs.Length >= len
                && 0 == UTF8ArrayUnsafeUtility.StrCmp(fs.GetUnsafePtr(), len, other.GetUnsafePtr(), len)
                ;
        }

        /// <summary>
        /// Returns true if a given character occurs at the end of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="rune">A character to search for within this string.</param>
        /// <returns>True if the character occurs at the end of this string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static bool EndsWith<T>(ref this T fs, Unicode.Rune rune)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var len = rune.LengthInUtf8Bytes();
            return fs.Length >= len
                && 0 == UTF8ArrayUnsafeUtility.StrCmp(fs.GetUnsafePtr() + fs.Length - len, len, &rune, 1)
                ;
        }

        /// <summary>
        /// Returns true if a given substring occurs at the end of this string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <typeparam name="U">A string type.</typeparam>
        /// <param name="fs">A string to search.</param>
        /// <param name="other">A substring to search for within this string.</param>
        /// <returns>True if the substring occurs at the end of this string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static bool EndsWith<T, U>(ref this T fs, in U other)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where U : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var len = other.Length;
            return fs.Length >= len
                && 0 == UTF8ArrayUnsafeUtility.StrCmp(fs.GetUnsafePtr() + fs.Length - len, len, other.GetUnsafePtr(), len)
                ;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        internal static int TrimStartIndex<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            int index = 0;
            while (true)
            {
                var prev = index;
                var error = Unicode.Utf8ToUcs(out var rune, ptr, ref index, lengthInBytes);
                if (error != ConversionError.None
                || !rune.IsWhiteSpace())
                {
                    index -= index - prev;
                    break;
                }
            }

            return index;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        internal static int TrimStartIndex<T>(ref this T fs, ReadOnlySpan<Unicode.Rune> trimRunes)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            int index = 0;
            while (true)
            {
                var prev = index;
                var error = Unicode.Utf8ToUcs(out var rune, ptr, ref index, lengthInBytes);

                var doTrim = false;
                for (int i = 0, num = trimRunes.Length; i < num && !doTrim; i++)
                {
                    doTrim |= trimRunes[i] == rune;
                }

                if (error != ConversionError.None
                || !doTrim)
                {
                    index -= index - prev;
                    break;
                }
            }

            return index;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        internal static int TrimEndIndex<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            int index = lengthInBytes;
            while (true)
            {
                var prev = index;
                var error = Unicode.Utf8ToUcsReverse(out var rune, ptr, ref index, lengthInBytes);
                if (error != ConversionError.None
                || !rune.IsWhiteSpace())
                {
                    index += prev - index;
                    break;
                }
            }

            return index;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        internal static int TrimEndIndex<T>(ref this T fs, ReadOnlySpan<Unicode.Rune> trimRunes)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            int index = lengthInBytes;
            while (true)
            {
                var prev = index;
                var error = Unicode.Utf8ToUcsReverse(out var rune, ptr, ref index, lengthInBytes);

                var doTrim = false;
                for (int i = 0, num = trimRunes.Length; i < num && !doTrim; i++)
                {
                    doTrim |= trimRunes[i] == rune;
                }

                if (error != ConversionError.None
                || !doTrim)
                {
                    index += prev - index;
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Removes whitespace characters from begining of the string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to perform operation.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the start of the string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T TrimStart<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var index = fs.TrimStartIndex();
            var result = new T();
            result.Append(fs.GetUnsafePtr() + index, fs.Length - index);

            return result;
        }

        /// <summary>
        /// Removes whitespace characters from begining of the string.
        /// </summary>
        /// <param name="fs">A <see cref="UnsafeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the start of the string.</returns>
        public static UnsafeText TrimStart(ref this UnsafeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var index = fs.TrimStartIndex();
            var lengthInBytes = fs.Length - index;
            var result = new UnsafeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr() + index, lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes whitespace characters from begining of the string.
        /// </summary>
        /// <param name="fs">A <see cref="NativeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the start of the string.</returns>
        public static NativeText TrimStart(ref this NativeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var index = fs.TrimStartIndex();
            var lengthInBytes = fs.Length - index;
            var result = new NativeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr() + index, lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes specific characters from begining of the string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to perform operation.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the start of the string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T TrimStart<T>(ref this T fs, ReadOnlySpan<Unicode.Rune> trimRunes)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var index = fs.TrimStartIndex(trimRunes);
            var result = new T();
            result.Append(fs.GetUnsafePtr() + index, fs.Length - index);

            return result;
        }

        /// <summary>
        /// Removes specific characters characters from begining of the string.
        /// </summary>
        /// <param name="fs">A <see cref="UnsafeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the start of the string.</returns>
        public static UnsafeText TrimStart(ref this UnsafeText fs, AllocatorManager.AllocatorHandle allocator, ReadOnlySpan<Unicode.Rune> trimRunes)
        {
            var index = fs.TrimStartIndex(trimRunes);
            var lengthInBytes = fs.Length - index;
            var result = new UnsafeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr() + index, lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes specific characters from begining of the string.
        /// </summary>
        /// <param name="fs">A <see cref="NativeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the start of the string.</returns>
        public static NativeText TrimStart(ref this NativeText fs, AllocatorManager.AllocatorHandle allocator, ReadOnlySpan<Unicode.Rune> trimRunes)
        {
            var index = fs.TrimStartIndex(trimRunes);
            var lengthInBytes = fs.Length - index;
            var result = new NativeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr() + index, lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes whitespace characters from the end of the string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to perform operation.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the end of the string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T TrimEnd<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var index = fs.TrimEndIndex();
            var result = new T();
            result.Append(fs.GetUnsafePtr(), index);

            return result;
        }

        /// <summary>
        /// Removes whitespace characters from the end of the string.
        /// </summary>
        /// <param name="fs">A <see cref="UnsafeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the end of the string.</returns>
        public static UnsafeText TrimEnd(ref this UnsafeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var index = fs.TrimEndIndex();
            var lengthInBytes = index;
            var result = new UnsafeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr(), lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes whitespace characters from the end of the string.
        /// </summary>
        /// <param name="fs">A <see cref="NativeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the end of the string.</returns>
        public static NativeText TrimEnd(ref this NativeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var index = fs.TrimEndIndex();
            var lengthInBytes = index;
            var result = new NativeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr(), lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes specific characters from the end of the string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to perform operation.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the end of the string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T TrimEnd<T>(ref this T fs, ReadOnlySpan<Unicode.Rune> trimRunes)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var index = fs.TrimEndIndex(trimRunes);
            var result = new T();
            result.Append(fs.GetUnsafePtr(), index);

            return result;
        }

        /// <summary>
        /// Removes specific characters from the end of the string.
        /// </summary>
        /// <param name="fs">A <see cref="UnsafeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the end of the string.</returns>
        public static UnsafeText TrimEnd(ref this UnsafeText fs, AllocatorManager.AllocatorHandle allocator, ReadOnlySpan<Unicode.Rune> trimRunes)
        {
            var index = fs.TrimEndIndex(trimRunes);
            var lengthInBytes = index;
            var result = new UnsafeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr(), lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes specific characters from the end of the string.
        /// </summary>
        /// <param name="fs">A <see cref="NativeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the end of the string.</returns>
        public static NativeText TrimEnd(ref this NativeText fs, AllocatorManager.AllocatorHandle allocator, ReadOnlySpan<Unicode.Rune> trimRunes)
        {
            var index = fs.TrimEndIndex(trimRunes);
            var lengthInBytes = index;
            var result = new NativeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr(), lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes whitespace characters from the begining and the end of the string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to perform operation.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the begining and the end of the string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T Trim<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var start = fs.TrimStartIndex();
            if (start == fs.Length)
            {
                return new T();
            }

            var end = fs.TrimEndIndex();
            var result = new T();
            result.Append(fs.GetUnsafePtr() + start, end - start);

            return result;
        }

        /// <summary>
        /// Removes whitespace characters from the begining and the end of the string.
        /// </summary>
        /// <param name="fs">A <see cref="UnsafeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the begining and the end of the string.</returns>
        public static UnsafeText Trim(ref this UnsafeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var start = fs.TrimStartIndex();
            if (start == fs.Length)
            {
                return new UnsafeText(0, allocator);
            }

            var end = fs.TrimEndIndex();
            var lengthInBytes = end - start;
            var result = new UnsafeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr() + start, lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes whitespace characters from the begining and the end of the string.
        /// </summary>
        /// <param name="fs">A <see cref="NativeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns instance of this string with whitespace characters removed from the begining and the end of the string.</returns>
        public static NativeText Trim(ref this NativeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var start = fs.TrimStartIndex();
            if (start == fs.Length)
            {
                return new NativeText(0, allocator);
            }

            var end = fs.TrimEndIndex();
            var lengthInBytes = end - start;
            var result = new NativeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr() + start, lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes specific characters from the begining and the end of the string.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to perform operation.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the begining and the end of the string.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T Trim<T>(ref this T fs, ReadOnlySpan<Unicode.Rune> trimRunes)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var start = fs.TrimStartIndex(trimRunes);
            if (start == fs.Length)
            {
                return new T();
            }

            var end = fs.TrimEndIndex(trimRunes);
            var result = new T();
            result.Append(fs.GetUnsafePtr() + start, end - start);

            return result;
        }

        /// <summary>
        /// Removes specific characters from the begining and the end of the string.
        /// </summary>
        /// <param name="fs">A <see cref="UnsafeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the begining and the end of the string.</returns>
        public static UnsafeText Trim(ref this UnsafeText fs, AllocatorManager.AllocatorHandle allocator, ReadOnlySpan<Unicode.Rune> trimRunes)
        {
            var start = fs.TrimStartIndex(trimRunes);
            if (start == fs.Length)
            {
                return new UnsafeText(0, allocator);
            }

            var end = fs.TrimEndIndex();
            var lengthInBytes = end - start;
            var result = new UnsafeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr() + start, lengthInBytes);

            return result;
        }

        /// <summary>
        /// Removes specific characters from the begining and the end of the string.
        /// </summary>
        /// <param name="fs">A <see cref="NativeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <param name="trimRunes">Runes that should be trimmed.</param>
        /// <returns>Returns instance of this string with specific characters removed from the begining and the end of the string.</returns>
        public static NativeText Trim(ref this NativeText fs, AllocatorManager.AllocatorHandle allocator, ReadOnlySpan<Unicode.Rune> trimRunes)
        {
            var start = fs.TrimStartIndex(trimRunes);
            if (start == fs.Length)
            {
                return new NativeText(0, allocator);
            }

            var end = fs.TrimEndIndex();
            var lengthInBytes = end - start;
            var result = new NativeText(lengthInBytes, allocator);
            result.Append(fs.GetUnsafePtr() + start, lengthInBytes);

            return result;
        }

        /// <summary>
        /// Converts string to lowercase only ASCII characters.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to perform operation.</param>
        /// <returns>Returns a copy of this string converted to lowercase ASCII.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T ToLowerAscii<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            T result = new T();

            Unicode.Rune rune;
            var error = ConversionError.None;
            for (var i = 0; i < lengthInBytes && error == ConversionError.None;)
            {
                error = Unicode.Utf8ToUcs(out rune, ptr, ref i, lengthInBytes);
                result.Append(rune.ToLowerAscii());
            }

            return result;
        }

        /// <summary>
        /// Converts string to lowercase only ASCII characters.
        /// </summary>
        /// <param name="fs">A <see cref="UnsafeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns a copy of this string converted to lowercase ASCII.</returns>
        public static UnsafeText ToLowerAscii(ref this UnsafeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            var result = new UnsafeText(lengthInBytes, allocator);

            Unicode.Rune rune;
            var error = ConversionError.None;
            for (var i = 0; i < lengthInBytes && error == ConversionError.None;)
            {
                error = Unicode.Utf8ToUcs(out rune, ptr, ref i, lengthInBytes);
                result.Append(rune.ToLowerAscii());
            }

            return result;
        }

        /// <summary>
        /// Converts string to lowercase only ASCII characters.
        /// </summary>
        /// <param name="fs">A <see cref="NativeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns a copy of this string converted to lowercase ASCII.</returns>
        public static NativeText ToLowerAscii(ref this NativeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            var result = new NativeText(lengthInBytes, allocator);

            Unicode.Rune rune;
            var error = ConversionError.None;
            for (var i = 0; i < lengthInBytes && error == ConversionError.None;)
            {
                error = Unicode.Utf8ToUcs(out rune, ptr, ref i, lengthInBytes);
                result.Append(rune.ToLowerAscii());
            }

            return result;
        }

        /// <summary>
        /// Converts string to uppercase only ASCII characters.
        /// </summary>
        /// <typeparam name="T">A string type.</typeparam>
        /// <param name="fs">A string to perform operation.</param>
        /// <returns>Returns a copy of this string converted to uppercase ASCII.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static T ToUpperAscii<T>(ref this T fs)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            T result = new T();

            Unicode.Rune rune;
            var error = ConversionError.None;
            for (var i = 0; i < lengthInBytes && error == ConversionError.None;)
            {
                error = Unicode.Utf8ToUcs(out rune, ptr, ref i, lengthInBytes);
                result.Append(rune.ToUpperAscii());
            }

            return result;
        }

        /// <summary>
        /// Converts string to uppercase only ASCII characters.
        /// </summary>
        /// <param name="fs">A <see cref="UnsafeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns a copy of this string converted to uppercase ASCII.</returns>
        public static UnsafeText ToUpperAscii(ref this UnsafeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            var result = new UnsafeText(lengthInBytes, allocator);

            Unicode.Rune rune;
            var error = ConversionError.None;
            for (var i = 0; i < lengthInBytes && error == ConversionError.None;)
            {
                error = Unicode.Utf8ToUcs(out rune, ptr, ref i, lengthInBytes);
                result.Append(rune.ToUpperAscii());
            }

            return result;
        }

        /// <summary>
        /// Converts string to uppercase only ASCII characters.
        /// </summary>
        /// <param name="fs">A <see cref="NativeText"/> string to perform operation.</param>
        /// <param name="allocator">The <see cref="AllocatorManager.AllocatorHandle"/> allocator type to use.</param>
        /// <returns>Returns a copy of this string converted to uppercase ASCII.</returns>
        public static NativeText ToUpperAscii(ref this NativeText fs, AllocatorManager.AllocatorHandle allocator)
        {
            var lengthInBytes = fs.Length;
            var ptr = fs.GetUnsafePtr();

            var result = new NativeText(lengthInBytes, allocator);

            Unicode.Rune rune;
            var error = ConversionError.None;
            for (var i = 0; i < lengthInBytes && error == ConversionError.None;)
            {
                error = Unicode.Utf8ToUcs(out rune, ptr, ref i, lengthInBytes);
                result.Append(rune.ToUpperAscii());
            }

            return result;
        }
    }
}
