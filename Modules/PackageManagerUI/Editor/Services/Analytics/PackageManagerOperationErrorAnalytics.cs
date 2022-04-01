// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageManagerOperationErrorAnalytics
    {
        private const string k_EventName = "packageManagerWindowOperationError";

        public string operation_type;
        public string message;
        public string error_type;
        public int status_code;
        public string[] attributes;
        public string read_more_url;

        public static void SendEvent(string operationType, UIError error)
        {
            var servicesContainer = ServicesContainer.instance;
            var editorAnalyticsProxy = servicesContainer.Resolve<EditorAnalyticsProxy>();
            if (!editorAnalyticsProxy.RegisterEvent(k_EventName))
                return;

            var parameters = new PackageManagerOperationErrorAnalytics
            {
                operation_type = operationType,
                message = error.message,
                error_type = error.errorCode.ToString(),
                status_code = error.operationErrorCode,
                attributes = error.attribute == UIError.Attribute.None ? new string[0] : error.attribute.ToString().Split(','),
                read_more_url = error.readMoreURL ?? string.Empty
            };
            editorAnalyticsProxy.SendEventWithLimit(k_EventName, parameters);
        }
    }
}
