// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using Unity.SmartStrings.Core.Settings;
using Unity.SmartStrings.Pooling.SmartPools;

namespace Unity.SmartStrings.Core.Parsing;

/// <summary>
/// Represents the literal text that is found
/// in a parsed format string.
/// </summary>
public class LiteralText : FormatItem
{
    string m_ToStringCache;

    /// <summary>
    /// Initializes the <see cref="LiteralText"/> instance, representing the literal text that is found in a parsed format string.
    /// </summary>
    /// <param name="smartSettings">Formatter and parser settings.</param>
    /// <param name="parent">Parent <see cref="FormatItem"/>.</param>
    /// <param name="baseString">Reference to the parsed format string.</param>
    /// <param name="startIndex">Start index of the <see cref="LiteralText"/> in the format string.</param>
    /// <param name="endIndex">End index of the <see cref="LiteralText"/> in the format string.</param>
    /// <returns>The <see cref="LiteralText"/> instance, representing the literal text that is found in a parsed format string.</returns>
    public new LiteralText Initialize(SmartSettings smartSettings, FormatItem parent, string baseString, int startIndex, int endIndex)
    {
        base.Initialize(smartSettings, parent, baseString, startIndex, endIndex);
        return this;
    }

    /// <summary>
    /// Get the string representation of the <see cref="LiteralText"/>, with escaped characters converted.
    /// Note: The <see cref="Parser"/> puts each escaped character of an input string
    /// into its own <see cref="LiteralText"/> item.
    /// </summary>
    /// <returns>The string representation of the <see cref="LiteralText"/>, with escaped characters converted.</returns>
    public override string ToString()
    {
        if (m_ToStringCache != null) return m_ToStringCache;
        if (Length == 0) m_ToStringCache = string.Empty;

        // The buffer is only 1 character
        m_ToStringCache = AsSpan().ToString();

        return m_ToStringCache;
    }

    /// <summary>
    /// Gets the character span for the <see cref="LiteralText"/>, with escaped characters converted.
    /// Note: The <see cref="Parser"/> puts each escaped character of an input string
    /// into its own <see cref="LiteralText"/> item.
    /// </summary>
    /// <returns>The character span for the <see cref="LiteralText"/>, with escaped characters converted.</returns>
    public override ReadOnlySpan<char> AsSpan()
    {
        if (Length == 0) return ReadOnlySpan<char>.Empty;

        // The buffer is only for 1 character - each escaped char goes into its own LiteralText
        return SmartSettings.Parser.ConvertCharacterStringLiterals &&
            BaseString.AsSpan(StartIndex)[0] == SmartSettings.Parser.CharLiteralEscapeChar
            ? EscapedLiteral.UnEscapeCharLiterals(SmartSettings.Parser.CharLiteralEscapeChar,
            BaseString.AsSpan(StartIndex, Length),
            false, new char[1])
            : BaseString.AsSpan(StartIndex, Length);
    }

    /// <summary>
    /// Clears the <see cref="LiteralText"/> item.
    /// This method gets called by <see cref="LiteralTextPool"/> when it releases an instance.
    /// </summary>
    public override void Clear()
    {
        base.Clear();
        m_ToStringCache = null;
    }
}
