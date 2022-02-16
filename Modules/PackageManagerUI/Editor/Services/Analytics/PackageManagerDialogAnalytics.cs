// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal struct PackageManagerDialogAnalytics
    {
        private const string k_EventName = "packageManagerDialogs";

        public string id;
        public string title;
        public string message;
        public string user_response;

        public static void SendEvent(string id, string title, string message, string userResponse)
        {
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<EditorAnalyticsProxy>();
            if (!editorAnalyticsProxy.RegisterEvent(k_EventName))
                return;

            var parameters = new PackageManagerDialogAnalytics
            {
                id = id,
                title = title,
                message = message,
                user_response = userResponse
            };
            editorAnalyticsProxy.SendEventWithLimit(k_EventName, parameters);
        }
    }
}
