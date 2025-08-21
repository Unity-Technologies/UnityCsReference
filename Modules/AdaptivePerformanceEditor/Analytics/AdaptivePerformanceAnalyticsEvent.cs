// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.AdaptivePerformance.Editor.Analytics
{
    /// <summary>
    /// AdaptivePerformanceUsage class is used for collecting and reporting analytics data in the Editor.
    /// It contains data, such as information about Adaptive Performance providers and build info.
    /// It helps to track how Adaptive Performance features are used during build processes.
    /// </summary>
    [AnalyticInfo(eventName: AdaptivePerformanceAnalyticsConstants.UsageEventName, vendorKey: AdaptivePerformanceAnalyticsConstants.VendorKey)]
    internal class AdaptivePerformanceUsage : IAnalytic
    {
        internal enum EventType
        {
            BuildPlayer
        }

        [Serializable]
        internal struct AdaptivePerformanceInfo
        {
            /// <summary>
            /// Name of the AP Manager.
            /// </summary>
            public string name;

            /// <summary>
            /// <c>True</c> if the AP Manager is active in the scene, otherwise <c>False</c>.
            /// </summary>
            public bool active;
        }

        [Serializable]
        internal struct AdaptivePerformanceUsageAnalyticsArgs : IAnalytic.IData
        {
            /// <summary>
            /// The actual event type which define the context of the event under a common table.
            /// </summary>
            public string eventType;

            /// <summary>
            /// The <see cref="GUID"/> of the build.
            /// </summary>
            public string buildGuid;

            /// <summary>
            /// The target platform.
            /// </summary>
            public string targetPlatform;

            /// <summary>
            /// The target platform version.
            /// </summary>
            public string targetPlatformVersion;

            /// <summary>
            /// List of Adaptive Performance Providers installed.
            /// </summary>
            public AdaptivePerformanceInfo[] apProvidersInfo;
        }

        AdaptivePerformanceUsageAnalyticsArgs m_Payload;

        public AdaptivePerformanceUsage()
        {
            m_Payload = new AdaptivePerformanceUsageAnalyticsArgs()
            {
                eventType = string.Empty,
                buildGuid = string.Empty,
                targetPlatform = string.Empty,
                apProvidersInfo = null,
                targetPlatformVersion = string.Empty,
            };
        }

        public AdaptivePerformanceUsage(AdaptivePerformanceUsageAnalyticsArgs payload)
        {
            m_Payload = payload;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Payload;
            return data != null;
        }
    }
}
