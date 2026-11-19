// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Unity.SmartStrings.Core.Formatting;
using Unity.SmartStrings.Core.Parsing;

namespace Unity.SmartStrings.Core.Extensions;

/// <summary>
/// Contains all necessary info for formatting a value
/// </summary>
/// <remarks>
/// In "{Items.Length:choose(1,2,3):one|two|three}",
/// the <see cref="CurrentValue" /> would be the value of "Items.Length",
/// the <see cref="FormatterOptions" /> would be "1,2,3",
/// and the <see cref="Format" /> would be "one|two|three".
/// </remarks>
public interface IFormattingInfo
{
    /// <summary>
    /// The current value that is to be formatted.
    /// </summary>
    object CurrentValue { get; }

    /// <summary>
    /// This format specifies how to output the <see cref="CurrentValue" />.
    /// </summary>
    Format Format { get; }

    /// <summary>
    /// Contains all the details about the current placeholder.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    Placeholder Placeholder { get; }

    /// <summary>
    /// Alignment inserts spaces into the output to ensure consistent length.
    /// Positive numbers insert spaces to the left, to right-align the text.
    /// Negative numbers insert spaces to the right, to left-align the text.
    /// This should only work with the Default Formatter, but is optional with custom formatters.
    /// This is primarily for compatibility with String.Format.
    /// </summary>
    int Alignment { get; }

    /// <summary>
    /// When a named formatter is used, this will hold the options.
    /// For example, in "{0:choose(1,2,3):one|two|three}", FormatterOptions is "1,2,3".
    /// </summary>
    string FormatterOptions { get; }

    /// <summary>
    /// Infrequently used details, often used for debugging
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    FormatDetails FormatDetails { get; }

    /// <summary>
    /// Writes a <see cref="string"/> to the output.
    /// </summary>
    void Write(string text);

    /// <summary>
    /// Writes a <see cref="ReadOnlySpan{T}"/> text to the output.
    /// </summary>
    void Write(ReadOnlySpan<char> text);

    /// <summary>
    /// Creates a child <see cref="IFormattingInfo"/> from the current <see cref="IFormattingInfo"/> instance
    /// and invokes formatting with <see cref="SmartFormatter"/> with the child as parameter.
    /// </summary>
    void FormatAsChild(Format format, object value);

    /// <summary>
    /// Creates a <see cref="FormattingException" /> associated with the <see cref="IFormattingInfo.Format" />.
    /// </summary>
    FormattingException FormattingException(string issue, FormatItem problemItem = null, int startIndex = -1);
}
