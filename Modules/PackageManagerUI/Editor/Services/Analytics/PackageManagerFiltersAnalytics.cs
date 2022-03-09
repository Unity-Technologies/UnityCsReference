// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageManagerFiltersAnalytics
    {
        private const string k_EventName = "packageManagerWindowFilters";

        public string filter_tab;
        public string order_by;
        public string status;
        public string[] categories;
        public string[] labels;

        public static void SendEvent(PageFilters filters)
        {
            var servicesContainer = ServicesContainer.instance;
            var editorAnalyticsProxy = servicesContainer.Resolve<EditorAnalyticsProxy>();
            if (!editorAnalyticsProxy.RegisterEvent(k_EventName))
                return;

            var filterTab = PackageManagerWindowAnalytics.GetFilterNameWithSubPage(servicesContainer.Resolve<PackageFiltering>(), servicesContainer.Resolve<PageManager>());
            var orderBy = filters.isReverseOrder ? $"{filters.orderBy} (reversed)" : filters.orderBy;
            var parameters = new PackageManagerFiltersAnalytics
            {
                filter_tab = filterTab,
                order_by = orderBy,
                status = filters.status,
                categories = filters.categories.ToArray(),
                labels = filters.labels.ToArray()
            };
            editorAnalyticsProxy.SendEventWithLimit(k_EventName, parameters);
        }
    }
}
