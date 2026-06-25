// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting;
using Unity.Burst.LowLevel.Unsafe;

namespace Unity.Burst
{
    internal static partial class BurstString
    {
        /// <summary>
        /// Copies a Burst managed UTF8 string prefixed by a ushort length to a FixedString with the specified maximum length.
        /// </summary>
        /// <param name="dest">Pointer to the fixed string.</param>
        /// <param name="destLength">Maximum number of UTF8 the fixed string supports without including the zero character.</param>
        /// <param name="src">The UTF8 Burst managed string prefixed by a ushort length and zero terminated.
        /// <param name="srcLength">Number of UTF8 the fixed string supports without including the zero character.</param>
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void CopyFixedString(byte* dest, int destLength, byte* src, int srcLength)
        {
            // TODO: should we throw an exception instead if the string doesn't fit?
            var finalLength = srcLength > destLength ? destLength : srcLength;
            // Write the length and zero null terminated
            *((ushort*)dest - 1) = (ushort)finalLength;
            dest[finalLength] = 0;
            BurstUnsafeUtility.MemCpy(dest, src, finalLength);
        }

        /// <summary>
        /// Format a UTF-8 string (with a specified source length) to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="src">The source buffer of the string to copy from.</param>
        /// <param name="srcLength">The length of the string from the source buffer.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, byte* src, int srcLength, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;

            // Align left
            if (AlignLeft(dest, ref destIndex, destLength, options.AlignAndSize, srcLength)) return;

            int maxToCopy = destLength - destIndex;
            int toCopyLength = srcLength > maxToCopy ? maxToCopy : srcLength;
            if (toCopyLength > 0)
            {
                BurstUnsafeUtility.MemCpy(dest + destIndex, src, toCopyLength);
                destIndex += toCopyLength;

                // Align right
                AlignRight(dest, ref destIndex, destLength, options.AlignAndSize, srcLength);
            }
        }

