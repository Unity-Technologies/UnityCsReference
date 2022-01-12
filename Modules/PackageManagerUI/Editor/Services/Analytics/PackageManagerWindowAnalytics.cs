// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageManagerWindowAnalytics
    {
        public string action;
        public string package_id;
        public string[] package_ids;
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

        public static void SendEvent(string action, string packageId = null, IEnumerable<string> packageIds = null)
        {
            // remove sensitive part of the id: file path or url is not tracked
            if (!string.IsNullOrEmpty(packageId))
                packageId = Regex.Replace(packageId, "(?<package>[^@]+)@(?<protocol>[^:]+):.+", "${package}@${protocol}");

            var servicesContainer = ServicesContainer.instance;
            var packageFiltering = servicesContainer.Resolve<PackageFiltering>();
            var settingsProxy = servicesContainer.Resolve<PackageManagerProjectSettingsProxy>();

            // Add the name of the sub page into the filter name for now
            var filterName = packageFiltering.currentFilterTab.ToString();
            var page = servicesContainer.Resolve<PageManager>().GetCurrentPage();
            var subPage = page.subPages.Skip(1).Any() ? page.currentSubPage : null;
            if (!string.IsNullOrEmpty(subPage?.name))
                filterName += "/" + subPage.name;

            var parameters = new PackageManagerWindowAnalytics
            {
                action = action,
                package_id = packageId ?? string.Empty,
                package_ids = packageIds?.ToArray() ?? new string[0],
                search_text = packageFiltering.currentSearchText,
                filter_name = filterName,
                window_docked = EditorWindow.GetWindowDontShow<PackageManagerWindow>()?.docked ?? false,
                dependencies_visible = settingsProxy.enablePackageDependencies,
                preview_visible = settingsProxy.enablePreReleasePackages,
                t_since_start = (long)(EditorApplication.timeSinceStartup * 1E6),
                ts = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond
            };
            EditorAnalytics.SendEventWithLimit("packageManagerWindowUserAction", parameters);
        }
    }
}
