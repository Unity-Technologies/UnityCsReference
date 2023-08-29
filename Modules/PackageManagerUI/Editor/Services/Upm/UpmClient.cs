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
    internal interface IUpmClient : IService
    {
        event Action<IEnumerable<(string packageIdOrName, PackageProgress progress)>> onPackagesProgressChange;
        event Action<string, UIError> onPackageOperationError;
        event Action<IOperation> onListOperation;
        event Action<IOperation> onSearchAllOperation;
        event Action<IOperation> onAddOperation;

        bool isAddOrRemoveInProgress { get; }
        IEnumerable<string> packageIdsOrNamesInstalling { get; }

        bool IsAnyExperimentalPackagesInUse();
        bool IsRemoveInProgress(string packageName);
        bool IsAddInProgress(string packageId);
        void AddById(string packageId);
        bool AddByPath(string path, out string tempPackageId);
        void AddByUrl(string url);
        void AddByIds(IEnumerable<string> versionIds);
        void RemoveByNames(IEnumerable<string> packagesNames);
        void AddAndResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames);
        void ResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames);
        void List(bool offlineMode = false);
        void RemoveByName(string packageName);
        void RemoveEmbeddedByName(string packageName);
        void SearchAll(bool offlineMode = false);
        void ExtraFetchPackageInfo(string packageIdOrName, long productId = 0, Action<PackageInfo> successCallback = null, Action<UIError> errorCallback = null, Action doneCallback = null);
        void ClearCache();
        void Resolve();
        RegistryType GetAvailableRegistryType(PackageInfo packageInfo);
        bool IsUnityPackage(PackageInfo packageInfo);
    }

    [Serializable]
    internal class UpmClient : BaseService<IUpmClient>, IUpmClient, ISerializationCallbackReceiver
    {
        private static readonly string[] k_UnityRegistryUrlsHosts = { ".unity.com", ".unity3d.com" };

        public event Action<IEnumerable<(string packageIdOrName, PackageProgress progress)>> onPackagesProgressChange = delegate { };
        public event Action<string, UIError> onPackageOperationError = delegate { };

        public event Action<IOperation> onListOperation = delegate {};
        public event Action<IOperation> onSearchAllOperation = delegate {};
        public event Action<IOperation> onAddOperation = delegate {};

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
        private UpmSearchOperation[] m_SerializedInProgressExtraFetchOperations = Array.Empty<UpmSearchOperation>();

        private readonly Dictionary<string, UpmSearchOperation> m_ExtraFetchOperations = new Dictionary<string, UpmSearchOperation>();

        [SerializeField]
        private string[] m_SerializedRegistryUrlsKeys;

        [SerializeField]
        private RegistryType[] m_SerializedRegistryUrlsValues;

        private Dictionary<string, RegistryType> m_RegistryUrls = new();

        private readonly IUpmCache m_UpmCache;
        private readonly IFetchStatusTracker m_FetchStatusTracker;
        private readonly IIOProxy m_IOProxy;
        private readonly IClientProxy m_ClientProxy;
        private readonly IApplicationProxy m_Application;
        public UpmClient(IUpmCache upmCache,
            IFetchStatusTracker fetchStatusTracker,
            IIOProxy ioProxy,
            IClientProxy clientProxy,
            IApplicationProxy applicationProxy)
        {
            m_UpmCache = RegisterDependency(upmCache);
            m_FetchStatusTracker = RegisterDependency(fetchStatusTracker);
            m_IOProxy = RegisterDependency(ioProxy);
            m_ClientProxy = RegisterDependency(clientProxy);
            m_Application = RegisterDependency(applicationProxy);
        }

        public bool IsAnyExperimentalPackagesInUse()
        {
            return PackageInfo.GetAllRegisteredPackages().Any(info => (info.version.Contains("-preview") || info.version.Contains("-exp.") || info.version.StartsWith("0.")) && IsUnityPackage(info));
        }

        public void OnBeforeSerialize()
        {
            m_SerializedRegistryUrlsKeys = m_RegistryUrls?.Keys.ToArray() ?? new string[0];
            m_SerializedRegistryUrlsValues = m_RegistryUrls?.Values.ToArray() ?? new RegistryType[0];

            m_SerializedInProgressExtraFetchOperations = m_ExtraFetchOperations?.Values.Where(i => i.isInProgress).ToArray() ?? new UpmSearchOperation[0];
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedRegistryUrlsKeys.Length; i++)
                m_RegistryUrls[m_SerializedRegistryUrlsKeys[i]] = m_SerializedRegistryUrlsValues[i];

            m_SearchOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_SearchOfflineOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_ListOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_ListOfflineOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_AddOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_RemoveOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_AddAndRemoveOperation?.ResolveDependencies(m_ClientProxy, m_Application);
        }

        public bool isAddOrRemoveInProgress => m_AddOperation?.isInProgress == true ||
            m_RemoveOperation?.isInProgress == true || m_AddAndRemoveOperation?.isInProgress == true;

        public IEnumerable<string> packageIdsOrNamesInstalling
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

        public bool IsRemoveInProgress(string packageName)
        {
            return (m_RemoveOperation?.isInProgress == true && m_RemoveOperation.packageName == packageName) ||
                (m_AddAndRemoveOperation?.isInProgress == true && m_AddAndRemoveOperation.packagesNamesToRemove.Contains(packageName));
        }

        public bool IsAddInProgress(string packageId)
        {
            return (m_AddOperation?.isInProgress == true && m_AddOperation.packageIdOrName == packageId) ||
                (m_AddAndRemoveOperation?.isInProgress == true && m_AddAndRemoveOperation.packageIdsToAdd.Contains(packageId));
        }

        public void AddById(string packageId)
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

        public bool AddByPath(string path, out string tempPackageId)
        {
            tempPackageId = string.Empty;
            if (isAddOrRemoveInProgress)
                return false;

            try
            {
                tempPackageId = GetTempPackageIdFromPath(path);
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

        public void AddByUrl(string url)
        {
            if (isAddOrRemoveInProgress)
                return;

            addOperation.AddByUrlOrPath(url, PackageTag.Git);
            SetupAddOperation();
        }

        public void AddByIds(IEnumerable<string> versionIds)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.AddByIds(versionIds);
            SetupAddAndRemoveOperation();
        }

        public void RemoveByNames(IEnumerable<string> packagesNames)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.RemoveByNames(packagesNames);
            SetupAddAndRemoveOperation();
        }

        public void AddAndResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.AddAndResetDependencies(packageId, dependencyPackagesNames);
            SetupAddAndRemoveOperation();
        }

        public void ResetDependencies(string packageId, IEnumerable<string> dependencyPackagesNames)
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

        public void List(bool offlineMode = false)
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

        public void RemoveByName(string packageName)
        {
            if (isAddOrRemoveInProgress)
                return;
            removeOperation.Remove(packageName);
            SetupRemoveOperation();
        }

        public void RemoveEmbeddedByName(string packageName)
        {
            if (isAddOrRemoveInProgress)
                return;

            var packageInfo = m_UpmCache.GetInstalledPackageInfo(packageName);
            var resolvedPath = packageInfo?.resolvedPath;
            if (string.IsNullOrEmpty(resolvedPath))
                return;

            try
            {
                // Fix case 1237777, make files writable first
                foreach (var file in m_IOProxy.DirectoryGetFiles(resolvedPath, "*", System.IO.SearchOption.AllDirectories))
                    m_IOProxy.MakeFileWritable(file, true);
                m_IOProxy.DeleteDirectory(resolvedPath);
                Resolve();
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot remove embedded package {packageName}: {e.Message}");
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

        public void SearchAll(bool offlineMode = false)
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

        public void ExtraFetchPackageInfo(string packageIdOrName, long productId = 0, Action<PackageInfo> successCallback = null, Action<UIError> errorCallback = null, Action doneCallback = null)
        {
            if (!m_ExtraFetchOperations.TryGetValue(packageIdOrName, out var operation))
            {
                operation = new UpmSearchOperation();
                operation.ResolveDependencies(m_ClientProxy, m_Application);
                operation.Search(packageIdOrName, productId);
                operation.onProcessResult += (request) => OnProcessExtraFetchResult(request, productId);
                operation.onOperationFinalized += (op) => m_ExtraFetchOperations.Remove(packageIdOrName);
                m_ExtraFetchOperations[packageIdOrName] = operation;

                if (productId > 0)
                {
                    operation.onOperationError += (op, error) => m_FetchStatusTracker.SetFetchError(productId, FetchType.ProductSearchInfo, error);
                    m_FetchStatusTracker.SetFetchInProgress(productId, FetchType.ProductSearchInfo);
                }
            }

            if (successCallback != null)
                operation.onProcessResult += (request) => successCallback.Invoke(request.Result.FirstOrDefault());
            if (errorCallback != null)
                operation.onOperationError += (op, error) => errorCallback.Invoke(error);
            if (doneCallback != null)
                operation.onOperationFinalized += (op) => doneCallback.Invoke();
        }

        private void OnProcessExtraFetchResult(SearchRequest request, long productId = 0)
        {
            var packageInfo = request.Result.FirstOrDefault();
            if (productId > 0)
            {
                // This is not really supposed to happen - this happening would mean there's an issue with data from the backend
                // Right now there isn't any recommended actions we can suggest the users to take, so we'll just add a message here
                // to expose it if it ever happens (rather than letting it pass silently)
                if (packageInfo?.assetStore?.productId != productId.ToString())
                {
                    var error = new UIError(UIErrorCode.AssetStorePackageError, L10n.Tr("Product Id mismatch between product details and package details."));
                    m_FetchStatusTracker.SetFetchError(productId, FetchType.ProductSearchInfo, error);
                    return;
                }
                m_UpmCache.SetProductSearchPackageInfo(packageInfo);
                m_FetchStatusTracker.SetFetchSuccess(productId, FetchType.ProductSearchInfo);
            }
            else
                m_UpmCache.AddExtraPackageInfo(packageInfo);
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

            foreach (var operation in m_SerializedInProgressExtraFetchOperations)
                ExtraFetchPackageInfo(operation.packageIdOrName, operation.productId);
        }

        public override void OnEnable()
        {
            RestoreInProgressOperations();
        }

        public override void OnDisable()
        {
        }

        public void ClearCache()
        {
            m_ExtraFetchOperations.Clear();
            m_UpmCache.ClearCache();
        }

        public void Resolve()
        {
            m_ClientProxy.Resolve();
        }

        public RegistryType GetAvailableRegistryType(PackageInfo packageInfo)
        {
            // Special handling for packages that's built in/bundled with unity, we always consider them from the Unity registry
            if (packageInfo?.source == PackageSource.BuiltIn)
                return RegistryType.UnityRegistry;

            if (string.IsNullOrEmpty(packageInfo?.registry?.url))
                return RegistryType.None;

            if (packageInfo.entitlements?.licensingModel == EntitlementLicensingModel.AssetStore)
                return RegistryType.AssetStore;

            if (m_RegistryUrls.TryGetValue(packageInfo.registry.url, out var result))
                return result;

            result = packageInfo.registry.isDefault && IsUnityUrl(packageInfo.registry.url) ? RegistryType.UnityRegistry : RegistryType.MyRegistries;
            m_RegistryUrls[packageInfo.registry.url] = result;
            return result;
        }

        public bool IsUnityPackage(PackageInfo packageInfo)
        {
            return packageInfo is { source: PackageSource.BuiltIn or PackageSource.Registry } &&
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
                operation.ResolveDependencies(m_ClientProxy, m_Application);
                return operation;
            }

            operation = new T();
            operation.ResolveDependencies(m_ClientProxy, m_Application);
            return operation;
        }
    }
}
