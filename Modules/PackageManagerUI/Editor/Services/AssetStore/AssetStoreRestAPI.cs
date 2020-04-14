// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI
{
    internal class AssetStoreRestAPI
    {
        // This instance reference is kept for compatibility reasons, as it is internal visible to the Quick Search package
        // To be addressed further in https://jira.unity3d.com/browse/PAX-1306
        internal static AssetStoreRestAPI instance => ServicesContainer.instance.Resolve<AssetStoreRestAPI>();

        private const string k_PurchasesUri = "/-/api/purchases";
        private const string k_TaggingsUri = "/-/api/taggings";
        private const string k_ProductInfoUri = "/-/api/product";
        private const string k_UpdateInfoUri = "/-/api/legacy-package-update-info";
        private const string k_DownloadInfoUri = "/-/api/legacy-package-download-info";

        private static readonly string[] k_Categories =
        {
            "3D",
            "Add-Ons",
            "2D",
            "Audio",
            "Essentials",
            "Templates",
            "Tools",
            "VFX"
        };

        private string m_Host;
        private string host
        {
            get
            {
                if (string.IsNullOrEmpty(m_Host))
                    m_Host = m_UnityConnect.GetConfigurationURL(CloudConfigUrl.CloudPackagesApi);
                return m_Host;
            }
        }


        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreOAuth m_AssetStoreOAuth;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private HttpClientFactory m_HttpClientFactory;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
            AssetStoreOAuth assetStoreOAuth,
            AssetStoreCache assetStoreCache,
            HttpClientFactory httpClientFactory)
        {
            m_UnityConnect = unityConnect;
            m_AssetStoreOAuth = assetStoreOAuth;
            m_AssetStoreCache = assetStoreCache;
            m_HttpClientFactory = httpClientFactory;
        }

        public virtual void GetPurchases(string query, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
        {
            var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_PurchasesUri}{query ?? string.Empty}");
            HandleHttpRequest(httpRequest, doneCallbackAction, errorCallbackAction);
        }

        public virtual void GetCategories(Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
        {
            IList<string> categories = k_Categories.ToList();
            var result = new Dictionary<string, object>
            {
                ["total"] = categories.Count,
                ["results"] = categories
            };

            doneCallbackAction?.Invoke(result);
        }

        public virtual void GetTaggings(Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
        {
            var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_TaggingsUri}");
            var etag = m_AssetStoreCache.GetLastETag(k_TaggingsUri);
            httpRequest.header["If-None-Match"] = etag.Replace("\"", "\\\"");
            HandleHttpRequest(httpRequest,
                result =>
                {
                    if (httpRequest.responseHeader.ContainsKey("ETag"))
                        etag = httpRequest.responseHeader["ETag"];
                    m_AssetStoreCache.SetLastETag(k_TaggingsUri, etag);

                    doneCallbackAction?.Invoke(result);
                },
                errorCallbackAction);
        }

        public virtual void GetProductDetail(long productID, Action<Dictionary<string, object>> doneCallbackAction)
        {
            var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_ProductInfoUri}/{productID}");
            var etag = m_AssetStoreCache.GetLastETag($"{k_ProductInfoUri}/{productID}");
            httpRequest.header["If-None-Match"] = etag.Replace("\"", "\\\"");

            HandleHttpRequest(httpRequest,
                result =>
                {
                    if (httpRequest.responseHeader.ContainsKey("ETag"))
                        etag = httpRequest.responseHeader["ETag"];
                    m_AssetStoreCache.SetLastETag($"{k_ProductInfoUri}/{productID}", etag);

                    doneCallbackAction?.Invoke(result);
                },
                error =>
                {
                    var ret = new Dictionary<string, object> { ["errorMessage"] = error.message };
                    doneCallbackAction?.Invoke(ret);
                });
        }

        public virtual void GetDownloadDetail(long productID, Action<AssetStoreDownloadInfo> doneCallbackAction)
        {
            var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_DownloadInfoUri}/{productID}");
            HandleHttpRequest(httpRequest,
                result =>
                {
                    var downloadInfo = AssetStoreDownloadInfo.ParseDownloadInfo(result);
                    doneCallbackAction?.Invoke(downloadInfo);
                },
                error =>
                {
                    var downloadInfo = new AssetStoreDownloadInfo
                    {
                        isValid = false,
                        errorMessage = error.message
                    };
                    doneCallbackAction?.Invoke(downloadInfo);
                });
        }

        public virtual void GetProductUpdateDetail(IEnumerable<AssetStoreLocalInfo> localInfos, Action<Dictionary<string, object>> doneCallbackAction)
        {
            if (localInfos?.Any() != true)
            {
                doneCallbackAction?.Invoke(new Dictionary<string, object>());
                return;
            }

            var localInfosJsonData = Json.Serialize(localInfos.Select(info => info?.ToDictionary() ?? new Dictionary<string, string>()).ToList());
            var httpRequest = m_HttpClientFactory.PostASyncHTTPClient($"{host}{k_UpdateInfoUri}", localInfosJsonData);

            HandleHttpRequest(httpRequest,
                result =>
                {
                    var ret = result["result"] as Dictionary<string, object>;
                    doneCallbackAction?.Invoke(ret);
                },
                error =>
                {
                    var ret = new Dictionary<string, object> { ["errorMessage"] = error.message };
                    doneCallbackAction?.Invoke(ret);
                });
        }

        private void HandleHttpRequest(IAsyncHTTPClient httpRequest, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
        {
            m_AssetStoreOAuth.FetchUserInfo(
                userInfo =>
                {
                    httpRequest.header["Content-Type"] = "application/json";
                    httpRequest.header["Authorization"] = "Bearer " + userInfo.accessToken;
                    httpRequest.doneCallback = httpClient =>
                    {
                        var parsedResult = AssetStoreUtils.ParseResponseAsDictionary(httpRequest, errorMessage =>
                        {
                            errorCallbackAction?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, errorMessage));
                        });
                        if (parsedResult != null)
                            doneCallbackAction?.Invoke(parsedResult);
                    };
                    httpRequest.Begin();
                },
                errorCallbackAction);
        }
    }
}
