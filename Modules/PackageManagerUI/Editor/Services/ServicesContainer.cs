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
        private readonly HttpClientFactory m_HttpClientFactory;

        private readonly UnityOAuthProxy m_UnityOAuthProxy;

        private readonly SelectionProxy m_SelectionProxy;

        private readonly AssetDatabaseProxy m_AssetDatabaseProxy;

        private readonly EditorAnalyticsProxy m_EditorAnalyticsProxy;

        private readonly IOProxy m_IOProxy;

        private readonly DateTimeProxy m_DateTimeProxy;

        private readonly PackageManagerProjectSettingsProxy m_SettingsProxy;

        private readonly ClientProxy m_ClientProxy;

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
        private AssetStoreCache m_AssetStoreCache;

        [SerializeField]
        private AssetStoreClient m_AssetStoreClient;

        [SerializeField]
        private AssetStoreCallQueue m_AssetStoreCallQueue;

        [SerializeField]
        private AssetStoreOAuth m_AssetStoreOAuth;

        private readonly AssetStoreUtils m_AssetStoreUtils;
        private readonly AssetStoreRestAPI m_AssetStoreRestAPI;

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
        private PackageFiltering m_PackageFiltering;

        [SerializeField]
        private PackageManagerPrefs m_PackageManagerPrefs;

        [SerializeField]
        private PageManager m_PageManager;

        [SerializeField]
        private PackageDatabase m_PackageDatabase;

        private readonly Dictionary<Type, object> m_RegisteredObjects;

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

            if (m_ResourceLoader != null)
                m_ResourceLoader.Reset();
            m_ResourceLoader = new ResourceLoader();
            m_ExtensionManager = new ExtensionManager();

            m_FetchStatusTracker = new FetchStatusTracker();
            m_AssetStoreCache = new AssetStoreCache();
            m_AssetStoreClient = new AssetStoreClient();
            m_AssetStoreOAuth = new AssetStoreOAuth();
            m_AssetStoreUtils = new AssetStoreUtils();
            m_AssetStoreRestAPI = new AssetStoreRestAPI();
            m_AssetStoreDownloadManager = new AssetStoreDownloadManager();
            m_AssetStoreCallQueue = new AssetStoreCallQueue();
            m_AssetStoreCachePathProxy = new AssetStoreCachePathProxy();

            m_UpmCache = new UpmCache();
            m_UpmClient = new UpmClient();
            m_UpmRegistryClient = new UpmRegistryClient();
            m_UpmCacheRootClient = new UpmCacheRootClient();

            m_PackageFiltering = new PackageFiltering();
            m_PackageManagerPrefs = new PackageManagerPrefs();

            m_PackageDatabase = new PackageDatabase();
            m_PageManager = new PageManager();

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

            m_AssetStoreCache.ResolveDependencies(m_ApplicationProxy, m_AssetStoreUtils, m_HttpClientFactory, m_IOProxy);
            m_AssetStoreClient.ResolveDependencies(m_UnityConnectProxy, m_AssetStoreCache, m_AssetStoreUtils, m_AssetStoreRestAPI, m_FetchStatusTracker, m_UpmCache, m_UpmClient, m_UpmRegistryClient, m_IOProxy, m_SettingsProxy);
            m_AssetStoreOAuth.ResolveDependencies(m_DateTimeProxy, m_UnityConnectProxy, m_UnityOAuthProxy, m_HttpClientFactory);
            m_AssetStoreUtils.ResolveDependencies(m_UnityConnectProxy);
            m_AssetStoreRestAPI.ResolveDependencies(m_UnityConnectProxy, m_AssetStoreOAuth, m_AssetStoreCache, m_HttpClientFactory);
            m_AssetStoreDownloadManager.ResolveDependencies(m_ApplicationProxy, m_HttpClientFactory, m_UnityConnectProxy, m_IOProxy, m_AssetStoreCache, m_AssetStoreUtils, m_AssetStoreRestAPI, m_AssetStoreCachePathProxy);
            m_AssetStoreCallQueue.ResolveDependencies(m_ApplicationProxy, m_UnityConnectProxy, m_PackageFiltering, m_UpmCache, m_AssetStoreClient, m_AssetStoreCache, m_PageManager);

            m_UpmClient.ResolveDependencies(m_UpmCache, m_FetchStatusTracker, m_IOProxy, m_SettingsProxy, m_ClientProxy, m_ApplicationProxy);
            m_UpmRegistryClient.ResolveDependencies(m_UpmCache, m_SettingsProxy, m_ClientProxy, m_ApplicationProxy);
            m_UpmCacheRootClient.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);

            m_PackageFiltering.ResolveDependencies(m_UnityConnectProxy, m_SettingsProxy);

            m_PackageDatabase.ResolveDependencies(m_UnityConnectProxy, m_AssetDatabaseProxy, m_AssetStoreUtils, m_AssetStoreClient, m_AssetStoreDownloadManager, m_UpmCache, m_UpmClient, m_IOProxy);
            m_PageManager.ResolveDependencies(m_ApplicationProxy, m_SelectionProxy, m_UnityConnectProxy, m_PackageFiltering, m_PackageManagerPrefs, m_UpmCache, m_UpmClient, m_UpmRegistryClient, m_AssetStoreClient, m_PackageDatabase, m_SettingsProxy);

            m_ApplicationProxy.ResolveDependencies(m_AssetDatabaseProxy);
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

            m_AssetStoreClient.OnEnable();
            m_AssetStoreOAuth.OnEnable();
            m_AssetStoreDownloadManager.OnEnable();
            m_AssetStoreCallQueue.OnEnable();

            m_UpmClient.OnEnable();

            m_PackageDatabase.OnEnable();
            m_PageManager.OnEnable();

            m_InitializeState = State.Initialized;
        }

        void OnDisable()
        {
            m_UnityConnectProxy.OnDisable();
            m_ApplicationProxy.OnDisable();
            m_SettingsProxy.OnDisable();

            m_AssetStoreClient.OnDisable();
            m_AssetStoreOAuth.OnDisable();
            m_AssetStoreDownloadManager.OnDisable();
            m_AssetStoreCallQueue.OnDisable();

            m_UpmClient.OnDisable();

            m_PackageDatabase.OnDisable();
            m_PageManager.OnDisable();
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

            Register(m_FetchStatusTracker);
            Register(m_AssetStoreCache);
            Register(m_AssetStoreClient);
            Register(m_AssetStoreOAuth);
            Register(m_AssetStoreUtils);
            Register(m_AssetStoreRestAPI);
            Register(m_AssetStoreCallQueue);
            Register(m_AssetStoreCachePathProxy);

            Register(m_UpmCache);
            Register(m_UpmClient);
            Register(m_UpmRegistryClient);
            Register(m_UpmCacheRootClient);

            Register(m_PackageFiltering);
            Register(m_PackageManagerPrefs);

            Register(m_PageManager);
            Register(m_PackageDatabase);

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
