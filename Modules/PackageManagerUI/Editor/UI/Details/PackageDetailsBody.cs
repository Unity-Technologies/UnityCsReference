// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class PackageDetailsBody : VisualElement
    {
        private IPackageVersion m_Version;
        private readonly PackageDetailsTabView m_TabView;

        private readonly IResourceLoader m_ResourceLoader;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPackageOperationDispatcher m_OperationDispatcher;
        private readonly IPackageManagerPrefs m_PackageManagerPrefs;
        private readonly IApplicationProxy m_Application;
        private readonly IIOProxy m_IOProxy;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IPageManager m_PageManager;
        private readonly IUpmCache m_UpmCache;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IPackageLinkFactory m_PackageLinkFactory;
        private readonly IDelayedSelectionHandler m_DelayedSelectionHandler;
        private readonly ISampleImporter m_SampleImporter;

        public PackageDetailsBody() : this(
            ServicesContainer.instance.Resolve<IResourceLoader>(),
            ServicesContainer.instance.Resolve<IPackageDatabase>(),
            ServicesContainer.instance.Resolve<IPackageOperationDispatcher>(),
            ServicesContainer.instance.Resolve<IPackageManagerPrefs>(),
            ServicesContainer.instance.Resolve<IApplicationProxy>(),
            ServicesContainer.instance.Resolve<IIOProxy>(),
            ServicesContainer.instance.Resolve<IAssetStoreCache>(),
            ServicesContainer.instance.Resolve<IPageManager>(),
            ServicesContainer.instance.Resolve<IUpmCache>(),
            ServicesContainer.instance.Resolve<IUnityConnectProxy>(),
            ServicesContainer.instance.Resolve<IPackageLinkFactory>(),
            ServicesContainer.instance.Resolve<IDelayedSelectionHandler>(),
            ServicesContainer.instance.Resolve<ISampleImporter>())
        {
        }

        public PackageDetailsBody(
            IResourceLoader resourceLoader,
            IPackageDatabase packageDatabase,
            IPackageOperationDispatcher operationDispatcher,
            IPackageManagerPrefs packageManagerPrefs,
            IApplicationProxy application,
            IIOProxy ioProxy,
            IAssetStoreCache assetStoreCache,
            IPageManager pageManager,
            IUpmCache upmCache,
            IUnityConnectProxy unityConnect,
            IPackageLinkFactory packageLinkFactory,
            IDelayedSelectionHandler delayedSelectionHandler,
            ISampleImporter sampleImporter)
        {
            m_ResourceLoader = resourceLoader;
            m_PackageDatabase = packageDatabase;
            m_OperationDispatcher = operationDispatcher;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_Application = application;
            m_IOProxy = ioProxy;
            m_AssetStoreCache = assetStoreCache;
            m_PageManager = pageManager;
            m_UpmCache = upmCache;
            m_UnityConnect = unityConnect;
            m_PackageLinkFactory = packageLinkFactory;
            m_DelayedSelectionHandler = delayedSelectionHandler;
            m_SampleImporter = sampleImporter;

            m_TabView = new PackageDetailsTabView { name = "packageDetailsTabView" };
            m_TabView.onTabSwitched += OnTabSwitched;
            Add(m_TabView);

            AddTabs();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_PackageDatabase.onSamplesChanged += RefreshSamplesTab;

            m_TabView.SelectTab(m_PackageManagerPrefs.selectedPackageDetailsTabIdentifier);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_PackageDatabase.onSamplesChanged -= RefreshSamplesTab;
        }

        public void AddTabs()
        {
            // The following list of tabs are added in the order we want them to be shown to the users.
            m_TabView.AddTab(new PackageDetailsDetailsTab(m_UnityConnect, m_ResourceLoader, m_Application, m_UpmCache));
            m_TabView.AddTab(new PackageDetailsOverviewTab(m_UnityConnect, m_ResourceLoader, m_UpmCache));
            m_TabView.AddTab(new PackageDetailsReleasesTab(m_UnityConnect));
            m_TabView.AddTab(new PackageDetailsImportedAssetsTab(m_UnityConnect, m_PackageManagerPrefs));
            m_TabView.AddTab(new PackageDetailsVersionsTab(m_UnityConnect, m_ResourceLoader, m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_OperationDispatcher, m_PageManager, m_UpmCache, m_PackageLinkFactory));
            m_TabView.AddTab(new PackageDetailsDependenciesTab(m_UnityConnect, m_ResourceLoader, m_PackageDatabase));
            m_TabView.AddTab(new FeatureDependenciesTab(m_UnityConnect, m_ResourceLoader, m_PackageDatabase, m_PackageManagerPrefs, m_Application));
            m_TabView.AddTab(new PackageDetailsSamplesTab(m_UnityConnect, m_ResourceLoader, m_PackageDatabase, m_Application, m_IOProxy, m_DelayedSelectionHandler, m_SampleImporter));
            m_TabView.AddTab(new PackageDetailsImagesTab(m_UnityConnect, m_AssetStoreCache));
        }

        private void RefreshSamplesTab(SamplesChangeArgs args)
        {
            if (m_Version != null && args.added.Join(args.updated, args.removed).AnyMatches(i => i.packageUniqueId == m_Version.package.uniqueId))
                m_TabView.RefreshTabs(new[] { PackageDetailsSamplesTab.k_Id }, m_Version);
        }

        public void Refresh(IPackage package)
        {
            m_Version = package.versions.primary;
            RefreshTabs();
        }

        private void RefreshTabs()
        {
            m_TabView.RefreshAllTabs(m_Version);
        }

        public void OnTabSwitched(PackageDetailsTabElement oldSelection, PackageDetailsTabElement newSelection)
        {
            m_PackageManagerPrefs.selectedPackageDetailsTabIdentifier = newSelection.id;
        }
    }
}
