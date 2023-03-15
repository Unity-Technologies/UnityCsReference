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
        private const string k_EventName = "packageManagerWindowUserAction";

        public string action;
        public string package_id;
        public string package_tag;
        public string[] package_ids;
        public string[] package_tags;
        public string search_text;
        public string filter_name;
        public string details_tab;
        public bool window_docked;
        public bool dependencies_visible;
        public bool preview_visible;

        public static void SendEvent(string action, IPackageVersion version)
        {
            SendEvent(action, GetAnalyticsPackageId(version), packageTag: version?.GetAnalyticsTags());
        }

        public static void SendEvent(string action, IEnumerable<IPackageVersion> versions)
        {
            SendEvent(action, packageIds: versions?.Select(GetAnalyticsPackageId), packageTags: versions?.Select(v => v.GetAnalyticsTags()));
        }

        // Our current analytics backend expects package_id to match product id for legacy asset store packages.
        // Hence we are doing the special handling here. If we want to change this in the future, we need to also make changes in the backend accordingly.
        private static string GetAnalyticsPackageId(IPackageVersion version)
        {
            return version?.HasTag(PackageTag.LegacyFormat) == true ? version.package.uniqueId : version?.uniqueId;
        }

        public static void SendEvent(string action, string packageId = null, IEnumerable<string> packageIds = null, string packageTag = null, IEnumerable<string> packageTags = null)
        {
            var servicesContainer = ServicesContainer.instance;
            var editorAnalyticsProxy = servicesContainer.Resolve<EditorAnalyticsProxy>();
            if (!editorAnalyticsProxy.RegisterEvent(k_EventName))
                return;

            // remove sensitive part of the id: file path or url is not tracked
            if (!string.IsNullOrEmpty(packageId))
                packageId = Regex.Replace(packageId, "(?<package>[^@]+)@(?<protocol>[^:]+):.+", "${package}@${protocol}");

            var packageManagerPrefs = servicesContainer.Resolve<PackageManagerPrefs>();
            var pageManager = servicesContainer.Resolve<PageManager>();
            var activePage = pageManager.activePage;
            var settingsProxy = servicesContainer.Resolve<PackageManagerProjectSettingsProxy>();

            var parameters = new PackageManagerWindowAnalytics
            {
                action = action,
                package_id = packageId ?? string.Empty,
                package_tag = packageTag ?? string.Empty,
                package_ids = packageIds?.ToArray() ?? new string[0],
                package_tags = packageTags?.ToArray() ?? new string[0],
                search_text = activePage.searchText,
                filter_name = activePage.id,
                details_tab = packageManagerPrefs.selectedPackageDetailsTabIdentifier ?? string.Empty,
                window_docked = EditorWindow.GetWindowDontShow<PackageManagerWindow>()?.docked ?? false,
                // packages installed as dependency are always visible
                // we keep the dependencies_visible to not break the analytics
                dependencies_visible = true,
                preview_visible = settingsProxy.enablePreReleasePackages
            };
            editorAnalyticsProxy.SendEventWithLimit(k_EventName, parameters);
        }
    }
}
