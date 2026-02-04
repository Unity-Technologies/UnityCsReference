// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsBody : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new PackageDetailsBody(
                    container.Resolve<IResourceLoader>(),
                    container.Resolve<IPackageDatabase>(),
                    container.Resolve<IPackageOperationDispatcher>(),
                    container.Resolve<IPackageManagerPrefs>(),
                    container.Resolve<IApplicationProxy>(),
                    container.Resolve<IIOProxy>(),
                    container.Resolve<IAssetStoreCache>(),
                    container.Resolve<IPageManager>(),
                    container.Resolve<IUpmCache>(),
                    container.Resolve<IUnityConnectProxy>(),
                    container.Resolve<IPackageLinkFactory>());
            }
        }

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
            IPackageLinkFactory packageLinkFactory)
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

            m_TabView = new PackageDetailsTabView { name = "packageDetailsTabView" };
            m_TabView.onTabSwitched += OnTabSwitched;
            Add(m_TabView);

            AddTabs();
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
            m_TabView.AddTab(new PackageDetailsSamplesTab(m_UnityConnect, m_ResourceLoader, m_PackageDatabase, m_Application, m_IOProxy));
            m_TabView.AddTab(new PackageDetailsImagesTab(m_UnityConnect, m_AssetStoreCache));
        }

        public void OnEnable()
        {
            m_PackageDatabase.onPackagesChanged += RefreshDependencies;
            m_PageManager.onVisualStateChange += RefreshVersionsHistory;

            // restore after domain reload
            m_TabView.SelectTab(m_PackageManagerPrefs.selectedPackageDetailsTabIdentifier);
        }

        public void OnDisable()
        {
            m_PackageDatabase.onPackagesChanged -= RefreshDependencies;
            m_PageManager.onVisualStateChange -= RefreshVersionsHistory;
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

        private void RefreshDependencies()
        {
            m_TabView.RefreshTabs(new[] { FeatureDependenciesTab.k_Id, PackageDetailsDependenciesTab.k_Id }, m_Version);
        }

        private void RefreshDependencies(bool _)
        {
            RefreshDependencies();
        }

        private void RefreshDependencies(PackagesChangeArgs _)
        {
            RefreshDependencies();
        }

        public void OnTabSwitched(PackageDetailsTabElement oldSelection, PackageDetailsTabElement newSelection)
        {
            m_PackageManagerPrefs.selectedPackageDetailsTabIdentifier = newSelection.id;
        }

        private void RefreshVersionsHistory(VisualStateChangeArgs args)
        {
            if (!args.page.isActivePage)
                return;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var newVisualState = args.visualStates.FirstOrDefault(state => state.packageUniqueId == m_Version?.package.uniqueId);
#pragma warning restore UA2001
            if (newVisualState?.userUnlocked == true && m_PageManager.activePage.GetSelection()?.Count == 1)
                m_TabView.RefreshTab(PackageDetailsVersionsTab.k_Id, m_Version);
        }
    }
}
