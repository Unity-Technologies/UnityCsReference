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

        internal class AssetStoreRestAPIInternal : IAssetStoreRestAPI
        {
            private static AssetStoreRestAPIInternal s_Instance;
            public static AssetStoreRestAPIInternal instance => s_Instance ?? (s_Instance = new AssetStoreRestAPIInternal());

            private string m_Host = "";
            private const int kDefaultLimit = 100;
            private const string kListUri = "/-/api/purchases";
            private const string kDetailUri = "/-/api/product";
            private const string kUpdateUri = "/-/api/legacy-package-update-info";
            private const string kDownloadUri = "/-/api/legacy-package-download-info";

            private IASyncHTTPClientFactory m_AsyncHTTPClient;

            private AssetStoreRestAPIInternal()
            {
                m_AsyncHTTPClient = new ASyncHTTPClientFactory();
                m_Host = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPackagesApi);
            }

            public void GetProductIDList(int startIndex, int limit, string searchText, Action<ProductList> doneCallbackAction)
            {
                var returnList = new ProductList
                {
                    total = 0,
                    startIndex = startIndex,
                    isValid = false,
                    searchText = searchText,
                    list = new List<long>()
                };

                AssetStoreOAuth.instance.FetchUserInfo(userInfo =>
                {
                    if (!userInfo.isValid)
                    {
                        returnList.errorMessage = userInfo.errorMessage;
                        doneCallbackAction?.Invoke(returnList);
                        return;
                    }

                    limit = limit > 0 ? limit : kDefaultLimit;
                    searchText = string.IsNullOrEmpty(searchText) ? "" : searchText;
                    var httpRequest = m_AsyncHTTPClient.GetASyncHTTPClient($"{m_Host}{kListUri}?offset={startIndex}&limit={limit}&query={System.Uri.EscapeDataString(searchText)}");
                    httpRequest.header["Authorization"] = "Bearer " + userInfo.accessToken.access_token;
                    httpRequest.doneCallback = httpClient =>
                    {
                        var errorMessage = "Failed to parse JSON.";
                        if (httpClient.IsSuccess() && httpClient.responseCode == 200)
                        {
                            try
                            {
                                var res = Json.Deserialize(httpClient.text) as Dictionary<string, object>;
                                if (res != null)
                                {
                                    var total = (long)res["total"];
                                    returnList.total = total;
                                    returnList.isValid = true;

                                    if (total == 0)
                                    {
                                        doneCallbackAction?.Invoke(returnList);
                                        return;
                                    }

                                    var results = res["results"] as IList<object>;
                                    foreach (var result in results)
                                    {
                                        var item = result as Dictionary<string, object>;
                                        var packageId = item["packageId"];
                                        returnList.list.Add((long)packageId);
                                    }

                                    doneCallbackAction?.Invoke(returnList);
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                errorMessage = e.Message;
                            }
                        }
                        else
                        {
                            errorMessage = httpClient.text;
                        }

                        returnList.errorMessage = errorMessage;
                        doneCallbackAction?.Invoke(returnList);
                    };
                    httpRequest.Begin();
                });
            }

            public void GetProductDetail(long productID, Action<Dictionary<string, object>> doneCallbackAction)
            {
                AssetStoreOAuth.instance.FetchUserInfo(userInfo =>
                {
                    if (!userInfo.isValid)
                    {
                        var ret = new Dictionary<string, object>();
                        ret["errorMessage"] = userInfo.errorMessage;
                        doneCallbackAction?.Invoke(ret);
                        return;
                    }

                    var httpRequest = m_AsyncHTTPClient.GetASyncHTTPClient($"{m_Host}{kDetailUri}/{productID}");
                    httpRequest.header["Authorization"] = "Bearer " + userInfo.accessToken.access_token;

                    var etag = AssetStoreCache.instance.GetLastETag(productID);
                    httpRequest.header["If-None-Match"] = etag.Replace("\"", "\\\"");

                    httpRequest.doneCallback = httpClient =>
                    {
                        var ret = new Dictionary<string, object>();
                        var errorMessage = "Failed to parse JSON.";
                        if (httpClient.IsSuccess() && httpClient.responseCode == 200)
                        {
                            try
                            {
                                if (httpClient.responseHeader.ContainsKey("ETag"))
                                {
                                    etag = httpClient.responseHeader["ETag"];
                                }

                                ret = Json.Deserialize(httpClient.text) as Dictionary<string, object>;
                                if (ret != null)
                                {
                                    AssetStoreCache.instance.SetLastETag(productID, etag);
                                    doneCallbackAction?.Invoke(ret);
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                errorMessage = e.Message;
                            }
                        }
                        else
                        {
                            errorMessage = httpClient.text;
                        }

                        ret = new Dictionary<string, object> {["errorMessage"] = errorMessage};
                        doneCallbackAction?.Invoke(ret);
                    };
                    httpRequest.Begin();
                });
            }

            public void GetDownloadDetail(long productID, Action<DownloadInformation> doneCallbackAction)
            {
                var downloadInfo = new DownloadInformation
                {
                    isValid = false
                };

                AssetStoreOAuth.instance.FetchUserInfo(userInfo =>
                {
                    if (!userInfo.isValid)
                    {
                        downloadInfo.errorMessage = userInfo.errorMessage;
                        doneCallbackAction?.Invoke(downloadInfo);
                        return;
                    }

                    var httpRequest = m_AsyncHTTPClient.GetASyncHTTPClient($"{m_Host}{kDownloadUri}/{productID}");
                    httpRequest.header["Content-Type"] = "application/json";
                    httpRequest.header["Authorization"] = "Bearer " + userInfo.accessToken.access_token;
                    httpRequest.doneCallback = httpClient =>
                    {
                        var errorMessage = "Failed to parse JSON.";
                        if (httpClient.IsSuccess() && httpClient.responseCode == 200)
                        {
                            try
                            {
                                var res = Json.Deserialize(httpClient.text) as Dictionary<string, object>;
                                if (res != null)
                                {
                                    var downloadRes = res["result"] as Dictionary<string, object>;
                                    var download = downloadRes["download"] as Dictionary<string, object>;
                                    downloadInfo.isValid = true;
                                    downloadInfo.CategoryName = download["filename_safe_category_name"] as string;
                                    downloadInfo.PackageName = download["filename_safe_package_name"] as string;
                                    downloadInfo.PublisherName = download["filename_safe_publisher_name"] as string;
                                    downloadInfo.PackageId = download["id"] as string;
                                    downloadInfo.Key = download["key"] as string;
                                    downloadInfo.Url = download["url"] as string;
                                    doneCallbackAction?.Invoke(downloadInfo);
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                errorMessage = e.Message;
                            }
                        }
                        else
                            errorMessage = httpClient.text;

                        downloadInfo.errorMessage = errorMessage;
                        doneCallbackAction?.Invoke(downloadInfo);
                    };
                    httpRequest.Begin();
                });
            }

            public void GetProductUpdateDetail(IEnumerable<LocalInfo> localInfos, Action<Dictionary<string, object>> doneCallbackAction)
            {
                AssetStoreOAuth.instance.FetchUserInfo(userInfo =>
                {
                    if (!userInfo.isValid)
                    {
                        var ret = new Dictionary<string, object>();
                        ret["errorMessage"] = userInfo.errorMessage;
                        doneCallbackAction?.Invoke(ret);
                        return;
                    }

                    if (localInfos == null || !localInfos.Any())
                    {
                        doneCallbackAction?.Invoke(new Dictionary<string, object>());
                        return;
                    }

                    var packageList = new List<Dictionary<string, string>>();

                    foreach (var info in localInfos)
                    {
                        var dictData = new Dictionary<string, string>();

                        dictData["local_path"] = info?.packagePath ?? string.Empty;
                        dictData["id"] = info?.id ?? string.Empty;
                        dictData["version"] = info?.versionString ?? string.Empty;
                        dictData["version_id"] = info?.versionId ?? string.Empty;
                        packageList.Add(dictData);
                    }

                    var data = Json.Serialize(packageList);
                    var url = $"{m_Host}{kUpdateUri}";

                    var httpRequest = m_AsyncHTTPClient.GetASyncHTTPClient(url, "POST");
                    httpRequest.postData = data;
                    httpRequest.header["Content-Type"] = "application/json";
                    httpRequest.header["Authorization"] = "Bearer " + userInfo.accessToken.access_token;
                    httpRequest.doneCallback = httpClient =>
                    {
                        var errorMessage = "Failed to parse JSON.";
                        if (httpClient.IsSuccess() && httpClient.responseCode == 200)
                        {
                            try
                            {
                                var res = Json.Deserialize(httpClient.text) as Dictionary<string, object>;
                                if (res != null)
                                {
                                    var result = res["result"] as Dictionary<string, object>;
                                    doneCallbackAction?.Invoke(result);
                                    return;
                                }
                            }
                            catch (Exception e)
                            {
                                errorMessage = e.Message;
                            }
                        }
                        else
                        {
                            errorMessage = httpClient.text;
                        }

                        var ret = new Dictionary<string, object> {["errorMessage"] = errorMessage};
                        doneCallbackAction?.Invoke(ret);
                    };
                    httpRequest.Begin();
                });
            }
        }
    }
}
