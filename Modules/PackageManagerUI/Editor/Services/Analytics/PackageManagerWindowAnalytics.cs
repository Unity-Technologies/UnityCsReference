// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal struct PackageManagerWindowAnalytics
    {
        public string action;
        public string package_id;
        public string search_text;
        public string filter_name;
        public bool window_docked;
        public bool dependencies_visible;
        public bool preview_visible;
        public long t_since_start;  // in microseconds
        public long ts;             // in milliseconds

        public static void Setup()
        {
            int maxEventsPerHour = 1000;
            int maxNumberOfElementInStruct = 100;
            string vendorKey = "unity.package-manager-ui";

            EditorAnalytics.RegisterEventWithLimit("packageManagerWindowUserAction", maxEventsPerHour, maxNumberOfElementInStruct, vendorKey);
        }

        public static void SendEvent(string action, string packageId = null)
        {
            // remove sensitive part of the id: file path or url is not tracked
            if (!string.IsNullOrEmpty(packageId))
                packageId = Regex.Replace(packageId, "(?<package>[^@]+)@(?<protocol>[^:]+):.+", "${package}@${protocol}");

            var packageFiltering = ServicesContainer.instance.Resolve<PackageFiltering>();
            var packageManagerPrefs = ServicesContainer.instance.Resolve<PackageManagerPrefs>();
            var settingsProxy = ServicesContainer.instance.Resolve<PackageManagerProjectSettingsProxy>();

            var parameters = new PackageManagerWindowAnalytics
            {
                action = action,
                package_id = packageId ?? string.Empty,
                search_text = packageFiltering.currentSearchText,
                filter_name = packageFiltering.currentFilterTab.ToString(),
                window_docked = EditorWindow.GetWindowDontShow<PackageManagerWindow>()?.docked ?? false,
                dependencies_visible = packageManagerPrefs.showPackageDependencies,
                preview_visible = settingsProxy.enablePreviewPackages,
                t_since_start = (long)(EditorApplication.timeSinceStartup * 1E6),
                ts = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond
            };
            EditorAnalytics.SendEventWithLimit("packageManagerWindowUserAction", parameters);
        }
    }
}
