// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Analytics;

namespace UnityEditor
{
    [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey, version: 1, maxEventsPerHour: 10)]
    internal class EditorWindowUsageAnalytic : IAnalytic
    {
        const string k_EventName = "editorWindowUsage";
        const string k_VendorKey = "unity.editorwindowusage";

        // Field names match the editor_analytics_editorWindowUsage_v1 schema.
        [Serializable]
        internal class Data : IAnalytic.IData
        {
            public int active_seconds;
            public int play_mode_seconds;
            public WindowRow[] windows;
        }

        [Serializable]
        internal class WindowRow
        {
            public string origin;
            public string window_type;
            public string package_name;
            public string mode;
            public int open_seconds;
            public int visible_seconds;
            public int focused_seconds;
            public int focus_gains;
            public int peak_instances;
        }

        const int k_SecondsPerSample = EditorWindowTelemetry.k_SamplePeriodSeconds;

        readonly Data m_Data;

        internal EditorWindowUsageAnalytic(List<WindowUsage> usage, int activeSamples, int playModeSamples)
        {
            var windows = new WindowRow[usage.Count];
            for (int i = 0; i < usage.Count; ++i)
                windows[i] = CreateRow(usage[i]);

            m_Data = new Data
            {
                active_seconds = activeSamples * k_SecondsPerSample,
                play_mode_seconds = playModeSamples * k_SecondsPerSample,
                windows = windows,
            };
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Data;
            return true;
        }

        static WindowRow CreateRow(WindowUsage usage)
        {
            var row = new WindowRow
            {
                mode = usage.inPlayMode ? "play" : "edit",
                open_seconds = usage.openSamples * k_SecondsPerSample,
                visible_seconds = usage.visibleSamples * k_SecondsPerSample,
                focused_seconds = usage.focusedSamples * k_SecondsPerSample,
                focus_gains = usage.focusGains,
                peak_instances = usage.peakInstances,
            };

            var id = usage.id;
            if (id.StartsWith(WindowIdentityResolver.k_FirstPartyPrefix, StringComparison.Ordinal))
            {
                row.origin = "first_party";
                row.window_type = id.Substring(WindowIdentityResolver.k_FirstPartyPrefix.Length);
            }
            else if (id.StartsWith(WindowIdentityResolver.k_PackagePrefix, StringComparison.Ordinal))
            {
                var nameStart = WindowIdentityResolver.k_PackagePrefix.Length;
                var separator = id.IndexOf('/', nameStart);
                row.origin = "package";
                row.package_name = id.Substring(nameStart, separator - nameStart);
                row.window_type = id.Substring(separator + 1);
            }
            else
            {
                row.origin = "third_party";
            }

            return row;
        }
    }
}
