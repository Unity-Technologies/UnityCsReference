// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: UIToolkitFramework not yet converted
//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Scripting.LifecycleManagement;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Handles escaped literals, like \\ or \n
/// </summary>
[NoAutoStaticsCleanup] // immutable escape-character lookup tables
static class EscapedLiteral
{
    static readonly Dictionary<char, char> GeneralLookupTable = new() {
        // General
        {'\\', '\\'},
        {'{', '{'},
        {'}', '}'},
        {'0', '\0'},
        {'a', '\a'},
        {'b', '\b'},
        {'f', '\f'},
        {'n', '\n'},
        {'r', '\r'},
        {'t', '\t'},
        {'v', '\v'},
        {':', ':'} // escaped colons can be used anywhere in the format string
    };

    static readonly Dictionary<char, char> FormatterOptionsLookupTable = new() {
        // Smart.Format characters used in formatter options
        {'(', '('},
        {')', ')'}
    };

    /// <summary>
    /// Tries to get the <see cref="char"/> that corresponds to an escaped input <see cref="char"/>.
    /// </summary>
    /// <param name="input">The input character.</param>
    /// <param name="result">The matching character.</param>
    /// <param name="includeFormatterOptionChars">If <see langword="true"/>, (){}: will be escaped, else not.</param>
    /// <returns><see langword="true"/>, if a matching character was found.</returns>
    public static bool TryGetChar(char input, out char result, bool includeFormatterOptionChars)
    {
        return includeFormatterOptionChars
            ? GeneralLookupTable.TryGetValue(input, out result) ||
            FormatterOptionsLookupTable.TryGetValue(input, out result)
            : GeneralLookupTable.TryGetValue(input, out result);
    }

    static char GetUnicode(ReadOnlySpan<char> input, int startIndex)
    {
        var unicode = input.Length - startIndex >= 4
            ? input.Slice(startIndex, 4)
            : input.Slice(startIndex);
        if (int.TryParse(unicode, NumberStyles.HexNumber, null, out var result))
        {
            return (char)result;
        }

        throw new ArgumentException($"Unrecognized escape sequence in literal: \"\\u{unicode.ToString()}\"");
    }

    /// <summary>
    /// Converts escaped characters in input with real characters, e.g. "\\" => "\", if '\' is the escape character.
    /// </summary>
    /// <param name="escapingSequenceStart"></param>
    /// <param name="input"></param>
    /// <param name="includeFormatterOptionChars">If <see langword="true"/>, (){}: will be escaped, else not.</param>
    /// <param name="resultBuffer">The buffer to fill. It's enough to have a buffer with the same size as the input length.</param>
    /// <returns>The input having escaped characters replaced with their real value.</returns>
    public static ReadOnlySpan<char> UnEscapeCharLiterals(char escapingSequenceStart, ReadOnlySpan<char> input, bool includeFormatterOptionChars, Span<char> resultBuffer)
    {
        var max = input.Length;
        var resultIndex = 0;
        for (var inputIndex = 0; inputIndex < max; inputIndex++)
        {
            int nextInputIndex;
            if (inputIndex + 1 < max)
            {
                nextInputIndex = inputIndex + 1;
            }
            else
            {
                resultBuffer[resultIndex++] = input[inputIndex];
                return resultBuffer.Slice(0, resultIndex);
            }

            if (input[inputIndex] == escapingSequenceStart)
            {
                if (input[nextInputIndex] == 'u')
                {
                    // GetUnicode will throw if code is illegal
                    resultBuffer[resultIndex++] = GetUnicode(input, nextInputIndex + 1);
                    inputIndex += 5;  // move to last unicode character
                    continue;
                }

                if (TryGetChar(input[nextInputIndex], out var realChar, includeFormatterOptionChars))
                {
                    resultBuffer[resultIndex++] = realChar;
                }
                else
                {
                    throw new ArgumentException($"Unrecognized escape sequence \"{input[inputIndex]}{input[nextInputIndex]}\" in literal.");
                }
                inputIndex++;
            }
            else
            {
                resultBuffer[resultIndex++] = input[inputIndex];
            }
        }
        return resultBuffer.Slice(0, resultIndex);
    }

    /// <summary>
    /// Escapes a string, that contains character which must be escaped.
    /// </summary>
    /// <param name="escapeSequenceStart">The character starting the escape sequence.</param>
    /// <param name="input">The string to escape.</param>
    /// <param name="startIndex"></param>
    /// <param name="length"></param>
    /// <param name="includeFormatterOptionChars"><see langword="true"/>, if characters for formatter options should be included. Default is <see langword="false"/>.</param>
    /// <returns>Returns the escaped characters.</returns>
    public static IEnumerable<char> EscapeCharLiterals(char escapeSequenceStart, string input, int startIndex, int length, bool includeFormatterOptionChars)
    {
        var max = startIndex + length;
        for (var index = startIndex; index < max; index++)
        {
            var c = input[index];
            if (GeneralLookupTable.ContainsValue(c))
            {
                yield return escapeSequenceStart;
                yield return GetKeyForValue(GeneralLookupTable, c);
                continue;
            }

            if (includeFormatterOptionChars && FormatterOptionsLookupTable.ContainsValue(c))
            {
                yield return escapeSequenceStart;
                yield return GetKeyForValue(FormatterOptionsLookupTable, c);
                continue;
            }

            yield return c;
        }
    }

    static char GetKeyForValue(Dictionary<char, char> table, char value)
    {
        foreach (var kv in table)
        {
            if (kv.Value == value) return kv.Key;
        }

        return default;
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
