// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageManagerFiltersAnalytics
    {
        private const string k_EventName = "packageManagerWindowFilters";
        private const string k_VendorKey = "unity.package-manager-ui";

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
        internal class PackageManagerFiltersAnalytic : IAnalytic
        {
            [Serializable]
            internal class PackageManagerFiltersAnalyticsData : IAnalytic.IData
            {
                public string filter_tab;
                public string order_by;
                public string status;
                public string[] categories;
                public string[] labels;
            }
            public PackageManagerFiltersAnalytic(PageFilters filters)
            {
                this.m_Filters = filters;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                var servicesContainer = ServicesContainer.instance;
                var filterTab = servicesContainer.Resolve<PageManager>().activePage.id;
                var parameters = new PackageManagerFiltersAnalyticsData
                {
                    filter_tab = filterTab,
                    order_by = m_Filters.sortOption.ToString(),
                    status = m_Filters.status.ToString(),
                    categories = m_Filters.categories.ToArray(),
                    labels = m_Filters.labels.ToArray()
                };
                data = parameters;
                return data != null;
            }

            private PageFilters m_Filters;
        }

        public static void SendEvent(PageFilters filters)
        {
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<EditorAnalyticsProxy>();
            editorAnalyticsProxy.SendAnalytic(new PackageManagerFiltersAnalytic(filters));
        }
    }
}
