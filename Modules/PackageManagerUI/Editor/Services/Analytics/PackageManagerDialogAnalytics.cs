// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
    internal class PackageManagerDialogAnalytics : IAnalytic
    {
        private const string k_EventName = "packageManagerDialogs";
        private const string k_VendorKey = "unity.package-manager-ui";

        [Serializable]
        private class Data : IAnalytic.IData
        {
            public string id;
            public string title;
            public string message;
            public string user_response;
        }

        private Data m_Data;
        private PackageManagerDialogAnalytics(string id, string title, string message, string userResponse)
        {
            m_Data = new Data
            {
                id = id,
                title = title,
                message = message,
                user_response = userResponse
            };
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            data = m_Data;
            error = null;
            return m_Data != null;
        }

        public static void SendEvent(string id, string title, string message, string userResponse)
        {
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<IEditorAnalyticsProxy>();
            editorAnalyticsProxy.SendAnalytic(new PackageManagerDialogAnalytics(id, title, message, userResponse));
        }
    }
}
