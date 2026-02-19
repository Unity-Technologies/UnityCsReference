// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;

using FrameRate = UnityEngine.Playables.FrameRate;

//Timeline Editor code
namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    /// <summary>
    /// The available display modes for time in the Timeline Editor.
    /// </summary>
    enum TimeFormat
    {
        /// <summary>Displays time values as frames.</summary>
        Frames,

        /// <summary>Displays time values as timecode (SS:FF) format.</summary>
        Timecode,

        /// <summary>Displays time values as seconds.</summary>
        Seconds
    }

    static class TimeDisplayUnitExtensions
    {
        public static string ToTimeString(this TimeFormat timeFormat, double time, FrameRate frameRate, string format = "f2")
        {
            switch (timeFormat)
            {
                case TimeFormat.Frames: return TimeUtility.TimeAsFrames(time, frameRate.rate, format);
                case TimeFormat.Timecode: return TimeUtility.TimeAsTimeCode(time, frameRate.rate, format, true);
                case TimeFormat.Seconds: return time.ToString(format, CultureInfo.InvariantCulture.NumberFormat);
            }

            return time.ToString(format);
        }

        public static string ToTimeStringWithDelta(this TimeFormat timeFormat, double time, FrameRate frameRate, double delta, string format = "f2")
        {
            return timeFormat.ToTimeStringWithDelta(time, frameRate, delta, false, format);
        }

        public static string ToTimeStringWithDeltaAsPercentage(this TimeFormat timeFormat, double time, FrameRate frameRate, double delta, string format = "f2")
        {
            return timeFormat.ToTimeStringWithDelta(time, frameRate, delta, true, format);
        }

        static string ToTimeStringWithDelta(this TimeFormat timeFormat, double time, FrameRate frameRate, double delta, bool asPercent, string format = "f2")
        {
            const double epsilon = 1e-7;
            var result = ToTimeString(timeFormat, time, frameRate, format);
            if (delta > epsilon || delta < -epsilon)
            {
                var sign = ((delta >= 0) ? "+" : "-");
                if (asPercent)
                    delta *= 100;
                string footer = asPercent ? "%" : string.Empty;

                var deltaStr = ToTimeString(timeFormat, Math.Abs(delta), frameRate, format);
                return $"{result} ({sign}{deltaStr}{footer})";
            }

            return result;
        }

        public static double FromTimeString(this TimeFormat timeFormat, string timeString, FrameRate frameRate, double defaultValue)
        {
            if (timeFormat == TimeFormat.Frames)
            {
                double time;
                if (!double.TryParse(timeString, NumberStyles.Any, CultureInfo.InvariantCulture, out time))
                    return defaultValue;
                return TimeUtility.FromFrames(time, frameRate.rate);
            }

            if (timeFormat == TimeFormat.Seconds)
            {
                return TimeUtility.ParseTimeSeconds(timeString, frameRate.rate, defaultValue);
            }

            // this handles seconds or timecode based on the formatting (. vs :)
            return TimeUtility.ParseTimeCode(timeString, frameRate.rate, defaultValue);
        }
    }
}
