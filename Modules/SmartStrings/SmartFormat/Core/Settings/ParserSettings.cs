// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using Unity.SmartStrings.Core.Parsing;

namespace Unity.SmartStrings.Core.Settings;

/// <summary>
/// Class for <see cref="Parser"/> settings.
/// Properties should be considered as 'init-only' like implemented in C# 9.
/// Any changes after passing settings as argument to CTORs may not have effect.
/// </summary>
[Serializable]
public class ParserSettings
{
    readonly List<char> m_AlphanumericSelectorChars = new List<char>("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_-");

    [SerializeField] ParseErrorAction m_ErrorAction = ParseErrorAction.ThrowError;

    [Tooltip(@"When enabled, character string literals will be converted, for example `\t` will become a TAB character.")]
    [SerializeField] bool m_ConvertCharacterStringLiterals = true;

    [SerializeField] List<char> m_CustomSelectorChars = new List<char>();
    [SerializeField] List<char> m_CustomOperatorChars = new List<char>();

    [Tooltip("Which characters a selector can contain: letters, digits, underscores and hyphens, or characters from any script.")]
    [SerializeField] SelectorFilterType m_SelectorCharFilter = SelectorFilterType.Alphanumeric;

    // Immutable char table, populated once from literals, so nothing user-defined can be pinned across a code reload.
    [NoAutoStaticsCleanup]
    static readonly char[] k_NonVisualUnicodeCharacters =
    {
        // Control characters
        '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007',
        '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F',
        '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017',
        '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F', '\u007F',
        '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087',
        '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
        '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097',
        '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
        // Format and directional formatting characters, and the invisible separator
        '\u200B', '\u200C', '\u200D', '\u2060', '\uFEFF',
        '\u202A', '\u202B', '\u202C', '\u202D', '\u202E', '\u2066', '\u2067', '\u2068', '\u2069', '\u2063',
        // Common combining marks
        '\u0300', '\u0301', '\u0302', '\u0308',
        // Whitespace without a glyph
        '\u00A0', '\u1680', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006',
        '\u2007', '\u2008', '\u2009', '\u200A', '\u202F', '\u205F', '\u3000',
        // Line and paragraph separators
        '\u2028', '\u2029'
    };

    /// <summary>
    /// Behavior that the <see cref="Parser" /> applies when a parsing error occurs.
    /// The default is <see cref="ParseErrorAction.ThrowError"/>.
    /// </summary>
    public ParseErrorAction ErrorAction { get => m_ErrorAction; set => m_ErrorAction = value; }

    /// <summary>
    /// Gets or sets which characters a selector can contain.
    /// </summary>
    /// <remarks>
    /// The default, <see cref="SelectorFilterType.Alphanumeric"/>, allows letters a to z in either case, digits,
    /// underscores and hyphens, plus characters added with <see cref="AddCustomSelectorChars"/>.
    /// <see cref="SelectorFilterType.VisualUnicodeChars"/> allows characters from any script. Set this before
    /// you create the <see cref="Parser"/> or <see cref="SmartFormatter"/> that uses these settings.
    /// </remarks>
    public SelectorFilterType SelectorCharFilter { get => m_SelectorCharFilter; set => m_SelectorCharFilter = value; }

    /// <summary>
    /// Gets the selector characters: an allowlist for <see cref="SelectorFilterType.Alphanumeric"/>,
    /// or a blocklist for <see cref="SelectorFilterType.VisualUnicodeChars"/>.
    /// </summary>
    internal CharSet GetSelectorChars()
    {
        if (m_SelectorCharFilter == SelectorFilterType.Alphanumeric)
        {
            var allowlist = new CharSet(m_AlphanumericSelectorChars) { IsAllowList = true };
            allowlist.AddRange(OperatorChars());
            allowlist.AddRange(m_CustomSelectorChars);
            return allowlist;
        }

        var blocklist = new CharSet(DisallowedSelectorChars());
        blocklist.AddRange(m_CustomOperatorChars);
        blocklist.AddRange(k_NonVisualUnicodeCharacters.AsSpan());
        foreach (var c in m_CustomSelectorChars)
            blocklist.Remove(c);
        return blocklist;
    }

    /// <summary>
    /// The list of standard selector characters.
    /// </summary>
    internal List<char> SelectorChars() => m_AlphanumericSelectorChars;

    /// <summary>
    /// Gets a read-only list of the custom selector characters, which were set with <see cref="AddCustomSelectorChars"/>.
    /// </summary>
    internal List<char> CustomSelectorChars() => m_CustomSelectorChars;

    /// <summary>
    /// Gets a list of characters which are allowed in a selector.
    /// </summary>
    internal List<char> DisallowedSelectorChars()
    {
        var chars = new List<char>
        {
            CharLiteralEscapeChar, FormatterNameSeparator, AlignmentOperator, SelectorOperator,
            PlaceholderBeginChar, PlaceholderEndChar, FormatterOptionsBeginChar, FormatterOptionsEndChar
        };
        chars.AddRange(OperatorChars());
        return chars;
    }

    /// <summary>
    /// Gets a read-only list of the custom operator characters, which were set with <see cref="AddCustomSelectorChars"/>.
    /// Contiguous operator characters are parsed as one operator (e.g. '?.').
    /// </summary>
    internal List<char> CustomOperatorChars() => m_CustomOperatorChars;

