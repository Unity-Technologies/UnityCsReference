// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

namespace Unity.SmartStrings.Core.Settings;

/// <summary>
/// Sets which characters a selector can contain.
/// </summary>
/// <remarks>
/// Assign a value to <see cref="ParserSettings.SelectorCharFilter"/>. Characters with a special meaning in a
/// format string, <c>{}[]()\.?,:</c>, never count as selector characters. Use
/// <see cref="ParserSettings.AddCustomSelectorChars"/> to allow further characters with
/// <see cref="Alphanumeric"/>, or to allow blocked characters again with <see cref="VisualUnicodeChars"/>.
/// </remarks>
/// <example>
/// <code source="../../../../../Modules/SmartStrings/Tests/UTFTests/SmartFormat.Samples/UnicodeSelectorExample.cs"/>
/// </example>
/// <seealso cref="ParserSettings.SelectorCharFilter"/>
/// <seealso cref="ParserSettings.AddCustomSelectorChars"/>
public enum SelectorFilterType
{
    /// <summary>
    /// Allows letters a to z in either case, digits, underscores and hyphens.
    /// </summary>
    /// <remarks>
    /// This is the default. Characters added with <see cref="ParserSettings.AddCustomSelectorChars"/> are allowed too.
    /// </remarks>
    Alphanumeric,

    /// <summary>
    /// Allows Unicode characters from any script.
    /// </summary>
    /// <remarks>
    /// Use this for selectors such as dictionary keys in Japanese. The parser allows the ordinary space. It rejects
    /// characters with a special meaning in a format string and this fixed set: control characters U+0000 to U+001F
    /// and U+007F to U+009F; spaces U+00A0, U+1680, U+2000 to U+200A, U+202F, U+205F and U+3000; line and paragraph
    /// separators U+2028 and U+2029; zero-width and text-direction characters U+200B to U+200D, U+202A to U+202E,
    /// U+2060, U+2063, U+2066 to U+2069 and U+FEFF; and combining accents U+0300, U+0301, U+0302 and U+0308. It
    /// allows every other character, including other invisible characters, such as U+200E, and other combining
    /// marks, which scripts such as Hindi need.
    /// </remarks>
    VisualUnicodeChars
}
