// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPageRefreshHandler : IService
    {
        event Action onRefreshOperationStart;
        event Action onRefreshOperationFinish;
        event Action<UIError> onRefreshOperationError;

        void Refresh(IPage page);
        void Refresh(RefreshOptions options);
        void CancelRefresh(RefreshOptions options);
        bool IsRefreshInProgress(RefreshOptions options);
        void SetRefreshTimestampSingleFlag(RefreshOptions option, long timestamp);
        long GetRefreshTimestamp(RefreshOptions options);
        long GetRefreshTimestamp(IPage page);
        UIError GetRefreshError(IPage page);
        bool IsRefreshInProgress(IPage page);
        bool IsInitialFetchingDone(IPage page);
    }

    [Serializable]
    internal class PageRefreshHandler : BaseService<IPageRefreshHandler>, IPageRefreshHandler, ISerializationCallbackReceiver
    {
        public event Action onRefreshOperationStart = delegate { };
        public event Action onRefreshOperationFinish = delegate { };
        public event Action<UIError> onRefreshOperationError = delegate { };

        private Dictionary<RefreshOptions, long> m_RefreshTimestamps = new();
        private Dictionary<RefreshOptions, UIError> m_RefreshErrors = new();

        [NonSerialized]
        private List<IOperation> m_RefreshOperationsInProgress = new();

        // array created to help serialize dictionaries
        [SerializeField]
        private RefreshOptions[] m_SerializedRefreshTimestampsKeys = Array.Empty<RefreshOptions>();

        [SerializeField]
        private long[] m_SerializedRefreshTimestampsValues = Array.Empty<long>();

        [SerializeField]
        private RefreshOptions[] m_SerializedRefreshErrorsKeys = Array.Empty<RefreshOptions>();

        [SerializeField]
        private UIError[] m_SerializedRefreshErrorsValues = Array.Empty<UIError>();

        private readonly IApplicationProxy m_Application;
        private readonly IUpmClient m_UpmClient;
        private readonly IUpmRegistryClient m_UpmRegistryClient;
        private readonly IPageManager m_PageManager;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IAssetDatabaseProxy m_AssetDatabase;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IAssetStoreClient m_AssetStoreClient;

        public PageRefreshHandler(IPageManager pageManager,
            IApplicationProxy application,
            IUnityConnectProxy unityConnect,
            IAssetDatabaseProxy assetDatabase,
            IPackageManagerPrefs packageManagerPrefs,
            IUpmClient upmClient,
            IUpmRegistryClient upmRegistryClient,
            IAssetStoreClient assetStoreClient)
        {
            m_PageManager = RegisterDependency(pageManager);
            m_Application = RegisterDependency(application);
            m_UpmClient = RegisterDependency(upmClient);
            m_UpmRegistryClient = RegisterDependency(upmRegistryClient);
            m_UnityConnect = RegisterDependency(unityConnect);
            m_AssetDatabase = RegisterDependency(assetDatabase);
            m_PackageManagerPrefs = RegisterDependency(packageManagerPrefs);
            m_AssetStoreClient = RegisterDependency(assetStoreClient);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedRefreshTimestampsKeys = m_RefreshTimestamps.Keys.ToArray();
            m_SerializedRefreshTimestampsValues = m_RefreshTimestamps.Values.ToArray();
            m_SerializedRefreshErrorsKeys = m_RefreshErrors.Keys.ToArray();
            m_SerializedRefreshErrorsValues = m_RefreshErrors.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedRefreshTimestampsKeys.Length; i++)
                m_RefreshTimestamps[m_SerializedRefreshTimestampsKeys[i]] = m_SerializedRefreshTimestampsValues[i];

            for (var i = 0; i < m_SerializedRefreshErrorsKeys.Length; i++)
                m_RefreshErrors[m_SerializedRefreshErrorsKeys[i]] = m_SerializedRefreshErrorsValues[i];
        }

        private void OnActivePageChanged(IPage page)
        {
            if (!IsInitialFetchingDone(page))
                Refresh(page);
        }

        // The virtual keyword is needed for unit tests
        public virtual void Refresh(IPage page)
        {
            Refresh(page.refreshOptions);
        }

        // The virtual keyword is needed for unit tests
        public virtual void Refresh(RefreshOptions options)
        {
            if (options.Contains(RefreshOptions.UpmSearch))
            {
                m_UpmClient.SearchAll();
                // Since the SearchAll online call now might return error and an empty list, we want to trigger a `SearchOffline` call if
                // we detect that SearchOffline has not been called before. That way we will have some offline result to show to the user instead of nothing
                if (GetRefreshTimestampSingleFlag(RefreshOptions.UpmSearchOffline) == 0)
                    options |= RefreshOptions.UpmSearchOffline;
            }
            if (options.Contains(RefreshOptions.UpmSearchOffline))
                m_UpmClient.SearchAll(true);
            if (options.Contains(RefreshOptions.UpmList))
            {
                m_UpmClient.List();
                // Do the same logic for the List operations as the Search operations
                if (GetRefreshTimestampSingleFlag(RefreshOptions.UpmListOffline) == 0)
                    options |= RefreshOptions.UpmListOffline;
            }
            if (options.Contains(RefreshOptions.UpmListOffline))
                m_UpmClient.List(true);
            if (options.Contains(RefreshOptions.LocalInfo))
                RefreshLocalInfos();
            if (options.Contains(RefreshOptions.Purchased))
            {
                var page = m_PageManager.GetPage(MyAssetsPage.k_Id);
                var numItems = Math.Max((int)page.visualStates.countLoaded, m_PackageManagerPrefs.numItemsPerPage ?? PackageManagerPrefs.k_DefaultPageSize);
                var queryArgs = new PurchasesQueryArgs(0, numItems, page.trimmedSearchText, page.filters);
                m_AssetStoreClient.ListPurchases(queryArgs);
            }
            if (options.Contains(RefreshOptions.ImportedAssets))
                RefreshImportedAssets();
        }

        private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // if a full scan was never done, it needs to be done now
            // otherwise, we want to avoid doing a full scan on every OnPostprocessAllAssets trigger for performance, so
            //  if a full scan was done already, just modify the cached assets according to the parameters passed by the event
            if (RefreshImportedAssets())
                return;

            SetRefreshTimestampSingleFlag(RefreshOptions.ImportedAssets, DateTime.Now.Ticks);
            m_AssetStoreClient.OnPostProcessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        // returns true if a full scan was done, false if not
        private bool RefreshImportedAssets()
        {
            var needRefresh = GetRefreshTimestampSingleFlag(RefreshOptions.ImportedAssets) == 0;
            // We set the timestamp before the actual operation because the results are retrieved synchronously, if we set the timestamp after
            // we will encounter an issue where the results are back but the UI still thinks the refresh is in progress
            // We also always update the timestamp even when the data has not changed to reflect the last time we do the Refresh operation
            SetRefreshTimestampSingleFlag(RefreshOptions.ImportedAssets, DateTime.Now.Ticks);

            if (needRefresh)
                m_AssetStoreClient.RefreshImportedAssets();
            return needRefresh;
        }

        private void RefreshLocalInfos()
        {
            var needRefresh = GetRefreshTimestampSingleFlag(RefreshOptions.LocalInfo) == 0;
            // We set the timestamp before the actual operation because the results are retrieved synchronously, if we set the timestamp after
            // we will encounter an issue where the results are back but the UI still thinks the refresh is in progress
            // We also always update the timestamp even when the data has not changed to reflect the last time we do the Refresh operation
            SetRefreshTimestampSingleFlag(RefreshOptions.LocalInfo, DateTime.Now.Ticks);

            if (needRefresh)
                m_AssetStoreClient.RefreshLocal();
        }

        public void CancelRefresh(RefreshOptions options)
        {
            if (options.Contains(RefreshOptions.Purchased))
                m_AssetStoreClient.CancelListPurchases();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!loggedIn)
            {
                // We also want to clear the refresh time stamp here so that the next time users visit the Asset Store page,
                // we'll call refresh properly
                m_RefreshTimestamps.Remove(RefreshOptions.Purchased);
                m_RefreshErrors.Remove(RefreshOptions.Purchased);
            }
            else if (m_PageManager.activePage.refreshOptions.Contains(RefreshOptions.Purchased)  &&
                m_Application.isInternetReachable &&
                !m_Application.isCompiling &&
                !IsRefreshInProgress(RefreshOptions.Purchased))
                Refresh(RefreshOptions.Purchased);
        }

        public override void OnEnable()
        {
            m_AssetDatabase.onPostprocessAllAssets += OnPostprocessAllAssets;

            m_UpmClient.onListOperation += OnRefreshOperation;
            m_UpmClient.onSearchAllOperation += OnRefreshOperation;
            m_AssetStoreClient.onListOperation += OnRefreshOperation;

            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_PageManager.onActivePageChanged += OnActivePageChanged;
        }

        public override void OnDisable()
        {
            m_AssetDatabase.onPostprocessAllAssets -= OnPostprocessAllAssets;

            m_UpmClient.onListOperation -= OnRefreshOperation;
            m_UpmClient.onSearchAllOperation -= OnRefreshOperation;
            m_AssetStoreClient.onListOperation -= OnRefreshOperation;

            m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_PageManager.onActivePageChanged -= OnActivePageChanged;
        }

        private void OnRegistriesModified()
        {
            Refresh(RefreshOptions.UpmSearch);
        }

        private void OnRefreshOperation(IOperation operation)
        {
            m_RefreshOperationsInProgress.Add(operation);
            operation.onOperationSuccess += OnRefreshOperationSuccess;
            operation.onOperationFinalized += OnRefreshOperationFinalized;
            operation.onOperationError += OnRefreshOperationError;
            if (m_RefreshOperationsInProgress.Count > 1)
                return;
            onRefreshOperationStart?.Invoke();
        }

        private void OnRefreshOperationSuccess(IOperation operation)
        {
            SetRefreshTimestampSingleFlag(operation.refreshOptions, operation.timestamp);
            m_RefreshErrors.Remove(operation.refreshOptions);
        }

        private void OnRefreshOperationError(IOperation operation, UIError error)
        {
            SetRefreshErrorSingleFlag(operation.refreshOptions, error);
            onRefreshOperationError?.Invoke(error);
        }

        private void OnRefreshOperationFinalized(IOperation operation)
        {
            m_RefreshOperationsInProgress.Remove(operation);
            if (m_RefreshOperationsInProgress.Any())
                return;
            onRefreshOperationFinish?.Invoke();
        }

        // The virtual keyword is needed for unit tests
        public virtual bool IsRefreshInProgress(RefreshOptions options)
        {
            return m_RefreshOperationsInProgress.Any(i => options.Contains(i.refreshOptions));
        }

        // The virtual keyword is needed for unit tests
        public virtual bool IsInitialFetchingDone(RefreshOptions options)
        {
            return options.Split().All(o => GetRefreshTimestampSingleFlag(o) != 0 || m_RefreshErrors.ContainsKey(o));
        }

        public void SetRefreshTimestampSingleFlag(RefreshOptions option, long timestamp)
        {
            m_RefreshTimestamps[option] = timestamp;
        }

        // The virtual keyword is needed for unit tests
        public virtual long GetRefreshTimestamp(RefreshOptions options)
        {
            return options == RefreshOptions.None ? 0 : options.Split().Min(GetRefreshTimestampSingleFlag);
        }

        // This function only work with single flag (e.g. `RefreshOption.UpmList`) refresh option.
        // If a refresh option with multiple flags (e.g. `RefreshOption.UpmList | RefreshOption.UpmSearch`)
        // is passed, the result won't be correct.
        private long GetRefreshTimestampSingleFlag(RefreshOptions option)
        {
            return m_RefreshTimestamps.GetValueOrDefault(option, 0);
        }

        public void SetRefreshErrorSingleFlag(RefreshOptions option, UIError error)
        {
            m_RefreshErrors[option] = error;
        }

        // The virtual keyword is needed for unit tests
        public virtual UIError GetRefreshError(RefreshOptions options)
        {
            return options.Split()
                .Select(o => m_RefreshErrors.GetValueOrDefault(o))
                .FirstOrDefault(e => e != null);
        }

        public long GetRefreshTimestamp(IPage page)
        {
            return GetRefreshTimestamp(page.refreshOptions);
        }

        public UIError GetRefreshError(IPage page)
        {
            return GetRefreshError(page.refreshOptions);
        }

        public bool IsRefreshInProgress(IPage page)
        {
            return IsRefreshInProgress(page.refreshOptions);
        }

        // The virtual keyword is needed for unit tests
        public virtual bool IsInitialFetchingDone(IPage page)
        {
            return IsInitialFetchingDone(page.refreshOptions);
        }
    }
}
