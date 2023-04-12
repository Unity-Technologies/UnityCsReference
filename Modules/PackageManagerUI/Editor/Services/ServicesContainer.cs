// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    internal sealed class ServicesContainer : ScriptableSingleton<ServicesContainer>, ISerializationCallbackReceiver
    {
        // Some services are stateless, hence no serialization needed
        private HttpClientFactory m_HttpClientFactory;

        private UnityOAuthProxy m_UnityOAuthProxy;

        private SelectionProxy m_SelectionProxy;

        private AssetDatabaseProxy m_AssetDatabaseProxy;

        private EditorAnalyticsProxy m_EditorAnalyticsProxy;

        private IOProxy m_IOProxy;

        private DateTimeProxy m_DateTimeProxy;

        private PackageManagerProjectSettingsProxy m_SettingsProxy;

        private ClientProxy m_ClientProxy;

        [SerializeField]
        private ResourceLoader m_ResourceLoader;

        private ExtensionManager m_ExtensionManager;

        [SerializeField]
        private UnityConnectProxy m_UnityConnectProxy;

        [SerializeField]
        private ApplicationProxy m_ApplicationProxy;

        [SerializeField]
        private FetchStatusTracker m_FetchStatusTracker;

        [SerializeField]
        private UniqueIdMapper m_UniqueIdMapper;

        [SerializeField]
        private AssetStoreCache m_AssetStoreCache;

        [SerializeField]
        private AssetStoreClientV2 m_AssetStoreClient;

        [SerializeField]
        private BackgroundFetchHandler m_BackgroundFetchHandler;

        [SerializeField]
        private AssetStoreOAuth m_AssetStoreOAuth;

        private AssetStoreUtils m_AssetStoreUtils;

        private JsonParser m_JsonParser;

        private AssetStoreRestAPI m_AssetStoreRestAPI;

        private AssetStorePackageInstaller m_AssetStorePackageInstaller;

        private AssetSelectionHandler m_AssetSelectionHandler;

        private SelectionWindowProxy m_SelectionWindowProxy;

        [SerializeField]
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;

        [SerializeField]
        private AssetStoreCachePathProxy m_AssetStoreCachePathProxy;

        [SerializeField]
        private UpmCache m_UpmCache;

        [SerializeField]
        private UpmClient m_UpmClient;

        [SerializeField]
        private UpmRegistryClient m_UpmRegistryClient;

        [SerializeField]
        private UpmCacheRootClient m_UpmCacheRootClient;

        [SerializeField]
        private PackageManagerPrefs m_PackageManagerPrefs;

        [SerializeField]
        private PageManager m_PageManager;

        [SerializeField]
        private InspectorSelectionHandler m_InspectorSelectionHandler;

        [SerializeField]
        private PageRefreshHandler m_PageRefreshHandler;

        [SerializeField]
        private PackageDatabase m_PackageDatabase;

        private PackageOperationDispatcher m_OperationDispatcher;

        private AssetStorePackageFactory m_AssetStorePackageFactory;
        private UpmPackageFactory m_UpmPackageFactory;
        private UpmOnAssetStorePackageFactory m_UpmOnAssetStorePackageFactory;

        private OperationFactory m_OperationFactory;

        private Dictionary<Type, object> m_RegisteredObjects;

        [NonSerialized]
        private bool m_DependenciesResolved;

        private enum State
        {
            NotInitialized = 0,
            Initializing   = 1,
            Initialized    = 2
        }

        [NonSerialized]
        private State m_InitializeState;

        public ServicesContainer()
        {
            Reload();
        }

        public void Reload()
        {
            // In the constructor we only need to worry about creating a brand new instance.
            // In the case of assembly reload, a deserialize step will automatically happen after the constructor
            // to restore all the serializable states/services and we don't need to worry about that
            m_HttpClientFactory = new HttpClientFactory();
            m_UnityOAuthProxy = new UnityOAuthProxy();
            m_SelectionProxy = new SelectionProxy();
            m_AssetDatabaseProxy = new AssetDatabaseProxy();
            m_UnityConnectProxy = new UnityConnectProxy();
            m_ApplicationProxy = new ApplicationProxy();
            m_EditorAnalyticsProxy = new EditorAnalyticsProxy();
            m_IOProxy = new IOProxy();
            m_DateTimeProxy = new DateTimeProxy();
            m_SettingsProxy = new PackageManagerProjectSettingsProxy();
            m_ClientProxy = new ClientProxy();
            m_SelectionWindowProxy = new SelectionWindowProxy();

            if (m_ResourceLoader != null)
                m_ResourceLoader.Reset();
            m_ResourceLoader = new ResourceLoader();
            m_ExtensionManager = new ExtensionManager();

            m_FetchStatusTracker = new FetchStatusTracker();
            m_UniqueIdMapper = new UniqueIdMapper();

            m_AssetStoreCache = new AssetStoreCache();
            m_AssetStoreClient = new AssetStoreClientV2();
            m_AssetStoreOAuth = new AssetStoreOAuth();
            m_AssetStoreUtils = new AssetStoreUtils();
            m_JsonParser = new JsonParser();
            m_AssetStoreRestAPI = new AssetStoreRestAPI();
            m_AssetStorePackageInstaller = new AssetStorePackageInstaller();
            m_AssetStoreDownloadManager = new AssetStoreDownloadManager();
            m_BackgroundFetchHandler = new BackgroundFetchHandler();
            m_AssetStoreCachePathProxy = new AssetStoreCachePathProxy();
            m_AssetSelectionHandler = new AssetSelectionHandler();

            m_UpmCache = new UpmCache();
            m_UpmClient = new UpmClient();
            m_UpmRegistryClient = new UpmRegistryClient();
            m_UpmCacheRootClient = new UpmCacheRootClient();

            m_PackageManagerPrefs = new PackageManagerPrefs();

            m_PackageDatabase = new PackageDatabase();
            m_OperationDispatcher = new PackageOperationDispatcher();
            m_PageManager = new PageManager();
            m_InspectorSelectionHandler = new InspectorSelectionHandler();
            m_PageRefreshHandler = new PageRefreshHandler();

            m_AssetStorePackageFactory = new AssetStorePackageFactory();
            m_UpmPackageFactory = new UpmPackageFactory();
            m_UpmOnAssetStorePackageFactory = new UpmOnAssetStorePackageFactory();

            m_OperationFactory = new OperationFactory();

            // Since dictionaries doesn't survive through serialization, we always re-create the default registration
            m_RegisteredObjects = new Dictionary<Type, object>();
            RegisterDefaultServices();

            m_DependenciesResolved = false;
            m_InitializeState = State.NotInitialized;
        }

        private void ResolveDependencies()
        {
            if (m_DependenciesResolved)
                return;

            m_ResourceLoader.ResolveDependencies(m_ApplicationProxy);

            m_AssetStoreCache.ResolveDependencies(m_ApplicationProxy, m_HttpClientFactory, m_IOProxy, m_UniqueIdMapper);
            m_AssetStoreClient.ResolveDependencies(m_UnityConnectProxy, m_AssetStoreCache, m_AssetStoreUtils, m_AssetStoreRestAPI, m_FetchStatusTracker, m_AssetDatabaseProxy, m_OperationFactory);
            m_AssetStoreOAuth.ResolveDependencies(m_DateTimeProxy, m_UnityConnectProxy, m_UnityOAuthProxy, m_HttpClientFactory);
            m_AssetStoreRestAPI.ResolveDependencies(m_UnityConnectProxy, m_AssetStoreOAuth, m_JsonParser, m_HttpClientFactory);
            m_AssetStorePackageInstaller.ResolveDependencies(m_IOProxy, m_AssetStoreCache, m_AssetDatabaseProxy, m_AssetSelectionHandler, m_ApplicationProxy);
            m_AssetStoreDownloadManager.ResolveDependencies(m_ApplicationProxy, m_HttpClientFactory, m_UnityConnectProxy, m_IOProxy, m_AssetStoreCache, m_AssetStoreUtils, m_AssetStoreRestAPI, m_AssetStoreCachePathProxy);
            m_BackgroundFetchHandler.ResolveDependencies(m_ApplicationProxy, m_UnityConnectProxy, m_UpmCache, m_UpmClient, m_AssetStoreClient, m_AssetStoreCache, m_FetchStatusTracker, m_PageManager, m_PageRefreshHandler);
            m_AssetSelectionHandler.ResolveDependencies(m_SelectionWindowProxy);

            m_UpmCache.ResolveDependencies(m_UniqueIdMapper);
            m_UpmClient.ResolveDependencies(m_UpmCache, m_FetchStatusTracker, m_IOProxy, m_SettingsProxy, m_ClientProxy, m_ApplicationProxy);
            m_UpmRegistryClient.ResolveDependencies(m_UpmCache, m_SettingsProxy, m_ClientProxy, m_ApplicationProxy);
            m_UpmCacheRootClient.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);

            m_PackageDatabase.ResolveDependencies(m_UniqueIdMapper, m_AssetDatabaseProxy, m_UpmCache, m_IOProxy);
            m_OperationDispatcher.ResolveDependencies(m_AssetStorePackageInstaller, m_AssetStoreDownloadManager, m_UpmClient, m_IOProxy);
            m_InspectorSelectionHandler.ResolveDependencies(m_SelectionProxy, m_PackageDatabase, m_PageManager);
            m_PageManager.ResolveDependencies(m_UnityConnectProxy, m_PackageManagerPrefs, m_AssetStoreClient, m_PackageDatabase, m_UpmCache, m_SettingsProxy, m_UpmRegistryClient);
            m_PageRefreshHandler.ResolveDependencies(m_PageManager, m_ApplicationProxy, m_UnityConnectProxy, m_AssetDatabaseProxy, m_PackageManagerPrefs, m_UpmClient, m_UpmRegistryClient, m_AssetStoreClient);

            m_AssetStorePackageFactory.ResolveDependencies(m_UniqueIdMapper, m_UnityConnectProxy, m_AssetStoreCache, m_AssetStoreClient, m_AssetStoreDownloadManager, m_PackageDatabase, m_FetchStatusTracker, m_IOProxy, m_BackgroundFetchHandler);
            m_UpmPackageFactory.ResolveDependencies(m_UniqueIdMapper, m_UpmCache, m_UpmClient, m_BackgroundFetchHandler, m_PackageDatabase, m_SettingsProxy);
            m_UpmOnAssetStorePackageFactory.ResolveDependencies(m_UniqueIdMapper, m_UnityConnectProxy, m_AssetStoreCache, m_BackgroundFetchHandler, m_PackageDatabase, m_FetchStatusTracker, m_UpmCache, m_UpmClient);

            m_OperationFactory.ResolveDependencies(m_UnityConnectProxy, m_AssetStoreRestAPI, m_AssetStoreCache);

            m_ExtensionManager.ResolveDependencies(m_PackageManagerPrefs);

            m_DependenciesResolved = true;
        }

        private void Initialize()
        {
            if (m_InitializeState == State.Initialized)
                return;

            if (m_InitializeState == State.Initializing)
            {
                Debug.LogWarning("[Package Manager Window] Nested ServicesContainer initialization detected.");
                return;
            }
            m_InitializeState = State.Initializing;

            ResolveDependencies();

            // Some services has dependencies that requires some initialization in `OnEnable`.
            m_UnityConnectProxy.OnEnable();
            m_ApplicationProxy.OnEnable();
            m_SettingsProxy.OnEnable();
            m_SelectionWindowProxy.OnEnable();

            m_AssetStoreOAuth.OnEnable();
            m_AssetStoreDownloadManager.OnEnable();
            m_BackgroundFetchHandler.OnEnable();

            m_UpmClient.OnEnable();

            m_InspectorSelectionHandler.OnEnable();
            m_PageManager.OnEnable();
            m_PageRefreshHandler.OnEnable();

            m_AssetStorePackageFactory.OnEnable();
            m_UpmPackageFactory.OnEnable();
            m_UpmOnAssetStorePackageFactory.OnEnable();

            m_AssetStorePackageInstaller.OnEnable();
            m_AssetSelectionHandler.OnEnable();

            m_InitializeState = State.Initialized;
        }

        public void OnDisable()
        {
            m_UnityConnectProxy.OnDisable();
            m_ApplicationProxy.OnDisable();
            m_SettingsProxy.OnDisable();
            m_SelectionWindowProxy.OnDisable();

            m_AssetStoreOAuth.OnDisable();
            m_AssetStoreDownloadManager.OnDisable();
            m_BackgroundFetchHandler.OnDisable();

            m_UpmClient.OnDisable();

            m_InspectorSelectionHandler.OnDisable();
            m_PageManager.OnDisable();
            m_PageRefreshHandler.OnDisable();

            m_AssetStorePackageFactory.OnDisable();
            m_UpmPackageFactory.OnDisable();
            m_UpmOnAssetStorePackageFactory.OnDisable();

            m_AssetStorePackageInstaller.OnDisable();
            m_AssetSelectionHandler.OnDisable();
        }

        public void RegisterDefaultServices()
        {
            Register(m_HttpClientFactory);
            Register(m_UnityOAuthProxy);
            Register(m_UnityConnectProxy);
            Register(m_SelectionProxy);
            Register(m_ApplicationProxy);
            Register(m_AssetDatabaseProxy);
            Register(m_ResourceLoader);
            Register(m_ExtensionManager);
            Register(m_EditorAnalyticsProxy);
            Register(m_IOProxy);
            Register(m_DateTimeProxy);
            Register(m_SettingsProxy);
            Register(m_ClientProxy);
            Register(m_SelectionWindowProxy);

            Register(m_FetchStatusTracker);
            Register(m_UniqueIdMapper);
            Register(m_AssetStoreCache);
            Register(m_AssetStoreClient);
            Register(m_AssetStoreOAuth);
            Register(m_AssetStoreUtils);
            Register(m_JsonParser);
            Register(m_AssetStoreRestAPI);
            Register(m_BackgroundFetchHandler);
            Register(m_AssetStoreCachePathProxy);
            Register(m_AssetSelectionHandler);

            Register(m_UpmCache);
            Register(m_UpmClient);
            Register(m_UpmRegistryClient);
            Register(m_UpmCacheRootClient);

            Register(m_PackageManagerPrefs);

            Register(m_InspectorSelectionHandler);
            Register(m_PageManager);
            Register(m_PackageDatabase);
            Register(m_PageRefreshHandler);
            Register(m_OperationDispatcher);

            Register(m_AssetStorePackageFactory);
            Register(m_UpmPackageFactory);
            Register(m_UpmOnAssetStorePackageFactory);

            Register(m_AssetStorePackageInstaller);
            Register(m_AssetStoreDownloadManager);
        }

        public void Register<T>(T obj) where T : class
        {
            m_RegisteredObjects[typeof(T)] = obj;
        }

        public T Resolve<T>() where T : class
        {
            Initialize();
            return m_RegisteredObjects.TryGetValue(typeof(T), out var result) ? result as T : null;
        }

        public void OnBeforeSerialize()
        {
            // No special handling needed
        }

        public void OnAfterDeserialize()
        {
            ResolveDependencies();
        }
    }
}
