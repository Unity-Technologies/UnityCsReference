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
            public override object CreateInstance() => new PackageDetailsBody();
        }

        private IResourceLoader m_ResourceLoader;
        private IPackageDatabase m_PackageDatabase;
        private IPackageOperationDispatcher m_OperationDispatcher;
        private IProjectSettingsProxy m_SettingsProxy;
        private IPackageManagerPrefs m_PackageManagerPrefs;
        private ISelectionProxy m_Selection;
        private IAssetDatabaseProxy m_AssetDatabase;
        private IApplicationProxy m_Application;
        private IIOProxy m_IOProxy;
        private IAssetStoreCache m_AssetStoreCache;
        private IPageManager m_PageManager;
        private IUpmCache m_UpmCache;
        private IUnityConnectProxy m_UnityConnect;
        private IPackageLinkFactory m_PackageLinkFactory;

        private PackageDetailsTabView m_TabView;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_SettingsProxy = container.Resolve<IProjectSettingsProxy>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
            m_OperationDispatcher = container.Resolve<IPackageOperationDispatcher>();
            m_PackageManagerPrefs = container.Resolve<IPackageManagerPrefs>();
            m_Selection = container.Resolve<ISelectionProxy>();
            m_AssetDatabase = container.Resolve<IAssetDatabaseProxy>();
            m_Application = container.Resolve<IApplicationProxy>();
            m_IOProxy = container.Resolve<IIOProxy>();
            m_AssetStoreCache = container.Resolve<IAssetStoreCache>();
            m_PageManager = container.Resolve<IPageManager>();
            m_UpmCache = container.Resolve<IUpmCache>();
            m_UnityConnect = container.Resolve<IUnityConnectProxy>();
            m_PackageLinkFactory = container.Resolve<IPackageLinkFactory>();
        }

        private IPackageVersion m_Version;

        public PackageDetailsBody()
        {
            ResolveDependencies();

            m_TabView = new PackageDetailsTabView()
            {
                name = "packageDetailsTabView"
            };
            Add(m_TabView);

            m_TabView.onTabSwitched += OnTabSwitched;

            AddTabs();
        }

        public void AddTabs()
        {
            // The following list of tabs are added in the order we want them to be shown to the users.
            m_TabView.AddTab(new PackageDetailsDescriptionTab(m_UnityConnect, m_ResourceLoader, m_PackageManagerPrefs));
            m_TabView.AddTab(new PackageDetailsOverviewTab(m_UnityConnect, m_ResourceLoader));
            m_TabView.AddTab(new PackageDetailsReleasesTab(m_UnityConnect));
            m_TabView.AddTab(new PackageDetailsImportedAssetsTab(m_UnityConnect, m_IOProxy, m_PackageManagerPrefs));
            m_TabView.AddTab(new PackageDetailsVersionsTab(m_UnityConnect, m_ResourceLoader, m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_OperationDispatcher, m_PageManager, m_SettingsProxy, m_UpmCache, m_PackageLinkFactory));
            m_TabView.AddTab(new PackageDetailsDependenciesTab(m_UnityConnect, m_ResourceLoader, m_PackageDatabase));
            m_TabView.AddTab(new FeatureDependenciesTab(m_UnityConnect, m_ResourceLoader, m_PackageDatabase, m_PackageManagerPrefs, m_Application));
            m_TabView.AddTab(new PackageDetailsSamplesTab(m_UnityConnect, m_ResourceLoader, m_PackageDatabase, m_Selection, m_AssetDatabase, m_Application, m_IOProxy));
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

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Version = version;
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

            var newVisualState = args.visualStates.FirstOrDefault(state => state.packageUniqueId == m_Version?.package.uniqueId);
            if (newVisualState?.userUnlocked == true && m_PageManager.activePage.GetSelection()?.Count == 1)
                m_TabView.RefreshTab(PackageDetailsVersionsTab.k_Id, m_Version);
        }
    }
}
