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
    internal class PageManager : ISerializationCallbackReceiver
    {
        internal const string k_AssetStorePackageGroupName = "Asset Store";
        internal const string k_UnityPackageGroupName = "Unity";
        internal const string k_OtherPackageGroupName = "Other";

        public virtual event Action<IPage> onListRebuild = delegate {};
        public virtual event Action<IPage> onSubPageChanged = delegate {};
        public virtual event Action<IPage, PageFilters> onFiltersChange = delegate {};
        public virtual event Action<PageSelectionChangeArgs> onSelectionChanged = delegate {};
        public virtual event Action<VisualStateChangeArgs> onVisualStateChange = delegate {};
        public virtual event Action<ListUpdateArgs> onListUpdate = delegate {};

        private Dictionary<PackageFilterTab, IPage> m_Pages = new Dictionary<PackageFilterTab, IPage>();

        [SerializeField]
        private SimplePage[] m_SerializedSimplePages = new SimplePage[0];

        [SerializeField]
        private PaginatedPage[] m_SerializedPaginatedPage = new PaginatedPage[0];

        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private PackageManagerPrefs m_PackageManagerPrefs;
        [NonSerialized]
        private AssetStoreClientV2 m_AssetStoreClient;
        [NonSerialized]
        private PackageDatabase m_PackageDatabase;
        [NonSerialized]
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
                                        PackageManagerPrefs packageManagerPrefs,
                                        AssetStoreClientV2 assetStoreClient,
                                        PackageDatabase packageDatabase,
                                        PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_UnityConnect = unityConnect;
            m_PackageManagerPrefs = packageManagerPrefs;
            m_AssetStoreClient = assetStoreClient;
            m_PackageDatabase = packageDatabase;
            m_SettingsProxy = settingsProxy;

            foreach (var page in m_SerializedSimplePages)
                page.ResolveDependencies(packageDatabase, packageManagerPrefs);
            foreach (var page in m_SerializedPaginatedPage)
                page.ResolveDependencies(packageDatabase, packageManagerPrefs, unityConnect, assetStoreClient);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedSimplePages = m_Pages.Values.Select(p => p as SimplePage).Where(p => p != null).ToArray();
            m_SerializedPaginatedPage = m_Pages.Values.Select(p => p as PaginatedPage).Where(p => p != null).ToArray();
        }

        public void OnAfterDeserialize()
        {
            foreach (var page in m_SerializedSimplePages.Cast<IPage>().Concat(m_SerializedPaginatedPage))
            {
                m_Pages[page.tab] = page;
                RegisterPageEvents(page);
            }
        }

        private IPage CreatePageFromTab(PackageFilterTab filterTab)
        {
            IPage page;
            if (filterTab == PackageFilterTab.AssetStore)
            {
                page = new PaginatedPage(L10n.Tr("assets"), m_PackageDatabase, m_PackageManagerPrefs, m_UnityConnect, m_AssetStoreClient, filterTab, new PageCapability
                {
                    requireUserLoggedIn = true,
                    requireNetwork = true,
                    supportFilters = true,
                    orderingValues = new[]
                    {
                        new PageCapability.Ordering("Purchased date", "purchased_date", PageCapability.Order.Descending),
                        new PageCapability.Ordering("Recently updated", "update_date", PageCapability.Order.Descending),
                        new PageCapability.Ordering("Name (asc)", "name", PageCapability.Order.Ascending),
                        new PageCapability.Ordering("Name (desc)", "name", PageCapability.Order.Descending)
                    }
                });
            }
            else
            {
                var orderings = new List<PageCapability.Ordering>();
                orderings.Add(new PageCapability.Ordering("Name (asc)", "displayName", PageCapability.Order.Ascending));
                orderings.Add(new PageCapability.Ordering("Name (desc)", "displayName", PageCapability.Order.Descending));
                if (filterTab != PackageFilterTab.BuiltIn)
                    orderings.Add(new PageCapability.Ordering("Published date", "publishedDate", PageCapability.Order.Descending));
                if (filterTab == PackageFilterTab.InProject)
                    orderings.Add(new PageCapability.Ordering("Recently updated", "updateDate", PageCapability.Order.Descending));
                page = new SimplePage(m_PackageDatabase, m_PackageManagerPrefs, filterTab, new PageCapability
                {
                    supportLocalReordering = true,
                    supportFilters = filterTab != PackageFilterTab.BuiltIn,
                    orderingValues = orderings.ToArray()
                });
            }
            page.OnEnable();
            m_Pages[filterTab] = page;
            RegisterPageEvents(page);
            return page;
        }

        private void RegisterPageEvents(IPage page)
        {
            page.onVisualStateChange += args => onVisualStateChange?.Invoke(args);
            page.onListUpdate += args => onListUpdate?.Invoke(args);
            page.onSelectionChanged += args => onSelectionChanged?.Invoke(args);
            page.onListRebuild += p => onListRebuild?.Invoke(p);
            page.onSubPageChanged += p => onSubPageChanged?.Invoke(p);
            page.onFiltersChange += filters => onFiltersChange?.Invoke(page, filters);
        }

        public virtual IPage GetPage(PackageFilterTab? tab = null)
        {
            var filterTab = tab ?? m_PackageManagerPrefs.currentFilterTab;
            return m_Pages.TryGetValue(filterTab, out var page) ? page : CreatePageFromTab(filterTab);
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            GetPage().OnPackagesChanged(args);
        }

        private void OnFilterChanged(PackageFilterTab filterTab)
        {
            GetPage(filterTab).OnActivated();
            if (m_PackageManagerPrefs.previousFilterTab != null)
                GetPage(m_PackageManagerPrefs.previousFilterTab.Value).OnDeactivated();
        }

        private void OnSearchTextChanged(string trimmedSearchText)
        {
            GetPage().UpdateSearchText(trimmedSearchText);
        }

        public virtual IPage FindPage(IPackage package, IPackageVersion version = null)
        {
            return FindPage(new[] { version ?? package?.versions.primary });
        }

        public virtual IPage FindPage(IEnumerable<IPackageVersion> packageVersions)
        {
            var currentPage = GetPage();
            if (packageVersions.All(v => currentPage.visualStates.Contains(v.package.uniqueId)))
                return currentPage;

            var firstPackageVersion = packageVersions?.FirstOrDefault();
            if (firstPackageVersion == null)
                return GetPage(m_PackageManagerPrefs.currentFilterTab);

            // Since built in packages can never be in other tabs, we only need to check the first item to know which tab these packages belong to
            if (firstPackageVersion.HasTag(PackageTag.BuiltIn))
                return GetPage(PackageFilterTab.BuiltIn);

            if (!m_SettingsProxy.enablePreReleasePackages && packageVersions.Any(v => v.version?.Prerelease.StartsWith("pre.") == true))
            {
                Debug.Log("You must check \"Enable Pre-release Packages\" in Project Settings > Package Manager in order to see this package.");
                return GetPage(m_PackageManagerPrefs.currentFilterTab);
            }

            if (packageVersions.All(v => v.package.versions.installed != null || (v.package.progress == PackageProgress.Installing && v.package.versions.primary.HasTag(PackageTag.Placeholder))))
                return GetPage(PackageFilterTab.InProject);

            if (packageVersions.All(v => v.availableRegistry == RegistryType.UnityRegistry))
                return GetPage(PackageFilterTab.UnityRegistry);

            if (packageVersions.All(v => v.HasTag(PackageTag.LegacyFormat)))
                return GetPage(PackageFilterTab.AssetStore);

            return GetPage(PackageFilterTab.MyRegistries);
        }

        public void OnEnable()
        {
            InitializeSubPages();

            foreach (var page in m_Pages.Values)
                page.OnEnable();

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
            m_PackageManagerPrefs.onFilterTabChanged += OnFilterChanged;
            m_PackageManagerPrefs.onTrimmedSearchTextChanged += OnSearchTextChanged;
        }

        public void OnDisable()
        {
            foreach (var page in m_Pages.Values)
                page.OnDisable();

            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
            m_PackageManagerPrefs.onFilterTabChanged -= OnFilterChanged;
            m_PackageManagerPrefs.onTrimmedSearchTextChanged -= OnSearchTextChanged;
        }

        private void InitializeSubPages()
        {
            static bool FilterAllPackages(IPackage package) => true;

            static string GroupPackagesAndFeatures(IPackage package)
            {
                var version = package?.versions.primary;
                return version?.HasTag(PackageTag.Feature) == true ? L10n.Tr("Features") : L10n.Tr("Packages");
            }

            static string GroupPackagesWithAuthorAndFeatures(IPackage package)
            {
                var version = package?.versions.primary;
                if (version?.HasTag(PackageTag.Feature) == true && version.HasTag(PackageTag.Unity))
                    return L10n.Tr("Features");
                return string.Format(L10n.Tr("Packages - {0}"), BasePage.GetDefaultGroupName(PackageFilterTab.InProject, package));
            }

            GetPage(PackageFilterTab.UnityRegistry).AddSubPage(new SubPage(PackageFilterTab.UnityRegistry, "all", L10n.Tr("All"), L10n.Tr("packages and features"), 0, FilterAllPackages, GroupPackagesAndFeatures));
            GetPage(PackageFilterTab.InProject).AddSubPage(new SubPage(PackageFilterTab.InProject, "all", L10n.Tr("All"), L10n.Tr("packages and features"), 0, FilterAllPackages, GroupPackagesWithAuthorAndFeatures));
        }
    }
}
