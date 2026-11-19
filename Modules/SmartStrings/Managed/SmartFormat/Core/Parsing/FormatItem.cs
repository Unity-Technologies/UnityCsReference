// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using Unity.SmartStrings.Core.Settings;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Base class that represents a substring
/// of text from a parsed format string.
/// </summary>
public abstract class FormatItem
{
    string m_ToStringCache;

    /// <summary>
    /// Gets the base format string.
    /// </summary>
    public string BaseString { get; protected set; }

    /// <summary>
    /// The end index is pointing to ONE POSITION AFTER the last character of item.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Format string: {0}{1}ABC
    /// Index:         012345678
    /// </code>
    /// Start index for 1st placeholder is 0, for the second it's 3, for the literal it's 6.
    /// End index for the 1st placeholder is 3, for the second it's 6, for the literal it's 9.
    /// </remarks>
    public int EndIndex { get; set; }

    /// <summary>
    /// The start index is pointing to the first character of item.
    /// </summary>
    /// <remarks>
    /// <code>
    /// Format string: {0}{1}ABC
    /// Index:         012345678
    /// </code>
    /// Start index for 1st placeholder is 0, for the second it's 3, for the literal it's 6.
    /// End index for the 1st placeholder is 3, for the second it's 6, for the literal it's 9.
    /// </remarks>
    public int StartIndex { get; set; }

    /// <summary>
    /// Gets the result of <see cref="EndIndex"/> minus <see cref="StartIndex"/>.
    /// </summary>
    public int Length => EndIndex - StartIndex;

    /// <summary>
    /// The settings for formatter and parser.
    /// </summary>
    public SmartSettings SmartSettings { get; protected set; }

    /// <summary>
    /// The parent <see cref="FormatItem"/> of this instance, <see langword="null"/> if no parent exists.
    /// </summary>
    public FormatItem ParentFormatItem { get; private set; }

    /// <summary>
    /// Initializes the <see cref="FormatItem"/> or the derived class.
    /// </summary>
    /// <param name="smartSettings">Formatter and parser settings.</param>
    /// <param name="parent">Parent <see cref="FormatItem"/>, or <see langword="null"/>.</param>
    /// <param name="baseString">Base format string.</param>
    /// <param name="startIndex">Start index of the <see cref="FormatItem"/> within the base format string.</param>
    /// <param name="endIndex">End index of the <see cref="FormatItem"/> within the base format string.</param>
    protected virtual void Initialize(SmartSettings smartSettings, FormatItem parent, string baseString, int startIndex, int endIndex)
    {
        ParentFormatItem = parent;
        SmartSettings = smartSettings;
        BaseString = baseString;
        StartIndex = startIndex;
        EndIndex = endIndex;
    }

    /// <summary>
    /// Clears the <see cref="FormatItem"/> or the derived class.
    /// </summary>
    public virtual void Clear()
    {
        m_ToStringCache = null;
        BaseString = string.Empty;
        EndIndex = 0;
        StartIndex = 0;
        SmartSettings = null;
        ParentFormatItem = null;
    }

    /// <summary>
    /// Retrieves the raw text that this item represents.
    /// </summary>
    public string RawText => ToString();

    /// <summary>
    /// Gets the string representation of this <see cref="FormatItem"/>.
    /// </summary>
    /// <returns>The string representation of this <see cref="FormatItem"/></returns>
    public override string ToString() => m_ToStringCache ??= AsSpan().ToString();

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> representation of this <see cref="FormatItem"/>.
    /// </summary>
    /// <returns>The <see cref="ReadOnlySpan{T}"/> representation of this <see cref="FormatItem"/></returns>
    public virtual ReadOnlySpan<char> AsSpan() => EndIndex <= StartIndex
    ? BaseString.AsSpan(StartIndex)
    : BaseString.AsSpan(StartIndex, Length);
}
