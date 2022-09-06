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

        public virtual event Action<IEnumerable<(string packageIdOrName, PackageProgress progress)>> onPackagesProgressChange = delegate { };
        public virtual event Action<string, UIError> onPackageOperationError = delegate { };

        public virtual event Action<IOperation> onListOperation = delegate {};
        public virtual event Action<IOperation> onSearchAllOperation = delegate {};
        public virtual event Action<IOperation> onExtraFetchOperation = delegate {};
        public virtual event Action<IOperation> onAddOperation = delegate {};

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

        private readonly Dictionary<string, UpmSearchOperation> m_ExtraFetchOperations = new Dictionary<string, UpmSearchOperation>();

        [SerializeField]
        private string[] m_SerializedRegistryUrlsKeys;

        [SerializeField]
        private bool[] m_SerializedRegistryUrlsValues;

        internal Dictionary<string, bool> m_RegistryUrls = new Dictionary<string, bool>();

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
            m_AddAndRemoveOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
        }

        public virtual bool IsAnyExperimentalPackagesInUse()
        {
            return PackageInfo.GetAllRegisteredPackages().Any(info => (info.version.Contains("-preview") || info.version.Contains("-exp.") || info.version.StartsWith("0.")) && IsUnityPackage(info));
        }

        public void OnBeforeSerialize()
        {
            m_SerializedRegistryUrlsKeys = m_RegistryUrls?.Keys.ToArray() ?? new string[0];
            m_SerializedRegistryUrlsValues = m_RegistryUrls?.Values.ToArray() ?? new bool[0];
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedRegistryUrlsKeys.Length; i++)
                m_RegistryUrls[m_SerializedRegistryUrlsKeys[i]] = m_SerializedRegistryUrlsValues[i];
        }

        public virtual bool isAddOrRemoveInProgress => m_AddOperation?.isInProgress == true ||
            m_RemoveOperation?.isInProgress == true || m_AddAndRemoveOperation?.isInProgress == true;

        public virtual IEnumerable<string> packageIdsOrNamesInstalling
        {
            get
            {
                if (m_AddOperation?.isInProgress == true)
                    yield return m_AddOperation.packageIdOrName;
                if (m_AddAndRemoveOperation?.isInProgress == true)
                    foreach (var id in m_AddAndRemoveOperation.packageIdsToAdd)
                        yield return id;
            }
        }

        public virtual bool IsRemoveInProgress(string packageName)
        {
            return (m_RemoveOperation?.isInProgress == true && m_RemoveOperation.packageName == packageName) ||
                (m_AddAndRemoveOperation?.isInProgress == true && m_AddAndRemoveOperation.packagesNamesToRemove.Contains(packageName));
        }

        public virtual bool IsAddInProgress(string packageId)
        {
            return (m_AddOperation?.isInProgress == true && m_AddOperation.packageIdOrName == packageId) ||
                (m_AddAndRemoveOperation?.isInProgress == true && m_AddAndRemoveOperation.packageIdsToAdd.Contains(packageId));
        }

        public virtual void AddById(string packageId)
        {
            if (isAddOrRemoveInProgress)
                return;
            addOperation.Add(packageId);
            SetupAddOperation();
        }

        private void SetupAddOperation()
        {
            onPackagesProgressChange?.Invoke(new[] { (addOperation.packageName, PackageProgress.Installing) });

            addOperation.onProcessResult += OnProcessAddResult;
            addOperation.onOperationError += (_, error) => onPackageOperationError?.Invoke(addOperation.packageName, error);
            addOperation.onOperationFinalized += (_) =>
                onPackagesProgressChange?.Invoke(new[] { (addOperation.packageName, PackageProgress.None) });

            addOperation.logErrorInConsole = true;
            onAddOperation?.Invoke(addOperation);
        }

        private void OnProcessAddResult(Request<PackageInfo> request)
        {
            var packageInfo = request.Result;
            var installedInfoUpdated = m_UpmCache.SetInstalledPackageInfo(packageInfo, addOperation.packageName);
            if (!installedInfoUpdated && packageInfo.source == PackageSource.Git)
            {
                Debug.Log(string.Format(L10n.Tr("{0} is already up-to-date."), packageInfo.displayName));
                return;
            }

            PackageManagerExtensions.ExtensionCallback(() =>
            {
                foreach (var extension in PackageManagerExtensions.Extensions)
                    extension.OnPackageAddedOrUpdated(packageInfo);
            });

            // do a list offline to refresh all the dependencies
            List(true);
        }

        public virtual bool AddByPath(string path, out string tempPackageId)
        {
            tempPackageId = string.Empty;
            if (isAddOrRemoveInProgress)
                return false;

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

                tempPackageId = $"file:{path}";
                addOperation.AddByUrlOrPath(tempPackageId, PackageTag.Local);
                SetupAddOperation();
                return true;
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot add package {path}: {e.Message}");
                return false;
            }
        }

        public virtual void AddByUrl(string url)
        {
            if (isAddOrRemoveInProgress)
                return;

            addOperation.AddByUrlOrPath(url, PackageTag.Git);
            SetupAddOperation();
        }

        public virtual void AddByIds(IEnumerable<string> versionIds)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.AddByIds(versionIds);
            SetupAddAndRemoveOperation();
        }

        public virtual void RemoveByNames(IEnumerable<string> packagesNames)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.RemoveByNames(packagesNames);
            SetupAddAndRemoveOperation();
        }

        public virtual void AddAndResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.AddAndResetDependencies(packageId, dependencyPackagesNames);
            SetupAddAndRemoveOperation();
        }

        public virtual void ResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.ResetDependencies(packageId, dependencyPackagesNames);
            SetupAddAndRemoveOperation();
        }

        private void SetupAddAndRemoveOperation()
        {
            var progressUpdates = addAndRemoveOperation.packageIdsToReset.Select(idOrName => (idOrName, PackageProgress.Resetting))
                .Concat(addAndRemoveOperation.packageIdsToAdd.Select(idOrName => (idOrName, PackageProgress.Installing)))
                .Concat(addAndRemoveOperation.packagesNamesToRemove.Select(name => (name, PackageProgress.Removing)));
            onPackagesProgressChange?.Invoke(progressUpdates);

            addAndRemoveOperation.onProcessResult += OnProcessAddAndRemoveResult;
            addAndRemoveOperation.onOperationError += (_, error) =>
            {
                // For now we only handle addAndRemove operation error when there's a primary `packageName` for the operation
                // This indicates that this operation is likely related to resetting a specific package.
                // For all other cases, since PAK team only provide one error message for all packages, we don't know which package
                // to attach the operation error to, so we don't do any extra handling other than logging the error in the console.
                // And we'll need to discuss with the PAK team if we want to properly handle error messaging in the UI.
                var packageName = addAndRemoveOperation.packageName;
                if (!string.IsNullOrEmpty(packageName))
                    onPackageOperationError?.Invoke(packageName, error);
            };
            addAndRemoveOperation.onOperationFinalized += (_) =>
            {
                var allIdOrNames = addAndRemoveOperation.packageIdsToAdd.Concat(addAndRemoveOperation.packageIdsToReset).Concat(addAndRemoveOperation.packagesNamesToRemove);
                onPackagesProgressChange?.Invoke(allIdOrNames.Select(idOrName => (idOrName, PackageProgress.None)));
            };
            addAndRemoveOperation.logErrorInConsole = true;
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
            if (offlineMode)
                operation.ListOffline(listOperation.lastSuccessTimestamp);
            else
                operation.List();
            operation.onProcessResult += request => OnProcessListResult(request, offlineMode);
            operation.logErrorInConsole = true;
            onListOperation(operation);
        }

        private void OnProcessListResult(ListRequest request, bool offlineMode)
        {
            // skip operation when the result from the online operation is more up-to-date.
            if (offlineMode && listOfflineOperation.timestamp < listOperation.lastSuccessTimestamp)
                return;

            m_UpmCache.SetInstalledPackageInfos(request.Result, listOperation.lastSuccessTimestamp);
        }

        public virtual void RemoveByName(string packageName)
        {
            if (isAddOrRemoveInProgress)
                return;
            removeOperation.Remove(packageName);
            SetupRemoveOperation();
        }

        public virtual void RemoveEmbeddedByName(string packageName)
        {
            if (isAddOrRemoveInProgress)
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
                    Debug.Log($"[Package Manager Window] Cannot remove embedded package {packageName}: {e.Message}");
                }
            }
        }

        private void SetupRemoveOperation()
        {
            onPackagesProgressChange?.Invoke(new[] { (removeOperation.packageName, progress: PackageProgress.Removing) });

            removeOperation.onProcessResult += OnProcessRemoveResult;
            removeOperation.onOperationError += (_, error) => onPackageOperationError?.Invoke(removeOperation.packageName, error);
            removeOperation.onOperationFinalized += (_) =>
                onPackagesProgressChange?.Invoke(new[] { (removeOperation.packageName, progress: PackageProgress.None) });

            removeOperation.logErrorInConsole = true;
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
            if (offlineMode)
                operation.SearchAllOffline(searchOperation.lastSuccessTimestamp);
            else
                operation.SearchAll();
            operation.onProcessResult += request => OnProcessSearchAllResult(request, offlineMode);
            operation.logErrorInConsole = true;
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
            operation.Search(packageIdOrName);
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
            if (m_AddOperation?.isInProgress ?? false)
            {
                SetupAddOperation();
                m_AddOperation.RestoreProgress();
            }

            if (m_RemoveOperation?.isInProgress ?? false)
            {
                SetupRemoveOperation();
                m_RemoveOperation.RestoreProgress();
            }

            if (m_AddAndRemoveOperation?.isInProgress ?? false)
            {
                SetupAddAndRemoveOperation();
                m_AddAndRemoveOperation.RestoreProgress();
            }

            if (m_ListOperation?.isInProgress ?? false)
                List();

            if (m_ListOfflineOperation?.isInProgress ?? false)
                List(true);

            if (m_SearchOperation?.isInProgress ?? false)
                SearchAll();
        }

        public void OnEnable()
        {
            RestoreInProgressOperations();
        }

        public void OnDisable()
        {
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

        public virtual bool IsUnityPackage(PackageInfo packageInfo)
        {
            if (packageInfo?.source == PackageSource.BuiltIn)
                return true;

            if (packageInfo == null
                || packageInfo.source != PackageSource.Registry
                || packageInfo.registry?.isDefault != true
                || string.IsNullOrEmpty(packageInfo.registry?.url)
                || !packageInfo.versions.all.Any()
                || packageInfo.entitlements?.licensingModel == EntitlementLicensingModel.AssetStore)
                return false;

            if (m_RegistryUrls.TryGetValue(packageInfo.registry.url, out var isUnityRegistry))
                return isUnityRegistry;

            isUnityRegistry = IsUnityUrl(packageInfo.registry.url);
            m_RegistryUrls[packageInfo.registry.url] = isUnityRegistry;
            return isUnityRegistry;
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
    }
}
