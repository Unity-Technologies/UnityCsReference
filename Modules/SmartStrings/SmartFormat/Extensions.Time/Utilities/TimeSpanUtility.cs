// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace Unity.SmartStrings.Extensions.Time.Utilities;

/// <summary>
/// Utility class to format a <see cref="TimeSpan"/> as a <see langword="string"/>.
/// </summary>
static class TimeSpanUtility
{
    // Scratch state for the in-progress ToTimeParts() call: each field is written near the top of that
    // call before it is read, so nothing meaningful survives the call, let alone a code reload.
    // s_TimeTextInfo only ever holds the value the caller passed in, so it cannot pin reloadable user code.
    [NoAutoStaticsCleanup]
    static TimeSpanFormatOptions s_RangeMin;
    [NoAutoStaticsCleanup]
    static TimeSpanFormatOptions s_Truncate;
    [NoAutoStaticsCleanup]
    static bool s_LessThan;
    [NoAutoStaticsCleanup]
    static bool s_Abbreviate;
    [NoAutoStaticsCleanup]
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
    /// Turns a TimeSpan into a human-readable text.
    /// Uses the specified timeSpanFormatOptions.
    /// For example: "31.23:59:00.555" = "31 days 23 hours 59 minutes 0 seconds 555 milliseconds"
    /// </summary>
    /// <param name="fromTime"></param>
    /// <param name="options">
    /// A combination of flags that determine the formatting options.
    /// These will be combined with the default timeSpanFormatOptions.
    /// </param>
    /// <param name="timeTextInfo">An object that supplies the text to use for output</param>
    public static string ToTimeString(this TimeSpan fromTime, TimeSpanFormatOptions options,
        TimeTextInfo timeTextInfo)
    {
        return string.Join(" ", fromTime.ToTimeParts(options, timeTextInfo));
    }

    /// <summary>
    /// Turns a TimeSpan into a list of human-readable text parts, one per unit.
    /// For example: "31.23:59:00.555" = "31 days", "23 hours", "59 minutes", "0 seconds", "555 milliseconds"
    /// </summary>
    /// <param name="fromTime"></param>
    /// <param name="options">
    /// A combination of flags that determine the formatting options.
    /// These will be combined with the default timeSpanFormatOptions.
    /// </param>
    /// <param name="timeTextInfo">An object that supplies the text to use for output</param>
    public static List<string> ToTimeParts(this TimeSpan fromTime, TimeSpanFormatOptions options,
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
        s_TimeTextInfo = timeTextInfo;

        // TimeSpan.MinValue cannot be negated, so it is formatted as TimeSpan.MaxValue
        var ticks = fromTime.Ticks;
        var isNegative = ticks < 0;
        if (isNegative)
            ticks = ticks == long.MinValue ? long.MaxValue : -ticks;

        var ticksPerRangeMin = TicksPerUnit(s_RangeMin);
        var remainingUnits = ticks / ticksPerRangeMin;
        if (!s_LessThan && ticks % ticksPerRangeMin != 0)
            remainingUnits++;
        var addSign = isNegative && remainingUnits > 0;

        // Create our result:
        var result = new List<string>();
        for (var i = rangeMax; i >= s_RangeMin; i = (TimeSpanFormatOptions)((int)i >> 1))
        {
            var unitsPerValue = TicksPerUnit(i) / ticksPerRangeMin;
            var value = remainingUnits / unitsPerValue;
            remainingUnits -= value * unitsPerValue;

            //Determine whether to display this value
            if (!ShouldTruncate(value, result.Count > 0, out var displayThisValue)) continue;

            PrepareOutput(value, i == s_RangeMin, result.Count > 0, result, ref displayThisValue);

            // Output the value:
            if (displayThisValue)
            {
                var unitTitle = s_TimeTextInfo.GetUnitText(i, value, s_Abbreviate);
                if (!string.IsNullOrEmpty(unitTitle)) result.Add(unitTitle);
            }
        }

        if (addSign && result.Count > 0)
            result[0] = "-" + result[0];

        return result;
    }

    static long TicksPerUnit(TimeSpanFormatOptions unit)
    {
        return unit switch
        {
            TimeSpanFormatOptions.RangeWeeks => TimeSpan.TicksPerDay * 7,
            TimeSpanFormatOptions.RangeDays => TimeSpan.TicksPerDay,
            TimeSpanFormatOptions.RangeHours => TimeSpan.TicksPerHour,
            TimeSpanFormatOptions.RangeMinutes => TimeSpan.TicksPerMinute,
            TimeSpanFormatOptions.RangeSeconds => TimeSpan.TicksPerSecond,
            TimeSpanFormatOptions.RangeMilliSeconds => TimeSpan.TicksPerMillisecond,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    static bool ShouldTruncate(long value, bool textStarted, out bool displayThisValue)
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

    static void PrepareOutput(long value, bool isRangeMin, bool hasTextStarted, List<string> result, ref bool displayThisValue)
    {
        // we need to display SOMETHING (even if it's zero)
        if (isRangeMin && !hasTextStarted)
        {
            displayThisValue = true;
            if (s_LessThan && value < 1)
            {
                // Output the "less than 1 unit" text:
                var unitTitle = s_TimeTextInfo !.GetUnitText(s_RangeMin, 1, s_Abbreviate);
                result.Add(s_TimeTextInfo.GetLessThanText(unitTitle));
                displayThisValue = false;
            }
        }
    }

    /// <summary>
    /// These are the default options that will be used when no option is specified.
    /// </summary>
    // Plain flags value seeded by the static constructor; callers may override it for the process
    // lifetime, and resetting it on code reload would silently discard that choice.
    [NoAutoStaticsCleanup]
    public static TimeSpanFormatOptions DefaultFormatOptions { get; set; }

    /// <summary>
    /// These are the absolute default options that will be used as
    /// a safeguard, just in case DefaultFormatOptions is missing a value.
    /// </summary>
    // Constant safeguard value, assigned once by the static constructor and never reassigned.
    [NoAutoStaticsCleanup]
    public static TimeSpanFormatOptions AbsoluteDefaults { get; }

    /// <summary>
    /// Returns the <see cref="TimeSpan"/> closest to the specified interval.
    /// For example: <c>Round("00:57:00", TimeSpan.TicksPerMinute * 5) =&gt; "00:55:00"</c>
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
