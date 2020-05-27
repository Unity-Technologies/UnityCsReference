// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmClient
    {
        public virtual event Action<IOperation> onListOperation = delegate {};
        public virtual event Action<IOperation> onSearchAllOperation = delegate {};
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

        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private IOProxy m_IOProxy;
        [NonSerialized]
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        public void ResolveDependencies(PackageManagerPrefs packageManagerPrefs,
            UpmCache upmCache,
            IOProxy IOProxy,
            PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_PackageManagerPrefs = packageManagerPrefs;
            m_UpmCache = upmCache;
            m_IOProxy = IOProxy;
            m_SettingsProxy = settingsProxy;
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
            addOperation.onProcessResult += OnProcessAddResult;
            addOperation.onOperationError += (op, error) =>
            {
                var packageId = string.IsNullOrEmpty(addOperation.packageId) ? addOperation.specialUniqueId : addOperation.packageId;
                Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Error adding package: {0}."), packageId));
            };
            onAddOperation(addOperation);
        }

        private void OnProcessAddResult(Request<PackageInfo> request)
        {
            var packageInfo = request.Result;
            m_UpmCache.SetInstalledPackageInfo(packageInfo);

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

            path = path.Replace('\\', '/');
            var projectPath = Path.GetDirectoryName(Application.dataPath).Replace('\\', '/') + '/';
            if (path.StartsWith(projectPath))
            {
                var packageFolderPrefix = "Packages/";
                var relativePathToProjectRoot = path.Substring(projectPath.Length);
                if (relativePathToProjectRoot.StartsWith(packageFolderPrefix, StringComparison.InvariantCultureIgnoreCase))
                    path = relativePathToProjectRoot.Substring(packageFolderPrefix.Length);
                else
                    path = $"../{relativePathToProjectRoot}";
            }

            addOperation.AddByUrlOrPath($"file:{path}");
            SetupAddOperation();
        }

        public virtual void AddByUrl(string url)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;

            // convert SCP-like syntax to SSH URL as currently UPM doesn't support it
            if (url.ToLower().StartsWith("git@"))
                url = "ssh://" + url.Replace(':', '/');

            addOperation.AddByUrlOrPath(url);
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

            m_UpmCache.SetInstalledPackageInfos(request.Result);
        }

        public virtual void EmbedByName(string packageName)
        {
            if (isAddRemoveOrEmbedInProgress)
                return;
            embedOperation.Embed(packageName, m_UpmCache.GetProductId(packageName));
            embedOperation.onProcessResult += OnProcessAddResult;
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
                m_IOProxy.DirectoryDelete(packageInfo.resolvedPath, true);
                Resolve();
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

            m_UpmCache.SetSearchPackageInfos(request.Result);
        }

        public virtual void ExtraFetch(string packageId)
        {
            ExtraFetchInternal(packageId);
        }

        private void ExtraFetchInternal(string packageIdOrName, string productId = null)
        {
            if (m_ExtraFetchOperations.ContainsKey(packageIdOrName))
                return;
            var operation = new UpmSearchOperation();
            operation.Search(packageIdOrName, productId);
            operation.onProcessResult += (requst) => OnProcessExtraFetchResult(requst, productId);
            operation.onOperationError += (op, error) => OnProcessExtraFetchError(error, productId);
            operation.onOperationFinalized += (op) => OnExtraFetchFinalized(packageIdOrName);
            m_ExtraFetchOperations[packageIdOrName] = operation;
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
                        onPackageVersionUpdated?.Invoke(packageInfo.name, new UpmPackageVersion(packageInfo, false));
                    else
                        onProductPackageVersionUpdated?.Invoke(productId, new UpmPackageVersion(packageInfo, false));
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
            var showPreview = m_SettingsProxy.enablePreviewPackages;
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
                if (!showPreview && HasHidablePreviewVersions(package))
                    RemovePreviewVersions(package);
                UpdateExtraPackageInfos(package.name, package.versions);
            }

            if (upmPackages.Any())
                onPackagesChanged(upmPackages.Cast<IPackage>());

            foreach (var package in productPackages)
                onProductPackageChanged?.Invoke(m_UpmCache.GetProductId(package.name), package);
        }

        private void OnShowPreviewPackagesesChanged(bool showPreview)
        {
            var updatedUpmPackages = new List<UpmPackage>();
            var updatedProductPackages = new List<UpmPackage>();
            foreach (var installedInfo in m_UpmCache.installedPackageInfos)
            {
                var productId = m_UpmCache.GetProductId(installedInfo.name);
                if (string.IsNullOrEmpty(productId))
                {
                    var package = CreateUpmPackage(m_UpmCache.GetSearchPackageInfo(installedInfo.name), installedInfo);
                    if (HasHidablePreviewVersions(package))
                        updatedUpmPackages.Add(package);
                }
                else
                {
                    var package = CreateUpmPackage(m_UpmCache.GetProductPackageInfo(installedInfo.name), installedInfo);
                    if (HasHidablePreviewVersions(package))
                        updatedProductPackages.Add(package);
                }
            }

            foreach (var searchInfo in m_UpmCache.searchPackageInfos.Where(p => !m_UpmCache.IsPackageInstalled(p.name)))
            {
                var package = CreateUpmPackage(searchInfo, null);
                if (HasHidablePreviewVersions(package))
                    updatedUpmPackages.Add(package);
            }

            foreach (var productPackageInfo in m_UpmCache.productPackageInfos.Where(p => !m_UpmCache.IsPackageInstalled(p.name)))
            {
                var package = CreateUpmPackage(productPackageInfo, null);
                if (HasHidablePreviewVersions(package))
                    updatedProductPackages.Add(package);
            }

            foreach (var package in updatedUpmPackages.Concat(updatedProductPackages))
            {
                if (!showPreview)
                    RemovePreviewVersions(package);
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
                result = new UpmPackage(installedInfo, true, false);
            else
            {
                result = new UpmPackage(searchInfo, false, true);
                if (installedInfo != null)
                    result.AddInstalledVersion(new UpmPackageVersion(installedInfo, true));
            }
            return result;
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
                        version.UpdatePackageInfo(info);
                }
            }

            // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
            // since the primary version's display name is used in the package list
            var primaryVersion = versions.primary;
            if (!primaryVersion.isFullyFetched)
                ExtraFetch(primaryVersion.uniqueId);
        }

        // check if this package have preview packages that's `hidable` (will be filtered out if `show preview` is not selected).
        // if the installed version is preview, then we we always show other preview versions
        // if no installed version or installed version is not preview, we hide the preview versions according to the `show previews` toggle
        private static bool HasHidablePreviewVersions(IPackage package)
        {
            var previewInstalled = (!package.versions.installed?.HasTag(PackageTag.Release)) ?? false;
            if (previewInstalled)
                return false;
            return package.versions.Any(v => !v.HasTag(PackageTag.Release));
        }

        private static void RemovePreviewVersions(UpmPackage package)
        {
            package.UpdateVersions(package.versions.Where(v => v.HasTag(PackageTag.Release)).Cast<UpmPackageVersion>());
        }

        // Restore operations that's interrupted by domain reloads
        private void RestoreInProgressOperations()
        {
            if (addOperation.isInProgress)
                SetupAddOperation();

            if (removeOperation.isInProgress)
                SetupRemoveOperation();
        }

        public void OnEnable()
        {
            m_SettingsProxy.onEnablePreviewPackagesChanged += OnShowPreviewPackagesesChanged;
            m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;

            RestoreInProgressOperations();
        }

        public void OnDisable()
        {
            m_SettingsProxy.onEnablePreviewPackagesChanged -= OnShowPreviewPackagesesChanged;
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
    }
}
