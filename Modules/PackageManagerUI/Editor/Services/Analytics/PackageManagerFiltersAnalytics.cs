// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
    internal class PackageManagerFiltersAnalytics : IAnalytic
    {
        private const string k_EventName = "packageManagerWindowFilters";
        private const string k_VendorKey = "unity.package-manager-ui";

        [Serializable]
        private class Data : IAnalytic.IData
        {
            public string filter_tab;
            public string order_by;
            public string status;
            public string[] categories;
            public string[] labels;
        }

        private Data m_Data;
        private PackageManagerFiltersAnalytics(PageFilters filters)
        {
            var servicesContainer = ServicesContainer.instance;
            var filterTab = servicesContainer.Resolve<IPageManager>().activePage.id;
            m_Data = new Data
            {
                filter_tab = filterTab,
                order_by = filters.sortOption.ToString(),
                status = filters.status.ToString(),
                categories = filters.categories.ToArray(),
                labels = filters.labels.ToArray()
            };
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Data;
            return data != null;
        }

        public static void SendEvent(PageFilters filters)
        {
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<IEditorAnalyticsProxy>();
            editorAnalyticsProxy.SendAnalytic(new PackageManagerFiltersAnalytics(filters));
        }
    }
}
