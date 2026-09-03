// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: UIToolkitFramework not yet converted
//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using Unity.Scripting.LifecycleManagement;
using System.Text;

namespace Unity.SmartStrings.Extensions.Time.Utilities;

/// <summary>
/// Utility class to format a <see cref="TimeSpan"/> as a <see langword="string"/>.
/// </summary>
[NoAutoStaticsCleanup] // plain-value formatting state and options; holds no references to reloadable code
static class TimeSpanUtility
{
    static TimeSpanFormatOptions s_RangeMin;
    static TimeSpanFormatOptions s_Truncate;
    static bool s_LessThan;
    static bool s_Abbreviate;
    static Func<double, double> s_Round;
    static TimeTextInfo s_TimeTextInfo;

    static TimeSpanUtility()
    {
        // Create our defaults:
        DefaultFormatOptions =
            TimeSpanFormatOptions.AbbreviateOff
            | TimeSpanFormatOptions.LessThan
            | TimeSpanFormatOptions.TruncateAuto
            | TimeSpanFormatOptions.RangeSeconds
            | TimeSpanFormatOptions.RangeDays;
        AbsoluteDefaults = DefaultFormatOptions;
    }

    /// <summary>
    /// <para>Turns a TimeSpan into a human-readable text.</para>
    /// <para>Uses the specified timeSpanFormatOptions.</para>
    /// <para>For example: "31.23:59:00.555" = "31 days 23 hours 59 minutes 0 seconds 555 milliseconds"</para>
    /// </summary>
    /// <param name="fromTime"></param>
    /// <param name="options">
    /// <para>A combination of flags that determine the formatting options.</para>
    /// <para>These will be combined with the default timeSpanFormatOptions.</para>
    /// </param>
    /// <param name="timeTextInfo">An object that supplies the text to use for output</param>
    public static string ToTimeString(this TimeSpan fromTime, TimeSpanFormatOptions options,
        TimeTextInfo timeTextInfo)
    {
        // If there are any missing options, merge with the defaults:
        // Also, as a safeguard against missing DefaultFormatOptions, let's also merge with the AbsoluteDefaults:
        options = options.Merge(DefaultFormatOptions).Merge(AbsoluteDefaults);

        // Extract the individual options:
        var rangeMax = TimeSpanFormatOptions.None;
        s_RangeMin = TimeSpanFormatOptions.None;
        var hasRange = false;
        foreach (var flag in options.Mask(TimeSpanFormatOptionsPresets.Range).AllFlags())
        {
            if (!hasRange)
            {
                s_RangeMin = flag;
                hasRange = true;
            }

            rangeMax = flag;
        }

        s_Truncate = TimeSpanFormatOptions.None;
        foreach (var flag in options.Mask(TimeSpanFormatOptionsPresets.Truncate).AllFlags())
        {
            s_Truncate = flag;
            break;
        }
        s_LessThan = options.Mask(TimeSpanFormatOptionsPresets.LessThan) != TimeSpanFormatOptions.LessThanOff;
        s_Abbreviate = options.Mask(TimeSpanFormatOptionsPresets.Abbreviate) != TimeSpanFormatOptions.AbbreviateOff;
        s_Round = s_LessThan ? (Func<double, double>)Math.Floor : Math.Ceiling;
        s_TimeTextInfo = timeTextInfo;

        switch (s_RangeMin)
        {
            case TimeSpanFormatOptions.RangeWeeks:
                fromTime = TimeSpan.FromDays(s_Round(fromTime.TotalDays / 7) * 7);
                break;
            case TimeSpanFormatOptions.RangeDays:
                fromTime = TimeSpan.FromDays(s_Round(fromTime.TotalDays));
                break;
            case TimeSpanFormatOptions.RangeHours:
                fromTime = TimeSpan.FromHours(s_Round(fromTime.TotalHours));
                break;
            case TimeSpanFormatOptions.RangeMinutes:
                fromTime = TimeSpan.FromMinutes(s_Round(fromTime.TotalMinutes));
                break;
            case TimeSpanFormatOptions.RangeSeconds:
                fromTime = TimeSpan.FromSeconds(s_Round(fromTime.TotalSeconds));
                break;
            case TimeSpanFormatOptions.RangeMilliSeconds:
                fromTime = TimeSpan.FromMilliseconds(s_Round(fromTime.TotalMilliseconds));
                break;
        }

        // Create our result:
        var textStarted = false;
        var result = new StringBuilder();
        for (var i = rangeMax; i >= s_RangeMin; i = (TimeSpanFormatOptions)((int)i >> 1))
        {
            // Determine the value and title:
            int value;
            switch (i)
            {
                case TimeSpanFormatOptions.RangeWeeks:
                    value = (int)Math.Floor(fromTime.TotalDays / 7);
                    fromTime -= TimeSpan.FromDays(value * 7);
                    break;
                case TimeSpanFormatOptions.RangeDays:
                    value = (int)Math.Floor(fromTime.TotalDays);
                    fromTime -= TimeSpan.FromDays(value);
                    break;
                case TimeSpanFormatOptions.RangeHours:
                    value = (int)Math.Floor(fromTime.TotalHours);
                    fromTime -= TimeSpan.FromHours(value);
                    break;
                case TimeSpanFormatOptions.RangeMinutes:
                    value = (int)Math.Floor(fromTime.TotalMinutes);
                    fromTime -= TimeSpan.FromMinutes(value);
                    break;
                case TimeSpanFormatOptions.RangeSeconds:
                    value = (int)Math.Floor(fromTime.TotalSeconds);
                    fromTime -= TimeSpan.FromSeconds(value);
                    break;
                case TimeSpanFormatOptions.RangeMilliSeconds:
                    value = (int)Math.Floor(fromTime.TotalMilliseconds);
                    fromTime -= TimeSpan.FromMilliseconds(value);
                    break;
                default:
                    // Should never happen. Ensures 'value' and 'fromTime' are always set.
                    continue;
            }

            //Determine whether to display this value
            if (!ShouldTruncate(value, textStarted, out var displayThisValue)) continue;

            PrepareOutput(value, i == s_RangeMin, textStarted, result, ref displayThisValue);

            // Output the value:
            if (displayThisValue)
            {
                if (textStarted) result.Append(' ');
                var unitTitle = s_TimeTextInfo.GetUnitText(i, value, s_Abbreviate);
                result.Append(unitTitle);
                textStarted = true;
            }
        }

        return result.ToString();
    }

