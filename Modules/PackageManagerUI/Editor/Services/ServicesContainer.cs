// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IService
    {
        IReadOnlyCollection<IService> dependencies { get; }
        bool enabled { get; set; }
        Type registrationType { get; }
    }

    internal abstract class BaseService : IService
    {
        private readonly List<IService> m_Dependencies = new();
        public IReadOnlyCollection<IService> dependencies => m_Dependencies;
        public abstract Type registrationType { get; }

        protected T RegisterDependency<T>(T dependency) where T : class, IService
        {
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));

            m_Dependencies.Add(dependency);
            return dependency;
        }

        private bool m_Enabled;

        public bool enabled
        {
            get => m_Enabled;
            set
            {
                if (m_Enabled == value)
                    return;
                if (value)
                    OnEnable();
                else
                    OnDisable();
                m_Enabled = value;
            }
        }

        public virtual void OnEnable() {}

        public virtual void OnDisable() {}
    }

    internal abstract class BaseService<T> : BaseService where T : IService
    {
        public override Type registrationType => typeof(T);
    }

    [Serializable]
    [ExcludeFromCodeCoverage]
    internal sealed class ServicesContainer : ScriptableSingleton<ServicesContainer>
    {
        [SerializeField]
        private UnityConnectProxy m_SerializedUnityConnectProxy;
        [SerializeField]
        private ApplicationProxy m_SerializedApplicationProxy;
        [SerializeField]
        private ResourceLoader m_SerializedResourceLoader;
        [SerializeField]
        private FetchStatusTracker m_SerializedFetchStatusTracker;
        [SerializeField]
        private UniqueIdMapper m_SerializedUniqueIdMapper;
        [SerializeField]
        private AssetStoreCache m_SerializedAssetStoreCache;
        [SerializeField]
        private AssetStoreClientV2 m_SerializedAssetStoreClient;
        [SerializeField]
        private BackgroundFetchHandler m_SerializedBackgroundFetchHandler;
        [SerializeField]
        private AssetStoreOAuth m_SerializedAssetStoreOAuth;
        [SerializeField]
        private AssetStoreDownloadManager m_SerializedAssetStoreDownloadManager;
        [SerializeField]
        private AssetStoreCachePathProxy m_SerializedAssetStoreCachePathProxy;
        [SerializeField]
        private UpmCache m_SerializedUpmCache;
        [SerializeField]
        private UpmClient m_SerializedUpmClient;
        [SerializeField]
        private UpmRegistryClient m_SerializedUpmRegistryClient;
        [SerializeField]
        private UpmCacheRootClient m_SerializedUpmCacheRootClient;
        [SerializeField]
        private PackageManagerPrefs m_SerializedPackageManagerPrefs;
        [SerializeField]
        private PageManager m_SerializedPageManager;
        [SerializeField]
        private InspectorSelectionHandler m_SerializedInspectorSelectionHandler;
        [SerializeField]
        private PageRefreshHandler m_SerializedPageRefreshHandler;
        [SerializeField]
        private PackageDatabase m_SerializedPackageDatabase;
        [SerializeField]
        private DelayedSelectionHandler m_DelayedSelectionHandler;

        private readonly Dictionary<Type, IService> m_RegisteredServices = new();

        private readonly Dictionary<IService, HashSet<IService>> m_ReverseDependencies = new();

        public ServicesContainer()
        {
            Reload();
        }

        public void Reload()
        {
            m_RegisteredServices.Clear();
            m_ReverseDependencies.Clear();

            var httpClientFactory = Register(new HttpClientFactory());
            var unityOAuthProxy = Register(new UnityOAuthProxy());
            var selectionProxy = Register(new SelectionProxy());
            var assetDatabaseProxy = Register(new AssetDatabaseProxy());
            var ioProxy = Register(new IOProxy());
            var dateTimeProxy = Register(new DateTimeProxy());
            var settingsProxy = Register(new PackageManagerProjectSettingsProxy());
            var clientProxy = Register(new ClientProxy());
            var selectionWindowProxy = Register(new SelectionWindowProxy());
            var assetStoreUtils = Register(new AssetStoreUtils());
            var jsonParser = Register(new JsonParser());
            var unityConnectProxy = Register(new UnityConnectProxy());
            var applicationProxy = Register(new ApplicationProxy());
            var packageManagerPrefs = Register(new PackageManagerPrefs());
            var fetchStatusTracker = Register(new FetchStatusTracker());
            var uniqueIdMapper = Register(new UniqueIdMapper());
            var assetStoreCachePathProxy = Register(new AssetStoreCachePathProxy());
            var resourceLoader = Register(new ResourceLoader());

            var assetSelectionHandler = Register(new AssetSelectionHandler(selectionWindowProxy));
            var assetStoreOAuth = Register(new AssetStoreOAuth(dateTimeProxy, unityConnectProxy, unityOAuthProxy, httpClientFactory));
            var assetStoreRestAPI = Register(new AssetStoreRestAPI(unityConnectProxy, assetStoreOAuth, jsonParser, httpClientFactory));
            var localInfoHandler = Register(new LocalInfoHandler(assetStoreUtils, ioProxy));
            var assetStoreCache = Register(new AssetStoreCache(applicationProxy, httpClientFactory, ioProxy));
            var operationFactory = Register(new OperationFactory(unityConnectProxy, assetStoreRestAPI, assetStoreCache));
            var assetStoreClient = Register(new AssetStoreClientV2(assetStoreCache, assetStoreRestAPI, fetchStatusTracker, assetDatabaseProxy, operationFactory, localInfoHandler));
            var assetStorePackageInstaller = Register(new AssetStorePackageInstaller(ioProxy, assetStoreCache, assetDatabaseProxy, assetSelectionHandler, applicationProxy));
            var assetStoreDownloadManager = Register(new AssetStoreDownloadManager(applicationProxy, unityConnectProxy, ioProxy, assetStoreCache, assetStoreUtils, assetStoreRestAPI, assetStoreCachePathProxy, localInfoHandler));

            var upmCache = Register(new UpmCache(uniqueIdMapper));
            var upmClient = Register(new UpmClient(upmCache, fetchStatusTracker, ioProxy, clientProxy, applicationProxy));
            var upmRegistryClient = Register(new UpmRegistryClient(upmCache, settingsProxy, clientProxy, applicationProxy));

            var packageDatabase = Register(new PackageDatabase(uniqueIdMapper, assetDatabaseProxy, upmCache, ioProxy));
            var pageFactory = Register(new PageFactory(unityConnectProxy, packageManagerPrefs, assetStoreClient, packageDatabase, upmCache));
            var pageManager = Register(new PageManager(unityConnectProxy, packageDatabase, settingsProxy, upmRegistryClient, pageFactory));
            var pageRefreshHandler = Register(new PageRefreshHandler(pageManager, applicationProxy, unityConnectProxy, assetDatabaseProxy, packageManagerPrefs, upmClient, upmRegistryClient, assetStoreClient));
            var backgroundFetchHandler = Register(new BackgroundFetchHandler(applicationProxy, unityConnectProxy, upmCache, upmClient, assetStoreClient, assetStoreCache, fetchStatusTracker, pageManager, pageRefreshHandler));
            var upmCacheRootClient = Register(new UpmCacheRootClient(clientProxy, applicationProxy));
            var inspectorSelectionHandler = Register(new InspectorSelectionHandler(selectionProxy, packageDatabase, pageManager));
            var delayedSelectionHandler = Register(new DelayedSelectionHandler(packageDatabase, pageManager, pageRefreshHandler, upmCache, settingsProxy));

            Register(new EditorAnalyticsProxy());
            Register(new ExtensionManager(packageManagerPrefs));
            Register(new PackageOperationDispatcher(assetStorePackageInstaller, assetStoreDownloadManager, upmClient));
            Register(new AssetStorePackageFactory(upmCache, unityConnectProxy, assetStoreCache, assetStoreDownloadManager, packageDatabase, fetchStatusTracker, backgroundFetchHandler));
            Register(new UpmPackageFactory(upmCache, upmClient, backgroundFetchHandler, packageDatabase, settingsProxy));
            Register(new UpmOnAssetStorePackageFactory(unityConnectProxy, assetStoreCache, backgroundFetchHandler, packageDatabase, fetchStatusTracker, upmCache, upmRegistryClient, settingsProxy));
            Register(new PackageLinkFactory(upmCache, assetStoreCache, applicationProxy, ioProxy));

            // We need to save some services as serialized members for them to survive domain reload properly
            m_SerializedUnityConnectProxy = unityConnectProxy;
            m_SerializedApplicationProxy = applicationProxy;
            m_SerializedFetchStatusTracker = fetchStatusTracker;
            m_SerializedUniqueIdMapper = uniqueIdMapper;
            m_SerializedAssetStoreCache = assetStoreCache;
            m_SerializedAssetStoreClient = assetStoreClient;
            m_SerializedBackgroundFetchHandler = backgroundFetchHandler;
            m_SerializedAssetStoreOAuth = assetStoreOAuth;
            m_SerializedAssetStoreDownloadManager = assetStoreDownloadManager;
            m_SerializedAssetStoreCachePathProxy = assetStoreCachePathProxy;
            m_SerializedUpmCache = upmCache;
            m_SerializedUpmClient = upmClient;
            m_SerializedUpmRegistryClient = upmRegistryClient;
            m_SerializedUpmCacheRootClient = upmCacheRootClient;
            m_SerializedPackageManagerPrefs = packageManagerPrefs;
            m_SerializedPageManager = pageManager;
            m_SerializedInspectorSelectionHandler = inspectorSelectionHandler;
            m_SerializedPageRefreshHandler = pageRefreshHandler;
            m_SerializedPackageDatabase = packageDatabase;
            // A reset is needed to avoid creating new stylesheets each time we reload through the internal menu
            m_SerializedResourceLoader?.Reset();
            m_SerializedResourceLoader = resourceLoader;
            m_DelayedSelectionHandler = delayedSelectionHandler;
        }

        public void OnDisable()
        {
            foreach (var service in m_RegisteredServices.Values)
                service.enabled = false;
        }

        private void RegisterReverseDependencies(IService service)
        {
            foreach (var dependency in service?.dependencies ?? Array.Empty<IService>())
            {
                if (m_ReverseDependencies.TryGetValue(dependency, out var result))
                    result.Add(service);
                else
                    m_ReverseDependencies[dependency] = new HashSet<IService> { service };
            }
        }

        public T Register<T>(T service) where T : class, IService
        {
            if (service == null)
                return null;
            m_RegisteredServices[typeof(T)] = service;
            var registrationType = service.registrationType;
            if (registrationType != null)
                m_RegisteredServices[registrationType] = service;
            RegisterReverseDependencies(service);
            return service;
        }

        public T Resolve<T>() where T : class, IService
        {
            var service = m_RegisteredServices.TryGetValue(typeof(T), out var result) ? result as T : null;
            if (service == null || service.enabled)
                return service;

            var serviceEnablingQueue = new Queue<IService>();
            serviceEnablingQueue.Enqueue(service);
            while (serviceEnablingQueue.Count > 0)
                EnableService(serviceEnablingQueue.Dequeue(), serviceEnablingQueue);
            return service;
        }

        private void EnableService(IService service, Queue<IService> serviceEnablingQueue)
        {
            if (service == null || service.enabled)
                return;

            foreach (var dependency in service.dependencies ?? Array.Empty<IService>())
                EnableService(dependency, serviceEnablingQueue);

            service.enabled = true;

            // All the reverse dependencies go into the queue to avoid nested enabling
            if (m_ReverseDependencies.TryGetValue(service, out var reverseDependencies))
                foreach (var reverseDependency in reverseDependencies)
                    serviceEnablingQueue.Enqueue(reverseDependency);
        }
    }
}
