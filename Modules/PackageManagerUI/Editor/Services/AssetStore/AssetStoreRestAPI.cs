// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Connect;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IAssetStoreRestAPI : IService
    {
        void ListLabels(Action<List<string>> successCallback, Action<UIError> errorCallback);
        void GetProductDetail(long productId, Action<AssetStoreProductInfo> successCallback, Action<UIError> errorCallback);
        void GetUpdateDetail(CheckUpdateInfoArgs args, Action<List<AssetStoreUpdateInfo>> successCallback = null, Action<UIError> errorCallback = null);
        void CheckTermsAndConditions(Action<bool> successCallback, Action<UIError> errorCallback);
        void GetPurchases(PurchasesQueryArgs query, Action<AssetStorePurchases> successCallback, Action<UIError> errorCallback);
        void AbortGetPurchases(PurchasesQueryArgs query);
        void GetDownloadDetail(long productId, Action<AssetStoreDownloadInfo> successCallback, Action<UIError> errorCallback);
    }

    internal class AssetStoreRestAPI : BaseService<IAssetStoreRestAPI>, IAssetStoreRestAPI
    {
        private const string k_PurchasesUri = "/-/api/purchases";
        private const string k_TaggingsUri = "/-/api/taggings";
        private const string k_ProductInfoUri = "/-/api/product";
        private const string k_UpdateInfoUri = "/-/api/product-update-info";
        private const string k_DownloadInfoUri = "/-/api/legacy-package-download-info";
        private const string k_TermsCheckUri = "/-/api/terms/check";

        private const int k_MaxRetries = 3;
        private const int k_ClientErrorResponseCode = 400;
        private const int k_ServerErrorResponseCode = 500;

        private static readonly string k_ErrorMessage = L10n.Tr("Something went wrong. Please try again later.");

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
        public string assetStoreUrl
        {
            get
            {
                if (string.IsNullOrEmpty(m_AssetStoreUrl))
                    m_AssetStoreUrl = m_UnityConnect.GetConfigurationURL(CloudConfigUrl.CloudAssetStoreUrl);
                return m_AssetStoreUrl;
            }
        }

        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IAssetStoreOAuth m_AssetStoreOAuth;
        private readonly IJsonParser m_JsonParser;
        private readonly IHttpClientFactory m_HttpClientFactory;
        public AssetStoreRestAPI(IUnityConnectProxy unityConnect,
            IAssetStoreOAuth assetStoreOAuth,
            IJsonParser jsonParser,
            IHttpClientFactory httpClientFactory)
        {
            m_UnityConnect = RegisterDependency(unityConnect);
            m_AssetStoreOAuth = RegisterDependency(assetStoreOAuth);
            m_JsonParser = RegisterDependency(jsonParser);
            m_HttpClientFactory = RegisterDependency(httpClientFactory);
        }

        public void ListLabels(Action<List<string>> successCallback, Action<UIError> errorCallback)
        {
            var parseDictionary = CreateParseDictionaryCallback(m_JsonParser.ParseLabels, L10n.Tr("Error parsing labels."), successCallback, errorCallback);
            HandleHttpRequest(k_TaggingsUri, parseDictionary, errorCallback, tag: "GetTaggings", abortPreviousRequest: true);
        }

        public void GetProductDetail(long productId, Action<AssetStoreProductInfo> successCallback, Action<UIError> errorCallback)
        {
            AssetStoreProductInfo ParseProductInfo(Dictionary<string, object> result) => m_JsonParser.ParseProductInfo(assetStoreUrl, productId, result);
            var parseDictionary = CreateParseDictionaryCallback(ParseProductInfo, L10n.Tr("Error parsing product details."), successCallback, errorCallback);
            HandleHttpRequest($"{k_ProductInfoUri}/{productId}", parseDictionary, errorCallback, tag: $"GetProductDetail{productId}", abortPreviousRequest: true);
        }

        public void GetUpdateDetail(CheckUpdateInfoArgs args, Action<List<AssetStoreUpdateInfo>> successCallback = null, Action<UIError> errorCallback = null)
        {
            var queryString = args.ToString();
            var parseDictionary = CreateParseDictionaryCallback(m_JsonParser.ParseUpdateInfos, L10n.Tr("Error parsing update details."), successCallback, errorCallback);
            HandleHttpRequest($"{k_UpdateInfoUri}{queryString}", parseDictionary, errorCallback, tag: $"GetUpdateDetail{queryString}");
        }

        public void CheckTermsAndConditions(Action<bool> successCallback, Action<UIError> errorCallback)
        {
            var parseDictionary = (Dictionary<string, object> result) => successCallback?.Invoke(result.Get<bool>("result"));
            HandleHttpRequest(k_TermsCheckUri, parseDictionary, errorCallback, tag: "CheckTermsAndConditions", abortPreviousRequest: true);
        }

        public void GetPurchases(PurchasesQueryArgs query, Action<AssetStorePurchases> successCallback, Action<UIError> errorCallback)
        {
            var queryString = query.ToString();
            var parseDictionary = CreateParseDictionaryCallback(m_JsonParser.ParsePurchases, L10n.Tr("Error parsing purchase list."), successCallback, errorCallback);
            HandleHttpRequest($"{k_PurchasesUri}{queryString ?? string.Empty}", parseDictionary, errorCallback, tag: $"GetPurchases{queryString}");
        }

        public void AbortGetPurchases(PurchasesQueryArgs query)
        {
            m_HttpClientFactory.AbortByTag($"GetPurchases{query}");
        }

        public void GetDownloadDetail(long productId, Action<AssetStoreDownloadInfo> successCallback, Action<UIError> errorCallback)
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
            return result =>
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
            var fullUrl = host + urlWithoutHost;

            if (m_UnityConnect.isUserInfoReady && !m_UnityConnect.isUserLoggedIn)
            {
                var errorMessage = L10n.Tr("You need to be signed in.");
                errorCallback?.Invoke(new UIError(UIErrorCode.UserNotSignedIn, L10n.Tr(errorMessage), UIError.Attribute.HiddenFromUI));
                return;
            }

            if (abortPreviousRequest && !string.IsNullOrEmpty(tag))
                m_HttpClientFactory.AbortByTag(tag);

            m_AssetStoreOAuth.FetchAccessToken(
                token =>
                {
                    var maxRetryCount = k_MaxRetries;

                    void DoHttpRequest(Action<int> retryCallbackAction)
                    {
                        var httpRequest = string.IsNullOrEmpty(postData) ? m_HttpClientFactory.GetASyncHTTPClient(fullUrl) : m_HttpClientFactory.PostASyncHTTPClient(fullUrl, postData);
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
                                var errorMessage = string.Format(L10n.Tr("[Error {0}] {1} (URL: {2})"), responseCode, k_ErrorMessage, fullUrl);
                                errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, errorMessage, operationErrorCode: responseCode));
                                return;
                            }

                            if (responseCode >= k_ServerErrorResponseCode)
                            {
                                retryCallbackAction?.Invoke(responseCode);
                                return;
                            }

                            if (responseCode is >= k_ClientErrorResponseCode and < k_ServerErrorResponseCode)
                            {
                                if (!k_KnownErrors.TryGetValue(request.responseCode, out var errorCodeMessage))
                                    errorCodeMessage = k_KnownErrors[k_GeneralClientError];
                                errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, string.Format(L10n.Tr("[Error {0}] {1}. {2} (URL: {3})"), responseCode, errorCodeMessage, k_ErrorMessage, fullUrl), operationErrorCode: responseCode));
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
                                    var parsedErrorMessage = parsedResult.GetString("errorMessage");
                                    var finalErrorMessage = operationErrorCode != -1
                                        ? string.Format(L10n.Tr("[Error {0}] {1} (URL: {2})"), operationErrorCode, parsedErrorMessage, fullUrl)
                                        : string.Format(L10n.Tr("{0} (URL: {1})"), parsedErrorMessage, fullUrl);
                                    errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, finalErrorMessage, operationErrorCode: operationErrorCode));
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
                                if (!k_KnownErrors.TryGetValue(lastResponseCode, out var errorCodeMessage))
                                    errorCodeMessage = k_KnownErrors[k_GeneralServerError];
                                errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, string.Format(L10n.Tr("[Error {0}] {1}. {2} (URL: {3})"), lastResponseCode, errorCodeMessage, k_ErrorMessage, fullUrl), operationErrorCode: lastResponseCode));
                            }
                            else
                            {
                                var finalErrorMessage = lastResponseCode != -1
                                    ? string.Format(L10n.Tr("[Error {0}] {1} (URL: {2})"), lastResponseCode, k_ErrorMessage, fullUrl)
                                    : string.Format(L10n.Tr("{0} (URL: {1})"), k_ErrorMessage, fullUrl);
                                errorCallback?.Invoke(new UIError(UIErrorCode.AssetStoreRestApiError, finalErrorMessage, operationErrorCode: lastResponseCode));
                            }
                        }
                    }

                    DoHttpRequest(RetryCallbackAction);
                },
                errorCallback);
        }
    }
}
