// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
    internal class PackageManagerOperationErrorAnalytics : IAnalytic
    {
        private const string k_EventName = "packageManagerWindowOperationError";
        private const string k_VendorKey = "unity.package-manager-ui";

        [Serializable]
        private class Data : IAnalytic.IData
        {
            public string operation_type;
            public string message;
            public string error_type;
            public int status_code;
            public string[] attributes;
            public string read_more_url;
        }

        private Data m_Data;
        private PackageManagerOperationErrorAnalytics(string operationType, UIError error)
        {
            m_Data = new Data
            {
                operation_type = operationType,
                message = error.message,
                error_type = error.errorCode.ToString(),
                status_code = error.operationErrorCode,
                attributes = error.attribute == UIError.Attribute.None ? Array.Empty<string>() : error.attribute.ToString().Split(','),
                read_more_url = error.readMoreURL ?? string.Empty
            };
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Data;
            return data != null;
        }

        public static void SendEvent(string operationType, UIError error)
        {
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<IEditorAnalyticsProxy>();
            editorAnalyticsProxy.SendAnalytic(new PackageManagerOperationErrorAnalytics(operationType, error));
        }
    }
}