        /// <summary>
        /// Format a float value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, float value, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;
            ConvertFloatToString(dest, ref destIndex, destLength, value, options);
        }

        /// <summary>
        /// Format a double value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, double value, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;
            ConvertDoubleToString(dest, ref destIndex, destLength, value, options);
        }

        /// <summary>
        /// Format a bool value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, bool value, int formatOptionsRaw)
        {
            var length = value ? 4 : 5; // True = 4 chars, False = 5 chars
            var options = *(FormatOptions*)&formatOptionsRaw;

            // Align left
            if (AlignLeft(dest, ref destIndex, destLength, options.AlignAndSize, length)) return;

            if (value)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'T';
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'r';
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'u';
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'e';
            }
            else
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'F';
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'a';
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'l';
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'s';
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'e';
            }

            // Align right
            AlignRight(dest, ref destIndex, destLength, options.AlignAndSize, length);
        }

        /// <summary>
        /// Format a char value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, char value, int formatOptionsRaw)
        {
            var length = value <= 0x7f ? 1 : value <= 0x7FF ? 2 : 3;
            var options = *(FormatOptions*)&formatOptionsRaw;

            // Align left - Special case for char, make the length as it was always one byte (one char)
            // so that alignment is working fine (on a char basis)
            if (AlignLeft(dest, ref destIndex, destLength, options.AlignAndSize, 1)) return;

            // Basic encoding of UTF16 to UTF8, doesn't handle high/low surrogate as we are given only one char
            if (length == 1)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)value;
            }
            else if (length == 2)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)((value >> 6) | 0xC0);

                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)((value & 0x3F) | 0x80);
            }
            else if (length == 3)
            {
                // We don't handle high/low surrogate, so we replace the char with the replacement char
                // 0xEF, 0xBF, 0xBD
                bool isHighOrLowSurrogate = value >= '\xD800' && value <= '\xDFFF';
                if (isHighOrLowSurrogate)
                {
                    if (destIndex >= destLength) return;
                    dest[destIndex++] = 0xEF;

                    if (destIndex >= destLength) return;
                    dest[destIndex++] = 0xBF;

                    if (destIndex >= destLength) return;
                    dest[destIndex++] = 0xBD;
                }
                else
                {
                    if (destIndex >= destLength) return;
                    dest[destIndex++] = (byte)((value >> 12) | 0xE0);

                    if (destIndex >= destLength) return;
                    dest[destIndex++] = (byte)(((value >> 6) & 0x3F) | 0x80);

                    if (destIndex >= destLength) return;
                    dest[destIndex++] = (byte)((value & 0x3F) | 0x80);
                }
            }

            // Align right - Special case for char, make the length as it was always one byte (one char)
            // so that alignment is working fine (on a char basis)
            AlignRight(dest, ref destIndex, destLength, options.AlignAndSize, 1);
        }

        /// <summary>
        /// Format a byte value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, byte value, int formatOptionsRaw)
        {
            Format(dest, ref destIndex, destLength, (ulong)value, formatOptionsRaw);
        }

        /// <summary>
        /// Format an ushort value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, ushort value, int formatOptionsRaw)
        {
            Format(dest, ref destIndex, destLength, (ulong)value, formatOptionsRaw);
        }

        /// <summary>
        /// Format an uint value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, uint value, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;
            ConvertUnsignedIntegerToString(dest, ref destIndex, destLength, value, options);
        }

        /// <summary>
        /// Format a ulong value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, ulong value, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;
            ConvertUnsignedIntegerToString(dest, ref destIndex, destLength, value, options);
        }

        /// <summary>
        /// Format a sbyte value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, sbyte value, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;
            if (options.Kind == NumberFormatKind.Hexadecimal)
            {
                ConvertUnsignedIntegerToString(dest, ref destIndex, destLength, (byte)value, options);
            }
            else
            {
                ConvertIntegerToString(dest, ref destIndex, destLength, value, options);
            }
        }

        /// <summary>
        /// Format a short value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, short value, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;
            if (options.Kind == NumberFormatKind.Hexadecimal)
            {
                ConvertUnsignedIntegerToString(dest, ref destIndex, destLength, (ushort)value, options);
            }
            else
            {
                ConvertIntegerToString(dest, ref destIndex, destLength, value, options);
            }

        }

        /// <summary>
        /// Format an int value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, int value, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;
            if (options.Kind == NumberFormatKind.Hexadecimal)
            {
                ConvertUnsignedIntegerToString(dest, ref destIndex, destLength, (uint)value, options);
            }
            else
            {
                ConvertIntegerToString(dest, ref destIndex, destLength, value, options);
            }
        }

        /// <summary>
        /// Format a long value to a destination buffer.
        /// </summary>
        /// <param name="dest">Destination buffer.</param>
        /// <param name="destIndex">Current index in destination buffer.</param>
        /// <param name="destLength">Maximum length of destination buffer.</param>
        /// <param name="value">The value to format.</param>
        /// <param name="formatOptionsRaw">Formatting options encoded in raw format.</param>
        [Unity.Scripting.RequiredByAssembly]
        public static unsafe void Format(byte* dest, ref int destIndex, int destLength, long value, int formatOptionsRaw)
        {
            var options = *(FormatOptions*)&formatOptionsRaw;
            if (options.Kind == NumberFormatKind.Hexadecimal)
            {
                ConvertUnsignedIntegerToString(dest, ref destIndex, destLength, (ulong)value, options);
            }
            else
            {
                ConvertIntegerToString(dest, ref destIndex, destLength, value, options);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void ConvertUnsignedIntegerToString(byte* dest, ref int destIndex, int destLength, ulong value, FormatOptions options)
        {
            var basis = (uint)options.GetBase();
            if (basis < 2 || basis > 36) return;

            // Calculate the full length (including zero padding)
            int length = 0;
            var tmp = value;
            do
            {
                tmp /= basis;
                length++;
            } while (tmp != 0);

            // Write the characters for the numbers to a temp buffer
            int tmpIndex = length - 1;
            byte* tmpBuffer = stackalloc byte[length + 1];

            tmp = value;
            do
            {
                tmpBuffer[tmpIndex--] = ValueToIntegerChar((int)(tmp % basis), options.Uppercase);
                tmp /= basis;
            } while (tmp != 0);

            tmpBuffer[length] = 0;

            var numberBuffer = new NumberBuffer(NumberBufferKind.Integer, tmpBuffer, length, length, false);
            FormatNumber(dest, ref destIndex, destLength, ref numberBuffer, options.Specifier, options);
        }

        private static int GetLengthIntegerToString(long value, int basis, int zeroPadding)
        {
            int length = 0;
            var tmp = value;
            do
            {
                tmp /= basis;
                length++;
            } while (tmp != 0);

            if (length < zeroPadding)
            {
                length = zeroPadding;
            }

            if (value < 0) length++;
            return length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void ConvertIntegerToString(byte* dest, ref int destIndex, int destLength, long value, FormatOptions options)
        {
            var basis = options.GetBase();
            if (basis < 2 || basis > 36) return;

            // Calculate the full length (including zero padding)
            int length = 0;
            var tmp = value;
            do
            {
                tmp /= basis;
                length++;
            } while (tmp != 0);

            // Write the characters for the numbers to a temp buffer
            byte* tmpBuffer = stackalloc byte[length + 1];

            tmp = value;
            int tmpIndex = length - 1;
            do
            {
                tmpBuffer[tmpIndex--] = ValueToIntegerChar((int)(tmp % basis), options.Uppercase);
                tmp /= basis;
            } while (tmp != 0);
            tmpBuffer[length] = 0;

            var numberBuffer = new NumberBuffer(NumberBufferKind.Integer, tmpBuffer, length, length, value < 0);
            FormatNumber(dest, ref destIndex, destLength, ref numberBuffer, options.Specifier, options);
        }

        private static unsafe void FormatNumber(byte* dest, ref int destIndex, int destLength, ref NumberBuffer number, int nMaxDigits, FormatOptions options)
        {
            bool isCorrectlyRounded = (number.Kind == NumberBufferKind.Float);

            // If we have an integer, and the rendering is the default `G`, then use Decimal rendering which is faster
            if (number.Kind == NumberBufferKind.Integer && options.Kind == NumberFormatKind.General && options.Specifier == 0)
            {
                options.Kind = NumberFormatKind.Decimal;
            }

            int length;
            switch (options.Kind)
            {
                case NumberFormatKind.DecimalForceSigned:
                case NumberFormatKind.Decimal:
                case NumberFormatKind.Hexadecimal:
                    length = number.DigitsCount;

                    var zeroPadding = (int)options.Specifier;
                    int actualZeroPadding = 0;
                    if (length < zeroPadding)
                    {
                        actualZeroPadding = zeroPadding - length;
                        length = zeroPadding;
                    }

                    bool outputPositiveSign = options.Kind == NumberFormatKind.DecimalForceSigned;
                    length += number.IsNegative || outputPositiveSign ? 1 : 0;

                    // Perform left align
                    if (AlignLeft(dest, ref destIndex, destLength, options.AlignAndSize, length)) return;

                    FormatDecimalOrHexadecimal(dest, ref destIndex, destLength, ref number, actualZeroPadding, outputPositiveSign);

                    // Perform right align
                    AlignRight(dest, ref destIndex, destLength, options.AlignAndSize, length);

                    break;

                default:
                case NumberFormatKind.General:

                    if (nMaxDigits < 1)
                    {
                        // This ensures that the PAL code pads out to the correct place even when we use the default precision
                        nMaxDigits = number.DigitsCount;
                    }

                    RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);

                    // Calculate final rendering length
                    length = GetLengthForFormatGeneral(ref number, nMaxDigits);

                    // Perform left align
                    if (AlignLeft(dest, ref destIndex, destLength, options.AlignAndSize, length)) return;

                    // Format using general formatting
                    FormatGeneral(dest, ref destIndex, destLength, ref number, nMaxDigits, options.Uppercase ? (byte)'E' : (byte)'e');

                    // Perform right align
                    AlignRight(dest, ref destIndex, destLength, options.AlignAndSize, length);
                    break;
            }
        }

        private static unsafe void FormatDecimalOrHexadecimal(byte* dest, ref int destIndex, int destLength, ref NumberBuffer number, int zeroPadding, bool outputPositiveSign)
        {
            if (number.IsNegative)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'-';
            }
            else if (outputPositiveSign)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'+';
            }

            // Zero Padding
            for (int i = 0; i < zeroPadding; i++)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'0';
            }

            var digitCount = number.DigitsCount;
            byte* digits = number.GetDigitsPointer();
            for (int i = 0; i < digitCount; i++)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = digits[i];
            }
        }

        private static byte ValueToIntegerChar(int value, bool uppercase)
        {
            value = value < 0 ? -value : value;
            if (value <= 9)
                return (byte)('0' + value);
            if (value < 36)
                return (byte)((uppercase ? 'A' : 'a') + (value - 10));

            return (byte)'?';
        }

        private static readonly char[] SplitByColon = new char[] { ':' };

        private static void OptsSplit(string fullFormat, out string padding, out string format)
        {
            var split = fullFormat.Split(SplitByColon, StringSplitOptions.RemoveEmptyEntries);
            format = split[0];
            padding = null;
            if (split.Length == 2)
            {
                padding = format;
                format = split[1];
            }
            else if (split.Length == 1)
            {
                if (format[0] == ',')
                {
                    padding = format;
                    format = null;
                }
            }
            else
            {
                throw new ArgumentException($"Format `{format}` not supported. Invalid number {split.Length} of :. Expecting no more than one.");
            }
        }

        internal static (NumberFormatKind, bool lowerCase, int specifier) ParseNumberFormatKind(string format)
        {
            var formatKind = NumberFormatKind.General;
            var lowercase = false;
            var specifier = 0;

            if (string.IsNullOrWhiteSpace(format)) return (formatKind, lowercase, specifier);

            (formatKind, lowercase) = format[0] switch
            {
                'G' => (NumberFormatKind.General, false),
                'g' => (NumberFormatKind.General, true),
                'D' => (NumberFormatKind.Decimal, false),
                'd' => (NumberFormatKind.Decimal, true),
                'X' => (NumberFormatKind.Hexadecimal, false),
                'x' => (NumberFormatKind.Hexadecimal, true),
                _ => throw new ArgumentException(
                    $"Format `{format}` not supported. Only G, g, D, d, X, x are supported.")
            };

            if (format.Length > 1)
            {
                var specifierString = format.Substring(1);
                if (!uint.TryParse(specifierString, out var unsignedSpecifier))
                {
                    throw new ArgumentException($"Expecting an unsigned integer for specifier `{format}` instead of {specifierString}.");
                }
                specifier = (int)unsignedSpecifier;
            }

            return (formatKind, lowercase, specifier);
        }

        /// <summary>
        /// Parse a format string as specified .NET string.Format https://docs.microsoft.com/en-us/dotnet/api/system.string.format?view=netframework-4.8
        /// - Supports only Left/Right Padding (e.g {0,-20} {0, 8})
        /// - 'G' 'g' General formatting for numbers with precision specifier (e.g G4 or g4)
        /// - 'D' 'd' General formatting for numbers with precision specifier (e.g D5 or d5)
        /// - 'X' 'x' General formatting for integers with precision specifier (e.g X8 or x8)
        /// </summary>
        /// <param name="fullFormat"></param>
        /// <returns></returns>
        public static FormatOptions ParseFormatToFormatOptions(string fullFormat)
        {
            if (string.IsNullOrWhiteSpace(fullFormat)) return new FormatOptions();

            OptsSplit(fullFormat, out var padding, out var format);

            format = format?.Trim();
            padding = padding?.Trim();

            int alignAndSize = 0;
            var formatKind = NumberFormatKind.General;
            bool lowercase = false;
            int specifier = 0;

            if (!string.IsNullOrEmpty(format))
            {
                (formatKind, lowercase, specifier) = ParseNumberFormatKind(format);
            }

            if (!string.IsNullOrEmpty(padding))
            {
                if (padding[0] != ',')
                {
                    throw new ArgumentException($"Invalid padding `{padding}`, expecting to start with a leading `,` comma.");
                }

                var numberStr = padding.Substring(1);
                if (!int.TryParse(numberStr, out alignAndSize))
                {
                    throw new ArgumentException($"Expecting an integer for align/size padding `{numberStr}`.");
                }
            }

            return new FormatOptions(formatKind, (sbyte)alignAndSize, (byte)specifier, lowercase);
        }

        private static unsafe bool AlignRight(byte* dest, ref int destIndex, int destLength, int align, int length)
        {
            // right align
            if (align < 0)
            {
                align = -align;
                return AlignLeft(dest, ref destIndex, destLength, align, length);
            }

            return false;
        }

        private static unsafe bool AlignLeft(byte* dest, ref int destIndex, int destLength, int align, int length)
        {
            // left align
            if (align > 0)
            {
                while (length < align)
                {
                    if (destIndex >= destLength) return true;
                    dest[destIndex++] = (byte)' ';
                    length++;
                }
            }

            return false;
        }

        private static unsafe int GetLengthForFormatGeneral(ref NumberBuffer number, int nMaxDigits)
        {
            // NOTE: Must be kept in sync with FormatGeneral!
            int length = 0;
            int scale = number.Scale;
            int digPos = scale;
            bool scientific = false;

            // Don't switch to scientific notation
            if (digPos > nMaxDigits || digPos < -3)
            {
                digPos = 1;
                scientific = true;
            }

            byte* dig = number.GetDigitsPointer();

            if (number.IsNegative)
            {
                length++; // (byte)'-';
            }

            if (digPos > 0)
            {
                do
                {
                    if (*dig != 0)
                    {
                        dig++;
                    }
                    length++;
                } while (--digPos > 0);
            }
            else
            {
                length++;
            }

            if (*dig != 0 || digPos < 0)
            {
                length++; // (byte)'.';

                while (digPos < 0)
                {
                    length++; // (byte)'0';
                    digPos++;
                }

                while (*dig != 0)
                {
                    length++; // *dig++;
                    dig++;
                }
            }

            if (scientific)
            {
                length++; // e or E
                int exponent = number.Scale - 1;
                if (exponent >= 0) length++;
                length += GetLengthIntegerToString(exponent, 10, 2);
            }

            return length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void FormatGeneral(byte* dest, ref int destIndex, int destLength, ref NumberBuffer number, int nMaxDigits, byte expChar)
        {
            int scale = number.Scale;
            int digPos = scale;
            bool scientific = false;

            // Don't switch to scientific notation
            if (digPos > nMaxDigits || digPos < -3)
            {
                digPos = 1;
                scientific = true;
            }

            byte* dig = number.GetDigitsPointer();

            if (number.IsNegative)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'-';
            }

            if (digPos > 0)
            {
                do
                {
                    if (destIndex >= destLength) return;
                    dest[destIndex++] = (*dig != 0) ? (byte)(*dig++) : (byte)'0';
                } while (--digPos > 0);
            }
            else
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'0';
            }

            if (*dig != 0 || digPos < 0)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = (byte)'.';

                while (digPos < 0)
                {
                    if (destIndex >= destLength) return;
                    dest[destIndex++] = (byte)'0';
                    digPos++;
                }

                while (*dig != 0)
                {
                    if (destIndex >= destLength) return;
                    dest[destIndex++] = *dig++;
                }
            }

            if (scientific)
            {
                if (destIndex >= destLength) return;
                dest[destIndex++] = expChar;

                int exponent = number.Scale - 1;
                var exponentFormatOptions = new FormatOptions(NumberFormatKind.DecimalForceSigned, 0, 2, false);

                ConvertIntegerToString(dest, ref destIndex, destLength, exponent, exponentFormatOptions);
            }
        }

        private static unsafe void RoundNumber(ref NumberBuffer number, int pos, bool isCorrectlyRounded)
        {
            byte* dig = number.GetDigitsPointer();

            int i = 0;
            while (i < pos && dig[i] != (byte)'\0')
                i++;

            if ((i == pos) && ShouldRoundUp(dig, i, isCorrectlyRounded))
            {
                while (i > 0 && dig[i - 1] == (byte)'9')
                    i--;

                if (i > 0)
                {
                    dig[i - 1]++;
                }
                else
                {
                    number.Scale++;
                    dig[0] = (byte)('1');
                    i = 1;
                }
            }
            else
            {
                while (i > 0 && dig[i - 1] == (byte)'0')
                    i--;
            }

            if (i == 0)
            {
                number.Scale = 0;      // Decimals with scale ('0.00') should be rounded.
            }

            dig[i] = (byte)('\0');
            number.DigitsCount = i;
        }

        private static unsafe bool ShouldRoundUp(byte* dig, int i, bool isCorrectlyRounded)
        {
            // We only want to round up if the digit is greater than or equal to 5 and we are
            // not rounding a floating-point number. If we are rounding a floating-point number
            // we have one of two cases.
            //
            // In the case of a standard numeric-format specifier, the exact and correctly rounded
            // string will have been produced. In this scenario, pos will have pointed to the
            // terminating null for the buffer and so this will return false.
            //
            // However, in the case of a custom numeric-format specifier, we currently fall back
            // to generating Single/DoublePrecisionCustomFormat digits and then rely on this
            // function to round correctly instead. This can unfortunately lead to double-rounding
            // bugs but is the best we have right now due to back-compat concerns.

            byte digit = dig[i];

            if ((digit == '\0') || isCorrectlyRounded)
            {
                // Fast path for the common case with no rounding
                return false;
            }

            // Values greater than or equal to 5 should round up, otherwise we round down. The IEEE
            // 754 spec actually dictates that ties (exactly 5) should round to the nearest even number
            // but that can have undesired behavior for custom numeric format strings. This probably
            // needs further thought for .NET 5 so that we can be spec compliant and so that users
            // can get the desired rounding behavior for their needs.

            return digit >= '5';
        }

        private enum NumberBufferKind
        {
            Integer,
            Float,
        }

        /// <summary>
        /// Information about a number: pointer to digit buffer, scale and if negative.
        /// </summary>
        private unsafe struct NumberBuffer
        {
            private readonly byte* _buffer;

            public NumberBuffer(NumberBufferKind kind, byte* buffer, int digitsCount, int scale, bool isNegative)
            {
                Kind = kind;
                _buffer = buffer;
                DigitsCount = digitsCount;
                Scale = scale;
                IsNegative = isNegative;
            }

            public NumberBufferKind Kind;

            public int DigitsCount;

            public int Scale;

            public readonly bool IsNegative;

            public byte* GetDigitsPointer() => _buffer;
        }

        /// <summary>
        /// Type of formatting
        /// </summary>
        public enum NumberFormatKind : byte
        {
            /// <summary>
            /// General 'G' or 'g' formatting.
            /// </summary>
            General,

            /// <summary>
            /// Decimal 'D' or 'd' formatting.
            /// </summary>
            Decimal,

            /// <summary>
            /// Internal use only. Decimal 'D' or 'd' formatting with a `+` positive in front of the decimal if positive
            /// </summary>
            DecimalForceSigned,

            /// <summary>
            /// Hexadecimal 'X' or 'x' formatting.
            /// </summary>
            Hexadecimal,
        }

        /// <summary>
        /// Formatting options. Must be sizeof(int)
        /// </summary>
        public struct FormatOptions
        {
            public FormatOptions(NumberFormatKind kind, sbyte alignAndSize, byte specifier, bool lowercase) : this()
            {
                Kind = kind;
                AlignAndSize = alignAndSize;
                Specifier = specifier;
                Lowercase = lowercase;
            }

            public NumberFormatKind Kind;
            public sbyte AlignAndSize;
            public byte Specifier;
            public bool Lowercase;

            public bool Uppercase => !Lowercase;

            /// <summary>
            /// Encode this options to a single integer.
            /// </summary>
            /// <returns></returns>
            public unsafe int EncodeToRaw()
            {
                Debug.Assert(sizeof(FormatOptions) == sizeof(int));
                var value = this;
                return *(int*)&value;
            }

            /// <summary>
            /// Get the base used for formatting this number.
            /// </summary>
            /// <returns></returns>
            public int GetBase()
            {
                switch (Kind)
                {
                    case NumberFormatKind.Hexadecimal:
                        return 16;
                    default:
                        return 10;
                }
            }

            public override string ToString()
            {
                return $"{nameof(Kind)}: {Kind}, {nameof(AlignAndSize)}: {AlignAndSize}, {nameof(Specifier)}: {Specifier}, {nameof(Uppercase)}: {Uppercase}";
            }
        }
    }
}
