// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI.Internal;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile;

[VisibleToOtherModules]
internal class PlatformPackageServiceInfoProvider
{
    public Action OnPackageInfoUpdated;
    public RequestState currentRequestState { get; private set; }
    public enum RequestState
    {
        Pending,
        Success,
        Failure,
        Timeout
    }

    List<PlatformPackageServiceInfo> m_PackageServiceInfoList = new List<PlatformPackageServiceInfo>();
    readonly HttpClientFactory m_HttpClientFactory = new HttpClientFactory();
    readonly DateTimeProxy m_DateTimeProxy = new DateTimeProxy();
    readonly UnityConnectProxy m_UnityConnectProxy = new UnityConnectProxy();
    readonly UnityOAuthProxy m_UnityOAuthProxy = new UnityOAuthProxy();
    readonly AssetStoreOAuth m_AssetStoreOAuth;

    string m_Host = string.Empty;
    string host
    {
        get
        {
            if (string.IsNullOrEmpty(m_Host))
                m_Host = m_UnityConnectProxy.GetConfigurationURL(Connect.CloudConfigUrl.CloudPackagesApi);
            return m_Host;
        }
    }

    const int k_TimeoutSeconds = 30;
    const string k_ProductInfoUri = "/-/api/product";

    class PlatformPackageServiceInfo
    {
        public string name { get; }
        public long productId { get; }
        public PackageManager.PackageInfo packageInfo { get; }
        public Texture thumbnail;

        public PlatformPackageServiceInfo(PackageManager.PackageInfo packageInfo)
        {
            name = packageInfo.name;
            this.productId = long.TryParse(packageInfo.assetStore?.productId, out var productId) ? productId : -1;
            this.packageInfo = packageInfo;
            thumbnail = null;
        }

        public void Dispose()
        {
            if (thumbnail != null)
            {
                UnityEngine.Object.DestroyImmediate(thumbnail);
                thumbnail = null;
            }
        }
    }

    internal PlatformPackageServiceInfoProvider()
    {
        m_AssetStoreOAuth = new AssetStoreOAuth(m_DateTimeProxy, m_UnityConnectProxy, m_UnityOAuthProxy, m_HttpClientFactory);
    }

    public void Dispose()
    {
        foreach (var packageInfo in m_PackageServiceInfoList)
            packageInfo.Dispose();
    }

    /// <summary>
    /// Fetches package information for all platform packages.
    /// </summary>
    /// <returns>True if the fetch was initiated.</returns>
    public bool FetchInfo()
    {
        if (currentRequestState == RequestState.Success)
            return false;

        currentRequestState = RequestState.Pending;
        FetchPackageServiceInfo();
        return true;
    }

    public PackageManager.PackageInfo GetPackageInfo(string packageName)
    {
        var packageServiceInfo = m_PackageServiceInfoList.Find(p => p.name == packageName);
        return packageServiceInfo?.packageInfo;
    }

    public Texture GetThumbnail(string packageName)
    {
        var packageServiceInfo = m_PackageServiceInfoList.Find(p => p.name == packageName);
        return packageServiceInfo?.thumbnail;
    }

    async void FetchPackageServiceInfo()
    {
        var packageInfoSearchTasks = new List<Task<bool>>();
        var packageNames = BuildProfileModuleUtil.GetAllPlatformPackageNames();

        foreach (var name in packageNames)
        {
            var packageInfo = PackageManager.PackageInfo.FindForPackageName(name);
            if (packageInfo != null)
            {
                var packageServiceInfo = new PlatformPackageServiceInfo(packageInfo);
                m_PackageServiceInfoList.Add(packageServiceInfo);

                if (packageServiceInfo.productId != -1)
                    packageInfoSearchTasks.Add(FetchThumbnailFromAssetStoreAsync(packageServiceInfo.productId));
            }
            else
                packageInfoSearchTasks.Add(FetchPackageServiceInfoFromServerAsync(name));
        }

        if (packageInfoSearchTasks.Count == 0)
        {
            currentRequestState = RequestState.Success;
            OnPackageInfoUpdated?.Invoke();
            return;
        }

        var searchTask = Task.WhenAll(packageInfoSearchTasks);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(k_TimeoutSeconds));
        
        var completedTask = await Task.WhenAny(searchTask, timeoutTask);

        if (completedTask == timeoutTask)
            currentRequestState = RequestState.Timeout;
        else
        {
            bool allTasksSucceeded = true;
            foreach (var task in packageInfoSearchTasks)
            {
                if (task.IsFaulted || task.IsCanceled || !task.Result)
                {
                    allTasksSucceeded = false;
                    break;
                }
            }

            if (allTasksSucceeded)
                currentRequestState = RequestState.Success;
            else
                currentRequestState = RequestState.Failure;
        }

