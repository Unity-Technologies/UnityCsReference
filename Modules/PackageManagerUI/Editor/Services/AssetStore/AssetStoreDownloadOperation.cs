// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    internal sealed class AssetStoreDownloadOperation
    {
        static IDownloadOperation s_Instance = null;
        public static IDownloadOperation instance => s_Instance ?? AssetStoreDownloadOperationInternal.instance;

        [Serializable]
        internal class AssetStoreDownloadOperationInternal : IDownloadOperation
        {
            private static AssetStoreDownloadOperationInternal s_Instance;
            public static AssetStoreDownloadOperationInternal instance => s_Instance ?? (s_Instance = new AssetStoreDownloadOperationInternal());

            private Texture2D m_MissingTexture;

            private List<DownloadInformation> m_DownloadInformations;

            private AssetStoreDownloadOperationInternal()
            {
                m_DownloadInformations = new List<DownloadInformation>();
            }

            public void DownloadImageAsync(long productID, string url, Action<long, Texture2D> doneCallbackAction = null)
            {
                if (m_MissingTexture == null)
                {
                    m_MissingTexture = (Texture2D)EditorGUIUtility.LoadRequired("Icons/UnityLogo.png");
                }

                var texture = AssetStoreCache.instance.LoadImage(productID, url);
                if (texture != null)
                {
                    doneCallbackAction?.Invoke(productID, texture);
                    return;
                }

                var httpRequest = ApplicationUtil.instance.GetASyncHTTPClient(url);
                httpRequest.doneCallback = httpClient =>
                {
                    if (httpClient.IsSuccess() && httpClient.texture != null)
                    {
                        AssetStoreCache.instance.SaveImage(productID, url, httpClient.texture);
                        doneCallbackAction?.Invoke(productID, httpClient.texture);
                        return;
                    }

                    doneCallbackAction?.Invoke(productID, m_MissingTexture);
                };
                httpRequest.Begin();
            }

            public void ClearDownloadInformation(string productID)
            {
                m_DownloadInformations.RemoveAll(d => d.productId == productID);
            }

            public void AbortDownloadPackage(long productID, Action<DownloadResult> doneCallbackAction = null)
            {
                AbortDownloadPackageInternal(productID.ToString(), doneCallbackAction);
            }

            private void AbortDownloadPackageInternal(string productID, Action<DownloadResult> doneCallbackAction = null)
            {
                var ret = new DownloadResult();

                var downloadInfo = m_DownloadInformations.FirstOrDefault(d => d.productId == productID);
                if (downloadInfo != null)
                {
                    var res = AssetStoreUtils.instance.AbortDownload($"content__{productID}", downloadInfo.destination);
                    ret.downloadState = res ? DownloadProgress.State.Aborted : DownloadProgress.State.Error;
                    if (!res)
                    {
                        ret.errorMessage = "Cannot abort download.";
                    }
                    doneCallbackAction?.Invoke(ret);

                    ClearDownloadInformation(downloadInfo.productId);
                }
            }

            public void DownloadUnityPackageAsync(long productID, Action<DownloadResult> doneCallbackAction = null)
            {
                AssetStoreRestAPI.instance.GetDownloadDetail(productID, downloadInfo =>
                {
                    var ret = new DownloadResult();
                    if (!downloadInfo.isValid)
                    {
                        ret.downloadState = DownloadProgress.State.Error;
                        ret.errorMessage = downloadInfo.errorMessage;
                        doneCallbackAction?.Invoke(ret);
                        return;
                    }

                    var dest = downloadInfo.destination;

                    var json = AssetStoreUtils.instance.CheckDownload(
                        $"content__{downloadInfo.productId}",
                        downloadInfo.url, dest,
                        downloadInfo.key);

                    var resumeOK = false;
                    try
                    {
                        json = Regex.Replace(json, "\"url\":(?<url>\"?[^,]+\"?),\"", "\"url\":\"${url}\",\"");
                        json = Regex.Replace(json, "\"key\":(?<key>\"?[0-9a-zA-Z]*\"?)\\}", "\"key\":\"${key}\"}");
                        json = Regex.Replace(json, "\"+(?<value>[^\"]+)\"+", "\"${value}\"");

                        var current = Json.Deserialize(json) as IDictionary<string, object>;
                        if (current == null)
                        {
                            throw new ArgumentException("Invalid JSON");
                        }

                        var inProgress = current.ContainsKey("in_progress") && (current["in_progress"] is bool? (bool)current["in_progress"] : false);
                        if (inProgress)
                        {
                            ret.downloadState = DownloadProgress.State.InProgress;
                            doneCallbackAction?.Invoke(ret);
                            return;
                        }

                        if (current.ContainsKey("download") && current["download"] is IDictionary<string, object>)
                        {
                            var download = (IDictionary<string, object>)current["download"];
                            var existingUrl = download.ContainsKey("url") ? download["url"] as string : string.Empty;
                            var existingKey = download.ContainsKey("key") ? download["key"] as string : string.Empty;
                            resumeOK = (existingUrl == downloadInfo.url && existingKey == downloadInfo.key);
                        }
                    }
                    catch (Exception e)
                    {
                        ret.downloadState = DownloadProgress.State.Error;
                        ret.errorMessage = e.Message;
                        doneCallbackAction?.Invoke(ret);
                        return;
                    }

                    json = $"{{\"download\":{{\"url\":\"{downloadInfo.url}\",\"key\":\"{downloadInfo.key}\"}}}}";
                    AssetStoreUtils.instance.Download(
                        $"content__{downloadInfo.productId}",
                        downloadInfo.url,
                        dest,
                        downloadInfo.key,
                        json,
                        resumeOK);

                    m_DownloadInformations.Add(downloadInfo);

                    ret.downloadState = DownloadProgress.State.Started;
                    doneCallbackAction?.Invoke(ret);
                });
            }

            public void ClearCache()
            {
                m_MissingTexture = null;

                var listProductIds = m_DownloadInformations.Select(info => info.productId).ToList();
                foreach (var productId in listProductIds)
                    AbortDownloadPackageInternal(productId);
            }
        }
    }
}