    /// <summary>
    /// Adds a list of allowable selector characters on top of the <see cref="SelectorChars"/> setting.
    /// This can be useful to support additional selector syntax such as math.
    /// Characters in <see cref="DisallowedSelectorChars"/> cannot be added.
    /// Operator chars and selector chars must be different.
    /// </summary>
    /// <param name="characters">Selector characters to allow in addition to the standard set.</param>
    public void AddCustomSelectorChars(IList<char> characters)
    {
        foreach (var c in characters)
        {
            if (DisallowedSelectorChars().Contains(c) || m_CustomOperatorChars.Contains(c))
                throw new ArgumentException($"Cannot add '{c}' as a custom selector character. It is disallowed or in use as an operator.");

            if (!m_CustomSelectorChars.Contains(c) && !m_AlphanumericSelectorChars.Contains(c))
                m_CustomSelectorChars.Add(c);
        }
    }

    /// <summary>
    /// Adds a list of allowable operator characters on top of the standard <see cref="OperatorChars"/> setting.
    /// Operator chars and selector chars must be different.
    /// </summary>
    /// <param name="characters">Operator characters to allow in addition to the standard set.</param>
    public void AddCustomOperatorChars(IList<char> characters)
    {
        foreach (var c in characters)
        {
            if ((!OperatorChars().Contains(c) && DisallowedSelectorChars().Contains(c)) ||
                SelectorChars().Contains(c) || CustomSelectorChars().Contains(c))
                throw new ArgumentException($"Cannot add '{c}' as a custom operator character. It is disallowed or in use as a selector.");

            if (!OperatorChars().Contains(c) && !CustomOperatorChars().Contains(c))
                m_CustomOperatorChars.Add(c);
        }
    }

    /// <summary>
    /// This setting is relevant for the <see cref="LiteralText" />.
    /// If <see langword="true"/> (the default), character string literals are treated like in "normal" string.Format:
    /// string.Format("\t")   will return a "TAB" character
    /// If <see langword="false"/>, character string literals are not converted, just like with this string.Format:
    /// string.Format(@"\t")  will return the 2 characters "\" and "t"
    /// </summary>
    public bool ConvertCharacterStringLiterals { get => m_ConvertCharacterStringLiterals; set => m_ConvertCharacterStringLiterals = value; }

    /// <summary>
    /// The character literal escape character for <see cref="PlaceholderBeginChar"/> and <see cref="PlaceholderEndChar"/>,
    /// but also others like for \t (TAB), \n (NEW LINE), \\ (BACKSLASH) and others defined in <see cref="EscapedLiteral"/>.
    /// </summary>
    internal char CharLiteralEscapeChar { get; } = '\\';

    /// <summary>
    /// The character which separates the formatter name (if any exists) from other parts of the placeholder.
    /// E.g.: {Variable:FormatterName:argument} or {Variable:FormatterName}
    /// </summary>
    internal char FormatterNameSeparator { get; } = ':';

    /// <summary>
    /// The standard operator characters.
    /// Contiguous operator characters are parsed as one operator (e.g. '?.').
    /// </summary>
    internal List<char> OperatorChars() => new()
    { SelectorOperator, NullableOperator, AlignmentOperator, ListIndexBeginChar, ListIndexEndChar };

    /// <summary>
    /// The character which separates the selector for alignment. <c>E.g.: Smart.Format("Name: {name,10}")</c>
    /// </summary>
    internal char AlignmentOperator { get; } = ',';

    /// <summary>
    /// The character which separates two or more selectors <c>E.g.: "First.Second.Third"</c>
    /// </summary>
    internal char SelectorOperator { get; } = '.';

    /// <summary>
    /// The character which flags the selector as <see langword="nullable"/>.
    /// The character after <see cref="NullableOperator"/> must be the <see cref="SelectorOperator"/>.
    /// <c>E.g.: "First?.Second"</c>
    /// </summary>
    internal char NullableOperator { get; } = '?';

    /// <summary>
    /// Gets the character indicating the start of a <see cref="Placeholder"/>.
    /// </summary>
    internal char PlaceholderBeginChar { get; } = '{';

    /// <summary>
    /// Gets the character indicating the end of a <see cref="Placeholder"/>.
    /// </summary>
    internal char PlaceholderEndChar { get; } = '}';

    /// <summary>
    /// Gets the character indicating the begin of formatter options.
    /// </summary>
    internal char FormatterOptionsBeginChar { get; } = '(';

    /// <summary>
    /// Gets the character indicating the end of formatter options.
    /// </summary>
    internal char FormatterOptionsEndChar { get; } = ')';

    /// <summary>
    /// Gets the character indicating the begin of a list index, like in "{Numbers[0]}"
    /// </summary>
    internal char ListIndexBeginChar { get; } = '[';

    /// <summary>
    /// Gets the character indicating the end of a list index, like in "{Numbers[0]}"
    /// </summary>
    internal char ListIndexEndChar { get; } = ']';

    /// <summary>
    /// Characters which terminate parsing of format options.
    /// To use them as options, they must be escaped (preceded) by the <see cref="CharLiteralEscapeChar"/>.
    /// </summary>
    internal List<char> FormatOptionsTerminatorChars() => new()
    {
        FormatterNameSeparator,
        FormatterOptionsBeginChar,
        FormatterOptionsEndChar,
        PlaceholderBeginChar,
        PlaceholderEndChar
    };
}
