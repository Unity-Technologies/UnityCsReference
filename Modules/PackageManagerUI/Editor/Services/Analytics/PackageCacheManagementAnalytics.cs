// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageCacheManagementAnalytics
    {
        private const string k_EventName = "packageCacheManagementUserAction";

        public string action;
        public string type;
        public string[] old_path_statuses;
        public string[] new_path_statuses;

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
            if (!editorAnalyticsProxy.RegisterEvent(k_EventName))
                return;

            var parameters = new PackageCacheManagementAnalytics
            {
                action = action,
                type = type,
                old_path_statuses = oldPathStatuses,
                new_path_statuses = newPathStatuses
            };

            editorAnalyticsProxy.SendEventWithLimit(k_EventName, parameters);
        }
    }
}
