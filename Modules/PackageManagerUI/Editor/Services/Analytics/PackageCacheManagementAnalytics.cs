// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageCacheManagementAnalytics
    {
        private const int k_MaxEventsPerHour = 1000;
        private const int k_MaxNumberOfElementInStruct = 100;
        private const string k_VendorKey = "unity.package-manager-ui";

        public string action;
        public string type;
        public string[] old_path_statuses;
        public string[] new_path_statuses;
        public long t_since_start;  // in microseconds
        public long ts;             // in milliseconds

        private static bool s_Registered;

        private static bool RegisterEvent()
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
                return false;

            if (!EditorAnalytics.enabled)
            {
                Console.WriteLine("[PackageManager] Editor analytics are disabled");
                return false;
            }

            if (s_Registered)
                return true;

            var result = EditorAnalytics.RegisterEventWithLimit("packageCacheManagementUserAction", k_MaxEventsPerHour, k_MaxNumberOfElementInStruct, k_VendorKey);
            switch (result)
            {
                case AnalyticsResult.Ok:
                case AnalyticsResult.TooManyRequests:
                {
                    s_Registered = true;
                    break;
                }
                default:
                {
                    Console.WriteLine($"[PackageManager] Failed to register analytics event 'packageCacheManagementUserAction'. Result: '{result}'");
                    s_Registered = false;
                    break;
                }
            }

            return s_Registered;
        }

        public static void SendAssetStoreEvent(string action, string[] oldPathStatuses, string[] newPathStatuses = null)
        {
            SendEvent(action, "AssetStore", oldPathStatuses, newPathStatuses);
        }

        public static void SendUpmEvent(string action, string[] oldPathStatuses, string[] newPathStatuses = null)
        {
            SendEvent(action, "UPM", oldPathStatuses, newPathStatuses);
        }

        private static void SendEvent(string action, string type, string[] oldPathStatuses, string[] newPathStatuses)
        {
            if (!RegisterEvent())
                return;

            var parameters = new PackageCacheManagementAnalytics
            {
                action = action,
                type = type,
                old_path_statuses = oldPathStatuses,
                new_path_statuses = newPathStatuses,
                t_since_start = (long)(EditorApplication.timeSinceStartup * 1E6),
                ts = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond
            };

            EditorAnalytics.SendEventWithLimit("packageCacheManagementUserAction", parameters);
        }
    }
}
