// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.Connect;
using UnityEditor.Marketplace;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile;

[VisibleToOtherModules]
internal class PlatformPackageServiceInfoProvider
{
    public event Action OnPackageInfoUpdated;
    public event Action OnUserLoginStateChanged;
    public RequestState currentRequestState { get; private set; }
    public enum RequestState
    {
        Pending,
        Success,
        Failure,
        Timeout
    }

    readonly Dictionary<string, PlatformPackageServiceInfo> m_PackageServiceInfos = new Dictionary<string, PlatformPackageServiceInfo>();
    IMarketplaceEditorApiClient m_ApiClient;
    IMarketplaceEditorApiClient apiClient => m_ApiClient ??= new MarketplaceEditorApiClient();

    public bool isUserLoggedIn => UnityConnect.instance.loggedIn;
    public void ShowLogin() => UnityConnect.instance.ShowLogin();

    const int k_TimeoutSeconds = 30;

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
        UnityConnect.instance.StateChanged += OnStateChanged;
    }

    public void Dispose()
    {
        UnityConnect.instance.StateChanged -= OnStateChanged;
        foreach (var packageInfo in m_PackageServiceInfos.Values)
            packageInfo.Dispose();
    }

    void OnStateChanged(ConnectInfo state)
    {
        OnUserLoginStateChanged?.Invoke();
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
        return m_PackageServiceInfos.TryGetValue(packageName, out var packageServiceInfo) ? packageServiceInfo.packageInfo : null;
    }

    public Texture GetThumbnail(string packageName)
    {
        return m_PackageServiceInfos.TryGetValue(packageName, out var packageServiceInfo) ? packageServiceInfo.thumbnail : null;
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
                var packageServiceInfo = Store(packageInfo);

                if (packageServiceInfo.productId != -1)
                    packageInfoSearchTasks.Add(FetchProductInfoAsync(packageServiceInfo.name));
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
                    var packageInformation = Store(packageInfo);
                    if (packageInformation.productId != -1)
                    {
                        var productInfoResult = await FetchProductInfoAsync(packageInformation.name);
                        if (!productInfoResult)
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

    async Task<bool> FetchProductInfoAsync(string packageName)
    {
        try
        {
            var productInfo = await apiClient.GetPackageInfoAsync(packageName);
            if (productInfo == null || !m_PackageServiceInfos.TryGetValue(packageName, out var packageServiceInfo))
                return false;

            if (string.IsNullOrEmpty(productInfo.thumbnail))
                return false;

            var image = await DownloadImageByUrlAsync(productInfo.thumbnail);
            if (image == null)
                return false;

            image.hideFlags = HideFlags.HideAndDontSave;
            packageServiceInfo.thumbnail = image;
            return true;
        }
        catch
        {
            return false;
        }
    }

    PlatformPackageServiceInfo Store(PackageManager.PackageInfo packageInfo)
    {
        var packageServiceInfo = new PlatformPackageServiceInfo(packageInfo);
        if (m_PackageServiceInfos.TryGetValue(packageServiceInfo.name, out var existing))
            existing.Dispose();

        m_PackageServiceInfos[packageServiceInfo.name] = packageServiceInfo;
        return packageServiceInfo;
    }

    Task<Texture> DownloadImageByUrlAsync(string url)
    {
        var tcs = new TaskCompletionSource<Texture>();

        if (string.IsNullOrEmpty(url))
        {
            tcs.SetResult(null);
            return tcs.Task;
        }

        var httpRequest = new AsyncHTTPClient(url);
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
}