        OnPackageInfoUpdated?.Invoke();
    }

    Task<bool> FetchPackageServiceInfoFromServerAsync(string packageName)
    {
        var tcs = new TaskCompletionSource<bool>();
        var request = Client.Search(packageName);

        async void OnUpdate()
        {
            if (!request.IsCompleted)
                return;

            EditorApplication.update -= OnUpdate;
            if (request.Status == StatusCode.Success && request.Result.Length > 0)
            {
                var packageInfo = request.Result[0];
                if (packageInfo == null)
                {
                    tcs.SetResult(false);
                    return;
                }

                try
                {
                    var packageInformation = new PlatformPackageServiceInfo(packageInfo);
                    m_PackageServiceInfoList.Add(packageInformation);

                    if (packageInformation.productId != -1)
                    {
                        var thumbnailResult = await FetchThumbnailFromAssetStoreAsync(packageInformation.productId);
                        if (!thumbnailResult)
                        {
                            tcs.SetResult(false);
                            return;
                        }
                    }
                }
                catch
                {
                    tcs.SetResult(false);
                    return;
                }

                tcs.SetResult(true);
            }
            else
                tcs.SetResult(false);
        }

        EditorApplication.update += OnUpdate;
        return tcs.Task;
    }

    Task<bool> FetchThumbnailFromAssetStoreAsync(long productId)
    {
        var tcs = new TaskCompletionSource<bool>();
        FetchProductInfoFromAssetStore(productId, async result =>
        {
            try
            {
                if (result == null || result.ContainsKey("errorMessage"))
                {
                    tcs.SetResult(false);
                    return;
                }

                var imageUrl = GetThumbnailUrlFromProductDetails(result);
                if (string.IsNullOrEmpty(imageUrl))
                {
                    tcs.SetResult(false);
                    return;
                }

                var image = await DownloadImageByUrlAsync(imageUrl);
                if (image == null)
                {
                    tcs.SetResult(false);
                    return;
                }

                var packageServiceInfo = m_PackageServiceInfoList.Find(p => p.productId == productId);
                if (packageServiceInfo == null)
                {
                    tcs.SetResult(false);
                    return;
                }

                image.hideFlags = HideFlags.HideAndDontSave;
                packageServiceInfo.thumbnail = image;
                tcs.SetResult(true);
            }
            catch
            {
                tcs.SetResult(false);
            }
        });
        return tcs.Task;
    }

    string GetThumbnailUrlFromProductDetails(IDictionary<string, object> productDetail)
    {
        if (productDetail == null)
            return string.Empty;

        var mainImageDictionary = productDetail.GetDictionary("mainImage");
        var thumbnailUrl = mainImageDictionary?.GetString("small_v2");
        if (string.IsNullOrEmpty(thumbnailUrl))
            return string.Empty;

        thumbnailUrl = thumbnailUrl.Replace("//d2ujflorbtfzji.cloudfront.net/", "//assetstorev1-prd-cdn.unity3d.com/");
        return "http:" + thumbnailUrl;
    }

    Task<Texture> DownloadImageByUrlAsync(string url)
    {
        var tcs = new TaskCompletionSource<Texture>();

        if (string.IsNullOrEmpty(url))
        {
            tcs.SetResult(null);
            return tcs.Task;
        }

        var httpRequest = m_HttpClientFactory.GetASyncHTTPClient(url);
        httpRequest.doneCallback = httpClient =>
        {
            if (httpClient.IsSuccess() && httpClient.texture != null)
                tcs.SetResult(httpClient.texture);
            else
                tcs.SetResult(null);
        };
        httpRequest.Begin();

        return tcs.Task;
    }

    void FetchProductInfoFromAssetStore(long productId, Action<Dictionary<string, object>> value)
    {
        var url = $"{host}{k_ProductInfoUri}/{productId}";
        var httpRequest = m_HttpClientFactory.GetASyncHTTPClient(url);
        m_AssetStoreOAuth.FetchAccessToken(
            token =>
            {
                httpRequest.tag = $"GetPlatformPackageProductDetail{productId}";
                httpRequest.header["Content-Type"] = "application/json";
                httpRequest.header["Authorization"] = "Bearer " + token.accessToken;
                httpRequest.doneCallback = request =>
                {
                    if (!request.IsSuccess())
                    {
                        value?.Invoke(null);
                        return;
                    }

                    var parsedResult = m_HttpClientFactory.ParseResponseAsDictionary(request);
                    value?.Invoke(parsedResult);
                };
                httpRequest.Begin();
            },
            error =>
            {
                value?.Invoke(null);
            });
    }
}
