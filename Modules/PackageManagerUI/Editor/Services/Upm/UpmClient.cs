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
        private static readonly string[] k_UnityRegistriesUrlHosts = { ".unity.com", ".unity3d.com" };

        public virtual event Action<IOperation> onListOperation = delegate {};
        public virtual event Action<IOperation> onSearchAllOperation = delegate {};
        public virtual event Action<IOperation> onExtraFetchOperation = delegate {};
        public virtual event Action<IOperation> onRemoveOperation = delegate {};
        public virtual event Action<IOperation> onAddOperation = delegate {};
        public virtual event Action<IOperation> onEmbedOperation = delegate {};

        public virtual event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
        public virtual event Action<string, IPackage> onProductPackageChanged = delegate {};

        public virtual event Action<string, IPackageVersion> onPackageVersionUpdated = delegate {};
        public virtual event Action<string, IPackageVersion> onProductPackageVersionUpdated = delegate {};
        public virtual event Action<string, UIError> onProductPackageFetchError = delegate {};

        [SerializeField]
        private UpmSearchOperation m_SearchOperation;
        private UpmSearchOperation searchOperation => m_SearchOperation ?? (m_SearchOperation = new UpmSearchOperation());
        [SerializeField]
        private UpmSearchOperation m_SearchOfflineOperation;
        private UpmSearchOperation searchOfflineOperation => m_SearchOfflineOperation ?? (m_SearchOfflineOperation = new UpmSearchOperation());
        [SerializeField]
        private UpmListOperation m_ListOperation;
        private UpmListOperation listOperation => m_ListOperation ?? (m_ListOperation = new UpmListOperation());
        [SerializeField]
        private UpmListOperation m_ListOfflineOperation;
        private UpmListOperation listOfflineOperation => m_ListOfflineOperation ?? (m_ListOfflineOperation = new UpmListOperation());

        [SerializeField]
        private UpmAddOperation m_AddOperation;
        private UpmAddOperation addOperation => m_AddOperation ?? (m_AddOperation = new UpmAddOperation());
        [SerializeField]
        private UpmRemoveOperation m_RemoveOperation;
        private UpmRemoveOperation removeOperation => m_RemoveOperation ?? (m_RemoveOperation = new UpmRemoveOperation());
        [SerializeField]
        private UpmEmbedOperation m_EmbedOperation;
        private UpmEmbedOperation embedOperation => m_EmbedOperation ?? (m_EmbedOperation = new UpmEmbedOperation());

        private readonly Dictionary<string, UpmBaseOperation> m_ExtraFetchOperations = new Dictionary<string, UpmBaseOperation>();

        private HashSet<string> m_PackagesToExtraFetchForRegistryVersions = new HashSet<string>();
        [SerializeField]
        private string[] m_SerializedPackagesToExtraFetchForRegistryVersions;

        [SerializeField]
        private string[] m_SerializedPRegistriesUrlKeys;

        [SerializeField]
        private bool[] m_SerializedRegistriesUrlValues;

        internal Dictionary<string, bool> m_RegistriesUrl = new Dictionary<string, bool>();

        // a list of unique ids (could be specialUniqueId or packageId)
        [SerializeField]
        private List<string> m_SpecialInstallations = new List<string>();
        public List<string> specialInstallations => m_SpecialInstallations;

        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private IOProxy m_IOProxy;
        [NonSerialized]
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        public void ResolveDependencies(UpmCache upmCache,
            IOProxy IOProxy,
            PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_UpmCache = upmCache;
            m_IOProxy = IOProxy;
            m_SettingsProxy = settingsProxy;
        }

        public virtual bool IsAnyExperimentalPackagesInUse()
        {
            return PackageInfo.GetAll().Any(info => (info.version.Contains("-preview") || info.version.Contains("-exp.") || info.version.StartsWith("0.")) && IsUnityPackage(info));
        }

        public void OnBeforeSerialize()
        {
            m_SerializedPRegistriesUrlKeys = m_RegistriesUrl?.Keys.ToArray() ?? new string[0];
            m_SerializedRegistriesUrlValues = m_RegistriesUrl?.Values.ToArray() ?? new bool[0];
            m_SerializedPackagesToExtraFetchForRegistryVersions = m_PackagesToExtraFetchForRegistryVersions.ToArray() ?? new string[0];
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedPRegistriesUrlKeys.Length; i++)
                m_RegistriesUrl[m_SerializedPRegistriesUrlKeys[i]] = m_SerializedRegistriesUrlValues[i];
            foreach (var packageName in m_SerializedPackagesToExtraFetchForRegistryVersions)
                m_PackagesToExtraFetchForRegistryVersions.Add(packageName);
        }

        public virtual bool isAddRemoveOrEmbedInProgress
        {
            get { return addOperation.isInProgress || removeOperation.isInProgress || embedOperation.isInProgress; }
        }

        public virtual bool IsEmbedInProgress(string packageName)
        {
            return embedOperation.isInProgress && embedOperation.packageName == packageName;
        }

        public virtual bool IsRemoveInProgress(string packageName)
        {
            return removeOperation.isInProgress && removeOperation.packageName == packageName;
        }

        public virtual bool IsAddInProgress(string packageId)
        {
            return addOperation.isInProgress && addOperation.packageId == packageId;
        }

        public virtual void AddById(string packageId)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;
            var packageName = packageId.Split(new[] { '@' }, 2)[0];
            addOperation.Add(packageId, m_UpmCache.GetProductId(packageName));
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
                path = path.Replace('\\', '/');
                var projectPath = m_IOProxy.GetProjectDirectory().Replace('\\', '/') + '/';
                if (path.StartsWith(projectPath))
                {
                    var packageFolderPrefix = "Packages/";
                    var relativePathToProjectRoot = path.Substring(projectPath.Length);
                    if (relativePathToProjectRoot.StartsWith(packageFolderPrefix, StringComparison.InvariantCultureIgnoreCase))
                        path = relativePathToProjectRoot.Substring(packageFolderPrefix.Length);
                    else
                        path = $"../{relativePathToProjectRoot}";
                }

                addOperation.AddByUrlOrPath($"file:{path}", PackageTag.Local);
                SetupAddOperation();
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager] Cannot add package {path}: {e.Message}");
            }
        }

        public virtual void AddByUrl(string url)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;

            addOperation.AddByUrlOrPath(url, PackageTag.Git);
            SetupAddOperation();
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
            embedOperation.Embed(packageName, m_UpmCache.GetProductId(packageName));
            embedOperation.onProcessResult += (request) => OnProcessAddResult(embedOperation, request);
            embedOperation.onOperationError += (op, error) => Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Error embedding package: {0}."), embedOperation.packageName));
            onEmbedOperation(embedOperation);
        }

        public virtual void RemoveByName(string packageName)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;
            removeOperation.Remove(packageName, m_UpmCache.GetProductId(packageName));
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
            operation.Search(packageIdOrName, productId);
            operation.onProcessResult += (requst) => OnProcessExtraFetchResult(requst, productId);
            operation.onOperationError += (op, error) => OnProcessExtraFetchError(error, productId);
            operation.onOperationFinalized += (op) => OnExtraFetchFinalized(packageIdOrName);
            m_ExtraFetchOperations[packageIdOrName] = operation;
            onExtraFetchOperation?.Invoke(operation);

            return operation;
        }

        private void OnProcessExtraFetchResult(SearchRequest request, string productId = null)
        {
            var packageInfo = request.Result.FirstOrDefault();

            if (!string.IsNullOrEmpty(productId))
            {
                var oldInfo = m_UpmCache.GetProductPackageInfo(packageInfo.name);
                // remove the created package that's created before asset store info was fetched
                // such that there won't be two entries of the same package
                if (oldInfo == null && m_UpmCache.IsPackageInstalled(packageInfo.name))
                    onPackagesChanged(new[] { CreateUpmPackage(null, null, packageInfo.name) });

                m_UpmCache.SetProductPackageInfo(productId, packageInfo);
            }
            else
            {
                m_UpmCache.AddExtraPackageInfo(packageInfo);

                // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
                var installedPackageInfo = m_UpmCache.GetInstalledPackageInfo(packageInfo.name);
                if (installedPackageInfo?.packageId != packageInfo.packageId)
                {
                    productId = m_UpmCache.GetProductId(packageInfo.name);
                    if (string.IsNullOrEmpty(productId))
                    {
                        onPackageVersionUpdated?.Invoke(packageInfo.name, new UpmPackageVersion(packageInfo, false, IsUnityPackage(packageInfo)));
                    }
                    else
                        onProductPackageVersionUpdated?.Invoke(productId, new UpmPackageVersion(packageInfo, false, false));
                }
            }
        }

        private void OnProcessExtraFetchError(UIError error, string productId = null)
        {
            if (!string.IsNullOrEmpty(productId))
                onProductPackageFetchError?.Invoke(productId, error);
        }

        private void OnExtraFetchFinalized(string packageIdOrName)
        {
            m_ExtraFetchOperations.Remove(packageIdOrName);
        }

        public virtual void FetchForProduct(string productId, string packageName)
        {
            ExtraFetchInternal(packageName, productId);
        }

        private void OnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
        {
            if (packageInfos?.Any() != true)
                return;

            var upmPackages = new List<UpmPackage>();
            var productPackages = new List<UpmPackage>();
            var showPreRelease = m_SettingsProxy.enablePreReleasePackages;
            var seeAllVersions = m_SettingsProxy.seeAllPackageVersions;
            foreach (var p in packageInfos)
            {
                var productId = m_UpmCache.GetProductId(p.name);
                var installedInfo = m_UpmCache.GetInstalledPackageInfo(p.name);
                if (string.IsNullOrEmpty(productId))
                    upmPackages.Add(CreateUpmPackage(m_UpmCache.GetSearchPackageInfo(p.name), installedInfo, p.name));
                else
                    productPackages.Add(CreateUpmPackage(m_UpmCache.GetProductPackageInfo(p.name), installedInfo, p.name));
            }

            foreach (var package in upmPackages.Concat(productPackages))
            {
                // only filter on Lifecycle tags if is a Unity package
                if (!seeAllVersions &&
                    HasHidableVersions(package) &&
                    (package.versions.primary as UpmPackageVersion)?.isUnityPackage == true)
                {
                    FilterVersions(package, showPreRelease);
                }
                UpdateExtraPackageInfos(package.name, package.versions);
            }

            if (upmPackages.Any())
                onPackagesChanged(upmPackages.Cast<IPackage>());

            foreach (var package in productPackages)
                onProductPackageChanged?.Invoke(m_UpmCache.GetProductId(package.name), package);
        }

        private void OnShowPreReleasePackagesesOrSeeAllVersionsChanged(bool showPreReleaseOrSeeAllVersions)
        {
            var updatedUpmPackages = new List<UpmPackage>();
            var updatedProductPackages = new List<UpmPackage>();
            var showPreRelease = m_SettingsProxy.enablePreReleasePackages;
            var seeAllVersions = m_SettingsProxy.seeAllPackageVersions;
            foreach (var installedInfo in m_UpmCache.installedPackageInfos)
            {
                var productId = m_UpmCache.GetProductId(installedInfo.name);
                if (string.IsNullOrEmpty(productId))
                {
                    var package = CreateUpmPackage(m_UpmCache.GetSearchPackageInfo(installedInfo.name), installedInfo);
                    if (HasHidableVersions(package))
                        updatedUpmPackages.Add(package);
                }
                else
                {
                    var package = CreateUpmPackage(m_UpmCache.GetProductPackageInfo(installedInfo.name), installedInfo);
                    if (HasHidableVersions(package))
                        updatedProductPackages.Add(package);
                }
            }

            foreach (var searchInfo in m_UpmCache.searchPackageInfos.Where(p => !m_UpmCache.IsPackageInstalled(p.name)))
            {
                var package = CreateUpmPackage(searchInfo, null);
                if (HasHidableVersions(package))
                    updatedUpmPackages.Add(package);
            }

            foreach (var productPackageInfo in m_UpmCache.productPackageInfos.Where(p => !m_UpmCache.IsPackageInstalled(p.name)))
            {
                var package = CreateUpmPackage(productPackageInfo, null);
                if (HasHidableVersions(package))
                    updatedProductPackages.Add(package);
            }

            foreach (var package in updatedUpmPackages.Concat(updatedProductPackages))
            {
                // only filter on Lifecycle tags if is a Unity package
                if (!seeAllVersions && (package.versions.primary as UpmPackageVersion)?.isUnityPackage == true)
                {
                    FilterVersions(package, showPreRelease);
                }
                UpdateExtraPackageInfos(package.name, package.versions);
            }

            if (updatedUpmPackages.Any())
                onPackagesChanged?.Invoke(updatedUpmPackages.Cast<IPackage>());

            foreach (var package in updatedProductPackages)
                onProductPackageChanged?.Invoke(m_UpmCache.GetProductId(package.name), package);
        }

        private UpmPackage CreateUpmPackage(PackageInfo searchInfo, PackageInfo installedInfo, string packageName = null)
        {
            if (searchInfo == null && installedInfo == null)
                return new UpmPackage(packageName, false, PackageType.Installable);

            UpmPackage result;
            if (searchInfo == null)
            {
                result = new UpmPackage(installedInfo, true, false, IsUnityPackage(installedInfo));

                var registryInfo = installedInfo.registry;
                var compatibleVersions = installedInfo.versions?.compatible;

                // if main version was installed from outside a registry but has other registry versions,
                //  need to fetch extra info for the other registry versions so they can be tagged properly
                if (result.versions?.installed?.HasTag(PackageTag.Bundled) == false
                    && registryInfo == null && compatibleVersions?.Count() > 0)
                {
                    ExtraFetchForRegistryVersions(installedInfo.name);
                }
            }
            else
            {
                var isUnityPackage = IsUnityPackage(searchInfo);
                result = new UpmPackage(searchInfo, false, true, isUnityPackage);
                if (installedInfo != null)
                    result.AddInstalledVersion(new UpmPackageVersion(installedInfo, true, isUnityPackage));
            }

            return result;
        }

        public void ExtraFetchForRegistryVersions(string packageName)
        {
            m_PackagesToExtraFetchForRegistryVersions.Add(packageName);

            var extraFetchOperation = ExtraFetchInternal(packageName);

            if (extraFetchOperation != null)
            {
                extraFetchOperation.onProcessResult += (request) => OnProcessExtraFetchRegistryVersionsResult(request);
                extraFetchOperation.onOperationFinalized += (op) => OnExtraFetchRegistryVersionsFinalized(op.packageUniqueId);
            }
        }

        private void OnExtraFetchRegistryVersionsFinalized(string packageName)
        {
            m_PackagesToExtraFetchForRegistryVersions.Remove(packageName);
        }

        private void OnProcessExtraFetchRegistryVersionsResult(SearchRequest request)
        {
            var packageInfo = request.Result.FirstOrDefault();
            var isUnityPackage = IsUnityPackage(packageInfo);

            var existingExtraPackageInfos = m_UpmCache.GetExtraPackageInfos(packageInfo.name);
            foreach (var registryVersion in packageInfo.versions.compatible)
            {
                if (!existingExtraPackageInfos.ContainsKey(registryVersion))
                    onPackageVersionUpdated?.Invoke(packageInfo.name, new UpmPackageVersion(packageInfo, false, Scripting.ScriptCompilation.SemVersionParser.Parse(registryVersion), packageInfo.displayName, isUnityPackage));
            }
        }

        private void UpdateExtraPackageInfos(string packageName, IVersionList versions)
        {
            if (!versions.Any())
                return;

            var extraVersions = m_UpmCache.GetExtraPackageInfos(packageName);
            if (extraVersions?.Any() ?? false)
            {
                foreach (var version in versions.Cast<UpmPackageVersion>())
                {
                    if (version.isFullyFetched)
                        continue;
                    PackageInfo info;
                    if (extraVersions.TryGetValue(version.version.ToString(), out info))
                        version.UpdatePackageInfo(info, IsUnityPackage(info));
                }
            }

            // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
            // since the primary version's display name is used in the package list
            var primaryVersion = versions.primary;
            if (!primaryVersion.isFullyFetched)
                ExtraFetch(primaryVersion.uniqueId);
        }

        // check if this package has any non-release versions in the list; if so, it will need to be
        //  filtered
        private static bool HasHidableVersions(IPackage package)
        {
            return package.versions.Any(v => !v.HasTag(PackageTag.Release | PackageTag.ReleaseCandidate));
        }

        private static void FilterVersions(UpmPackage package, bool showPreRelease)
        {
            var versions = (UpmVersionList)package.versions;
            var packageTagsToKeep = PackageTag.Release | PackageTag.ReleaseCandidate;

            if (showPreRelease || package.versions.installed?.HasTag(PackageTag.PreRelease | PackageTag.Experimental) == true)
                packageTagsToKeep |= PackageTag.PreRelease;

            // should see updates to the installed experimental packages, if they exist
            if (package.versions.installed?.HasTag(PackageTag.Experimental) == true)
                packageTagsToKeep |= PackageTag.Experimental;

            var filteredVersions = versions.Where(v => v.isInstalled || v.HasTag(packageTagsToKeep)).ToList();

            package.UpdateVersions(filteredVersions.Cast<UpmPackageVersion>());
        }

        // Restore operations that's interrupted by domain reloads
        private void RestoreInProgressOperations()
        {
            if (addOperation.isInProgress)
                SetupAddOperation();

            if (removeOperation.isInProgress)
                SetupRemoveOperation();

            if (searchOperation.isInProgress)
                SearchAll();

            foreach (var packageName in m_PackagesToExtraFetchForRegistryVersions)
            {
                ExtraFetchForRegistryVersions(packageName);
            }
        }

        public void OnEnable()
        {
            m_SettingsProxy.onEnablePreReleasePackagesChanged += OnShowPreReleasePackagesesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged += OnShowPreReleasePackagesesOrSeeAllVersionsChanged;
            m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;

            RestoreInProgressOperations();
        }

        public void OnDisable()
        {
            m_SettingsProxy.onEnablePreReleasePackagesChanged -= OnShowPreReleasePackagesesOrSeeAllVersionsChanged;
            m_SettingsProxy.onSeeAllVersionsChanged -= OnShowPreReleasePackagesesOrSeeAllVersionsChanged;
            m_UpmCache.onPackageInfosUpdated -= OnPackageInfosUpdated;
        }

        public virtual void ClearCache()
        {
            m_ExtraFetchOperations.Clear();

            m_UpmCache.ClearCache();
        }

        public virtual void ClearProductCache()
        {
            m_UpmCache.ClearProductCache();
        }

        public void Resolve()
        {
            Client.Resolve();
        }

        public virtual bool IsUnityPackage(PackageInfo packageInfo)
        {
            if (!(packageInfo?.registry?.isDefault ?? false) || string.IsNullOrEmpty(packageInfo.registry?.url))
                return false;

            if (m_RegistriesUrl.TryGetValue(packageInfo.registry.url, out var isUnityRegistry))
                return isUnityRegistry;

            isUnityRegistry = IsUnityUrl(packageInfo.registry.url);
            m_RegistriesUrl[packageInfo.registry.url] = isUnityRegistry;
            return isUnityRegistry;
        }

        public virtual bool IsUnityUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
                return !uri.IsLoopback && k_UnityRegistriesUrlHosts.Any(unityHost => uri.Host.EndsWith(unityHost, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
    }
}
