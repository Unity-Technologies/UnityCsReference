// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI.Internal
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
        private const string k_TermsCheckUri = "/-/api/terms/check";

        internal const int k_ProductUpdateDetailsChunkSize = 30;
        internal const int k_MaxRetries = 3;
        internal const int k_ClientErrorResponseCode = 400;
        internal const int k_ServerErrorResponseCode = 500;

        internal static string k_ErrorMessage = "Something went wrong. Please try again later.";

        private const int k_GeneralServerError = 599;
        private const int k_GeneralClientError = 499;
        internal static readonly Dictionary<int, string> k_KnownErrors = new Dictionary<int, string>
        {
            [400] = "Bad Request",
            [401] = "Unauthorized",
            [403] = "Forbidden",
            [404] = "Not Found",
            [407] = "Proxy Authentication Required",
            [408] = "Request Timeout",
            [k_GeneralClientError] = "Client Error",
            [500] = "Internal Server Error",
            [502] = "Bad Gateway",
            [503] = "Service Unavailable",
            [504] = "Gateway Timeout",
            [k_GeneralServerError] = "Server Error"
        };

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
        private Queue<string> m_Queue;

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
            // Abort any previous request
            m_HttpClientFactory.AbortByTag($"GetPurchases{query}");

            HandleHttpRequest(() =>
            {
                var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_PurchasesUri}{query ?? string.Empty}");
                httpRequest.tag = $"GetPurchases{query}";
                return httpRequest;
            }, doneCallbackAction, errorCallbackAction);
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
            // Abort any previous request
            m_HttpClientFactory.AbortByTag("GetTaggings");

            HandleHttpRequest(() =>
            {
                var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_TaggingsUri}");
                httpRequest.tag = "GetTaggings";
                return httpRequest;
            },
                result =>
                {
                    doneCallbackAction?.Invoke(result);
                },
                errorCallbackAction);
        }

        public virtual void GetProductDetail(long productID, Action<Dictionary<string, object>> doneCallbackAction)
        {
            // Abort any previous request
            m_HttpClientFactory.AbortByTag($"GetProductDetail_{productID}");

            HandleHttpRequest(() =>
            {
                var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_ProductInfoUri}/{productID}");
                httpRequest.tag = $"GetProductDetail_{productID}";
                return httpRequest;
            },
                result =>
                {
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
            // Abort any previous request
            m_HttpClientFactory.AbortByTag($"GetDownloadDetail{productID}");

            HandleHttpRequest(() =>
            {
                var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_DownloadInfoUri}/{productID}");
                httpRequest.tag = $"GetDownloadDetail{productID}";
                return httpRequest;
            },
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

        public virtual void GetChunkProductUpdateDetails(int chunkSize, Action<IEnumerable<IDictionary<string, object>>> doneCallbackAction, Action<string> errorCallbackAction)
        {
            if (m_Queue?.Any() != true)
            {
                doneCallbackAction?.Invoke(null);
                return;
            }

            var partialInfos = new List<AssetStoreLocalInfo>(chunkSize);
            while (m_Queue.Any() && partialInfos.Count < chunkSize)
            {
                var productId = m_Queue.Dequeue();
                var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
                if (localInfo?.updateInfoFetched == false)
                    partialInfos.Add(localInfo);
            }

            var localInfosJsonData = Json.Serialize(partialInfos.Select(info => info?.ToDictionary() ?? new Dictionary<string, string>()).ToList());

            HandleHttpRequest(() => m_HttpClientFactory.PostASyncHTTPClient($"{host}{k_UpdateInfoUri}", localInfosJsonData),
                result =>
                {
                    var ret = result["result"] as Dictionary<string, object>;
                    var results = ret?.GetList<IDictionary<string, object>>("results");
                    if (results != null)
                        doneCallbackAction?.Invoke(results);
                },
                error =>
                {
                    errorCallbackAction?.Invoke(error.message);
                });
        }

        public virtual void GetProductUpdateDetail(IEnumerable<string> productIds, Action<Dictionary<string, object>> doneCallbackAction)
        {
            if (productIds?.Any() != true)
            {
                doneCallbackAction?.Invoke(new Dictionary<string, object>());
                return;
            }

            var updateDetails = new List<IDictionary<string, object>>();

            // We want to prioritize new fetch update details calls as those are more likely to be the ones users are interested in
            m_Queue = new Queue<string>(productIds.Concat(m_Queue?.ToArray() ?? Enumerable.Empty<string>()));

            void ErrorCallBack(string message)
            {
                var ret = new Dictionary<string, object> {["errorMessage"] = message};
                doneCallbackAction?.Invoke(ret);
            }

            void ChunkCallbackAction(IEnumerable<IDictionary<string, object>> chunkUpdateDetails)
            {
                if (chunkUpdateDetails != null)
                    updateDetails.AddRange(chunkUpdateDetails);

                if (!m_Queue.Any())
                {
                    doneCallbackAction?.Invoke(new Dictionary<string, object>
                    {
                        {"results", updateDetails}
                    });
                }
                else
                    GetChunkProductUpdateDetails(k_ProductUpdateDetailsChunkSize, ChunkCallbackAction, ErrorCallBack);
            }

            GetChunkProductUpdateDetails(k_ProductUpdateDetailsChunkSize, ChunkCallbackAction, ErrorCallBack);
        }

        public virtual void CheckTermsAndConditions(Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
        {
            // Abort any previous request
            m_HttpClientFactory.AbortByTag("CheckTermsAndConditions");

            HandleHttpRequest(() =>
            {
                var httpRequest = m_HttpClientFactory.GetASyncHTTPClient($"{host}{k_TermsCheckUri}");
                httpRequest.tag = "CheckTermsAndConditions";
                return httpRequest;
            },
                result =>
                {
                    doneCallbackAction?.Invoke(result);
                },
                error =>
                {
                    errorCallbackAction?.Invoke(error);
                });
        }

        public virtual void HandleHttpRequest(IAsyncHTTPClient httpRequest, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
        {
            HandleHttpRequest(() => httpRequest, doneCallbackAction, errorCallbackAction);
        }

        private void HandleHttpRequest(Func<IAsyncHTTPClient> httpRequestCreate, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
        {
            m_AssetStoreOAuth.FetchUserInfo(
                userInfo =>
                {
                    var maxRetryCount = k_MaxRetries;

                    void DoHttpRequest(Action<int> retryCallbackAction)
                    {
                        var httpRequest = httpRequestCreate();

                        httpRequest.header["Content-Type"] = "application/json";
                        httpRequest.header["Authorization"] = "Bearer " + userInfo.accessToken;
                        httpRequest.doneCallback = request =>
                        {
                            // Ignore if aborted
                            if (request.IsAborted())
                                return;

                            var responseCode = request.responseCode;
                            if (responseCode >= k_ClientErrorResponseCode && responseCode < k_ServerErrorResponseCode)
                            {
                                var errorMessage = k_KnownErrors[k_GeneralClientError];
                                k_KnownErrors.TryGetValue(request.responseCode, out errorMessage);
                                errorCallbackAction?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, $"{responseCode} {errorMessage}. {k_ErrorMessage}"));
                            }
                            else if (responseCode >= k_ServerErrorResponseCode)
                            {
                                retryCallbackAction?.Invoke(responseCode);
                                return;
                            }

                            var parsedResult = AssetStoreUtils.ParseResponseAsDictionary(request);
                            if (parsedResult == null)
                                retryCallbackAction?.Invoke(responseCode);
                            else
                            {
                                if (parsedResult.ContainsKey("errorMessage"))
                                    errorCallbackAction?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, parsedResult.GetString("errorMessage")));
                                else
                                    doneCallbackAction?.Invoke(parsedResult);
                            }
                        };

                        httpRequest.Begin();
                    }

                    void RetryCallbackAction(int lastResponseCode)
                    {
                        maxRetryCount--;
                        if (maxRetryCount > 0)
                            DoHttpRequest(RetryCallbackAction);
                        else
                        {
                            if (lastResponseCode >= k_ServerErrorResponseCode)
                            {
                                var errorMessage = k_KnownErrors[k_GeneralServerError];
                                k_KnownErrors.TryGetValue(lastResponseCode, out errorMessage);
                                errorCallbackAction?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, $"{lastResponseCode} {errorMessage}. {k_ErrorMessage}"));
                            }
                            else
                                errorCallbackAction?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, k_ErrorMessage));
                        }
                    }

                    DoHttpRequest(RetryCallbackAction);
                },
                errorCallbackAction);
        }
    }
}
