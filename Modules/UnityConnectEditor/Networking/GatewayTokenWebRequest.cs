// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    internal class GatewayTokenWebRequest
    {
        const string k_ContentHeaderName = "Content-Type";
        const string k_ContentHeaderValue = "application/json;charset=UTF-8";

        const string k_GenesisTokenKeyJsonLabel = "token";
        const string k_GatewayTokenKeyJsonLabel = "token";

        const string k_GatewayTokenExceptionMessage = "Exception occurred trying to obtain authentication signature for project {0} and was not handled. Message: {1}";

        internal static void RequestGatewayToken(Action<string> onGetGatewayToken, Notification.Topic exceptionNotificationTopic)
        {
            var payload = "{\"" + k_GenesisTokenKeyJsonLabel + "\": \"" + CloudProjectSettings.accessToken + "\"}";
            var jsonUploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            var jsonDownloadHandler = new DownloadHandlerBuffer();
            var request = new UnityWebRequest(PurchasingConfiguration.instance.gatewayTokenApiUrl,
                UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = jsonUploadHandler,
                downloadHandler = jsonDownloadHandler,
                suppressErrorsToConsole = true
            };
            request.SetRequestHeader(k_ContentHeaderName, k_ContentHeaderValue);

            var operation = request.SendWebRequest();
            operation.completed += _ => OnGetGatewayToken(request, onGetGatewayToken, exceptionNotificationTopic);
        }

        static void OnGetGatewayToken(UnityWebRequest request, Action<string> onGetGatewayToken, Notification.Topic exceptionNotificationTopic)
        {
            if (request.downloadHandler.isDone && request.isDone && request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    onGetGatewayToken?.Invoke(NetworkingUtils.GetStringFromRawJsonDictionaryString(request.downloadHandler.text, k_GatewayTokenKeyJsonLabel));
                }
                catch (Exception ex)
                {
                    NotificationManager.instance.Publish(exceptionNotificationTopic, Notification.Severity.Error,
                                string.Format(L10n.Tr(k_GatewayTokenExceptionMessage), Connect.UnityConnect.instance.projectInfo.projectName, ex.Message));

                    Debug.LogException(ex);
                }
            }
        }
    }
}
