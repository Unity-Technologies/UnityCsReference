// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class Formatting
    {
        /// <summary>
        /// Formats a given DateTime object as a string in the format "yyyy/MM/dd HH:mm".
        /// </summary>
        /// <param name="dateTime">The DateTime object to format.</param>
        /// <returns>A string representation of the input DateTime object in the specified format.</returns>
        public static string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd HH:mm");
        }

        /// <summary>
        /// Formats a given TimeSpan object as a string in the format "HH:mm:ss".
        /// </summary>
        /// <param name="timeSpan">The TimeSpan object to format.</param>
        /// <returns>A string representation of the input value.</returns>
        public static string FormatDuration(TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }

        /// <summary>
        /// Formats a given TimeSpan object as a string in the format "HH:mm:ss".
        /// </summary>
        /// <param name="timeSpan">The TimeSpan object to format.</param>
        /// <returns>A string representation of the input value.</returns>
        public static string FormatDurationWithMs(TimeSpan timeSpan)
        {
            return $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds:000}";
        }

        /// <summary>
        /// Formats a given TimeSpan object as a string in the format "X ms", "X s", or "X min", depending on the length of the time span.
        /// </summary>
        /// <param name="timeSpan">The TimeSpan object to format.</param>
        /// <returns>A string representation of the input TimeSpan object.</returns>
        public static string FormatTime(TimeSpan timeSpan)
        {
            var timeMs = timeSpan.TotalMilliseconds;
            if (timeMs < 1000)
                return timeMs.ToString("F1") + " ms";
            if (timeMs < 60000)
                return timeSpan.TotalSeconds.ToString("F2") + " s";
            return timeSpan.TotalMinutes.ToString("F2") + " min";
        }

        /// <summary>
        /// Formats a given time value as a string in the format "X ms", "X s", or "X min"
        /// </summary>
        /// <param name="timeMs">The time value to format, in milliseconds.</param>
        /// <returns>A string representation of the input float value.</returns>
        public static string FormatTime(float timeMs)
        {
            if (float.IsNaN(timeMs))
                return "NaN";
            return FormatTime(TimeSpan.FromMilliseconds(timeMs));
        }

        /// <summary>
        /// Formats a decimal number as a percentage with a specified number of decimal places.
        /// </summary>
        /// <param name="number">The decimal number to format.</param>
        /// <param name="numDecimalPlaces">Number of decimals.</param>
        /// <returns>A string representation of the decimal number as a percentage.</returns>
        public static string FormatPercentage(float number, int numDecimalPlaces = 0)
        {
            var formatString = $"{{0:F{numDecimalPlaces}}}";
            return string.Format(CultureInfo.InvariantCulture.NumberFormat, formatString, (100.0f * number)) + "%";
        }

        /// <summary>
        /// Formats a given size in bytes as a string in the format "X bytes".
        /// </summary>
        /// <param name="size">Size value to format.</param>
        /// <returns>A string representation of the input value as a size.</returns>
        public static string FormatSize(ulong size)
        {
            return EditorUtility.FormatBytes((long)size);
        }

        /// <summary>
        /// Formats a given frequency as a string in the format "X Hz" or "X kHz".
        /// </summary>
        /// <param name="size">Frequency value to format.</param>
        /// <returns>A string representation of the input value as a frequency in Hz or kHz.</returns>
        public static string FormatHz(int frequency)
        {
            return (frequency < 1000) ? $"{frequency} Hz" : $"{((float)frequency / 1000.0f):G0} kHz";
        }

        /// <summary>
        /// Formats a given float duration as a string in the format "X.XXX s".
        /// </summary>
        /// <param name="length">Length value to format.</param>
        /// <returns>A string representation of the input value as a duration in seconds.</returns>
        public static string FormatLengthInSeconds(float length)
        {
            return length.ToString("F3") + " s";
        }

        /// <summary>
        /// Formats a given float framerate as a string in the format "X fps".
        /// </summary>
        /// <param name="framerate">Framerate value to format.</param>
        /// <returns>A string representation of the input value as a framerate.</returns>
        public static string FormatFramerate(float framerate)
        {
            return framerate + " fps";
        }

        static readonly string k_StringSeparator = ", ";

        public static string CombineStrings(string[] strings, string separator = null)
        {
            return string.Join(separator ?? k_StringSeparator, strings);
        }

        public static string[] SplitStrings(string combinedString, string separator = null)
        {
            return combinedString.Split(new[] {separator ?? k_StringSeparator}, StringSplitOptions.None);
        }

        public static string ReplaceStringSeparators(string combinedString, string separator)
        {
            return combinedString.Replace(k_StringSeparator, separator);
        }

        public static string StripRichTextTags(string text)
        {
            text = RemoveRichTextTag(text, "b", string.Empty);
            text = RemoveRichTextTag(text, "i", string.Empty);
            text = RemoveRichTextTag(text, "u", string.Empty);
            text = RemoveRichTextTag(text, "color", string.Empty);

            return text;
        }

        static string RemoveRichTextTag(string input, string tagName, string replaceWith)
        {
            const string k_RichTextTagRegExp = "</?{0}[^<]*?>";

            var reg = new Regex(String.Format(k_RichTextTagRegExp, tagName), RegexOptions.IgnoreCase);
            return reg.Replace(input, replaceWith);
        }

        // Strings to match the new Build Profiles page. We can't access them directly right now, so duplicate.
        public static string GetModernBuildTargetName(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:             return "macOS";
                case BuildTarget.StandaloneWindows:         return "Windows (32 bit)";
                case BuildTarget.StandaloneWindows64:       return "Windows";
                case BuildTarget.iOS:                       return "iOS";
                case BuildTarget.Android:                   return "Android™";
                case BuildTarget.WebGL:                     return "Web";
                case BuildTarget.WSAPlayer:                 return "Universal Windows Platform";
                case BuildTarget.StandaloneLinux64:         return "Linux";
                case BuildTarget.PS4:                       return "PlayStation®4";
                case BuildTarget.tvOS:                      return "tvOS";
                case BuildTarget.Switch:                    return "Nintendo Switch™";
                case BuildTarget.XboxOne:
                case BuildTarget.GameCoreXboxOne:           return "Xbox One";
                case BuildTarget.GameCoreXboxSeries:        return "Xbox Series X|S";
                case BuildTarget.PS5:                       return "PlayStation®5";
                case BuildTarget.LinuxHeadlessSimulation:   return "Linux Headless Simulation";
                case BuildTarget.EmbeddedLinux:             return "Embedded Linux";
                case BuildTarget.QNX:                       return "QNX®";
                case BuildTarget.VisionOS:                  return "visionOS";
                default: return BuildPipeline.GetBuildTargetName(buildTarget);
            }
        }

        public static string GetModernBuildTargetName(BuildTargetGroup buildTargetGroup)
        {
            switch (buildTargetGroup)
            {
                case BuildTargetGroup.Standalone:              return "Windows, Mac, Linux";
                case BuildTargetGroup.iOS:                     return GetModernBuildTargetName(BuildTarget.iOS);
                case BuildTargetGroup.Android:                 return GetModernBuildTargetName(BuildTarget.Android);
                case BuildTargetGroup.WebGL:                   return GetModernBuildTargetName(BuildTarget.WebGL);
                case BuildTargetGroup.WSA:                     return GetModernBuildTargetName(BuildTarget.WSAPlayer);
                case BuildTargetGroup.PS4:                     return GetModernBuildTargetName(BuildTarget.PS4);
                case BuildTargetGroup.XboxOne:                 return GetModernBuildTargetName(BuildTarget.XboxOne);
                case BuildTargetGroup.tvOS:                    return GetModernBuildTargetName(BuildTarget.tvOS);
                case BuildTargetGroup.Switch:                  return GetModernBuildTargetName(BuildTarget.Switch);
                case BuildTargetGroup.LinuxHeadlessSimulation: return GetModernBuildTargetName(BuildTarget.LinuxHeadlessSimulation);
                case BuildTargetGroup.GameCoreXboxOne:         return GetModernBuildTargetName(BuildTarget.GameCoreXboxOne);
                case BuildTargetGroup.GameCoreXboxSeries:      return GetModernBuildTargetName(BuildTarget.GameCoreXboxSeries);
                case BuildTargetGroup.PS5:                     return GetModernBuildTargetName(BuildTarget.PS5);
                case BuildTargetGroup.EmbeddedLinux:           return GetModernBuildTargetName(BuildTarget.EmbeddedLinux);
                case BuildTargetGroup.QNX:                     return GetModernBuildTargetName(BuildTarget.QNX);
                case BuildTargetGroup.VisionOS:                return GetModernBuildTargetName(BuildTarget.VisionOS);
                default: return "Unknown platform group";
            }
        }
    }
}
