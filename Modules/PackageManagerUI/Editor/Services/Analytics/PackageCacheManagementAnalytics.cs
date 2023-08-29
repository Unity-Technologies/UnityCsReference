// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
    internal class PackageCacheManagementAnalytics : IAnalytic
    {
        private const string k_EventName = "packageCacheManagementUserAction";
        private const string k_VendorKey = "unity.package-manager-ui";

        [Serializable]
        private class Data : IAnalytic.IData
        {
            public string action;
            public string type;
            public string[] old_path_statuses;
            public string[] new_path_statuses;
        }

        private Data m_Data;
        private PackageCacheManagementAnalytics(string action, string type, string[] oldPathStatuses, string[] newPathStatuses)
        {
            m_Data = new Data
            {
                action = action,
                type = type,
                old_path_statuses = oldPathStatuses,
                new_path_statuses = newPathStatuses
            };
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = m_Data;
            error = null;
            return data != null;
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
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<IEditorAnalyticsProxy>();
            editorAnalyticsProxy.SendAnalytic(new PackageCacheManagementAnalytics(action, type, oldPathStatuses, newPathStatuses));
        }
    }
}
