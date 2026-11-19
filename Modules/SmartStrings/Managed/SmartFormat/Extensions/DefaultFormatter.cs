// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using Unity.SmartStrings.Core.Extensions;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Performs default formatting, using the same logic as <c>string.Format</c>.
/// </summary>
[Serializable]
public class DefaultFormatter : FormatterBase
{
    /// <inheritdoc/>
    public override string DefaultName => "d";

    /// <summary>
    /// Creates a new instance of the formatter.
    /// </summary>
    public DefaultFormatter()
    {
        CanAutoDetect = true;
    }

    /// <inheritdoc/>
    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var format = formattingInfo.Format;
        var current = formattingInfo.CurrentValue;

        // If the format has nested placeholders, we process those first
        // instead of formatting the item.
        if (format is { HasNested : true })
        {
            formattingInfo.FormatAsChild(format, current);
            return true;
        }

        // Use the provider to see if a CustomFormatter is available:
        var provider = formattingInfo.FormatDetails.Provider;

        //  We will try using IFormatProvider, IFormattable, and if all else fails, ToString.
        string result;
        if (provider?.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter cFormatter)
        {
            var formatText = format?.GetLiteralText();
            result = cFormatter.Format(formatText, current, provider);
        }
        // IFormattable
        // Note: This is what ValueStringBuilder is implementing in the same way
        else if (current is IFormattable formattable)
        {
            var formatText = format?.ToString();
            result = formattable.ToString(formatText, provider);
        }
        else if (current is string str)
        {
            formattingInfo.Write(str.AsSpan());
            return true;
        }
        // ToString:
        else
        {
            result = current?.ToString();
        }

        // Output the result:
        formattingInfo.Write(result != null ? result.AsSpan() : ReadOnlySpan<char>.Empty);

        return true;
    }
}
