// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Globalization;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Formatting;
using Unity.SmartStrings.Extensions.Time.Utilities;
using Unity.SmartStrings.Utilities;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// A formatter that outputs <see cref="TimeSpan"/> values as human-readable text.
/// </summary>
[Serializable]
public class TimeFormatter : FormatterBase
{
    [SerializeField]
    TimeSpanFormatOptions m_DefaultFormatOptions = TimeSpanUtility.DefaultFormatOptions;

    string m_FallbackLanguage = "en";

    /// <inheritdoc/>
    public override string DefaultName => "time";

    /// <summary>
    /// The options that control how time values are formatted.
    /// </summary>
    public TimeSpanFormatOptions DefaultFormatOptions { get => m_DefaultFormatOptions; set => m_DefaultFormatOptions = value; }

    /// <summary>
    /// Initializes the extension with a default <see cref="TimeTextInfo"/>.
    /// </summary>
    /// <remarks>
    /// Culture is determined in this sequence:<br/>
    /// 1. Get the culture from the <see cref="FormattingInfo.FormatterOptions"/>.<br/>
    /// 2. Get the culture from the <see cref="IFormatProvider"/> argument (which may be a <see cref="CultureInfo"/>) to <see cref="SmartFormatter.Format(IFormatProvider, string, object?[])"/><br/>
    /// 3. Get the culture from the selected locale.
    /// 4. The <see cref="CultureInfo.CurrentUICulture"/>.<br/><br/>
    /// <see cref="TimeFormatter"/> makes use of <see cref="PluralRules"/> and <see cref="PluralLocalizationFormatter"/>.
    /// </remarks>
    public TimeFormatter()
    {
        DefaultFormatOptions = TimeSpanUtility.DefaultFormatOptions;
    }

    /// <summary>
    /// The fallback language used when no supported language is found.
    /// Default is "en". If no fallback language shall be used, set it to <see cref="string.Empty"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if no <see cref="TimeTextInfo"/> could be found for the language.</exception>
    public string FallbackLanguage
    {
        get
        {
            return m_FallbackLanguage;
        }

        set
        {
            if (value == string.Empty)
                m_FallbackLanguage = value;
            else if (CommonLanguagesTimeTextInfo.GetTimeTextInfo(value) != null)
                m_FallbackLanguage = value;
            else
                throw new ArgumentException($"No {nameof(TimeTextInfo)} found for language '{value}'.");
        }
    }
    ///<inheritdoc />
    public override bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var format = formattingInfo.Format;
        var formatterName = formattingInfo.Placeholder?.FormatterName ?? string.Empty;
        var current = formattingInfo.CurrentValue;

        // Check whether arguments can be handled by this formatter
        if (format is {HasNested : true})
        {
            // Auto detection calls just return a failure to evaluate
            if (formatterName == string.Empty)
                return false;

            // throw, if the formatter has been called explicitly
            throw new FormatException($"Formatter named '{formatterName}' cannot handle nested formats.");
        }

        var options = formattingInfo.FormatterOptions.Trim();
        var formatText = format?.RawText.Trim() ?? string.Empty;

        // Not clear, whether we can process this format
        if (formatterName == string.Empty && options == string.Empty && formatText == string.Empty) return false;

        // In SmartFormat 2.x, the format could be included in options, with empty format.
        // Using compatibility with v2, there is no reliable way to set a language as an option
        var v2Compatibility = options != string.Empty && formatText == string.Empty;
        var formattingOptions = v2Compatibility ? options : formatText;

        var fromTime = GetFromTime(current, formattingOptions);

        if (fromTime is null)
        {
            // Auto detection calls just return a failure to evaluate
            if (formatterName == string.Empty)
                return false;

            // throw, if the formatter has been called explicitly
            throw new FormatException(
                $"Formatter named '{formatterName}' can only process types of {nameof(TimeSpan)}, {nameof(DateTime)}, {nameof(DateTimeOffset)}");
        }

        var timeTextInfo = GetTimeTextInfo(formattingInfo, v2Compatibility);

        var timeSpanFormatOptions = TimeSpanFormatOptionsConverter.Parse(v2Compatibility ? options : formatText);
        var timeString = fromTime.Value.ToTimeString(timeSpanFormatOptions, timeTextInfo);
        formattingInfo.Write(timeString);
        return true;
    }

    static TimeSpan? GetFromTime(object current, string formattingOptions)
    {
        TimeSpan? fromTime = null;

        switch (current)
        {
            case TimeSpan timeSpan:
                fromTime = timeSpan;
                break;
            case DateTime dateTime:
                if (formattingOptions != string.Empty)
                {
                    fromTime = SystemTime.Now().ToUniversalTime().Subtract(dateTime.ToUniversalTime());
                }
                break;
            case DateTimeOffset dateTimeOffset:
                if (formattingOptions != string.Empty)
                {
                    fromTime = SystemTime.OffsetNow().UtcDateTime.Subtract(dateTimeOffset.UtcDateTime);
                }
                break;
        }

        return fromTime;
    }

    TimeTextInfo GetTimeTextInfo(IFormattingInfo formattingInfo, bool v2Compatibility)
    {
        // See if the provider can give us a TimeTextInfo:
        if (formattingInfo.FormatDetails.Provider?.GetFormat(typeof(TimeTextInfo)) is TimeTextInfo timeTextInfo) return timeTextInfo;

        // Figure out the culture to use
        var culture = GetCultureInfo(formattingInfo, v2Compatibility);
        // See if there is a rule for this culture:
        var timeTextInfoFromCulture = CommonLanguagesTimeTextInfo.GetTimeTextInfo(culture.TwoLetterISOLanguageName);

        if (timeTextInfoFromCulture != null) return timeTextInfoFromCulture;

        if (timeTextInfoFromCulture is null && FallbackLanguage == string.Empty)
            throw new FormattingException(formattingInfo.Placeholder, $"{nameof(TimeTextInfo)} could not be found for the given culture argument '{formattingInfo.FormatterOptions}'.", 0);

        if (FallbackLanguage != string.Empty)
            return CommonLanguagesTimeTextInfo.GetTimeTextInfo(FallbackLanguage) !;

        throw new ArgumentException($"{nameof(TimeTextInfo)} could not be found for the given {nameof(IFormatProvider)}.", nameof(formattingInfo));
    }

    static CultureInfo GetCultureInfo(IFormattingInfo formattingInfo, bool v2Compatibility)
    {
        var culture = !v2Compatibility? formattingInfo.FormatterOptions.Trim() : string.Empty;
        CultureInfo cultureInfo;
        if (culture == string.Empty)
        {
            if (formattingInfo.FormatDetails.Provider is CultureInfo ci)
                cultureInfo = ci;
            else
                cultureInfo = CultureInfo.CurrentUICulture;
        }
        else
        {
            cultureInfo = CultureInfo.GetCultureInfo(culture);
        }

        return cultureInfo;
    }
}
