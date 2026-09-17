// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    // The summary for this partial class lives in FixedStringMethods.cs - don't add one here.
    [GenerateTestsForBurstCompatibility]
    public unsafe static partial class FixedStringMethods
    {
        /// <summary>
        /// Appends a Unicode.Rune to this string.
        /// </summary>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="rune">A Unicode.Rune to append.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Append<T>(ref this T fs, Unicode.Rune rune)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var len = fs.Length;
            var runeLen = rune.LengthInUtf8Bytes();
            if (!fs.TryResize(len + runeLen, NativeArrayOptions.UninitializedMemory))
                return FormatError.Overflow;
            return fs.Write(ref len, rune);
        }

        /// <summary>
        /// Appends a char to this string.
        /// </summary>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="ch">A char to append.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Append<T>(ref this T fs, char ch)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            return fs.Append((Unicode.Rune) ch);
        }

        /// <summary>
        /// Appends a byte to this string.
        /// </summary>
        /// <remarks>
        /// No validation is performed: it is your responsibility for the data to be valid UTF-8 when you're done appending bytes.
        /// </remarks>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="a">A byte to append.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError AppendRawByte<T>(ref this T fs, byte a)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var origLength = fs.Length;
            if (!fs.TryResize(origLength + 1, NativeArrayOptions.UninitializedMemory))
                return FormatError.Overflow;
            fs.GetUnsafePtr()[origLength] = a;
            return FormatError.None;
        }

        /// <summary>
        /// Appends a Unicode.Rune a number of times to this string.
        /// </summary>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="rune">A Unicode.Rune to append some number of times.</param>
        /// <param name="count">The number of times to append the rune.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Append<T>(ref this T fs, Unicode.Rune rune, int count)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var origLength = fs.Length;

            if (!fs.TryResize(origLength + rune.LengthInUtf8Bytes() * count, NativeArrayOptions.UninitializedMemory))
                return FormatError.Overflow;

            var cap = fs.Capacity;
            var b = fs.GetUnsafePtr();
            int offset = origLength;
            for (int i = 0; i < count; ++i)
            {
                var error = Unicode.UcsToUtf8(b, ref offset, cap, rune);
                if (error != ConversionError.None)
                    return FormatError.Overflow;
            }

            return FormatError.None;
        }

        /// <summary>
        /// Appends a number (converted to UTF-8 characters) to this string.
        /// </summary>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="input">A long integer to append to the string.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Append<T>(ref this T fs, long input)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            const int maximumDigits = 20;
            var temp = stackalloc byte[maximumDigits];
            int offset = maximumDigits;
            if (input >= 0)
            {
                do
                {
                    var digit = (byte)(input % 10);
                    temp[--offset] = (byte)('0' + digit);
                    input /= 10;
                }
                while (input != 0);
            }
            else
            {
                do
                {
                    var digit = (byte)(input % 10);
                    temp[--offset] = (byte)('0' - digit);
                    input /= 10;
                }
                while (input != 0);
                temp[--offset] = (byte)'-';
            }

            return fs.Append(temp + offset, maximumDigits - offset);
        }

        /// <summary>
        /// Appends a number (converted to UTF-8 characters) to this string.
        /// </summary>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="input">An int to append to the string.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Append<T>(ref this T fs, int input)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            return fs.Append((long)input);
        }

        /// <summary>
        /// Appends a number (converted to UTF-8 characters) to this string.
        /// </summary>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="input">A ulong integer to append to the string.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Append<T>(ref this T fs, ulong input)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            const int maximumDigits = 20;
            var temp = stackalloc byte[maximumDigits];
            int offset = maximumDigits;
            do
            {
                var digit = (byte)(input % 10);
                temp[--offset] = (byte)('0' + digit);
                input /= 10;
            }
            while (input != 0);

            return fs.Append(temp + offset, maximumDigits - offset);
        }

        /// <summary>
        /// Appends a number (converted to UTF-8 characters) to this string.
        /// </summary>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="input">A uint to append to the string.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Append<T>(ref this T fs, uint input)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            return fs.Append((ulong)input);
        }

        /// <summary>
        /// Appends a number (converted to UTF-8 characters) to this string.
        /// </summary>
        /// <typeparam name="T">The type of FixedString*N*Bytes.</typeparam>
        /// <param name="fs">A FixedString*N*Bytes.</param>
        /// <param name="input">A float to append to the string.</param>
        /// <param name="decimalSeparator">The character to use as the decimal separator. Defaults to a period ('.').</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static FormatError Append<T>(ref this T fs, float input, char decimalSeparator = '.')
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            FixedStringUtils.UintFloatUnion ufu = new FixedStringUtils.UintFloatUnion();
            ufu.floatValue = input;
            var sign = ufu.uintValue >> 31;
            ufu.uintValue &= ~(1 << 31);
            FormatError error;
            if ((ufu.uintValue & 0x7F800000) == 0x7F800000)
            {
                if (ufu.uintValue == 0x7F800000)
                {
                    if (sign != 0 && ((error = fs.Append('-')) != FormatError.None))
                        return error;
                    return fs.Append('I', 'n', 'f', 'i', 'n', 'i', 't', 'y');
                }
                return fs.Append('N', 'a', 'N');
            }
            if (sign != 0 && ufu.uintValue != 0) // Non-CoreCLR C# prints -0 as 0
                if ((error = fs.Append('-')) != FormatError.None)
                    return error;
            ulong decimalMantissa = 0;
            int decimalExponent = 0;
            FixedStringUtils.Base2ToBase10(ref decimalMantissa, ref decimalExponent, ufu.floatValue);
            var backwards = stackalloc char[9];
            int decimalDigits = 0;
            do
            {
                if (decimalDigits >= 9)
                    return FormatError.Overflow;
                var decimalDigit = decimalMantissa % 10;
                backwards[8 - decimalDigits++] = (char)('0' + decimalDigit);
                decimalMantissa /= 10;
            }
            while (decimalMantissa > 0);
            char *ascii = backwards + 9 - decimalDigits;
            var leadingZeroes = -decimalExponent - decimalDigits + 1;
            if (leadingZeroes > 0)
            {
                if (leadingZeroes > 4)
                    return fs.AppendScientific(ascii, decimalDigits, decimalExponent, decimalSeparator);
                if ((error = fs.Append('0', decimalSeparator)) != FormatError.None)
                    return error;
                --leadingZeroes;
                while (leadingZeroes > 0)
                {
                    if ((error = fs.Append('0')) != FormatError.None)
                        return error;
                    --leadingZeroes;
                }
                for (var i = 0; i < decimalDigits; ++i)
                {
                    if ((error = fs.Append(ascii[i])) != FormatError.None)
                        return error;
                }
                return FormatError.None;
            }
            var trailingZeroes = decimalExponent;
            if (trailingZeroes > 0)
            {
                if (trailingZeroes > 4)
                    return fs.AppendScientific(ascii, decimalDigits, decimalExponent, decimalSeparator);
                for (var i = 0; i < decimalDigits; ++i)
                {
                    if ((error = fs.Append(ascii[i])) != FormatError.None)
                        return error;
                }
                while (trailingZeroes > 0)
                {
                    if ((error = fs.Append('0')) != FormatError.None)
                        return error;
                    --trailingZeroes;
                }
                return FormatError.None;
            }
            var indexOfSeparator = decimalDigits + decimalExponent;
            for (var i = 0; i < decimalDigits; ++i)
            {
                if (i == indexOfSeparator)
                    if ((error = fs.Append(decimalSeparator)) != FormatError.None)
                        return error;
                if ((error = fs.Append(ascii[i])) != FormatError.None)
                    return error;
            }
            return FormatError.None;
        }

        /// <summary>
        /// Appends another string to this string.
        /// </summary>
        /// <remarks>
        /// When the method returns an error, the destination string is not modified.
        /// </remarks>
        /// <typeparam name="T">The type of the destination string.</typeparam>
        /// <typeparam name="T2">The type of the source string.</typeparam>
        /// <param name="fs">The destination string.</param>
        /// <param name="input">The source string.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the destination string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static FormatError Append<T,T2>(ref this T fs, in T2 input)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref var inputRef = ref UnsafeUtilityExtensions.AsRef(input);
            return fs.Append(inputRef.GetUnsafePtr(), inputRef.Length);
        }

        /// <summary>
        /// Copies another string to this string (making the two strings equal).
        /// </summary>
        /// <remarks>
        /// When the method returns an error, the destination string is not modified.
        /// </remarks>
        /// <typeparam name="T">The type of the destination string.</typeparam>
        /// <typeparam name="T2">The type of the source string.</typeparam>
        /// <param name="fs">The destination string.</param>
        /// <param name="input">The source string.</param>
        /// <returns>CopyError.None if successful. Returns CopyError.Truncation if the source string is too large to fit in the destination.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static CopyError CopyFrom<T, T2>(ref this T fs, in T2 input)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            var fe = Append(ref fs, input);
            if (fe != FormatError.None)
                return CopyError.Truncation;
            return CopyError.None;
        }

        /// <summary>
        /// Appends bytes to this string.
        /// </summary>
        /// <remarks>
        /// When the method returns an error, the destination string is not modified.
        ///
        /// No validation is performed: it is your responsibility for the destination to contain valid UTF-8 when you're done appending bytes.
        /// </remarks>
        /// <typeparam name="T">The type of the destination string.</typeparam>
        /// <param name="fs">The destination string.</param>
        /// <param name="utf8Bytes">The bytes to append.</param>
        /// <param name="utf8BytesLength">The number of bytes to append.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the destination string is exceeded.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public unsafe static FormatError Append<T>(ref this T fs, byte* utf8Bytes, int utf8BytesLength)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var origLength = fs.Length;
            if (!fs.TryResize(origLength + utf8BytesLength, NativeArrayOptions.UninitializedMemory))
                return FormatError.Overflow;
            UnsafeUtility.MemCpy(fs.GetUnsafePtr() + origLength, utf8Bytes, utf8BytesLength);
            return FormatError.None;
        }

        /// <summary>
        /// Appends another string to this string.
        /// </summary>
        /// <remarks>
        /// When the method returns an error, the destination string is not modified.
        /// </remarks>
        /// <typeparam name="T">The type of the destination string.</typeparam>
        /// <param name="fs">The destination string.</param>
        /// <param name="s">The string to append.</param>
        /// <returns>FormatError.None if successful. Returns FormatError.Overflow if the capacity of the destination string is exceeded.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public unsafe static FormatError Append<T>(ref this T fs, string s)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            // we don't know how big the expansion from UTF16 to UTF8 will be, so we account for worst case.
            int worstCaseCapacity = s.Length * 4;
            byte* utf8Bytes = stackalloc byte[worstCaseCapacity];
            int utf8Len;

            fixed (char* chars = s)
            {
                var err = UTF8ArrayUnsafeUtility.Copy(utf8Bytes, out utf8Len, worstCaseCapacity, chars, s.Length);
                if (err != CopyError.None)
                {
                    return FormatError.Overflow;
                }
            }

            return fs.Append(utf8Bytes, utf8Len);
        }

        /// <summary>
        /// Copies another string to this string (making the two strings equal).
        /// Replaces any existing content of the FixedString.
        /// </summary>
        /// <remarks>
        /// When the method returns an error, the destination string is not modified.
        /// </remarks>
        /// <typeparam name="T">The type of the destination string.</typeparam>
        /// <param name="fs">The destination string.</param>
        /// <param name="s">The source string.</param>
        /// <returns>CopyError.None if successful. Returns CopyError.Truncation if the source string is too large to fit in the destination.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static CopyError CopyFrom<T>(ref this T fs, string s)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            fs.Length = 0;
            var fe = Append(ref fs, s);
            if (fe != FormatError.None)
                return CopyError.Truncation;
            return CopyError.None;
        }

        /// <summary>
        /// Copies another string to this string. If the string exceeds the capacity it will be truncated.
        /// Replaces any existing content of the FixedString.
        /// </summary>
        /// <typeparam name="T">The type of the destination string.</typeparam>
        /// <param name="fs">The destination string.</param>
        /// <param name="s">The source string.</param>
        /// <returns>CopyError.None if successful. Returns CopyError.Truncation if the source string is too large to fit in the destination.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static CopyError CopyFromTruncated<T>(ref this T fs, string s)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            if (s == null)
            {
                fs.Length = 0;
                return CopyError.None;
            }

            int utf8Len;
            fixed (char* chars = s)
            {
                var error = UTF8ArrayUnsafeUtility.Copy(fs.GetUnsafePtr(), out utf8Len, fs.Capacity, chars, s.Length);
                fs.Length = utf8Len;
                return error;
            }
        }

        /// <summary>
        /// Copies another string to this string. If the string exceeds the capacity it will be truncated.
        /// </summary>
        /// <remarks>
        /// When the method returns an error, the destination string is not modified.
        /// </remarks>
        /// <typeparam name="T">The type of the destination string.</typeparam>
        /// <typeparam name="T2">The type of the source string.</typeparam>
        /// <param name="fs">The destination string.</param>
        /// <param name="input">The source string.</param>
        /// <returns>CopyError.None if successful. Returns CopyError.Truncation if the source string is too large to fit in the destination.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes), typeof(FixedString128Bytes) })]
        public static CopyError CopyFromTruncated<T, T2>(ref this T fs, in T2 input)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
            where T2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            var error = UTF8ArrayUnsafeUtility.Copy(fs.GetUnsafePtr(), out int utf8Len, fs.Capacity, input.GetUnsafePtr(), input.Length);
            fs.Length = utf8Len;
            return error;
        }
    }
}
