// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageManagerDialogAnalytics
    {
        private const string k_EventName = "packageManagerDialogs";
        private const string k_VendorKey = "unity.package-manager-ui";

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
        internal class PackageManagerDialogAnalytic : IAnalytic
        {
            [Serializable]
            internal class PackageManagerDialogAnalyticsData : IAnalytic.IData
            {
                public string id;
                public string title;
                public string message;
                public string user_response;
            }
            public PackageManagerDialogAnalytic(string id, string title, string message, string userResponse)
            {
                m_data = new PackageManagerDialogAnalyticsData
                {
                    id = id,
                    title = title,
                    message = message,
                    user_response = userResponse
                };
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_data;
                error = null;
                return true;
            }

            private PackageManagerDialogAnalyticsData m_data;
        }

        public static void SendEvent(string id, string title, string message, string userResponse)
        {
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<EditorAnalyticsProxy>();
            editorAnalyticsProxy.SendAnalytic(new PackageManagerDialogAnalytic(id, title, message, userResponse));
        }
    }
}
