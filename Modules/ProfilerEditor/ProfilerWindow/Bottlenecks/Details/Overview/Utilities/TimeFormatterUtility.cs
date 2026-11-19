// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace Unity.Profiling.Editor.UI
{
    class TimeFormatterUtility
    {
        public static string FormatTimeNsToMs(UInt64 timeNs)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F3}ms", timeNs * 1.0e-6f);
        }
    }
}
