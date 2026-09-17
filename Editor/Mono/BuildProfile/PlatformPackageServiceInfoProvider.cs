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
internal class PlatformPackageServiceInfoProvider : IPackageServiceInfoProvider
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

    /// <summary>
    /// Product info requests in flight keyed by package name.
    /// </summary>
    readonly Dictionary<string, Task<MarketplacePackageInfo>> m_InFlightProductInfo = new Dictionary<string, Task<MarketplacePackageInfo>>();

    /// <summary>
    /// Claims in flight, keyed by organization and product.
    /// </summary>
    readonly Dictionary<string, Task<EntitlementClaimResult>> m_InFlightClaims = new Dictionary<string, Task<EntitlementClaimResult>>();

    Task<string> m_InFlightOrganizationIdWait;

    IMarketplaceEditorApiClient m_ApiClient;
    IMarketplaceEditorApiClient apiClient => m_ApiClient ??= new MarketplaceEditorApiClient();

    public bool isUserLoggedIn => UnityConnect.instance.loggedIn;
    public void ShowLogin() => UnityConnect.instance.ShowLogin();

    const int k_TimeoutSeconds = 30;

    /// <summary>
    /// Must stay below BuildProfileEntitlementClaimInfo.k_AttemptTimeoutSeconds. The wait runs inside a
    /// claim attempt, so if the attempt times out first the caller sees a cancellation instead of
    /// NoOrganizationIdException, and the stage reports a timeout rather than the missing organization.
    /// </summary>
    const int k_OrganizationIdTimeoutSeconds = 20;
    const int k_OrganizationIdPollMilliseconds = 250;

    class PlatformPackageServiceInfo
    {
        public string name { get; }
        public long productId { get; }
        public PackageManager.PackageInfo packageInfo { get; }
        public Texture thumbnail;
        public MarketplacePackageInfo marketplaceInfo;

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

    public bool HasPackageInfo(string packageName) => GetPackageInfo(packageName) != null;

    public PackageManager.PackageInfo GetPackageInfo(string packageName)
    {
        return m_PackageServiceInfos.TryGetValue(packageName, out var packageServiceInfo) ? packageServiceInfo.packageInfo : null;
    }

    public Texture GetThumbnail(string packageName)
    {
        return m_PackageServiceInfos.TryGetValue(packageName, out var packageServiceInfo) ? packageServiceInfo.thumbnail : null;
    }

    public string GetMarketplaceProductId(string packageName) => GetMarketplaceInfo(packageName)?.productId;

    /// <summary>
    /// Whether the user already holds the entitlement for the package. Null means unknown, which is not the same
    /// as not entitled and must not be collapsed into it.
    /// </summary>
    public bool? HasMarketplaceEntitlement(string packageName) => GetMarketplaceInfo(packageName)?.entitlement?.hasEntitlement;

    /// <summary>
    /// URL of the license the package is distributed under. Null when the product names no license.
    /// </summary>
    public string GetLicenseUrl(string packageName) => GetMarketplaceInfo(packageName)?.license?.url;

    /// <summary>
    /// Short name to label <see cref="GetLicenseUrl"/> with.
    /// </summary>
    public string GetLicenseName(string packageName) => GetMarketplaceInfo(packageName)?.license?.name;

    /// <summary>
    /// True when the package uses the Enterprise licensing model, meaning access is granted by an entitlement.
    /// </summary>
    public bool IsEnterprisePackage(string packageName)
    {
        return GetPackageInfo(packageName)?.entitlements?.licensingModel == EntitlementLicensingModel.Enterprise;
    }

    /// <summary>
    /// True when the package uses the Asset Store licensing model, meaning access depends on an
    /// entitlement the user can claim.
    /// </summary>
    public bool IsAssetStorePackage(string packageName)
    {
        return GetPackageInfo(packageName)?.entitlements?.licensingModel == EntitlementLicensingModel.AssetStore;
    }

    public Task<MarketplacePackageInfo> GetOrFetchProductInfoAsync(string packageName)
    {
        var resolved = GetMarketplaceInfo(packageName);
        if (resolved != null)
            return Task.FromResult(resolved);

        if (m_InFlightProductInfo.TryGetValue(packageName, out var inFlight))
            return inFlight;

        var request = FetchProductInfoCoreAsync(packageName);
        if (!request.IsCompleted)
            m_InFlightProductInfo[packageName] = request;

        return request;
    }

    public async Task<EntitlementClaimResult> ClaimEntitlementAsync(string productId)
    {
        var claimOrganizationId = await GetOrganizationIdAsync();
        var key = claimOrganizationId + ":" + productId;

        if (m_InFlightClaims.TryGetValue(key, out var inFlight))
            return await inFlight;

        var request = ClaimEntitlementCoreAsync(key, claimOrganizationId, productId);
        if (!request.IsCompleted)
            m_InFlightClaims[key] = request;

        return await request;
    }

    MarketplacePackageInfo GetMarketplaceInfo(string packageName)
    {
        return m_PackageServiceInfos.TryGetValue(packageName, out var packageServiceInfo) ? packageServiceInfo.marketplaceInfo : null;
    }

    async Task<MarketplacePackageInfo> FetchProductInfoCoreAsync(string packageName)
    {
        try
        {
            var productInfo = await apiClient.GetPackageInfoAsync(packageName);
            if (productInfo != null && m_PackageServiceInfos.TryGetValue(packageName, out var packageServiceInfo))
                packageServiceInfo.marketplaceInfo = productInfo;

            return productInfo;
        }
        finally
        {
            m_InFlightProductInfo.Remove(packageName);
        }
    }

    async Task<EntitlementClaimResult> ClaimEntitlementCoreAsync(string key, string claimOrganizationId, string productId)
    {
        try
        {
            return await apiClient.ClaimEntitlementAsync(new EntitlementClaimArgs(claimOrganizationId, productId));
        }
        finally
        {
            m_InFlightClaims.Remove(key);
        }
    }

    /// <summary>
    /// The project-linked organization's Genesis ID, which is what the services gateway expects an
    /// entitlement to be claimed against. Empty until the project state machine has resolved it.
    /// </summary>
    string organizationId => CloudProjectSettings.organizationKey;

    Task<string> GetOrganizationIdAsync()
    {
        if (m_InFlightOrganizationIdWait != null)
            return m_InFlightOrganizationIdWait;

        var request = WaitForOrganizationIdAsync();
        if (!request.IsCompleted)
            m_InFlightOrganizationIdWait = request;

        return request;
    }

    /// <summary>
    /// Polls until the organization ID resolves or it is clear that none is coming.
    /// Required for claiming entitlements for <see cref="BuildProfile.CreateBuildProfile"/>
    /// at project start up and batch mode.
    /// </summary>
    async Task<string> WaitForOrganizationIdAsync()
    {
        try
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(k_OrganizationIdTimeoutSeconds);
            while (true)
            {
                var resolved = organizationId;
                if (!string.IsNullOrEmpty(resolved))
                    return resolved;

                if (UnityConnect.instance.isUserInfoReady && !UnityConnect.instance.loggedIn)
                    throw new NoOrganizationIdException("No user is signed in.");

                if (UnityConnect.instance.projectInfo.valid || DateTime.UtcNow >= deadline)
                    throw new NoOrganizationIdException("The project's organization could not be determined.");

                await Task.Delay(k_OrganizationIdPollMilliseconds);
            }
        }
        finally
        {
            m_InFlightOrganizationIdWait = null;
        }
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
            var productInfo = await GetOrFetchProductInfoAsync(packageName);
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
