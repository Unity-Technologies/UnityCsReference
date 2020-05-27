// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal sealed class ServicesContainer : ScriptableSingleton<ServicesContainer>, ISerializationCallbackReceiver
    {
        // Some services are stateless, hence no serialization needed
        private HttpClientFactory m_HttpClientFactory;

        private UnityOAuthProxy m_UnityOAuthProxy;

        private SelectionProxy m_SelectionProxy;

        private AssetDatabaseProxy m_AssetDatabaseProxy;

        private IOProxy m_IOProxy;

        private PackageManagerProjectSettingsProxy m_SettingsProxy;

        private ResourceLoader m_ResourceLoader;

        [SerializeField]
        private UnityConnectProxy m_UnityConnectProxy;

        [SerializeField]
        private ApplicationProxy m_ApplicationProxy;

        [SerializeField]
        private AssetStoreCache m_AssetStoreCache;

        [SerializeField]
        private AssetStoreClient m_AssetStoreClient;

        [SerializeField]
        private AssetStoreOAuth m_AssetStoreOAuth;

        private AssetStoreUtils m_AssetStoreUtils;
        private AssetStoreRestAPI m_AssetStoreRestAPI;

        [SerializeField]
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;

        [SerializeField]
        private UpmCache m_UpmCache;

        [SerializeField]
        private UpmClient m_UpmClient;

        [SerializeField]
        private PackageFiltering m_PackageFiltering;

        [SerializeField]
        private PackageManagerPrefs m_PackageManagerPrefs;

        [SerializeField]
        private PageManager m_PageManager;

        [SerializeField]
        private PackageDatabase m_PackageDatabase;


        private Dictionary<Type, object> m_RegisteredObjects;

        [NonSerialized]
        private bool m_DependenciesResolved;

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
            m_IOProxy = new IOProxy();
            m_SettingsProxy = new PackageManagerProjectSettingsProxy();

            m_ResourceLoader = new ResourceLoader();

            m_AssetStoreCache = new AssetStoreCache();
            m_AssetStoreClient = new AssetStoreClient();
            m_AssetStoreOAuth = new AssetStoreOAuth();
            m_AssetStoreUtils = new AssetStoreUtils();
            m_AssetStoreRestAPI = new AssetStoreRestAPI();
            m_AssetStoreDownloadManager = new AssetStoreDownloadManager();

            m_UpmCache = new UpmCache();
            m_UpmClient = new UpmClient();

            m_PackageFiltering = new PackageFiltering();
            m_PackageManagerPrefs = new PackageManagerPrefs();

            m_PackageDatabase = new PackageDatabase();
            m_PageManager = new PageManager();

            // Since dictionaries doesn't survive through serialization, we always re-create the default registration
            m_RegisteredObjects = new Dictionary<Type, object>();
            RegisterDefaultServices();

            m_DependenciesResolved = false;
        }

        private void ResolveDependencies()
        {
            if (m_DependenciesResolved)
                return;

            m_AssetStoreCache.ResolveDependencies(m_ApplicationProxy, m_AssetStoreUtils, m_HttpClientFactory, m_IOProxy);
            m_AssetStoreClient.ResolveDependencies(m_UnityConnectProxy, m_AssetStoreCache, m_AssetStoreUtils, m_AssetStoreRestAPI, m_UpmClient, m_IOProxy);
            m_AssetStoreOAuth.ResolveDependencies(m_UnityConnectProxy, m_UnityOAuthProxy, m_AssetStoreUtils, m_HttpClientFactory);
            m_AssetStoreUtils.ResolveDependencies(m_UnityConnectProxy);
            m_AssetStoreRestAPI.ResolveDependencies(m_UnityConnectProxy, m_AssetStoreOAuth, m_AssetStoreCache, m_HttpClientFactory);
            m_AssetStoreDownloadManager.ResolveDependencies(m_ApplicationProxy, m_HttpClientFactory, m_UnityConnectProxy, m_AssetStoreCache, m_AssetStoreUtils, m_AssetStoreRestAPI);

            m_UpmCache.ResolveDependencies(m_PackageManagerPrefs);
            m_UpmClient.ResolveDependencies(m_PackageManagerPrefs, m_UpmCache, m_IOProxy, m_SettingsProxy);

            m_PackageFiltering.ResolveDependencies(m_UnityConnectProxy, m_PackageManagerPrefs);

            m_PackageDatabase.ResolveDependencies(m_UnityConnectProxy, m_AssetDatabaseProxy, m_AssetStoreUtils, m_AssetStoreClient, m_AssetStoreDownloadManager, m_UpmClient, m_IOProxy);
            m_PageManager.ResolveDependencies(m_ApplicationProxy, m_SelectionProxy, m_UnityConnectProxy, m_PackageFiltering, m_PackageManagerPrefs, m_UpmClient, m_AssetStoreClient, m_PackageDatabase, m_SettingsProxy);

            m_DependenciesResolved = true;
        }

        void OnEnable()
        {
            ResolveDependencies();

            // Some services has dependencies that requires some initialization in `OnEnable`.
            m_UnityConnectProxy.OnEnable();
            m_ApplicationProxy.OnEnable();
            m_SettingsProxy.OnEnable();

            m_AssetStoreClient.OnEnable();
            m_AssetStoreOAuth.OnEnable();
            m_AssetStoreDownloadManager.OnEnable();

            m_UpmClient.OnEnable();

            m_PackageDatabase.OnEnable();
            m_PageManager.OnEnable();
        }

        void OnDisable()
        {
            m_UnityConnectProxy.OnDisable();
            m_ApplicationProxy.OnDisable();
            m_SettingsProxy.OnDisable();

            m_AssetStoreClient.OnDisable();
            m_AssetStoreOAuth.OnDisable();
            m_AssetStoreDownloadManager.OnDisable();

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
            Register(m_IOProxy);
            Register(m_SettingsProxy);

            Register(m_AssetStoreCache);
            Register(m_AssetStoreClient);
            Register(m_AssetStoreOAuth);
            Register(m_AssetStoreUtils);
            Register(m_AssetStoreRestAPI);

            Register(m_UpmCache);
            Register(m_UpmClient);

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
            object result;
            return m_RegisteredObjects.TryGetValue(typeof(T), out result) ? result as T : null;
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
