// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// Provides methods for copying and encoding Unicode text.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static unsafe class UTF8ArrayUnsafeUtility
    {

        /// <summary>
        /// Copies a buffer of UCS-2 text. The copy is encoded as UTF-8.
        /// </summary>
        /// <remarks>Assumes the source data is valid UCS-2.</remarks>
        /// <param name="src">The source buffer for reading UCS-2.</param>
        /// <param name="srcLength">The number of chars to read from the source.</param>
        /// <param name="dest">The destination buffer for writing UTF-8.</param>
        /// <param name="destLength">Outputs the number of bytes written to the destination.</param>
        /// <param name="destUTF8MaxLengthInBytes">The max number of bytes that will be written to the destination buffer.</param>
        /// <returns><see cref="CopyError.None"/> if the copy fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Copy(byte *dest, out int destLength, int destUTF8MaxLengthInBytes, char *src, int srcLength)
        {
            var error = Unicode.Utf16ToUtf8(src, srcLength, dest, out destLength, destUTF8MaxLengthInBytes);
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Copies a buffer of UCS-2 text. The copy is encoded as UTF-8.
        /// </summary>
        /// <remarks>Assumes the source data is valid UCS-2.</remarks>
        /// <param name="src">The source buffer for reading UCS-2.</param>
        /// <param name="srcLength">The number of chars to read from the source.</param>
        /// <param name="dest">The destination buffer for writing UTF-8.</param>
        /// <param name="destLength">Outputs the number of bytes written to the destination.</param>
        /// <param name="destUTF8MaxLengthInBytes">The max number of bytes that will be written to the destination buffer.</param>
        /// <returns><see cref="CopyError.None"/> if the copy fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Copy(byte *dest, out ushort destLength, ushort destUTF8MaxLengthInBytes, char *src, int srcLength)
        {
            var error = Unicode.Utf16ToUtf8(src, srcLength, dest, out var temp, destUTF8MaxLengthInBytes);
            destLength = (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Copies a buffer of UCS-8 text.
        /// </summary>
        /// <remarks>Assumes the source data is valid UTF-8.</remarks>
        /// <param name="src">The source buffer.</param>
        /// <param name="srcLength">The number of chars to read from the source.</param>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="destLength">Outputs the number of bytes written to the destination.</param>
        /// <param name="destUTF8MaxLengthInBytes">The max number of bytes that will be written to the destination buffer.</param>
        /// <returns><see cref="CopyError.None"/> if the copy fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Copy(byte *dest, out int destLength, int destUTF8MaxLengthInBytes, byte *src, int srcLength)
        {
            var error = Unicode.Utf8ToUtf8(src, srcLength, dest, out var temp, destUTF8MaxLengthInBytes);
            destLength = temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Copies a buffer of UCS-8 text.
        /// </summary>
        /// <remarks>Assumes the source data is valid UTF-8.</remarks>
        /// <param name="src">The source buffer.</param>
        /// <param name="srcLength">The number of chars to read from the source.</param>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="destLength">Outputs the number of bytes written to the destination.</param>
        /// <param name="destUTF8MaxLengthInBytes">The max number of bytes that will be written to the destination buffer.</param>
        /// <returns><see cref="CopyError.None"/> if the copy fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Copy(byte *dest, out ushort destLength, ushort destUTF8MaxLengthInBytes, byte *src, ushort srcLength)
        {
            var error = Unicode.Utf8ToUtf8(src, srcLength, dest, out var temp, destUTF8MaxLengthInBytes);
            destLength = (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Copies a buffer of UTF-8 text. The copy is encoded as UCS-2.
        /// </summary>
        /// <remarks>Assumes the source data is valid UTF-8.</remarks>
        /// <param name="src">The source buffer for reading UTF-8.</param>
        /// <param name="srcLength">The number of bytes to read from the source.</param>
        /// <param name="dest">The destination buffer for writing UCS-2.</param>
        /// <param name="destLength">Outputs the number of chars written to the destination.</param>
        /// <param name="destUCS2MaxLengthInChars">The max number of chars that will be written to the destination buffer.</param>
        /// <returns><see cref="CopyError.None"/> if the copy fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Copy(char *dest, out int destLength, int destUCS2MaxLengthInChars, byte *src, int srcLength)
        {
            if (ConversionError.None == Unicode.Utf8ToUtf16(src, srcLength, dest, out destLength, destUCS2MaxLengthInChars))
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Copies a buffer of UTF-8 text. The copy is encoded as UCS-2.
        /// </summary>
        /// <remarks>Assumes the source data is valid UTF-8.</remarks>
        /// <param name="src">The source buffer for reading UTF-8.</param>
        /// <param name="srcLength">The number of bytes to read from the source.</param>
        /// <param name="dest">The destination buffer for writing UCS-2.</param>
        /// <param name="destLength">Outputs the number of chars written to the destination.</param>
        /// <param name="destUCS2MaxLengthInChars">The max number of chars that will be written to the destination buffer.</param>
        /// <returns><see cref="CopyError.None"/> if the copy fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Copy(char *dest, out ushort destLength, ushort destUCS2MaxLengthInChars, byte *src, ushort srcLength)
        {
            var error = Unicode.Utf8ToUtf16(src, srcLength, dest, out var temp, destUCS2MaxLengthInChars);
            destLength = (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Appends UTF-8 text to a buffer.
        /// </summary>
        /// <remarks>Assumes the source data is valid UTF-8.
        ///
        /// No data will be copied if the destination has insufficient capacity for the full append, *i.e.* if `srcLength > (destCapacity - destLength)`.
        /// </remarks>
        /// <param name="src">The source buffer.</param>
        /// <param name="srcLength">The number of bytes to read from the source.</param>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="destLength">Reference to the destination buffer's length in bytes *before* the append. Will be assigned the new length *after* the append.</param>
        /// <param name="destCapacity">The destination buffer capacity in bytes.</param>
        /// <returns><see cref="FormatError.None"/> if the append fully completes. Otherwise, returns <see cref="FormatError.Overflow"/>.</returns>
        public static FormatError AppendUTF8Bytes(byte* dest, ref int destLength, int destCapacity, byte* src, int srcLength)
        {
            if (destLength + srcLength > destCapacity)
                return FormatError.Overflow;
            UnsafeUtility.MemCpy(dest + destLength, src, srcLength);
            destLength += srcLength;
            return FormatError.None;
        }

        /// <summary>
        /// Appends UTF-8 text to a buffer.
        /// </summary>
        /// <remarks>Assumes the source data is valid UTF-8.</remarks>
        /// <param name="src">The source buffer.</param>
        /// <param name="srcLength">The number of bytes to read from the source.</param>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="destLength">Reference to the destination buffer's length in bytes *before* the append. Will be assigned the number of bytes appended.</param>
        /// <param name="destUTF8MaxLengthInBytes">The destination buffer's length in bytes. Data will not be appended past this length.</param>
        /// <returns><see cref="CopyError.None"/> if the append fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Append(byte *dest, ref ushort destLength, ushort destUTF8MaxLengthInBytes, byte *src, ushort srcLength)
        {
            var error = Unicode.Utf8ToUtf8(src, srcLength, dest + destLength, out var temp, destUTF8MaxLengthInBytes - destLength);
            destLength += (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Appends UCS-2 text to a buffer, encoded as UTF-8.
        /// </summary>
        /// <remarks>Assumes the source data is valid UCS-2.</remarks>
        /// <param name="src">The source buffer.</param>
        /// <param name="srcLength">The number of chars to read from the source.</param>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="destLength">Reference to the destination buffer's length in bytes *before* the append. Will be assigned the number of bytes appended.</param>
        /// <param name="destUTF8MaxLengthInBytes">The destination buffer's length in bytes. Data will not be appended past this length.</param>
        /// <returns><see cref="CopyError.None"/> if the append fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Append(byte *dest, ref ushort destLength, ushort destUTF8MaxLengthInBytes, char *src, int srcLength)
        {
            var error = Unicode.Utf16ToUtf8(src, srcLength, dest + destLength, out var temp, destUTF8MaxLengthInBytes - destLength);
            destLength += (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        /// <summary>
        /// Appends UTF-8 text to a buffer, encoded as UCS-2.
        /// </summary>
        /// <remarks>Assumes the source data is valid UTF-8.</remarks>
        /// <param name="src">The source buffer.</param>
        /// <param name="srcLength">The number of bytes to read from the source.</param>
        /// <param name="dest">The destination buffer.</param>
        /// <param name="destLength">Reference to the destination buffer's length in chars *before* the append. Will be assigned the number of chars appended.</param>
        /// <param name="destUCS2MaxLengthInChars">The destination buffer's length in chars. Data will not be appended past this length.</param>
        /// <returns><see cref="CopyError.None"/> if the append fully completes. Otherwise, returns <see cref="CopyError.Truncation"/>.</returns>
        public static CopyError Append(char *dest, ref ushort destLength, ushort destUCS2MaxLengthInChars, byte *src, ushort srcLength)
        {
            var error = Unicode.Utf8ToUtf16(src, srcLength, dest + destLength, out var temp, destUCS2MaxLengthInChars - destLength);
            destLength += (ushort)temp;
            if (error == ConversionError.None)
                return CopyError.None;
            return CopyError.Truncation;
        }

        internal struct Comparison
        {
            public bool terminates;
            public int result;
            public Comparison(Unicode.Rune runeA, ConversionError errorA, Unicode.Rune runeB, ConversionError errorB)
            {
                if(errorA != ConversionError.None)
                    runeA.value = 0;
                if(errorB != ConversionError.None)
                    runeB.value = 0;
                if(runeA.value != runeB.value)
                {
                    result = runeA.value - runeB.value;
                    terminates = true;
                }
                else
                {
                    result = 0;
                    terminates = (runeA.value == 0 && runeB.value == 0);
                }
            }
        }

        /// <summary>Compares two UTF-8 buffers for relative equality.</summary>
        /// <param name="utf8BufferA">The first buffer of UTF-8 text.</param>
        /// <param name="utf8LengthInBytesA">The length in bytes of the first UTF-8 buffer.</param>
        /// <param name="utf8BufferB">The second buffer of UTF-8 text.</param>
        /// <param name="utf8LengthInBytesB">The length in bytes of the second UTF-8 buffer.</param>
        /// <returns>
        /// Less than zero if first different code point is less in the first UTF-8 buffer.
        /// Zero if the strings are identical.
        /// More than zero if first different code point is less in the second UTF-8 buffer.
        /// </returns>
        public static int StrCmp(byte* utf8BufferA, int utf8LengthInBytesA, byte* utf8BufferB, int utf8LengthInBytesB)
        {
            int byteIndexA = 0;
            int byteIndexB = 0;
            while(true)
            {
                var utf8ErrorA = Unicode.Utf8ToUcs(out var utf8RuneA, utf8BufferA,ref byteIndexA, utf8LengthInBytesA);
                var utf8ErrorB = Unicode.Utf8ToUcs(out var utf8RuneB, utf8BufferB, ref byteIndexB, utf8LengthInBytesB);
                var comparison = new Comparison(utf8RuneA, utf8ErrorA, utf8RuneB, utf8ErrorB);
                if(comparison.terminates)
                    return comparison.result;
            }
        }

        internal static int StrCmp(byte* utf8BufferA, int utf8LengthInBytesA, Unicode.Rune* runeBufferB, int lengthInRunesB)
        {
            int charIndexA = 0;
            int charIndexB = 0;
            while (true)
            {
                var utf16ErrorA = Unicode.Utf8ToUcs(out var utf16RuneA, utf8BufferA, ref charIndexA, utf8LengthInBytesA);
                var errorB = Unicode.UcsToUcs(out var runeB, runeBufferB, ref charIndexB, lengthInRunesB);
                var comparison = new Comparison(utf16RuneA, utf16ErrorA, runeB, errorB);
                if (comparison.terminates)
                    return comparison.result;
            }
        }

        /// <summary>Compares two UTF-16 buffers for relative equality.</summary>
        /// <param name="utf16BufferA">The first buffer of UTF-16 text.</param>
        /// <param name="utf16LengthInCharsA">The length in chars of the first UTF-16 buffer.</param>
        /// <param name="utf16BufferB">The second buffer of UTF-16 text.</param>
        /// <param name="utf16LengthInCharsB">The length in chars of the second UTF-16 buffer.</param>
        /// <returns>
        /// Less than zero if first different code point is less in the first UTF-16 buffer.
        /// Zero if the strings are identical.
        /// More than zero if first different code point is less in the second UTF-16 buffer.
        /// </returns>
        public static int StrCmp(char* utf16BufferA, int utf16LengthInCharsA, char* utf16BufferB, int utf16LengthInCharsB)
        {
            int charIndexA = 0;
            int charIndexB = 0;
            while(true)
            {
                var utf16ErrorA = Unicode.Utf16ToUcs(out var utf16RuneA, utf16BufferA,ref charIndexA, utf16LengthInCharsA);
                var utf16ErrorB = Unicode.Utf16ToUcs(out var utf16RuneB, utf16BufferB, ref charIndexB, utf16LengthInCharsB);
                var comparison = new Comparison(utf16RuneA, utf16ErrorA, utf16RuneB, utf16ErrorB);
                if(comparison.terminates)
                    return comparison.result;
            }
        }

        /// <summary>Returns true if two UTF-8 buffers have the same length and content.</summary>
        /// <param name="aBytes">The first buffer of UTF-8 text.</param>
        /// <param name="aLength">The length in bytes of the first buffer.</param>
        /// <param name="bBytes">The second buffer of UTF-8 text.</param>
        /// <param name="bLength">The length in bytes of the second buffer.</param>
        /// <returns>True if the content of both strings is identical.</returns>
        public static bool EqualsUTF8Bytes(byte* aBytes, int aLength, byte* bBytes, int bLength)
        {
            return aLength == bLength && StrCmp(aBytes, aLength, bBytes, bLength) == 0;
        }

        /// <summary>Compares a UTF-8 buffer and a UTF-16 buffer for relative equality.</summary>
        /// <param name="utf8Buffer">The buffer of UTF-8 text.</param>
        /// <param name="utf8LengthInBytes">The length in bytes of the UTF-8 buffer.</param>
        /// <param name="utf16Buffer">The buffer of UTF-16 text.</param>
        /// <param name="utf16LengthInChars">The length in chars of the UTF-16 buffer.</param>
        /// <returns>
        /// Less than zero if first different code point is less in UTF-8 buffer.
        /// Zero if the strings are identical.
        /// More than zero if first different code point is less in UTF-16 buffer.
        /// </returns>
        public static int StrCmp(byte* utf8Buffer, int utf8LengthInBytes, char* utf16Buffer, int utf16LengthInChars)
        {
            int byteIndex = 0;
            int charIndex = 0;
            while(true)
            {
                var utf8Error = Unicode.Utf8ToUcs(out var utf8Rune, utf8Buffer,ref byteIndex, utf8LengthInBytes);
                var utf16Error = Unicode.Utf16ToUcs(out var utf16Rune, utf16Buffer, ref charIndex, utf16LengthInChars);
                var comparison = new Comparison(utf8Rune, utf8Error, utf16Rune, utf16Error);
                if(comparison.terminates)
                    return comparison.result;
            }
        }

        /// <summary>Compares a UTF-16 buffer and a UTF-8 buffer for relative equality.</summary>
        /// <param name="utf16Buffer">The buffer of UTF-16 text.</param>
        /// <param name="utf16LengthInChars">The length in chars of the UTF-16 buffer.</param>
        /// <param name="utf8Buffer">The buffer of UTF-8 text.</param>
        /// <param name="utf8LengthInBytes">The length in bytes of the UTF-8 buffer.</param>
        /// <returns>
        /// Less than zero if first different code point is less in UTF-16 buffer.
        /// Zero if the strings are identical.
        /// More than zero if first different code point is less in UTF-8 buffer.
        /// </returns>
        public static int StrCmp(char* utf16Buffer, int utf16LengthInChars, byte* utf8Buffer, int utf8LengthInBytes)
        {
            return -StrCmp(utf8Buffer, utf8LengthInBytes, utf16Buffer, utf16LengthInChars);
        }

    }
}
