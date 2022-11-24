// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PageRefreshHandler: ISerializationCallbackReceiver
    {
        private static readonly RefreshOptions[] k_RefreshOptionsByTab =
        {
            RefreshOptions.UpmList | RefreshOptions.UpmSearch,                  // PackageFilterTab.UnityRegistry
            RefreshOptions.UpmList,                                             // PackageFilterTab.InProject
            RefreshOptions.UpmListOffline | RefreshOptions.UpmSearchOffline,    // PackageFilterTab.BuiltIn
            RefreshOptions.Purchased | RefreshOptions.ImportedAssets,           // PackageFilterTab.AssetStore
            RefreshOptions.UpmList | RefreshOptions.UpmSearch,                  // PackageFilterTab.MyRegistries
        };

        public virtual event Action onRefreshOperationStart = delegate { };
        public virtual event Action onRefreshOperationFinish = delegate { };
        public virtual event Action<UIError> onRefreshOperationError = delegate { };

        private Dictionary<RefreshOptions, long> m_RefreshTimestamps = new Dictionary<RefreshOptions, long>();
        private Dictionary<RefreshOptions, UIError> m_RefreshErrors = new Dictionary<RefreshOptions, UIError>();

        [NonSerialized]
        private List<IOperation> m_RefreshOperationsInProgress = new List<IOperation>();

        // array created to help serialize dictionaries
        [SerializeField]
        private RefreshOptions[] m_SerializedRefreshTimestampsKeys = new RefreshOptions[0];

        [SerializeField]
        private long[] m_SerializedRefreshTimestampsValues = new long[0];

        [SerializeField]
        private RefreshOptions[] m_SerializedRefreshErrorsKeys = new RefreshOptions[0];

        [SerializeField]
        private UIError[] m_SerializedRefreshErrorsValues = new UIError[0];

        [NonSerialized]
        private ApplicationProxy m_Application;
        [NonSerialized]
        private UpmClient m_UpmClient;
        [NonSerialized]
        private UpmRegistryClient m_UpmRegistryClient;
        [NonSerialized]
        private PageManager m_PageManager;
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetDatabaseProxy m_AssetDatabase;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;

        public void ResolveDependencies(PageManager pageManager,
            ApplicationProxy application,
            UnityConnectProxy unityConnect,
            AssetDatabaseProxy assetDatabase,
            PackageManagerPrefs packageManagerPrefs,
            UpmClient upmClient,
            UpmRegistryClient upmRegistryClient,
            AssetStoreClientV2 assetStoreClient)
        {
            m_PageManager = pageManager;
            m_Application = application;
            m_UpmClient = upmClient;
            m_UpmRegistryClient = upmRegistryClient;
            m_UnityConnect = unityConnect;
            m_AssetDatabase = assetDatabase;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_AssetStoreClient = assetStoreClient;
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

        private void OnFilterChanged(PackageFilterTab filterTab)
        {
            if (!IsInitialFetchingDone(filterTab))
                Refresh(filterTab);
        }

        private static RefreshOptions GetRefreshOptionsByTab(PackageFilterTab tab)
        {
            var index = (int)tab;
            return index >= k_RefreshOptionsByTab.Length ? RefreshOptions.None : k_RefreshOptionsByTab[index];
        }

        public virtual void Refresh(PackageFilterTab? tab = null)
        {
            Refresh(GetRefreshOptionsByTab(tab ?? m_PackageManagerPrefs.currentFilterTab));
        }

        public virtual void Refresh(RefreshOptions options)
        {
            if ((options & RefreshOptions.UpmSearch) != 0)
            {
                m_UpmClient.SearchAll();
                // Since the SearchAll online call now might return error and an empty list, we want to trigger a `SearchOffline` call if
                // we detect that SearchOffline has not been called before. That way we will have some offline result to show to the user instead of nothing
                if (GetRefreshTimestampSingleFlag(RefreshOptions.UpmSearchOffline) == 0)
                    options |= RefreshOptions.UpmSearchOffline;
            }
            if ((options & RefreshOptions.UpmSearchOffline) != 0)
                m_UpmClient.SearchAll(true);
            if ((options & RefreshOptions.UpmList) != 0)
            {
                m_UpmClient.List();
                // Do the same logic for the List operations as the Search operations
                if (GetRefreshTimestampSingleFlag(RefreshOptions.UpmListOffline) == 0)
                    options |= RefreshOptions.UpmListOffline;
            }
            if ((options & RefreshOptions.UpmListOffline) != 0)
                m_UpmClient.List(true);
            if ((options & RefreshOptions.Purchased) != 0)
            {
                var page = m_PageManager.GetPage(PackageFilterTab.AssetStore);
                var numItems = Math.Max((int)page.visualStates.countLoaded, m_PackageManagerPrefs.numItemsPerPage ?? PackageManagerPrefs.k_DefaultPageSize);
                var queryArgs = new PurchasesQueryArgs(0, numItems, m_PackageManagerPrefs.trimmedSearchText, page.filters);
                m_AssetStoreClient.ListPurchases(queryArgs);
            }
            if ((options & RefreshOptions.PurchasedOffline) != 0)
                m_AssetStoreClient.RefreshLocal();
            if ((options & RefreshOptions.ImportedAssets) != 0)
                RefreshImportedAssets();
        }

        private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            RefreshImportedAssets(true);
        }

        private void RefreshImportedAssets(bool forceRefresh = false)
        {
            // When `OnPostprocessAllAssets` call `RefreshImportedAssets` because asset changes are detected, we set the
            // forceRefresh to true so we don't missed any potential modified asset
            // When `RefreshImportedAssets` is triggered by a user page refresh, we only want to actually call the refresh
            // if it was never called before, because we don't want to scan through Asset Database all the time and `OnPostprocessAllAssets`
            // will catch all the changes anyways.
            if (forceRefresh || GetRefreshTimestampSingleFlag(RefreshOptions.ImportedAssets) == 0)
            {
                m_AssetStoreClient.RefreshImportedAssets();
                m_RefreshTimestamps[RefreshOptions.ImportedAssets] = DateTime.Now.Ticks;
            }
        }

        public virtual void CancelRefresh(PackageFilterTab? tab = null)
        {
            CancelRefresh(GetRefreshOptionsByTab(tab ?? m_PackageManagerPrefs.currentFilterTab));
        }

        public virtual void CancelRefresh(RefreshOptions options)
        {
            if ((options & RefreshOptions.Purchased) != 0)
                m_AssetStoreClient.CancelListPurchases();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (!loggedIn)
            {
                // We also want to clear the refresh time stamp here so that the next time users visit the Asset Store page,
                // we'll call refresh properly
                m_RefreshTimestamps[RefreshOptions.Purchased] = 0;
                m_RefreshErrors.Remove(RefreshOptions.Purchased);
            }
            else if (m_PackageManagerPrefs.currentFilterTab == PackageFilterTab.AssetStore &&
                m_Application.isInternetReachable &&
                !EditorApplication.isCompiling &&
                !IsRefreshInProgress(RefreshOptions.Purchased))
                Refresh(RefreshOptions.Purchased);
        }

        public void OnEnable()
        {
            InitializeRefreshTimestamps();

            m_AssetDatabase.onPostprocessAllAssets += OnPostprocessAllAssets;

            m_UpmClient.onListOperation += OnRefreshOperation;
            m_UpmClient.onSearchAllOperation += OnRefreshOperation;
            m_AssetStoreClient.onListOperation += OnRefreshOperation;

            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_PackageManagerPrefs.onFilterTabChanged += OnFilterChanged;
        }

        public void OnDisable()
        {
            m_AssetDatabase.onPostprocessAllAssets -= OnPostprocessAllAssets;

            m_UpmClient.onListOperation -= OnRefreshOperation;
            m_UpmClient.onSearchAllOperation -= OnRefreshOperation;
            m_AssetStoreClient.onListOperation -= OnRefreshOperation;

            m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_PackageManagerPrefs.onFilterTabChanged -= OnFilterChanged;
        }

        // Gets called in Reload as well
        public void InitializeRefreshTimestamps()
        {
            foreach (RefreshOptions filter in Enum.GetValues(typeof(RefreshOptions)))
            {
                if (filter == RefreshOptions.None || m_RefreshTimestamps.ContainsKey(filter))
                    continue;
                m_RefreshTimestamps[filter] = 0;
            }
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
            m_RefreshTimestamps[operation.refreshOptions] = operation.timestamp;
            if (operation.refreshOptions == RefreshOptions.UpmSearch)
            {
                // when an online operation successfully returns with a timestamp newer than the offline timestamp, we update the offline timestamp as well
                // since we merge the online & offline result in the PackageDatabase and it's the newer ones that are being shown
                if (GetRefreshTimestampSingleFlag(RefreshOptions.UpmSearchOffline) < operation.timestamp)
                    m_RefreshTimestamps[RefreshOptions.UpmSearchOffline] = operation.timestamp;
            }
            else if (operation.refreshOptions == RefreshOptions.UpmList)
            {
                // Do the same logic for the List operations as the Search operations
                if (GetRefreshTimestampSingleFlag(RefreshOptions.UpmListOffline) < operation.timestamp)
                    m_RefreshTimestamps[RefreshOptions.UpmListOffline] = operation.timestamp;
            }
            if (m_RefreshErrors.ContainsKey(operation.refreshOptions))
                m_RefreshErrors.Remove(operation.refreshOptions);
        }

        private void OnRefreshOperationError(IOperation operation, UIError error)
        {
            m_RefreshErrors[operation.refreshOptions] = error;
            onRefreshOperationError?.Invoke(error);
        }

        private void OnRefreshOperationFinalized(IOperation operation)
        {
            m_RefreshOperationsInProgress.Remove(operation);
            if (m_RefreshOperationsInProgress.Any())
                return;
            onRefreshOperationFinish?.Invoke();
        }

        public virtual bool IsRefreshInProgress(RefreshOptions option)
        {
            return m_RefreshOperationsInProgress.Any(operation => (operation.refreshOptions & option) != 0);
        }

        public virtual bool IsInitialFetchingDone(RefreshOptions option)
        {
            foreach (var item in m_RefreshTimestamps)
            {
                if ((option & item.Key) == 0)
                    continue;
                if (item.Value == 0 && !m_RefreshErrors.ContainsKey(item.Key))
                    return false;
            }
            return true;
        }

        public virtual long GetRefreshTimestamp(RefreshOptions option)
        {
            var result = 0L;
            foreach (var item in m_RefreshTimestamps)
            {
                if ((option & item.Key) == 0)
                    continue;
                if (result == 0)
                    result = item.Value;
                else if (result > item.Value)
                    result = item.Value;
            }
            return result;
        }

        // This function only work with single flag (e.g. `RefreshOption.UpmList`) refresh option.
        // If a refresh option with multiple flags (e.g. `RefreshOption.UpmList | RefreshOption.UpmSearch`)
        // is passed, the result won't be correct.
        private long GetRefreshTimestampSingleFlag(RefreshOptions option)
        {
            return m_RefreshTimestamps.TryGetValue(option, out var value) ? value : 0;
        }

        public virtual UIError GetRefreshError(RefreshOptions option)
        {
            // only return the first one when there are multiple errors
            foreach (var item in m_RefreshErrors)
                if ((option & item.Key) != 0)
                    return item.Value;
            return null;
        }

        public virtual long GetRefreshTimestamp(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageManagerPrefs.currentFilterTab;
            return GetRefreshTimestamp(GetRefreshOptionsByTab(filterTab));
        }

        public virtual UIError GetRefreshError(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageManagerPrefs.currentFilterTab;
            return GetRefreshError(GetRefreshOptionsByTab(filterTab));
        }

        public virtual bool IsRefreshInProgress(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageManagerPrefs.currentFilterTab;
            return IsRefreshInProgress(GetRefreshOptionsByTab(filterTab));
        }

        public virtual bool IsInitialFetchingDone(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageManagerPrefs.currentFilterTab;
            return IsInitialFetchingDone(GetRefreshOptionsByTab(filterTab));
        }
    }
}
