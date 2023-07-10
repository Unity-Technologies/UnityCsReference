// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal struct PackageCacheManagementAnalytics
    {
        private const string k_EventName = "packageCacheManagementUserAction";
        private const string k_VendorKey = "unity.package-manager-ui";

        [Serializable]
        internal class PackageCacheManagementAnalyticData : IAnalytic.IData
        {
            public string action;
            public string type;
            public string[] old_path_statuses;
            public string[] new_path_statuses;
        }

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
        internal class PackageCacheManagementAnalytic : IAnalytic
        {
            public PackageCacheManagementAnalytic(string action, string type, string[] oldPathStatuses, string[] newPathStatuses)
            {
                this.m_Action = action;
                this.m_Type = type;
                this.m_OldPathStatuses = oldPathStatuses;
                this.m_NewPathStatuses = newPathStatuses;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data =  new PackageCacheManagementAnalyticData
                {
                    action = m_Action,
                    type = m_Type,
                    old_path_statuses = m_OldPathStatuses,
                    new_path_statuses = m_NewPathStatuses
                };
             
                error = null;
                return true;
            }

            private string m_Action;
            private string m_Type;
            private string[] m_OldPathStatuses;
            private string[] m_NewPathStatuses;
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
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<EditorAnalyticsProxy>();
            editorAnalyticsProxy.SendAnalytic(new PackageCacheManagementAnalytic(action, type, oldPathStatuses, newPathStatuses));
        }
    }
}
