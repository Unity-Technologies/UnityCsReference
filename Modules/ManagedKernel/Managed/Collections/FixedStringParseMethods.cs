// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace Unity.Collections
{
    // The summary for this partial class lives in FixedStringMethods.cs - don't add one here.
    [GenerateTestsForBurstCompatibility]
    public unsafe static partial class FixedStringMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        internal static bool ParseLongInternal<T>(ref T fs, ref int offset, out long value)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            int resetOffset = offset;
            int sign = 1;
            if (offset < fs.Length)
            {
                if (fs.Peek(offset).value == '+')
                    fs.Read(ref offset);
                else if (fs.Peek(offset).value == '-')
                {
                    sign = -1;
                    fs.Read(ref offset);
                }
            }

            int digitOffset = offset;
            value = 0;
            while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
            {
                value *= 10;
                value += fs.Read(ref offset).value - '0';
            }
            value = sign * value;

            // If there was no number parsed, revert the offset since it's a syntax error and we might
            // have erroneously parsed a '-' or '+'
            if (offset == digitOffset)
            {
                offset = resetOffset;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses an int from this string starting at a byte offset.
        /// </summary>
        /// <remarks>
        /// Stops parsing after the last number character. (Unlike parsing methods in other API's, this method does not expect to necessarily parse the entire string.)
        ///
        /// The parsed value is bitwise-identical to the result of System.Int32.Parse.
        /// </remarks>
        /// <typeparam name="T">A FixedString*N*Bytes type.</typeparam>
        /// <param name="fs">The string from which to parse.</param>
        /// <param name="offset">A reference to an index of the byte at which to parse an int.</param>
        /// <param name="output">Outputs the parsed int. Ignore if parsing fails.</param>
        /// <returns>ParseError.None if successful. Otherwise returns ParseError.Overflow or ParseError.Syntax.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static ParseError Parse<T>(ref this T fs, ref int offset, ref int output)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            if (!ParseLongInternal(ref fs, ref offset, out long value))
                return ParseError.Syntax;
            if (value > int.MaxValue)
                return ParseError.Overflow;
            if (value < int.MinValue)
                return ParseError.Overflow;
            output = (int)value;
            return ParseError.None;
        }

        /// <summary>
        /// Parses an uint from this string starting at a byte offset.
        /// </summary>
        /// <remarks>
        /// Stops parsing after the last number character. (Unlike parsing methods in other API's, this method does not expect to necessarily parse the entire string.)
        ///
        /// The parsed value is bitwise-identical to the result of System.UInt32.Parse.
        /// </remarks>
        /// <typeparam name="T">A FixedString*N*Bytes type.</typeparam>
        /// <param name="fs">The string from which to parse.</param>
        /// <param name="offset">A reference to an index of the byte at which to parse a uint.</param>
        /// <param name="output">Outputs the parsed uint. Ignore if parsing fails.</param>
        /// <returns>ParseError.None if successful. Otherwise returns ParseError.Overflow or ParseError.Syntax.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static ParseError Parse<T>(ref this T fs, ref int offset, ref uint output)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            if (!ParseLongInternal(ref fs, ref offset, out long value))
                return ParseError.Syntax;
            if (value > uint.MaxValue)
                return ParseError.Overflow;
            if (value < uint.MinValue)
                return ParseError.Overflow;
            output = (uint)value;
            return ParseError.None;
        }

        /// <summary>
        /// Parses a float from this string starting at a byte offset.
        /// </summary>
        /// Stops parsing after the last number character. (Unlike parsing methods in other API's, this method does not expect to necessarily parse the entire string.)
        ///
        /// <remarks>The parsed value is bitwise-identical to the result of System.Single.Parse.</remarks>
        /// <typeparam name="T">A FixedString*N*Bytes type.</typeparam>
        /// <param name="fs">The string from which to parse.</param>
        /// <param name="offset">Index of the byte at which to parse a float.</param>
        /// <param name="output">Outputs the parsed float. Ignore if parsing fails.</param>
        /// <param name="decimalSeparator">The character used to separate the integer part of the number from the fractional part. Defaults to '.' (period).</param>
        /// <returns>ParseError.None if successful. Otherwise returns ParseError.Overflow, ParseError.Underflow, or ParseError.Syntax.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString128Bytes) })]
        public static ParseError Parse<T>(ref this T fs, ref int offset, ref float output, char decimalSeparator = '.')
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            int resetOffset = offset;
            int sign = 1;
            if (offset < fs.Length)
            {
                if (fs.Peek(offset).value == '+')
                    fs.Read(ref offset);
                else if (fs.Peek(offset).value == '-')
                {
                    sign = -1;
                    fs.Read(ref offset);
                }
            }
            if (fs.Found(ref offset, 'n', 'a', 'n'))
            {
                FixedStringUtils.UintFloatUnion ufu = new FixedStringUtils.UintFloatUnion();
                ufu.uintValue = 4290772992U;
                output = ufu.floatValue;
                return ParseError.None;
            }
            if (fs.Found(ref offset, 'i', 'n', 'f', 'i', 'n', 'i', 't', 'y'))
            {
                output = (sign == 1) ? Single.PositiveInfinity : Single.NegativeInfinity;
                return ParseError.None;
            }

            ulong decimalMantissa = 0;
            int significantDigits = 0;
            int digitsAfterDot = 0;
            int mantissaDigits = 0;
            while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
            {
                ++mantissaDigits;
                if (significantDigits < 9)
                {
                    var temp = decimalMantissa * 10 + (ulong)(fs.Peek(offset).value - '0');
                    if (temp > decimalMantissa)
                        ++significantDigits;
                    decimalMantissa = temp;
                }
                else
                    --digitsAfterDot;
                fs.Read(ref offset);
            }
            if (offset < fs.Length && fs.Peek(offset).value == decimalSeparator)
            {
                fs.Read(ref offset);
                while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
                {
                    ++mantissaDigits;
                    if (significantDigits < 9)
                    {
                        var temp = decimalMantissa * 10 + (ulong)(fs.Peek(offset).value - '0');
                        if (temp > decimalMantissa)
                            ++significantDigits;
                        decimalMantissa = temp;
                        ++digitsAfterDot;
                    }
                    fs.Read(ref offset);
                }
            }
            if (mantissaDigits == 0)
            {
                // Reset offset in case '+' or '-' was erroneously parsed
                offset = resetOffset;
                return ParseError.Syntax;
            }
            int decimalExponent = 0;
            int decimalExponentSign = 1;
            if (offset < fs.Length && (fs.Peek(offset).value | 32) == 'e')
            {
                fs.Read(ref offset);
                if (offset < fs.Length)
                {
                    if (fs.Peek(offset).value == '+')
                        fs.Read(ref offset);
                    else if (fs.Peek(offset).value == '-')
                    {
                        decimalExponentSign = -1;
                        fs.Read(ref offset);
                    }
                }
                int digitOffset = offset;
                while (offset < fs.Length && Unicode.Rune.IsDigit(fs.Peek(offset)))
                {
                    decimalExponent = decimalExponent * 10 + (fs.Peek(offset).value - '0');
                    fs.Read(ref offset);
                }
                if (offset == digitOffset)
                {
                    // Reset offset in case '+' or '-' was erroneously parsed
                    offset = resetOffset;
                    return ParseError.Syntax;
                }
                if (decimalExponent > 38)
                {
                    if (decimalExponentSign == 1)
                        return ParseError.Overflow;
                    else
                        return ParseError.Underflow;
                }
            }
            decimalExponent = decimalExponent * decimalExponentSign - digitsAfterDot;
            var error = FixedStringUtils.Base10ToBase2(ref output, decimalMantissa, decimalExponent);
            if (error != ParseError.None)
                return error;
            output *= sign;
            return ParseError.None;
        }
    }
}
