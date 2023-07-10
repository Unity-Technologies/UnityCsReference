// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageManagerOperationErrorAnalytics
    {
        private const string k_EventName = "packageManagerWindowOperationError";
        private const string k_VendorKey = "unity.package-manager-ui";

        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
        internal class PackageManagerOperationErrorAnalytic : IAnalytic
        {
            [Serializable]
            internal class PackageManagerOperationErrorAnalyticsData : IAnalytic.IData
            {
                public string operation_type;
                public string message;
                public string error_type;
                public int status_code;
                public string[] attributes;
                public string read_more_url;
            }

            public PackageManagerOperationErrorAnalytic(string operationType, UIError error)
            {
                this.operationType = operationType;
                this.uiError = error;
            }

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                var parameters = new PackageManagerOperationErrorAnalyticsData
                {
                    operation_type = operationType,
                    message = uiError.message,
                    error_type = uiError.errorCode.ToString(),
                    status_code = uiError.operationErrorCode,
                    attributes = uiError.attribute == UIError.Attribute.None ? new string[0] : uiError.attribute.ToString().Split(','),
                    read_more_url = uiError.readMoreURL ?? string.Empty
                };
                data = parameters;
                return data != null;
            }

            string operationType;
            UIError uiError;
        }

        public static void SendEvent(string operationType, UIError error)
        {
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<EditorAnalyticsProxy>();
            PackageManagerOperationErrorAnalytic analytic = new PackageManagerOperationErrorAnalytic(operationType, error);
            editorAnalyticsProxy.SendAnalytic(analytic);
        }
    }
}
