// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    internal class GetGoogleKeyWebRequest
    {
        const string k_GoogleJsonLabel = "google";
        const string k_PublicKeyJsonLabel = "publicKey";

        const string k_AuthHeaderName = "Authorization";
        const string k_AuthHeaderValueFormat = "Bearer {0}";

        const string k_ContentHeaderName = "Content-Type";
        const string k_ContentHeaderValue = "application/json;charset=UTF-8";

        const string k_KeyParsingExceptionMessage = "Exception occurred trying to parse Google Play Key and was not handled. Message: {0}";

        internal static void RequestGooglePlayKey(string gatewayToken, Action<string, long> onGetGatewayToken, Notification.Topic exceptionNotificationTopic)
        {
            var url = string.Format(PurchasingConfiguration.instance.iapSettingssApiUrl, CloudProjectSettings.projectId);
            var request = UnityWebRequest.Get(url);
            request.suppressErrorsToConsole = true;

            request.SetRequestHeader(k_AuthHeaderName, string.Format(k_AuthHeaderValueFormat, gatewayToken));

            var operation = request.SendWebRequest();
            operation.completed += _ => OnGetGooglePlayKey(request, onGetGatewayToken, exceptionNotificationTopic);
        }

        static void OnGetGooglePlayKey(UnityWebRequest request, Action<string, long> onGetGatewayToken, Notification.Topic exceptionNotificationTopic)
        {
            if (request.downloadHandler.isDone)
            {
                var googlePlayKey = "";
                if (request.isDone && request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        googlePlayKey = FetchGooglePlayKeyFromRequest(request.downloadHandler.text);
                    }
                    catch (Exception ex)
                    {
                        NotificationManager.instance.Publish(exceptionNotificationTopic, Notification.Severity.Error,
                                    string.Format(L10n.Tr(k_KeyParsingExceptionMessage), Connect.UnityConnect.instance.projectInfo.projectName, ex.Message));

                        Debug.LogException(ex);
                    }
                }

                onGetGatewayToken?.Invoke(googlePlayKey, request.responseCode);
            }
        }

        static string FetchGooglePlayKeyFromRequest(string downloadedText)
        {
            var innerBlock = NetworkingUtils.GetJsonDictionaryWithinRawJsonDictionaryString(downloadedText, k_GoogleJsonLabel);
            return NetworkingUtils.GetStringFromJsonDictionary(innerBlock, k_PublicKeyJsonLabel);
        }
    }
}
