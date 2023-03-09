// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsBody : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetailsBody> {}

        private ResourceLoader m_ResourceLoader;
        private PackageDatabase m_PackageDatabase;
        private PackageOperationDispatcher m_OperationDispatcher;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private SelectionProxy m_Selection;
        private AssetDatabaseProxy m_AssetDatabase;
        private ApplicationProxy m_Application;
        private IOProxy m_IOProxy;
        private AssetStoreCache m_AssetStoreCache;
        private PageManager m_PageManager;
        private UpmCache m_UpmCache;

        private PackageDetailsTabView m_TabView;

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_OperationDispatcher = container.Resolve<PackageOperationDispatcher>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_Selection = container.Resolve<SelectionProxy>();
            m_AssetDatabase = container.Resolve<AssetDatabaseProxy>();
            m_Application = container.Resolve<ApplicationProxy>();
            m_IOProxy = container.Resolve<IOProxy>();
            m_AssetStoreCache = container.Resolve<AssetStoreCache>();
            m_PageManager = container.Resolve<PageManager>();
            m_UpmCache = container.Resolve<UpmCache>();
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
            m_TabView.AddTab(new PackageDetailsDescriptionTab(m_ResourceLoader, m_PackageManagerPrefs));
            m_TabView.AddTab(new PackageDetailsOverviewTab(m_ResourceLoader));
            m_TabView.AddTab(new PackageDetailsReleasesTab());
            m_TabView.AddTab(new PackageDetailsImportedAssetsTab(m_IOProxy, m_PackageManagerPrefs));
            m_TabView.AddTab(new PackageDetailsVersionsTab(m_ResourceLoader, m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_OperationDispatcher, m_PageManager, m_SettingsProxy, m_UpmCache, m_IOProxy));
            m_TabView.AddTab(new PackageDetailsDependenciesTab(m_ResourceLoader, m_PackageDatabase));
            m_TabView.AddTab(new FeatureDependenciesTab(m_ResourceLoader, m_PackageDatabase, m_PackageManagerPrefs, m_SettingsProxy, m_Application));
            m_TabView.AddTab(new PackageDetailsSamplesTab(m_ResourceLoader, m_PackageDatabase, m_Selection, m_AssetDatabase, m_Application, m_IOProxy));
            m_TabView.AddTab(new PackageDetailsImagesTab(m_AssetStoreCache));
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
