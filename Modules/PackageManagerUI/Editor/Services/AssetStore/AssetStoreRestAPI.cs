// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class AssetStoreRestAPI
    {
        private const string k_PurchasesUri = "/-/api/purchases";
        private const string k_TaggingsUri = "/-/api/taggings";
        private const string k_ProductInfoUri = "/-/api/product";
        private const string k_UpdateInfoUri = "/-/api/legacy-package-update-info";
        private const string k_DownloadInfoUri = "/-/api/legacy-package-download-info";
        private const string k_TermsCheckUri = "/-/api/terms/check";

        internal const int k_MaxRetries = 3;
        internal const int k_ClientErrorResponseCode = 400;
        internal const int k_ServerErrorResponseCode = 500;

        public static readonly string k_ErrorMessage = L10n.Tr("Something went wrong. Please try again later.");

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

        private string m_AssetStoreUrl;
        public virtual string assetStoreUrl
        {
            get
            {
                if (string.IsNullOrEmpty(m_AssetStoreUrl))
                    m_AssetStoreUrl = m_UnityConnect.GetConfigurationURL(CloudConfigUrl.CloudAssetStoreUrl);
                return m_AssetStoreUrl;
            }
        }

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetStoreOAuth m_AssetStoreOAuth;
        [NonSerialized]
        private JsonParser m_JsonParser;
        [NonSerialized]
        private HttpClientFactory m_HttpClientFactory;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
            AssetStoreOAuth assetStoreOAuth,
            JsonParser jsonParser,
            HttpClientFactory httpClientFactory)
        {
            m_UnityConnect = unityConnect;
            m_AssetStoreOAuth = assetStoreOAuth;
            m_JsonParser = jsonParser;
            m_HttpClientFactory = httpClientFactory;
        }

        public virtual IList<string> GetCategories()
        {
            return k_Categories;
        }

        public virtual void ListLabels(Action<List<string>> successCallback, Action<UIError> errorCallback)
        {
            var parseDictionary = CreateParseDictionaryCallback(m_JsonParser.ParseLabels, L10n.Tr("Error parsing labels."), successCallback, errorCallback);
            HandleHttpRequest(k_TaggingsUri, parseDictionary, errorCallback, tag: "GetTaggings", abortPreviousRequest: true);
        }

        public virtual void GetProductDetail(long productId, Action<AssetStoreProductInfo> successCallback, Action<UIError> errorCallback)
        {
            var parseProductInfo = (Dictionary<string, object> result) => m_JsonParser.ParseProductInfo(assetStoreUrl, productId, result);
            var parseDictionary = CreateParseDictionaryCallback(parseProductInfo, L10n.Tr("Error parsing product details."), successCallback, errorCallback);
            HandleHttpRequest($"{k_ProductInfoUri}/{productId}", parseDictionary, errorCallback, tag: $"GetProductDetail{productId}", abortPreviousRequest: true);
        }

        public virtual void GetUpdateDetail(CheckUpdateInfoArgs args, Action<List<AssetStoreUpdateInfo>> successCallback = null, Action<UIError> errorCallback = null)
        {
            var parseUpdateInfo = (Dictionary<string, object> result) => m_JsonParser.ParseUpdateInfos(args, result);
            var parseDictionary = CreateParseDictionaryCallback(parseUpdateInfo, L10n.Tr("Error parsing update details."), successCallback, errorCallback);
            HandleHttpRequest(k_UpdateInfoUri, parseDictionary, errorCallback, postData: args.ToString());
        }

        public virtual void CheckTermsAndConditions(Action<bool> successCallback, Action<UIError> errorCallback)
        {
            var parseDictionary = (Dictionary<string, object> result) => successCallback?.Invoke(result.Get<bool>("result"));
            HandleHttpRequest(k_TermsCheckUri, parseDictionary, errorCallback, tag: "CheckTermsAndConditions", abortPreviousRequest: true);
        }

        public virtual void GetPurchases(PurchasesQueryArgs query, Action<AssetStorePurchases> successCallback, Action<UIError> errorCallback)
        {
            var queryString = query.ToString();
            var parseDictionary = CreateParseDictionaryCallback(m_JsonParser.ParsePurchases, L10n.Tr("Error parsing purchase list."), successCallback, errorCallback);
            HandleHttpRequest($"{k_PurchasesUri}{queryString ?? string.Empty}", parseDictionary, errorCallback, tag: $"GetPurchases{queryString}");
        }

        public virtual void AbortGetPurchases(PurchasesQueryArgs query)
        {
            m_HttpClientFactory.AbortByTag($"GetPurchases{query}");
        }

        public virtual void GetDownloadDetail(long productId, Action<AssetStoreDownloadInfo> successCallback, Action<UIError> errorCallback)
        {
            var parseDictionary = CreateParseDictionaryCallback(m_JsonParser.ParseDownloadInfo, L10n.Tr("Error parsing download details."), successCallback, errorCallback);
            HandleHttpRequest($"{k_DownloadInfoUri}/{productId}", parseDictionary, errorCallback, tag: $"GetDownloadDetail{productId}", abortPreviousRequest: true);
        }

        private Action<Dictionary<string, object>> CreateParseDictionaryCallback<T>(
            Func<Dictionary<string, object>, T> parseFunc,
            string parseErrorMessage,
            Action<T> successCallback,
            Action<UIError> errorCallback) where T : class
        {
            return (Dictionary<string, object> result) =>
            {
                var parsedResult = parseFunc?.Invoke(result);
                if (parsedResult == null)
                    errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, parseErrorMessage));
                else
                    successCallback?.Invoke(parsedResult);
            };
        }

        private void HandleHttpRequest(string urlWithoutHost, Action<Dictionary<string, object>> doneCallback, Action<UIError> errorCallback, string postData = null, string tag = null, bool abortPreviousRequest = false)
        {
            if (abortPreviousRequest && !string.IsNullOrEmpty(tag))
                m_HttpClientFactory.AbortByTag(tag);

            m_AssetStoreOAuth.FetchAccessToken(
                token =>
                {
                    var maxRetryCount = k_MaxRetries;

                    void DoHttpRequest(Action<int> retryCallbackAction)
                    {
                        var url = $"{host}{urlWithoutHost}";
                        var httpRequest = string.IsNullOrEmpty(postData) ? m_HttpClientFactory.GetASyncHTTPClient(url) : m_HttpClientFactory.PostASyncHTTPClient(url, postData);
                        httpRequest.tag = tag ?? httpRequest.tag;

                        httpRequest.header["Content-Type"] = "application/json";
                        httpRequest.header["Authorization"] = "Bearer " + token.accessToken;
                        httpRequest.doneCallback = request =>
                        {
                            // Ignore if aborted
                            if (request.IsAborted())
                                return;

                            var responseCode = request.responseCode;
                            if (responseCode == 0)
                            {
                                errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, k_ErrorMessage, operationErrorCode: responseCode));
                                return;
                            }

                            if (responseCode >= k_ServerErrorResponseCode)
                            {
                                retryCallbackAction?.Invoke(responseCode);
                                return;
                            }

                            if (responseCode >= k_ClientErrorResponseCode && responseCode < k_ServerErrorResponseCode)
                            {
                                var errorMessage = k_KnownErrors[k_GeneralClientError];
                                k_KnownErrors.TryGetValue(request.responseCode, out errorMessage);
                                errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, $"{responseCode} {errorMessage}. {k_ErrorMessage}", operationErrorCode: responseCode));
                                return;
                            }

                            var parsedResult = m_HttpClientFactory.ParseResponseAsDictionary(request);
                            if (parsedResult == null)
                                retryCallbackAction?.Invoke(responseCode);
                            else
                            {
                                if (parsedResult.ContainsKey("errorMessage"))
                                {
                                    var operationErrorCode = parsedResult.ContainsKey("errorCode") ? int.Parse(parsedResult.GetString("errorCode")) : -1;
                                    errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, parsedResult.GetString("errorMessage"), operationErrorCode: operationErrorCode));
                                }
                                else
                                    doneCallback?.Invoke(parsedResult);
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
                                errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, $"{lastResponseCode} {errorMessage}. {k_ErrorMessage}", operationErrorCode: lastResponseCode));
                            }
                            else
                                errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, k_ErrorMessage, operationErrorCode: lastResponseCode));
                        }
                    }

                    DoHttpRequest(RetryCallbackAction);
                },
                errorCallback);
        }
    }
}
