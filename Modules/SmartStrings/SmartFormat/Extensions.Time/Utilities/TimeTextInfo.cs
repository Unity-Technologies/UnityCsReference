// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// 
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Unity.SmartStrings.Utilities;

namespace Unity.SmartStrings.Extensions.Time.Utilities;

/// <summary>
/// Supplies the localized text used for <see cref="TimeSpan"/> formatting.
/// </summary>
class TimeTextInfo
{
    /// <summary>
    /// The delegate for the plural rule the corresponds to the language of this <see cref="TimeTextInfo"/>.
    /// </summary>
    public PluralRules.PluralRuleDelegate PluralRule { get; set; }

    /// <summary>
    /// Plural text for "d".
    /// </summary>
    public string[] Ptxt_d { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Plural text for "day".
    /// </summary>
    public string[] Ptxt_day { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "h".
    /// </summary>
    public string[] Ptxt_h { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "hour".
    /// </summary>
    public string[] Ptxt_hour { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Text for "less than".
    /// </summary>
    public string Ptxt_lessThan { get; set; } = string.Empty;

    /// <summary>
    /// Plural text for "m".
    /// </summary>
    public string[] Ptxt_m { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "millisecond".
    /// </summary>
    public string[] Ptxt_millisecond { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "minute".
    /// </summary>
    public string[] Ptxt_minute { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "ms".
    /// </summary>
    public string[] Ptxt_ms { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "s".
    /// </summary>
    public string[] Ptxt_s { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "second".
    /// </summary>
    public string[] Ptxt_second { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "w".
    /// </summary>
    public string[] Ptxt_w { get; set;} = Array.Empty<string>();

    /// <summary>
    /// Plural text for "week".
    /// </summary>
    public string[] Ptxt_week { get; set;} = Array.Empty<string>();

    static string GetValue(PluralRules.PluralRuleDelegate pluralRule, long value, IReadOnlyList<string> units)
    {
        // Get the plural index from the plural rule,
        // unless there's only 1 unit in the first place:
        var pluralIndex = units.Count == 1 ? 0 : pluralRule(value, units.Count);
        return string.Format(units[pluralIndex], value);
    }

    /// <summary>
    /// Gets the "less than" text for the given threshold.
    /// </summary>
    /// <param name="minimumValue"></param>
    /// <returns>The "less than" text for the given threshold.</returns>
    public virtual string GetLessThanText(string minimumValue)
    {
        return string.Format(Ptxt_lessThan, minimumValue);
    }

    /// <summary>
    /// Gets the text for <see cref="TimeSpanFormatOptions"/> ranges,
    /// that correspond to a certain value.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="value"></param>
    /// <param name="abbr"></param>
    /// <returns>
    /// The text for <see cref="TimeSpanFormatOptions"/> ranges,
    /// that correspond to a certain value.
    /// </returns>
    public virtual string GetUnitText(TimeSpanFormatOptions unit, long value, bool abbr)
    {
        if (PluralRule == null) throw new InvalidOperationException("Plural rule delegate must not be null");

        return unit switch
        {
            TimeSpanFormatOptions.RangeWeeks => GetValue(PluralRule, value, abbr ? Ptxt_w : Ptxt_week),
            TimeSpanFormatOptions.RangeDays => GetValue(PluralRule, value, abbr ? Ptxt_d : Ptxt_day),
            TimeSpanFormatOptions.RangeHours => GetValue(PluralRule, value, abbr ? Ptxt_h : Ptxt_hour),
            TimeSpanFormatOptions.RangeMinutes => GetValue(PluralRule, value, abbr ? Ptxt_m : Ptxt_minute),
            TimeSpanFormatOptions.RangeSeconds => GetValue(PluralRule, value, abbr ? Ptxt_s : Ptxt_second),
            TimeSpanFormatOptions.RangeMilliSeconds => GetValue(PluralRule, value, abbr ? Ptxt_ms : Ptxt_millisecond),
            // (should be unreachable)
            _ => string.Empty
        };
    }
}
