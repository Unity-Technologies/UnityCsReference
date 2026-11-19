// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Unity.SmartStrings.Extensions.Time.Utilities;

internal static class TimeSpanFormatOptionsConverter
{
    static readonly Regex parser =
        new Regex(
            @"\b(w|week|weeks|d|day|days|h|hour|hours|m|minute|minutes|s|second|seconds|ms|millisecond|milliseconds|auto|short|fill|full|abbr|noabbr|less|noless)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static TimeSpanFormatOptions Merge(this TimeSpanFormatOptions left, TimeSpanFormatOptions right)
    {
        var masks = new[]
        {
            TimeSpanFormatOptionsPresets.Abbreviate,
            TimeSpanFormatOptionsPresets.LessThan,
            TimeSpanFormatOptionsPresets.Range,
            TimeSpanFormatOptionsPresets.Truncate
        };
        foreach (var mask in masks)
            if ((left & mask) == 0)
                left |= right & mask;
        return left;
    }

    public static TimeSpanFormatOptions Mask(this TimeSpanFormatOptions timeSpanFormatOptions,
        TimeSpanFormatOptions mask)
    {
        return timeSpanFormatOptions & mask;
    }

    public static IEnumerable<TimeSpanFormatOptions> AllFlags(this TimeSpanFormatOptions timeSpanFormatOptions)
    {
        uint value = 0x1;
        while (value <= (uint)timeSpanFormatOptions)
        {
            if ((value & (uint)timeSpanFormatOptions) != 0) yield return (TimeSpanFormatOptions)value;
            value <<= 1;
        }
    }

    public static TimeSpanFormatOptions Parse(string formatString)
    {
        formatString = formatString.ToLower();

        var t = TimeSpanFormatOptions.None;
        foreach (Match m in parser.Matches(formatString))
            switch (m.Value)
            {
                case "w":
                case "week":
                case "weeks":
                    t |= TimeSpanFormatOptions.RangeWeeks;
                    break;
                case "d":
                case "day":
                case "days":
                    t |= TimeSpanFormatOptions.RangeDays;
                    break;
                case "h":
                case "hour":
                case "hours":
                    t |= TimeSpanFormatOptions.RangeHours;
                    break;
                case "m":
                case "minute":
                case "minutes":
                    t |= TimeSpanFormatOptions.RangeMinutes;
                    break;
                case "s":
                case "second":
                case "seconds":
                    t |= TimeSpanFormatOptions.RangeSeconds;
                    break;
                case "ms":
                case "millisecond":
                case "milliseconds":
                    t |= TimeSpanFormatOptions.RangeMilliSeconds;
                    break;


                case "short":
                    t |= TimeSpanFormatOptions.TruncateShortest;
                    break;
                case "auto":
                    t |= TimeSpanFormatOptions.TruncateAuto;
                    break;
                case "fill":
                    t |= TimeSpanFormatOptions.TruncateFill;
                    break;
                case "full":
                    t |= TimeSpanFormatOptions.TruncateFull;
                    break;


                case "abbr":
                    t |= TimeSpanFormatOptions.Abbreviate;
                    break;
                case "noabbr":
                    t |= TimeSpanFormatOptions.AbbreviateOff;
                    break;


                case "less":
                    t |= TimeSpanFormatOptions.LessThan;
                    break;
                case "noless":
                    t |= TimeSpanFormatOptions.LessThanOff;
                    break;
            }

        return t;
    }
}
