// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;

namespace Unity.SmartStrings.Utilities;

/// <summary>
/// Wraps a delegate so that it can be used as a parameter
/// to any string-formatting method (such as <see cref="string.Format(string, object)" />).
/// For example:
/// <code>
/// var textWithLink = string.Format("Please click on {0:this link}.", new FormatDelegate((text) => Html.ActionLink(text, "SomeAction"));
/// </code>
/// </summary>
public class FormatDelegate : IFormattable
{
    readonly Func<string, string> m_GetFormat1;
    readonly Func<string, IFormatProvider, string> m_GetFormat2;

    /// <summary>
    /// Creates a format delegate that wraps a function taking the format string.
    /// </summary>
    /// <param name="getFormat">Function that returns the formatted output for a given format string.</param>
    public FormatDelegate(Func<string, string> getFormat)
    {
        m_GetFormat1 = getFormat;
    }

    /// <summary>
    /// Creates a format delegate that wraps a function taking the format string and a format provider.
    /// </summary>
    /// <param name="getFormat">Function that returns the formatted output for a given format string and format provider.</param>
    public FormatDelegate(Func<string, IFormatProvider, string> getFormat)
    {
        m_GetFormat2 = getFormat;
    }

    /// <summary>
    /// Implements <see cref="IFormattable"/>.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="formatProvider"></param>
    /// <returns></returns>
    string IFormattable.ToString(string format, IFormatProvider formatProvider)
    {
        if (m_GetFormat1 != null) return m_GetFormat1(format);
        if (m_GetFormat2 != null) return m_GetFormat2(format, formatProvider);
        return string.Empty;
    }
}
