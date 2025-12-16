// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IUpmClient : IService
    {
        event Action<string> onSpecialInstallStart;
        event Action<string, string> onSpecialInstallFinalize;
        event Action<IEnumerable<(string packageIdOrName, PackageProgress progress)>> onPackagesProgressChange;
        event Action<string, UIError> onPackageOperationError;
        event Action<IOperation> onListOperation;
        event Action<IOperation> onSearchAllOperation;
        event Action<IOperation> onPackOperation;

        bool isAddOrRemoveInProgress { get; }
        bool isEmbedInProgress { get; }
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
        void Embed(string packageName);
        void SearchAll(bool offlineMode = false);
        void ExtraFetchPackageInfo(string packageIdOrName, long productId = 0, Action<PackageInfo> successCallback = null, Action<UIError> errorCallback = null, Action doneCallback = null);
        void ClearCache();
        void Resolve(bool delayCall = false);
        void Pack(string packageName, string packageFolder, string exportPath, string orgId);
    }

    [Serializable]
    internal class UpmClient : BaseService<IUpmClient>, IUpmClient, ISerializationCallbackReceiver
    {
        // SpecialInstall refers to installation of packages that are not already in the PackageDatabase, and not through directly clicking "Install" button in the UI and in most cases
        // we don't know the final packageId until the installation is finalized, hence we need to do some special handling. Those specially install packages are usually from git/local/tarball,
        // but since in the "Add package by git url" UI, we don't restrict people to only install git packages, non git packages can go through this special install flow as well
        public event Action<string> onSpecialInstallStart = delegate {};
        public event Action<string, string> onSpecialInstallFinalize = delegate {};

        public event Action<IEnumerable<(string packageIdOrName, PackageProgress progress)>> onPackagesProgressChange = delegate { };
        public event Action<string, UIError> onPackageOperationError = delegate { };

        public event Action<IOperation> onListOperation = delegate {};
        public event Action<IOperation> onSearchAllOperation = delegate {};
        public event Action<IOperation> onPackOperation = delegate {};

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
        private UpmAddAndRemoveOperation m_AddAndRemoveOperation;
        private UpmAddAndRemoveOperation addAndRemoveOperation => CreateOperation(ref m_AddAndRemoveOperation);

        [SerializeField]
        private UpmEmbedOperation m_EmbedOperation;
        private UpmEmbedOperation embedOperation => CreateOperation(ref m_EmbedOperation);

        [SerializeField]
        private UpmPackOperation m_PackOperation;
        private UpmPackOperation packOperation => CreateOperation(ref m_PackOperation);

        [SerializeField]
        private UpmSearchOperation[] m_SerializedInProgressExtraFetchOperations = Array.Empty<UpmSearchOperation>();

        private readonly Dictionary<string, UpmSearchOperation> m_ExtraFetchOperations = new();

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
            return PackageInfo.GetAllRegisteredPackages().Any(info => SemVersionParser.TryParse(info.version, out var parsedVersion) && parsedVersion?.GetExpOrPreOrReleaseTag() == PackageTag.Experimental);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedInProgressExtraFetchOperations = m_ExtraFetchOperations?.Values.Where(i => i.isInProgress).ToArray() ?? new UpmSearchOperation[0];
        }

        public void OnAfterDeserialize()
        {
            m_SearchOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_SearchOfflineOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_ListOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_ListOfflineOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_AddAndRemoveOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_PackOperation?.ResolveDependencies(m_ClientProxy, m_Application);
        }

        public bool isAddOrRemoveInProgress => m_AddAndRemoveOperation?.isInProgress == true;

        public bool isEmbedInProgress => m_EmbedOperation?.isInProgress == true;

        public IEnumerable<string> packageIdsOrNamesInstalling
        {
            get
            {
                if (m_AddAndRemoveOperation?.isInProgress == true)
                    foreach (var id in m_AddAndRemoveOperation.packageIdsToAdd)
                        yield return id;
            }
        }

        public bool IsRemoveInProgress(string packageName)
        {
            return m_AddAndRemoveOperation?.isInProgress == true && m_AddAndRemoveOperation.packagesNamesToRemove.Contains(packageName);
        }

        public bool IsAddInProgress(string packageId)
        {
            return m_AddAndRemoveOperation?.isInProgress == true && m_AddAndRemoveOperation.packageIdsToAdd.Contains(packageId);
        }

        public void AddById(string packageId)
        {
            if (isAddOrRemoveInProgress)
                return;

            addAndRemoveOperation.AddById(packageId);
            SetupAddAndRemoveOperation();
        }

        public bool AddByPath(string path, out string tempPackageId)
        {
            tempPackageId = string.Empty;
            if (isAddOrRemoveInProgress)
                return false;

            try
            {
                tempPackageId = GetTempPackageIdFromPath(path);
                addAndRemoveOperation.AddByPathOrUrl(tempPackageId);
                SetupAddAndRemoveOperation();

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

            return $"file:../{relativePathToProjectRoot}";
        }

        public void AddByUrl(string url)
        {
            if (isAddOrRemoveInProgress)
                return;

            addAndRemoveOperation.AddByPathOrUrl(url);
            SetupAddAndRemoveOperation();
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

            if (addAndRemoveOperation.isSpecialInstall)
                onSpecialInstallStart?.Invoke(addAndRemoveOperation.packageIdOrName);

            addAndRemoveOperation.SetDryRunFunction(FindTrustIssuePackagesAndShowPopUp);

            addAndRemoveOperation.onProcessResult += OnProcessAddAndRemoveResult;
            addAndRemoveOperation.onOperationError += (_, error) =>
            {
                // For now, we only handle addAndRemove operation error when there's a primary `packageName` for the operation
                // This indicates that this operation is likely related to resetting a specific package.
                // For all other cases, since PAK team only provide one error message for all packages, we don't know which package
                // to attach the operation error to, so we don't do any extra handling other than logging the error in the console.
                // And we'll need to discuss with the PAK team if we want to properly handle error messaging in the UI.
                var packageName = addAndRemoveOperation.packageName;
                if (!string.IsNullOrEmpty(packageName))
                    onPackageOperationError?.Invoke(packageName, error);
            };
            addAndRemoveOperation.onOperationFinalized += _ =>
            {
                var mainPackageInfo = addAndRemoveOperation.FindMainPackageInfoFromResult();
                if (addAndRemoveOperation.isSpecialInstall)
                    onSpecialInstallFinalize?.Invoke(addAndRemoveOperation.packageIdOrName, mainPackageInfo?.name ?? string.Empty);

                var allIdOrNames = addAndRemoveOperation.packageIdsToAdd.Concat(addAndRemoveOperation.packageIdsToReset).Concat(addAndRemoveOperation.packagesNamesToRemove);
                onPackagesProgressChange?.Invoke(allIdOrNames.Select(idOrName => (idOrName, PackageProgress.None)));
            };
            addAndRemoveOperation.logErrorInConsole = true;
        }

       private bool FindTrustIssuePackagesAndShowPopUp(PackageCollection requestResult)
       {
            var invalidSignaturePackages = new List<PackageInfo>();
            var missingSignaturePackages = new List<PackageInfo>();
            var limitedTrustPackages = new List<PackageInfo>();

            foreach (var info in requestResult)
            {
                if (info == null)
                    continue;

                var trustAndSignature = UpmPackageVersion.GetTrustAndSignature(info, true);
                var currentlyInstalled = m_UpmCache.GetInstalledPackageInfo(info.name);
                if (currentlyInstalled?.packageId == info.packageId && UpmPackageVersion.GetTrustAndSignature(currentlyInstalled, true) == trustAndSignature)
                    continue;
                switch (trustAndSignature)
                {
                    case TrustAndSignature.UntrustedInvalidSignature:
                        invalidSignaturePackages.Add(info);
                        break;
                    case TrustAndSignature.UntrustedNoSignature:
                        missingSignaturePackages.Add(info);
                        break;
                    case TrustAndSignature.LimitedTrust:
                        limitedTrustPackages.Add(info);
                        break;
                }
            }

            if (invalidSignaturePackages.Count == 0 && missingSignaturePackages.Count == 0 && limitedTrustPackages.Count == 0)
                return true;

            return ActiveTrustWindow.ShowActiveTrustWindow(invalidSignaturePackages, missingSignaturePackages, limitedTrustPackages) == ActiveTrustReturnValue.InstallAnyway;
        }

        private void OnProcessAddAndRemoveResult(Request<PackageCollection> request)
        {
            var updatedInfos = m_UpmCache.SetInstalledPackageInfos(request.Result, changedSource: PackagesChangedSource.AddAndRemove);

            var mainPackageInfo = addAndRemoveOperation.FindMainPackageInfoFromResult();
            if (updatedInfos.Count == 0 && mainPackageInfo?.source == PackageSource.Git)
                Debug.Log(string.Format(L10n.Tr("{0} is already up-to-date."), mainPackageInfo.displayName));
            else if (updatedInfos.Count > 0)
            {
                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                    {
                        foreach (var (oldInfo, newInfo) in updatedInfos)
                        {
                            if (newInfo == null)
                                extension.OnPackageRemoved(oldInfo);
                            else
                                extension.OnPackageAddedOrUpdated(newInfo);
                        }
                    }
                });
            }
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
            if (offlineMode && listOfflineOperation.dataTimestamp < listOperation.lastSuccessTimestamp)
                return;

            m_UpmCache.SetInstalledPackageInfos(request.Result, listOfflineOperation.dataTimestamp);
        }

        public void RemoveByName(string packageName)
        {
            if (isAddOrRemoveInProgress)
                return;

            addAndRemoveOperation.RemoveByNames(new [] {packageName});
            SetupAddAndRemoveOperation();
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

        public void Embed(string packageName)
        {
            if (embedOperation.isInProgress)
                return;
            embedOperation.Embed(packageName);
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
            if (offlineMode && searchOfflineOperation.dataTimestamp < searchOperation.lastSuccessTimestamp)
                return;

            m_UpmCache.SetSearchPackageInfos(request.Result, searchOfflineOperation.dataTimestamp);
        }

        public void ExtraFetchPackageInfo(string packageIdOrName, long productId = 0, Action<PackageInfo> successCallback = null, Action<UIError> errorCallback = null, Action doneCallback = null)
        {
            if (!m_ExtraFetchOperations.TryGetValue(packageIdOrName, out var operation))
            {
                operation = new UpmSearchOperation();
                operation.ResolveDependencies(m_ClientProxy, m_Application);
                operation.Search(packageIdOrName, productId);
                operation.onProcessResult += request => OnProcessExtraFetchResult(request, operation.dataTimestamp, productId);
                operation.onOperationFinalized += _ => m_ExtraFetchOperations.Remove(packageIdOrName);
                m_ExtraFetchOperations[packageIdOrName] = operation;

                if (productId > 0)
                {
                    operation.onOperationError += (_, error) => m_FetchStatusTracker.SetFetchError(productId, FetchType.ProductSearchInfo, error);
                    m_FetchStatusTracker.SetFetchInProgress(productId, FetchType.ProductSearchInfo);
                }
            }

            if (successCallback != null)
                operation.onProcessResult += request => successCallback.Invoke(request.Result.FirstOrDefault());
            if (errorCallback != null)
                operation.onOperationError += (_, error) => errorCallback.Invoke(error);
            if (doneCallback != null)
                operation.onOperationFinalized += _ => doneCallback.Invoke();
        }

        private void OnProcessExtraFetchResult(SearchRequest request, long timestamp, long productId = 0)
        {
            var packageInfo = request.Result.FirstOrDefault();
            if (productId > 0)
            {
                m_UpmCache.SetProductSearchPackageInfo(productId, packageInfo, timestamp);
                m_FetchStatusTracker.SetFetchSuccess(productId, FetchType.ProductSearchInfo);
            }
            else
                m_UpmCache.AddExtraPackageInfo(packageInfo);
        }

        // Restore operations that's interrupted by domain reloads
        private void RestoreInProgressOperations()
        {
            if (m_AddAndRemoveOperation?.isInProgress ?? false)
            {
                SetupAddAndRemoveOperation();
                m_AddAndRemoveOperation.RestoreProgress();
            }

            if (m_PackOperation?.isInProgress ?? false)
            {
                SetupPackOperation();
                m_PackOperation.RestoreProgress();
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

        public void Resolve(bool delayCall = false)
        {
            if (delayCall)
                EditorApplication.delayCall += () => m_ClientProxy.Resolve();
            else
                m_ClientProxy.Resolve();
        }

        public void Pack(string packageName, string packageFolder, string exportPath, string orgId)
        {
            packOperation.Pack(packageName, packageFolder, exportPath, orgId);
            SetupPackOperation();
        }

        private void SetupPackOperation()
        {
            onPackagesProgressChange?.Invoke(new[] { (packOperation.packageName, PackageProgress.Exporting) });
            packOperation.onOperationFinalized += _ => onPackagesProgressChange?.Invoke(new[] { (packOperation.packageName, PackageProgress.None) });

            onPackOperation?.Invoke(packOperation);
        }

        private T CreateOperation<T>(ref T operation) where T : UpmBaseOperation, new()
        {
            operation ??= new T();
            operation.ResolveDependencies(m_ClientProxy, m_Application);
            return operation;
        }
    }
}
