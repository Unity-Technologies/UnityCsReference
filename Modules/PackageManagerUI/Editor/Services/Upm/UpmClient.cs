// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        event Action<IReadOnlyCollection<string>> onPackagesReadyToReevaluate;
        event Action<IOperation> onListOperation;
        event Action<IOperation> onSearchAllOperation;
        event Action<IOperation> onPackOperation;

        bool isAddOrRemoveInProgress { get; }
        bool isEmbedInProgress { get; }
        IReadOnlyCollection<string> packageIdsOrNamesInstalling { get; }

        void OnRegisteredPackages();
        bool IsAnyExperimentalPackagesInUse();
        bool IsRemoveInProgress(string packageName);
        bool IsAddInProgress(string packageId);
        void AddById(string packageId, bool isUnlisted, OperationType operationType);
        bool AddByPath(string path, OperationType operationType, out string tempPackageId);
        void AddByUrl(string url, OperationType operationType);
        void AddByIds(string[] versionIds, OperationType operationType);
        void RemoveByNames(string[] packagesNames, OperationType operationType);
        void AddAndResetDependencies(string packageId, string[] dependencyPackagesNames, OperationType operationType);
        void ResetDependencies(string packageId, string[] dependencyPackagesNames, OperationType operationType);
        void List(bool offlineMode = false);
        void RemoveByName(string packageName, OperationType operationType);
        void RemoveEmbeddedByName(string packageName);
        void Embed(string packageName);
        void SearchAll(bool offlineMode = false);
        void SearchNonDiscoverable(string packageName, Action doneCallback = null);
        void ExtraFetchPackageInfo(string packageId, Action<PackageInfo> successCallback = null, Action<UIError> errorCallback = null, Action doneCallback = null);
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
        public event Action<IReadOnlyCollection<string>> onPackagesReadyToReevaluate = delegate {};

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
        private UpmSearchOperation[] m_SerializedInProgressSearchNonDiscoverableOperations = Array.Empty<UpmSearchOperation>();

        [SerializeField]
        private UpmSearchOperation[] m_SerializedInProgressExtraFetchOperations = Array.Empty<UpmSearchOperation>();

        [SerializeField]
        private List<string> m_PackagesToReevaluate = new();

        [SerializeField]
        private long m_RegisteredPackagesTimestamp = -1;

        private readonly Dictionary<string, UpmSearchOperation> m_SearchNonDiscoverableOperations = new();
        private readonly Dictionary<string, UpmSearchOperation> m_ExtraFetchOperations = new();

        private readonly IUpmCache m_UpmCache;
        private readonly IFetchStatusTracker m_FetchStatusTracker;
        private readonly IIOProxy m_IOProxy;
        private readonly IClientProxy m_ClientProxy;
        private readonly IApplicationProxy m_Application;
        private readonly IDateTimeProxy m_DateTimeProxy;
        public UpmClient(IUpmCache upmCache,
            IFetchStatusTracker fetchStatusTracker,
            IIOProxy ioProxy,
            IClientProxy clientProxy,
            IApplicationProxy applicationProxy,
            IDateTimeProxy dateTimeProxy)
        {
            m_UpmCache = RegisterDependency(upmCache);
            m_FetchStatusTracker = RegisterDependency(fetchStatusTracker);
            m_IOProxy = RegisterDependency(ioProxy);
            m_ClientProxy = RegisterDependency(clientProxy);
            m_Application = RegisterDependency(applicationProxy);
            m_DateTimeProxy = RegisterDependency(dateTimeProxy);
        }

        public void OnRegisteredPackages()
        {
            m_RegisteredPackagesTimestamp = m_DateTimeProxy.now.Ticks;
            TriggerReevaluation();
        }

        private void TriggerReevaluation()
        {
            if (m_PackagesToReevaluate.Count == 0)
                return;
            onPackagesReadyToReevaluate?.Invoke(m_PackagesToReevaluate);
            m_PackagesToReevaluate.Clear();
        }

        public bool IsAnyExperimentalPackagesInUse()
        {
            return Array.Exists(PackageInfo.GetAllRegisteredPackages(), info => SemVersionParser.TryParse(info.version, out var parsedVersion) && parsedVersion?.GetExpOrPreOrReleaseTag() == PackageTag.Experimental);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedInProgressSearchNonDiscoverableOperations = m_SearchNonDiscoverableOperations.Values.Filter(i => i.isInProgress).ToNewArray(m_SearchNonDiscoverableOperations.Count) ?? Array.Empty<UpmSearchOperation>();
            m_SerializedInProgressExtraFetchOperations = m_ExtraFetchOperations.Values.Filter(i => i.isInProgress).ToNewArray(m_ExtraFetchOperations.Count) ?? Array.Empty<UpmSearchOperation>();
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

        public IReadOnlyCollection<string> packageIdsOrNamesInstalling => m_AddAndRemoveOperation is not { isInProgress: true } ? Array.Empty<string>() : m_AddAndRemoveOperation.packageIdsToAdd;

        public bool IsRemoveInProgress(string packageName)
        {
            return m_AddAndRemoveOperation?.isInProgress == true && m_AddAndRemoveOperation.packagesNamesToRemove.ContainsMatches(packageName);
        }

        public bool IsAddInProgress(string packageId)
        {
            return m_AddAndRemoveOperation?.isInProgress == true && m_AddAndRemoveOperation.packageIdsToAdd.ContainsMatches(packageId);
        }

        public void AddById(string packageId, bool isUnlisted, OperationType operationType)
        {
            if (isAddOrRemoveInProgress)
                return;

            addAndRemoveOperation.AddById(packageId, isUnlisted, operationType);
            SetupAddAndRemoveOperation();
        }

        public bool AddByPath(string path, OperationType operationType, out string tempPackageId)
        {
            tempPackageId = string.Empty;
            if (isAddOrRemoveInProgress)
                return false;

            try
            {
                tempPackageId = GetTempPackageIdFromPath(path);
                addAndRemoveOperation.AddByPathOrUrl(tempPackageId, operationType);
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

        public void AddByUrl(string url, OperationType operationType)
        {
            if (isAddOrRemoveInProgress)
                return;

            addAndRemoveOperation.AddByPathOrUrl(url, operationType);
            SetupAddAndRemoveOperation();
        }

        public void AddByIds(string[] versionIds, OperationType operationType)
        {
            if (isAddOrRemoveInProgress)
                return;

            addAndRemoveOperation.AddByIds(versionIds, operationType);
            SetupAddAndRemoveOperation();
        }

        public void RemoveByNames(string[] packagesNames, OperationType operationType)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.RemoveByNames(packagesNames, operationType);
            SetupAddAndRemoveOperation();
        }

        public void AddAndResetDependencies(string packageId, string[] dependencyPackagesNames, OperationType operationType)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.AddAndResetDependencies(packageId, dependencyPackagesNames, operationType);
            SetupAddAndRemoveOperation();
        }

        public void ResetDependencies(string packageId, string[] dependencyPackagesNames, OperationType operationType)
        {
            if (isAddOrRemoveInProgress)
                return;
            addAndRemoveOperation.ResetDependencies(packageId, dependencyPackagesNames, operationType);
            SetupAddAndRemoveOperation();
        }

       private void SetupAddAndRemoveOperation()
        {
            var progressUpdates = addAndRemoveOperation.packageIdsToReset.SelectAsEnumerable(idOrName => (idOrName, PackageProgress.Resetting))
                .Join(addAndRemoveOperation.packageIdsToAdd.SelectAsEnumerable(idOrName => (idOrName, PackageProgress.Installing)))
                .Join(addAndRemoveOperation.packagesNamesToRemove.SelectAsEnumerable(name => (name, PackageProgress.Removing)));
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

                var allIdOrNames = addAndRemoveOperation.packageIdsToAdd.Join(addAndRemoveOperation.packageIdsToReset).Join(addAndRemoveOperation.packagesNamesToRemove);
                onPackagesProgressChange?.Invoke(allIdOrNames.SelectAsEnumerable(idOrName => (idOrName, PackageProgress.None)));
            };
            addAndRemoveOperation.logErrorInConsole = true;
        }

       private bool FindTrustIssuePackagesAndShowPopUp(PackageCollection requestResult)
       {
            var viewData = ActiveTrustWindow.CreateViewData(m_UpmCache, requestResult, addAndRemoveOperation.operationType, m_Application.shortUnityVersion);
            if (viewData != null)
                return ActiveTrustWindow.Show(viewData) == ActiveTrustReturnValue.ProceedAnyway;
            return true;
       }

        private void OnProcessAddAndRemoveResult(Request<PackageCollection> request)
        {
            var updatedInfos = m_UpmCache.SetInstalledPackageInfos(request.Result, changedSource: PackagesChangedSource.AddAndRemove);

            foreach (var (_, newInfo) in updatedInfos)
            {
                var name = newInfo?.name;
                if (!string.IsNullOrEmpty(name))
                    m_PackagesToReevaluate.Add(name);
            }

            // In some occasions, package registration already happened before the addAndRemove results are processed. In this case a future registration event is not coming,
            // and we need to do the reevaluation right away. This does generate the packages twice, but a bigger rework of the current flow is needed to address that.
            if (m_RegisteredPackagesTimestamp > addAndRemoveOperation.timestamp)
                TriggerReevaluation();

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

            m_UpmCache.SetInstalledPackageInfos(request.Result, listOfflineOperation.dataTimestamp, PackagesChangedSource.UpmList);
        }

        public void RemoveByName(string packageName, OperationType operationType)
        {
            if (isAddOrRemoveInProgress)
                return;

            addAndRemoveOperation.RemoveByNames(new [] {packageName}, operationType);
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
                foreach (var file in m_IOProxy.GetFiles(resolvedPath, "*", System.IO.SearchOption.AllDirectories))
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

        public void SearchNonDiscoverable(string packageName, Action doneCallback = null)
        {
            if (!m_SearchNonDiscoverableOperations.TryGetValue(packageName, out var operation))
            {
                operation = new UpmSearchOperation();
                operation.ResolveDependencies(m_ClientProxy, m_Application);
                operation.Search(packageName);
                operation.onProcessResult += request =>
                {
                    var packageInfo = request.Result.Length > 0 ? request.Result[0] : null;
                    m_UpmCache.AddSearchNonDiscoverableResult(packageName, packageInfo, operation.dataTimestamp);
                    m_FetchStatusTracker.SetSearchInfoFetchSuccess(packageName);
                };
                operation.onOperationFinalized += _ => m_SearchNonDiscoverableOperations.Remove(packageName);
                operation.onOperationError += (_, error) => m_FetchStatusTracker.SetSearchInfoFetchError(packageName, error);
                m_FetchStatusTracker.SetSearchInfoFetchInProgress(packageName);
                m_SearchNonDiscoverableOperations[packageName] = operation;
            }
            if (doneCallback != null)
                operation.onOperationFinalized += _ => doneCallback.Invoke();
        }

        public void ExtraFetchPackageInfo(string packageId, Action<PackageInfo> successCallback = null, Action<UIError> errorCallback = null, Action doneCallback = null)
        {
            if (!m_ExtraFetchOperations.TryGetValue(packageId, out var operation))
            {
                operation = new UpmSearchOperation();
                operation.ResolveDependencies(m_ClientProxy, m_Application);
                operation.Search(packageId);
                operation.onProcessResult += request => m_UpmCache.AddExtraFetchResult(request.Result.Length > 0 ? request.Result[0] : null);
                operation.onOperationFinalized += _ => m_ExtraFetchOperations.Remove(packageId);
                m_ExtraFetchOperations[packageId] = operation;
            }

            if (successCallback != null)
                operation.onProcessResult += request => successCallback.Invoke(request.Result.Length > 0 ? request.Result[0] : null);
            if (errorCallback != null)
                operation.onOperationError += (_, error) => errorCallback.Invoke(error);
            if (doneCallback != null)
                operation.onOperationFinalized += _ => doneCallback.Invoke();
        }

        // Restore operations interrupted by domain reloads
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

            foreach (var operation in m_SerializedInProgressSearchNonDiscoverableOperations)
                SearchNonDiscoverable(operation.packageIdOrName);

            foreach (var operation in m_SerializedInProgressExtraFetchOperations)
                ExtraFetchPackageInfo(operation.packageIdOrName);
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
            m_SearchNonDiscoverableOperations.Clear();
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
