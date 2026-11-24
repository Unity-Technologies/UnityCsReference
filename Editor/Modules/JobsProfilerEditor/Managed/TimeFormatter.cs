// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace UnityEditor.JobsProfiling;

// Times use a decimal point regardless of the OS region, consistent with the rest of the Profiler window.
internal static class TimeFormatter
{
    internal static string FormatTickLabel(float time, float tickPeriod)
    {
        string format = Settings.TickFormatMilliseconds;
        var log10 = Mathf.FloorToInt(Mathf.Log10(tickPeriod));
        if (log10 >= 3)
        {
            time /= 1000;
            format = Settings.TickFormatSeconds;
        }

        return String.Format(CultureInfo.InvariantCulture, format, time.ToString("N" + Mathf.Max(0, -log10), CultureInfo.InvariantCulture));
    }

    internal static void AppendBarLabel(StringBuilder sb, string name, float timeMs)
    {
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ({1:F4} ms)", name, timeMs);
    }

    internal static string FormatMilliseconds(float timeMs, string numberFormat = null)
    {
        return timeMs.ToString(numberFormat, CultureInfo.InvariantCulture) + " ms";
    }
}