    static bool ShouldTruncate(int value, bool textStarted, out bool displayThisValue)
    {
        displayThisValue = false;
        switch (s_Truncate)
        {
            case TimeSpanFormatOptions.TruncateShortest:
                if (textStarted) return false; // continue with next for
                if (value > 0) displayThisValue = true;
                return true;
            case TimeSpanFormatOptions.TruncateAuto:
                if (value > 0) displayThisValue = true;
                return true;
            case TimeSpanFormatOptions.TruncateFill:
                if (textStarted || value > 0) displayThisValue = true;
                return true;
            case TimeSpanFormatOptions.TruncateFull:
                displayThisValue = true;
                return true;
        }

        // Should never happen
        return false;
    }

    static void PrepareOutput(int value, bool isRangeMin, bool hasTextStarted, StringBuilder result, ref bool displayThisValue)
    {
        // we need to display SOMETHING (even if it's zero)
        if (isRangeMin && !hasTextStarted)
        {
            displayThisValue = true;
            if (s_LessThan && value < 1)
            {
                // Output the "less than 1 unit" text:
                var unitTitle = s_TimeTextInfo !.GetUnitText(s_RangeMin, 1, s_Abbreviate);
                result.Append(s_TimeTextInfo.GetLessThanText(unitTitle));
                displayThisValue = false;
            }
        }
    }

    /// <summary>
    /// These are the default options that will be used when no option is specified.
    /// </summary>
    public static TimeSpanFormatOptions DefaultFormatOptions { get; set; }

    /// <summary>
    /// These are the absolute default options that will be used as
    /// a safeguard, just in case DefaultFormatOptions is missing a value.
    /// </summary>
    public static TimeSpanFormatOptions AbsoluteDefaults { get; }

    /// <summary>
    /// <para>Returns the <see cref="TimeSpan"/> closest to the specified interval.</para>
    /// <para>For example: <c>Round("00:57:00", TimeSpan.TicksPerMinute * 5) =&gt; "00:55:00"</c></para>
    /// </summary>
    /// <param name="fromTime">A <see cref="TimeSpan"/> to be rounded.</param>
    /// <param name="intervalTicks">Specifies the interval for rounding. Use <c>TimeSpan.TicksPer...</c> constants.</param>
    public static TimeSpan Round(this TimeSpan fromTime, long intervalTicks)
    {
        var extra = fromTime.Ticks % intervalTicks;
        if (extra >= intervalTicks >> 1) extra -= intervalTicks;
        return TimeSpan.FromTicks(fromTime.Ticks - extra);
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
