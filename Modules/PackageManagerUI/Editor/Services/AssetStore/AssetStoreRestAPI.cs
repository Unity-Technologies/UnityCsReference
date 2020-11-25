// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class AssetStoreRestAPI
    {
        static IAssetStoreRestAPI s_Instance = null;
        public static IAssetStoreRestAPI instance => s_Instance ?? AssetStoreRestAPIInternal.instance;

        internal const int k_ProductUpdateDetailsChunkSize = 30;
        internal const int k_MaxRetries = 3;
        internal const int k_ClientErrorResponseCode = 400;
        internal const int k_ServerErrorResponseCode = 500;

        internal static string k_ErrorMessage = "Something went wrong. Please try again later.";

        internal class AssetStoreRestAPIInternal : IAssetStoreRestAPI
        {
            private static AssetStoreRestAPIInternal s_Instance;
            public static AssetStoreRestAPIInternal instance => s_Instance ?? (s_Instance = new AssetStoreRestAPIInternal());

            private const string k_PurchasesUri = "/-/api/purchases";
            private const string k_TaggingsUri = "/-/api/taggings";
            private const string k_ProductInfoUri = "/-/api/product";
            private const string k_UpdateInfoUri = "/-/api/legacy-package-update-info";
            private const string k_DownloadInfoUri = "/-/api/legacy-package-download-info";

            private const int k_GeneralServerError = 599;
            private const int k_GeneralClientError = 499;
            private static readonly Dictionary<int, string> k_KnownErrors = new Dictionary<int, string>
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
                        m_Host = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPackagesApi);
                    return m_Host;
                }
            }

            private Queue<AssetStoreLocalInfo> m_Queue;

            private AssetStoreRestAPIInternal()
            {
            }

            public void GetPurchases(string query, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
            {
                // Abort any previous request
                ApplicationUtil.instance.AbortASyncHTTPClientByTag("GetPurchases");

                HandleHttpRequest(() =>
                {
                    var httpRequest = ApplicationUtil.instance.GetASyncHTTPClient($"{host}{k_PurchasesUri}{query ?? string.Empty}");
                    httpRequest.tag = "GetPurchases";
                    return httpRequest;
                }, doneCallbackAction, errorCallbackAction);
            }

            public void GetCategories(Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
            {
                IList<string> categories = k_Categories.ToList();
                var result = new Dictionary<string, object>
                {
                    ["total"] = categories.Count,
                    ["results"] = categories
                };

                doneCallbackAction?.Invoke(result);
            }

            public void GetTaggings(Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
            {
                // Abort any previous request
                ApplicationUtil.instance.AbortASyncHTTPClientByTag("GetTaggings");

                HandleHttpRequest(() =>
                {
                    var httpRequest = ApplicationUtil.instance.GetASyncHTTPClient($"{host}{k_TaggingsUri}");
                    httpRequest.tag = "GetTaggings";
                    return httpRequest;
                },
                    result =>
                    {
                        doneCallbackAction?.Invoke(result);
                    },
                    errorCallbackAction);
            }

            public void GetProductDetail(long productID, Action<Dictionary<string, object>> doneCallbackAction)
            {
                // Abort any previous request
                ApplicationUtil.instance.AbortASyncHTTPClientByTag($"GetProductDetail_{productID}");

                HandleHttpRequest(() =>
                {
                    var httpRequest = ApplicationUtil.instance.GetASyncHTTPClient($"{host}{k_ProductInfoUri}/{productID}");
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

            public void GetDownloadDetail(long productID, Action<AssetStoreDownloadInfo> doneCallbackAction)
            {
                // Abort any previous request
                ApplicationUtil.instance.AbortASyncHTTPClientByTag($"GetDownloadDetail_{productID}");

                HandleHttpRequest(() =>
                {
                    var httpRequest = ApplicationUtil.instance.GetASyncHTTPClient($"{host}{k_DownloadInfoUri}/{productID}");
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

            private void GetChunkProductUpdateDetails(int chunkSize, Action<IEnumerable<IDictionary<string, object>>> doneCallbackAction, Action<string> errorCallbackAction)
            {
                if (m_Queue?.Any() != true)
                {
                    doneCallbackAction?.Invoke(null);
                    return;
                }

                var partialInfos = new List<AssetStoreLocalInfo>(chunkSize);
                for (var i = 0; i < chunkSize && m_Queue.Any(); i++)
                    partialInfos.Add(m_Queue.Dequeue());

                var localInfosJsonData = Json.Serialize(partialInfos.Select(info => info?.ToDictionary() ?? new Dictionary<string, string>()).ToList());

                HandleHttpRequest(() => ApplicationUtil.instance.PostASyncHTTPClient($"{host}{k_UpdateInfoUri}", localInfosJsonData),
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

            public void GetProductUpdateDetail(IEnumerable<AssetStoreLocalInfo> localInfos, Action<Dictionary<string, object>> doneCallbackAction)
            {
                if (localInfos?.Any() != true)
                {
                    doneCallbackAction?.Invoke(new Dictionary<string, object>());
                    return;
                }

                var updateDetails = new List<IDictionary<string, object>>();
                m_Queue = new Queue<AssetStoreLocalInfo>(localInfos);

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

            public virtual void HandleHttpRequest(IAsyncHTTPClient httpRequest, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
            {
                HandleHttpRequest(() => httpRequest, doneCallbackAction, errorCallbackAction);
            }

            private void HandleHttpRequest(Func<IAsyncHTTPClient> httpRequestCreate, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction)
            {
                AssetStoreOAuth.instance.FetchUserInfo(
                    userInfo =>
                    {
                        var maxRetryCount = k_MaxRetries;

                        void DoHttpRequest(Action<int> retryCallbackAction)
                        {
                            var httpRequest = httpRequestCreate();

                            httpRequest.header["Content-Type"] = "application/json";
                            httpRequest.header["Authorization"] = "Bearer " + userInfo.accessToken;
                            httpRequest.doneCallback = httpClient =>
                            {
                                // Ignore if aborted
                                if (httpClient.IsAborted())
                                    return;

                                var responseCode = httpClient.responseCode;
                                if (responseCode >= k_ClientErrorResponseCode && responseCode < k_ServerErrorResponseCode)
                                {
                                    var errorMessage = k_KnownErrors[k_GeneralClientError];
                                    k_KnownErrors.TryGetValue(httpClient.responseCode, out errorMessage);
                                    errorCallbackAction?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, $"{responseCode} {errorMessage}. {k_ErrorMessage}"));
                                }
                                else if (responseCode >= k_ServerErrorResponseCode)
                                {
                                    retryCallbackAction?.Invoke(responseCode);
                                    return;
                                }

                                var parsedResult = AssetStoreUtils.ParseResponseAsDictionary(httpClient);
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
}
