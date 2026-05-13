// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Utility class for formatting values for display
    /// </summary>
    internal static class FormatUtils
    {
        /// <summary>
        /// Format bytes into human-readable size
        /// </summary>
        public static string FormatSize(long bytes)
        {
            if (bytes < 0)
                return "0 B";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            if (order == 0)
            {
                // Bytes - no decimal places
                return $"{size:0} {sizes[order]}";
            }
            else if (size >= 100)
            {
                // Large numbers - no decimal places
                return $"{size:0} {sizes[order]}";
            }
            else if (size >= 10)
            {
                // Medium numbers - 1 decimal place
                return $"{size:0.#} {sizes[order]}";
            }
            else
            {
                // Small numbers - 2 decimal places
                return $"{size:0.##} {sizes[order]}";
            }
        }

        /// <summary>
        /// Format milliseconds into human-readable duration
        /// </summary>
        public static string FormatDuration(long milliseconds)
        {
            if (milliseconds < 0)
                return "0s";

            var timeSpan = TimeSpan.FromMilliseconds(milliseconds);

            if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{(int)timeSpan.TotalMinutes}m {timeSpan.Seconds}s";
            }
            else if (timeSpan.TotalSeconds >= 10)
            {
                return $"{timeSpan.Seconds}s";
            }
            else if (timeSpan.TotalSeconds >= 1)
            {
                return $"{timeSpan.TotalSeconds:0.#}s";
            }
            else
            {
                return $"{milliseconds}ms";
            }
        }

        /// <summary>
        /// Format a DateTime for display in build list
        /// </summary>
        public static string FormatBuildDate(DateTime date)
        {
            if (date == DateTime.MinValue)
                return "Unknown";
            return date.ToString("MM'/'dd'/'yyyy • HH:mm");
        }

        /// <summary>
        /// Format a count, capping at 999+
        /// </summary>
        public static string FormatCount(int count)
        {
            return count > 999 ? "999+" : count.ToString();
        }

        /// <summary>
        /// Format a percentage value
        /// </summary>
        public static string FormatPercentage(double value, int decimals = 1)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "0%";

            var percentage = value * 100;

            if (decimals == 0 || Math.Abs(percentage) >= 100)
            {
                return $"{percentage:0}%";
            }
            else if (decimals == 1)
            {
                return $"{percentage:0.#}%";
            }
            else
            {
                return $"{percentage:0.##}%";
            }
        }
    }
}
