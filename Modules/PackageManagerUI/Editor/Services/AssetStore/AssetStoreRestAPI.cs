// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI.AssetStore
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

            private const int kDefaultLimit = 100;
            private const string kListUri = "/-/api/purchases";
            private const string kDetailUri = "/-/api/product";
            private const string kUpdateUri = "/-/api/legacy-package-update-info";
            private const string kDownloadUri = "/-/api/legacy-package-download-info";

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

            private IASyncHTTPClientFactory m_AsyncHTTPClient;
            private ProductUpdateDetailHelper m_Helper;

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

            private AssetStoreRestAPIInternal()
            {
                m_AsyncHTTPClient = new ASyncHTTPClientFactory();
            }

            public void GetProductIDList(int startIndex, int limit, string searchText, Action<ProductList> doneCallbackAction)
            {
                // Abort any previous request
                m_AsyncHTTPClient.AbortASyncHTTPClientByTag("GetProductIDList");

                var returnList = new ProductList
                {
                    total = 0,
                    startIndex = startIndex,
                    isValid = false,
                    searchText = searchText,
                    list = new List<ProductInfo>()
                };

                limit = limit > 0 ? limit : kDefaultLimit;
                searchText = string.IsNullOrEmpty(searchText) ? "" : searchText;
                HandleHttpRequest(() =>
                {
                    var httpRequest = m_AsyncHTTPClient.GetASyncHTTPClient($"{host}{kListUri}?offset={startIndex}&limit={limit}&query={Uri.EscapeDataString(searchText)}");
                    httpRequest.tag = "GetProductIDList";
                    return httpRequest;
                }, result =>
                    {
                        if (result != null)
                        {
                            var total = (long)result["total"];
                            returnList.total = total;
                            returnList.isValid = true;

                            if (total == 0)
                            {
                                doneCallbackAction?.Invoke(returnList);
                                return;
                            }

                            var results = result["results"] as IList<object>;
                            foreach (var r in results)
                            {
                                var item = r as Dictionary<string, object>;
                                returnList.list.Add(new ProductInfo
                                {
                                    productId = (long)item["packageId"],
                                    displayName = (string)item["displayName"]
                                });
                            }

                            doneCallbackAction?.Invoke(returnList);
                        }
                    }, errorMessage =>
                    {
                        returnList.errorMessage = errorMessage;
                        doneCallbackAction?.Invoke(returnList);
                    });
            }

            public void GetProductDetail(long productID, Action<Dictionary<string, object>> doneCallbackAction)
            {
                // Abort any previous request
                m_AsyncHTTPClient.AbortASyncHTTPClientByTag($"GetProductDetail_{productID}");

                HandleHttpRequest(() =>
                {
                    var httpRequest = m_AsyncHTTPClient.GetASyncHTTPClient($"{host}{kDetailUri}/{productID}");
                    httpRequest.tag = $"GetProductDetail_{productID}";
                    return httpRequest;
                }, result =>
                    {
                        doneCallbackAction?.Invoke(result);
                    }, errorMessage =>
                    {
                        var ret = new Dictionary<string, object> {["errorMessage"] = errorMessage};
                        doneCallbackAction?.Invoke(ret);
                    });
            }

            public void GetDownloadDetail(long productID, Action<DownloadInformation> doneCallbackAction)
            {
                // Abort any previous request
                m_AsyncHTTPClient.AbortASyncHTTPClientByTag($"GetDownloadDetail_{productID}");

                HandleHttpRequest(() =>
                {
                    var httpRequest = m_AsyncHTTPClient.GetASyncHTTPClient($"{host}{kDownloadUri}/{productID}");
                    httpRequest.tag = $"GetDownloadDetail{productID}";
                    return httpRequest;
                }, result =>
                    {
                        if (result != null)
                        {
                            var downloadRes = result["result"] as Dictionary<string, object>;
                            var download = downloadRes["download"] as Dictionary<string, object>;

                            var downloadInfo = new DownloadInformation
                            {
                                isValid = true,
                                CategoryName = download["filename_safe_category_name"] as string,
                                PackageName = download["filename_safe_package_name"] as string,
                                PublisherName = download["filename_safe_publisher_name"] as string,
                                PackageId = download["id"] as string,
                                Key = download["key"] as string,
                                Url = download["url"] as string
                            };
                            doneCallbackAction?.Invoke(downloadInfo);
                        }
                    }, errorMessage =>
                    {
                        var downloadInfo = new DownloadInformation
                        {
                            isValid = false,
                            errorMessage = errorMessage
                        };
                        doneCallbackAction?.Invoke(downloadInfo);
                    });
            }

            internal class ProductUpdateDetailHelper
            {
                private Action<Dictionary<string, object>> m_CallbackAction;
                private readonly List<IDictionary<string, object>> m_UpdateDetails;
                private readonly string m_Url;
                private readonly IASyncHTTPClientFactory m_Factory;
                private readonly int m_ChunkSize;
                internal Queue<LocalInfo> m_Queue;

                public ProductUpdateDetailHelper(string url, IASyncHTTPClientFactory factory, IEnumerable<LocalInfo> localInfos, int chunkSize, Action<Dictionary<string, object>> doneCallbackAction)
                {
                    m_Url = url;
                    m_Factory = factory;
                    m_UpdateDetails = new List<IDictionary<string, object>>();
                    m_CallbackAction = doneCallbackAction;
                    m_ChunkSize = chunkSize;
                    m_Queue = new Queue<LocalInfo>(localInfos);
                }

                public void Start()
                {
                    Chunk(Next, errorMessage =>
                    {
                        var ret = new Dictionary<string, object> {["errorMessage"] = errorMessage};
                        m_CallbackAction?.Invoke(ret);
                    });
                }

                public void Stop()
                {
                    m_Queue = new Queue<LocalInfo>();
                    m_CallbackAction = null;
                }

                private void Next(IEnumerable<IDictionary<string, object>> chunkUpdateDetails)
                {
                    if (chunkUpdateDetails != null)
                        m_UpdateDetails.AddRange(chunkUpdateDetails);

                    if (!m_Queue.Any())
                    {
                        m_CallbackAction?.Invoke(new Dictionary<string, object> {{"results", m_UpdateDetails}});
                    }
                    else
                        Chunk(Next, errorMessage =>
                        {
                            var ret = new Dictionary<string, object> {["errorMessage"] = errorMessage};
                            m_CallbackAction?.Invoke(ret);
                        });
                }

                private void Chunk(Action<IEnumerable<IDictionary<string, object>>> doneCallbackAction, Action<string> errorCallbackAction)
                {
                    if (m_Queue?.Any() != true)
                    {
                        doneCallbackAction?.Invoke(null);
                        return;
                    }

                    var partialInfos = new List<LocalInfo>(m_ChunkSize);
                    for (var i = 0; i < m_ChunkSize && m_Queue.Any(); i++)
                        partialInfos.Add(m_Queue.Dequeue());

                    var packageList = new List<Dictionary<string, string>>();
                    foreach (var info in partialInfos)
                    {
                        if (info == null)
                        {
                            errorCallbackAction?.Invoke(L10n.Tr("Invalid package information."));
                            return;
                        }

                        var dictData = new Dictionary<string, string>();

                        dictData["local_path"] = info?.packagePath ?? string.Empty;
                        dictData["id"] = info?.id ?? string.Empty;
                        dictData["version"] = info?.versionString ?? string.Empty;
                        dictData["version_id"] = info?.versionId ?? string.Empty;
                        packageList.Add(dictData);
                    }

                    HandleHttpRequestInternal(() =>
                    {
                        var httpRequest = m_Factory.GetASyncHTTPClient(m_Url, "POST");
                        httpRequest.postData = Json.Serialize(packageList);
                        return httpRequest;
                    }, result =>
                        {
                            if (result != null)
                            {
                                var ret = result["result"] as Dictionary<string, object>;
                                var results = ret?.GetList<IDictionary<string, object>>("results");
                                if (results != null)
                                    doneCallbackAction?.Invoke(results);
                            }
                        }, errorMessage =>
                        {
                            errorCallbackAction?.Invoke(errorMessage);
                        });
                }
            }

            public void GetProductUpdateDetail(IEnumerable<LocalInfo> localInfos, Action<Dictionary<string, object>> doneCallbackAction)
            {
                if (localInfos?.Any() != true)
                {
                    doneCallbackAction?.Invoke(new Dictionary<string, object>());
                    return;
                }

                // We want to prioritize new fetch update details calls as those are more likely to be the ones users are interested in
                var newlocalInfos = localInfos.Concat(m_Helper?.m_Queue?.ToArray() ?? Enumerable.Empty<LocalInfo>());
                m_Helper?.Stop();
                m_Helper = new ProductUpdateDetailHelper($"{host}{kUpdateUri}", m_AsyncHTTPClient, newlocalInfos, k_ProductUpdateDetailsChunkSize, doneCallbackAction);
                m_Helper.Start();
            }

            private class HttpRequestHelper
            {
                private readonly Func<IAsyncHTTPClient> m_HttpRequestCreate;
                private readonly AssetStoreOAuth.UserInfo m_UserInfo;
                private readonly Action<Dictionary<string, object>> m_CallbackAction;
                private readonly Action<string> m_ErrorCallbackAction;
                private int m_RetryCount;

                public HttpRequestHelper(Func<IAsyncHTTPClient> httpRequestCreate, AssetStoreOAuth.UserInfo userInfo, Action<Dictionary<string, object>> doneCallbackAction, Action<string> errorCallbackAction)
                {
                    m_HttpRequestCreate = httpRequestCreate;
                    m_UserInfo = userInfo;
                    m_CallbackAction = doneCallbackAction;
                    m_ErrorCallbackAction = errorCallbackAction;
                    m_RetryCount = k_MaxRetries;
                }

                private void Retry(int lastResponseCode)
                {
                    m_RetryCount--;
                    if (m_RetryCount > 0)
                        Begin();
                    else
                    {
                        if (lastResponseCode >= k_ServerErrorResponseCode)
                        {
                            var errorMessage = k_KnownErrors[k_GeneralServerError];
                            k_KnownErrors.TryGetValue(lastResponseCode, out errorMessage);
                            m_ErrorCallbackAction?.Invoke($"{lastResponseCode} {errorMessage}. {k_ErrorMessage}");
                        }
                        else
                            m_ErrorCallbackAction?.Invoke(k_ErrorMessage);
                    }
                }

                public void Begin()
                {
                    var httpRequest = m_HttpRequestCreate();

                    httpRequest.header["Content-Type"] = "application/json";
                    httpRequest.header["Authorization"] = "Bearer " + m_UserInfo.accessToken.access_token;
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
                            m_ErrorCallbackAction?.Invoke($"{responseCode} {errorMessage}. {k_ErrorMessage}");
                        }
                        else if (responseCode >= k_ServerErrorResponseCode)
                        {
                            Retry(responseCode);
                            return;
                        }

                        var parsedResult = ParseResponseAsDictionary(httpClient);
                        if (parsedResult == null)
                            Retry(responseCode);
                        else
                        {
                            if (parsedResult.ContainsKey("errorMessage"))
                                m_ErrorCallbackAction?.Invoke(parsedResult.GetString("errorMessage"));
                            else
                                m_CallbackAction?.Invoke(parsedResult);
                        }
                    };
                    httpRequest.Begin();
                }
            }

            public void HandleHttpRequest(Func<IAsyncHTTPClient> httpRequestCreate, Action<Dictionary<string, object>> doneCallbackAction, Action<string> errorCallbackAction)
            {
                HandleHttpRequestInternal(httpRequestCreate, doneCallbackAction, errorCallbackAction);
            }

            public void HandleHttpRequest(IAsyncHTTPClient httpRequest, Action<Dictionary<string, object>> doneCallbackAction, Action<string> errorCallbackAction)
            {
                HandleHttpRequestInternal(() => httpRequest, doneCallbackAction, errorCallbackAction);
            }

            private static void HandleHttpRequestInternal(Func<IAsyncHTTPClient> httpRequestCreate, Action<Dictionary<string, object>> doneCallbackAction, Action<string> errorCallbackAction)
            {
                AssetStoreOAuth.instance.FetchUserInfo(
                    userInfo =>
                    {
                        if (!userInfo.isValid)
                        {
                            errorCallbackAction?.Invoke(userInfo.errorMessage);
                            return;
                        }

                        var helper = new HttpRequestHelper(httpRequestCreate, userInfo, doneCallbackAction, errorCallbackAction);
                        helper.Begin();
                    });
            }

            private static Dictionary<string, object> ParseResponseAsDictionary(IAsyncHTTPClient request)
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

                        errorMessage = L10n.Tr(k_ErrorMessage);
                    }
                    catch (Exception e)
                    {
                        errorMessage = $"{L10n.Tr(k_ErrorMessage)} {e.Message}";
                    }
                }
                else
                {
                    if (request.responseCode == 0 &&
                        (string.IsNullOrEmpty(request.text) || request.text.ToLower().Contains("timeout")))
                        return null;

                    var text = request.text.Length <= 128 ? request.text : request.text.Substring(0, 128) + "...";
                    errorMessage = $"{L10n.Tr(k_ErrorMessage)} \"{text}\" [Code {request.responseCode}]";
                }
                return string.IsNullOrEmpty(errorMessage) ? null : new Dictionary<string, object> { ["errorMessage"] = errorMessage };
            }
        }
    }
}
