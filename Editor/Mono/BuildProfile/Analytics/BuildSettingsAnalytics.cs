// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.Build.Profile.Analytics
{
    internal static class BuildSettingsAnalytics
    {
        const string k_EventName = "build_setting_changed";
        const string k_VendorKey = "unity.editor";

        [Serializable]
        class Data : IAnalytic.IData
        {
            public string setting;
            public string build_target;
            public string platform_id;
            public string platform_display_name;
            public bool bool_value;
        }

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
        class SettingChangedAnalytic : IAnalytic
        {
            readonly Data m_Data;
            public SettingChangedAnalytic(Data data) => m_Data = data;

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }
        }

        // Each setting is expected to be instrumented from a single call site. If this method ever becomes callable
        // from multiple contexts for the same setting, consider adding a field to disambiguate provenance (i.e. which
        // window or editor in which the change originated).
        public static void SendSettingChanged(
            string setting,
            bool value,
            BuildTarget buildTarget,
            GUID platformGuid = default
        )
        {
            var resolvedGuid = platformGuid.Empty() ? BuildTargetDiscovery.GetGUIDFromBuildTarget(buildTarget) : platformGuid;
            EditorAnalytics.SendAnalytic(new SettingChangedAnalytic(new Data
            {
                setting = setting,
                build_target = buildTarget.ToString(),
                platform_id = resolvedGuid.ToString(),
                platform_display_name = BuildTargetDiscovery.BuildPlatformDisplayName(resolvedGuid),
                bool_value = value,
            }));
        }
    }
}
