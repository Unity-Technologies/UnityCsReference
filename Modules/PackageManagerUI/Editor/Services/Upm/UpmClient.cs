// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmClient : ISerializationCallbackReceiver
    {
        private static readonly string[] k_UnityRegistryUrlsHosts = { ".unity.com", ".unity3d.com" };

        public virtual event Action<IOperation> onListOperation = delegate {};
        public virtual event Action<IOperation> onSearchAllOperation = delegate {};
        public virtual event Action<IOperation> onExtraFetchOperation = delegate {};
        public virtual event Action<IOperation> onRemoveOperation = delegate {};
        public virtual event Action<IOperation> onAddOperation = delegate {};
        public virtual event Action<IOperation> onEmbedOperation = delegate {};
        public virtual event Action<UpmAddAndRemoveOperation> onAddAndRemoveOperation = delegate {};

        public virtual event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};

        [SerializeField]
        private UpmSearchOperation m_SearchOperation;
        private UpmSearchOperation searchOperation => CreateOperation(ref m_SearchOperation);
        [SerializeField]
        private UpmSearchOperation m_SearchOfflineOperation;
        private UpmSearchOperation searchOfflineOperation => CreateOperation(ref m_SearchOfflineOperation);
        [SerializeField]
        private UpmListOperation m_ListOperation;
        private UpmListOperation listOperation => CreateOperation(ref m_ListOperation);
        [SerializeField]
        private UpmListOperation m_ListOfflineOperation;
        private UpmListOperation listOfflineOperation => CreateOperation(ref m_ListOfflineOperation);

        [SerializeField]
        private UpmAddOperation m_AddOperation;
        private UpmAddOperation addOperation => CreateOperation(ref m_AddOperation);
        [SerializeField]
        private UpmAddAndRemoveOperation m_AddAndRemoveOperation;
        private UpmAddAndRemoveOperation addAndRemoveOperation => CreateOperation(ref m_AddAndRemoveOperation);
        [SerializeField]
        private UpmRemoveOperation m_RemoveOperation;
        private UpmRemoveOperation removeOperation => CreateOperation(ref m_RemoveOperation);
        [SerializeField]
        private UpmEmbedOperation m_EmbedOperation;
        private UpmEmbedOperation embedOperation => CreateOperation(ref m_EmbedOperation);

        private readonly Dictionary<string, UpmSearchOperation> m_ExtraFetchOperations = new Dictionary<string, UpmSearchOperation>();

        [SerializeField]
        private string[] m_SerializedRegistryUrlsKeys;

        [SerializeField]
        private RegistryType[] m_SerializedRegistryUrlsValues;

        internal Dictionary<string, RegistryType> m_RegistryUrls = new Dictionary<string, RegistryType>();

        // a list of unique ids (could be specialUniqueId or packageId)
        [SerializeField]
        private List<string> m_SpecialInstallations = new List<string>();
        public List<string> specialInstallations => m_SpecialInstallations;

        private UpmPackageFactory m_PackageFactory = new UpmPackageFactory();

        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private FetchStatusTracker m_FetchStatusTracker;
        [NonSerialized]
        private IOProxy m_IOProxy;
        [NonSerialized]
        private ClientProxy m_ClientProxy;
        [NonSerialized]
        private ApplicationProxy m_ApplicationProxy;
        public void ResolveDependencies(UpmCache upmCache,
            FetchStatusTracker fetchStatusTracker,
            IOProxy IOProxy,
            PackageManagerProjectSettingsProxy settingsProxy,
            ClientProxy clientProxy,
            ApplicationProxy applicationProxy)
        {
            m_UpmCache = upmCache;
            m_FetchStatusTracker = fetchStatusTracker;
            m_IOProxy = IOProxy;
            m_ClientProxy = clientProxy;
            m_ApplicationProxy = applicationProxy;

            m_SearchOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_SearchOfflineOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_ListOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_ListOfflineOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_AddOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_RemoveOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_EmbedOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_AddAndRemoveOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);

            m_PackageFactory.ResolveDependencies(upmCache, this, settingsProxy);
        }

        public virtual bool IsAnyExperimentalPackagesInUse()
        {
            return PackageInfo.GetAll().Any(info => (info.version.Contains("-preview") || info.version.Contains("-exp.") || info.version.StartsWith("0.")) && IsUnityPackage(info));
        }

        public void OnBeforeSerialize()
        {
            m_SerializedRegistryUrlsKeys = m_RegistryUrls?.Keys.ToArray() ?? new string[0];
            m_SerializedRegistryUrlsValues = m_RegistryUrls?.Values.ToArray() ?? new RegistryType[0];
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedRegistryUrlsKeys.Length; i++)
                m_RegistryUrls[m_SerializedRegistryUrlsKeys[i]] = m_SerializedRegistryUrlsValues[i];
        }

        public virtual bool isAddRemoveOrEmbedInProgress
        {
            get { return addOperation.isInProgress || removeOperation.isInProgress || addAndRemoveOperation.isInProgress || embedOperation.isInProgress; }
        }

        public virtual bool IsEmbedInProgress(string packageName)
        {
            return embedOperation.isInProgress && embedOperation.packageName == packageName;
        }

        public virtual bool IsRemoveInProgress(string packageName)
        {
            return (removeOperation.isInProgress && removeOperation.packageName == packageName)
                || (addAndRemoveOperation.isInProgress && addAndRemoveOperation.packagesNamesToRemove.Contains(packageName));
        }

        public virtual bool IsAddInProgress(string packageId)
        {
            return (addOperation.isInProgress && addOperation.packageId == packageId)
                || (addAndRemoveOperation.isInProgress && addAndRemoveOperation.packageIdsToAdd.Contains(packageId));
        }

        public virtual void AddById(string packageId)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;
            var packageName = packageId.Split(new[] { '@' }, 2)[0];
            addOperation.Add(packageId, m_UpmCache.GetProductIdByName(packageName));
            SetupAddOperation();
        }

        private void SetupAddOperation()
        {
            addOperation.onProcessResult += (request) => OnProcessAddResult(addOperation, request);
            addOperation.onOperationError += (op, error) =>
            {
                var packageId = string.IsNullOrEmpty(addOperation.packageId) ? addOperation.specialUniqueId : addOperation.packageId;
                Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Error adding package: {0}."), packageId));
            };
            onAddOperation(addOperation);
        }

        private void OnProcessAddResult(IOperation operation, Request<PackageInfo> request)
        {
            var packageInfo = request.Result;
            var specialUniqueId = (operation as UpmAddOperation)?.specialUniqueId;

            m_UpmCache.SetInstalledPackageInfo(packageInfo, !string.IsNullOrEmpty(specialUniqueId));

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageAddedOrUpdated(packageInfo);
            });

            // do a list offline to refresh all the dependencies
            List(true);
        }

        public virtual void AddByPath(string path)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;

            try
            {
                var tempPackageId = GetTempPackageIdFromPath(path);
                addOperation.AddByUrlOrPath(tempPackageId, PackageTag.Local);
                SetupAddOperation();
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager] Cannot add package {path}: {e.Message}");
            }
        }

        public string GetTempPackageIdFromPath(string path)
        {
            path = path.Replace('\\', '/');
            var projectPath = m_IOProxy.GetProjectDirectory().Replace('\\', '/') + '/';
            if (!path.StartsWith(projectPath))
                return $"file:{path}";

            const string packageFolderPrefix = "Packages/";
            var relativePathToProjectRoot = path.Substring(projectPath.Length);
            if (relativePathToProjectRoot.StartsWith(packageFolderPrefix, StringComparison.InvariantCultureIgnoreCase))
                return $"file:{relativePathToProjectRoot.Substring(packageFolderPrefix.Length)}";
            else
                return $"file:../{relativePathToProjectRoot}";
        }

        public virtual void AddByUrl(string url)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;

            addOperation.AddByUrlOrPath(url, PackageTag.Git);
            SetupAddOperation();
        }

        public virtual void AddAndResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;
            addAndRemoveOperation.AddAndResetDependencies(packageId, dependencyPackagesNames);
            SetupAddAndRemoveOperation();
        }

        public virtual void ResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;
            addAndRemoveOperation.ResetDependencies(packageId, dependencyPackagesNames);
            SetupAddAndRemoveOperation();
        }

        private void SetupAddAndRemoveOperation()
        {
            addAndRemoveOperation.onProcessResult += OnProcessAddAndRemoveResult;
            addAndRemoveOperation.onOperationError += (op, error) =>
            {
                var packageIds = addAndRemoveOperation.packageIdsToAdd.Concat(addAndRemoveOperation.packagesNamesToRemove);
                Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Error adding/removing packages: {0}."), string.Join(",", packageIds.ToArray())));
            };
            onAddAndRemoveOperation?.Invoke(addAndRemoveOperation);
        }

        private void OnProcessAddAndRemoveResult(Request<PackageCollection> request)
        {
            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var packageInfo in addAndRemoveOperation.packagesNamesToRemove.Select(name => m_UpmCache.GetInstalledPackageInfo(name)).Where(p => p != null))
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageRemoved(packageInfo);
            });

            m_UpmCache.SetInstalledPackageInfos(request.Result);

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var packageInfo in addAndRemoveOperation.packageIdsToAdd.Select(id => m_UpmCache.GetInstalledPackageInfoById(id)).Where(p => p != null))
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageAddedOrUpdated(packageInfo);
            });
        }

        public virtual void List(bool offlineMode = false)
        {
            var operation = offlineMode ? listOfflineOperation : listOperation;
            if (operation.isInProgress)
                operation.Cancel();
            var errorMessage = offlineMode ? L10n.Tr("Error fetching package list offline.") : L10n.Tr("Error fetching package list.");
            if (offlineMode)
                operation.ListOffline(listOperation.lastSuccessTimestamp);
            else
                operation.List();
            operation.onProcessResult += request => OnProcessListResult(request, offlineMode);
            operation.onOperationError += (op, error) => Debug.LogError($"{L10n.Tr("[Package Manager Window]")} {errorMessage}");
            onListOperation(operation);
        }

        private void OnProcessListResult(ListRequest request, bool offlineMode)
        {
            // skip operation when the result from the online operation is more up-to-date.
            if (offlineMode && listOfflineOperation.timestamp < listOperation.lastSuccessTimestamp)
                return;

            m_UpmCache.SetInstalledPackageInfos(request.Result, listOperation.lastSuccessTimestamp);
        }

        public virtual void EmbedByName(string packageName)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;
            embedOperation.Embed(packageName, m_UpmCache.GetProductIdByName(packageName));
            embedOperation.onProcessResult += (request) => OnProcessAddResult(embedOperation, request);
            embedOperation.onOperationError += (op, error) => Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Error embedding package: {0}."), embedOperation.packageName));
            onEmbedOperation(embedOperation);
        }

        public virtual void RemoveByName(string packageName)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;
            removeOperation.Remove(packageName, m_UpmCache.GetProductIdByName(packageName));
            SetupRemoveOperation();
        }

        public virtual void RemoveEmbeddedByName(string packageName)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;

            var packageInfo = m_UpmCache.GetInstalledPackageInfo(packageName);
            if (packageInfo != null)
            {
                try
                {
                    // Fix case 1237777, make files writable first
                    foreach (var file in m_IOProxy.DirectoryGetFiles(packageInfo.resolvedPath, "*", System.IO.SearchOption.AllDirectories))
                        m_IOProxy.MakeFileWritable(file, true);
                    m_IOProxy.DeleteDirectory(packageInfo.resolvedPath);
                    Resolve();
                }
                catch (System.IO.IOException e)
                {
                    Debug.Log($"[Package Manager] Cannot remove embedded package {packageName}: {e.Message}");
                }
            }
        }

        private void SetupRemoveOperation()
        {
            removeOperation.onProcessResult += OnProcessRemoveResult;
            removeOperation.onOperationError += (op, error) => Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Error removing package: {0}."), removeOperation.packageName));
            onRemoveOperation(removeOperation);
        }

        private void OnProcessRemoveResult(RemoveRequest request)
        {
            var installedPackage = m_UpmCache.GetInstalledPackageInfo(request.PackageIdOrName);
            if (installedPackage == null)
                return;
            m_UpmCache.RemoveInstalledPackageInfo(installedPackage.name);

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageRemoved(installedPackage);
            });

            // do a list offline to refresh all the dependencies
            List(true);
        }

        public virtual void SearchAll(bool offlineMode = false)
        {
            var operation = offlineMode ? searchOfflineOperation : searchOperation;
            if (operation.isInProgress)
                operation.Cancel();
            var errorMessage = offlineMode ? L10n.Tr("Error searching for packages offline.") : L10n.Tr("Error searching for packages.");
            if (offlineMode)
                operation.SearchAllOffline(searchOperation.lastSuccessTimestamp);
            else
                operation.SearchAll();
            operation.onProcessResult += request => OnProcessSearchAllResult(request, offlineMode);
            operation.onOperationError += (op, error) => Debug.LogError($"{L10n.Tr("[Package Manager Window]")} {errorMessage}");
            onSearchAllOperation(operation);
        }

        private void OnProcessSearchAllResult(SearchRequest request, bool offlineMode)
        {
            // skip operation when the result from the online operation is more up-to-date.
            if (offlineMode && searchOfflineOperation.timestamp < searchOperation.lastSuccessTimestamp)
                return;

            m_UpmCache.SetSearchPackageInfos(request.Result, searchOperation.lastSuccessTimestamp);
        }

        public virtual void ExtraFetch(string packageId)
        {
            ExtraFetchInternal(packageId);
        }

        private UpmSearchOperation ExtraFetchInternal(string packageIdOrName, string productId = null)
        {
            if (m_ExtraFetchOperations.ContainsKey(packageIdOrName))
                return null;
            var operation = new UpmSearchOperation();
            operation.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            operation.Search(packageIdOrName, productId);
            operation.onProcessResult += (requst) => OnProcessExtraFetchResult(requst, productId);
            operation.onOperationFinalized += (op) => m_ExtraFetchOperations.Remove(packageIdOrName);
            m_ExtraFetchOperations[packageIdOrName] = operation;

            if (!string.IsNullOrEmpty(productId))
            {
                operation.onOperationError += (op, error) => m_FetchStatusTracker.SetFetchError(productId, FetchType.ProductSearchInfo, error);
                m_FetchStatusTracker.SetFetchInProgress(productId, FetchType.ProductSearchInfo);
            }
            onExtraFetchOperation?.Invoke(operation);
            return operation;
        }

        private void OnProcessExtraFetchResult(SearchRequest request, string productId = null)
        {
            var packageInfo = request.Result.FirstOrDefault();

            if (!string.IsNullOrEmpty(productId))
            {
                // This is not really supposed to happen - this happening would mean there's an issue with data from the backend
                // Right now there isn't any recommended actions we can suggest the users to take, so we'll just add a message here
                // to expose it if it ever happens (rather than letting it pass silently)
                if (packageInfo?.assetStore?.productId != productId)
                {
                    var error = new UIError(UIErrorCode.AssetStorePackageError, L10n.Tr("Product Id mismatch between product details and package details."));
                    m_FetchStatusTracker.SetFetchError(productId, FetchType.ProductSearchInfo, error);
                    return;
                }
                m_UpmCache.SetProductSearchPackageInfo(productId, packageInfo);
                m_FetchStatusTracker.SetFetchSuccess(productId, FetchType.ProductSearchInfo);
            }
            else
                m_UpmCache.AddExtraPackageInfo(packageInfo);
        }

        public virtual void SearchPackageInfoForProduct(string productId, string packageName)
        {
            ExtraFetchInternal(packageName, productId);
        }

        // Restore operations that's interrupted by domain reloads
        private void RestoreInProgressOperations()
        {
            if (addOperation.isInProgress)
                SetupAddOperation();

            if (removeOperation.isInProgress)
                SetupRemoveOperation();

            if (addAndRemoveOperation.isInProgress)
                SetupAddAndRemoveOperation();

            if (listOfflineOperation.isInProgress)
                List(true);

            if (searchOperation.isInProgress)
                SearchAll();
        }

        public void OnEnable()
        {
            m_PackageFactory.OnEnable();
            RestoreInProgressOperations();
        }

        public void OnDisable()
        {
            m_PackageFactory.OnDisable();
        }

        public virtual void ClearCache()
        {
            m_ExtraFetchOperations.Clear();

            m_UpmCache.ClearCache();
        }

        public virtual void Resolve()
        {
            m_ClientProxy.Resolve();
        }

        public virtual RegistryType GetAvailableRegistryType(PackageInfo packageInfo)
        {
            // Special handling for packages that's built in/bundled with unity, we always consider them from the Unity registry
            if (packageInfo?.source == PackageSource.BuiltIn)
                return RegistryType.UnityRegistry;

            if (string.IsNullOrEmpty(packageInfo?.registry?.url))
                return RegistryType.None;

            if (packageInfo?.entitlements?.licenseType == EntitlementLicenseType.AssetStore)
                return RegistryType.AssetStore;

            if (m_RegistryUrls.TryGetValue(packageInfo.registry.url, out var result))
                return result;

            result = packageInfo.registry.isDefault && IsUnityUrl(packageInfo.registry.url) ? RegistryType.UnityRegistry : RegistryType.MyRegistries;
            m_RegistryUrls[packageInfo.registry.url] = result;
            return result;
        }

        public virtual bool IsUnityPackage(PackageInfo packageInfo)
        {
            return packageInfo != null && (packageInfo.source == PackageSource.BuiltIn || packageInfo.source == PackageSource.Registry) &&
                   GetAvailableRegistryType(packageInfo) == RegistryType.UnityRegistry;
        }

        public static bool IsUnityUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
                return !uri.IsLoopback && k_UnityRegistryUrlsHosts.Any(unityHost => uri.Host.EndsWith(unityHost, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        private T CreateOperation<T>(ref T operation) where T : UpmBaseOperation, new()
        {
            if (operation != null)
            {
                operation.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
                return operation;
            }

            operation = new T();
            operation.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            return operation;
        }

        internal class UpmPackageFactory
        {
            [NonSerialized]
            private UpmCache m_UpmCache;
            [NonSerialized]
            private UpmClient m_UpmClient;
            [NonSerialized]
            private PackageManagerProjectSettingsProxy m_SettingsProxy;
            public void ResolveDependencies(UpmCache upmCache, UpmClient upmClient, PackageManagerProjectSettingsProxy settingsProxy)
            {
                m_UpmCache = upmCache;
                m_UpmClient = upmClient;
                m_SettingsProxy = settingsProxy;
            }

            public void OnEnable()
            {
                m_SettingsProxy.onEnablePreReleasePackagesChanged += OnShowPreReleasePackagesOrSeeAllVersionsChanged;
                m_SettingsProxy.onSeeAllVersionsChanged += OnShowPreReleasePackagesOrSeeAllVersionsChanged;
                m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;
                m_UpmCache.onExtraPackageInfoFetched += OnExtraPackageInfoFetched;
                m_UpmCache.onLoadAllVersionsChanged += OnLoadAllVersionsChanged;
            }

            public void OnDisable()
            {
                m_SettingsProxy.onEnablePreReleasePackagesChanged -= OnShowPreReleasePackagesOrSeeAllVersionsChanged;
                m_SettingsProxy.onSeeAllVersionsChanged -= OnShowPreReleasePackagesOrSeeAllVersionsChanged;
                m_UpmCache.onPackageInfosUpdated -= OnPackageInfosUpdated;
                m_UpmCache.onExtraPackageInfoFetched -= OnExtraPackageInfoFetched;
                m_UpmCache.onLoadAllVersionsChanged -= OnLoadAllVersionsChanged;
            }

            private void OnExtraPackageInfoFetched(PackageInfo packageInfo)
            {
                // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
                var productId = packageInfo.assetStore?.productId;
                if (string.IsNullOrEmpty(productId) && m_UpmCache.GetInstalledPackageInfo(packageInfo.name)?.packageId != packageInfo.packageId)
                    GeneratePackagesAndTriggerChangeEvent(new[] { packageInfo.name });
            }

            public void GeneratePackagesAndTriggerChangeEvent(IEnumerable<string> packageNames)
            {
                if (packageNames?.Any() != true)
                    return;

                var updatedPackages = new List<UpmPackage>();
                var showPreRelease = m_SettingsProxy.enablePreReleasePackages;
                var seeAllVersions = m_SettingsProxy.seeAllPackageVersions;
                foreach (var packageName in packageNames)
                {
                    // Upm packages with product ids are handled in UpmOnAssetStorePackageFactory, we don't want to worry about it here.
                    if (!string.IsNullOrEmpty(m_UpmCache.GetProductIdByName(packageName)))
                        continue;
                    var installedInfo = m_UpmCache.GetInstalledPackageInfo(packageName);
                    var searchInfo = m_UpmCache.GetSearchPackageInfo(packageName);
                    if (installedInfo == null && searchInfo == null)
                        updatedPackages.Add(new UpmPackage(packageName, false, new UpmVersionList()));
                    else
                    {
                        var availableRegistry = m_UpmClient.GetAvailableRegistryType(searchInfo ?? installedInfo);
                        var extraVersions = m_UpmCache.GetExtraPackageInfos(packageName);
                        var versionList = new UpmVersionList(searchInfo, installedInfo, availableRegistry, extraVersions);
                        versionList = VersionsFilter.GetFilteredVersionList(versionList, seeAllVersions, showPreRelease);
                        versionList = VersionsFilter.UnloadVersionsIfNeeded(versionList, m_UpmCache.IsLoadAllVersions(packageName));
                        updatedPackages.Add(new UpmPackage(packageName, searchInfo != null, versionList));
                    }
                }

                if (updatedPackages.Any())
                    m_UpmClient.onPackagesChanged?.Invoke(updatedPackages.Cast<IPackage>());
            }

            private void OnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
            {
                GeneratePackagesAndTriggerChangeEvent(packageInfos.Select(p => p.name));
            }

            private void OnLoadAllVersionsChanged(string packageUniqueId, bool _)
            {
                if (!long.TryParse(packageUniqueId, out var _))
                    GeneratePackagesAndTriggerChangeEvent(new[] { packageUniqueId });
            }

            private void OnShowPreReleasePackagesOrSeeAllVersionsChanged(bool _)
            {
                var allPackageNames = new HashSet<string>(m_UpmCache.installedPackageInfos.Concat(m_UpmCache.searchPackageInfos).Select(p => p.name));
                GeneratePackagesAndTriggerChangeEvent(allPackageNames);
            }
        }
    }
}
