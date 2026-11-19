// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;
using Unity.SmartStrings.Core.Parsing;

namespace Unity.SmartStrings.Core.Formatting;

/// <summary>
/// An exception caused while attempting to output the format.
/// </summary>
[Serializable]
public class FormattingException : Exception
{
    /// <summary>
    /// Creates a new instance of <see cref="FormattingException"/>.
    /// </summary>
    /// <param name="errorItem">The <see cref="FormatItem"/> which caused the <see cref="Exception"/>.</param>
    /// <param name="formatException">The <see cref="Exception"/> that was caused by the <see cref="FormatItem"/>.</param>
    /// <param name="index">The index inside the format string, where the error occurred.</param>
    public FormattingException(FormatItem errorItem, Exception formatException, int index) : base(formatException.Message, formatException)
    {
        Format = errorItem.BaseString;
        ErrorItem = errorItem;
        Issue = formatException.Message;
        Index = index;
    }

    /// <summary>
    /// Creates a new instance of <see cref="FormattingException"/>.
    /// </summary>
    /// <param name="errorItem">The <see cref="FormatItem"/> which caused the <see cref="Exception"/>.</param>
    /// <param name="issue">The description of the error.</param>
    /// <param name="index">The index inside the format string, where the error occurred.</param>
    public FormattingException(FormatItem errorItem, string issue, int index)
    {
        Format = errorItem.BaseString;
        ErrorItem = errorItem;
        Issue = issue;
        Index = index;
    }

    ///<inheritdoc/>
    protected FormattingException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Issue = string.Empty;
    }

    /// <summary>
    /// The base format string of the <see cref="FormatItem"/> that caused the <see cref="Exception"/>.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// The <see cref="FormatItem"/> that caused the <see cref="Exception"/>.
    /// </summary>
    public FormatItem ErrorItem { get; }

    /// <summary>
    /// Description of the formatting error.
    /// </summary>
    public string Issue { get; }

    /// <summary>
    /// The index inside the format string, where the error occurred.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Error message that indicates the position within the format string where the error occurred.
    /// </summary>
    public override string Message => $"Error parsing format string: {Issue} at {Index}\n{Format}\n{new string('-', Index) + "^"}";
}
