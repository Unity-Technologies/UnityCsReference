// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Connect;
using UnityEngine;
using UnityAssetStoreUtils = UnityEditor.AssetStoreUtils;
using UnityAssetStorePackageInfo = UnityEditor.PackageInfo;

namespace UnityEditor.PackageManager.UI
{
    internal class AssetStoreUtils
    {
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        public void ResolveDependencies(UnityConnectProxy unityConnect)
        {
            m_UnityConnect = unityConnect;
        }

        public static Dictionary<string, object> ParseResponseAsDictionary(IAsyncHTTPClient request, Action<string> errorMessageCallback)
        {
            string errorMessage;
            if (request.IsSuccess() && request.responseCode == 200)
            {
                try
                {
                    var response = Json.Deserialize(request.text) as Dictionary<string, object>;
                    if (response != null)
                        return response;

                    errorMessage = L10n.Tr("Failed to parse JSON.");
                }
                catch (Exception e)
                {
                    errorMessage = string.Format(L10n.Tr("Failed to parse JSON: {0}"), e.Message);
                }
            }
            else
            {
                if (request.responseCode == 0 && string.IsNullOrEmpty(request.text))
                    errorMessage = L10n.Tr("Failed to parse response.");
                else
                {
                    var text = request.text.Length <= 128 ? request.text : request.text.Substring(0, 128) + "...";
                    errorMessage = string.Format(L10n.Tr("Failed to parse response: Code {0} \"{1}\""), request.responseCode, text);
                }
            }
            errorMessageCallback?.Invoke(errorMessage);
            return null;
        }

        public virtual void Download(string id, string url, string[] destination, string key, string jsonData, bool resumeOK)
        {
            UnityAssetStoreUtils.Download(id, url, destination, key, jsonData, resumeOK);
        }

        public virtual string CheckDownload(string id, string url, string[] destination, string key)
        {
            return UnityAssetStoreUtils.CheckDownload(id, url, destination, key);
        }

        public virtual bool AbortDownload(string id, string[] destination)
        {
            return UnityAssetStoreUtils.AbortDownload(id, destination);
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
