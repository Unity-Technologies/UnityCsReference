// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Connect;
using UnityEngine;
using UnityAssetStoreUtils = UnityEditor.AssetStoreUtils;
using UnityAssetStorePackageInfo = UnityEditor.PackageInfo;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class AssetStoreUtils
    {
        public static readonly string k_InvalidJSONErrorMessage = L10n.Tr("Server response is not a valid JSON");
        public static readonly string k_ServerErrorMessage = L10n.Tr("Server response is");

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        public void ResolveDependencies(UnityConnectProxy unityConnect)
        {
            m_UnityConnect = unityConnect;
        }

        public static Dictionary<string, object> ParseResponseAsDictionary(IAsyncHTTPClient request)
        {
            string errorMessage;
            if (request.IsSuccess() && request.responseCode == 200)
            {
                if (string.IsNullOrEmpty(request.text))
                    return null;

                try
                {
                    var response = Json.Deserialize(request.text) as Dictionary<string, object>;
                    if (response != null)
                        return response;

                    errorMessage = k_InvalidJSONErrorMessage;
                }
                catch (Exception e)
                {
                    errorMessage = $"{k_InvalidJSONErrorMessage} {e.Message}";
                }
            }
            else
            {
                if (request.responseCode == 0 &&
                    (string.IsNullOrEmpty(request.text) || request.text.ToLower().Contains("timeout")))
                    return null;

                var text = request.text.Length <= 128 ? request.text : request.text.Substring(0, 128) + "...";
                errorMessage = $"{k_ServerErrorMessage} \"{text}\" [Code {request.responseCode}]";
            }

            return string.IsNullOrEmpty(errorMessage) ? null : new Dictionary<string, object> { ["errorMessage"] = errorMessage };
        }

        public virtual string BuildBaseDownloadPath(string publisher, string category)
        {
            return UnityAssetStoreUtils.BuildBaseDownloadPath(publisher, category);
        }

        public virtual string BuildFinalDownloadPath(string basePath, string packageName)
        {
            return UnityAssetStoreUtils.BuildFinalDownloadPath(basePath, packageName);
        }

        public virtual void Download(string id, string url, string[] destination, string key, string jsonData, bool resumeOK)
        {
            UnityAssetStoreUtils.Download(id, url, destination, key, jsonData, resumeOK);
        }

        public virtual string CheckDownload(string id, string url, string[] destination, string key)
        {
            return UnityAssetStoreUtils.CheckDownload(id, url, destination, key);
        }

        public virtual bool AbortDownload(string[] destination)
        {
            return UnityAssetStoreUtils.AbortDownload(destination);
        }

        public virtual void RegisterDownloadDelegate(ScriptableObject d)
        {
            UnityAssetStoreUtils.RegisterDownloadDelegate(d);
        }

        public virtual void UnRegisterDownloadDelegate(ScriptableObject d)
        {
            UnityAssetStoreUtils.UnRegisterDownloadDelegate(d);
        }

        public virtual UnityAssetStorePackageInfo[] GetLocalPackageList()
        {
            return UnityAssetStorePackageInfo.GetPackageList();
        }

        public virtual string assetStoreUrl => m_UnityConnect.GetConfigurationURL(CloudConfigUrl.CloudAssetStoreUrl);
    }
}
