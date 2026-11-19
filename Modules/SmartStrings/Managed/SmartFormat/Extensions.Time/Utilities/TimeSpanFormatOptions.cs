// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;

namespace Unity.SmartStrings.Extensions.Time.Utilities;

/// <summary>
/// Determines all options for time formatting.
/// This one value actually contains 4 settings:
/// <c>Abbreviate</c> / <c>AbbreviateOff</c>
/// <c>LessThan</c> / <c>LessThanOff</c>
/// <c>Truncate</c> &#160; <c>Auto</c> / <c>Shortest</c> / <c>Fill</c> / <c>Full</c>
/// 
///     <c>Range</c> &#160; <c>MilliSeconds</c> / <c>Seconds</c> / <c>Minutes</c> / <c>Hours</c> / <c>Days</c> /
///     <c>Weeks</c> (Min / Max)
/// 
/// </summary>
[Flags]
public enum TimeSpanFormatOptions
{
    /// <summary>
    /// Specifies that all <c>timeSpanFormatOptions</c> should be inherited from
    /// <c>TimeSpanUtility.DefaultTimeFormatOptions</c>.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// Abbreviates units.
    /// Example: "1d 2h 3m 4s 5ms"
    /// </summary>
    Abbreviate = 0x1,

    /// <summary>
    /// Does not abbreviate units.
    /// Example: "1 day 2 hours 3 minutes 4 seconds 5 milliseconds"
    /// </summary>
    AbbreviateOff = 0x2,

    /// <summary>
    /// Displays "less than 1 (unit)" when the TimeSpan is smaller than the minimum range.
    /// </summary>
    LessThan = 0x4,

    /// <summary>
    /// Displays "0 (units)" when the TimeSpan is smaller than the minimum range.
    /// </summary>
    LessThanOff = 0x8,

    /// <summary>
    /// Displays the highest non-zero value within the range.
    /// Example: "00.23:00:59.000" = "23 hours"
    /// </summary>
    TruncateShortest = 0x10,

    /// <summary>
    /// Displays all non-zero values within the range.
    /// Example: "00.23:00:59.000" = "23 hours 59 minutes"
    /// </summary>
    TruncateAuto = 0x20,

    /// <summary>
    /// Displays the highest non-zero value and all lesser values within the range.
    /// Example: "00.23:00:59.000" = "23 hours 0 minutes 59 seconds 0 milliseconds"
    /// </summary>
    TruncateFill = 0x40,

    /// <summary>
    /// Displays all values within the range.
    /// Example: "00.23:00:59.000" = "0 days 23 hours 0 minutes 59 seconds 0 milliseconds"
    /// </summary>
    TruncateFull = 0x80,

    /// <summary>
    /// Determines the range of units to display.
    /// You may combine two values to form the minimum and maximum for the range.
    /// 
    ///     Example: (RangeMinutes) defines a range of Minutes only; (RangeHours | RangeSeconds) defines a range of Hours
    ///     to Seconds.
    /// 
    /// </summary>
    RangeMilliSeconds = 0x100,

    /// <summary>
    /// Determines the range of units to display.
    /// You may combine two values to form the minimum and maximum for the range.
    /// 
    ///     Example: (RangeMinutes) defines a range of Minutes only; (RangeHours | RangeSeconds) defines a range of Hours
    ///     to Seconds.
    /// 
    /// </summary>
    RangeSeconds = 0x200,

    /// <summary>
    /// Determines the range of units to display.
    /// You may combine two values to form the minimum and maximum for the range.
    /// 
    ///     Example: (RangeMinutes) defines a range of Minutes only; (RangeHours | RangeSeconds) defines a range of Hours
    ///     to Seconds.
    /// 
    /// </summary>
    RangeMinutes = 0x400,

    /// <summary>
    /// Determines the range of units to display.
    /// You may combine two values to form the minimum and maximum for the range.
    /// 
    ///     Example: (RangeMinutes) defines a range of Minutes only; (RangeHours | RangeSeconds) defines a range of Hours
    ///     to Seconds.
    /// 
    /// </summary>
    RangeHours = 0x800,

    /// <summary>
    /// Determines the range of units to display.
    /// You may combine two values to form the minimum and maximum for the range.
    /// 
    ///     Example: (RangeMinutes) defines a range of Minutes only; (RangeHours | RangeSeconds) defines a range of Hours
    ///     to Seconds.
    /// 
    /// </summary>
    RangeDays = 0x1000,

    /// <summary>
    /// Determines the range of units to display.
    /// You may combine two values to form the minimum and maximum for the range.
    /// 
    ///     Example: (RangeMinutes) defines a range of Minutes only; (RangeHours | RangeSeconds) defines a range of Hours
    ///     to Seconds.
    /// 
    /// </summary>
    RangeWeeks = 0x2000,
}

static class TimeSpanFormatOptionsPresets
{
    public const TimeSpanFormatOptions Abbreviate = TimeSpanFormatOptions.Abbreviate | TimeSpanFormatOptions.AbbreviateOff;
    public const TimeSpanFormatOptions LessThan = TimeSpanFormatOptions.LessThan | TimeSpanFormatOptions.LessThanOff;
    public const TimeSpanFormatOptions Truncate = TimeSpanFormatOptions.TruncateShortest | TimeSpanFormatOptions.TruncateAuto | TimeSpanFormatOptions.TruncateFill | TimeSpanFormatOptions.TruncateFull;
    public const TimeSpanFormatOptions Range = TimeSpanFormatOptions.RangeMilliSeconds | TimeSpanFormatOptions.RangeSeconds | TimeSpanFormatOptions.RangeMinutes | TimeSpanFormatOptions.RangeHours | TimeSpanFormatOptions.RangeDays | TimeSpanFormatOptions.RangeWeeks;
}
